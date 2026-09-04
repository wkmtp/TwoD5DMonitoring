-- =====================================================
-- TwoD5DMonitoring 报表系统数据库迁移脚本 - MySQL
-- 版本: 1.3.0
-- =====================================================

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- 1. 历史趋势数据表
CREATE TABLE `historical_trend_data` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `tag_name` VARCHAR(100) NOT NULL,
    `value` DECIMAL(18,4) NOT NULL,
    `quality` INT NOT NULL DEFAULT 192,
    `timestamp` DATETIME NOT NULL,
    PRIMARY KEY (`id`),
    KEY `ix_historical_trend_data_tag_time` (`tag_name`, `timestamp` DESC),
    KEY `ix_historical_trend_data_timestamp` (`timestamp` DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='历史趋势数据表';

-- 2. 班次定义表
CREATE TABLE `shift_definitions` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `name` VARCHAR(50) NOT NULL,
    `start_time` TIME NOT NULL,
    `end_time` TIME NOT NULL,
    `color` VARCHAR(20) DEFAULT '#2196F3',
    `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='班次定义表';

INSERT INTO `shift_definitions` (`name`, `start_time`, `end_time`, `color`) VALUES
('白班', '08:00:00', '20:00:00', '#FF9800'),
('夜班', '20:00:00', '08:00:00', '#3F51B5');

-- 3. 生产报表主表
CREATE TABLE `production_reports` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `report_number` VARCHAR(50) NOT NULL,
    `report_type` INT NOT NULL DEFAULT 2 COMMENT '1=Shift, 2=Daily, 3=Monthly, 4=Yearly',
    `period_start` DATETIME NOT NULL,
    `period_end` DATETIME NOT NULL,
    `shift_id` INT DEFAULT NULL,
    `shift_name` VARCHAR(50) DEFAULT NULL,
    `status` INT NOT NULL DEFAULT 1 COMMENT '1=Draft, 2=Generated, 3=Approved, 4=Published, 5=Archived',
    `generated_by` VARCHAR(100) DEFAULT NULL,
    `generated_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    `approved_at` DATETIME DEFAULT NULL,
    `approved_by` VARCHAR(100) DEFAULT NULL,
    `summary_data_json` JSON DEFAULT '{}',
    `detail_data_json` JSON DEFAULT '{}',
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_production_reports_number` (`report_number`),
    KEY `ix_production_reports_type_period` (`report_type`, `period_start` DESC),
    KEY `ix_production_reports_status` (`status`),
    KEY `ix_production_reports_shift` (`shift_id`),
    CONSTRAINT `fk_production_reports_shift` FOREIGN KEY (`shift_id`) REFERENCES `shift_definitions` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='生产报表主表';

-- 4. OEE 结果表
CREATE TABLE `oee_results` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `equipment_code` VARCHAR(50) NOT NULL,
    `equipment_name` VARCHAR(100) DEFAULT NULL,
    `period_start` DATETIME NOT NULL,
    `period_end` DATETIME NOT NULL,
    `availability` DECIMAL(6,2) NOT NULL DEFAULT 0,
    `performance` DECIMAL(6,2) NOT NULL DEFAULT 0,
    `quality` DECIMAL(6,2) NOT NULL DEFAULT 0,
    `oee` DECIMAL(6,2) NOT NULL DEFAULT 0,
    `planned_production_time` DECIMAL(18,2) DEFAULT 0,
    `actual_run_time` DECIMAL(18,2) DEFAULT 0,
    `downtime` DECIMAL(18,2) DEFAULT 0,
    `ideal_run_rate` DECIMAL(18,4) DEFAULT 0,
    `actual_output` DECIMAL(18,4) DEFAULT 0,
    `good_output` DECIMAL(18,4) DEFAULT 0,
    `defect_output` DECIMAL(18,4) DEFAULT 0,
    `downtime_records_json` JSON DEFAULT '[]',
    `loss_categories_json` JSON DEFAULT '[]',
    `calculated_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    KEY `ix_oee_results_equipment_period` (`equipment_code`, `period_start` DESC),
    KEY `ix_oee_results_period` (`period_start` DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='OEE 结果表';

-- 5. 停机记录表
CREATE TABLE `downtime_records` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `equipment_code` VARCHAR(50) NOT NULL,
    `equipment_name` VARCHAR(100) DEFAULT NULL,
    `start_time` DATETIME NOT NULL,
    `end_time` DATETIME DEFAULT NULL,
    `reason` VARCHAR(200) DEFAULT NULL,
    `category` VARCHAR(50) DEFAULT NULL,
    `duration_minutes` DECIMAL(18,2) DEFAULT NULL,
    `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    KEY `ix_downtime_records_equipment_time` (`equipment_code`, `start_time` DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='停机记录表';

-- 6. 能耗记录表
CREATE TABLE `energy_records` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `meter_code` VARCHAR(50) NOT NULL,
    `meter_name` VARCHAR(100) NOT NULL,
    `energy_type` INT NOT NULL DEFAULT 1 COMMENT '1=Electricity, 2=Gas, 3=Water, 4=Steam, 5=CompressedAir',
    `reading` DECIMAL(18,4) NOT NULL,
    `consumption` DECIMAL(18,4) NOT NULL DEFAULT 0,
    `cost` DECIMAL(18,4) DEFAULT 0,
    `timestamp` DATETIME NOT NULL,
    `recorded_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    KEY `ix_energy_records_meter_time` (`meter_code`, `timestamp` DESC),
    KEY `ix_energy_records_energy_type_time` (`energy_type`, `timestamp` DESC),
    KEY `ix_energy_records_timestamp` (`timestamp` DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='能耗记录表';

-- 7. 能耗统计缓存表
CREATE TABLE `energy_statistics_cache` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `energy_type` INT NOT NULL,
    `meter_code` VARCHAR(50) DEFAULT NULL,
    `period_start` DATETIME NOT NULL,
    `period_end` DATETIME NOT NULL,
    `period_type` INT NOT NULL COMMENT '1=Minute, 2=Hour, 3=Shift, 4=Day, 5=Month',
    `total_consumption` DECIMAL(18,4) DEFAULT 0,
    `total_cost` DECIMAL(18,4) DEFAULT 0,
    `avg_consumption_per_hour` DECIMAL(18,4) DEFAULT 0,
    `peak_consumption` DECIMAL(18,4) DEFAULT 0,
    `peak_time` DATETIME DEFAULT NULL,
    `min_consumption` DECIMAL(18,4) DEFAULT 0,
    `min_time` DATETIME DEFAULT NULL,
    `by_equipment_json` JSON DEFAULT '{}',
    `by_area_json` JSON DEFAULT '{}',
    `hourly_data_json` JSON DEFAULT '{}',
    `daily_data_json` JSON DEFAULT '{}',
    `consumption_per_ton` DECIMAL(18,4) DEFAULT 0,
    `consumption_per_batch` DECIMAL(18,4) DEFAULT 0,
    `yoy_change_percent` DECIMAL(10,2) DEFAULT 0,
    `mom_change_percent` DECIMAL(10,2) DEFAULT 0,
    `calculated_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_energy_statistics_cache` (`energy_type`, `meter_code`, `period_start`, `period_end`, `period_type`),
    KEY `ix_energy_statistics_cache_type_period` (`energy_type`, `period_start` DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='能耗统计缓存表';

-- 8. 报表模板表
CREATE TABLE `report_templates` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `name` VARCHAR(100) NOT NULL,
    `description` VARCHAR(500) DEFAULT NULL,
    `report_type` INT NOT NULL DEFAULT 2,
    `template_content` JSON DEFAULT NULL,
    `is_system` TINYINT(1) DEFAULT 0,
    `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    `created_by` VARCHAR(100) DEFAULT NULL,
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='报表模板表';

-- 9. 报表生成任务表
CREATE TABLE `report_generation_tasks` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `task_name` VARCHAR(100) NOT NULL,
    `template_id` INT DEFAULT NULL,
    `report_type` INT NOT NULL DEFAULT 2,
    `scheduled_time` DATETIME NOT NULL,
    `start_time` DATETIME DEFAULT NULL,
    `end_time` DATETIME DEFAULT NULL,
    `status` INT NOT NULL DEFAULT 1 COMMENT '1=Pending, 2=Running, 3=Completed, 4=Failed, 5=Cancelled',
    `error_message` VARCHAR(1000) DEFAULT NULL,
    `output_path` VARCHAR(500) DEFAULT NULL,
    `parameters_json` JSON DEFAULT '{}',
    `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    KEY `ix_report_generation_tasks_scheduled` (`scheduled_time`, `status`),
    CONSTRAINT `fk_report_generation_tasks_template` FOREIGN KEY (`template_id`) REFERENCES `report_templates` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='报表生成任务表';

-- 10. 初始化数据
INSERT INTO `shift_definitions` (`name`, `start_time`, `end_time`, `color`) VALUES
('白班', '08:00:00', '20:00:00', '#FF9800'),
('夜班', '20:00:00', '08:00:00', '#3F51B5');

INSERT INTO `report_templates` (`name`, `description`, `report_type`, `template_content`, `is_system`, `created_by`) VALUES
('标准班报模板', '标准生产班报，包含产量、能耗、质量、设备运行', 1,
'{"sections":[{"title":"生产概况","type":"summary"},{"title":"批次明细","type":"batches"},{"title":"设备运行","type":"equipment"},{"title":"能耗统计","type":"energy"},{"title":"质量分析","type":"quality"},{"title":"报警汇总","type":"alarms"}]}', 1, 'SYSTEM'),

('标准日报模板', '标准生产日报，包含全天汇总', 2,
'{"sections":[{"title":"生产概况","type":"summary"},{"title":"班次对比","type":"shift_comparison"},{"title":"批次明细","type":"batches"},{"title":"设备运行","type":"equipment"},{"title":"能耗统计","type":"energy"},{"title":"质量分析","type":"quality"},{"title":"报警汇总","type":"alarms"}]}', 1, 'SYSTEM'),

('标准月报模板', '标准生产月报，包含趋势分析', 3,
'{"sections":[{"title":"月度概况","type":"summary"},{"title":"日产量趋势","type":"daily_trend"},{"title":"设备OEE趋势","type":"oee_trend"},{"title":"能耗趋势","type":"energy_trend"},{"title":"质量趋势","type":"quality_trend"},{"title":"Top问题分析","type":"top_issues"}]}', 1, 'SYSTEM'),

('OEE分析报表', '设备OEE详细分析报表', 2,
'{"sections":[{"title":"OEE概况","type":"oee_summary"},{"title":"可用率分析","type":"availability"},{"title":"性能分析","type":"performance"},{"title":"质量分析","type":"quality_rate"},{"title":"六大损失分析","type":"six_losses"},{"title":"停机原因分析","type":"downtime_analysis"}]}', 1, 'SYSTEM'),

('能耗分析报表', '能耗详细分析报表', 2,
'{"sections":[{"title":"能耗概况","type":"energy_summary"},{"title":"分项能耗","type":"energy_breakdown"},{"title":"能耗趋势","type":"energy_trend"},{"title":"单耗分析","type":"specific_consumption"},{"title":"同比环比分析","type":"yoy_mom"},{"title":"节能建议","type":"saving_suggestions"}]}', 1, 'SYSTEM');

-- 完成
SELECT 'TwoD5DMonitoring 报表系统数据库迁移完成 (MySQL)' AS `message`;

SET FOREIGN_KEY_CHECKS = 1;