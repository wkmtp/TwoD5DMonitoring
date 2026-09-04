using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using TwoD5DMonitoring.Infrastructure.Database;
using TwoD5DMonitoring.Models;
using TwoD5DMonitoring.Services.Repositories;

namespace TwoD5DMonitoring.Services.Repositories
{
    /// <summary>
    /// 报警定义仓储
    /// </summary>
    public class AlarmDefinitionRepository : IAlarmDefinitionRepository
    {
        private readonly IDatabaseProvider _db;

        public AlarmDefinitionRepository(IDatabaseProvider db)
        {
            _db = db;
        }

        public async Task<AlarmDefinition?> GetByIdAsync(int id)
        {
            const string sql = @"
                SELECT d.*, g.Name as GroupName
                FROM AlarmDefinitions d
                LEFT JOIN AlarmGroups g ON d.GroupId = g.Id
                WHERE d.Id = @Id";
            return await _db.QuerySingleAsync<AlarmDefinition>(sql, new { Id = id });
        }

        public async Task<IEnumerable<AlarmDefinition>> GetAllAsync(AlarmDefinitionFilter filter)
        {
            var where = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                where.Add("(TagName LIKE @Keyword OR AlarmName LIKE @Keyword)");
                parameters.Add("@Keyword", $"%{filter.Keyword}%");
            }

            if (filter.AlarmType.HasValue)
            {
                where.Add("AlarmType = @AlarmType");
                parameters.Add("@AlarmType", (int)filter.AlarmType.Value);
            }

            if (filter.Priority.HasValue)
            {
                where.Add("Priority = @Priority");
                parameters.Add("@Priority", (int)filter.Priority.Value);
            }

            if (filter.Category.HasValue)
            {
                where.Add("Category = @Category");
                parameters.Add("@Category", (int)filter.Category.Value);
            }

            if (filter.IsEnabled.HasValue)
            {
                where.Add("IsEnabled = @IsEnabled");
                parameters.Add("@IsEnabled", filter.IsEnabled.Value);
            }

            if (filter.EquipmentId.HasValue)
            {
                where.Add("EquipmentId = @EquipmentId");
                parameters.Add("@EquipmentId", filter.EquipmentId.Value);
            }

            var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";
            var orderBy = !string.IsNullOrWhiteSpace(filter.SortBy) ? filter.SortBy : "AlarmName";
            var orderDir = filter.SortDescending ? "DESC" : "ASC";

            var sql = $@"
                SELECT d.*, g.Name as GroupName
                FROM AlarmDefinitions d
                LEFT JOIN AlarmGroups g ON d.GroupId = g.Id
                {whereClause}
                ORDER BY {orderBy} {orderDir}";

            if (filter.PageSize > 0)
            {
                sql += $@"
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
                parameters.Add("@Offset", (filter.PageIndex - 1) * filter.PageSize);
                parameters.Add("@PageSize", filter.PageSize);
            }

            return await _db.QueryAsync<AlarmDefinition>(sql, parameters);
        }

        public async Task<int> GetCountAsync(AlarmDefinitionFilter filter)
        {
            var where = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                where.Add("(TagName LIKE @Keyword OR AlarmName LIKE @Keyword)");
                parameters.Add("@Keyword", $"%{filter.Keyword}%");
            }

            if (filter.AlarmType.HasValue)
            {
                where.Add("AlarmType = @AlarmType");
                parameters.Add("@AlarmType", (int)filter.AlarmType.Value);
            }

            if (filter.Priority.HasValue)
            {
                where.Add("Priority = @Priority");
                parameters.Add("@Priority", (int)filter.Priority.Value);
            }

            if (filter.Category.HasValue)
            {
                where.Add("Category = @Category");
                parameters.Add("@Category", (int)filter.Category.Value);
            }

            if (filter.IsEnabled.HasValue)
            {
                where.Add("IsEnabled = @IsEnabled");
                parameters.Add("@IsEnabled", filter.IsEnabled.Value);
            }

            if (filter.EquipmentId.HasValue)
            {
                where.Add("EquipmentId = @EquipmentId");
                parameters.Add("@EquipmentId", filter.EquipmentId.Value);
            }

            var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";
            var sql = $"SELECT COUNT(*) FROM AlarmDefinitions {whereClause}";
            return await _db.ExecuteScalarAsync<int>(sql, parameters);
        }

        public async Task<AlarmDefinition> AddAsync(AlarmDefinition definition)
        {
            const string sql = @"
                INSERT INTO AlarmDefinitions (TagName, AlarmName, AlarmType, Priority, Category, Setpoint, Deadband,
                    DelaySeconds, Message, HelpText, IsEnabled, RequireAck, LogToHistory, PrintAlarm,
                    PlcTagAddress, EquipmentId, GroupId, CreatedAt, UpdatedAt)
                VALUES (@TagName, @AlarmName, @AlarmType, @Priority, @Category, @Setpoint, @Deadband,
                    @DelaySeconds, @Message, @HelpText, @IsEnabled, @RequireAck, @LogToHistory, @PrintAlarm,
                    @PlcTagAddress, @EquipmentId, @GroupId, @CreatedAt, @UpdatedAt);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            var id = await _db.ExecuteScalarAsync<int>(sql, new
            {
                definition.TagName,
                definition.AlarmName,
                definition.AlarmType,
                definition.Priority,
                definition.Category,
                definition.Setpoint,
                definition.Deadband,
                definition.DelaySeconds,
                definition.Message,
                definition.HelpText,
                definition.IsEnabled,
                definition.RequireAck,
                definition.LogToHistory,
                definition.PrintAlarm,
                definition.PlcTagAddress,
                definition.EquipmentId,
                definition.GroupId,
                definition.CreatedAt,
                definition.UpdatedAt
            });

            definition.Id = id;
            return definition;
        }

        public async Task<AlarmDefinition> UpdateAsync(AlarmDefinition definition)
        {
            const string sql = @"
                UPDATE AlarmDefinitions 
                SET TagName = @TagName, AlarmName = @AlarmName, AlarmType = @AlarmType, Priority = @Priority,
                    Category = @Category, Setpoint = @Setpoint, Deadband = @Deadband, DelaySeconds = @DelaySeconds,
                    Message = @Message, HelpText = @HelpText, IsEnabled = @IsEnabled, RequireAck = @RequireAck,
                    LogToHistory = @LogToHistory, PrintAlarm = @PrintAlarm, PlcTagAddress = @PlcTagAddress,
                    EquipmentId = @EquipmentId, GroupId = @GroupId, UpdatedAt = @UpdatedAt
                WHERE Id = @Id";

            await _db.ExecuteAsync(sql, new
            {
                definition.Id,
                definition.TagName,
                definition.AlarmName,
                definition.AlarmType,
                definition.Priority,
                definition.Category,
                definition.Setpoint,
                definition.Deadband,
                definition.DelaySeconds,
                definition.Message,
                definition.HelpText,
                definition.IsEnabled,
                definition.RequireAck,
                definition.LogToHistory,
                definition.PrintAlarm,
                definition.PlcTagAddress,
                definition.EquipmentId,
                definition.GroupId,
                UpdatedAt = DateTime.Now
            });

            return definition;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "DELETE FROM AlarmDefinitions WHERE Id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            return affected > 0;
        }

        public async Task<IEnumerable<AlarmGroup>> GetGroupsAsync()
        {
            const string sql = "SELECT * FROM AlarmGroups WHERE IsEnabled = 1 ORDER BY Name";
            return await _db.QueryAsync<AlarmGroup>(sql);
        }

        public async Task<AlarmGroup> AddGroupAsync(AlarmGroup group)
        {
            const string sql = @"
                INSERT INTO AlarmGroups (Name, Description, IsEnabled, CreatedAt)
                VALUES (@Name, @Description, @IsEnabled, @CreatedAt);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            var id = await _db.ExecuteScalarAsync<int>(sql, new
            {
                group.Name,
                group.Description,
                group.IsEnabled,
                group.CreatedAt
            });

            group.Id = id;
            return group;
        }
    }

    /// <summary>
    /// 报警实例仓储
    /// </summary>
    public class AlarmInstanceRepository : IAlarmInstanceRepository
    {
        private readonly IDatabaseProvider _db;

        public AlarmInstanceRepository(IDatabaseProvider db)
        {
            _db = db;
        }

        public async Task<AlarmInstance?> GetByIdAsync(int id)
        {
            const string sql = @"
                SELECT i.*, d.TagName, d.AlarmName, d.Priority, d.AlarmType, d.Category,
                    e.Name as EquipmentName
                FROM AlarmInstances i
                LEFT JOIN AlarmDefinitions d ON i.DefinitionId = d.Id
                LEFT JOIN EquipmentBase e ON d.EquipmentId = e.Id
                WHERE i.Id = @Id";
            return await _db.QuerySingleAsync<AlarmInstance>(sql, new { Id = id });
        }

        public async Task<IEnumerable<AlarmInstance>> GetActiveAlarmsAsync()
        {
            const string sql = @"
                SELECT i.*, d.TagName, d.AlarmName, d.Priority, d.AlarmType, d.Category,
                    e.Name as EquipmentName
                FROM AlarmInstances i
                LEFT JOIN AlarmDefinitions d ON i.DefinitionId = d.Id
                LEFT JOIN EquipmentBase e ON d.EquipmentId = e.Id
                WHERE i.State IN (2, 3) AND i.IsShelved = 0
                ORDER BY d.Priority DESC, i.OccurTime ASC";
            return await _db.QueryAsync<AlarmInstance>(sql);
        }

        public async Task<(IEnumerable<AlarmInstance> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, AlarmFilter filter)
        {
            var where = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                where.Add("(d.TagName LIKE @Keyword OR d.AlarmName LIKE @Keyword OR d.Message LIKE @Keyword)");
                parameters.Add("@Keyword", $"%{filter.Keyword}%");
            }

            if (filter.State.HasValue)
            {
                where.Add("i.State = @State");
                parameters.Add("@State", (int)filter.State.Value);
            }

            if (filter.Priority.HasValue)
            {
                where.Add("d.Priority = @Priority");
                parameters.Add("@Priority", (int)filter.Priority.Value);
            }

            if (filter.AlarmType.HasValue)
            {
                where.Add("d.AlarmType = @AlarmType");
                parameters.Add("@AlarmType", (int)filter.AlarmType.Value);
            }

            if (filter.Category.HasValue)
            {
                where.Add("d.Category = @Category");
                parameters.Add("@Category", (int)filter.Category.Value);
            }

            if (filter.EquipmentId.HasValue)
            {
                where.Add("d.EquipmentId = @EquipmentId");
                parameters.Add("@EquipmentId", filter.EquipmentId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.TagName))
            {
                where.Add("d.TagName = @TagName");
                parameters.Add("@TagName", filter.TagName);
            }

            if (filter.OccurFrom.HasValue)
            {
                where.Add("i.OccurTime >= @OccurFrom");
                parameters.Add("@OccurFrom", filter.OccurFrom.Value);
            }

            if (filter.OccurTo.HasValue)
            {
                where.Add("i.OccurTime <= @OccurTo");
                parameters.Add("@OccurTo", filter.OccurTo.Value);
            }

            if (filter.AckFrom.HasValue)
            {
                where.Add("i.AckTime >= @AckFrom");
                parameters.Add("@AckFrom", filter.AckFrom.Value);
            }

            if (filter.AckTo.HasValue)
            {
                where.Add("i.AckTime <= @AckTo");
                parameters.Add("@AckTo", filter.AckTo.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.AckedBy))
            {
                where.Add("i.AckedBy = @AckedBy");
                parameters.Add("@AckedBy", filter.AckedBy);
            }

            if (filter.IsShelved.HasValue)
            {
                where.Add("i.IsShelved = @IsShelved");
                parameters.Add("@IsShelved", filter.IsShelved.Value);
            }

            var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";
            var orderBy = !string.IsNullOrWhiteSpace(filter.SortBy) ? filter.SortBy : "i.OccurTime";
            var orderDir = filter.SortDescending ? "DESC" : "ASC";

            var sql = $@"
                SELECT i.*, d.TagName, d.AlarmName, d.Priority, d.AlarmType, d.Category,
                    e.Name as EquipmentName
                FROM AlarmInstances i
                LEFT JOIN AlarmDefinitions d ON i.DefinitionId = d.Id
                LEFT JOIN EquipmentBase e ON d.EquipmentId = e.Id
                {whereClause}
                ORDER BY {orderBy} {orderDir}
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            parameters.Add("@Offset", (filter.PageIndex - 1) * filter.PageSize);
            parameters.Add("@PageSize", filter.PageSize);

            var items = await _db.QueryAsync<AlarmInstance>(sql, parameters);

            // 获取总数
            var countSql = $@"
                SELECT COUNT(*)
                FROM AlarmInstances i
                LEFT JOIN AlarmDefinitions d ON i.DefinitionId = d.Id
                {whereClause}";
            var total = await _db.ExecuteScalarAsync<int>(countSql, parameters);

            return (items, total);
        }

        public async Task<int> GetCountAsync(AlarmFilter filter)
        {
            var where = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                where.Add("(d.TagName LIKE @Keyword OR d.AlarmName LIKE @Keyword)");
                parameters.Add("@Keyword", $"%{filter.Keyword}%");
            }

            if (filter.State.HasValue)
            {
                where.Add("i.State = @State");
                parameters.Add("@State", (int)filter.State.Value);
            }

            if (filter.Priority.HasValue)
            {
                where.Add("d.Priority = @Priority");
                parameters.Add("@Priority", (int)filter.Priority.Value);
            }

            if (filter.AlarmType.HasValue)
            {
                where.Add("d.AlarmType = @AlarmType");
                parameters.Add("@AlarmType", (int)filter.AlarmType.Value);
            }

            if (filter.Category.HasValue)
            {
                where.Add("d.Category = @Category");
                parameters.Add("@Category", (int)filter.Category.Value);
            }

            if (filter.EquipmentId.HasValue)
            {
                where.Add("d.EquipmentId = @EquipmentId");
                parameters.Add("@EquipmentId", filter.EquipmentId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.TagName))
            {
                where.Add("d.TagName = @TagName");
                parameters.Add("@TagName", filter.TagName);
            }

            if (filter.OccurFrom.HasValue)
            {
                where.Add("i.OccurTime >= @OccurFrom");
                parameters.Add("@OccurFrom", filter.OccurFrom.Value);
            }

            if (filter.OccurTo.HasValue)
            {
                where.Add("i.OccurTime <= @OccurTo");
                parameters.Add("@OccurTo", filter.OccurTo.Value);
            }

            var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";
            var sql = $"SELECT COUNT(*) FROM AlarmInstances i LEFT JOIN AlarmDefinitions d ON i.DefinitionId = d.Id {whereClause}";
            return await _db.ExecuteScalarAsync<int>(sql, parameters);
        }

        public async Task<AlarmInstance> AddAsync(AlarmInstance instance)
        {
            const string sql = @"
                INSERT INTO AlarmInstances (DefinitionId, State, CurrentValue, Setpoint, OccurTime, 
                    AckTime, ReturnTime, AckedBy, Comment, OccurrenceCount, IsShelved)
                VALUES (@DefinitionId, @State, @CurrentValue, @Setpoint, @OccurTime,
                    @AckTime, @ReturnTime, @AckedBy, @Comment, @OccurrenceCount, @IsShelved);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            var id = await _db.ExecuteScalarAsync<int>(sql, new
            {
                instance.DefinitionId,
                instance.State,
                instance.CurrentValue,
                instance.Setpoint,
                instance.OccurTime,
                instance.AckTime,
                instance.ReturnTime,
                instance.AckedBy,
                instance.Comment,
                instance.OccurrenceCount,
                instance.IsShelved
            });

            instance.Id = id;
            return instance;
        }

        public async Task<AlarmInstance> UpdateAsync(AlarmInstance instance)
        {
            const string sql = @"
                UPDATE AlarmInstances 
                SET State = @State, CurrentValue = @CurrentValue, Setpoint = @Setpoint,
                    AckTime = @AckTime, ReturnTime = @ReturnTime, AckedBy = @AckedBy,
                    Comment = @Comment, OccurrenceCount = @OccurrenceCount, IsShelved = @IsShelved
                WHERE Id = @Id";

            await _db.ExecuteAsync(sql, new
            {
                instance.Id,
                instance.State,
                instance.CurrentValue,
                instance.Setpoint,
                instance.AckTime,
                instance.ReturnTime,
                instance.AckedBy,
                instance.Comment,
                instance.OccurrenceCount,
                instance.IsShelved
            });

            return instance;
        }

        public async Task<bool> AcknowledgeAsync(int id, string ackedBy, string comment)
        {
            const string sql = @"
                UPDATE AlarmInstances 
                SET State = 3, AckTime = @AckTime, AckedBy = @AckedBy, Comment = @Comment
                WHERE Id = @Id AND State IN (2, 3)";

            var affected = await _db.ExecuteAsync(sql, new
            {
                Id = id,
                AckTime = DateTime.Now,
                AckedBy = ackedBy,
                Comment = comment
            });

            return affected > 0;
        }

        public async Task<bool> ShelveAsync(int id, bool shelve)
        {
            const string sql = "UPDATE AlarmInstances SET IsShelved = @IsShelved WHERE Id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id, IsShelved = shelve });
            return affected > 0;
        }

        public async Task<bool> ReturnToNormalAsync(int id, double returnValue)
        {
            const string sql = @"
                UPDATE AlarmInstances 
                SET State = 1, ReturnTime = @ReturnTime, CurrentValue = @ReturnValue
                WHERE Id = @Id AND State IN (2, 3)";

            var affected = await _db.ExecuteAsync(sql, new
            {
                Id = id,
                ReturnTime = DateTime.Now,
                ReturnValue = returnValue
            });

            return affected > 0;
        }
    }

    /// <summary>
    /// 历史报警仓储
    /// </summary>
    public class AlarmHistoryRepository : IAlarmHistoryRepository
    {
        private readonly IDatabaseProvider _db;

        public AlarmHistoryRepository(IDatabaseProvider db)
        {
            _db = db;
        }

        public async Task<(IEnumerable<AlarmHistory> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, AlarmHistoryFilter filter)
        {
            var where = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                where.Add("(TagName LIKE @Keyword OR AlarmName LIKE @Keyword)");
                parameters.Add("@Keyword", $"%{filter.Keyword}%");
            }

            if (filter.Priority.HasValue)
            {
                where.Add("Priority = @Priority");
                parameters.Add("@Priority", (int)filter.Priority.Value);
            }

            if (filter.AlarmType.HasValue)
            {
                where.Add("AlarmType = @AlarmType");
                parameters.Add("@AlarmType", (int)filter.AlarmType.Value);
            }

            if (filter.EquipmentId.HasValue)
            {
                where.Add("EquipmentId = @EquipmentId");
                parameters.Add("@EquipmentId", filter.EquipmentId.Value);
            }

            if (filter.OccurFrom.HasValue)
            {
                where.Add("OccurTime >= @OccurFrom");
                parameters.Add("@OccurFrom", filter.OccurFrom.Value);
            }

            if (filter.OccurTo.HasValue)
            {
                where.Add("OccurTime <= @OccurTo");
                parameters.Add("@OccurTo", filter.OccurTo.Value);
            }

            var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";
            var sql = $@"
                SELECT * FROM AlarmHistory
                {whereClause}
                ORDER BY OccurTime DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            parameters.Add("@Offset", (filter.PageIndex - 1) * filter.PageSize);
            parameters.Add("@PageSize", filter.PageSize);

            var items = await _db.QueryAsync<AlarmHistory>(sql, parameters);

            var countSql = $"SELECT COUNT(*) FROM AlarmHistory {whereClause}";
            var total = await _db.ExecuteScalarAsync<int>(countSql, parameters);

            return (items, total);
        }

        public async Task<int> GetCountAsync(AlarmHistoryFilter filter)
        {
            var where = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                where.Add("(TagName LIKE @Keyword OR AlarmName LIKE @Keyword)");
                parameters.Add("@Keyword", $"%{filter.Keyword}%");
            }

            if (filter.Priority.HasValue)
            {
                where.Add("Priority = @Priority");
                parameters.Add("@Priority", (int)filter.Priority.Value);
            }

            if (filter.AlarmType.HasValue)
            {
                where.Add("AlarmType = @AlarmType");
                parameters.Add("@AlarmType", (int)filter.AlarmType.Value);
            }

            if (filter.EquipmentId.HasValue)
            {
                where.Add("EquipmentId = @EquipmentId");
                parameters.Add("@EquipmentId", filter.EquipmentId.Value);
            }

            if (filter.OccurFrom.HasValue)
            {
                where.Add("OccurTime >= @OccurFrom");
                parameters.Add("@OccurFrom", filter.OccurFrom.Value);
            }

            if (filter.OccurTo.HasValue)
            {
                where.Add("OccurTime <= @OccurTo");
                parameters.Add("@OccurTo", filter.OccurTo.Value);
            }

            var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";
            var sql = $"SELECT COUNT(*) FROM AlarmHistory {whereClause}";
            return await _db.ExecuteScalarAsync<int>(sql, parameters);
        }

        public async Task<AlarmHistory> AddAsync(AlarmHistory history)
        {
            const string sql = @"
                INSERT INTO AlarmHistory (DefinitionId, FinalState, OccurValue, ReturnValue, OccurTime,
                    AckTime, ReturnTime, AckedBy, Comment, OccurrenceCount, TotalActiveTime,
                    TagName, AlarmName, Priority, AlarmType, EquipmentName)
                VALUES (@DefinitionId, @FinalState, @OccurValue, @ReturnValue, @OccurTime,
                    @AckTime, @ReturnTime, @AckedBy, @Comment, @OccurrenceCount, @TotalActiveTime,
                    @TagName, @AlarmName, @Priority, @AlarmType, @EquipmentName);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            var id = await _db.ExecuteScalarAsync<int>(sql, new
            {
                history.DefinitionId,
                history.FinalState,
                history.OccurValue,
                history.ReturnValue,
                history.OccurTime,
                history.AckTime,
                history.ReturnTime,
                history.AckedBy,
                history.Comment,
                history.OccurrenceCount,
                history.TotalActiveTime.Ticks,
                history.TagName,
                history.AlarmName,
                history.Priority,
                history.AlarmType,
                history.EquipmentName
            });

            history.Id = id;
            return history;
        }

        public async Task<Dictionary<AlarmPriority, int>> GetStatisticsAsync(DateTime from, DateTime to)
        {
            const string sql = @"
                SELECT Priority, COUNT(*) as Count
                FROM AlarmHistory
                WHERE OccurTime >= @From AND OccurTime <= @To
                GROUP BY Priority";

            var results = await _db.QueryAsync<dynamic>(sql, new { From = from, To = to });
            var dict = new Dictionary<AlarmPriority, int>();
            foreach (var r in results)
            {
                dict[(AlarmPriority)(int)r.Priority] = (int)r.Count;
            }
            return dict;
        }
    }

    /// <summary>
    /// 报警通知仓储
    /// </summary>
    public class AlarmNotificationRepository : IAlarmNotificationRepository
    {
        private readonly IDatabaseProvider _db;

        public AlarmNotificationRepository(IDatabaseProvider db)
        {
            _db = db;
        }

        public async Task<IEnumerable<AlarmNotification>> GetEnabledAsync()
        {
            const string sql = "SELECT * FROM AlarmNotifications WHERE IsEnabled = 1";
            return await _db.QueryAsync<AlarmNotification>(sql);
        }

        public async Task<AlarmNotification> AddAsync(AlarmNotification notification)
        {
            const string sql = @"
                INSERT INTO AlarmNotifications (Name, Recipients, Method, MinPriority, IsEnabled, CreatedAt)
                VALUES (@Name, @Recipients, @Method, @MinPriority, @IsEnabled, @CreatedAt);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            var id = await _db.ExecuteScalarAsync<int>(sql, new
            {
                notification.Name,
                notification.Recipients,
                notification.Method,
                notification.MinPriority,
                notification.IsEnabled,
                notification.CreatedAt
            });

            notification.Id = id;
            return notification;
        }
    }

    /// <summary>
    /// 报警抑制仓储
    /// </summary>
    public class AlarmSuppressionRepository : IAlarmSuppressionRepository
    {
        private readonly IDatabaseProvider _db;

        public AlarmSuppressionRepository(IDatabaseProvider db)
        {
            _db = db;
        }

        public async Task<IEnumerable<AlarmSuppression>> GetActiveAsync()
        {
            const string sql = "SELECT * FROM AlarmSuppressions WHERE IsEnabled = 1";
            return await _db.QueryAsync<AlarmSuppression>(sql);
        }

        public async Task<AlarmSuppression> AddAsync(AlarmSuppression suppression)
        {
            const string sql = @"
                INSERT INTO AlarmSuppressions (Name, Type, StartTime, EndTime, DaysOfWeekJson,
                    TagFilter, EquipmentId, IsEnabled, CreatedAt)
                VALUES (@Name, @Type, @StartTime, @EndTime, @DaysOfWeekJson,
                    @TagFilter, @EquipmentId, @IsEnabled, @CreatedAt);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            var id = await _db.ExecuteScalarAsync<int>(sql, new
            {
                suppression.Name,
                suppression.Type,
                suppression.StartTime,
                suppression.EndTime,
                suppression.DaysOfWeekJson,
                suppression.TagFilter,
                suppression.EquipmentId,
                suppression.IsEnabled,
                suppression.CreatedAt
            });

            suppression.Id = id;
            return suppression;
        }

        public async Task<bool> IsSuppressedAsync(int definitionId, DateTime time)
        {
            // 检查时间抑制
            var timeSuppressions = await _db.QueryAsync<AlarmSuppression>(@"
                SELECT * FROM AlarmSuppressions 
                WHERE IsEnabled = 1 AND Type = 1 
                AND @Time BETWEEN StartTime AND EndTime
                AND (DaysOfWeekJson IS NULL OR DaysOfWeekJson = '' OR JSON_CONTAINS(DaysOfWeekJson, @DayOfWeek))",
                new { Time = time.TimeOfDay, DayOfWeek = (int)time.DayOfWeek });

            foreach (var s in timeSuppressions)
            {
                // 这里需要根据 TagFilter 或 EquipmentId 进一步匹配
                // 简化处理：假设匹配所有
                return true;
            }

            return false;
        }
    }
}