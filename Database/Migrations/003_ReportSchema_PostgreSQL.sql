-- =====================================================
-- TwoD5DMonitoring 报表系统数据库迁移脚本 - PostgreSQL
-- 版本: 1.3.0
-- =====================================================

-- 1. 历史趋势数据表 (使用分区表)
CREATE TABLE historical_trend_data (
    id BIGSERIAL PRIMARY KEY,
    tag_name VARCHAR(100) NOT NULL,
    value NUMERIC(18,4) NOT NULL,
    quality INT NOT NULL DEFAULT 192,
    timestamp TIMESTAMPTZ NOT NULL
) PARTITION BY RANGE (timestamp);

-- 创建月度分区示例 (实际部署时需按需创建)
-- CREATE TABLE historical_trend_data_202601 PARTITION OF historical_trend_data
--     FOR VALUES FROM ('2026-01-01') TO ('2026-02-01');

CREATE INDEX ix_historical_trend_data_tag_time ON historical_trend_data (tag_name, timestamp DESC);
CREATE INDEX ix_historical_trend_data_timestamp ON historical_trend_data (timestamp DESC);

-- 2. 班次定义表
CREATE TABLE shift_definitions (
    id SERIAL PRIMARY KEY,
    name VARCHAR(50) NOT NULL,
    start_time TIME NOT NULL,
    end_time TIME NOT NULL,
    color VARCHAR(20) DEFAULT '#2196F3',
    created_at TIMESTAMPTZ DEFAULT NOW()
);

INSERT INTO shift_definitions (name, start_time, end_time, color) VALUES
('白班', '08:00:00', '20:00:00', '#FF9800'),
('夜班', '20:00:00', '08:00:00', '#3F51B5');

-- 3. 生产报表主表
CREATE TABLE production_reports (
    id SERIAL PRIMARY KEY,
    report_number VARCHAR(50) NOT NULL UNIQUE,
    report_type INT NOT NULL DEFAULT 2,
    period_start TIMESTAMPTZ NOT NULL,
    period_end TIMESTAMPTZ NOT NULL,
    shift_id INT,
    shift_name VARCHAR(50),
    status INT NOT NULL DEFAULT 1,
    generated_by VARCHAR(100),
    generated_at TIMESTAMPTZ DEFAULT NOW(),
    approved_at TIMESTAMPTZ,
    approved_by VARCHAR(100),
    summary_data_json JSONB DEFAULT '{}',
    detail_data_json JSONB DEFAULT '{}',
    CONSTRAINT fk_production_reports_shift FOREIGN KEY (shift_id) REFERENCES shift_definitions(id)
);

CREATE INDEX ix_production_reports_type_period ON production_reports (report_type, period_start DESC);
CREATE INDEX ix_production_reports_status ON production_reports (status);
CREATE INDEX ix_production_reports_shift ON production_reports (shift_id);

-- 4. OEE 结果表
CREATE TABLE oee_results (
    id SERIAL PRIMARY KEY,
    equipment_code VARCHAR(50) NOT NULL,
    equipment_name VARCHAR(100),
    period_start TIMESTAMPTZ NOT NULL,
    period_end TIMESTAMPTZ NOT NULL,
    availability NUMERIC(6,2) NOT NULL DEFAULT 0,
    performance NUMERIC(6,2) NOT NULL DEFAULT 0,
    quality NUMERIC(6,2) NOT NULL DEFAULT 0,
    oee NUMERIC(6,2) NOT NULL DEFAULT 0,
    planned_production_time NUMERIC(18,2) DEFAULT 0,
    actual_run_time NUMERIC(18,2) DEFAULT 0,
    downtime NUMERIC(18,2) DEFAULT 0,
    ideal_run_rate NUMERIC(18,4) DEFAULT 0,
    actual_output NUMERIC(18,4) DEFAULT 0,
    good_output NUMERIC(18,4) DEFAULT 0,
    defect_output NUMERIC(18,4) DEFAULT 0,
    downtime_records_json JSONB DEFAULT '[]',
    loss_categories_json JSONB DEFAULT '[]',
    calculated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX ix_oee_results_equipment_period ON oee_results (equipment_code, period_start DESC);
CREATE INDEX ix_oee_results_period ON oee_results (period_start DESC);

-- 5. 停机记录表
CREATE TABLE downtime_records (
    id BIGSERIAL PRIMARY KEY,
    equipment_code VARCHAR(50) NOT NULL,
    equipment_name VARCHAR(100),
    start_time TIMESTAMPTZ NOT NULL,
    end_time TIMESTAMPTZ,
    reason VARCHAR(200),
    category VARCHAR(50),
    duration_minutes NUMERIC(18,2),
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX ix_downtime_records_equipment_time ON downtime_records (equipment_code, start_time DESC);

-- 6. 能耗记录表
CREATE TABLE energy_records (
    id BIGSERIAL PRIMARY KEY,
    meter_code VARCHAR(50) NOT NULL,
    meter_name VARCHAR(100) NOT NULL,
    energy_type INT NOT NULL DEFAULT 1,
    reading NUMERIC(18,4) NOT NULL,
    consumption NUMERIC(18,4) NOT NULL DEFAULT 0,
    cost NUMERIC(18,4) DEFAULT 0,
    timestamp TIMESTAMPTZ NOT NULL,
    recorded_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX ix_energy_records_meter_time ON energy_records (meter_code, timestamp DESC);
CREATE INDEX ix_energy_records_energy_type_time ON energy_records (energy_type, timestamp DESC);
CREATE INDEX ix_energy_records_timestamp ON energy_records (timestamp DESC);

-- 7. 能耗统计缓存表
CREATE TABLE energy_statistics_cache (
    id SERIAL PRIMARY KEY,
    energy_type INT NOT NULL,
    meter_code VARCHAR(50),
    period_start TIMESTAMPTZ NOT NULL,
    period_end TIMESTAMPTZ NOT NULL,
    period_type INT NOT NULL,
    total_consumption NUMERIC(18,4) DEFAULT 0,
    total_cost NUMERIC(18,4) DEFAULT 0,
    avg_consumption_per_hour NUMERIC(18,4) DEFAULT 0,
    peak_consumption NUMERIC(18,4) DEFAULT 0,
    peak_time TIMESTAMPTZ,
    min_consumption NUMERIC(18,4) DEFAULT 0,
    min_time TIMESTAMPTZ,
    by_equipment_json JSONB DEFAULT '{}',
    by_area_json JSONB DEFAULT '{}',
    hourly_data_json JSONB DEFAULT '{}',
    daily_data_json JSONB DEFAULT '{}',
    consumption_per_ton NUMERIC(18,4) DEFAULT 0,
    consumption_per_batch NUMERIC(18,4) DEFAULT 0,
    yoy_change_percent NUMERIC(10,2) DEFAULT 0,
    mom_change_percent NUMERIC(10,2) DEFAULT 0,
    calculated_at TIMESTAMPTZ DEFAULT NOW(),
    CONSTRAINT uq_energy_statistics_cache UNIQUE (energy_type, meter_code, period_start, period_end, period_type)
);

CREATE INDEX ix_energy_statistics_cache_type_period ON energy_statistics_cache (energy_type, period_start DESC);

-- 8. 报表模板表
CREATE TABLE report_templates (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    description VARCHAR(500),
    report_type INT NOT NULL DEFAULT 2,
    template_content JSONB,
    is_system BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    created_by VARCHAR(100)
);

-- 9. 报表生成任务表
CREATE TABLE report_generation_tasks (
    id SERIAL PRIMARY KEY,
    task_name VARCHAR(100) NOT NULL,
    template_id INT,
    report_type INT NOT NULL DEFAULT 2,
    scheduled_time TIMESTAMPTZ NOT NULL,
    start_time TIMESTAMPTZ,
    end_time TIMESTAMPTZ,
    status INT NOT NULL DEFAULT 1,
    error_message VARCHAR(1000),
    output_path VARCHAR(500),
    parameters_json JSONB DEFAULT '{}',
    created_at TIMESTAMPTZ DEFAULT NOW(),
    CONSTRAINT fk_report_generation_tasks_template FOREIGN KEY (template_id) REFERENCES report_templates(id)
);

CREATE INDEX ix_report_generation_tasks_scheduled ON report_generation_tasks (scheduled_time, status);

-- 触发器函数
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- 10. 初始化数据
INSERT INTO shift_definitions (name, start_time, end_time, color) VALUES
('白班', '08:00:00', '20:00:00', '#FF9800'),
('夜班', '20:00:00', '08:00:00', '#3F51B5');

INSERT INTO report_templates (name, description, report_type, template_content, is_system, created_by) VALUES
('标准班报模板', '标准生产班报，包含产量、能耗、质量、设备运行', 1,
'{"sections":[{"title":"生产概况","type":"summary"},{"title":"批次明细","type":"batches"},{"title":"设备运行","type":"equipment"},{"title":"能耗统计","type":"energy"},{"title":"质量分析","type":"quality"},{"title":"报警汇总","type":"alarms"}]}', TRUE, 'SYSTEM'),

('标准日报模板', '标准生产日报，包含全天汇总', 2,
'{"sections":[{"title":"生产概况","type":"summary"},{"title":"班次对比","type":"shift_comparison"},{"title":"批次明细","type":"batches"},{"title":"设备运行","type":"equipment"},{"title":"能耗统计","type":"energy"},{"title":"质量分析","type":"quality"},{"title":"报警汇总","type":"alarms"}]}', TRUE, 'SYSTEM'),

('标准月报模板', '标准生产月报，包含趋势分析', 3,
'{"sections":[{"title":"月度概况","type":"summary"},{"title":"日产量趋势","type":"daily_trend"},{"title":"设备OEE趋势","type":"oee_trend"},{"title":"能耗趋势","type":"energy_trend"},{"title":"质量趋势","type":"quality_trend"},{"title":"Top问题分析","type":"top_issues"}]}', TRUE, 'SYSTEM'),

('OEE分析报表', '设备OEE详细分析报表', 2,
'{"sections":[{"title":"OEE概况","type":"oee_summary"},{"title":"可用率分析","type":"availability"},{"title":"性能分析","type":"performance"},{"title":"质量分析","type":"quality_rate"},{"title":"六大损失分析","type":"six_losses"},{"title":"停机原因分析","type":"downtime_analysis"}]}', TRUE, 'SYSTEM'),

('能耗分析报表', '能耗详细分析报表', 2,
'{"sections":[{"title":"能耗概况","type":"energy_summary"},{"title":"分项能耗","type":"energy_breakdown"},{"title":"能耗趋势","type":"energy_trend"},{"title":"单耗分析","type":"specific_consumption"},{"title":"同比环比分析","type":"yoy_mom"},{"title":"节能建议","type":"saving_suggestions"}]}', TRUE, 'SYSTEM');

-- 完成
-- SELECT 'TwoD5DMonitoring 报表系统数据库迁移完成 (PostgreSQL)' AS message;