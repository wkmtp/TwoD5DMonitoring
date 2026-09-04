-- =====================================================
-- TwoD5DMonitoring 数据库迁移脚本 - PostgreSQL 版本
-- 版本: 1.1.0 - 物料管理与配方管理模块
-- =====================================================

-- 1. 物料分类表
CREATE TABLE material_categories (
    id SERIAL PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL,
    description VARCHAR(500),
    sort_order INT DEFAULT 0,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX ix_material_categories_code ON material_categories(code);
CREATE INDEX ix_material_categories_is_active ON material_categories(is_active);

-- 2. 物料主数据表
CREATE TABLE materials (
    id SERIAL PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL,
    specification VARCHAR(200),
    unit VARCHAR(20) NOT NULL,
    material_type INT NOT NULL DEFAULT 1,  -- 1=原材料, 2=半成品, 3=成品, 4=添加剂, 5=包材, 99=其他
    status INT NOT NULL DEFAULT 1,         -- 1=启用, 2=停用, 3=废弃, 4=待审核
    category_id INT,
    density NUMERIC(10,4) DEFAULT 0,
    moisture_content NUMERIC(6,2) DEFAULT 0,
    particle_size NUMERIC(10,2) DEFAULT 0,
    color VARCHAR(50),
    manufacturer VARCHAR(100),
    supplier VARCHAR(100),
    standard_cost NUMERIC(18,4) DEFAULT 0,
    remark VARCHAR(1000),
    is_weighable BOOLEAN DEFAULT TRUE,
    is_batch_managed BOOLEAN DEFAULT TRUE,
    shelf_life_days INT DEFAULT 0,
    storage_condition VARCHAR(200),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    created_by VARCHAR(100),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    updated_by VARCHAR(100),
    CONSTRAINT fk_materials_category FOREIGN KEY (category_id) REFERENCES material_categories(id)
);

CREATE INDEX ix_materials_code ON materials(code);
CREATE INDEX ix_materials_name ON materials(name);
CREATE INDEX ix_materials_material_type ON materials(material_type);
CREATE INDEX ix_materials_status ON materials(status);
CREATE INDEX ix_materials_category_id ON materials(category_id);
CREATE INDEX ix_materials_is_weighable ON materials(is_weighable);

-- 3. 物料扩展属性表
CREATE TABLE material_properties (
    id SERIAL PRIMARY KEY,
    material_id INT NOT NULL,
    name VARCHAR(100) NOT NULL,
    value VARCHAR(500),
    unit VARCHAR(20),
    data_type VARCHAR(20) DEFAULT 'String',
    sort_order INT DEFAULT 0,
    is_required BOOLEAN DEFAULT FALSE,
    validation_rule JSONB,
    CONSTRAINT fk_material_properties_material FOREIGN KEY (material_id) REFERENCES materials(id) ON DELETE CASCADE
);

CREATE INDEX ix_material_properties_material_id ON material_properties(material_id);

-- 4. 罐体物料分配表
CREATE TABLE tank_materials (
    id SERIAL PRIMARY KEY,
    tank_id INT NOT NULL,
    tank_code VARCHAR(50),
    tank_name VARCHAR(100),
    tank_type INT,
    material_id INT NOT NULL,
    is_primary BOOLEAN DEFAULT TRUE,
    priority INT DEFAULT 1,
    max_capacity NUMERIC(18,4) DEFAULT 0,
    min_level NUMERIC(18,4) DEFAULT 0,
    max_level NUMERIC(18,4) DEFAULT 0,
    remark VARCHAR(500),
    effective_from TIMESTAMPTZ DEFAULT NOW(),
    effective_to TIMESTAMPTZ,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    created_by VARCHAR(100),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    updated_by VARCHAR(100),
    CONSTRAINT fk_tank_materials_material FOREIGN KEY (material_id) REFERENCES materials(id)
);

CREATE INDEX ix_tank_materials_tank_id ON tank_materials(tank_id);
CREATE INDEX ix_tank_materials_material_id ON tank_materials(material_id);
CREATE INDEX ix_tank_materials_is_active ON tank_materials(is_active);

-- 5. 罐体物料变更历史表
CREATE TABLE tank_material_history (
    id SERIAL PRIMARY KEY,
    tank_id INT NOT NULL,
    tank_code VARCHAR(50),
    tank_name VARCHAR(100),
    old_material_id INT,
    new_material_id INT,
    change_reason VARCHAR(500),
    operator VARCHAR(100),
    change_time TIMESTAMPTZ DEFAULT NOW(),
    CONSTRAINT fk_tank_material_history_old_material FOREIGN KEY (old_material_id) REFERENCES materials(id),
    CONSTRAINT fk_tank_material_history_new_material FOREIGN KEY (new_material_id) REFERENCES materials(id)
);

CREATE INDEX ix_tank_material_history_tank_id ON tank_material_history(tank_id);
CREATE INDEX ix_tank_material_history_change_time ON tank_material_history(change_time);

-- 6. 配方基础表
CREATE TABLE recipes (
    id SERIAL PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL,
    recipe_type INT NOT NULL DEFAULT 1,  -- 1=称量, 2=混炼, 3=挤出, 4=压延, 99=其他
    status INT NOT NULL DEFAULT 1,       -- 1=草稿, 2=待审核, 3=已审核, 4=已发布, 5=已归档, 6=作废
    product_code VARCHAR(50),
    product_name VARCHAR(100),
    batch_size NUMERIC(18,4) DEFAULT 1000,
    yield_rate NUMERIC(6,2) DEFAULT 100,
    remark VARCHAR(2000),
    current_version VARCHAR(50) DEFAULT '1.0',
    current_version_id INT,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    created_by VARCHAR(100),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    updated_by VARCHAR(100),
    approved_at TIMESTAMPTZ,
    approved_by VARCHAR(100),
    released_at TIMESTAMPTZ,
    released_by VARCHAR(100)
);

CREATE INDEX ix_recipes_code ON recipes(code);
CREATE INDEX ix_recipes_recipe_type ON recipes(recipe_type);
CREATE INDEX ix_recipes_status ON recipes(status);
CREATE INDEX ix_recipes_product_code ON recipes(product_code);

-- 7. 配方版本表
CREATE TABLE recipe_versions (
    id SERIAL PRIMARY KEY,
    recipe_id INT NOT NULL,
    version VARCHAR(50) NOT NULL,
    change_description VARCHAR(1000),
    is_current BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    created_by VARCHAR(100),
    CONSTRAINT fk_recipe_versions_recipe FOREIGN KEY (recipe_id) REFERENCES recipes(id) ON DELETE CASCADE
);

CREATE INDEX ix_recipe_versions_recipe_id ON recipe_versions(recipe_id);
CREATE INDEX ix_recipe_versions_is_current ON recipe_versions(is_current);

-- 8. 称量配方扩展表
CREATE TABLE weighing_recipes (
    recipe_id INT PRIMARY KEY,
    weighing_mode INT DEFAULT 1,         -- 1=顺序, 2=同时, 3=手工
    scale_id INT,
    total_weight NUMERIC(18,4) DEFAULT 0,
    decimal_places INT DEFAULT 2,
    allow_manual_adjust BOOLEAN DEFAULT TRUE,
    CONSTRAINT fk_weighing_recipes_recipe FOREIGN KEY (recipe_id) REFERENCES recipes(id) ON DELETE CASCADE
);

-- 9. 称量配方明细行
CREATE TABLE weighing_recipe_lines (
    id SERIAL PRIMARY KEY,
    recipe_id INT NOT NULL,
    line_number INT NOT NULL,
    material_id INT NOT NULL,
    target_weight NUMERIC(18,4) NOT NULL,
    tolerance_positive NUMERIC(6,2) DEFAULT 1.0,
    tolerance_negative NUMERIC(6,2) DEFAULT 1.0,
    weighing_order INT DEFAULT 0,
    is_critical BOOLEAN DEFAULT TRUE,
    remark VARCHAR(500),
    feeder_type INT DEFAULT 1,           -- 1=自动, 2=半自动, 3=手工, 4=真空, 5=螺旋
    plc_tag_index INT,
    plc_weight_tag VARCHAR(200),
    plc_material_tag VARCHAR(200),
    CONSTRAINT fk_weighing_recipe_lines_recipe FOREIGN KEY (recipe_id) REFERENCES recipes(id) ON DELETE CASCADE,
    CONSTRAINT fk_weighing_recipe_lines_material FOREIGN KEY (material_id) REFERENCES materials(id)
);

CREATE INDEX ix_weighing_recipe_lines_recipe_id ON weighing_recipe_lines(recipe_id);
CREATE INDEX ix_weighing_recipe_lines_material_id ON weighing_recipe_lines(material_id);
CREATE UNIQUE INDEX uq_weighing_recipe_lines_recipe_line_number ON weighing_recipe_lines(recipe_id, line_number);

-- 10. 混炼配方扩展表
CREATE TABLE mixing_recipes (
    recipe_id INT PRIMARY KEY,
    mixer_id INT,
    process_type INT DEFAULT 1,          -- 1=密炼机, 2=内混机, 3=开炼机, 4=连续混炼
    total_steps INT DEFAULT 0,
    target_temperature NUMERIC(6,2) DEFAULT 0,
    target_time INT DEFAULT 0,
    dump_temperature INT DEFAULT 0,
    ram_pressure NUMERIC(10,2) DEFAULT 0,
    ram_position INT DEFAULT 0,
    rotor_speed INT DEFAULT 0,
    enable_temp_control BOOLEAN DEFAULT TRUE,
    enable_energy_control BOOLEAN DEFAULT FALSE,
    CONSTRAINT fk_mixing_recipes_recipe FOREIGN KEY (recipe_id) REFERENCES recipes(id) ON DELETE CASCADE
);

-- 11. 混炼配方步骤表
CREATE TABLE mixing_recipe_steps (
    id SERIAL PRIMARY KEY,
    recipe_id INT NOT NULL,
    step_number INT NOT NULL,
    step_type INT DEFAULT 1,             -- 1=投料, 2=混炼, 3=升温, 4=降温, 5=出料, 6=等待, 99=自定义
    material_id INT,
    amount NUMERIC(18,4) DEFAULT 0,
    add_time INT DEFAULT 0,
    duration INT DEFAULT 0,
    target_temp INT DEFAULT 0,
    rotor_speed INT DEFAULT 0,
    ram_pressure NUMERIC(10,2) DEFAULT 0,
    is_auto_dump BOOLEAN DEFAULT FALSE,
    wait_for_temp BOOLEAN DEFAULT FALSE,
    condition VARCHAR(500),
    remark VARCHAR(500),
    plc_step_index INT,
    plc_command_tag VARCHAR(200),
    plc_param_tags JSONB,
    CONSTRAINT fk_mixing_recipe_steps_recipe FOREIGN KEY (recipe_id) REFERENCES recipes(id) ON DELETE CASCADE,
    CONSTRAINT fk_mixing_recipe_steps_material FOREIGN KEY (material_id) REFERENCES materials(id)
);

CREATE INDEX ix_mixing_recipe_steps_recipe_id ON mixing_recipe_steps(recipe_id);
CREATE INDEX ix_mixing_recipe_steps_material_id ON mixing_recipe_steps(material_id);
CREATE UNIQUE INDEX uq_mixing_recipe_steps_recipe_step_number ON mixing_recipe_steps(recipe_id, step_number);

-- 12. 配方下发日志表
CREATE TABLE recipe_download_logs (
    id SERIAL PRIMARY KEY,
    recipe_id INT NOT NULL,
    recipe_code VARCHAR(50) NOT NULL,
    recipe_name VARCHAR(100) NOT NULL,
    recipe_type INT NOT NULL,
    version VARCHAR(50) NOT NULL,
    target_plc VARCHAR(100) NOT NULL,
    status INT DEFAULT 1,               -- 1=待下发, 2=下发中, 3=成功, 4=失败, 5=部分成功, 6=已校验
    error_message VARCHAR(1000),
    operator VARCHAR(100),
    start_time TIMESTAMPTZ DEFAULT NOW(),
    end_time TIMESTAMPTZ,
    download_data JSONB,
    CONSTRAINT fk_recipe_download_logs_recipe FOREIGN KEY (recipe_id) REFERENCES recipes(id)
);

CREATE INDEX ix_recipe_download_logs_recipe_id ON recipe_download_logs(recipe_id);
CREATE INDEX ix_recipe_download_logs_target_plc ON recipe_download_logs(target_plc);
CREATE INDEX ix_recipe_download_logs_start_time ON recipe_download_logs(start_time);
CREATE INDEX ix_recipe_download_logs_status ON recipe_download_logs(status);

-- 13. 批次生产记录表
CREATE TABLE batch_records (
    id SERIAL PRIMARY KEY,
    batch_number VARCHAR(50) NOT NULL UNIQUE,
    recipe_id INT NOT NULL,
    recipe_code VARCHAR(50) NOT NULL,
    recipe_version VARCHAR(50) NOT NULL,
    mixer_id INT,
    scale_id INT,
    status INT DEFAULT 1,               -- 1=已创建, 2=称量中, 3=称量完成, 4=混炼中, 5=混炼完成, 6=已出料, 7=完成, 8=中止, 9=暂停
    actual_weight NUMERIC(18,4) DEFAULT 0,
    target_weight NUMERIC(18,4) DEFAULT 0,
    start_time TIMESTAMPTZ DEFAULT NOW(),
    end_time TIMESTAMPTZ,
    operator VARCHAR(100),
    shift VARCHAR(50),
    remark VARCHAR(1000),
    weighing_details JSONB,
    mixing_details JSONB,
    CONSTRAINT fk_batch_records_recipe FOREIGN KEY (recipe_id) REFERENCES recipes(id)
);

CREATE INDEX ix_batch_records_batch_number ON batch_records(batch_number);
CREATE INDEX ix_batch_records_recipe_id ON batch_records(recipe_id);
CREATE INDEX ix_batch_records_status ON batch_records(status);
CREATE INDEX ix_batch_records_start_time ON batch_records(start_time);

-- 14. 初始化基础物料分类数据
INSERT INTO material_categories (code, name, description, sort_order, is_active) VALUES
('RM', '橡胶原料', '天然橡胶、合成橡胶等', 1, TRUE),
('CB', '炭黑', '各类炭黑产品', 2, TRUE),
('OIL', '油类', '工艺油、软化剂等', 3, TRUE),
('CHEM', '化学品', '硫化剂、促进剂、防老剂等', 4, TRUE),
('ADD', '添加剂', '填充剂、增塑剂等', 5, TRUE),
('PKG', '包装材料', '编织袋、内衬袋等', 6, TRUE),
('SFG', '半成品', '母胶、混炼胶等', 7, TRUE),
('FG', '成品', '最终成品胶料', 8, TRUE),
('OTH', '其他', '其他辅助材料', 99, TRUE);

-- 15. 初始化示例物料数据
INSERT INTO materials (code, name, specification, unit, material_type, status, category_id, density, moisture_content, particle_size, is_weighable, is_batch_managed, created_by) VALUES
('NR-001', '天然橡胶 STR 20', 'STR 20级', 'KG', 1, 1, 1, 0.92, 0.5, 0, TRUE, TRUE, 'SYSTEM'),
('SBR-001', '丁苯橡胶 1502', '1502型', 'KG', 1, 1, 1, 0.94, 0.3, 0, TRUE, TRUE, 'SYSTEM'),
('BR-001', '顺丁橡胶 BR 9000', 'BR 9000', 'KG', 1, 1, 1, 0.91, 0.2, 0, TRUE, TRUE, 'SYSTEM'),
('CB-N330', '炭黑 N330', 'N330 高耐磨', 'KG', 1, 1, 2, 1.85, 0.5, 30, TRUE, TRUE, 'SYSTEM'),
('CB-N550', '炭黑 N550', 'N550 半补强', 'KG', 1, 1, 2, 1.82, 0.5, 40, TRUE, TRUE, 'SYSTEM'),
('OIL-ARA', '芳香族油', '工艺用芳香油', 'KG', 1, 1, 3, 0.98, 0.1, 0, TRUE, TRUE, 'SYSTEM'),
('SUL-001', '硫磺', '工业级硫磺', 'KG', 4, 1, 4, 2.07, 0.1, 200, TRUE, TRUE, 'SYSTEM'),
('ACC-TBBS', '促进剂 TBBS', 'N-叔丁基-2-苯并噻唑次磺酰胺', 'KG', 4, 1, 4, 1.35, 0.2, 150, TRUE, TRUE, 'SYSTEM'),
('ANT-4010', '防老剂 4010', 'N-异丙基-N'-苯基对苯二胺', 'KG', 4, 1, 4, 1.15, 0.3, 100, TRUE, TRUE, 'SYSTEM'),
('ZNO-001', '氧化锌', '活性氧化锌', 'KG', 4, 1, 4, 5.6, 0.5, 50, TRUE, TRUE, 'SYSTEM'),
('STEARIC', '硬脂酸', '工业级硬脂酸', 'KG', 4, 1, 4, 0.94, 0.2, 0, TRUE, TRUE, 'SYSTEM');

-- 16. 创建更新时间触发器函数
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- 应用触发器
CREATE TRIGGER tr_materials_updated_at
    BEFORE UPDATE ON materials
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER tr_recipes_updated_at
    BEFORE UPDATE ON recipes
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER tr_material_categories_updated_at
    BEFORE UPDATE ON material_categories
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER tr_tank_materials_updated_at
    BEFORE UPDATE ON tank_materials
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- 完成
-- SELECT 'TwoD5DMonitoring 物料与配方数据库迁移完成 (PostgreSQL)' AS message;