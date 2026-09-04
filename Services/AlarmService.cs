using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Serilog;
using TwoD5DMonitoring.Infrastructure.Logging;
using TwoD5DMonitoring.Infrastructure.PLC;
using TwoD5DMonitoring.Models;
using TwoD5DMonitoring.Services.Repositories;

namespace TwoD5DMonitoring.Services
{
    /// <summary>
    /// 报警管理服务
    /// </summary>
    public class AlarmService
    {
        private readonly ILogger _logger;
        private readonly IAlarmDefinitionRepository _definitionRepository;
        private readonly IAlarmInstanceRepository _instanceRepository;
        private readonly IAlarmHistoryRepository _historyRepository;
        private readonly IAlarmNotificationRepository _notificationRepository;
        private readonly IAlarmSuppressionRepository _suppressionRepository;
        private readonly PlcManager _plcManager;

        // 内存中的活动报警缓存
        private readonly ConcurrentDictionary<int, AlarmInstance> _activeAlarms = new();
        private readonly Timer _evaluationTimer;
        private readonly Timer _cleanupTimer;
        private bool _isRunning;

        public event EventHandler<AlarmEventArgs>? AlarmRaised;
        public event EventHandler<AlarmEventArgs>? AlarmAcknowledged;
        public event EventHandler<AlarmEventArgs>? AlarmReturned;
        public event EventHandler<AlarmEventArgs>? AlarmShelved;

        public AlarmService(
            IAlarmDefinitionRepository definitionRepository,
            IAlarmInstanceRepository instanceRepository,
            IAlarmHistoryRepository historyRepository,
            IAlarmNotificationRepository notificationRepository,
            IAlarmSuppressionRepository suppressionRepository,
            PlcManager plcManager)
        {
            _logger = LogManager.CreateModuleLogger(nameof(AlarmService));
            _definitionRepository = definitionRepository;
            _instanceRepository = instanceRepository;
            _historyRepository = historyRepository;
            _notificationRepository = notificationRepository;
            _suppressionRepository = suppressionRepository;
            _plcManager = plcManager;

            // 评估定时器 - 每秒检查一次
            _evaluationTimer = new Timer(EvaluateAlarms, null, Timeout.Infinite, Timeout.Infinite);
            // 清理定时器 - 每小时清理一次
            _cleanupTimer = new Timer(CleanupOldAlarms, null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));
        }

        public async Task StartAsync()
        {
            if (_isRunning) return;

            // 加载启用的报警定义
            var definitions = await _definitionRepository.GetAllAsync(new AlarmDefinitionFilter { IsEnabled = true });
            _logger.Information("报警服务启动，加载报警定义: {Count} 个", definitions.Count());

            // 从数据库恢复活动报警
            await RestoreActiveAlarmsAsync();

            _evaluationTimer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1));
            _isRunning = true;
            _logger.Information("报警服务已启动");
        }

        public async Task StopAsync()
        {
            if (!_isRunning) return;

            _evaluationTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _cleanupTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _isRunning = false;

            // 持久化活动报警到数据库
            foreach (var alarm in _activeAlarms.Values)
            {
                await _instanceRepository.UpdateAsync(alarm);
            }

            _logger.Information("报警服务已停止");
        }

        private async Task RestoreActiveAlarmsAsync()
        {
            var activeAlarms = await _instanceRepository.GetActiveAlarmsAsync();
            foreach (var alarm in activeAlarms)
            {
                _activeAlarms[alarm.Id] = alarm;
            }
            _logger.Information("恢复活动报警: {Count} 个", _activeAlarms.Count);
        }

        private async void EvaluateAlarms(object? state)
        {
            if (!_isRunning) return;

            try
            {
                var definitions = await _definitionRepository.GetAllAsync(new AlarmDefinitionFilter { IsEnabled = true });
                
                foreach (var def in definitions)
                {
                    await EvaluateAlarmDefinition(def);
                }

                // 检查已恢复正常的报警
                CheckReturnedAlarms();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "报警评估循环异常");
            }
        }

        private async Task EvaluateAlarmDefinition(AlarmDefinition def)
        {
            try
            {
                // 读取 PLC 标签值
                var value = await ReadTagValueAsync(def);
                if (value == null) return;

                // 检查是否被抑制
                if (await IsSuppressed(def, DateTime.Now))
                    return;

                var shouldAlarm = CheckAlarmCondition(def, value.Value);

                // 获取或创建报警实例
                var instance = await GetOrCreateInstance(def);

                if (shouldAlarm && instance.State != AlarmState.Active && instance.State != AlarmState.Acknowledged)
                {
                    // 触发报警
                    await ActivateAlarmAsync(instance, value.Value);
                }
                else if (!shouldAlarm && (instance.State == AlarmState.Active || instance.State == AlarmState.Acknowledged))
                {
                    // 报警条件消失，自动恢复
                    await ReturnToNormalAsync(instance, value.Value);
                }
                else if (shouldAlarm && (instance.State == AlarmState.Active || instance.State == AlarmState.Acknowledged))
                {
                    // 更新当前值
                    instance.CurrentValue = value.Value;
                    await _instanceRepository.UpdateAsync(instance);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "评估报警定义异常: {TagName}", def.TagName);
            }
        }

        private bool CheckAlarmCondition(AlarmDefinition def, double value)
        {
            return def.AlarmType switch
            {
                AlarmType.HiHi => value >= def.Setpoint,
                AlarmType.Hi => value >= def.Setpoint,
                AlarmType.Lo => value <= def.Setpoint,
                AlarmType.LoLo => value <= def.Setpoint,
                AlarmType.ROC => Math.Abs(value - _lastValues.GetValueOrDefault(def.TagName, value)) > def.Setpoint, // 简化：使用设定值作为变化率阈值
                AlarmType.Dev => false, // 偏差报警需要参考值，暂不实现
                AlarmType.Digital => value != 0,
                AlarmType.System => false, // 系统报警由外部触发
                AlarmType.Communication => false, // 通讯报警由外部触发
                _ => false
            };
        }

        private readonly ConcurrentDictionary<string, double> _lastValues = new();

        private async Task<double?> ReadTagValueAsync(AlarmDefinition def)
        {
            if (string.IsNullOrEmpty(def.PlcTagAddress))
            {
                // 尝试通过 TagName 读取
                var parts = def.TagName.Split('.');
                if (parts.Length >= 2)
                {
                    var plcName = parts[0];
                    var tagName = parts[1];
                    var driver = _plcManager.GetDriver(plcName);
                    if (driver != null && driver.ConnectionState == PlcConnectionState.Connected)
                    {
                        double? val = await driver.ReadAsync<double>(tagName);
                        if (val.HasValue)
                        {
                            _lastValues[def.TagName] = val.Value;
                            return val;
                        }
                    }
                }
                return null;
            }

            // 通过 PLC 标签地址读取
            // 格式: PLC名称.标签名
            var addrParts = def.PlcTagAddress.Split('.');
            if (addrParts.Length >= 2)
            {
                var plcName = addrParts[0];
                var tagName = addrParts[1];
                var driver = _plcManager.GetDriver(plcName);
                if (driver != null && driver.ConnectionState == PlcConnectionState.Connected)
                {
                    double? val = await driver.ReadAsync<double>(tagName);
                    if (val.HasValue)
                    {
                        _lastValues[def.TagName] = val.Value;
                        return val;
                    }
                }
            }

            return null;
        }

        private async Task<AlarmInstance> GetOrCreateInstance(AlarmDefinition def)
        {
            // 先查找内存中是否已存在该定义的活动报警
            var existing = _activeAlarms.Values.FirstOrDefault(x => x.DefinitionId == def.Id && 
                (x.State == AlarmState.Active || x.State == AlarmState.Acknowledged));

            if (existing != null)
                return existing;

            // 数据库中查找
            var instances = await _instanceRepository.GetActiveAlarmsAsync();
            var dbInstance = instances.FirstOrDefault(x => x.DefinitionId == def.Id);
            
            if (dbInstance != null)
            {
                _activeAlarms[dbInstance.Id] = dbInstance;
                return dbInstance;
            }

            // 创建新实例
            var newInstance = new AlarmInstance
            {
                DefinitionId = def.Id,
                State = AlarmState.Inactive,
                CurrentValue = 0,
                Setpoint = def.Setpoint,
                OccurTime = DateTime.Now,
                OccurrenceCount = 1
            };

            return await _instanceRepository.AddAsync(newInstance);
        }

        private async Task ActivateAlarmAsync(AlarmInstance instance, double value)
        {
            instance.State = AlarmState.Active;
            instance.CurrentValue = value;
            instance.OccurTime = DateTime.Now;
            instance.Setpoint = (await _definitionRepository.GetByIdAsync(instance.DefinitionId))?.Setpoint ?? 0;

            await _instanceRepository.UpdateAsync(instance);
            _activeAlarms[instance.Id] = instance;

            // 发送事件
            var def = await _definitionRepository.GetByIdAsync(instance.DefinitionId);
            var args = new AlarmEventArgs
            {
                AlarmInstance = instance,
                Definition = def!,
                EventType = AlarmEventType.Raised,
                Timestamp = DateTime.Now
            };

            AlarmRaised?.Invoke(this, args);

            // 记录日志
            _logger.Warning("报警触发: {AlarmName} [{TagName}] = {Value} (设定值: {Setpoint})", 
                def?.AlarmName, def?.TagName, value, def?.Setpoint);

            // 发送通知
            await SendNotificationsAsync(instance, def!);
        }

        private void CheckReturnedAlarms()
        {
            var toReturn = _activeAlarms.Values
                .Where(x => x.State == AlarmState.Active || x.State == AlarmState.Acknowledged)
                .ToList();

            foreach (var instance in toReturn)
            {
                // 这里需要重新读取值判断是否恢复正常
                // 简化处理：在 EvaluateAlarmDefinition 中处理
            }
        }

        private async Task ReturnToNormalAsync(AlarmInstance instance, double value)
        {
            var def = await _definitionRepository.GetByIdAsync(instance.DefinitionId);
            
            instance.State = AlarmState.Inactive;
            instance.ReturnTime = DateTime.Now;
            instance.CurrentValue = value;

            await _instanceRepository.UpdateAsync(instance);
            _activeAlarms.TryRemove(instance.Id, out _);

            // 归档到历史表
            await ArchiveToHistoryAsync(instance, def);

            var args = new AlarmEventArgs
            {
                AlarmInstance = instance,
                Definition = def!,
                EventType = AlarmEventType.Returned,
                Timestamp = DateTime.Now
            };

            AlarmReturned?.Invoke(this, args);

            _logger.Information("报警恢复: {AlarmName} [{TagName}] = {Value}", 
                def?.AlarmName, def?.TagName, value);
        }

        private async Task ArchiveToHistoryAsync(AlarmInstance instance, AlarmDefinition? def)
        {
            if (def == null) return;

            var history = new AlarmHistory
            {
                DefinitionId = instance.DefinitionId,
                FinalState = instance.State,
                OccurValue = instance.CurrentValue,
                ReturnValue = instance.CurrentValue,
                OccurTime = instance.OccurTime,
                AckTime = instance.AckTime,
                ReturnTime = instance.ReturnTime ?? DateTime.Now,
                AckedBy = instance.AckedBy,
                Comment = instance.Comment,
                OccurrenceCount = instance.OccurrenceCount,
                TotalActiveTime = instance.ReturnTime.HasValue 
                    ? instance.ReturnTime.Value - instance.OccurTime 
                    : DateTime.Now - instance.OccurTime,
                TagName = def.TagName,
                AlarmName = def.AlarmName,
                Priority = def.Priority,
                AlarmType = def.AlarmType,
                EquipmentName = "" // TODO: 获取设备名称
            };

            await _historyRepository.AddAsync(history);
        }

        private async Task SendNotificationsAsync(AlarmInstance instance, AlarmDefinition def)
        {
            var notifications = await _notificationRepository.GetEnabledAsync();
            var applicable = notifications.Where(n => (int)def.Priority >= (int)n.MinPriority);

            foreach (var notification in applicable)
            {
                // 检查抑制规则
                if (await IsSuppressed(def, DateTime.Now))
                    continue;

                // 发送通知
                await SendNotificationAsync(notification, instance, def);
            }
        }

        private async Task<bool> IsSuppressed(AlarmDefinition def, DateTime time)
        {
            return await _suppressionRepository.IsSuppressedAsync(def.Id, time);
        }

        private async void CleanupOldAlarms(object? state)
        {
            try
            {
                // 清理超过 30 天的历史报警
                var cutoffDate = DateTime.Now.AddDays(-30);
                // TODO: 实现清理逻辑
                _logger.Information("清理旧报警记录，截止日期: {CutoffDate}", cutoffDate);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "清理旧报警异常");
            }
        }

        #region 公共方法

        public async Task<AlarmDefinition> CreateDefinitionAsync(AlarmDefinition definition, string operatorName)
        {
            definition.CreatedAt = DateTime.Now;
            definition.UpdatedAt = DateTime.Now;
            return await _definitionRepository.AddAsync(definition);
        }

        public async Task<AlarmDefinition> UpdateDefinitionAsync(AlarmDefinition definition, string operatorName)
        {
            definition.UpdatedAt = DateTime.Now;
            return await _definitionRepository.UpdateAsync(definition);
        }

        public async Task<bool> DeleteDefinitionAsync(int id)
        {
            // 检查是否有关联实例
            // 简化：直接删除
            return await _definitionRepository.DeleteAsync(id);
        }

        public async Task<(IEnumerable<AlarmDefinition> Items, int TotalCount)> GetDefinitionsPagedAsync(
            int page, int pageSize, AlarmDefinitionFilter filter)
        {
            var items = await _definitionRepository.GetAllAsync(filter);
            var total = await _definitionRepository.GetCountAsync(filter);
            return (items, total);
        }

        public async Task<AcknowledgeResult> AcknowledgeAlarmAsync(int instanceId, string ackedBy, string comment)
        {
            var instance = await _instanceRepository.GetByIdAsync(instanceId);
            if (instance == null)
                return new AcknowledgeResult { Success = false, ErrorMessage = "报警实例不存在" };

            if (instance.State == AlarmState.Inactive)
                return new AcknowledgeResult { Success = false, ErrorMessage = "报警已恢复，无需确认" };

            var success = await _instanceRepository.AcknowledgeAsync(instanceId, ackedBy, comment);
            
            if (success)
            {
                instance.State = AlarmState.Acknowledged;
                instance.AckTime = DateTime.Now;
                instance.AckedBy = ackedBy;
                instance.Comment = comment;

                var def = await _definitionRepository.GetByIdAsync(instance.DefinitionId);
                var args = new AlarmEventArgs
                {
                    AlarmInstance = instance,
                    Definition = def!,
                    EventType = AlarmEventType.Acknowledged,
                    Timestamp = DateTime.Now
                };

                AlarmAcknowledged?.Invoke(this, args);

                _logger.Information("报警确认: {AlarmName} 由 {Operator} 确认", 
                    (await _definitionRepository.GetByIdAsync(instance.DefinitionId))?.AlarmName, ackedBy);

                return new AcknowledgeResult { Success = true };
            }

            return new AcknowledgeResult { Success = false, ErrorMessage = "确认失败" };
        }

        public async Task<ShelveResult> ShelveAlarmAsync(int instanceId, bool shelve, string operatorName)
        {
            var instance = await _instanceRepository.GetByIdAsync(instanceId);
            if (instance == null)
                return new ShelveResult { Success = false, ErrorMessage = "报警实例不存在" };

            var success = await _instanceRepository.ShelveAsync(instanceId, shelve);
            
            if (success)
            {
                instance.IsShelved = shelve;
                
                var def = await _definitionRepository.GetByIdAsync(instance.DefinitionId);
                var args = new AlarmEventArgs
                {
                    AlarmInstance = instance,
                    Definition = def!,
                    EventType = shelve ? AlarmEventType.Shelved : AlarmEventType.Unshelved,
                    Timestamp = DateTime.Now
                };

                AlarmShelved?.Invoke(this, args);

                return new ShelveResult { Success = true };
            }

            return new ShelveResult { Success = false, ErrorMessage = shelve ? "屏蔽失败" : "取消屏蔽失败" };
        }

        public async Task<(IEnumerable<AlarmInstance> Items, int TotalCount)> GetActiveAlarmsPagedAsync(
            int page, int pageSize, AlarmFilter filter)
        {
            return await _instanceRepository.GetPagedAsync(page, pageSize, filter);
        }

        public async Task<(IEnumerable<AlarmHistory> Items, int TotalCount)> GetHistoryPagedAsync(
            int page, int pageSize, AlarmHistoryFilter filter)
        {
            return await _historyRepository.GetPagedAsync(page, pageSize, filter);
        }

        public async Task<Dictionary<AlarmPriority, int>> GetStatisticsAsync(DateTime from, DateTime to)
        {
            return await _historyRepository.GetStatisticsAsync(from, to);
        }

        public async Task<IEnumerable<AlarmGroup>> GetAlarmGroupsAsync()
        {
            return await _definitionRepository.GetGroupsAsync();
        }

        public async Task<AlarmGroup> CreateGroupAsync(AlarmGroup group)
        {
            return await _definitionRepository.AddGroupAsync(group);
        }

        public int GetActiveAlarmsCount()
        {
            return _activeAlarms.Count(x => x.Value.State == AlarmState.Active || x.Value.State == AlarmState.Acknowledged);
        }

        #region 报警关联分析/趋势分析

        /// <summary>
        /// 获取报警关联分析 - 找出经常一起出现的报警对
        /// </summary>
        public async Task<List<AlarmCorrelation>> GetAlarmCorrelationsAsync(DateTime from, DateTime to, int minOccurrences = 5)
        {
            var history = await _historyRepository.GetPagedAsync(1, int.MaxValue, 
                new AlarmHistoryFilter { OccurFrom = from, OccurTo = to });
            
            var alarms = history.Items.Where(x => x.FinalState == AlarmState.Acknowledged || x.FinalState == AlarmState.Active).ToList();
            
            // 按时间窗口分组 (5分钟窗口)
            var windowMinutes = 5;
            var groups = alarms
                .GroupBy(x => new DateTime(x.OccurTime.Ticks / (windowMinutes * 60 * 10000000) * (windowMinutes * 60 * 10000000)))
                .Where(g => g.Count() >= 2)
                .ToList();

            var correlations = new Dictionary<string, AlarmCorrelation>();
            
            foreach (var group in groups)
            {
                var groupAlarms = group.ToList();
                for (int i = 0; i < groupAlarms.Count; i++)
                {
                    for (int j = i + 1; j < groupAlarms.Count; j++)
                    {
                        var key1 = $"{groupAlarms[i].DefinitionId}_{groupAlarms[j].DefinitionId}";
                        var reverseKey = $"{groupAlarms[j].DefinitionId}_{groupAlarms[i].DefinitionId}";
                        var pairKey = string.Compare(key1, reverseKey) < 0 ? key1 : reverseKey;
                        
                        if (!correlations.ContainsKey(pairKey))
                        {
                            var def1 = await _definitionRepository.GetByIdAsync(groupAlarms[i].DefinitionId);
                            var def2 = await _definitionRepository.GetByIdAsync(groupAlarms[j].DefinitionId);
                            correlations[pairKey] = new AlarmCorrelation
                            {
                                DefinitionId1 = groupAlarms[i].DefinitionId,
                                DefinitionId2 = groupAlarms[j].DefinitionId,
                                AlarmName1 = groupAlarms[i].AlarmName,
                                AlarmName2 = groupAlarms[j].AlarmName,
                                TagName1 = groupAlarms[i].TagName,
                                TagName2 = groupAlarms[j].TagName,
                                CoOccurrenceCount = 0,
                                TotalOccurrences1 = 0,
                                TotalOccurrences2 = 0
                            };
                        }
                        correlations[pairKey].CoOccurrenceCount++;
                    }
                }
                
                // 统计单个报警总次数
                foreach (var alarm in groupAlarms)
                {
                    var key1 = groupAlarms.FirstOrDefault(a => a.DefinitionId == alarm.DefinitionId);
                    if (key1 != null)
                    {
                        var key = $"{alarm.DefinitionId}_0";
                        if (correlations.ContainsKey(key))
                            correlations[key].TotalOccurrences1++;
                    }
                }
            }

            // 计算置信度和提升度
            var totalWindows = groups.Count;
            var allAlarms = groups.SelectMany(g => g).ToList();
            var result = correlations.Values
                .Where(c => c.CoOccurrenceCount >= minOccurrences)
                .Select(c =>
                {
                    var total1 = allAlarms.Count(x => x.DefinitionId == c.DefinitionId1);
                    var total2 = allAlarms.Count(x => x.DefinitionId == c.DefinitionId2);
                    c.Support = (double)c.CoOccurrenceCount / totalWindows;
                    c.Confidence = total1 > 0 ? (double)c.CoOccurrenceCount / total1 : 0;
                    c.Lift = (total1 > 0 && total2 > 0) ? (double)c.CoOccurrenceCount * totalWindows / (total1 * total2) : 0;
                    return c;
                })
                .OrderByDescending(c => c.Lift)
                .ToList();

            return result;
        }

        /// <summary>
        /// 获取报警趋势统计
        /// </summary>
        public async Task<AlarmTrendStatistics> GetAlarmTrendStatisticsAsync(DateTime from, DateTime to, int intervalMinutes = 60)
        {
            var history = await _historyRepository.GetPagedAsync(1, int.MaxValue, 
                new AlarmHistoryFilter { OccurFrom = from, OccurTo = to });
            
            var alarms = history.Items.ToList();
            
            // 按时间间隔分组
            var intervalTicks = TimeSpan.FromMinutes(intervalMinutes).Ticks;
            var groups = history.Items
                .GroupBy(x => new DateTime(x.OccurTime.Ticks / intervalTicks * intervalTicks))
                .OrderBy(g => g.Key)
                .ToList();

            var timeSeries = groups.Select(g => new AlarmTimePoint
            {
                Time = g.Key,
                TotalCount = g.Count(),
                ActiveCount = g.Count(x => x.FinalState == AlarmState.Active),
                AcknowledgedCount = g.Count(x => x.FinalState == AlarmState.Acknowledged),
                ReturnedCount = g.Count(x => x.FinalState == AlarmState.Returned),
                ByPriority = g.GroupBy(x => x.Priority).ToDictionary(g => g.Key, g => g.Count()),
                ByType = g.GroupBy(x => x.AlarmType).ToDictionary(g => g.Key, g => g.Count()),
                ByCategory = g.GroupBy(x => x.Category).ToDictionary(g => g.Key, g => g.Count()),
                TopAlarms = g.GroupBy(x => x.DefinitionId)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .Select(g => new TopAlarm
                    {
                        DefinitionId = g.Key,
                        AlarmName = g.First().AlarmName,
                        Count = g.Count(),
                        Priority = g.First().Priority
                    }).ToList()
            }).ToList();

// 计算趋势
            var totalCount = alarms.Count;
            var ackRate = totalCount > 0 ? (double)history.Items.Count(x => x.FinalState == AlarmState.Acknowledged) / totalCount : 0;
            var avgDuration = history.Items.Where(x => x.ReturnTime.HasValue)
                .Select(x => (x.ReturnTime!.Value - x.OccurTime).Ticks)
                .DefaultIfEmpty(0L)
                .Average();
            var avgDurationTs = TimeSpan.FromTicks((long)avgDuration);

            return new AlarmTrendStatistics
            {
                From = from,
                To = to,
                IntervalMinutes = intervalMinutes,
                TotalAlarms = totalCount,
                AcknowledgmentRate = ackRate,
                AverageDuration = avgDurationTs,
                TimeSeries = timeSeries,
                TopAlarms = history.Items
                    .GroupBy(x => x.DefinitionId)
                    .OrderByDescending(g => g.Count())
                    .Take(20)
                    .Select(g => 
                    {
                        var avgTicks = g.Where(x => x.ReturnTime.HasValue)
                            .Select(x => (x.ReturnTime!.Value - x.OccurTime).Ticks)
                            .DefaultIfEmpty(0L)
                            .Average();
                        return new TopAlarm
                        {
                            DefinitionId = g.Key,
                            AlarmName = g.First().AlarmName,
                            TagName = g.First().TagName,
                            Count = g.Count(),
                            Priority = g.First().Priority,
                            AvgDuration = TimeSpan.FromTicks((long)avgTicks)
                        };
                    }).ToList(),
                PriorityDistribution = history.Items
                        .GroupBy(x => x.Priority)
                        .ToDictionary(g => g.Key, g => g.Count()),
                TypeDistribution = history.Items
                        .GroupBy(x => x.AlarmType)
                        .ToDictionary(g => g.Key, g => g.Count()),
                CategoryDistribution = history.Items
                        .GroupBy(x => x.Category)
                        .ToDictionary(g => g.Key, g => g.Count())
            };
        }

        /// <summary>
        /// 获取报警关联规则 (Apriori 简化版)
        /// </summary>
        public async Task<List<AlarmAssociationRule>> GetAlarmAssociationRulesAsync(DateTime from, DateTime to, double minSupport = 0.01, double minConfidence = 0.5)
        {
            var correlations = await GetAlarmCorrelationsAsync(from, to, 3);
            
            var rules = new List<AlarmAssociationRule>();
            foreach (var corr in correlations)
            {
                if (corr.Confidence >= minConfidence && corr.Support >= minSupport)
                {
                    rules.Add(new AlarmAssociationRule
                    {
                        Antecedent = $"{corr.AlarmName1} ({corr.TagName1})",
                        Consequent = $"{corr.AlarmName2} ({corr.TagName2})",
                        Support = corr.Support,
                        Confidence = corr.Confidence,
                        Lift = corr.Lift,
                        Conviction = corr.Confidence > 0 ? (1 - corr.Support) / (1 - corr.Confidence) : 0
                    });
                    
                    // 反向规则
                    if (corr.Confidence >= minConfidence)
                    {
                        rules.Add(new AlarmAssociationRule
                        {
                            Antecedent = $"{corr.AlarmName2} ({corr.TagName2})",
                            Consequent = $"{corr.AlarmName1} ({corr.TagName1})",
                            Support = corr.Support,
                            Confidence = corr.Confidence,
                            Lift = corr.Lift,
                            Conviction = corr.Confidence > 0 ? (1 - corr.Support) / (1 - corr.Confidence) : 0
                        });
                    }
            }
                }

            return rules.OrderByDescending(r => r.Lift).ThenByDescending(r => r.Confidence).ToList();
        }

        #endregion

        #region 报警导出功能

        /// <summary>
        /// 导出报警历史到 Excel
        /// </summary>
        public async Task<byte[]> ExportAlarmsToExcelAsync(DateTime from, DateTime to, AlarmHistoryFilter? filter = null)
        {
            var (items, _) = await _historyRepository.GetPagedAsync(1, int.MaxValue, filter ?? new AlarmHistoryFilter { OccurFrom = from, OccurTo = to });
            var alarms = items.ToList();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("报警历史");

            var headers = new[] 
            { 
                "ID", "报警名称", "标签名", "报警类型", "优先级", "分类", 
                "发生值", "设定值", "发生时间", "确认时间", "恢复时间", 
                "最终状态", "确认人", "确认备注", "持续时长(秒)", "发生次数", "设备名称"
            };
            
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            var alarmList = items.ToList();
            for (int i = 0; i < alarmList.Count; i++)
            {
                var alarm = alarmList[i];
                var row = i + 2;
                worksheet.Cell(row, 1).Value = alarm.Id;
                worksheet.Cell(row, 2).Value = alarm.AlarmName;
                worksheet.Cell(row, 3).Value = alarm.TagName;
                worksheet.Cell(row, 4).Value = alarm.AlarmType.ToString();
                worksheet.Cell(row, 5).Value = alarm.Priority.ToString();
                worksheet.Cell(row, 6).Value = alarm.Category.ToString();
                worksheet.Cell(row, 7).Value = alarm.OccurValue;
                worksheet.Cell(row, 8).Value = alarm.Setpoint;
                worksheet.Cell(row, 9).Value = alarm.OccurTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
                worksheet.Cell(row, 10).Value = alarm.AckTime?.ToString("yyyy-MM-dd HH:mm:ss.fff") ?? "";
                worksheet.Cell(row, 11).Value = alarm.ReturnTime?.ToString("yyyy-MM-dd HH:mm:ss.fff") ?? "";
                worksheet.Cell(row, 12).Value = alarm.FinalState.ToString();
                worksheet.Cell(row, 13).Value = alarm.AckedBy;
                worksheet.Cell(row, 14).Value = alarm.Comment;
                worksheet.Cell(row, 15).Value = alarm.TotalActiveTime.TotalSeconds;
                worksheet.Cell(row, 16).Value = alarm.OccurrenceCount;
                worksheet.Cell(row, 17).Value = alarm.EquipmentName;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        /// <summary>
        /// 导出报警历史到 CSV
        /// </summary>
        public async Task<string> ExportAlarmsToCsvAsync(DateTime from, DateTime to, AlarmHistoryFilter? filter = null)
        {
            var (items, _) = await _historyRepository.GetPagedAsync(1, int.MaxValue, filter ?? new AlarmHistoryFilter { OccurFrom = from, OccurTo = to });
            var alarmList = items.ToList();

            var sb = new StringBuilder();
            sb.AppendLine("ID,报警名称,标签名,报警类型,优先级,分类,发生值,设定值,发生时间,确认时间,恢复时间,最终状态,确认人,确认备注,持续时长(秒),发生次数,设备名称");

            foreach (var alarm in alarmList)
            {
                sb.AppendLine($"{alarm.Id},{alarm.AlarmName},{alarm.TagName},{alarm.AlarmType},{alarm.Priority},{alarm.Category},{alarm.OccurValue},{alarm.Setpoint},{alarm.OccurTime:yyyy-MM-dd HH:mm:ss.fff},{alarm.AckTime?.ToString("yyyy-MM-dd HH:mm:ss.fff") ?? ""},{alarm.ReturnTime?.ToString("yyyy-MM-dd HH:mm:ss.fff") ?? ""},{alarm.FinalState},{alarm.AckedBy},{alarm.Comment},{alarm.TotalActiveTime.TotalSeconds},{alarm.OccurrenceCount},{alarm.EquipmentName}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// 导出报警定义到 Excel
        /// </summary>
        public async Task<byte[]> ExportDefinitionsToExcelAsync(AlarmDefinitionFilter? filter = null)
        {
            var items = await _definitionRepository.GetAllAsync(filter ?? new AlarmDefinitionFilter());
            var defList = items.ToList();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("报警定义");

            var headers = new[] { "ID", "标签名", "报警名称", "类型", "优先级", "分类", "设定值", "死区", "延迟(秒)", "优先级", "消息", "帮助文本", "启用", "需要确认", "记录历史", "打印报警", "PLC标签地址", "设备ID", "分组ID", "创建时间", "更新时间" };
            
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGreen;
            }

            for (int i = 0; i < defList.Count; i++)
            {
                var d = defList[i];
                var row = i + 2;
                worksheet.Cell(row, 1).Value = d.Id;
                worksheet.Cell(row, 2).Value = d.TagName;
                worksheet.Cell(row, 3).Value = d.AlarmName;
                worksheet.Cell(row, 4).Value = d.AlarmType.ToString();
                worksheet.Cell(row, 5).Value = d.Priority.ToString();
                worksheet.Cell(row, 6).Value = d.Category.ToString();
                worksheet.Cell(row, 7).Value = d.Setpoint;
                worksheet.Cell(row, 8).Value = d.Deadband;
                worksheet.Cell(row, 9).Value = d.DelaySeconds;
                worksheet.Cell(row, 10).Value = d.Priority.ToString();
                worksheet.Cell(row, 11).Value = d.Message;
                worksheet.Cell(row, 12).Value = d.HelpText;
                worksheet.Cell(row, 13).Value = d.IsEnabled ? "是" : "否";
                worksheet.Cell(row, 14).Value = d.RequireAck ? "是" : "否";
                worksheet.Cell(row, 15).Value = d.LogToHistory ? "是" : "否";
                worksheet.Cell(row, 16).Value = d.PrintAlarm ? "是" : "否";
                worksheet.Cell(row, 17).Value = d.PlcTagAddress;
                worksheet.Cell(row, 18).Value = d.EquipmentId;
                worksheet.Cell(row, 19).Value = d.GroupId;
                worksheet.Cell(row, 20).Value = d.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cell(row, 21).Value = d.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss");
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        #endregion

        #region 通知集成 (语音/邮件/短信/Webhook)

        private SmtpClient? _smtpClient;
        private SmsGatewayConfig? _smsConfig;
        private WebhookConfig? _webhookConfig;

        /// <summary>
        /// 配置邮件服务
        /// </summary>
        public void ConfigureEmail(SmtpConfig config)
        {
            _smtpClient = new SmtpClient(config.Host, config.Port)
            {
                Credentials = new NetworkCredential(config.UserName, config.Password),
                EnableSsl = config.EnableSsl
            };
        }

        /// <summary>
        /// 配置短信网关
        /// </summary>
        public void ConfigureSms(SmsGatewayConfig config)
        {
            _smsConfig = config;
        }

        /// <summary>
        /// 配置 Webhook
        /// </summary>
        public void ConfigureWebhook(WebhookConfig config)
        {
            _webhookConfig = config;
        }

        private async Task SendNotificationAsync(AlarmNotification notification, AlarmInstance instance, AlarmDefinition def)
        {
            try
            {
                var message = $"[{def.Priority}] {def.AlarmName} ({def.TagName}) = {instance.CurrentValue}";
                var subject = $"[报警] {def.AlarmName} - {def.Priority}";

                foreach (var recipient in notification.RecipientList)
                {
                    switch (notification.Method)
                    {
                        case NotificationMethod.UI:
                            break;
                        case NotificationMethod.Sound:
                            System.Media.SystemSounds.Hand.Play();
                            break;
                        case NotificationMethod.Email:
                            await SendEmailAsync(recipient, subject, message);
                            break;
                        case NotificationMethod.SMS:
                            await SendSmsAsync(recipient, message);
                            break;
                        case NotificationMethod.Webhook:
                            await SendWebhookAsync(notification.WebhookUrl, instance, def);
                            break;
                        case NotificationMethod.Print:
                            // TODO: 打印实现
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "发送报警通知失败: {NotificationName}", notification.Name);
            }
        }

        private async Task SendEmailAsync(string to, string subject, string body)
        {
            if (_smtpClient == null)
            {
                _logger.Warning("未配置邮件服务，无法发送邮件通知");
                return;
            }

            try
            {
                var credentials = _smtpClient.Credentials as NetworkCredential;
                var fromAddress = credentials?.UserName ?? "noreply@localhost";
                
                var mail = new MailMessage
                {
                    From = new MailAddress(fromAddress),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mail.To.Add(to);

                await _smtpClient.SendMailAsync(mail);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "发送邮件失败: {To}", to);
            }
        }

        private async Task SendSmsAsync(string to, string message)
        {
            if (_smsConfig == null)
            {
                _logger.Warning("未配置短信网关，无法发送短信通知");
                return;
            }

            try
            {
                // 这里需要根据具体的短信网关 API 实现
                // 示例：HTTP POST 到短信网关
                using var httpClient = new HttpClient();
                var payload = new
                {
                    phone = to,
                    message = message,
                    sign = _smsConfig.SignName,
                    template = _smsConfig.TemplateCode
                };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await httpClient.PostAsync(_smsConfig.ApiUrl, content);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.Error("发送短信失败: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "发送短信异常: {To}", to);
            }
        }

        private async Task SendWebhookAsync(string webhookUrl, AlarmInstance instance, AlarmDefinition def)
        {
            if (string.IsNullOrEmpty(webhookUrl)) return;

            try
            {
                using var httpClient = new HttpClient();
                var payload = new
                {
                    alarmId = instance.Id,
                    definitionId = instance.DefinitionId,
                    alarmName = instance.AlarmName,
                    tagName = instance.TagName,
                    currentValue = instance.CurrentValue,
                    setpoint = instance.Setpoint,
                    priority = instance.Priority?.ToString(),
                    alarmType = instance.AlarmType?.ToString(),
                    state = instance.State.ToString(),
                    occurTime = instance.OccurTime,
                    ackTime = instance.AckTime,
                    ackedBy = instance.AckedBy,
                    comment = instance.Comment,
                    equipmentName = instance.EquipmentName
                };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await httpClient.PostAsync(webhookUrl, content);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.Error("Webhook 调用失败: {StatusCode} {Url}", response.StatusCode, webhookUrl);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Webhook 调用异常: {Url}", webhookUrl);
            }
        }

        /// <summary>
        /// 语音报警 (通过 TTS 或外部语音网关)
        /// </summary>
        public async Task PlayVoiceAlarmAsync(AlarmInstance instance, AlarmDefinition def)
        {
            try
            {
                var message = $"报警: {def.AlarmName}, 当前值: {instance.CurrentValue}, 设定值: {def.Setpoint}";
                
                // 方式1: 本地 TTS (Windows)
                try
                {
                    var synth = new System.Speech.Synthesis.SpeechSynthesizer();
                    synth.Speak(message);
                }
                catch
                {
                    // 忽略本地 TTS 失败
                }

                // 方式2: 调用外部语音网关 API
                if (_voiceGatewayConfig != null)
                {
                    using var httpClient = new HttpClient();
                    var payload = new { text = message, voice = _voiceGatewayConfig.Voice };
                    var json = JsonSerializer.Serialize(payload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    await httpClient.PostAsync(_voiceGatewayConfig.ApiUrl, new StringContent(json, Encoding.UTF8, "application/json"));
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "语音报警失败");
            }
        }

        private VoiceGatewayConfig? _voiceGatewayConfig;

        public void ConfigureVoiceGateway(VoiceGatewayConfig config)
        {
            _voiceGatewayConfig = config;
        }

        #endregion

        #endregion

        public void Dispose()
        {
            _evaluationTimer?.Dispose();
            _cleanupTimer?.Dispose();
            _smtpClient?.Dispose();
            _isRunning = false;
        }
    }

    public class AlarmEventArgs : EventArgs
    {
        public AlarmInstance AlarmInstance { get; set; } = new();
        public AlarmDefinition Definition { get; set; } = new();
        public AlarmEventType EventType { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public enum AlarmEventType
    {
        Raised,
        Acknowledged,
        Returned,
        Shelved,
        Unshelved
    }

    public class AcknowledgeResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = "";
    }

    public class ShelveResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = "";
    
}
}
