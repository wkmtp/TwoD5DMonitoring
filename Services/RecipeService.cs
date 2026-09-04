using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using TwoD5DMonitoring.Infrastructure.Logging;
using TwoD5DMonitoring.Models;
using TwoD5DMonitoring.Services.Repositories;

namespace TwoD5DMonitoring.Services
{
    /// <summary>
    /// 配方管理服务
    /// </summary>
    public class RecipeService
    {
        private readonly ILogger _logger;
        private readonly IRecipeRepository _recipeRepository;
        private readonly IRecipeDownloadLogRepository _downloadLogRepository;
        private readonly IBatchRecordRepository _batchRecordRepository;
        private readonly IMaterialRepository _materialRepository;

        public RecipeService(
            IRecipeRepository recipeRepository,
            IRecipeDownloadLogRepository downloadLogRepository,
            IBatchRecordRepository batchRecordRepository,
            IMaterialRepository materialRepository)
        {
            _logger = LogManager.CreateModuleLogger(nameof(RecipeService));
            _recipeRepository = recipeRepository;
            _downloadLogRepository = downloadLogRepository;
            _batchRecordRepository = batchRecordRepository;
            _materialRepository = materialRepository;
        }

        #region 配方基础操作

        public async Task<RecipeBase?> GetRecipeByIdAsync(int id)
        {
            return await _recipeRepository.GetByIdAsync(id);
        }

        public async Task<RecipeBase?> GetRecipeByCodeAsync(string code)
        {
            return await _recipeRepository.GetByCodeAsync(code);
        }

        public async Task<WeighingRecipe?> GetWeighingRecipeAsync(int id)
        {
            return await _recipeRepository.GetWeighingRecipeAsync(id);
        }

        public async Task<MixingRecipe?> GetMixingRecipeAsync(int id)
        {
            return await _recipeRepository.GetMixingRecipeAsync(id);
        }

        public async Task<(IEnumerable<RecipeBase> Items, int TotalCount)> GetRecipesPagedAsync(
            int page, int pageSize, RecipeFilter filter)
        {
            var items = await _recipeRepository.GetPagedAsync(page, pageSize, filter);
            var total = await _recipeRepository.GetCountAsync(filter);
            return (items, total);
        }

        public async Task<RecipeBase> CreateRecipeAsync(RecipeBase recipe, string operatorName)
        {
            if (await _recipeRepository.ExistsCodeAsync(recipe.Code))
                throw new InvalidOperationException($"配方编码 '{recipe.Code}' 已存在");

            recipe.CreatedAt = DateTime.Now;
            recipe.CreatedBy = operatorName;
            recipe.UpdatedAt = DateTime.Now;
            recipe.UpdatedBy = operatorName;
            recipe.Status = RecipeStatus.Draft;

            var result = await _recipeRepository.AddAsync(recipe);

            _logger.Information("创建配方: {Code} - {Name}, Type: {Type}, Operator: {Operator}",
                recipe.Code, recipe.Name, recipe.RecipeType, operatorName);

            return result;
        }

        public async Task<RecipeBase> UpdateRecipeAsync(RecipeBase recipe, string operatorName)
        {
            if (await _recipeRepository.ExistsCodeAsync(recipe.Code, recipe.Id))
                throw new InvalidOperationException($"配方编码 '{recipe.Code}' 已存在");

            recipe.UpdatedAt = DateTime.Now;
            recipe.UpdatedBy = operatorName;

            var result = await _recipeRepository.UpdateAsync(recipe);

            _logger.Information("更新配方: {Code} - {Name}, Operator: {Operator}",
                recipe.Code, recipe.Name, operatorName);

            return result;
        }

        public async Task<bool> DeleteRecipeAsync(int id, string operatorName)
        {
            var recipe = await _recipeRepository.GetByIdAsync(id);
            if (recipe == null) return false;

            // 检查是否有批次记录引用
            // 这里可以添加检查逻辑

            var result = await _recipeRepository.DeleteAsync(id);

            if (result)
                _logger.Information("删除配方: Id={Id}, Code={Code}, Operator: {Operator}", id, recipe.Code, operatorName);

            return result;
        }

        public async Task<bool> CopyRecipeAsync(int sourceId, string newCode, string newName, string operatorName)
        {
            var source = await _recipeRepository.GetByIdAsync(sourceId);
            if (source == null)
                throw new InvalidOperationException("源配方不存在");

            if (await _recipeRepository.ExistsCodeAsync(newCode))
                throw new InvalidOperationException($"配方编码 '{newCode}' 已存在");

            // 深拷贝
            var newRecipe = CloneRecipe(source);
            newRecipe.Code = newCode;
            newRecipe.Name = newName;
            newRecipe.Status = RecipeStatus.Draft;
            newRecipe.CurrentVersion = "1.0";
            newRecipe.CreatedAt = DateTime.Now;
            newRecipe.CreatedBy = operatorName;
            newRecipe.UpdatedAt = DateTime.Now;
            newRecipe.UpdatedBy = operatorName;
            newRecipe.ApprovedAt = null;
            newRecipe.ApprovedBy = "";
            newRecipe.ReleasedAt = null;
            newRecipe.ReleasedBy = "";

            // 清除扩展数据的ID，让数据库重新生成
            if (newRecipe is WeighingRecipe wr)
            {
                foreach (var line in wr.Lines)
                {
                    line.Id = 0;
                }
            }
            else if (newRecipe is MixingRecipe mr)
            {
                foreach (var step in mr.Steps)
                {
                    step.Id = 0;
                }
            }

            await _recipeRepository.AddAsync(newRecipe);

            _logger.Information("复制配方: 从 {SourceCode} 到 {NewCode}, Operator: {Operator}",
                source.Code, newCode, operatorName);

            return true;
        }

        private RecipeBase CloneRecipe(RecipeBase source)
        {
            var settings = new Newtonsoft.Json.JsonSerializerSettings
            {
                PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.Objects,
                ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto
            };
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(source, settings);
            // 使用自定义转换器仅反序列化顶层 Recipe
            var jobj = Newtonsoft.Json.Linq.JObject.Parse(json);
            var converter = new RecipeBaseJsonConverter();
            return converter.ReadJson(jobj.CreateReader(), typeof(RecipeBase), null, false, Newtonsoft.Json.JsonSerializer.Create(settings))!;
        }

        #endregion

        #region 自定义 JSON 转换器

        /// <summary>
        /// RecipeBase 多态反序列化转换器
        /// </summary>
        public class RecipeBaseJsonConverter : JsonConverter<RecipeBase>
        {
            public override bool CanRead => true;
            public override bool CanWrite => false;

            public override RecipeBase ReadJson(JsonReader reader, Type objectType, RecipeBase existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                var jobj = JObject.Load(reader);
                var typeName = jobj["$type"]?.Value<string>();

                if (string.IsNullOrEmpty(typeName))
                {
                    // 尝试根据属性判断类型
                    if (jobj["WeighingMode"] != null)
                        typeName = typeof(WeighingRecipe).AssemblyQualifiedName;
                    else if (jobj["TotalSteps"] != null)
                        typeName = typeof(MixingRecipe).AssemblyQualifiedName;
                }

                if (string.IsNullOrEmpty(typeName))
                    throw new JsonSerializationException("无法确定配方类型");

                var concreteType = Type.GetType(typeName);
                if (concreteType == null || !typeof(RecipeBase).IsAssignableFrom(concreteType))
                    throw new JsonSerializationException($"未知的配方类型: {typeName}");

                var instance = (RecipeBase)Activator.CreateInstance(concreteType)!;
                serializer.Populate(jobj.CreateReader(), instance);
                return instance;
            }

            public override void WriteJson(JsonWriter writer, RecipeBase value, JsonSerializer serializer)
            {
                // 写入时使用默认序列化（包含 $type）
                JObject.FromObject(value, serializer).WriteTo(writer);
            }
        }

        #endregion

        #region 版本管理

        public async Task<RecipeVersion> CreateVersionAsync(int recipeId, string version, string changeDescription, string operatorName)
        {
            var recipe = await _recipeRepository.GetByIdAsync(recipeId);
            if (recipe == null)
                throw new InvalidOperationException("配方不存在");

            var recipeVersion = new RecipeVersion
            {
                RecipeId = recipeId,
                Version = version,
                ChangeDescription = changeDescription,
                IsCurrent = true,
                CreatedAt = DateTime.Now,
                CreatedBy = operatorName
            };

            var result = await _recipeRepository.AddVersionAsync(recipeVersion);

            // 更新配方当前版本
            recipe.CurrentVersion = version;
            recipe.CurrentVersionId = result.Id;
            recipe.UpdatedAt = DateTime.Now;
            recipe.UpdatedBy = operatorName;
            await _recipeRepository.UpdateAsync(recipe);

            _logger.Information("创建配方版本: RecipeId={RecipeId}, Version={Version}, Operator: {Operator}",
                recipeId, version, operatorName);

            return result;
        }

        public async Task<bool> SetCurrentVersionAsync(int recipeId, int versionId, string operatorName)
        {
            var result = await _recipeRepository.SetCurrentVersionAsync(recipeId, versionId);

            if (result)
            {
                var version = (await _recipeRepository.GetVersionsAsync(recipeId)).FirstOrDefault(v => v.Id == versionId);
                if (version != null)
                {
                    var recipe = await _recipeRepository.GetByIdAsync(recipeId);
                    if (recipe != null)
                    {
                        recipe.CurrentVersion = version.Version;
                        recipe.CurrentVersionId = versionId;
                        recipe.UpdatedAt = DateTime.Now;
                        recipe.UpdatedBy = operatorName;
                        await _recipeRepository.UpdateAsync(recipe);
                    }
                }

                _logger.Information("设置当前版本: RecipeId={RecipeId}, VersionId={VersionId}, Operator: {Operator}",
                    recipeId, versionId, operatorName);
            }

            return result;
        }

        public async Task<IEnumerable<RecipeVersion>> GetVersionsAsync(int recipeId)
        {
            return await _recipeRepository.GetVersionsAsync(recipeId);
        }

        #endregion

        #region 状态流转

        public async Task<bool> SubmitForReviewAsync(int recipeId, string operatorName)
        {
            var recipe = await _recipeRepository.GetByIdAsync(recipeId);
            if (recipe == null) return false;

            if (recipe.Status != RecipeStatus.Draft)
                throw new InvalidOperationException("只有草稿状态的配方才能提交审核");

            recipe.Status = RecipeStatus.PendingReview;
            recipe.UpdatedAt = DateTime.Now;
            recipe.UpdatedBy = operatorName;
            await _recipeRepository.UpdateAsync(recipe);

            _logger.Information("配方提交审核: {Code} - {Name}, Operator: {Operator}", recipe.Code, recipe.Name, operatorName);

            return true;
        }

        public async Task<bool> ApproveAsync(int recipeId, string operatorName)
        {
            var recipe = await _recipeRepository.GetByIdAsync(recipeId);
            if (recipe == null) return false;

            if (recipe.Status != RecipeStatus.PendingReview)
                throw new InvalidOperationException("只有待审核状态的配方才能审核通过");

            recipe.Status = RecipeStatus.Approved;
            recipe.ApprovedAt = DateTime.Now;
            recipe.ApprovedBy = operatorName;
            recipe.UpdatedAt = DateTime.Now;
            recipe.UpdatedBy = operatorName;
            await _recipeRepository.UpdateAsync(recipe);

            _logger.Information("配方审核通过: {Code} - {Name}, Operator: {Operator}", recipe.Code, recipe.Name, operatorName);

            return true;
        }

        public async Task<bool> RejectAsync(int recipeId, string reason, string operatorName)
        {
            var recipe = await _recipeRepository.GetByIdAsync(recipeId);
            if (recipe == null) return false;

            if (recipe.Status != RecipeStatus.PendingReview)
                throw new InvalidOperationException("只有待审核状态的配方才能驳回");

            recipe.Status = RecipeStatus.Draft;
            recipe.Remark = $"驳回原因: {reason}\n{recipe.Remark}";
            recipe.UpdatedAt = DateTime.Now;
            recipe.UpdatedBy = operatorName;
            await _recipeRepository.UpdateAsync(recipe);

            _logger.Information("配方驳回: {Code} - {Name}, Reason: {Reason}, Operator: {Operator}", recipe.Code, recipe.Name, reason, operatorName);

            return true;
        }

        public async Task<bool> ReleaseAsync(int recipeId, string operatorName)
        {
            var recipe = await _recipeRepository.GetByIdAsync(recipeId);
            if (recipe == null) return false;

            if (recipe.Status != RecipeStatus.Approved)
                throw new InvalidOperationException("只有已审核状态的配方才能发布");

            recipe.Status = RecipeStatus.Released;
            recipe.ReleasedAt = DateTime.Now;
            recipe.ReleasedBy = operatorName;
            recipe.UpdatedAt = DateTime.Now;
            recipe.UpdatedBy = operatorName;
            await _recipeRepository.UpdateAsync(recipe);

            _logger.Information("配方发布: {Code} - {Name}, Operator: {Operator}", recipe.Code, recipe.Name, operatorName);

            return true;
        }

        public async Task<bool> ArchiveAsync(int recipeId, string operatorName)
        {
            var recipe = await _recipeRepository.GetByIdAsync(recipeId);
            if (recipe == null) return false;

            if (recipe.Status != RecipeStatus.Released)
                throw new InvalidOperationException("只有已发布状态的配方才能归档");

            recipe.Status = RecipeStatus.Archived;
            recipe.UpdatedAt = DateTime.Now;
            recipe.UpdatedBy = operatorName;
            await _recipeRepository.UpdateAsync(recipe);

            _logger.Information("配方归档: {Code} - {Name}, Operator: {Operator}", recipe.Code, recipe.Name, operatorName);

            return true;
        }

        public async Task<bool> ObsoleteAsync(int recipeId, string operatorName)
        {
            var recipe = await _recipeRepository.GetByIdAsync(recipeId);
            if (recipe == null) return false;

            recipe.Status = RecipeStatus.Obsolete;
            recipe.UpdatedAt = DateTime.Now;
            recipe.UpdatedBy = operatorName;
            await _recipeRepository.UpdateAsync(recipe);

            _logger.Information("配方作废: {Code} - {Name}, Operator: {Operator}", recipe.Code, recipe.Name, operatorName);

            return true;
        }

        #endregion

        #region 称量配方明细管理

        public async Task<IEnumerable<WeighingRecipeLine>> GetWeighingLinesAsync(int recipeId)
        {
            return await _recipeRepository.GetWeighingLinesAsync(recipeId);
        }

        public async Task<WeighingRecipeLine> AddWeighingLineAsync(int recipeId, WeighingRecipeLine line, string operatorName)
        {
            var recipe = await _recipeRepository.GetWeighingRecipeAsync(recipeId);
            if (recipe == null)
                throw new InvalidOperationException("配方不存在或不是称量配方");

            line.RecipeId = recipeId;
            if (line.LineNumber == 0)
            {
                var existingLines = await _recipeRepository.GetWeighingLinesAsync(recipeId);
                line.LineNumber = existingLines.Count() + 1;
            }

            var result = await _recipeRepository.AddWeighingLineAsync(line);

            // 重新计算总重量
            await RecalculateTotalWeightAsync(recipeId);

            _logger.Information("添加称量配方明细: RecipeId={RecipeId}, Line={LineNumber}, MaterialId={MaterialId}, Weight={Weight}",
                recipeId, line.LineNumber, line.MaterialId, line.TargetWeight);

            return result;
        }

        public async Task<WeighingRecipeLine> UpdateWeighingLineAsync(WeighingRecipeLine line, string operatorName)
        {
            var result = await _recipeRepository.UpdateWeighingLineAsync(line);

            // 重新计算总重量
            await RecalculateTotalWeightAsync(line.RecipeId);

            return result;
        }

        public async Task<bool> DeleteWeighingLineAsync(int lineId, string operatorName)
        {
            var line = await _recipeRepository.GetWeighingLinesAsync(0); // 需要先获取lineId对应的recipeId
            // 这里简化处理，实际需要先查询line信息

            var result = await _recipeRepository.DeleteWeighingLineAsync(lineId);
            return result;
        }

        public async Task<bool> ReorderWeighingLinesAsync(int recipeId, List<int> lineIdsInOrder, string operatorName)
        {
            var result = await _recipeRepository.ReorderWeighingLinesAsync(recipeId, lineIdsInOrder);

            if (result)
            {
                await RecalculateTotalWeightAsync(recipeId);
                _logger.Information("重排称量配方明细: RecipeId={RecipeId}, Operator: {Operator}", recipeId, operatorName);
            }

            return result;
        }

        private async Task RecalculateTotalWeightAsync(int recipeId)
        {
            var lines = await _recipeRepository.GetWeighingLinesAsync(recipeId);
            var totalWeight = lines.Sum(l => l.TargetWeight);

            var recipe = await _recipeRepository.GetWeighingRecipeAsync(recipeId);
            if (recipe != null)
            {
                recipe.TotalWeight = totalWeight;
                await _recipeRepository.UpdateAsync(recipe);
            }
        }

        #endregion

        #region 混炼配方步骤管理

        public async Task<IEnumerable<MixingRecipeStep>> GetMixingStepsAsync(int recipeId)
        {
            return await _recipeRepository.GetMixingStepsAsync(recipeId);
        }

        public async Task<MixingRecipeStep> AddMixingStepAsync(int recipeId, MixingRecipeStep step, string operatorName)
        {
            var recipe = await _recipeRepository.GetMixingRecipeAsync(recipeId);
            if (recipe == null)
                throw new InvalidOperationException("配方不存在或不是混炼配方");

            step.RecipeId = recipeId;
            if (step.StepNumber == 0)
            {
                var existingSteps = await _recipeRepository.GetMixingStepsAsync(recipeId);
                step.StepNumber = existingSteps.Count() + 1;
            }

            var result = await _recipeRepository.AddMixingStepAsync(step);

            // 更新总步骤数
            await UpdateTotalStepsAsync(recipeId);

            _logger.Information("添加混炼步骤: RecipeId={RecipeId}, Step={StepNumber}, Type={Type}",
                recipeId, step.StepNumber, step.StepType);

            return result;
        }

        public async Task<MixingRecipeStep> UpdateMixingStepAsync(MixingRecipeStep step, string operatorName)
        {
            return await _recipeRepository.UpdateMixingStepAsync(step);
        }

        public async Task<bool> DeleteMixingStepAsync(int stepId, string operatorName)
        {
            // 这里简化处理
            return await _recipeRepository.DeleteMixingStepAsync(stepId);
        }

        public async Task<bool> ReorderMixingStepsAsync(int recipeId, List<int> stepIdsInOrder, string operatorName)
        {
            var result = await _recipeRepository.ReorderMixingStepsAsync(recipeId, stepIdsInOrder);

            if (result)
            {
                _logger.Information("重排混炼步骤: RecipeId={RecipeId}, Operator: {Operator}", recipeId, operatorName);
            }

            return result;
        }

        private async Task UpdateTotalStepsAsync(int recipeId)
        {
            var steps = await _recipeRepository.GetMixingStepsAsync(recipeId);
            var recipe = await _recipeRepository.GetMixingRecipeAsync(recipeId);
            if (recipe != null)
            {
                recipe.TotalSteps = steps.Count();
                await _recipeRepository.UpdateAsync(recipe);
            }
        }

        #endregion

        #region 配方下发

        public async Task<RecipeDownloadLog> DownloadRecipeToPlcAsync(int recipeId, string targetPlc, string operatorName)
        {
            var recipe = await _recipeRepository.GetByIdAsync(recipeId);
            if (recipe == null)
                throw new InvalidOperationException("配方不存在");

            if (recipe.Status != RecipeStatus.Released)
                throw new InvalidOperationException("只有已发布状态的配方才能下发");

            // 创建下发日志
            var log = new RecipeDownloadLog
            {
                RecipeId = recipeId,
                RecipeCode = recipe.Code,
                RecipeName = recipe.Name,
                RecipeType = recipe.RecipeType,
                Version = recipe.CurrentVersion,
                TargetPlc = targetPlc,
                Status = DownloadStatus.InProgress,
                Operator = operatorName,
                StartTime = DateTime.Now,
                DownloadData = SerializeRecipeForPlc(recipe)
            };

            await _downloadLogRepository.AddAsync(log);

            try
            {
                // 实际下发逻辑 - 这里调用PLC服务
                var success = await PerformPlcDownloadAsync(recipe, targetPlc, log);

                if (success)
                {
                    log.Status = DownloadStatus.Success;
                    log.EndTime = DateTime.Now;
                    _logger.Information("配方下发成功: {Code} -> {Plc}, Operator: {Operator}", recipe.Code, targetPlc, operatorName);
                }
                else
                {
                    log.Status = DownloadStatus.Failed;
                    log.EndTime = DateTime.Now;
                    log.ErrorMessage = "PLC下发失败";
                    _logger.Error("配方下发失败: {Code} -> {Plc}, Operator: {Operator}", recipe.Code, targetPlc, operatorName);
                }
            }
            catch (Exception ex)
            {
                log.Status = DownloadStatus.Failed;
                log.EndTime = DateTime.Now;
                log.ErrorMessage = ex.Message;
                _logger.Error(ex, "配方下发异常: {Code} -> {Plc}", recipe.Code, targetPlc);
            }

            await _downloadLogRepository.UpdateAsync(log);
            return log;
        }

        private string SerializeRecipeForPlc(RecipeBase recipe)
        {
            var data = new
            {
                RecipeId = recipe.Id,
                RecipeCode = recipe.Code,
                RecipeName = recipe.Name,
                Version = recipe.CurrentVersion,
                Timestamp = DateTime.Now,
                Data = recipe
            };
            return Newtonsoft.Json.JsonConvert.SerializeObject(data);
        }

        private async Task<bool> PerformPlcDownloadAsync(RecipeBase recipe, string targetPlc, RecipeDownloadLog log)
        {
            // 这里需要集成 PLC 服务
            // 暂时模拟成功
            await Task.Delay(500); // 模拟网络延迟
            return true;
        }

        public async Task<RecipeDownloadLog?> GetDownloadLogAsync(int logId)
        {
            return await _downloadLogRepository.GetByIdAsync(logId);
        }

        public async Task<IEnumerable<RecipeDownloadLog>> GetDownloadHistoryAsync(int recipeId)
        {
            return await _downloadLogRepository.GetByRecipeIdAsync(recipeId);
        }

        public async Task<(IEnumerable<RecipeDownloadLog> Items, int TotalCount)> GetDownloadLogsPagedAsync(
            int page, int pageSize, string? plcName = null, DownloadStatus? status = null)
        {
            var items = await _downloadLogRepository.GetPagedAsync(page, pageSize, plcName, status);
            var total = await _downloadLogRepository.GetCountAsync(plcName, status);
            return (items, total);
        }

        #endregion

        #region 批次管理

        public async Task<BatchRecord?> GetBatchByIdAsync(int id)
        {
            return await _batchRecordRepository.GetByIdAsync(id);
        }

        public async Task<BatchRecord?> GetBatchByNumberAsync(string batchNumber)
        {
            return await _batchRecordRepository.GetByBatchNumberAsync(batchNumber);
        }

        public async Task<(IEnumerable<BatchRecord> Items, int TotalCount)> GetBatchesPagedAsync(
            int page, int pageSize, BatchFilter filter)
        {
            var items = await _batchRecordRepository.GetPagedAsync(page, pageSize, filter);
            var total = await _batchRecordRepository.GetCountAsync(filter);
            return (items, total);
        }

        public async Task<BatchRecord> CreateBatchAsync(int recipeId, int mixerId, int scaleId, string operatorName)
        {
            var recipe = await _recipeRepository.GetByIdAsync(recipeId);
            if (recipe == null)
                throw new InvalidOperationException("配方不存在");

            if (recipe.Status != RecipeStatus.Released)
                throw new InvalidOperationException("只有已发布的配方才能创建批次");

            var batchNumber = await _batchRecordRepository.GenerateBatchNumberAsync();

            var batch = new BatchRecord
            {
                BatchNumber = batchNumber,
                RecipeId = recipeId,
                RecipeCode = recipe.Code,
                RecipeVersion = recipe.CurrentVersion,
                MixerId = mixerId,
                ScaleId = scaleId,
                Status = BatchStatus.Created,
                TargetWeight = recipe.BatchSize,
                StartTime = DateTime.Now,
                Operator = operatorName
            };

            var result = await _batchRecordRepository.AddAsync(batch);

            _logger.Information("创建生产批次: {BatchNumber}, Recipe: {RecipeCode}, Operator: {Operator}",
                batchNumber, recipe.Code, operatorName);

            return result;
        }

        public async Task<BatchRecord> UpdateBatchStatusAsync(int batchId, BatchStatus status, string operatorName)
        {
            var batch = await _batchRecordRepository.GetByIdAsync(batchId);
            if (batch == null)
                throw new InvalidOperationException("批次不存在");

            batch.Status = status;
            if (status == BatchStatus.Completed || status == BatchStatus.Aborted)
            {
                batch.EndTime = DateTime.Now;
            }

            await _batchRecordRepository.UpdateAsync(batch);

            _logger.Information("更新批次状态: {BatchNumber} -> {Status}, Operator: {Operator}",
                batch.BatchNumber, status, operatorName);

            return batch;
        }

        #endregion

        #region 配方导入导出

        public async Task<byte[]> ExportRecipesToExcelAsync(RecipeFilter? filter = null)
        {
            var items = await _recipeRepository.GetPagedAsync(1, int.MaxValue, filter ?? new RecipeFilter());
            var recipes = items.ToList();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("配方清单");

            var headers = new[] { "编码", "名称", "类型", "状态", "产品编码", "产品名称", "批次规模", "收率", "当前版本", "备注", "创建时间", "创建人", "更新时间", "更新人" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            var recipes_list = recipes.ToList();
            for (int i = 0; i < recipes_list.Count; i++)
            {
                var r = recipes_list[i];
                var row = i + 2;
                worksheet.Cell(row, 1).Value = r.Code;
                worksheet.Cell(row, 2).Value = r.Name;
                worksheet.Cell(row, 3).Value = r.RecipeType.ToString();
                worksheet.Cell(row, 4).Value = r.Status.ToString();
                worksheet.Cell(row, 5).Value = r.ProductCode;
                worksheet.Cell(row, 6).Value = r.ProductName;
                worksheet.Cell(row, 7).Value = r.BatchSize;
                worksheet.Cell(row, 8).Value = r.YieldRate;
                worksheet.Cell(row, 8).Value = r.CurrentVersion;
                worksheet.Cell(row, 9).Value = r.Remark;
                worksheet.Cell(row, 9).Value = r.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cell(row, 10).Value = r.CreatedBy;
                worksheet.Cell(row, 11).Value = r.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cell(row, 11).Value = r.UpdatedBy;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<ImportResult> ImportRecipesFromExcelAsync(Stream excelStream, string operatorName)
        {
            var result = new ImportResult { Success = false };

            try
            {
                using var workbook = new XLWorkbook(excelStream);
                var worksheet = workbook.Worksheet(1);
                var rows = worksheet.RowsUsed().Skip(1).ToList();

                var recipesToAdd = new List<RecipeBase>();
                var errors = new List<string>();
                var existingCodes = new HashSet<string>(
                    (await _recipeRepository.GetAllAsync(new RecipeFilter())).Select(r => r.Code),
                    StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < rows.Count; i++)
                {
                    var row = rows[i];
                    var rowNum = i + 2;

                    try
                    {
                        var code = row.Cell(1).GetString().Trim();
                        var name = row.Cell(2).GetString().Trim();
                        var recipeType = Enum.TryParse<RecipeType>(row.Cell(3).GetString().Trim(), out var rt) ? rt : RecipeType.Weighing;
                        var status = Enum.TryParse<RecipeStatus>(row.Cell(4).GetString().Trim(), out var rs) ? rs : RecipeStatus.Draft;
                        var productCode = row.Cell(5).GetString().Trim();
                        var productName = row.Cell(6).GetString().Trim();
                        var batchSize = decimal.TryParse(row.Cell(7).GetString(), out var bs) ? bs : 0;
                        var yieldRate = decimal.TryParse(row.Cell(8).GetString(), out var yr) ? yr : 0;
                        var version = row.Cell(9).GetString().Trim();
                        var remark = row.Cell(10).GetString().Trim();

                        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
                        {
                            errors.Add($"第 {rowNum} 行: 编码或名称为空");
                            continue;
                        }

                        if (existingCodes.Contains(code))
                        {
                            errors.Add($"第 {rowNum} 行: 编码 '{code}' 已存在");
                            continue;
                        }

                        RecipeBase recipe = recipeType == RecipeType.Weighing
                            ? new WeighingRecipe()
                            : new MixingRecipe();

                        recipe.Code = code;
                        recipe.Name = name;
                        recipe.RecipeType = recipeType;
                        recipe.Status = status;
                        recipe.ProductCode = productCode;
                        recipe.ProductName = productName;
                        recipe.BatchSize = batchSize;
                        recipe.YieldRate = yieldRate;
                        recipe.CurrentVersion = version;
                        recipe.Remark = remark;
                        recipe.CreatedAt = DateTime.Now;
                        recipe.CreatedBy = operatorName;
                        recipe.UpdatedAt = DateTime.Now;
                        recipe.UpdatedBy = operatorName;

                        recipesToAdd.Add(recipe);
                        existingCodes.Add(code);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"第 {rowNum} 行: {ex.Message}");
                    }
                }

                if (recipesToAdd.Count > 0)
                {
                    foreach (var r in recipesToAdd)
                    {
                        await _recipeRepository.AddAsync(r);
                    }
                    result.Success = true;
                    result.ImportedCount = recipesToAdd.Count;
                }

                result.Errors = errors;
                result.Message = $"成功导入 {result.ImportedCount} 条，失败 {errors.Count} 条";

                _logger.Information("Excel导入配方: 成功 {Count} 条, 失败 {ErrorCount} 条, Operator: {Operator}",
                    result.ImportedCount, errors.Count, operatorName);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"导入失败: {ex.Message}";
                _logger.Error(ex, "Excel导入配方失败, Operator: {Operator}", operatorName);
            }

            return result;
        }

        public async Task<RecipeComparisonResult> CompareRecipesAsync(int recipeId1, int recipeId2)
        {
            var recipe1 = await _recipeRepository.GetByIdAsync(recipeId1);
            var recipe2 = await _recipeRepository.GetByIdAsync(recipeId2);

            if (recipe1 == null || recipe2 == null)
                throw new InvalidOperationException("配方不存在");

            var result = new RecipeComparisonResult
            {
                Recipe1Id = recipeId1,
                Recipe2Id = recipeId2,
                Recipe1Code = recipe1.Code,
                Recipe2Code = recipe2.Code,
                AreIdentical = true
            };

            // 基础属性对比
            if (recipe1.Name != recipe2.Name) { result.AreIdentical = false; result.Differences.Add($"名称不同: '{recipe1.Name}' vs '{recipe2.Name}'"); }
            if (recipe1.RecipeType != recipe2.RecipeType) { result.AreIdentical = false; result.Differences.Add($"类型不同: {recipe1.RecipeType} vs {recipe2.RecipeType}"); }
            if (recipe1.ProductCode != recipe2.ProductCode) { result.AreIdentical = false; result.Differences.Add($"产品编码不同"); }
            if (recipe1.ProductName != recipe2.ProductName) { result.AreIdentical = false; result.Differences.Add($"产品名称不同"); }
            if (recipe1.BatchSize != recipe2.BatchSize) { result.AreIdentical = false; result.Differences.Add($"批次规模不同: {recipe1.BatchSize} vs {recipe2.BatchSize}"); }
            if (recipe1.YieldRate != recipe2.YieldRate) { result.AreIdentical = false; result.Differences.Add($"收率不同: {recipe1.YieldRate} vs {recipe2.YieldRate}"); }
            if (recipe1.CurrentVersion != recipe2.CurrentVersion) { result.AreIdentical = false; result.Differences.Add($"版本不同: {recipe1.CurrentVersion} vs {recipe2.CurrentVersion}"); }

            // 称量配方明细对比
            if (recipe1 is WeighingRecipe wr1 && recipe2 is WeighingRecipe wr2)
            {
                var lines1 = await _recipeRepository.GetWeighingLinesAsync(recipeId1);
                var lines2 = await _recipeRepository.GetWeighingLinesAsync(recipeId2);

                if (lines1.Count() != lines2.Count())
                {
                    result.AreIdentical = false;
                    result.Differences.Add($"明细行数不同: {lines1.Count()} vs {lines2.Count()}");
                }
                else
                {
                    var lines1List = lines1.OrderBy(l => l.LineNumber).ToList();
                    var lines2List = lines2.OrderBy(l => l.LineNumber).ToList();
                    for (int i = 0; i < lines1List.Count; i++)
                    {
                        var l1 = lines1List[i];
                        var l2 = lines2List[i];
                        if (l1.MaterialId != l2.MaterialId) { result.AreIdentical = false; result.Differences.Add($"行 {i+1} 物料不同"); }
                        if (l1.TargetWeight != l2.TargetWeight) { result.AreIdentical = false; result.Differences.Add($"行 {i+1} 目标重量不同: {l1.TargetWeight} vs {l2.TargetWeight}"); }
                        if (l1.TolerancePositive != l2.TolerancePositive) { result.AreIdentical = false; result.Differences.Add($"行 {i+1} 正公差不同"); }
                        if (l1.ToleranceNegative != l2.ToleranceNegative) { result.AreIdentical = false; result.Differences.Add($"行 {i+1} 负公差不同"); }
                    }
                }
            }
            else if (recipe1 is MixingRecipe mr1 && recipe2 is MixingRecipe mr2)
            {
                var steps1 = await _recipeRepository.GetMixingStepsAsync(recipeId1);
                var steps2 = await _recipeRepository.GetMixingStepsAsync(recipeId2);

                if (steps1.Count() != steps2.Count())
                {
                    result.AreIdentical = false;
                    result.Differences.Add($"步骤数不同: {steps1.Count()} vs {steps2.Count()}");
                }
                else
                {
                    var steps1List = steps1.OrderBy(s => s.StepNumber).ToList();
                    var steps2List = steps2.OrderBy(s => s.StepNumber).ToList();
                    for (int i = 0; i < steps1List.Count; i++)
                    {
                        var s1 = steps1List[i];
                        var s2 = steps2List[i];
                        if (s1.StepType != s2.StepType) { result.AreIdentical = false; result.Differences.Add($"步骤 {i+1} 类型不同"); }
                        if (s1.MaterialId != s2.MaterialId) { result.AreIdentical = false; result.Differences.Add($"步骤 {i+1} 物料不同"); }
                        if (s1.Amount != s2.Amount) { result.AreIdentical = false; result.Differences.Add($"步骤 {i+1} 量不同: {s1.Amount} vs {s2.Amount}"); }
                        if (s1.Duration != s2.Duration) { result.AreIdentical = false; result.Differences.Add($"步骤 {i+1} 时长不同"); }
                        if (s1.TargetTemp != s2.TargetTemp) { result.AreIdentical = false; result.Differences.Add($"步骤 {i+1} 目标温度不同"); }
                    }
                }
            }

            return result;
        }

        public async Task<BatchDownloadResult> BatchDownloadRecipesAsync(List<int> recipeIds, string targetPlc, string operatorName)
        {
            var result = new BatchDownloadResult
            {
                TotalCount = recipeIds.Count
            };

            foreach (var recipeId in recipeIds)
            {
                try
                {
                    var log = await DownloadRecipeToPlcAsync(recipeId, targetPlc, operatorName);
                    if (log.Status == DownloadStatus.Success)
                    {
                        result.SuccessCount++;
                        result.Results.Add(new DownloadResult { Success = true, Message = $"配方 {recipeId} 下发成功" });
                    }
                    else
                    {
                        result.FailedCount++;
                        result.Results.Add(new DownloadResult { Success = false, ErrorMessage = log.ErrorMessage });
                    }
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    result.Results.Add(new DownloadResult { Success = false, ErrorMessage = ex.Message });
                    _logger.Error(ex, "批量下发配方异常: RecipeId={RecipeId}", recipeId);
                }
            }

            result.Message = $"批量下发完成: 成功 {result.SuccessCount} 条，失败 {result.FailedCount} 条";
            return result;
        }

        public async Task<byte[]> ExportRecipeToJsonAsync(int recipeId)
        {
            var recipe = await _recipeRepository.GetByIdAsync(recipeId);
            if (recipe == null) throw new InvalidOperationException("配方不存在");

            var data = new
            {
                Recipe = recipe,
                WeighingLines = recipe is WeighingRecipe wr ? await _recipeRepository.GetWeighingLinesAsync(recipeId) : null,
                MixingSteps = recipe is MixingRecipe mr ? await _recipeRepository.GetMixingStepsAsync(recipeId) : null,
                Versions = await _recipeRepository.GetVersionsAsync(recipeId),
                ExportTime = DateTime.Now
            };

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
            return System.Text.Encoding.UTF8.GetBytes(json);
        }

        public async Task<RecipeBase> ImportRecipeFromJsonAsync(byte[] jsonData, string operatorName)
        {
            var json = System.Text.Encoding.UTF8.GetString(jsonData);
            var settings = new Newtonsoft.Json.JsonSerializerSettings
            {
                TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto
            };
            var jobj = Newtonsoft.Json.Linq.JObject.Parse(json);
            var serializer = Newtonsoft.Json.JsonSerializer.Create(settings);

            // 使用自定义转换器仅反序列化 Recipe 属性
            var converter = new RecipeBaseJsonConverter();
            var recipe = jobj["Recipe"] != null 
                ? converter.ReadJson(jobj["Recipe"].CreateReader(), typeof(RecipeBase), null, false, serializer) as RecipeBase
                : null;

            if (recipe == null)
                throw new InvalidOperationException("JSON数据中无有效配方");

            // 其他属性正常反序列化
            var weighingLines = jobj["WeighingLines"]?.ToObject<List<WeighingRecipeLine>>(serializer) ?? new List<WeighingRecipeLine>();
            var mixingSteps = jobj["MixingSteps"]?.ToObject<List<MixingRecipeStep>>(serializer) ?? new List<MixingRecipeStep>();
            var versions = jobj["Versions"]?.ToObject<List<RecipeVersion>>(serializer) ?? new List<RecipeVersion>();

            recipe.Code = recipe.Code + "_import_" + DateTime.Now.Ticks;
            recipe.Status = RecipeStatus.Draft;
            recipe.CurrentVersion = "1.0";
            recipe.CreatedAt = DateTime.Now;
            recipe.CreatedBy = operatorName;
            recipe.UpdatedAt = DateTime.Now;
            recipe.UpdatedBy = operatorName;

            if (recipe is WeighingRecipe wr)
            {
                foreach (var line in weighingLines)
                {
                    line.Id = 0;
                    line.RecipeId = 0;
                    wr.Lines.Add(line);
                }
            }
            else if (recipe is MixingRecipe mr)
            {
                foreach (var step in mixingSteps)
                {
                    step.Id = 0;
                    step.RecipeId = 0;
                    mr.Steps.Add(step);
                }
            }

            foreach (var v in versions)
            {
                v.Id = 0;
                v.RecipeId = 0;
            }

            var result = await _recipeRepository.AddAsync(recipe);
            _logger.Information("从JSON导入配方: {Code} - {Name}, Operator: {Operator}", recipe.Code, recipe.Name, operatorName);

            return result;
        }
    }
}
#endregion

