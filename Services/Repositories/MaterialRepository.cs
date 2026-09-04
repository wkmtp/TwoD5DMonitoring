using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using TwoD5DMonitoring.Infrastructure.Database;
using TwoD5DMonitoring.Models;
using TwoD5DMonitoring.Services.Repositories;

namespace TwoD5DMonitoring.Services.Repositories
{
    /// <summary>
    /// 物料分类仓储实现
    /// </summary>
    public class MaterialCategoryRepository : IMaterialCategoryRepository
    {
        private readonly IDatabaseProvider _db;

        public MaterialCategoryRepository(IDatabaseProvider db)
        {
            _db = db;
        }

        public async Task<MaterialCategory?> GetByIdAsync(int id)
        {
            const string sql = "SELECT * FROM MaterialCategories WHERE Id = @Id";
            return await _db.QuerySingleAsync<MaterialCategory>(sql, new { Id = id });
        }

        public async Task<MaterialCategory?> GetByCodeAsync(string code)
        {
            const string sql = "SELECT * FROM MaterialCategories WHERE Code = @Code";
            return await _db.QuerySingleAsync<MaterialCategory>(sql, new { Code = code });
        }

        public async Task<IEnumerable<MaterialCategory>> GetAllAsync(bool activeOnly = true)
        {
            var sql = "SELECT * FROM MaterialCategories";
            if (activeOnly) sql += " WHERE IsActive = 1";
            sql += " ORDER BY SortOrder, Code";
            return await _db.QueryAsync<MaterialCategory>(sql);
        }

        public async Task<IEnumerable<MaterialCategory>> GetPagedAsync(int page, int pageSize, string? keyword = null, bool activeOnly = true)
        {
            var where = new List<string>();
            var parameters = new DynamicParameters();

            if (activeOnly) where.Add("IsActive = 1");
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                where.Add("(Code LIKE @Keyword OR Name LIKE @Keyword)");
                parameters.Add("@Keyword", $"%{keyword}%");
            }

            var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";
            var sql = $@"
                SELECT * FROM MaterialCategories 
                {whereClause}
                ORDER BY SortOrder, Code
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            parameters.Add("@Offset", (page - 1) * pageSize);
            parameters.Add("@PageSize", pageSize);

            return await _db.QueryAsync<MaterialCategory>(sql, parameters);
        }

        public async Task<int> GetCountAsync(string? keyword = null, bool activeOnly = true)
        {
            var where = new List<string>();
            var parameters = new DynamicParameters();

            if (activeOnly) where.Add("IsActive = 1");
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                where.Add("(Code LIKE @Keyword OR Name LIKE @Keyword)");
                parameters.Add("@Keyword", $"%{keyword}%");
            }

            var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";
            var sql = $"SELECT COUNT(*) FROM MaterialCategories {whereClause}";
            return await _db.ExecuteScalarAsync<int>(sql, parameters);
        }

        public async Task<MaterialCategory> AddAsync(MaterialCategory category)
        {
            const string sql = @"
                INSERT INTO MaterialCategories (Code, Name, Description, SortOrder, IsActive, CreatedAt, UpdatedAt)
                VALUES (@Code, @Name, @Description, @SortOrder, @IsActive, @CreatedAt, @UpdatedAt);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            var id = await _db.ExecuteScalarAsync<int>(sql, new
            {
                category.Code,
                category.Name,
                category.Description,
                category.SortOrder,
                category.IsActive,
                category.CreatedAt,
                category.UpdatedAt
            });

            category.Id = id;
            return category;
        }

        public async Task<MaterialCategory> UpdateAsync(MaterialCategory category)
        {
            const string sql = @"
                UPDATE MaterialCategories 
                SET Code = @Code, Name = @Name, Description = @Description, 
                    SortOrder = @SortOrder, IsActive = @IsActive, UpdatedAt = @UpdatedAt
                WHERE Id = @Id";

            await _db.ExecuteAsync(sql, new
            {
                category.Id,
                category.Code,
                category.Name,
                category.Description,
                category.SortOrder,
                category.IsActive,
                UpdatedAt = DateTime.Now
            });

            return category;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "DELETE FROM MaterialCategories WHERE Id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            return affected > 0;
        }

        public async Task<bool> ExistsCodeAsync(string code, int? excludeId = null)
        {
            var sql = "SELECT COUNT(*) FROM MaterialCategories WHERE Code = @Code";
            var parameters = new DynamicParameters();
            parameters.Add("@Code", code);

            if (excludeId.HasValue)
            {
                sql += " AND Id != @ExcludeId";
                parameters.Add("@ExcludeId", excludeId.Value);
            }

            var count = await _db.ExecuteScalarAsync<int>(sql, parameters);
            return count > 0;
        }
    }

    /// <summary>
    /// 物料仓储实现
    /// </summary>
    public class MaterialRepository : IMaterialRepository
    {
        private readonly IDatabaseProvider _db;

        public MaterialRepository(IDatabaseProvider db)
        {
            _db = db;
        }

        public async Task<Material?> GetByIdAsync(int id)
        {
            const string sql = @"
                SELECT m.*, c.Name as CategoryName 
                FROM Materials m
                LEFT JOIN MaterialCategories c ON m.CategoryId = c.Id
                WHERE m.Id = @Id";
            return await _db.QuerySingleAsync<Material>(sql, new { Id = id });
        }

        public async Task<Material?> GetByCodeAsync(string code)
        {
            const string sql = @"
                SELECT m.*, c.Name as CategoryName 
                FROM Materials m
                LEFT JOIN MaterialCategories c ON m.CategoryId = c.Id
                WHERE m.Code = @Code";
            return await _db.QuerySingleAsync<Material>(sql, new { Code = code });
        }

        public async Task<IEnumerable<Material>> GetAllAsync(bool activeOnly = true)
        {
            var sql = @"
                SELECT m.*, c.Name as CategoryName 
                FROM Materials m
                LEFT JOIN MaterialCategories c ON m.CategoryId = c.Id";
            if (activeOnly) sql += " WHERE m.Status = 1";
            sql += " ORDER BY m.Code";
            return await _db.QueryAsync<Material>(sql);
        }

        public async Task<IEnumerable<Material>> GetByCategoryAsync(int categoryId, bool activeOnly = true)
        {
            var sql = @"
                SELECT m.*, c.Name as CategoryName 
                FROM Materials m
                LEFT JOIN MaterialCategories c ON m.CategoryId = c.Id
                WHERE m.CategoryId = @CategoryId";
            if (activeOnly) sql += " AND m.Status = 1";
            sql += " ORDER BY m.Code";
            return await _db.QueryAsync<Material>(sql, new { CategoryId = categoryId });
        }

        public async Task<IEnumerable<Material>> GetWeighableMaterialsAsync()
        {
            const string sql = @"
                SELECT m.*, c.Name as CategoryName 
                FROM Materials m
                LEFT JOIN MaterialCategories c ON m.CategoryId = c.Id
                WHERE m.Status = 1 AND m.IsWeighable = 1
                ORDER BY m.Code";
            return await _db.QueryAsync<Material>(sql);
        }

        public async Task<IEnumerable<Material>> GetPagedAsync(int page, int pageSize, MaterialFilter filter)
        {
            var where = new List<string> { "m.Status != 3" }; // 排除废弃
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                where.Add("(m.Code LIKE @Keyword OR m.Name LIKE @Keyword OR m.Specification LIKE @Keyword)");
                parameters.Add("@Keyword", $"%{filter.Keyword}%");
            }

            if (filter.CategoryId.HasValue)
            {
                where.Add("m.CategoryId = @CategoryId");
                parameters.Add("@CategoryId", filter.CategoryId.Value);
            }

            if (filter.MaterialType.HasValue)
            {
                where.Add("m.MaterialType = @MaterialType");
                parameters.Add("@MaterialType", (int)filter.MaterialType.Value);
            }

            if (filter.Status.HasValue)
            {
                where.Add("m.Status = @Status");
                parameters.Add("@Status", (int)filter.Status.Value);
            }

            if (filter.IsWeighable.HasValue)
            {
                where.Add("m.IsWeighable = @IsWeighable");
                parameters.Add("@IsWeighable", filter.IsWeighable.Value);
            }

            if (filter.CreatedFrom.HasValue)
            {
                where.Add("m.CreatedAt >= @CreatedFrom");
                parameters.Add("@CreatedFrom", filter.CreatedFrom.Value);
            }

            if (filter.CreatedTo.HasValue)
            {
                where.Add("m.CreatedAt <= @CreatedTo");
                parameters.Add("@CreatedTo", filter.CreatedTo.Value);
            }

            var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";
            var orderBy = !string.IsNullOrWhiteSpace(filter.SortBy) ? filter.SortBy : "m.Code";
            var orderDir = filter.SortDescending ? "DESC" : "ASC";

            var sql = $@"
                SELECT m.*, c.Name as CategoryName 
                FROM Materials m
                LEFT JOIN MaterialCategories c ON m.CategoryId = c.Id
                {whereClause}
                ORDER BY {orderBy} {orderDir}
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            parameters.Add("@Offset", (page - 1) * pageSize);
            parameters.Add("@PageSize", pageSize);

            return await _db.QueryAsync<Material>(sql, parameters);
        }

        public async Task<int> GetCountAsync(MaterialFilter filter)
        {
            var where = new List<string> { "m.Status != 3" };
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                where.Add("(m.Code LIKE @Keyword OR m.Name LIKE @Keyword OR m.Specification LIKE @Keyword)");
                parameters.Add("@Keyword", $"%{filter.Keyword}%");
            }

            if (filter.CategoryId.HasValue)
            {
                where.Add("m.CategoryId = @CategoryId");
                parameters.Add("@CategoryId", filter.CategoryId.Value);
            }

            if (filter.MaterialType.HasValue)
            {
                where.Add("m.MaterialType = @MaterialType");
                parameters.Add("@MaterialType", (int)filter.MaterialType.Value);
            }

            if (filter.Status.HasValue)
            {
                where.Add("m.Status = @Status");
                parameters.Add("@Status", (int)filter.Status.Value);
            }

            if (filter.IsWeighable.HasValue)
            {
                where.Add("m.IsWeighable = @IsWeighable");
                parameters.Add("@IsWeighable", filter.IsWeighable.Value);
            }

            if (filter.CreatedFrom.HasValue)
            {
                where.Add("m.CreatedAt >= @CreatedFrom");
                parameters.Add("@CreatedFrom", filter.CreatedFrom.Value);
            }

            if (filter.CreatedTo.HasValue)
            {
                where.Add("m.CreatedAt <= @CreatedTo");
                parameters.Add("@CreatedTo", filter.CreatedTo.Value);
            }

            var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";
            var sql = $"SELECT COUNT(*) FROM Materials m {whereClause}";
            return await _db.ExecuteScalarAsync<int>(sql, parameters);
        }

        public async Task<Material> AddAsync(Material material)
        {
            const string sql = @"
                INSERT INTO Materials (Code, Name, Specification, Unit, MaterialType, Status, CategoryId,
                    Density, MoistureContent, ParticleSize, Color, Manufacturer, Supplier, StandardCost,
                    Remark, IsWeighable, IsBatchManaged, ShelfLifeDays, StorageCondition,
                    CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
                VALUES (@Code, @Name, @Specification, @Unit, @MaterialType, @Status, @CategoryId,
                    @Density, @MoistureContent, @ParticleSize, @Color, @Manufacturer, @Supplier, @StandardCost,
                    @Remark, @IsWeighable, @IsBatchManaged, @ShelfLifeDays, @StorageCondition,
                    @CreatedAt, @CreatedBy, @UpdatedAt, @UpdatedBy);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            var id = await _db.ExecuteScalarAsync<int>(sql, new
            {
                material.Code,
                material.Name,
                material.Specification,
                material.Unit,
                material.MaterialType,
                material.Status,
                material.CategoryId,
                material.Density,
                material.MoistureContent,
                material.ParticleSize,
                material.Color,
                material.Manufacturer,
                material.Supplier,
                material.StandardCost,
                material.Remark,
                material.IsWeighable,
                material.IsBatchManaged,
                material.ShelfLifeDays,
                material.StorageCondition,
                material.CreatedAt,
                material.CreatedBy,
                material.UpdatedAt,
                material.UpdatedBy
            });

            material.Id = id;
            return material;
        }

        public async Task<Material> UpdateAsync(Material material)
        {
            const string sql = @"
                UPDATE Materials 
                SET Code = @Code, Name = @Name, Specification = @Specification, Unit = @Unit,
                    MaterialType = @MaterialType, Status = @Status, CategoryId = @CategoryId,
                    Density = @Density, MoistureContent = @MoistureContent, ParticleSize = @ParticleSize,
                    Color = @Color, Manufacturer = @Manufacturer, Supplier = @Supplier,
                    StandardCost = @StandardCost, Remark = @Remark,
                    IsWeighable = @IsWeighable, IsBatchManaged = @IsBatchManaged,
                    ShelfLifeDays = @ShelfLifeDays, StorageCondition = @StorageCondition,
                    UpdatedAt = @UpdatedAt, UpdatedBy = @UpdatedBy
                WHERE Id = @Id";

            await _db.ExecuteAsync(sql, new
            {
                material.Id,
                material.Code,
                material.Name,
                material.Specification,
                material.Unit,
                material.MaterialType,
                material.Status,
                material.CategoryId,
                material.Density,
                material.MoistureContent,
                material.ParticleSize,
                material.Color,
                material.Manufacturer,
                material.Supplier,
                material.StandardCost,
                material.Remark,
                material.IsWeighable,
                material.IsBatchManaged,
                material.ShelfLifeDays,
                material.StorageCondition,
                UpdatedAt = DateTime.Now,
                material.UpdatedBy
            });

            return material;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "DELETE FROM Materials WHERE Id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            return affected > 0;
        }

        public async Task<bool> ExistsCodeAsync(string code, int? excludeId = null)
        {
            var sql = "SELECT COUNT(*) FROM Materials WHERE Code = @Code";
            var parameters = new DynamicParameters();
            parameters.Add("@Code", code);

            if (excludeId.HasValue)
            {
                sql += " AND Id != @ExcludeId";
                parameters.Add("@ExcludeId", excludeId.Value);
            }

            var count = await _db.ExecuteScalarAsync<int>(sql, parameters);
            return count > 0;
        }

        public async Task<IEnumerable<MaterialProperty>> GetPropertiesAsync(int materialId)
        {
            const string sql = "SELECT * FROM MaterialProperties WHERE MaterialId = @MaterialId ORDER BY SortOrder";
            return await _db.QueryAsync<MaterialProperty>(sql, new { MaterialId = materialId });
        }

        public async Task<MaterialProperty> AddPropertyAsync(MaterialProperty property)
        {
            const string sql = @"
                INSERT INTO MaterialProperties (MaterialId, Name, Value, Unit, DataType, SortOrder, IsRequired, ValidationRule)
                VALUES (@MaterialId, @Name, @Value, @Unit, @DataType, @SortOrder, @IsRequired, @ValidationRule);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            var id = await _db.ExecuteScalarAsync<int>(sql, new
            {
                property.MaterialId,
                property.Name,
                property.Value,
                property.Unit,
                property.DataType,
                property.SortOrder,
                property.IsRequired,
                property.ValidationRule
            });

            property.Id = id;
            return property;
        }

        public async Task<bool> DeletePropertyAsync(int propertyId)
        {
            const string sql = "DELETE FROM MaterialProperties WHERE Id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = propertyId });
            return affected > 0;
        }
    }

    /// <summary>
    /// 罐体物料分配仓储实现
    /// </summary>
    public class TankMaterialRepository : ITankMaterialRepository
    {
        private readonly IDatabaseProvider _db;

        public TankMaterialRepository(IDatabaseProvider db)
        {
            _db = db;
        }

        public async Task<TankMaterial?> GetByIdAsync(int id)
        {
            const string sql = @"
                SELECT tm.*, m.Code as MaterialCode, m.Name as MaterialName, m.Unit as MaterialUnit
                FROM TankMaterials tm
                LEFT JOIN Materials m ON tm.MaterialId = m.Id
                WHERE tm.Id = @Id";
            return await _db.QuerySingleAsync<TankMaterial>(sql, new { Id = id });
        }

        public async Task<IEnumerable<TankMaterial>> GetByTankIdAsync(int tankId, bool activeOnly = true)
        {
            var sql = @"
                SELECT tm.*, m.Code as MaterialCode, m.Name as MaterialName, m.Unit as MaterialUnit
                FROM TankMaterials tm
                LEFT JOIN Materials m ON tm.MaterialId = m.Id
                WHERE tm.TankId = @TankId";
            if (activeOnly) sql += " AND tm.IsActive = 1";
            sql += " ORDER BY tm.Priority, tm.Id";
            return await _db.QueryAsync<TankMaterial>(sql, new { TankId = tankId });
        }

        public async Task<IEnumerable<TankMaterial>> GetByMaterialIdAsync(int materialId, bool activeOnly = true)
        {
            var sql = @"
                SELECT tm.*, m.Code as MaterialCode, m.Name as MaterialName, m.Unit as MaterialUnit
                FROM TankMaterials tm
                LEFT JOIN Materials m ON tm.MaterialId = m.Id
                WHERE tm.MaterialId = @MaterialId";
            if (activeOnly) sql += " AND tm.IsActive = 1";
            sql += " ORDER BY tm.TankId, tm.Priority";
            return await _db.QueryAsync<TankMaterial>(sql, new { MaterialId = materialId });
        }

        public async Task<TankMaterial?> GetCurrentAssignmentAsync(int tankId)
        {
            const string sql = @"
                SELECT TOP 1 tm.*, m.Code as MaterialCode, m.Name as MaterialName, m.Unit as MaterialUnit
                FROM TankMaterials tm
                LEFT JOIN Materials m ON tm.MaterialId = m.Id
                WHERE tm.TankId = @TankId AND tm.IsActive = 1 AND tm.IsPrimary = 1
                ORDER BY tm.Priority, tm.Id";
            return await _db.QuerySingleAsync<TankMaterial>(sql, new { TankId = tankId });
        }

        public async Task<IEnumerable<TankMaterial>> GetAllAsync(bool activeOnly = true)
        {
            var sql = @"
                SELECT tm.*, m.Code as MaterialCode, m.Name as MaterialName, m.Unit as MaterialUnit
                FROM TankMaterials tm
                LEFT JOIN Materials m ON tm.MaterialId = m.Id";
            if (activeOnly) sql += " WHERE tm.IsActive = 1";
            sql += " ORDER BY tm.TankId, tm.Priority";
            return await _db.QueryAsync<TankMaterial>(sql);
        }

        public async Task<TankMaterial> AddAsync(TankMaterial tankMaterial)
        {
            const string sql = @"
                INSERT INTO TankMaterials (TankId, TankCode, TankName, TankType, MaterialId, IsPrimary, Priority,
                    MaxCapacity, MinLevel, MaxLevel, Remark, EffectiveFrom, EffectiveTo, IsActive,
                    CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
                VALUES (@TankId, @TankCode, @TankName, @TankType, @MaterialId, @IsPrimary, @Priority,
                    @MaxCapacity, @MinLevel, @MaxLevel, @Remark, @EffectiveFrom, @EffectiveTo, @IsActive,
                    @CreatedAt, @CreatedBy, @UpdatedAt, @UpdatedBy);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            var id = await _db.ExecuteScalarAsync<int>(sql, new
            {
                tankMaterial.TankId,
                tankMaterial.TankCode,
                tankMaterial.TankName,
                tankMaterial.TankType,
                tankMaterial.MaterialId,
                tankMaterial.IsPrimary,
                tankMaterial.Priority,
                tankMaterial.MaxCapacity,
                tankMaterial.MinLevel,
                tankMaterial.MaxLevel,
                tankMaterial.Remark,
                tankMaterial.EffectiveFrom,
                tankMaterial.EffectiveTo,
                tankMaterial.IsActive,
                tankMaterial.CreatedAt,
                tankMaterial.CreatedBy,
                tankMaterial.UpdatedAt,
                tankMaterial.UpdatedBy
            });

            tankMaterial.Id = id;
            return tankMaterial;
        }

        public async Task<TankMaterial> UpdateAsync(TankMaterial tankMaterial)
        {
            const string sql = @"
                UPDATE TankMaterials 
                SET TankCode = @TankCode, TankName = @TankName, TankType = @TankType,
                    MaterialId = @MaterialId, IsPrimary = @IsPrimary, Priority = @Priority,
                    MaxCapacity = @MaxCapacity, MinLevel = @MinLevel, MaxLevel = @MaxLevel,
                    Remark = @Remark, EffectiveFrom = @EffectiveFrom, EffectiveTo = @EffectiveTo,
                    IsActive = @IsActive, UpdatedAt = @UpdatedAt, UpdatedBy = @UpdatedBy
                WHERE Id = @Id";

            await _db.ExecuteAsync(sql, new
            {
                tankMaterial.Id,
                tankMaterial.TankCode,
                tankMaterial.TankName,
                tankMaterial.TankType,
                tankMaterial.MaterialId,
                tankMaterial.IsPrimary,
                tankMaterial.Priority,
                tankMaterial.MaxCapacity,
                tankMaterial.MinLevel,
                tankMaterial.MaxLevel,
                tankMaterial.Remark,
                tankMaterial.EffectiveFrom,
                tankMaterial.EffectiveTo,
                tankMaterial.IsActive,
                UpdatedAt = DateTime.Now,
                tankMaterial.UpdatedBy
            });

            return tankMaterial;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "DELETE FROM TankMaterials WHERE Id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            return affected > 0;
        }

        public async Task<bool> SetPrimaryAsync(int tankId, int materialId)
        {
            // 先取消该罐体所有主物料标记
            const string sql1 = "UPDATE TankMaterials SET IsPrimary = 0 WHERE TankId = @TankId";
            await _db.ExecuteAsync(sql1, new { TankId = tankId });

            // 再设置指定物料为主物料
            const string sql2 = "UPDATE TankMaterials SET IsPrimary = 1 WHERE TankId = @TankId AND MaterialId = @MaterialId";
            var affected = await _db.ExecuteAsync(sql2, new { TankId = tankId, MaterialId = materialId });
            return affected > 0;
        }

        public async Task<IEnumerable<TankMaterialHistory>> GetHistoryAsync(int tankId, DateTime? from = null, DateTime? to = null)
        {
            var where = new List<string> { "TankId = @TankId" };
            var parameters = new DynamicParameters();
            parameters.Add("@TankId", tankId);

            if (from.HasValue)
            {
                where.Add("ChangeTime >= @From");
                parameters.Add("@From", from.Value);
            }

            if (to.HasValue)
            {
                where.Add("ChangeTime <= @To");
                parameters.Add("@To", to.Value);
            }

            var whereClause = string.Join(" AND ", where);
            var sql = $@"
                SELECT h.*, 
                    om.Code as OldMaterialCode, om.Name as OldMaterialName,
                    nm.Code as NewMaterialCode, nm.Name as NewMaterialName
                FROM TankMaterialHistory h
                LEFT JOIN Materials om ON h.OldMaterialId = om.Id
                LEFT JOIN Materials nm ON h.NewMaterialId = nm.Id
                WHERE {whereClause}
                ORDER BY h.ChangeTime DESC";

            return await _db.QueryAsync<TankMaterialHistory>(sql, parameters);
        }

        public async Task<TankMaterialHistory> AddHistoryAsync(TankMaterialHistory history)
        {
            const string sql = @"
                INSERT INTO TankMaterialHistory (TankId, TankCode, TankName, OldMaterialId, NewMaterialId, ChangeReason, Operator, ChangeTime)
                VALUES (@TankId, @TankCode, @TankName, @OldMaterialId, @NewMaterialId, @ChangeReason, @Operator, @ChangeTime);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            var id = await _db.ExecuteScalarAsync<int>(sql, new
            {
                history.TankId,
                history.TankCode,
                history.TankName,
                history.OldMaterialId,
                history.NewMaterialId,
                history.ChangeReason,
                history.Operator,
                history.ChangeTime
            });

            history.Id = id;
            return history;
        }
    }
}