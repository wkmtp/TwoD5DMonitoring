using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog;
using TwoD5DMonitoring.Infrastructure.Logging;
using TwoD5DMonitoring.Infrastructure.PLC;
using TwoD5DMonitoring.Models;
using TwoD5DMonitoring.Services.Repositories;

namespace TwoD5DMonitoring.Services
{
    /// <summary>
    /// PLC 配方下发服务 - 处理配方数据到 PLC 的下载、校验、同步
    /// </summary>
    public class PlcRecipeDownloadService
    {
        private readonly ILogger _logger;
        private readonly PlcManager _plcManager;
        private readonly IRecipeRepository _recipeRepository;
        private readonly IRecipeDownloadLogRepository _downloadLogRepository;
        private readonly IMaterialRepository _materialRepository;

        // PLC 标签地址映射配置
        private readonly Dictionary<string, PlcTagMapping> _weighingTagMappings = new();
        private readonly Dictionary<string, PlcTagMapping> _mixingTagMappings = new();

        public PlcRecipeDownloadService(
            PlcManager plcManager,
            IRecipeRepository recipeRepository,
            IRecipeDownloadLogRepository downloadLogRepository,
            IMaterialRepository materialRepository)
        {
            _logger = LogManager.CreateModuleLogger(nameof(PlcRecipeDownloadService));
            _plcManager = plcManager;
            _recipeRepository = recipeRepository;
            _downloadLogRepository = downloadLogRepository;
            _materialRepository = materialRepository;

            InitializeDefaultTagMappings();
        }

        /// <summary>
        /// 下发称量配方到 PLC
        /// </summary>
        public async Task<DownloadResult> DownloadWeighingRecipeAsync(
            int recipeId, 
            string plcName, 
            string operatorName,
            bool verifyAfterDownload = true)
        {
            var recipe = await _recipeRepository.GetWeighingRecipeAsync(recipeId);
            if (recipe == null)
                return new DownloadResult { Success = false, ErrorMessage = "配方不存在" };

            if (recipe.Status != RecipeStatus.Released)
                return new DownloadResult { Success = false, ErrorMessage = "只有已发布状态的配方才能下发" };

            var lines = await _recipeRepository.GetWeighingLinesAsync(recipeId);
            if (!lines.Any())
                return new DownloadResult { Success = false, ErrorMessage = "配方无明细行" };

            // 创建下发日志
            var log = new RecipeDownloadLog
            {
                RecipeId = recipeId,
                RecipeCode = recipe.Code,
                RecipeName = recipe.Name,
                RecipeType = RecipeType.Weighing,
                Version = recipe.CurrentVersion,
                TargetPlc = plcName,
                Status = DownloadStatus.InProgress,
                Operator = operatorName,
                StartTime = DateTime.Now
            };
            await _downloadLogRepository.AddAsync(log);

            try
            {
                var driver = _plcManager.GetDriver(plcName);
                if (driver == null)
                    throw new InvalidOperationException($"PLC驱动不存在: {plcName}");

                if (driver.ConnectionState != PlcConnectionState.Connected)
                {
                    var connected = await driver.ReconnectAsync();
                    if (!connected)
                        throw new InvalidOperationException($"PLC {plcName} 连接失败");
                }

                // 1. 下发配方基本信息
                await DownloadWeighingHeaderAsync(driver, recipe, lines);

                // 2. 下发物料明细
                await DownloadWeighingLinesAsync(driver, recipe, lines);

                // 3. 下发称量参数
                await DownloadWeighingParametersAsync(driver, recipe);

                // 4. 校验下发
                if (verifyAfterDownload)
                {
                    var verifyResult = await VerifyWeighingRecipeAsync(driver, recipe, lines);
                    if (!verifyResult.Success)
                    {
                        log.Status = DownloadStatus.Failed;
                        log.ErrorMessage = $"校验失败: {verifyResult.ErrorMessage}";
                        await _downloadLogRepository.UpdateAsync(log);
                        return new DownloadResult { Success = false, ErrorMessage = verifyResult.ErrorMessage };
                    }
                }

                log.Status = DownloadStatus.Verified;
                log.EndTime = DateTime.Now;
                await _downloadLogRepository.UpdateAsync(log);

                _logger.Information("称量配方下发成功: {RecipeCode} -> {PlcName}, Lines: {LineCount}",
                    recipe.Code, plcName, lines.Count());

                return new DownloadResult { Success = true, Message = "下发并校验成功" };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "称量配方下发异常: RecipeId={RecipeId}, Plc={PlcName}", recipeId, plcName);
                
                log.Status = DownloadStatus.Failed;
                log.EndTime = DateTime.Now;
                log.ErrorMessage = ex.Message;
                await _downloadLogRepository.UpdateAsync(log);

                return new DownloadResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        /// <summary>
        /// 下发混炼配方到 PLC
        /// </summary>
        public async Task<DownloadResult> DownloadMixingRecipeAsync(
            int recipeId, 
            string plcName, 
            string operatorName,
            bool verifyAfterDownload = true)
        {
            var recipe = await _recipeRepository.GetMixingRecipeAsync(recipeId);
            if (recipe == null)
                return new DownloadResult { Success = false, ErrorMessage = "配方不存在" };

            if (recipe.Status != RecipeStatus.Released)
                return new DownloadResult { Success = false, ErrorMessage = "只有已发布状态的配方才能下发" };

            var steps = await _recipeRepository.GetMixingStepsAsync(recipeId);
            if (!steps.Any())
                return new DownloadResult { Success = false, ErrorMessage = "配方无步骤" };

            var log = new RecipeDownloadLog
            {
                RecipeId = recipeId,
                RecipeCode = recipe.Code,
                RecipeName = recipe.Name,
                RecipeType = RecipeType.Mixing,
                Version = recipe.CurrentVersion,
                TargetPlc = plcName,
                Status = DownloadStatus.InProgress,
                Operator = operatorName,
                StartTime = DateTime.Now
            };
            await _downloadLogRepository.AddAsync(log);

            try
            {
                var driver = _plcManager.GetDriver(plcName);
                if (driver == null)
                    throw new InvalidOperationException($"PLC驱动不存在: {plcName}");

                if (driver.ConnectionState != PlcConnectionState.Connected)
                {
                    var connected = await driver.ReconnectAsync();
                    if (!connected)
                        throw new InvalidOperationException($"PLC {plcName} 连接失败");
                }

                // 1. 下发配方基本信息
                await DownloadMixingHeaderAsync(driver, recipe, steps);

                // 2. 下发步骤参数
                await DownloadMixingStepsAsync(driver, recipe, steps);

                // 3. 下发工艺参数
                await DownloadMixingParametersAsync(driver, recipe);

                // 4. 校验下发
                if (verifyAfterDownload)
                {
                    var verifyResult = await VerifyMixingRecipeAsync(driver, recipe, steps);
                    if (!verifyResult.Success)
                    {
                        log.Status = DownloadStatus.Failed;
                        log.ErrorMessage = $"校验失败: {verifyResult.ErrorMessage}";
                        await _downloadLogRepository.UpdateAsync(log);
                        return new DownloadResult { Success = false, ErrorMessage = verifyResult.ErrorMessage };
                    }
                }

                log.Status = DownloadStatus.Verified;
                log.EndTime = DateTime.Now;
                await _downloadLogRepository.UpdateAsync(log);

                _logger.Information("混炼配方下发成功: {RecipeCode} -> {PlcName}, Steps: {StepCount}",
                    recipe.Code, plcName, steps.Count());

                return new DownloadResult { Success = true, Message = "下发并校验成功" };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "混炼配方下发异常: RecipeId={RecipeId}, Plc={PlcName}", recipeId, plcName);

                log.Status = DownloadStatus.Failed;
                log.EndTime = DateTime.Now;
                log.ErrorMessage = ex.Message;
                await _downloadLogRepository.UpdateAsync(log);

                return new DownloadResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        /// <summary>
        /// 批量下发配方
        /// </summary>
        public async Task<BatchDownloadResult> DownloadRecipesBatchAsync(
            List<int> recipeIds, 
            string plcName, 
            string operatorName)
        {
            var results = new List<DownloadResult>();
            var successCount = 0;
            var failedCount = 0;

            foreach (var recipeId in recipeIds)
            {
                var recipe = await _recipeRepository.GetByIdAsync(recipeId);
                if (recipe == null)
                {
                    results.Add(new DownloadResult { Success = false, ErrorMessage = "配方不存在" });
                    failedCount++;
                    continue;
                }

                DownloadResult result;
                if (recipe.RecipeType == RecipeType.Weighing)
                {
                    result = await DownloadWeighingRecipeAsync(recipeId, plcName, operatorName);
                }
                else
                {
                    result = await DownloadMixingRecipeAsync(recipeId, plcName, operatorName);
                }

                results.Add(result);
                if (result.Success) successCount++;
                else failedCount++;
            }

            return new BatchDownloadResult
            {
                TotalCount = recipeIds.Count,
                SuccessCount = successCount,
                FailedCount = failedCount,
                Results = results
            };
        }

        /// <summary>
        /// 从 PLC 读回配方进行校验
        /// </summary>
        public async Task<VerifyResult> VerifyRecipeFromPlcAsync(int recipeId, string plcName)
        {
            var recipe = await _recipeRepository.GetByIdAsync(recipeId);
            if (recipe == null)
                return new VerifyResult { Success = false, ErrorMessage = "配方不存在" };

            var driver = _plcManager.GetDriver(plcName);
            if (driver == null)
                return new VerifyResult { Success = false, ErrorMessage = $"PLC驱动不存在: {plcName}" };

            if (recipe.RecipeType == RecipeType.Weighing)
            {
                var lines = await _recipeRepository.GetWeighingLinesAsync(recipeId);
                return await VerifyWeighingRecipeAsync(driver, (WeighingRecipe)recipe, lines);
            }
            else
            {
                var steps = await _recipeRepository.GetMixingStepsAsync(recipeId);
                return await VerifyMixingRecipeAsync(driver, (MixingRecipe)recipe, steps);
            }
        }

        /// <summary>
        /// 配置 PLC 标签映射
        /// </summary>
        public void SetTagMapping(string recipeType, string logicalName, PlcTagMapping mapping)
        {
            if (recipeType == "Weighing")
                _weighingTagMappings[logicalName] = mapping;
            else if (recipeType == "Mixing")
                _mixingTagMappings[logicalName] = mapping;
        }

        /// <summary>
        /// 获取标签映射
        /// </summary>
        public PlcTagMapping? GetTagMapping(string recipeType, string logicalName)
        {
            if (recipeType == "Weighing")
                return _weighingTagMappings.TryGetValue(logicalName, out var m) ? m : null;
            else if (recipeType == "Mixing")
                return _mixingTagMappings.TryGetValue(logicalName, out var m) ? m : null;
            return null;
        }

        #region 私有下发方法

        private async Task DownloadWeighingHeaderAsync(IPlcDriver driver, WeighingRecipe recipe, IEnumerable<WeighingRecipeLine> lines)
        {
            var writes = new Dictionary<string, object>
            {
                { GetTagAddress("Weighing", "RecipeCode"), recipe.Code },
                { GetTagAddress("Weighing", "RecipeVersion"), recipe.CurrentVersion },
                { GetTagAddress("Weighing", "TotalLines"), lines.Count() },
                { GetTagAddress("Weighing", "WeighingMode"), (int)recipe.WeighingMode },
                { GetTagAddress("Weighing", "ScaleId"), recipe.ScaleId },
                { GetTagAddress("Weighing", "DecimalPlaces"), recipe.DecimalPlaces },
                { GetTagAddress("Weighing", "AllowManualAdjust"), recipe.AllowManualAdjust }
            };

            await driver.WriteMultipleAsync(writes);
        }

        private async Task DownloadWeighingLinesAsync(IPlcDriver driver, WeighingRecipe recipe, IEnumerable<WeighingRecipeLine> lines)
        {
            var lineList = lines.OrderBy(l => l.LineNumber).ToList();
            int index = 0;

            foreach (var line in lineList)
            {
                var material = await _materialRepository.GetByIdAsync(line.MaterialId);
                var materialCode = material?.Code ?? line.MaterialId.ToString();

                var baseAddr = GetLineBaseAddress("Weighing", index);
                
                var writes = new Dictionary<string, object>
                {
                    { $"{baseAddr}.LineNumber", line.LineNumber },
                    { $"{baseAddr}.MaterialCode", materialCode },
                    { $"{baseAddr}.TargetWeight", (float)line.TargetWeight },
                    { $"{baseAddr}.TolerancePositive", (float)line.TolerancePositive },
                    { $"{baseAddr}.ToleranceNegative", (float)line.ToleranceNegative },
                    { $"{baseAddr}.WeighingOrder", line.WeighingOrder },
                    { $"{baseAddr}.IsCritical", line.IsCritical },
                    { $"{baseAddr}.FeederType", (int)line.FeederType }
                };

                await driver.WriteMultipleAsync(writes);
                index++;
            }

            // 清空剩余行
            for (int i = index; i < 100; i++) // 假设最大100行
            {
                var baseAddr = GetLineBaseAddress("Weighing", i);
                await driver.WriteAsync($"{baseAddr}.LineNumber", 0);
            }
        }

        private async Task DownloadWeighingParametersAsync(IPlcDriver driver, WeighingRecipe recipe)
        {
            var writes = new Dictionary<string, object>
            {
                { GetTagAddress("Weighing", "BatchSize"), (float)recipe.BatchSize },
                { GetTagAddress("Weighing", "YieldRate"), (float)recipe.YieldRate }
            };

            await driver.WriteMultipleAsync(writes);
        }

        private async Task DownloadMixingHeaderAsync(IPlcDriver driver, MixingRecipe recipe, IEnumerable<MixingRecipeStep> steps)
        {
            var writes = new Dictionary<string, object>
            {
                { GetTagAddress("Mixing", "RecipeCode"), recipe.Code },
                { GetTagAddress("Mixing", "RecipeVersion"), recipe.CurrentVersion },
                { GetTagAddress("Mixing", "TotalSteps"), steps.Count() },
                { GetTagAddress("Mixing", "ProcessType"), (int)recipe.ProcessType },
                { GetTagAddress("Mixing", "MixerId"), recipe.MixerId }
            };

            await driver.WriteMultipleAsync(writes);
        }

        private async Task DownloadMixingStepsAsync(IPlcDriver driver, MixingRecipe recipe, IEnumerable<MixingRecipeStep> steps)
        {
            var stepList = steps.OrderBy(s => s.StepNumber).ToList();
            int index = 0;

            foreach (var step in stepList)
            {
                var material = step.MaterialId.HasValue 
                    ? await _materialRepository.GetByIdAsync(step.MaterialId.Value) 
                    : null;
                var materialCode = material?.Code ?? "";

                var baseAddr = GetLineBaseAddress("Mixing", index);
                
                var writes = new Dictionary<string, object>
                {
                    { $"{baseAddr}.StepNumber", step.StepNumber },
                    { $"{baseAddr}.StepType", (int)step.StepType },
                    { $"{baseAddr}.MaterialCode", materialCode },
                    { $"{baseAddr}.Amount", (float)step.Amount },
                    { $"{baseAddr}.AddTime", step.AddTime },
                    { $"{baseAddr}.Duration", step.Duration },
                    { $"{baseAddr}.TargetTemp", step.TargetTemp },
                    { $"{baseAddr}.RotorSpeed", step.RotorSpeed },
                    { $"{baseAddr}.RamPressure", (float)step.RamPressure },
                    { $"{baseAddr}.IsAutoDump", step.IsAutoDump },
                    { $"{baseAddr}.WaitForTemp", step.WaitForTemp }
                };

                // 条件表达式单独写入（字符串）
                if (!string.IsNullOrEmpty(step.Condition))
                {
                    writes[$"{baseAddr}.Condition"] = step.Condition;
                }

                await driver.WriteMultipleAsync(writes);
                index++;
            }

            // 清空剩余步骤
            for (int i = index; i < 50; i++) // 假设最大50步
            {
                var baseAddr = GetLineBaseAddress("Mixing", i);
                await driver.WriteAsync($"{baseAddr}.StepNumber", 0);
            }
        }

        private async Task DownloadMixingParametersAsync(IPlcDriver driver, MixingRecipe recipe)
        {
            var writes = new Dictionary<string, object>
            {
                { GetTagAddress("Mixing", "TargetTemperature"), (float)recipe.TargetTemperature },
                { GetTagAddress("Mixing", "TargetTime"), recipe.TargetTime },
                { GetTagAddress("Mixing", "DumpTemperature"), recipe.DumpTemperature },
                { GetTagAddress("Mixing", "RamPressure"), (float)recipe.RamPressure },
                { GetTagAddress("Mixing", "RamPosition"), recipe.RamPosition },
                { GetTagAddress("Mixing", "RotorSpeed"), recipe.RotorSpeed },
                { GetTagAddress("Mixing", "EnableTempControl"), recipe.EnableTempControl },
                { GetTagAddress("Mixing", "EnableEnergyControl"), recipe.EnableEnergyControl },
                { GetTagAddress("Mixing", "BatchSize"), (float)recipe.BatchSize },
                { GetTagAddress("Mixing", "YieldRate"), (float)recipe.YieldRate }
            };

            await driver.WriteMultipleAsync(writes);
        }

        #endregion

        #region 私有校验方法

        private async Task<VerifyResult> VerifyWeighingRecipeAsync(IPlcDriver driver, WeighingRecipe recipe, IEnumerable<WeighingRecipeLine> lines)
        {
            var mismatches = new List<string>();

            // 校验头部信息
            var headerChecks = new Dictionary<string, object>
            {
                { "RecipeCode", recipe.Code },
                { "RecipeVersion", recipe.CurrentVersion },
                { "TotalLines", lines.Count() }
            };

            foreach (var check in headerChecks)
            {
                var plcValue = await driver.ReadAsync<object>(GetTagAddress("Weighing", check.Key));
                if (!ValuesEqual(check.Value, plcValue))
                {
                    mismatches.Add($"头部 {check.Key}: 期望={check.Value}, 实际={plcValue}");
                }
            }

            // 校验明细行
            var lineList = lines.OrderBy(l => l.LineNumber).ToList();
            for (int i = 0; i < lineList.Count; i++)
            {
                var line = lineList[i];
                var baseAddr = GetLineBaseAddress("Weighing", i);
                var material = await _materialRepository.GetByIdAsync(line.MaterialId);
                var materialCode = material?.Code ?? line.MaterialId.ToString();

                var lineChecks = new Dictionary<string, object>
                {
                    { "LineNumber", line.LineNumber },
                    { "MaterialCode", materialCode },
                    { "TargetWeight", (float)line.TargetWeight },
                    { "TolerancePositive", (float)line.TolerancePositive },
                    { "ToleranceNegative", (float)line.ToleranceNegative }
                };

                foreach (var check in lineChecks)
                {
                    var plcValue = await driver.ReadAsync<object>($"{baseAddr}.{check.Key}");
                    if (!ValuesEqual(check.Value, plcValue))
                    {
                        mismatches.Add($"行{line.LineNumber} {check.Key}: 期望={check.Value}, 实际={plcValue}");
                    }
                }
            }

            return mismatches.Count == 0
                ? new VerifyResult { Success = true, Message = "校验通过" }
                : new VerifyResult { Success = false, ErrorMessage = string.Join("; ", mismatches), Mismatches = mismatches };
        }

        private async Task<VerifyResult> VerifyMixingRecipeAsync(IPlcDriver driver, MixingRecipe recipe, IEnumerable<MixingRecipeStep> steps)
        {
            var mismatches = new List<string>();

            // 校验头部
            var headerChecks = new Dictionary<string, object>
            {
                { "RecipeCode", recipe.Code },
                { "RecipeVersion", recipe.CurrentVersion },
                { "TotalSteps", steps.Count() }
            };

            foreach (var check in headerChecks)
            {
                var plcValue = await driver.ReadAsync<object>(GetTagAddress("Mixing", check.Key));
                if (!ValuesEqual(check.Value, plcValue))
                {
                    mismatches.Add($"头部 {check.Key}: 期望={check.Value}, 实际={plcValue}");
                }
            }

            // 校验步骤
            var stepList = steps.OrderBy(s => s.StepNumber).ToList();
            for (int i = 0; i < stepList.Count; i++)
            {
                var step = stepList[i];
                var baseAddr = GetLineBaseAddress("Mixing", i);
                var material = step.MaterialId.HasValue 
                    ? await _materialRepository.GetByIdAsync(step.MaterialId.Value) 
                    : null;
                var materialCode = material?.Code ?? "";

                var stepChecks = new Dictionary<string, object>
                {
                    { "StepNumber", step.StepNumber },
                    { "StepType", (int)step.StepType },
                    { "MaterialCode", materialCode },
                    { "Amount", (float)step.Amount },
                    { "Duration", step.Duration },
                    { "TargetTemp", step.TargetTemp }
                };

                foreach (var check in stepChecks)
                {
                    var plcValue = await driver.ReadAsync<object>($"{baseAddr}.{check.Key}");
                    if (!ValuesEqual(check.Value, plcValue))
                    {
                        mismatches.Add($"步骤{step.StepNumber} {check.Key}: 期望={check.Value}, 实际={plcValue}");
                    }
                }
            }

            return mismatches.Count == 0
                ? new VerifyResult { Success = true, Message = "校验通过" }
                : new VerifyResult { Success = false, ErrorMessage = string.Join("; ", mismatches), Mismatches = mismatches };
        }

        private bool ValuesEqual(object expected, object? actual)
        {
            if (expected == null && actual == null) return true;
            if (expected == null || actual == null) return false;

            // 数值类型比较（允许浮点数微小误差）
            if (expected is float ef && actual is float af)
                return Math.Abs(ef - af) < 0.001f;
            if (expected is double ed && actual is double ad)
                return Math.Abs(ed - ad) < 0.001;
            if (expected is decimal em && actual is decimal am)
                return Math.Abs(em - am) < 0.001m;

            return expected.Equals(actual);
        }

        #endregion

        #region 标签地址辅助

        private void InitializeDefaultTagMappings()
        {
            // 称量配方标签映射
            _weighingTagMappings["RecipeCode"] = new PlcTagMapping { Address = "Recipe.Header.Code", DataType = PlcDataType.String };
            _weighingTagMappings["RecipeVersion"] = new PlcTagMapping { Address = "Recipe.Header.Version", DataType = PlcDataType.String };
            _weighingTagMappings["TotalLines"] = new PlcTagMapping { Address = "Recipe.Header.TotalLines", DataType = PlcDataType.Int };
            _weighingTagMappings["WeighingMode"] = new PlcTagMapping { Address = "Recipe.Header.WeighingMode", DataType = PlcDataType.Int };
            _weighingTagMappings["ScaleId"] = new PlcTagMapping { Address = "Recipe.Header.ScaleId", DataType = PlcDataType.Int };
            _weighingTagMappings["DecimalPlaces"] = new PlcTagMapping { Address = "Recipe.Header.DecimalPlaces", DataType = PlcDataType.Int };
            _weighingTagMappings["AllowManualAdjust"] = new PlcTagMapping { Address = "Recipe.Header.AllowManualAdjust", DataType = PlcDataType.Bool };
            _weighingTagMappings["BatchSize"] = new PlcTagMapping { Address = "Recipe.Params.BatchSize", DataType = PlcDataType.Real };
            _weighingTagMappings["YieldRate"] = new PlcTagMapping { Address = "Recipe.Params.YieldRate", DataType = PlcDataType.Real };

            // 混炼配方标签映射
            _mixingTagMappings["RecipeCode"] = new PlcTagMapping { Address = "Mixing.Header.Code", DataType = PlcDataType.String };
            _mixingTagMappings["RecipeVersion"] = new PlcTagMapping { Address = "Mixing.Header.Version", DataType = PlcDataType.String };
            _mixingTagMappings["TotalSteps"] = new PlcTagMapping { Address = "Mixing.Header.TotalSteps", DataType = PlcDataType.Int };
            _mixingTagMappings["ProcessType"] = new PlcTagMapping { Address = "Mixing.Header.ProcessType", DataType = PlcDataType.Int };
            _mixingTagMappings["MixerId"] = new PlcTagMapping { Address = "Mixing.Header.MixerId", DataType = PlcDataType.Int };
            _mixingTagMappings["TargetTemperature"] = new PlcTagMapping { Address = "Mixing.Params.TargetTemp", DataType = PlcDataType.Real };
            _mixingTagMappings["TargetTime"] = new PlcTagMapping { Address = "Mixing.Params.TargetTime", DataType = PlcDataType.Int };
            _mixingTagMappings["DumpTemperature"] = new PlcTagMapping { Address = "Mixing.Params.DumpTemp", DataType = PlcDataType.Int };
            _mixingTagMappings["RamPressure"] = new PlcTagMapping { Address = "Mixing.Params.RamPressure", DataType = PlcDataType.Real };
            _mixingTagMappings["RamPosition"] = new PlcTagMapping { Address = "Mixing.Params.RamPosition", DataType = PlcDataType.Int };
            _mixingTagMappings["RotorSpeed"] = new PlcTagMapping { Address = "Mixing.Params.RotorSpeed", DataType = PlcDataType.Int };
            _mixingTagMappings["EnableTempControl"] = new PlcTagMapping { Address = "Mixing.Params.EnableTempCtrl", DataType = PlcDataType.Bool };
            _mixingTagMappings["EnableEnergyControl"] = new PlcTagMapping { Address = "Mixing.Params.EnableEnergyCtrl", DataType = PlcDataType.Bool };
        }

        private string GetTagAddress(string recipeType, string logicalName)
        {
            var mappings = recipeType == "Weighing" ? _weighingTagMappings : _mixingTagMappings;
            return mappings.TryGetValue(logicalName, out var mapping) ? mapping.Address : $"{recipeType}.{logicalName}";
        }

        private string GetLineBaseAddress(string recipeType, int index)
        {
            var prefix = recipeType == "Weighing" ? "Recipe.Lines" : "Mixing.Steps";
            return $"{prefix}[{index}]";
        }

        #endregion
    }

    /// <summary>
    /// PLC 标签映射配置
    /// </summary>
    public class PlcTagMapping
    {
        public string Address { get; set; } = "";
        public PlcDataType DataType { get; set; } = PlcDataType.Real;
        public string Description { get; set; } = "";
        public float Scale { get; set; } = 1.0f;
        public float Offset { get; set; } = 0.0f;
    }

    /// <summary>
    /// 下发结果
    /// </summary>
    public class DownloadResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
    }

    /// <summary>
    /// 批量下发结果
    /// </summary>
    public class BatchDownloadResult
    {
        public int TotalCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public string Message { get; set; } = "";
        public List<DownloadResult> Results { get; set; } = new();
    }

    /// <summary>
    /// 校验结果
    /// </summary>
    public class VerifyResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
        public List<string> Mismatches { get; set; } = new();
    }
}