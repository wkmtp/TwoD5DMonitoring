-- =====================================================
-- TwoD5DMonitoring 报警管理数据库迁移脚本 - MySQL
-- 版本: 1.2.0
-- =====================================================

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- 1. 报警组表
CREATE TABLE `alarm_groups` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `name` VARCHAR(50) NOT NULL,
    `description` VARCHAR(500) DEFAULT NULL,
    `is_active` TINYINT(1) DEFAULT 1,
    `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='报警组表';

-- 2. 报警定义表
CREATE TABLE `alarm_definitions` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `tag_name` VARCHAR(100) NOT NULL,
    `alarm_name` VARCHAR(100) NOT NULL,
    `alarm_type` INT NOT NULL DEFAULT 1 COMMENT '1=HiHi, 2=Hi, 3=Lo, 4=LoLo, 5=ROC, 6=Dev, 7=Digital, 8=System, 9=Communication, 99=Custom',
    `priority` INT NOT NULL DEFAULT 2 COMMENT '1=Low, 2=Medium, 3=High, 4=Critical',
    `category` INT NOT NULL DEFAULT 1 COMMENT '1=Process, 2=Equipment, 3=Quality, 4=Safety, 5=Environmental, 6=Maintenance, 7=System',
    `setpoint` DECIMAL(18,4) NOT NULL DEFAULT 0,
    `deadband` DECIMAL(10,4) DEFAULT 0.5,
    `delay_seconds` INT DEFAULT 0,
    `message` VARCHAR(500) DEFAULT NULL,
    `help_text` VARCHAR(1000) DEFAULT NULL,
    `is_enabled` TINYINT(1) DEFAULT 1,
    `require_ack` TINYINT(1) DEFAULT 1,
    `log_to_history` TINYINT(1) DEFAULT 1,
    `print_alarm` TINYINT(1) DEFAULT 0,
    `plc_tag_address` VARCHAR(200) DEFAULT NULL,
    `equipment_id` INT DEFAULT NULL,
    `group_id` INT DEFAULT NULL,
    `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    KEY `ix_alarm_definitions_tag_name` (`tag_name`),
    KEY `ix_alarm_definitions_equipment_id` (`equipment_id`),
    KEY `ix_alarm_definitions_is_enabled` (`is_enabled`),
    KEY `ix_alarm_definitions_alarm_type` (`alarm_type`),
    KEY `ix_alarm_definitions_priority` (`priority`),
    CONSTRAINT `fk_alarm_definitions_group` FOREIGN KEY (`group_id`) REFERENCES `alarm_groups` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='报警定义表';

-- 3. 报警分组成员表
CREATE TABLE `alarm_group_members` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `group_id` INT NOT NULL,
    `alarm_definition_id` INT NOT NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_alarm_group_members_group_alarm` (`group_id`, `alarm_definition_id`),
    CONSTRAINT `fk_alarm_group_members_group` FOREIGN KEY (`group_id`) REFERENCES `alarm_groups` (`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_alarm_group_members_alarm_definition` FOREIGN KEY (`alarm_definition_id`) REFERENCES `alarm_definitions` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='报警分组成员表';

-- 4. 报警实例表 (运行时活动报警)
CREATE TABLE `alarm_instances` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `definition_id` INT NOT NULL,
    `state` INT NOT NULL DEFAULT 1 COMMENT '1=Inactive, 2=Active, 3=Acknowledged, 4=Shelved, 5=Suppressed',
    `current_value` DECIMAL(18,4) NOT NULL DEFAULT 0,
    `setpoint` DECIMAL(18,4) NOT NULL DEFAULT 0,
    `occur_time` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `ack_time` DATETIME DEFAULT NULL,
    `return_time` DATETIME DEFAULT NULL,
    `acked_by` VARCHAR(100) DEFAULT NULL,
    `comment` VARCHAR(500) DEFAULT NULL,
    `occurrence_count` INT DEFAULT 1,
    `is_shelved` TINYINT(1) DEFAULT 0,
    PRIMARY KEY (`id`),
    KEY `ix_alarm_instances_definition_id` (`definition_id`),
    KEY `ix_alarm_instances_state` (`state`),
    KEY `ix_alarm_instances_occur_time` (`occur_time` DESC),
    KEY `ix_alarm_instances_is_shelved` (`is_shelved`),
    CONSTRAINT `fk_alarm_instances_definition` FOREIGN KEY (`definition_id`) REFERENCES `alarm_definitions` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='报警实例表(运行时活动报警)';

-- 5. 历史报警记录表
CREATE TABLE `alarm_history` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `definition_id` INT NOT NULL,
    `final_state` INT NOT NULL,
    `occur_value` DECIMAL(18,4) NOT NULL DEFAULT 0,
    `return_value` DECIMAL(18,4) NOT NULL DEFAULT 0,
    `occur_time` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `ack_time` DATETIME DEFAULT NULL,
    `return_time` DATETIME DEFAULT NULL,
    `acked_by` VARCHAR(100) DEFAULT NULL,
    `comment` VARCHAR(500) DEFAULT NULL,
    `occurrence_count` INT DEFAULT 1,
    `total_active_time` BIGINT COMMENT '微秒数',
    `tag_name` VARCHAR(100) DEFAULT NULL,
    `alarm_name` VARCHAR(100) DEFAULT NULL,
    `priority` INT DEFAULT NULL,
    `alarm_type` INT DEFAULT NULL,
    `equipment_name` VARCHAR(100) DEFAULT NULL,
    PRIMARY KEY (`id`),
    KEY `ix_alarm_history_definition_id` (`definition_id`),
    KEY `ix_alarm_history_occur_time` (`occur_time` DESC),
    KEY `ix_alarm_history_priority` (`priority`),
    KEY `ix_alarm_history_alarm_type` (`alarm_type`),
    KEY `ix_alarm_history_equipment_name` (`equipment_name`),
    CONSTRAINT `fk_alarm_history_definition` FOREIGN KEY (`definition_id`) REFERENCES `alarm_definitions` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='历史报警记录表';

-- 6. 报警通知配置表
CREATE TABLE `alarm_notifications` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `name` VARCHAR(100) NOT NULL,
    `recipients` JSON COMMENT 'JSON数组',
    `method` INT NOT NULL DEFAULT 1 COMMENT '1=UI, 2=Sound, 3=Email, 4=SMS, 5=Webhook, 6=Print',
    `min_priority` INT NOT NULL DEFAULT 2,
    `is_enabled` TINYINT(1) DEFAULT 1,
    `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='报警通知配置表';

-- 7. 报警抑制规则表
CREATE TABLE `alarm_suppressions` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `name` VARCHAR(100) NOT NULL,
    `type` INT NOT NULL DEFAULT 1 COMMENT '1=TimeBased, 2=EquipmentBased, 3=TagBased, 4=Manual',
    `start_time` TIME NOT NULL DEFAULT '00:00:00',
    `end_time` TIME NOT NULL DEFAULT '23:59:59',
    `days_of_week` JSON COMMENT 'JSON数组',
    `tag_filter` VARCHAR(500) DEFAULT NULL,
    `equipment_id` INT DEFAULT NULL,
    `is_enabled` TINYINT(1) DEFAULT 1,
    `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='报警抑制规则表';

-- 初始化默认报警组
INSERT INTO `alarm_groups` (`name`, `description`, `is_active`) VALUES
('炭黑系统', '炭黑输送、储存相关报警', 1),
('油品系统', '油品储存、供油相关报警', 1),
('混炼系统', '混炼机、秤量相关报警', 1),
('通用设备', '风机、泵、阀门等通用设备报警', 1),
('系统报警', '通讯、数据库、系统级报警', 1);

-- 完成
SELECT 'TwoD5DMonitoring 报警管理数据库迁移完成 (MySQL)' AS `message`;

SET FOREIGN_KEY_CHECKS = 1;