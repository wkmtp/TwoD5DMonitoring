using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using TwoD5DMonitoring.Infrastructure.Database;
using TwoD5DMonitoring.Models;
using TwoD5DMonitoring.Services.Repositories;

namespace TwoD5DMonitoring.Services.Repositories
{
    /// <summary>
    /// 配方仓储实现
    /// </summary>
    public class RecipeRepository : IRecipeRepository
    {
        private readonly IDatabaseProvider _db;

        public RecipeRepository(IDatabaseProvider db)
        {
            _db = db;
        }

        public async Task<RecipeBase?> GetByIdAsync(int id)
        {
            const string sql = @"
                SELECT r.*, 
                    wr.WeighingMode, wr.ScaleId, wr.TotalWeight, wr.DecimalPlaces, wr.AllowManualAdjust,
                    mr.MixerId, mr.ProcessType, mr.TotalSteps, mr.TargetTemperature, mr.TargetTime,
                    mr.DumpTemperature, mr.RamPressure, mr.RamPosition, mr.RotorSpeed,
                    mr.EnableTempControl, mr.EnableEnergyControl
                FROM Recipes r
                LEFT JOIN WeighingRecipes wr ON r.Id = wr.RecipeId
                LEFT JOIN MixingRecipes mr ON r.Id = mr.RecipeId
                WHERE r.Id = @Id";
            
            var recipe = await _db.QuerySingleAsync<dynamic>(sql, new { Id = id });
            if (recipe == null) return null;

            return MapToRecipe(recipe);
        }

        public async Task<RecipeBase?> GetByCodeAsync(string code)
        {
            const string sql = @"
                SELECT r.*, 
                    wr.WeighingMode, wr.ScaleId, wr.TotalWeight, wr.DecimalPlaces, wr.AllowManualAdjust,
                    mr.MixerId, mr.ProcessType, mr.TotalSteps, mr.TargetTemperature, mr.TargetTime,
                    mr.DumpTemperature, mr.RamPressure, mr.RamPosition, mr.RotorSpeed,
                    mr.EnableTempControl, mr.EnableEnergyControl
                FROM Recipes r
                LEFT JOIN WeighingRecipes wr ON r.Id = wr.RecipeId
                LEFT JOIN MixingRecipes mr ON r.Id = mr.RecipeId
                WHERE r.Code = @Code";
            
            var recipe = await _db.QuerySingleAsync<dynamic>(sql, new { Code = code });
            if (recipe == null) return null;

            return MapToRecipe(recipe);
        }

        public async Task<IEnumerable<RecipeBase>> GetAllAsync(RecipeFilter filter)
        {
            var recipes = await GetPagedAsync(1, int.MaxValue, filter);
            return recipes;
        }

        public async Task<IEnumerable<RecipeBase>> GetPagedAsync(int page, int pageSize, RecipeFilter filter)
        {
            var where = new List<string> { "r.Status != 6" }; // 排除作废
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                where.Add("(r.Code LIKE @Keyword OR r.Name LIKE @Keyword OR r.ProductCode LIKE @Keyword OR r.ProductName LIKE @Keyword)");
                parameters.Add("@Keyword", $"%{filter.Keyword}%");
            }

            if (filter.RecipeType.HasValue)
            {
                where.Add("r.RecipeType = @RecipeType");
                parameters.Add("@RecipeType", (int)filter.RecipeType.Value);
            }

            if (filter.Status.HasValue)
            {
                where.Add("r.Status = @Status");
                parameters.Add("@Status", (int)filter.Status.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.ProductCode))
            {
                where.Add("r.ProductCode = @ProductCode");
                parameters.Add("@ProductCode", filter.ProductCode);
            }

            if (filter.CreatedFrom.HasValue)
            {
                where.Add("r.CreatedAt >= @CreatedFrom");
                parameters.Add("@CreatedFrom", filter.CreatedFrom.Value);
            }

            if (filter.CreatedTo.HasValue)
            {
                where.Add("r.CreatedAt <= @CreatedTo");
                parameters.Add("@CreatedTo", filter.CreatedTo.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.CreatedBy))
            {
                where.Add("r.CreatedBy = @CreatedBy");
                parameters.Add("@CreatedBy", filter.CreatedBy);
            }

            var whereClause = "WHERE " + string.Join(" AND ", where);
            var orderBy = !string.IsNullOrWhiteSpace(filter.SortBy) ? filter.SortBy : "r.CreatedAt";
            var orderDir = filter.SortDescending ? "DESC" : "ASC";

            var sql = $@"
                SELECT r.*, 
                    wr.WeighingMode, wr.ScaleId, wr.TotalWeight, wr.DecimalPlaces, wr.AllowManualAdjust,
                    mr.MixerId, mr.ProcessType, mr.TotalSteps, mr.TargetTemperature, mr.TargetTime,
                    mr.DumpTemperature, mr.RamPressure, mr.RamPosition, mr.RotorSpeed,
                    mr.EnableTempControl, mr.EnableEnergyControl
                FROM Recipes r
                LEFT JOIN WeighingRecipes wr ON r.Id = wr.RecipeId
                LEFT JOIN MixingRecipes mr ON r.Id = mr.RecipeId
                {whereClause}
                ORDER BY {orderBy} {orderDir}
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            parameters.Add("@Offset", (page - 1) * pageSize);
            parameters.Add("@PageSize", pageSize);

            var results = await _db.QueryAsync<dynamic>(sql, parameters);
            return results.Select(MapToRecipe).Where(r => r != null)!;
        }

        public async Task<int> GetCountAsync(RecipeFilter filter)
        {
            var where = new List<string> { "r.Status != 6" };
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                where.Add("(r.Code LIKE @Keyword OR r.Name LIKE @Keyword OR r.ProductCode LIKE @Keyword OR r.ProductName LIKE @Keyword)");
                parameters.Add("@Keyword", $"%{filter.Keyword}%");
            }

            if (filter.RecipeType.HasValue)
            {
                where.Add("r.RecipeType = @RecipeType");
                parameters.Add("@RecipeType", (int)filter.RecipeType.Value);
            }

            if (filter.Status.HasValue)
            {
                where.Add("r.Status = @Status");
                parameters.Add("@Status", (int)filter.Status.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.ProductCode))
            {
                where.Add("r.ProductCode = @ProductCode");
                parameters.Add("@ProductCode", filter.ProductCode);
            }

            if (filter.CreatedFrom.HasValue)
            {
                where.Add("r.CreatedAt >= @CreatedFrom");
                parameters.Add("@CreatedFrom", filter.CreatedFrom.Value);
            }

            if (filter.CreatedTo.HasValue)
            {
                where.Add("r.CreatedAt <= @CreatedTo");
                parameters.Add("@CreatedTo", filter.CreatedTo.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.CreatedBy))
            {
                where.Add("r.CreatedBy = @CreatedBy");
                parameters.Add("@CreatedBy", filter.CreatedBy);
            }

            var whereClause = "WHERE " + string.Join(" AND ", where);
            var sql = $"SELECT COUNT(*) FROM Recipes r {whereClause}";
            return await _db.ExecuteScalarAsync<int>(sql, parameters);
        }

        public async Task<WeighingRecipe?> GetWeighingRecipeAsync(int id)
        {
            var recipe = await GetByIdAsync(id);
            return recipe as WeighingRecipe;
        }

        public async Task<MixingRecipe?> GetMixingRecipeAsync(int id)
        {
            var recipe = await GetByIdAsync(id);
            return recipe as MixingRecipe;
        }

        public async Task<RecipeBase> AddAsync(RecipeBase recipe)
        {
            using var transaction = await _db.BeginTransactionAsync(await _db.OpenConnectionAsync(""));
            try
            {
                var dbTransaction = transaction as DbTransaction;
                var connection = dbTransaction?.Connection;
                if (connection == null) throw new InvalidOperationException("无法获取数据库连接");

                // 插入基础配方
                const string sqlRecipe = @"
                    INSERT INTO Recipes (Code, Name, RecipeType, Status, ProductCode, ProductName, BatchSize, YieldRate,
                        Remark, CurrentVersion, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
                    VALUES (@Code, @Name, @RecipeType, @Status, @ProductCode, @ProductName, @BatchSize, @YieldRate,
                        @Remark, @CurrentVersion, @CreatedAt, @CreatedBy, @UpdatedAt, @UpdatedBy);
                    SELECT CAST(SCOPE_IDENTITY() as int);";

                var recipeId = await connection.ExecuteScalarAsync<int>(sqlRecipe, new
                {
                    recipe.Code,
                    recipe.Name,
                    recipe.RecipeType,
                    recipe.Status,
                    recipe.ProductCode,
                    recipe.ProductName,
                    recipe.BatchSize,
                    recipe.YieldRate,
                    recipe.Remark,
                    recipe.CurrentVersion,
                    recipe.CreatedAt,
                    recipe.CreatedBy,
                    recipe.UpdatedAt,
                    recipe.UpdatedBy
                }, transaction);

                recipe.Id = recipeId;

                // 插入扩展表
                if (recipe is WeighingRecipe weighingRecipe)
                {
                    const string sqlWeighing = @"
                        INSERT INTO WeighingRecipes (RecipeId, WeighingMode, ScaleId, TotalWeight, DecimalPlaces, AllowManualAdjust)
                        VALUES (@RecipeId, @WeighingMode, @ScaleId, @TotalWeight, @DecimalPlaces, @AllowManualAdjust)";
                    
                    await connection.ExecuteAsync(sqlWeighing, new
                    {
                        RecipeId = recipeId,
                        weighingRecipe.WeighingMode,
                        weighingRecipe.ScaleId,
                        weighingRecipe.TotalWeight,
                        weighingRecipe.DecimalPlaces,
                        weighingRecipe.AllowManualAdjust
                    }, transaction);

                    // 插入明细行
                    if (weighingRecipe.Lines != null)
                    {
                        foreach (var line in weighingRecipe.Lines)
                        {
                            line.RecipeId = recipeId;
                            await AddWeighingLineInternalAsync(connection, transaction, line);
                        }
                    }
                }
                else if (recipe is MixingRecipe mixingRecipe)
                {
                    const string sqlMixing = @"
                        INSERT INTO MixingRecipes (RecipeId, MixerId, ProcessType, TotalSteps, TargetTemperature, TargetTime,
                            DumpTemperature, RamPressure, RamPosition, RotorSpeed, EnableTempControl, EnableEnergyControl)
                        VALUES (@RecipeId, @MixerId, @ProcessType, @TotalSteps, @TargetTemperature, @TargetTime,
                            @DumpTemperature, @RamPressure, @RamPosition, @RotorSpeed, @EnableTempControl, @EnableEnergyControl)";
                    
                    await connection.ExecuteAsync(sqlMixing, new
                    {
                        RecipeId = recipeId,
                        mixingRecipe.MixerId,
                        mixingRecipe.ProcessType,
                        mixingRecipe.TotalSteps,
                        mixingRecipe.TargetTemperature,
                        mixingRecipe.TargetTime,
                        mixingRecipe.DumpTemperature,
                        mixingRecipe.RamPressure,
                        mixingRecipe.RamPosition,
                        mixingRecipe.RotorSpeed,
                        mixingRecipe.EnableTempControl,
                        mixingRecipe.EnableEnergyControl
                    }, transaction);

                    // 插入步骤
                    if (mixingRecipe.Steps != null)
                    {
                        foreach (var step in mixingRecipe.Steps)
                        {
                            step.RecipeId = recipeId;
                            await AddMixingStepInternalAsync(connection, transaction, step);
                        }
                    }
                }

                // 插入初始版本
                var version = new RecipeVersion
                {
                    RecipeId = recipeId,
                    Version = recipe.CurrentVersion,
                    ChangeDescription = "初始版本",
                    IsCurrent = true,
                    CreatedAt = DateTime.Now,
                    CreatedBy = recipe.CreatedBy
                };
                await AddVersionInternalAsync(connection, transaction, version);

                dbTransaction?.Commit();
                return recipe;
            }
            catch
            {
                (transaction as DbTransaction)?.Rollback();
                throw;
            }
        }

        public async Task<RecipeBase> UpdateAsync(RecipeBase recipe)
        {
            using var transaction = await _db.BeginTransactionAsync(await _db.OpenConnectionAsync(""));
            try
            {
                var dbTransaction = transaction as DbTransaction;
                var connection = dbTransaction?.Connection;
                if (connection == null) throw new InvalidOperationException("无法获取数据库连接");

                // 更新基础配方
                const string sqlRecipe = @"
                    UPDATE Recipes 
                    SET Code = @Code, Name = @Name, RecipeType = @RecipeType, Status = @Status,
                        ProductCode = @ProductCode, ProductName = @ProductName, BatchSize = @BatchSize,
                        YieldRate = @YieldRate, Remark = @Remark, CurrentVersion = @CurrentVersion,
                        UpdatedAt = @UpdatedAt, UpdatedBy = @UpdatedBy
                    WHERE Id = @Id";

                await connection.ExecuteAsync(sqlRecipe, new
                {
                    recipe.Id,
                    recipe.Code,
                    recipe.Name,
                    recipe.RecipeType,
                    recipe.Status,
                    recipe.ProductCode,
                    recipe.ProductName,
                    recipe.BatchSize,
                    recipe.YieldRate,
                    recipe.Remark,
                    recipe.CurrentVersion,
                    UpdatedAt = DateTime.Now,
                    recipe.UpdatedBy
                }, transaction);

                // 更新扩展表
                if (recipe is WeighingRecipe weighingRecipe)
                {
                    const string sqlWeighing = @"
                        MERGE WeighingRecipes AS target
                        USING (SELECT @RecipeId as RecipeId) AS source
                        ON target.RecipeId = source.RecipeId
                        WHEN MATCHED THEN
                            UPDATE SET WeighingMode = @WeighingMode, ScaleId = @ScaleId, 
                                TotalWeight = @TotalWeight, DecimalPlaces = @DecimalPlaces, AllowManualAdjust = @AllowManualAdjust
                        WHEN NOT MATCHED THEN
                            INSERT (RecipeId, WeighingMode, ScaleId, TotalWeight, DecimalPlaces, AllowManualAdjust)
                            VALUES (@RecipeId, @WeighingMode, @ScaleId, @TotalWeight, @DecimalPlaces, @AllowManualAdjust);";

                    await connection.ExecuteAsync(sqlWeighing, new
                    {
                        RecipeId = recipe.Id,
                        weighingRecipe.WeighingMode,
                        weighingRecipe.ScaleId,
                        weighingRecipe.TotalWeight,
                        weighingRecipe.DecimalPlaces,
                        weighingRecipe.AllowManualAdjust
                    }, transaction);
                }
                else if (recipe is MixingRecipe mixingRecipe)
                {
                    const string sqlMixing = @"
                        MERGE MixingRecipes AS target
                        USING (SELECT @RecipeId as RecipeId) AS source
                        ON target.RecipeId = source.RecipeId
                        WHEN MATCHED THEN
                            UPDATE SET MixerId = @MixerId, ProcessType = @ProcessType, TotalSteps = @TotalSteps,
                                TargetTemperature = @TargetTemperature, TargetTime = @TargetTime,
                                DumpTemperature = @DumpTemperature, RamPressure = @RamPressure,
                                RamPosition = @RamPosition, RotorSpeed = @RotorSpeed,
                                EnableTempControl = @EnableTempControl, EnableEnergyControl = @EnableEnergyControl
                        WHEN NOT MATCHED THEN
                            INSERT (RecipeId, MixerId, ProcessType, TotalSteps, TargetTemperature, TargetTime,
                                DumpTemperature, RamPressure, RamPosition, RotorSpeed, EnableTempControl, EnableEnergyControl)
                            VALUES (@RecipeId, @MixerId, @ProcessType, @TotalSteps, @TargetTemperature, @TargetTime,
                                @DumpTemperature, @RamPressure, @RamPosition, @RotorSpeed, @EnableTempControl, @EnableEnergyControl);";

                    await connection.ExecuteAsync(sqlMixing, new
                    {
                        RecipeId = recipe.Id,
                        mixingRecipe.MixerId,
                        mixingRecipe.ProcessType,
                        mixingRecipe.TotalSteps,
                        mixingRecipe.TargetTemperature,
                        mixingRecipe.TargetTime,
                        mixingRecipe.DumpTemperature,
                        mixingRecipe.RamPressure,
                        mixingRecipe.RamPosition,
                        mixingRecipe.RotorSpeed,
                        mixingRecipe.EnableTempControl,
                        mixingRecipe.EnableEnergyControl
                    }, transaction);
                }

                dbTransaction?.Commit();
                return recipe;
            }
            catch
            {
                (transaction as DbTransaction)?.Rollback();
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "DELETE FROM Recipes WHERE Id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            return affected > 0;
        }

        public async Task<bool> ExistsCodeAsync(string code, int? excludeId = null)
        {
            var sql = "SELECT COUNT(*) FROM Recipes WHERE Code = @Code";
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

        public async Task<RecipeVersion> AddVersionAsync(RecipeVersion version)
        {
            using var connection = await _db.OpenConnectionAsync("");
            return await AddVersionInternalAsync(connection, null, version);
        }

        private async Task<RecipeVersion> AddVersionInternalAsync(IDbConnection connection, IDbTransaction? transaction, RecipeVersion version)
        {
            // 取消当前版本
            const string sqlUnset = "UPDATE RecipeVersions SET IsCurrent = 0 WHERE RecipeId = @RecipeId";
            await connection.ExecuteAsync(sqlUnset, new { version.RecipeId }, transaction);

            const string sql = @"
                INSERT INTO RecipeVersions (RecipeId, Version, ChangeDescription, IsCurrent, CreatedAt, CreatedBy)
                VALUES (@RecipeId, @Version, @ChangeDescription, @IsCurrent, @CreatedAt, @CreatedBy);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            var id = await connection.ExecuteScalarAsync<int>(sql, new
            {
                version.RecipeId,
                version.Version,
                version.ChangeDescription,
                version.IsCurrent,
                version.CreatedAt,
                version.CreatedBy
            }, transaction);

            version.Id = id;
            return version;
        }

        public async Task<bool> SetCurrentVersionAsync(int recipeId, int versionId)
        {
            const string sql = @"
                UPDATE RecipeVersions SET IsCurrent = CASE WHEN Id = @VersionId THEN 1 ELSE 0 END
                WHERE RecipeId = @RecipeId;
                UPDATE Recipes SET CurrentVersion = (SELECT Version FROM RecipeVersions WHERE Id = @VersionId), CurrentVersionId = @VersionId
                WHERE Id = @RecipeId";

            var affected = await _db.ExecuteAsync(sql, new { RecipeId = recipeId, VersionId = versionId });
            return affected > 0;
        }

        public async Task<IEnumerable<RecipeVersion>> GetVersionsAsync(int recipeId)
        {
            const string sql = "SELECT * FROM RecipeVersions WHERE RecipeId = @RecipeId ORDER BY CreatedAt DESC";
            return await _db.QueryAsync<RecipeVersion>(sql, new { RecipeId = recipeId });
        }

        public async Task<IEnumerable<WeighingRecipeLine>> GetWeighingLinesAsync(int recipeId)
        {
            const string sql = @"
                SELECT l.*, m.Code as MaterialCode, m.Name as MaterialName, m.Unit as MaterialUnit
                FROM WeighingRecipeLines l
                LEFT JOIN Materials m ON l.MaterialId = m.Id
                WHERE l.RecipeId = @RecipeId
                ORDER BY l.LineNumber";
            return await _db.QueryAsync<WeighingRecipeLine>(sql, new { RecipeId = recipeId });
        }

        public async Task<WeighingRecipeLine> AddWeighingLineAsync(WeighingRecipeLine line)
        {
            using var connection = await _db.OpenConnectionAsync("");
            return await AddWeighingLineInternalAsync(connection, null, line);
        }

        private async Task<WeighingRecipeLine> AddWeighingLineInternalAsync(IDbConnection connection, IDbTransaction? transaction, WeighingRecipeLine line)
        {
            const string sql = @"
                INSERT INTO WeighingRecipeLines (RecipeId, LineNumber, MaterialId, TargetWeight, TolerancePositive, ToleranceNegative,
                    WeighingOrder, IsCritical, Remark, FeederType, PlcTagIndex, PlcWeightTag, PlcMaterialTag)
                VALUES (@RecipeId, @LineNumber, @MaterialId, @TargetWeight, @TolerancePositive, @ToleranceNegative,
                    @WeighingOrder, @IsCritical, @Remark, @FeederType, @PlcTagIndex, @PlcWeightTag, @PlcMaterialTag);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            var id = await connection.ExecuteScalarAsync<int>(sql, new
            {
                line.RecipeId,
                line.LineNumber,
                line.MaterialId,
                line.TargetWeight,
                line.TolerancePositive,
                line.ToleranceNegative,
                line.WeighingOrder,
                line.IsCritical,
                line.Remark,
                line.FeederType,
                line.PlcTagIndex,
                line.PlcWeightTag,
                line.PlcMaterialTag
            }, transaction);

            line.Id = id;
            return line;
        }

        public async Task<WeighingRecipeLine> UpdateWeighingLineAsync(WeighingRecipeLine line)
        {
            const string sql = @"
                UPDATE WeighingRecipeLines 
                SET LineNumber = @LineNumber, MaterialId = @MaterialId, TargetWeight = @TargetWeight,
                    TolerancePositive = @TolerancePositive, ToleranceNegative = @ToleranceNegative,
                    WeighingOrder = @WeighingOrder, IsCritical = @IsCritical, Remark = @Remark,
                    FeederType = @FeederType, PlcTagIndex = @PlcTagIndex, PlcWeightTag = @PlcWeightTag, PlcMaterialTag = @PlcMaterialTag
                WHERE Id = @Id";

            await _db.ExecuteAsync(sql, new
            {
                line.Id,
                line.LineNumber,
                line.MaterialId,
                line.TargetWeight,
                line.TolerancePositive,
                line.ToleranceNegative,
                line.WeighingOrder,
                line.IsCritical,
                line.Remark,
                line.FeederType,
                line.PlcTagIndex,
                line.PlcWeightTag,
                line.PlcMaterialTag
            });

            return line;
        }

        public async Task<bool> DeleteWeighingLineAsync(int lineId)
        {
            const string sql = "DELETE FROM WeighingRecipeLines WHERE Id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = lineId });
            return affected > 0;
        }

        public async Task<bool> ReorderWeighingLinesAsync(int recipeId, List<int> lineIdsInOrder)
        {
            using var transaction = await _db.BeginTransactionAsync(await _db.OpenConnectionAsync(""));
            try
            {
                var dbTransaction = transaction as DbTransaction;
                var connection = dbTransaction?.Connection;
                if (connection == null) throw new InvalidOperationException("无法获取数据库连接");

                for (int i = 0; i < lineIdsInOrder.Count; i++)
                {
                    const string sql = "UPDATE WeighingRecipeLines SET LineNumber = @LineNumber WHERE Id = @Id AND RecipeId = @RecipeId";
                    await connection.ExecuteAsync(sql, new
                    {
                        LineNumber = i + 1,
                        Id = lineIdsInOrder[i],
                        RecipeId = recipeId
                    }, transaction);
                }

                dbTransaction?.Commit();
                return true;
            }
            catch
            {
                (transaction as DbTransaction)?.Rollback();
                return false;
            }
        }

        public async Task<IEnumerable<MixingRecipeStep>> GetMixingStepsAsync(int recipeId)
        {
            const string sql = @"
                SELECT s.*, m.Code as MaterialCode, m.Name as MaterialName
                FROM MixingRecipeSteps s
                LEFT JOIN Materials m ON s.MaterialId = m.Id
                WHERE s.RecipeId = @RecipeId
                ORDER BY s.StepNumber";
            return await _db.QueryAsync<MixingRecipeStep>(sql, new { RecipeId = recipeId });
        }

        public async Task<MixingRecipeStep> AddMixingStepAsync(MixingRecipeStep step)
        {
            using var connection = await _db.OpenConnectionAsync("");
            return await AddMixingStepInternalAsync(connection, null, step);
        }

        private async Task<MixingRecipeStep> AddMixingStepInternalAsync(IDbConnection connection, IDbTransaction? transaction, MixingRecipeStep step)
        {
            const string sql = @"
                INSERT INTO MixingRecipeSteps (RecipeId, StepNumber, StepType, MaterialId, Amount, AddTime, Duration,
                    TargetTemp, RotorSpeed, RamPressure, IsAutoDump, WaitForTemp, Condition, Remark,
                    PlcStepIndex, PlcCommandTag, PlcParamTags)
                VALUES (@RecipeId, @StepNumber, @StepType, @MaterialId, @Amount, @AddTime, @Duration,
                    @TargetTemp, @RotorSpeed, @RamPressure, @IsAutoDump, @WaitForTemp, @Condition, @Remark,
                    @PlcStepIndex, @PlcCommandTag, @PlcParamTags);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            var id = await connection.ExecuteScalarAsync<int>(sql, new
            {
                step.RecipeId,
                step.StepNumber,
                step.StepType,
                step.MaterialId,
                step.Amount,
                step.AddTime,
                step.Duration,
                step.TargetTemp,
                step.RotorSpeed,
                step.RamPressure,
                step.IsAutoDump,
                step.WaitForTemp,
                step.Condition,
                step.Remark,
                step.PlcStepIndex,
                step.PlcCommandTag,
                step.PlcParamTags
            }, transaction);

            step.Id = id;
            return step;
        }

        public async Task<MixingRecipeStep> UpdateMixingStepAsync(MixingRecipeStep step)
        {
            const string sql = @"
                UPDATE MixingRecipeSteps 
                SET StepNumber = @StepNumber, StepType = @StepType, MaterialId = @MaterialId, Amount = @Amount,
                    AddTime = @AddTime, Duration = @Duration, TargetTemp = @TargetTemp, RotorSpeed = @RotorSpeed,
                    RamPressure = @RamPressure, IsAutoDump = @IsAutoDump, WaitForTemp = @WaitForTemp,
                    Condition = @Condition, Remark = @Remark, PlcStepIndex = @PlcStepIndex,
                    PlcCommandTag = @PlcCommandTag, PlcParamTags = @PlcParamTags
                WHERE Id = @Id";

            await _db.ExecuteAsync(sql, new
            {
                step.Id,
                step.StepNumber,
                step.StepType,
                step.MaterialId,
                step.Amount,
                step.AddTime,
                step.Duration,
                step.TargetTemp,
                step.RotorSpeed,
                step.RamPressure,
                step.IsAutoDump,
                step.WaitForTemp,
                step.Condition,
                step.Remark,
                step.PlcStepIndex,
                step.PlcCommandTag,
                step.PlcParamTags
            });

            return step;
        }

        public async Task<bool> DeleteMixingStepAsync(int stepId)
        {
            const string sql = "DELETE FROM MixingRecipeSteps WHERE Id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = stepId });
            return affected > 0;
        }

        public async Task<bool> ReorderMixingStepsAsync(int recipeId, List<int> stepIdsInOrder)
        {
            using var transaction = await _db.BeginTransactionAsync(await _db.OpenConnectionAsync(""));
            try
            {
                var dbTransaction = transaction as DbTransaction;
                var connection = dbTransaction?.Connection;
                if (connection == null) throw new InvalidOperationException("无法获取数据库连接");

                for (int i = 0; i < stepIdsInOrder.Count; i++)
                {
                    const string sql = "UPDATE MixingRecipeSteps SET StepNumber = @StepNumber WHERE Id = @Id AND RecipeId = @RecipeId";
                    await connection.ExecuteAsync(sql, new
                    {
                        StepNumber = i + 1,
                        Id = stepIdsInOrder[i],
                        RecipeId = recipeId
                    }, transaction);
                }

                // 更新总步骤数
                const string sqlUpdate = "UPDATE MixingRecipes SET TotalSteps = @TotalSteps WHERE RecipeId = @RecipeId";
                await connection.ExecuteAsync(sqlUpdate, new { TotalSteps = stepIdsInOrder.Count, RecipeId = recipeId }, transaction);

                dbTransaction?.Commit();
                return true;
            }
            catch
            {
                (transaction as DbTransaction)?.Rollback();
                return false;
            }
        }

        private RecipeBase? MapToRecipe(dynamic row)
        {
            if (row == null) return null;

            var recipeType = (RecipeType)(int)row.RecipeType;
            
            RecipeBase recipe = recipeType == RecipeType.Weighing 
                ? new WeighingRecipe() 
                : new MixingRecipe();

            // 基础字段
            recipe.Id = (int)row.Id;
            recipe.Code = (string)row.Code;
            recipe.Name = (string)row.Name;
            recipe.RecipeType = recipeType;
            recipe.Status = (RecipeStatus)(int)row.Status;
            recipe.ProductCode = row.ProductCode;
            recipe.ProductName = row.ProductName;
            recipe.BatchSize = (decimal)row.BatchSize;
            recipe.YieldRate = (decimal)row.YieldRate;
            recipe.Remark = row.Remark;
            recipe.CurrentVersion = (string)row.CurrentVersion;
            recipe.CreatedAt = (DateTime)row.CreatedAt;
            recipe.CreatedBy = row.CreatedBy;
            recipe.UpdatedAt = (DateTime)row.UpdatedAt;
            recipe.UpdatedBy = row.UpdatedBy;
            recipe.ApprovedAt = row.ApprovedAt;
            recipe.ApprovedBy = row.ApprovedBy;
            recipe.ReleasedAt = row.ReleasedAt;
            recipe.ReleasedBy = row.ReleasedBy;

            // 扩展字段
            if (recipe is WeighingRecipe wr)
            {
                wr.WeighingMode = (WeighingMode)(row.WeighingMode != null ? (int)row.WeighingMode : 1);
                wr.ScaleId = row.ScaleId != null ? (int)row.ScaleId : 0;
                wr.TotalWeight = row.TotalWeight != null ? (decimal)row.TotalWeight : 0;
                wr.DecimalPlaces = row.DecimalPlaces != null ? (int)row.DecimalPlaces : 2;
                wr.AllowManualAdjust = row.AllowManualAdjust != null ? (bool)row.AllowManualAdjust : true;
            }
            else if (recipe is MixingRecipe mr)
            {
                mr.MixerId = row.MixerId != null ? (int)row.MixerId : 0;
                mr.ProcessType = (MixingProcessType)(row.ProcessType != null ? (int)row.ProcessType : 1);
                mr.TotalSteps = row.TotalSteps != null ? (int)row.TotalSteps : 0;
                mr.TargetTemperature = row.TargetTemperature != null ? (decimal)row.TargetTemperature : 0;
                mr.TargetTime = row.TargetTime != null ? (int)row.TargetTime : 0;
                mr.DumpTemperature = row.DumpTemperature != null ? (int)row.DumpTemperature : 0;
                mr.RamPressure = row.RamPressure != null ? (decimal)row.RamPressure : 0;
                mr.RamPosition = row.RamPosition != null ? (int)row.RamPosition : 0;
                mr.RotorSpeed = row.RotorSpeed != null ? (int)row.RotorSpeed : 0;
                mr.EnableTempControl = row.EnableTempControl != null ? (bool)row.EnableTempControl : true;
                mr.EnableEnergyControl = row.EnableEnergyControl != null ? (bool)row.EnableEnergyControl : false;
            }

            return recipe;
        }
    }
}