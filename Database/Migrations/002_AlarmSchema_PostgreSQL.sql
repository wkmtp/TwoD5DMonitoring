-- =====================================================
-- TwoD5DMonitoring 报警管理数据库迁移脚本 - PostgreSQL
-- 版本: 1.2.0
-- =====================================================

-- 1. 报警组表
CREATE TABLE alarm_groups (
    id SERIAL PRIMARY KEY,
    name VARCHAR(50) NOT NULL,
    description VARCHAR(500),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- 2. 报警定义表
CREATE TABLE alarm_definitions (
    id SERIAL PRIMARY KEY,
    tag_name VARCHAR(100) NOT NULL,
    alarm_name VARCHAR(100) NOT NULL,
    alarm_type INT NOT NULL DEFAULT 1,
    priority INT NOT NULL DEFAULT 2,
    category INT NOT NULL DEFAULT 1,
    setpoint NUMERIC(18,4) NOT NULL DEFAULT 0,
    deadband NUMERIC(10,4) DEFAULT 0.5,
    delay_seconds INT DEFAULT 0,
    message VARCHAR(500),
    help_text VARCHAR(1000),
    is_enabled BOOLEAN DEFAULT TRUE,
    require_ack BOOLEAN DEFAULT TRUE,
    log_to_history BOOLEAN DEFAULT TRUE,
    print_alarm BOOLEAN DEFAULT FALSE,
    plc_tag_address VARCHAR(200),
    equipment_id INT,
    group_id INT,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    CONSTRAINT fk_alarm_definitions_group FOREIGN KEY (group_id) REFERENCES alarm_groups(id)
);

CREATE INDEX ix_alarm_definitions_tag_name ON alarm_definitions(tag_name);
CREATE INDEX ix_alarm_definitions_equipment_id ON alarm_definitions(equipment_id);
CREATE INDEX ix_alarm_definitions_is_enabled ON alarm_definitions(is_enabled);
CREATE INDEX ix_alarm_definitions_alarm_type ON alarm_definitions(alarm_type);
CREATE INDEX ix_alarm_definitions_priority ON alarm_definitions(priority);

-- 3. 报警分组成员表
CREATE TABLE alarm_group_members (
    id SERIAL PRIMARY KEY,
    group_id INT NOT NULL,
    alarm_definition_id INT NOT NULL,
    CONSTRAINT fk_alarm_group_members_group FOREIGN KEY (group_id) REFERENCES alarm_groups(id) ON DELETE CASCADE,
    CONSTRAINT fk_alarm_group_members_alarm_definition FOREIGN KEY (alarm_definition_id) REFERENCES alarm_definitions(id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX uq_alarm_group_members_group_alarm ON alarm_group_members(group_id, alarm_definition_id);

-- 4. 报警实例表 (运行时活动报警)
CREATE TABLE alarm_instances (
    id SERIAL PRIMARY KEY,
    definition_id INT NOT NULL,
    state INT NOT NULL DEFAULT 1,
    current_value NUMERIC(18,4) NOT NULL DEFAULT 0,
    setpoint NUMERIC(18,4) NOT NULL DEFAULT 0,
    occur_time TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    ack_time TIMESTAMPTZ,
    return_time TIMESTAMPTZ,
    acked_by VARCHAR(100),
    comment VARCHAR(500),
    occurrence_count INT DEFAULT 1,
    is_shelved BOOLEAN DEFAULT FALSE,
    CONSTRAINT fk_alarm_instances_definition FOREIGN KEY (definition_id) REFERENCES alarm_definitions(id)
);

CREATE INDEX ix_alarm_instances_definition_id ON alarm_instances(definition_id);
CREATE INDEX ix_alarm_instances_state ON alarm_instances(state);
CREATE INDEX ix_alarm_instances_occur_time ON alarm_instances(occur_time DESC);
CREATE INDEX ix_alarm_instances_is_shelved ON alarm_instances(is_shelved);

-- 5. 历史报警记录表
CREATE TABLE alarm_history (
    id SERIAL PRIMARY KEY,
    definition_id INT NOT NULL,
    final_state INT NOT NULL,
    occur_value NUMERIC(18,4) NOT NULL DEFAULT 0,
    return_value NUMERIC(18,4) NOT NULL DEFAULT 0,
    occur_time TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    ack_time TIMESTAMPTZ,
    return_time TIMESTAMPTZ,
    acked_by VARCHAR(100),
    comment VARCHAR(500),
    occurrence_count INT DEFAULT 1,
    total_active_time BIGINT, -- microseconds
    -- 冗余字段
    tag_name VARCHAR(100),
    alarm_name VARCHAR(100),
    priority INT,
    alarm_type INT,
    equipment_name VARCHAR(100),
    CONSTRAINT fk_alarm_history_definition FOREIGN KEY (definition_id) REFERENCES alarm_definitions(id)
);

CREATE INDEX ix_alarm_history_definition_id ON alarm_history(definition_id);
CREATE INDEX ix_alarm_history_occur_time ON alarm_history(occur_time DESC);
CREATE INDEX ix_alarm_history_priority ON alarm_history(priority);
CREATE INDEX ix_alarm_history_alarm_type ON alarm_history(alarm_type);
CREATE INDEX ix_alarm_history_equipment_name ON alarm_history(equipment_name);

-- 6. 报警通知配置表
CREATE TABLE alarm_notifications (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    recipients JSONB, -- JSON数组
    method INT NOT NULL DEFAULT 1,
    min_priority INT NOT NULL DEFAULT 2,
    is_enabled BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- 7. 报警抑制规则表
CREATE TABLE alarm_suppressions (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    type INT NOT NULL DEFAULT 1,
    start_time TIME NOT NULL DEFAULT '00:00:00',
    end_time TIME NOT NULL DEFAULT '23:59:59',
    days_of_week JSONB, -- JSON数组
    tag_filter VARCHAR(500),
    equipment_id INT,
    is_enabled BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- 触发器：更新 updated_at
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER tr_alarm_definitions_updated_at
    BEFORE UPDATE ON alarm_definitions
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- 初始化默认报警组
INSERT INTO alarm_groups (name, description, is_active) VALUES
('炭黑系统', '炭黑输送、储存相关报警', TRUE),
('油品系统', '油品储存、供油相关报警', TRUE),
('混炼系统', '混炼机、秤量相关报警', TRUE),
('通用设备', '风机、泵、阀门等通用设备报警', TRUE),
('系统报警', '通讯、数据库、系统级报警', TRUE);

-- 完成
-- SELECT 'TwoD5DMonitoring 报警管理数据库迁移完成 (PostgreSQL)' AS message;