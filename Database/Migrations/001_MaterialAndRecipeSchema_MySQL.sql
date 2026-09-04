-- =====================================================
-- TwoD5DMonitoring 数据库迁移脚本 - MySQL 版本
-- 版本: 1.1.0 - 物料管理与配方管理模块
-- =====================================================

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- 1. 物料分类表
CREATE TABLE `material_categories` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `code` VARCHAR(50) NOT NULL,
    `name` VARCHAR(100) NOT NULL,
    `description` VARCHAR(500) DEFAULT NULL,
    `sort_order` INT DEFAULT 0,
    `is_active` TINYINT(1) DEFAULT 1,
    `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_material_categories_code` (`code`),
    KEY `ix_material_categories_is_active` (`is_active`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='物料分类表';

-- 2. 物料主数据表
CREATE TABLE `materials` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `code` VARCHAR(50) NOT NULL,
    `name` VARCHAR(100) NOT NULL,
    `specification` VARCHAR(200) DEFAULT NULL,
    `unit` VARCHAR(20) NOT NULL,
    `material_type` INT NOT NULL DEFAULT 1 COMMENT '1=原材料, 2=半成品, 3=成品, 4=添加剂, 5=包材, 99=其他',
    `status` INT NOT NULL DEFAULT 1 COMMENT '1=启用, 2=停用, 3=废弃, 4=待审核',
    `category_id` INT DEFAULT NULL,
    `density` DECIMAL(10,4) DEFAULT 0,
    `moisture_content` DECIMAL(6,2) DEFAULT 0,
    `particle_size` DECIMAL(10,2) DEFAULT 0,
    `color` VARCHAR(50) DEFAULT NULL,
    `manufacturer` VARCHAR(100) DEFAULT NULL,
    `supplier` VARCHAR(100) DEFAULT NULL,
    `standard_cost` DECIMAL(18,4) DEFAULT 0,
    `remark` VARCHAR(1000) DEFAULT NULL,
    `is_weighable` TINYINT(1) DEFAULT 1,
    `is_batch_managed` TINYINT(1) DEFAULT 1,
    `shelf_life_days` INT DEFAULT 0,
    `storage_condition` VARCHAR(200) DEFAULT NULL,
    `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    `created_by` VARCHAR(100) DEFAULT NULL,
    `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by` VARCHAR(100) DEFAULT NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_materials_code` (`code`),
    KEY `ix_materials_name` (`name`),
    KEY `ix_materials_material_type` (`material_type`),
    KEY `ix_materials_status` (`status`),
    KEY `ix_materials_category_id` (`category_id`),
    KEY `ix_materials_is_weighable` (`is_weighable`),
    CONSTRAINT `fk_materials_category` FOREIGN KEY (`category_id`) REFERENCES `material_categories` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='物料主数据表';

-- 3. 物料扩展属性表
CREATE TABLE `material_properties` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `material_id` INT NOT NULL,
    `name` VARCHAR(100) NOT NULL,
    `value` VARCHAR(500) DEFAULT NULL,
    `unit` VARCHAR(20) DEFAULT NULL,
    `data_type` VARCHAR(20) DEFAULT 'String',
    `sort_order` INT DEFAULT 0,
    `is_required` TINYINT(1) DEFAULT 0,
    `validation_rule` JSON DEFAULT NULL,
    PRIMARY KEY (`id`),
    KEY `ix_material_properties_material_id` (`material_id`),
    CONSTRAINT `fk_material_properties_material` FOREIGN KEY (`material_id`) REFERENCES `materials` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='物料扩展属性表';

-- 4. 罐体物料分配表
CREATE TABLE `tank_materials` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `tank_id` INT NOT NULL COMMENT '关联设备ID',
    `tank_code` VARCHAR(50) DEFAULT NULL COMMENT '罐体编码(冗余)',
    `tank_name` VARCHAR(100) DEFAULT NULL COMMENT '罐体名称(冗余)',
    `tank_type` INT DEFAULT NULL COMMENT '罐体类型(冗余)',
    `material_id` INT NOT NULL,
    `is_primary` TINYINT(1) DEFAULT 1,
    `priority` INT DEFAULT 1,
    `max_capacity` DECIMAL(18,4) DEFAULT 0,
    `min_level` DECIMAL(18,4) DEFAULT 0,
    `max_level` DECIMAL(18,4) DEFAULT 0,
    `remark` VARCHAR(500) DEFAULT NULL,
    `effective_from` DATETIME DEFAULT CURRENT_TIMESTAMP,
    `effective_to` DATETIME DEFAULT NULL,
    `is_active` TINYINT(1) DEFAULT 1,
    `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    `created_by` VARCHAR(100) DEFAULT NULL,
    `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by` VARCHAR(100) DEFAULT NULL,
    PRIMARY KEY (`id`),
    KEY `ix_tank_materials_tank_id` (`tank_id`),
    KEY `ix_tank_materials_material_id` (`material_id`),
    KEY `ix_tank_materials_is_active` (`is_active`),
    CONSTRAINT `fk_tank_materials_material` FOREIGN KEY (`material_id`) REFERENCES `materials` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='罐体物料分配表';

-- 5. 罐体物料变更历史表
CREATE TABLE `tank_material_history` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `tank_id` INT NOT NULL,
    `tank_code` VARCHAR(50) DEFAULT NULL,
    `tank_name` VARCHAR(100) DEFAULT NULL,
    `old_material_id` INT DEFAULT NULL,
    `new_material_id` INT DEFAULT NULL,
    `change_reason` VARCHAR(500) DEFAULT NULL,
    `operator` VARCHAR(100) DEFAULT NULL,
    `change_time` DATETIME DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    KEY `ix_tank_material_history_tank_id` (`tank_id`),
    KEY `ix_tank_material_history_change_time` (`change_time`),
    CONSTRAINT `fk_tank_material_history_old_material` FOREIGN KEY (`old_material_id`) REFERENCES `materials` (`id`) ON DELETE SET NULL,
    CONSTRAINT `fk_tank_material_history_new_material` FOREIGN KEY (`new_material_id`) REFERENCES `materials` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='罐体物料变更历史表';

-- 6. 配方基础表
CREATE TABLE `recipes` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `code` VARCHAR(50) NOT NULL,
    `name` VARCHAR(100) NOT NULL,
    `recipe_type` INT NOT NULL DEFAULT 1 COMMENT '1=称量, 2=混炼, 3=挤出, 4=压延, 99=其他',
    `status` INT NOT NULL DEFAULT 1 COMMENT '1=草稿, 2=待审核, 3=已审核, 4=已发布, 5=已归档, 6=作废',
    `product_code` VARCHAR(50) DEFAULT NULL,
    `product_name` VARCHAR(100) DEFAULT NULL,
    `batch_size` DECIMAL(18,4) DEFAULT 1000,
    `yield_rate` DECIMAL(6,2) DEFAULT 100,
    `remark` VARCHAR(2000) DEFAULT NULL,
    `current_version` VARCHAR(50) DEFAULT '1.0',
    `current_version_id` INT DEFAULT NULL,
    `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    `created_by` VARCHAR(100) DEFAULT NULL,
    `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by` VARCHAR(100) DEFAULT NULL,
    `approved_at` DATETIME DEFAULT NULL,
    `approved_by` VARCHAR(100) DEFAULT NULL,
    `released_at` DATETIME DEFAULT NULL,
    `released_by` VARCHAR(100) DEFAULT NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_recipes_code` (`code`),
    KEY `ix_recipes_recipe_type` (`recipe_type`),
    KEY `ix_recipes_status` (`status`),
    KEY `ix_recipes_product_code` (`product_code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='配方基础表';

-- 7. 配方版本表
CREATE TABLE `recipe_versions` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `recipe_id` INT NOT NULL,
    `version` VARCHAR(50) NOT NULL,
    `change_description` VARCHAR(1000) DEFAULT NULL,
    `is_current` TINYINT(1) DEFAULT 1,
    `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    `created_by` VARCHAR(100) DEFAULT NULL,
    PRIMARY KEY (`id`),
    KEY `ix_recipe_versions_recipe_id` (`recipe_id`),
    KEY `ix_recipe_versions_is_current` (`is_current`),
    CONSTRAINT `fk_recipe_versions_recipe` FOREIGN KEY (`recipe_id`) REFERENCES `recipes` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='配方版本表';

-- 8. 称量配方扩展表
CREATE TABLE `weighing_recipes` (
    `recipe_id` INT NOT NULL,
    `weighing_mode` INT DEFAULT 1 COMMENT '1=顺序, 2=同时, 3=手工',
    `scale_id` INT DEFAULT NULL,
    `total_weight` DECIMAL(18,4) DEFAULT 0,
    `decimal_places` INT DEFAULT 2,
    `allow_manual_adjust` TINYINT(1) DEFAULT 1,
    PRIMARY KEY (`recipe_id`),
    CONSTRAINT `fk_weighing_recipes_recipe` FOREIGN KEY (`recipe_id`) REFERENCES `recipes` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='称量配方扩展表';

-- 9. 称量配方明细行
CREATE TABLE `weighing_recipe_lines` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `recipe_id` INT NOT NULL,
    `line_number` INT NOT NULL,
    `material_id` INT NOT NULL,
    `target_weight` DECIMAL(18,4) NOT NULL,
    `tolerance_positive` DECIMAL(6,2) DEFAULT 1.0,
    `tolerance_negative` DECIMAL(6,2) DEFAULT 1.0,
    `weighing_order` INT DEFAULT 0,
    `is_critical` TINYINT(1) DEFAULT 1,
    `remark` VARCHAR(500) DEFAULT NULL,
    `feeder_type` INT DEFAULT 1 COMMENT '1=自动, 2=半自动, 3=手工, 4=真空, 5=螺旋',
    `plc_tag_index` INT DEFAULT NULL,
    `plc_weight_tag` VARCHAR(200) DEFAULT NULL,
    `plc_material_tag` VARCHAR(200) DEFAULT NULL,
    PRIMARY KEY (`id`),
    KEY `ix_weighing_recipe_lines_recipe_id` (`recipe_id`),
    KEY `ix_weighing_recipe_lines_material_id` (`material_id`),
    UNIQUE KEY `uq_weighing_recipe_lines_recipe_line_number` (`recipe_id`, `line_number`),
    CONSTRAINT `fk_weighing_recipe_lines_recipe` FOREIGN KEY (`recipe_id`) REFERENCES `recipes` (`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_weighing_recipe_lines_material` FOREIGN KEY (`material_id`) REFERENCES `materials` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='称量配方明细行';

-- 10. 混炼配方扩展表
CREATE TABLE `mixing_recipes` (
    `recipe_id` INT NOT NULL,
    `mixer_id` INT DEFAULT NULL,
    `process_type` INT DEFAULT 1 COMMENT '1=密炼机, 2=内混机, 3=开炼机, 4=连续混炼',
    `total_steps` INT DEFAULT 0,
    `target_temperature` DECIMAL(6,2) DEFAULT 0,
    `target_time` INT DEFAULT 0,
    `dump_temperature` INT DEFAULT 0,
    `ram_pressure` DECIMAL(10,2) DEFAULT 0,
    `ram_position` INT DEFAULT 0,
    `rotor_speed` INT DEFAULT 0,
    `enable_temp_control` TINYINT(1) DEFAULT 1,
    `enable_energy_control` TINYINT(1) DEFAULT 0,
    PRIMARY KEY (`recipe_id`),
    CONSTRAINT `fk_mixing_recipes_recipe` FOREIGN KEY (`recipe_id`) REFERENCES `recipes` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='混炼配方扩展表';

-- 11. 混炼配方步骤表
CREATE TABLE `mixing_recipe_steps` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `recipe_id` INT NOT NULL,
    `step_number` INT NOT NULL,
    `step_type` INT DEFAULT 1 COMMENT '1=投料, 2=混炼, 3=升温, 4=降温, 5=出料, 6=等待, 99=自定义',
    `material_id` INT DEFAULT NULL,
    `amount` DECIMAL(18,4) DEFAULT 0,
    `add_time` INT DEFAULT 0 COMMENT '相对步骤开始的投料时间(秒)',
    `duration` INT DEFAULT 0 COMMENT '步骤持续时间(秒)',
    `target_temp` INT DEFAULT 0,
    `rotor_speed` INT DEFAULT 0,
    `ram_pressure` DECIMAL(10,2) DEFAULT 0,
    `is_auto_dump` TINYINT(1) DEFAULT 0,
    `wait_for_temp` TINYINT(1) DEFAULT 0,
    `condition` VARCHAR(500) DEFAULT NULL COMMENT '跳转条件表达式',
    `remark` VARCHAR(500) DEFAULT NULL,
    `plc_step_index` INT DEFAULT NULL,
    `plc_command_tag` VARCHAR(200) DEFAULT NULL,
    `plc_param_tags` JSON DEFAULT NULL,
    PRIMARY KEY (`id`),
    KEY `ix_mixing_recipe_steps_recipe_id` (`recipe_id`),
    KEY `ix_mixing_recipe_steps_material_id` (`material_id`),
    UNIQUE KEY `uq_mixing_recipe_steps_recipe_step_number` (`recipe_id`, `step_number`),
    CONSTRAINT `fk_mixing_recipe_steps_recipe` FOREIGN KEY (`recipe_id`) REFERENCES `recipes` (`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_mixing_recipe_steps_material` FOREIGN KEY (`material_id`) REFERENCES `materials` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='混炼配方步骤表';

-- 12. 配方下发日志表
CREATE TABLE `recipe_download_logs` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `recipe_id` INT NOT NULL,
    `recipe_code` VARCHAR(50) NOT NULL,
    `recipe_name` VARCHAR(100) NOT NULL,
    `recipe_type` INT NOT NULL,
    `version` VARCHAR(50) NOT NULL,
    `target_plc` VARCHAR(100) NOT NULL,
    `status` INT DEFAULT 1 COMMENT '1=待下发, 2=下发中, 3=成功, 4=失败, 5=部分成功, 6=已校验',
    `error_message` VARCHAR(1000) DEFAULT NULL,
    `operator` VARCHAR(100) DEFAULT NULL,
    `start_time` DATETIME DEFAULT CURRENT_TIMESTAMP,
    `end_time` DATETIME DEFAULT NULL,
    `download_data` JSON DEFAULT NULL,
    PRIMARY KEY (`id`),
    KEY `ix_recipe_download_logs_recipe_id` (`recipe_id`),
    KEY `ix_recipe_download_logs_target_plc` (`target_plc`),
    KEY `ix_recipe_download_logs_start_time` (`start_time`),
    KEY `ix_recipe_download_logs_status` (`status`),
    CONSTRAINT `fk_recipe_download_logs_recipe` FOREIGN KEY (`recipe_id`) REFERENCES `recipes` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='配方下发日志表';

-- 13. 批次生产记录表
CREATE TABLE `batch_records` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `batch_number` VARCHAR(50) NOT NULL,
    `recipe_id` INT NOT NULL,
    `recipe_code` VARCHAR(50) NOT NULL,
    `recipe_version` VARCHAR(50) NOT NULL,
    `mixer_id` INT DEFAULT NULL,
    `scale_id` INT DEFAULT NULL,
    `status` INT DEFAULT 1 COMMENT '1=已创建, 2=称量中, 3=称量完成, 4=混炼中, 5=混炼完成, 6=已出料, 7=完成, 8=中止, 9=暂停',
    `actual_weight` DECIMAL(18,4) DEFAULT 0,
    `target_weight` DECIMAL(18,4) DEFAULT 0,
    `start_time` DATETIME DEFAULT CURRENT_TIMESTAMP,
    `end_time` DATETIME DEFAULT NULL,
    `operator` VARCHAR(100) DEFAULT NULL,
    `shift` VARCHAR(50) DEFAULT NULL,
    `remark` VARCHAR(1000) DEFAULT NULL,
    `weighing_details` JSON DEFAULT NULL,
    `mixing_details` JSON DEFAULT NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_batch_records_batch_number` (`batch_number`),
    KEY `ix_batch_records_recipe_id` (`recipe_id`),
    KEY `ix_batch_records_status` (`status`),
    KEY `ix_batch_records_start_time` (`start_time`),
    CONSTRAINT `fk_batch_records_recipe` FOREIGN KEY (`recipe_id`) REFERENCES `recipes` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='批次生产记录表';

-- 14. 初始化基础物料分类数据
INSERT INTO `material_categories` (`code`, `name`, `description`, `sort_order`, `is_active`) VALUES
('RM', '橡胶原料', '天然橡胶、合成橡胶等', 1, 1),
('CB', '炭黑', '各类炭黑产品', 2, 1),
('OIL', '油类', '工艺油、软化剂等', 3, 1),
('CHEM', '化学品', '硫化剂、促进剂、防老剂等', 4, 1),
('ADD', '添加剂', '填充剂、增塑剂等', 5, 1),
('PKG', '包装材料', '编织袋、内衬袋等', 6, 1),
('SFG', '半成品', '母胶、混炼胶等', 7, 1),
('FG', '成品', '最终成品胶料', 8, 1),
('OTH', '其他', '其他辅助材料', 99, 1);

-- 15. 初始化示例物料数据
INSERT INTO `materials` (`code`, `name`, `specification`, `unit`, `material_type`, `status`, `category_id`, `density`, `moisture_content`, `particle_size`, `is_weighable`, `is_batch_managed`, `created_by`) VALUES
('NR-001', '天然橡胶 STR 20', 'STR 20级', 'KG', 1, 1, 1, 0.92, 0.5, 0, 1, 1, 'SYSTEM'),
('SBR-001', '丁苯橡胶 1502', '1502型', 'KG', 1, 1, 1, 0.94, 0.3, 0, 1, 1, 'SYSTEM'),
('BR-001', '顺丁橡胶 BR 9000', 'BR 9000', 'KG', 1, 1, 1, 0.91, 0.2, 0, 1, 1, 'SYSTEM'),
('CB-N330', '炭黑 N330', 'N330 高耐磨', 'KG', 1, 1, 2, 1.85, 0.5, 30, 1, 1, 'SYSTEM'),
('CB-N550', '炭黑 N550', 'N550 半补强', 'KG', 1, 1, 2, 1.82, 0.5, 40, 1, 1, 'SYSTEM'),
('OIL-ARA', '芳香族油', '工艺用芳香油', 'KG', 1, 1, 3, 0.98, 0.1, 0, 1, 1, 'SYSTEM'),
('SUL-001', '硫磺', '工业级硫磺', 'KG', 4, 1, 4, 2.07, 0.1, 200, 1, 1, 'SYSTEM'),
('ACC-TBBS', '促进剂 TBBS', 'N-叔丁基-2-苯并噻唑次磺酰胺', 'KG', 4, 1, 4, 1.35, 0.2, 150, 1, 1, 'SYSTEM'),
('ANT-4010', '防老剂 4010', 'N-异丙基-N'-苯基对苯二胺', 'KG', 4, 1, 4, 1.15, 0.3, 100, 1, 1, 'SYSTEM'),
('ZNO-001', '氧化锌', '活性氧化锌', 'KG', 4, 1, 4, 5.6, 0.5, 50, 1, 1, 'SYSTEM'),
('STEARIC', '硬脂酸', '工业级硬脂酸', 'KG', 4, 1, 4, 0.94, 0.2, 0, 1, 1, 'SYSTEM');

-- 完成
SELECT 'TwoD5DMonitoring 物料与配方数据库迁移完成 (MySQL)' AS `message`;

SET FOREIGN_KEY_CHECKS = 1;