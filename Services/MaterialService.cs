using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Serilog;
using TwoD5DMonitoring.Infrastructure.Logging;
using TwoD5DMonitoring.Models;
using TwoD5DMonitoring.Services.Repositories;

namespace TwoD5DMonitoring.Services
{
    /// <summary>
    /// 物料管理服务
    /// </summary>
    public class MaterialService
    {
        private readonly ILogger _logger;
        private readonly IMaterialCategoryRepository _categoryRepository;
        private readonly IMaterialRepository _materialRepository;
        private readonly ITankMaterialRepository _tankMaterialRepository;

        public MaterialService(
            IMaterialCategoryRepository categoryRepository,
            IMaterialRepository materialRepository,
            ITankMaterialRepository tankMaterialRepository)
        {
            _logger = LogManager.CreateModuleLogger(nameof(MaterialService));
            _categoryRepository = categoryRepository;
            _materialRepository = materialRepository;
            _tankMaterialRepository = tankMaterialRepository;
        }

        #region 物料分类管理

        public async Task<MaterialCategory?> GetCategoryByIdAsync(int id)
        {
            return await _categoryRepository.GetByIdAsync(id);
        }

        public async Task<MaterialCategory?> GetCategoryByCodeAsync(string code)
        {
            return await _categoryRepository.GetByCodeAsync(code);
        }

        public async Task<IEnumerable<MaterialCategory>> GetAllCategoriesAsync(bool activeOnly = true)
        {
            return await _categoryRepository.GetAllAsync(activeOnly);
        }

        public async Task<(IEnumerable<MaterialCategory> Items, int TotalCount)> GetCategoriesPagedAsync(
            int page, int pageSize, string? keyword = null, bool activeOnly = true)
        {
            var items = await _categoryRepository.GetPagedAsync(page, pageSize, keyword, activeOnly);
            var total = await _categoryRepository.GetCountAsync(keyword, activeOnly);
            return (items, total);
        }

        public async Task<MaterialCategory> CreateCategoryAsync(MaterialCategory category, string operatorName)
        {
            if (await _categoryRepository.ExistsCodeAsync(category.Code))
                throw new InvalidOperationException($"分类编码 '{category.Code}' 已存在");

            category.CreatedAt = DateTime.Now;
            category.CreatedBy = operatorName;
            category.UpdatedAt = DateTime.Now;
            category.UpdatedBy = operatorName;

            var result = await _categoryRepository.AddAsync(category);
            
            _logger.Information("创建物料分类: {Code} - {Name}, Operator: {Operator}", category.Code, category.Name, operatorName);
            
            return result;
        }

        public async Task<MaterialCategory> UpdateCategoryAsync(MaterialCategory category, string operatorName)
        {
            if (await _categoryRepository.ExistsCodeAsync(category.Code, category.Id))
                throw new InvalidOperationException($"分类编码 '{category.Code}' 已存在");

            category.UpdatedAt = DateTime.Now;
            category.UpdatedBy = operatorName;

            var result = await _categoryRepository.UpdateAsync(category);
            
            _logger.Information("更新物料分类: {Code} - {Name}, Operator: {Operator}", category.Code, category.Name, operatorName);
            
            return result;
        }

        public async Task<bool> DeleteCategoryAsync(int id, string operatorName)
        {
            // 检查是否有关联物料
            var materials = await _materialRepository.GetByCategoryAsync(id, false);
            if (materials.Any())
                throw new InvalidOperationException("该分类下存在物料，无法删除");

            var result = await _categoryRepository.DeleteAsync(id);
            
            if (result)
                _logger.Information("删除物料分类: Id={Id}, Operator: {Operator}", id, operatorName);
            
            return result;
        }

        #endregion

        #region 物料管理

        public async Task<Material?> GetMaterialByIdAsync(int id)
        {
            return await _materialRepository.GetByIdAsync(id);
        }

        public async Task<Material?> GetMaterialByCodeAsync(string code)
        {
            return await _materialRepository.GetByCodeAsync(code);
        }

        public async Task<IEnumerable<Material>> GetAllMaterialsAsync(bool activeOnly = true)
        {
            return await _materialRepository.GetAllAsync(activeOnly);
        }

        public async Task<IEnumerable<Material>> GetWeighableMaterialsAsync()
        {
            return await _materialRepository.GetWeighableMaterialsAsync();
        }

        public async Task<IEnumerable<Material>> GetMaterialsByCategoryAsync(int categoryId, bool activeOnly = true)
        {
            return await _materialRepository.GetByCategoryAsync(categoryId, activeOnly);
        }

        public async Task<(IEnumerable<Material> Items, int TotalCount)> GetMaterialsPagedAsync(
            int page, int pageSize, MaterialFilter filter)
        {
            var items = await _materialRepository.GetPagedAsync(page, pageSize, filter);
            var total = await _materialRepository.GetCountAsync(filter);
            return (items, total);
        }

        public async Task<Material> CreateMaterialAsync(Material material, string operatorName)
        {
            if (await _materialRepository.ExistsCodeAsync(material.Code))
                throw new InvalidOperationException($"物料编码 '{material.Code}' 已存在");

            material.CreatedAt = DateTime.Now;
            material.CreatedBy = operatorName;
            material.UpdatedAt = DateTime.Now;
            material.UpdatedBy = operatorName;

            var result = await _materialRepository.AddAsync(material);
            
            _logger.Information("创建物料: {Code} - {Name}, Type: {Type}, Operator: {Operator}", 
                material.Code, material.Name, material.MaterialType, operatorName);
            
            return result;
        }

        public async Task<Material> UpdateMaterialAsync(Material material, string operatorName)
        {
            if (await _materialRepository.ExistsCodeAsync(material.Code, material.Id))
                throw new InvalidOperationException($"物料编码 '{material.Code}' 已存在");

            material.UpdatedAt = DateTime.Now;
            material.UpdatedBy = operatorName;

            var result = await _materialRepository.UpdateAsync(material);
            
            _logger.Information("更新物料: {Code} - {Name}, Operator: {Operator}", 
                material.Code, material.Name, operatorName);
            
            return result;
        }

        public async Task<bool> DeleteMaterialAsync(int id, string operatorName)
        {
            // 检查是否被配方引用
            // 这里需要配方仓储支持，暂不检查
            
            var result = await _materialRepository.DeleteAsync(id);
            
            if (result)
                _logger.Information("删除物料: Id={Id}, Operator: {Operator}", id, operatorName);
            
            return result;
        }

        public async Task<MaterialProperty> AddMaterialPropertyAsync(int materialId, MaterialProperty property, string operatorName)
        {
            property.MaterialId = materialId;
            return await _materialRepository.AddPropertyAsync(property);
        }

        public async Task<bool> DeleteMaterialPropertyAsync(int propertyId, string operatorName)
        {
            return await _materialRepository.DeletePropertyAsync(propertyId);
        }

        #endregion

        #region 罐体物料分配

        public async Task<IEnumerable<TankMaterial>> GetTankMaterialsAsync(int tankId, bool activeOnly = true)
        {
            return await _tankMaterialRepository.GetByTankIdAsync(tankId, activeOnly);
        }

        public async Task<TankMaterial?> GetCurrentTankMaterialAsync(int tankId)
        {
            return await _tankMaterialRepository.GetCurrentAssignmentAsync(tankId);
        }

        public async Task<IEnumerable<TankMaterial>> GetMaterialTanksAsync(int materialId, bool activeOnly = true)
        {
            return await _tankMaterialRepository.GetByMaterialIdAsync(materialId, activeOnly);
        }

        public async Task<TankMaterial> AssignMaterialToTankAsync(TankMaterial assignment, string operatorName)
        {
            assignment.CreatedAt = DateTime.Now;
            assignment.CreatedBy = operatorName;
            assignment.UpdatedAt = DateTime.Now;
            assignment.UpdatedBy = operatorName;

            var result = await _tankMaterialRepository.AddAsync(assignment);

            // 记录历史
            var history = new TankMaterialHistory
            {
                TankId = assignment.TankId,
                TankCode = assignment.TankCode,
                TankName = assignment.TankName,
                OldMaterialId = 0,
                NewMaterialId = assignment.MaterialId,
                ChangeReason = "新增分配",
                Operator = operatorName,
                ChangeTime = DateTime.Now
            };
            await _tankMaterialRepository.AddHistoryAsync(history);

            _logger.Information("罐体分配物料: TankId={TankId}, MaterialId={MaterialId}, Operator: {Operator}",
                assignment.TankId, assignment.MaterialId, operatorName);

            return result;
        }

        public async Task<TankMaterial> UpdateTankMaterialAsync(TankMaterial assignment, string operatorName)
        {
            assignment.UpdatedAt = DateTime.Now;
            assignment.UpdatedBy = operatorName;

            var result = await _tankMaterialRepository.UpdateAsync(assignment);
            
            _logger.Information("更新罐体物料分配: TankId={TankId}, MaterialId={MaterialId}, Operator: {Operator}",
                assignment.TankId, assignment.MaterialId, operatorName);
            
            return result;
        }

        public async Task<bool> ChangeTankMaterialAsync(int tankId, int newMaterialId, string reason, string operatorName)
        {
            var current = await _tankMaterialRepository.GetCurrentAssignmentAsync(tankId);
            var oldMaterialId = current?.MaterialId ?? 0;

            if (oldMaterialId == newMaterialId)
                return true; // 无变化

            // 取消当前主物料
            if (current != null)
            {
                current.IsPrimary = false;
                await _tankMaterialRepository.UpdateAsync(current);
            }

            // 设置新主物料
            await _tankMaterialRepository.SetPrimaryAsync(tankId, newMaterialId);

            // 记录历史
            var history = new TankMaterialHistory
            {
                TankId = tankId,
                TankCode = current?.TankCode,
                TankName = current?.TankName,
                OldMaterialId = oldMaterialId,
                NewMaterialId = newMaterialId,
                ChangeReason = reason,
                Operator = operatorName,
                ChangeTime = DateTime.Now
            };
            await _tankMaterialRepository.AddHistoryAsync(history);

            _logger.Information("变更罐体物料: TankId={TankId}, OldMaterialId={OldMaterialId}, NewMaterialId={NewMaterialId}, Reason: {Reason}, Operator: {Operator}",
                tankId, oldMaterialId, newMaterialId, reason, operatorName);

            return true;
        }

        public async Task<IEnumerable<TankMaterialHistory>> GetTankMaterialHistoryAsync(int tankId, DateTime? from = null, DateTime? to = null)
        {
            return await _tankMaterialRepository.GetHistoryAsync(tankId, from, to);
        }

        #endregion

        public async Task<IEnumerable<TankMaterial>> GetAllTankMaterialsAsync()
        {
            return await _tankMaterialRepository.GetAllAsync(true);
        }

        public async Task AddTankMaterialHistoryAsync(TankMaterialHistory history)
        {
            await _tankMaterialRepository.AddHistoryAsync(history);
        }

        public async Task<IEnumerable<MaterialProperty>> GetMaterialPropertiesAsync(int materialId)
        {
            return await _materialRepository.GetPropertiesAsync(materialId);
        }

        public async Task SetPrimaryAsync(int tankId, int materialId)
        {
            // 取消该罐体其他主物料
            var allMaterials = await _tankMaterialRepository.GetByTankIdAsync(tankId);
            foreach (var m in allMaterials.Where(x => x.IsPrimary && x.MaterialId != materialId))
            {
                m.IsPrimary = false;
                await _tankMaterialRepository.UpdateAsync(m);
            }
            
            // 设置新的主物料
            var primary = allMaterials.FirstOrDefault(x => x.MaterialId == materialId);
            if (primary != null)
            {
                primary.IsPrimary = true;
                await _tankMaterialRepository.UpdateAsync(primary);
            }
        }

        #region 导入导出

        public async Task<byte[]> ExportMaterialsToExcelAsync(MaterialFilter? filter = null)
        {
            var items = await _materialRepository.GetPagedAsync(1, int.MaxValue, filter ?? new MaterialFilter());
            var materials = items.ToList();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("物料清单");

            // 表头
            var headers = new[] { "编码", "名称", "规格", "单位", "类型", "状态", "分类", "密度", "水分", "颗粒度", "颜色", "厂家", "供应商", "标准成本", "备注", "可称量", "批次管理", "保质期", "存储条件", "创建时间", "创建人", "更新时间", "更新人" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            // 数据行
            for (int i = 0; i < materials.Count; i++)
            {
                var m = materials[i];
                var row = i + 2;
                worksheet.Cell(row, 1).Value = m.Code;
                worksheet.Cell(row, 2).Value = m.Name;
                worksheet.Cell(row, 3).Value = m.Specification;
                worksheet.Cell(row, 4).Value = m.Unit;
                worksheet.Cell(row, 5).Value = m.MaterialType.ToString();
                worksheet.Cell(row, 6).Value = m.Status.ToString();
                worksheet.Cell(row, 7).Value = m.CategoryName ?? "";
                worksheet.Cell(row, 7).Value = m.Density;
                worksheet.Cell(row, 8).Value = m.MoistureContent;
                worksheet.Cell(row, 9).Value = m.ParticleSize;
                worksheet.Cell(row, 10).Value = m.Color;
                worksheet.Cell(row, 11).Value = m.Manufacturer;
                worksheet.Cell(row, 12).Value = m.Supplier;
                worksheet.Cell(row, 13).Value = m.StandardCost;
                worksheet.Cell(row, 14).Value = m.Remark;
                worksheet.Cell(row, 15).Value = m.IsWeighable ? "是" : "否";
                worksheet.Cell(row, 16).Value = m.IsBatchManaged ? "是" : "否";
                worksheet.Cell(row, 17).Value = m.ShelfLifeDays;
                worksheet.Cell(row, 18).Value = m.StorageCondition;
                worksheet.Cell(row, 19).Value = m.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cell(row, 20).Value = m.CreatedBy;
                worksheet.Cell(row, 21).Value = m.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cell(row, 22).Value = m.UpdatedBy;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<ImportResult> ImportMaterialsFromExcelAsync(Stream excelStream, string operatorName)
        {
            var result = new ImportResult { Success = false };

            try
            {
                using var workbook = new XLWorkbook(excelStream);
                var worksheet = workbook.Worksheet(1);
                var rows = worksheet.RowsUsed().Skip(1).ToList(); // 跳过表头

                var materialsToAdd = new List<Material>();
                var errors = new List<string>();
                var existingCodes = new HashSet<string>(
                    (await _materialRepository.GetAllAsync(false)).Select(m => m.Code),
                    StringComparer.OrdinalIgnoreCase);

                var categories = await _categoryRepository.GetAllAsync(true);
                var categoryDict = categories.ToDictionary(c => c.Code, c => c.Id, StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < rows.Count; i++)
                {
                    var row = rows[i];
                    var rowNum = i + 2; // Excel行号（含表头）

                    try
                    {
                        var code = row.Cell(1).GetString().Trim();
                        var name = row.Cell(2).GetString().Trim();

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

                        var material = new Material
                        {
                            Code = code,
                            Name = name,
                            Specification = row.Cell(3).GetString().Trim(),
                            Unit = row.Cell(4).GetString().Trim(),
                            MaterialType = Enum.TryParse<MaterialType>(row.Cell(5).GetString().Trim(), out var mt) ? mt : MaterialType.RawMaterial,
                            Status = Enum.TryParse<MaterialStatus>(row.Cell(6).GetString().Trim(), out var ms) ? ms : MaterialStatus.Active,
                            CategoryId = categoryDict.TryGetValue(row.Cell(7).GetString().Trim(), out var catId) ? catId : 0,
                            Density = decimal.TryParse(row.Cell(7).GetString(), out var d) ? d : 0,
                            MoistureContent = decimal.TryParse(row.Cell(8).GetString(), out var mc) ? mc : 0,
                            ParticleSize = row.Cell(9).GetString().Trim(),
                            Color = row.Cell(10).GetString().Trim(),
                            Manufacturer = row.Cell(11).GetString().Trim(),
                            Supplier = row.Cell(12).GetString().Trim(),
                            StandardCost = decimal.TryParse(row.Cell(13).GetString(), out var sc) ? sc : 0,
                            Remark = row.Cell(14).GetString().Trim(),
                            IsWeighable = row.Cell(15).GetString().Trim().Equals("是", StringComparison.OrdinalIgnoreCase),
                            IsBatchManaged = row.Cell(16).GetString().Trim().Equals("是", StringComparison.OrdinalIgnoreCase),
                            ShelfLifeDays = int.TryParse(row.Cell(17).GetString(), out var sl) ? sl : null,
                            StorageCondition = row.Cell(18).GetString().Trim(),
                            CreatedAt = DateTime.Now,
                            CreatedBy = operatorName,
                            UpdatedAt = DateTime.Now,
                            UpdatedBy = operatorName
                        };

                        materialsToAdd.Add(material);
                        existingCodes.Add(code);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"第 {rowNum} 行: {ex.Message}");
                    }
                }

                if (materialsToAdd.Count > 0)
                {
                    foreach (var m in materialsToAdd)
                    {
                        await _materialRepository.AddAsync(m);
                    }
                    result.Success = true;
                    result.ImportedCount = materialsToAdd.Count;
                }

                result.Errors = errors;
                result.Message = $"成功导入 {result.ImportedCount} 条，失败 {errors.Count} 条";

                _logger.Information("Excel导入物料: 成功 {Count} 条, 失败 {ErrorCount} 条, Operator: {Operator}",
                    result.ImportedCount, errors.Count, operatorName);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"导入失败: {ex.Message}";
                _logger.Error(ex, "Excel导入物料失败, Operator: {Operator}", operatorName);
            }

            return result;
        }

        public async Task<byte[]> ExportTankMaterialsToExcelAsync()
        {
            var tankMaterials = await _tankMaterialRepository.GetAllAsync(true);
            
            // 批量预加载物料信息以避免 N+1 查询
            var materialIds = tankMaterials.Select(tm => tm.MaterialId).Distinct().ToList();
            var materials = new Dictionary<int, Material>();
            foreach (var id in materialIds)
            {
                var m = await _materialRepository.GetByIdAsync(id);
                if (m != null) materials[id] = m;
            }

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("罐体物料分配");

            var headers = new[] { "罐体ID", "罐体编码", "罐体名称", "罐体类型", "物料编码", "物料名称", "是否主物料", "优先级", "最大容量", "最低液位", "最高液位", "备注", "生效时间", "失效时间", "是否激活", "创建时间", "创建人", "更新时间", "更新人" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            var items = tankMaterials.ToList();
            for (int i = 0; i < items.Count; i++)
            {
                var tm = items[i];
                var row = i + 2;
                worksheet.Cell(row, 1).Value = tm.TankId;
                worksheet.Cell(row, 2).Value = tm.TankCode;
                worksheet.Cell(row, 3).Value = tm.TankName;
                worksheet.Cell(row, 4).Value = tm.TankType;
                
                if (materials.TryGetValue(tm.MaterialId, out var material))
                {
                    worksheet.Cell(row, 5).Value = material.Code ?? "";
                    worksheet.Cell(row, 6).Value = material.Name ?? "";
                }
                else
                {
                    worksheet.Cell(row, 5).Value = tm.MaterialId.ToString();
                    worksheet.Cell(row, 6).Value = "";
                }
                
                worksheet.Cell(row, 7).Value = tm.IsPrimary ? "是" : "否";
                worksheet.Cell(row, 8).Value = tm.Priority;
                worksheet.Cell(row, 9).Value = tm.MaxCapacity;
                worksheet.Cell(row, 10).Value = tm.MinLevel;
                worksheet.Cell(row, 11).Value = tm.MaxLevel;
                worksheet.Cell(row, 12).Value = tm.Remark;
                worksheet.Cell(row, 12).Value = tm.EffectiveFrom.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cell(row, 13).Value = tm.EffectiveTo?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                worksheet.Cell(row, 14).Value = tm.IsActive ? "是" : "否";
                worksheet.Cell(row, 15).Value = tm.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cell(row, 16).Value = tm.CreatedBy;
                worksheet.Cell(row, 17).Value = tm.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cell(row, 18).Value = tm.UpdatedBy;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        #endregion

        #region 批量操作

        public async Task<BatchOperationResult> BatchUpdateMaterialsAsync(List<Material> materials, string operatorName)
        {
            var result = new BatchOperationResult();

            foreach (var material in materials)
            {
                try
                {
                    material.UpdatedAt = DateTime.Now;
                    material.UpdatedBy = operatorName;
                    await _materialRepository.UpdateAsync(material);
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.FailCount++;
                    result.Errors.Add($"物料 {material.Code}: {ex.Message}");
                    _logger.Error(ex, "批量更新物料失败: {Code}", material.Code);
                }
            }

            result.Success = result.FailCount == 0;
            result.Message = $"成功 {result.SuccessCount} 条，失败 {result.FailCount} 条";
            return result;
        }

        public async Task<BatchOperationResult> BatchDeleteMaterialsAsync(List<int> ids, string operatorName)
        {
            var batchResult = new BatchOperationResult();

            foreach (var id in ids)
            {
                try
                {
                    var deleteResult = await _materialRepository.DeleteAsync(id);
                    if (deleteResult) batchResult.SuccessCount++;
                    else batchResult.FailCount++;
                }
                catch (Exception ex)
                {
                    batchResult.FailCount++;
                    batchResult.Errors.Add($"ID {id}: {ex.Message}");
                }
            }

            batchResult.Success = batchResult.FailCount == 0;
            batchResult.Message = $"成功 {batchResult.SuccessCount} 条，失败 {batchResult.FailCount} 条";
            return batchResult;
        }

        public async Task<BatchOperationResult> BatchAssignTankMaterialsAsync(List<TankMaterial> assignments, string operatorName)
        {
            var result = new BatchOperationResult();

            foreach (var assignment in assignments)
            {
                try
                {
                    assignment.CreatedAt = DateTime.Now;
                    assignment.CreatedBy = operatorName;
                    assignment.UpdatedAt = DateTime.Now;
                    assignment.UpdatedBy = operatorName;

                    await _tankMaterialRepository.AddAsync(assignment);

                    // 记录历史
                    var history = new TankMaterialHistory
                    {
                        TankId = assignment.TankId,
                        TankCode = assignment.TankCode,
                        TankName = assignment.TankName,
                        OldMaterialId = 0,
                        NewMaterialId = assignment.MaterialId,
                        ChangeReason = "批量分配",
                        Operator = operatorName,
                        ChangeTime = DateTime.Now
                    };
                    await _tankMaterialRepository.AddHistoryAsync(history);

                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.FailCount++;
                    result.Errors.Add($"罐体 {assignment.TankCode}: {ex.Message}");
                }
            }

            result.Success = result.FailCount == 0;
            result.Message = $"成功 {result.SuccessCount} 条，失败 {result.FailCount} 条";
            return result;
        }

        #endregion

        #region 库存预警

        public async Task<List<InventoryAlert>> CheckInventoryAlertsAsync()
        {
            var alerts = new List<InventoryAlert>();

            // 低库存预警（这里简化：实际需要结合库存表）
            // 假设有一个库存表或通过其他方式获取当前库存
            // 这里仅演示框架

            // 过期预警（基于保质期）
            var materials = await _materialRepository.GetAllAsync(true);
            foreach (var m in materials.Where(m => m.ShelfLifeDays.HasValue))
            {
                // 实际需要结合入库时间和当前库存
                // 这里仅作演示
            }

            // 罐体液位预警
            var tankMaterials = await _tankMaterialRepository.GetAllAsync(true);
            foreach (var tm in tankMaterials.Where(tm => tm.IsActive && tm.IsPrimary))
            {
                // 实际需要实时读取PLC液位
                // 这里仅作演示框架
            }

            return alerts;
        }

        public async Task<Dictionary<int, decimal>> GetMaterialConsumptionAsync(int materialId, DateTime from, DateTime to)
        {
            // 从批次记录中统计物料消耗
            // 实际需要结合批次记录和称重明细
            return new Dictionary<int, decimal>();
        }

        #endregion
    }
}