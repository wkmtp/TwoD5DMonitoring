using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TwoD5DMonitoring.Models;

namespace TwoD5DMonitoring.Services.Repositories
{
    /// <summary>
    /// 物料分类仓储接口
    /// </summary>
    public interface IMaterialCategoryRepository
    {
        Task<MaterialCategory?> GetByIdAsync(int id);
        Task<MaterialCategory?> GetByCodeAsync(string code);
        Task<IEnumerable<MaterialCategory>> GetAllAsync(bool activeOnly = true);
        Task<IEnumerable<MaterialCategory>> GetPagedAsync(int page, int pageSize, string? keyword = null, bool activeOnly = true);
        Task<int> GetCountAsync(string? keyword = null, bool activeOnly = true);
        Task<MaterialCategory> AddAsync(MaterialCategory category);
        Task<MaterialCategory> UpdateAsync(MaterialCategory category);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsCodeAsync(string code, int? excludeId = null);
    }

    /// <summary>
    /// 物料仓储接口
    /// </summary>
    public interface IMaterialRepository
    {
        Task<Material?> GetByIdAsync(int id);
        Task<Material?> GetByCodeAsync(string code);
        Task<IEnumerable<Material>> GetAllAsync(bool activeOnly = true);
        Task<IEnumerable<Material>> GetByCategoryAsync(int categoryId, bool activeOnly = true);
        Task<IEnumerable<Material>> GetWeighableMaterialsAsync();
        Task<IEnumerable<Material>> GetPagedAsync(int page, int pageSize, MaterialFilter filter);
        Task<int> GetCountAsync(MaterialFilter filter);
        Task<Material> AddAsync(Material material);
        Task<Material> UpdateAsync(Material material);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsCodeAsync(string code, int? excludeId = null);
        Task<IEnumerable<MaterialProperty>> GetPropertiesAsync(int materialId);
        Task<MaterialProperty> AddPropertyAsync(MaterialProperty property);
        Task<bool> DeletePropertyAsync(int propertyId);
    }

    /// <summary>
    /// 物料查询过滤器
    /// </summary>
    public class MaterialFilter
    {
        public string? Keyword { get; set; }
        public int? CategoryId { get; set; }
        public MaterialType? MaterialType { get; set; }
        public MaterialStatus? Status { get; set; }
        public bool? IsWeighable { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public string? SortBy { get; set; } = "Code";
        public bool SortDescending { get; set; } = false;
    }

    /// <summary>
    /// 罐体物料分配仓储接口
    /// </summary>
    public interface ITankMaterialRepository
    {
        Task<TankMaterial?> GetByIdAsync(int id);
        Task<IEnumerable<TankMaterial>> GetByTankIdAsync(int tankId, bool activeOnly = true);
        Task<IEnumerable<TankMaterial>> GetByMaterialIdAsync(int materialId, bool activeOnly = true);
        Task<TankMaterial?> GetCurrentAssignmentAsync(int tankId);
        Task<IEnumerable<TankMaterial>> GetAllAsync(bool activeOnly = true);
        Task<TankMaterial> AddAsync(TankMaterial tankMaterial);
        Task<TankMaterial> UpdateAsync(TankMaterial tankMaterial);
        Task<bool> DeleteAsync(int id);
        Task<bool> SetPrimaryAsync(int tankId, int materialId);
        Task<IEnumerable<TankMaterialHistory>> GetHistoryAsync(int tankId, DateTime? from = null, DateTime? to = null);
        Task<TankMaterialHistory> AddHistoryAsync(TankMaterialHistory history);
    }

    /// <summary>
    /// 配方仓储接口
    /// </summary>
    public interface IRecipeRepository
    {
        Task<RecipeBase?> GetByIdAsync(int id);
        Task<RecipeBase?> GetByCodeAsync(string code);
        Task<IEnumerable<RecipeBase>> GetAllAsync(RecipeFilter filter);
        Task<IEnumerable<RecipeBase>> GetPagedAsync(int page, int pageSize, RecipeFilter filter);
        Task<int> GetCountAsync(RecipeFilter filter);
        Task<WeighingRecipe?> GetWeighingRecipeAsync(int id);
        Task<MixingRecipe?> GetMixingRecipeAsync(int id);
        Task<RecipeBase> AddAsync(RecipeBase recipe);
        Task<RecipeBase> UpdateAsync(RecipeBase recipe);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsCodeAsync(string code, int? excludeId = null);
        Task<RecipeVersion> AddVersionAsync(RecipeVersion version);
        Task<bool> SetCurrentVersionAsync(int recipeId, int versionId);
        Task<IEnumerable<RecipeVersion>> GetVersionsAsync(int recipeId);
        
        // 称量配方明细
        Task<IEnumerable<WeighingRecipeLine>> GetWeighingLinesAsync(int recipeId);
        Task<WeighingRecipeLine> AddWeighingLineAsync(WeighingRecipeLine line);
        Task<WeighingRecipeLine> UpdateWeighingLineAsync(WeighingRecipeLine line);
        Task<bool> DeleteWeighingLineAsync(int lineId);
        Task<bool> ReorderWeighingLinesAsync(int recipeId, List<int> lineIdsInOrder);
        
        // 混炼配方步骤
        Task<IEnumerable<MixingRecipeStep>> GetMixingStepsAsync(int recipeId);
        Task<MixingRecipeStep> AddMixingStepAsync(MixingRecipeStep step);
        Task<MixingRecipeStep> UpdateMixingStepAsync(MixingRecipeStep step);
        Task<bool> DeleteMixingStepAsync(int stepId);
        Task<bool> ReorderMixingStepsAsync(int recipeId, List<int> stepIdsInOrder);
    }

    /// <summary>
    /// 配方下发日志仓储接口
    /// </summary>
    public interface IRecipeDownloadLogRepository
    {
        Task<RecipeDownloadLog?> GetByIdAsync(int id);
        Task<IEnumerable<RecipeDownloadLog>> GetByRecipeIdAsync(int recipeId);
        Task<IEnumerable<RecipeDownloadLog>> GetByPlcAsync(string plcName, DateTime? from = null, DateTime? to = null);
        Task<IEnumerable<RecipeDownloadLog>> GetPagedAsync(int page, int pageSize, string? plcName = null, DownloadStatus? status = null);
        Task<int> GetCountAsync(string? plcName = null, DownloadStatus? status = null);
        Task<RecipeDownloadLog> AddAsync(RecipeDownloadLog log);
        Task<RecipeDownloadLog> UpdateAsync(RecipeDownloadLog log);
    }

    /// <summary>
    /// 批次记录仓储接口
    /// </summary>
    public interface IBatchRecordRepository
    {
        Task<BatchRecord?> GetByIdAsync(int id);
        Task<BatchRecord?> GetByBatchNumberAsync(string batchNumber);
        Task<IEnumerable<BatchRecord>> GetPagedAsync(int page, int pageSize, BatchFilter filter);
        Task<int> GetCountAsync(BatchFilter filter);
        Task<BatchRecord> AddAsync(BatchRecord record);
        Task<BatchRecord> UpdateAsync(BatchRecord record);
        Task<string> GenerateBatchNumberAsync(string prefix = "BATCH");
    }

    /// <summary>
    /// 批次查询过滤器
    /// </summary>
    public class BatchFilter
    {
        public string? BatchNumber { get; set; }
        public int? RecipeId { get; set; }
        public BatchStatus? Status { get; set; }
        public DateTime? StartFrom { get; set; }
        public DateTime? StartTo { get; set; }
        public string? Operator { get; set; }
        public string? SortBy { get; set; } = "StartTime";
        public bool SortDescending { get; set; } = true;
    }

    // ===== 报警仓储接口 =====

    public interface IAlarmDefinitionRepository
    {
        Task<AlarmDefinition?> GetByIdAsync(int id);
        Task<IEnumerable<AlarmDefinition>> GetAllAsync(AlarmDefinitionFilter filter);
        Task<int> GetCountAsync(AlarmDefinitionFilter filter);
        Task<AlarmDefinition> AddAsync(AlarmDefinition definition);
        Task<AlarmDefinition> UpdateAsync(AlarmDefinition definition);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<AlarmGroup>> GetGroupsAsync();
        Task<AlarmGroup> AddGroupAsync(AlarmGroup group);
    }

    public interface IAlarmInstanceRepository
    {
        Task<AlarmInstance?> GetByIdAsync(int id);
        Task<IEnumerable<AlarmInstance>> GetActiveAlarmsAsync();
        Task<(IEnumerable<AlarmInstance> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, AlarmFilter filter);
        Task<int> GetCountAsync(AlarmFilter filter);
        Task<AlarmInstance> AddAsync(AlarmInstance instance);
        Task<AlarmInstance> UpdateAsync(AlarmInstance instance);
        Task<bool> AcknowledgeAsync(int id, string ackedBy, string comment);
        Task<bool> ShelveAsync(int id, bool shelve);
        Task<bool> ReturnToNormalAsync(int id, double returnValue);
    }

    public interface IAlarmHistoryRepository
    {
        Task<(IEnumerable<AlarmHistory> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, AlarmHistoryFilter filter);
        Task<int> GetCountAsync(AlarmHistoryFilter filter);
        Task<AlarmHistory> AddAsync(AlarmHistory history);
        Task<Dictionary<AlarmPriority, int>> GetStatisticsAsync(DateTime from, DateTime to);
    }

    public interface IAlarmNotificationRepository
    {
        Task<IEnumerable<AlarmNotification>> GetEnabledAsync();
        Task<AlarmNotification> AddAsync(AlarmNotification notification);
    }

    public interface IAlarmSuppressionRepository
    {
        Task<IEnumerable<AlarmSuppression>> GetActiveAsync();
        Task<AlarmSuppression> AddAsync(AlarmSuppression suppression);
        Task<bool> IsSuppressedAsync(int definitionId, DateTime time);
    }
}