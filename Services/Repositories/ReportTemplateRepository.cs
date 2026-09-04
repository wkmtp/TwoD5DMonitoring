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
    /// 报表模板仓储实现
    /// </summary>
    public class ReportTemplateRepository : IReportTemplateRepository
    {
        private readonly IDatabaseProvider _db;

        public ReportTemplateRepository(IDatabaseProvider db)
        {
            _db = db;
        }

        public async Task<ReportTemplate?> GetByIdAsync(int id)
        {
            const string sql = "SELECT * FROM ReportTemplates WHERE Id = @Id";
            return await _db.QuerySingleAsync<ReportTemplate>(sql, new { Id = id });
        }

        public async Task<IEnumerable<ReportTemplate>> GetByTypeAsync(ReportType type)
        {
            const string sql = "SELECT * FROM ReportTemplates WHERE ReportType = @Type ORDER BY Name";
            return await _db.QueryAsync<ReportTemplate>(sql, new { Type = (int)type });
        }

        public async Task<IEnumerable<ReportTemplate>> GetAllAsync()
        {
            const string sql = "SELECT * FROM ReportTemplates ORDER BY Name";
            return await _db.QueryAsync<ReportTemplate>(sql);
        }

        public async Task<ReportTemplate> AddAsync(ReportTemplate template)
        {
            const string sql = @"
                INSERT INTO ReportTemplates (Name, Description, ReportType, TemplateContent, ParametersJson, Version, IsDefault, CreatedAt, CreatedBy)
                VALUES (@Name, @Description, @ReportType, @TemplateContent, @ParametersJson, @Version, @IsDefault, @CreatedAt, @CreatedBy);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            var id = await _db.ExecuteScalarAsync<int>(sql, new
            {
                template.Name,
                template.Description,
                template.ReportType,
                template.TemplateContent,
                template.ParametersJson,
                template.Version,
                template.IsDefault,
                template.CreatedAt,
                template.CreatedBy
            });

            template.Id = id;
            return template;
        }

        public async Task<ReportTemplate> UpdateAsync(ReportTemplate template)
        {
            const string sql = @"
                UPDATE ReportTemplates 
                SET Name = @Name, Description = @Description, ReportType = @ReportType, TemplateContent = @TemplateContent,
                    ParametersJson = @ParametersJson, Version = @Version, IsDefault = @IsDefault,
                    UpdatedAt = @UpdatedAt, UpdatedBy = @UpdatedBy
                WHERE Id = @Id";

            await _db.ExecuteAsync(sql, new
            {
                template.Id,
                template.Name,
                template.Description,
                template.ReportType,
                template.TemplateContent,
                template.ParametersJson,
                template.Version,
                template.IsDefault,
                UpdatedAt = DateTime.Now,
                template.UpdatedBy
            });

            return template;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "DELETE FROM ReportTemplates WHERE Id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            return affected > 0;
        }
    }
}