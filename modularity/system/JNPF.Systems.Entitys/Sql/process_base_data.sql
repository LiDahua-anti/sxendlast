-- ============================================================
-- 工序 + 设备 基础数据（适用于 江苏三协铝业 MES 系统）
-- 数据库：MySQL
-- 列名严格对应 Entity 中 [SugarColumn(ColumnName = "xxx")] 定义
-- 执行顺序：0→1→2→3→4→5→6→7
-- ============================================================
-- 基类 CLDSEntityBase 公共列：
--   F_ID               主键(字符串)
--   F_TENANT_ID        租户ID(默认'0')
--   F_CREATOR_TIME     创建时间
--   F_CREATOR_USER_ID  创建用户(默认'349057407209541')
--   F_LAST_MODIFY_TIME 修改时间
--   F_LAST_MODIFY_USER_ID 修改用户
--   F_DELETE_MARK      删除标志(NULL=正常,1=删除)
--   F_DELETE_TIME      删除时间
--   F_DELETE_USER_ID   删除用户
--   F_SORT_CODE        排序码
--   F_ENABLED_MARK     启用标识(1=启用)
-- ============================================================

-- ============================================================
-- 0. sx_devicetype_master 设备类型表（先建类型，设备台账引用）
-- 列：parent_id, devicetype_code, devicetype_name, f_note, sort_direct(NOT NULL)
-- ============================================================

-- 一级分类（parent_id = NULL 或 '0'）
INSERT INTO sx_devicetype_master
  (F_ID, F_TENANT_ID, F_CREATOR_TIME, F_CREATOR_USER_ID, F_ENABLED_MARK, F_DELETE_MARK, F_SORT_CODE,
   parent_id, devicetype_code, devicetype_name, f_note, sort_direct)
VALUES
  (REPLACE(UUID(),'-',''), '0', NOW(), '349057407209541', 1, NULL, 1,
   NULL, 'SB-EJ', '挤压设备', '铝棒加热炉、挤压机等', 1),
  (REPLACE(UUID(),'-',''), '0', NOW(), '349057407209541', 1, NULL, 2,
   NULL, 'SB-RE', '热处理设备', '时效炉、淬火炉等', 1),
  (REPLACE(UUID(),'-',''), '0', NOW(), '349057407209541', 1, NULL, 3,
   NULL, 'SB-QG', '切割设备', '精密锯床、切割机等', 1),
  (REPLACE(UUID(),'-',''), '0', NOW(), '349057407209541', 1, NULL, 4,
   NULL, 'SB-YH', '表面处理设备', '阳极氧化线、电泳线等', 1),
  (REPLACE(UUID(),'-',''), '0', NOW(), '349057407209541', 1, NULL, 5,
   NULL, 'SB-CNC', 'CNC加工设备', '数控铣床、钻攻中心等', 1),
  (REPLACE(UUID(),'-',''), '0', NOW(), '349057407209541', 1, NULL, 6,
   NULL, 'SB-JY', '检验检测设备', '三坐标、硬度计等', 1),
  (REPLACE(UUID(),'-',''), '0', NOW(), '349057407209541', 1, NULL, 7,
   NULL, 'SB-BZ', '包装设备', '自动包装机、打包机等', 1);

-- 二级分类（parent_id 引用一级）
INSERT INTO sx_devicetype_master
  (F_ID, F_TENANT_ID, F_CREATOR_TIME, F_CREATOR_USER_ID, F_ENABLED_MARK, F_DELETE_MARK, F_SORT_CODE,
   parent_id, devicetype_code, devicetype_name, f_note, sort_direct)
SELECT
  REPLACE(UUID(),'-',''), '0', NOW(), '349057407209541', 1, NULL, v.seq_no,
  p.F_ID, v.code, v.name, v.note, 1
FROM sx_devicetype_master p
INNER JOIN (
  SELECT 'SB-EJ' AS parent, 'SB-EJ-01' AS code, '挤压机' AS name, '600T-3600T各规格' AS note, 1 AS seq_no UNION ALL
  SELECT 'SB-EJ', 'SB-EJ-02', '铝棒加热炉', '天然气/电加热', 2 UNION ALL
  SELECT 'SB-RE', 'SB-RE-01', '时效炉', 'T5/T6时效处理', 3 UNION ALL
  SELECT 'SB-QG', 'SB-QG-01', '精密双头锯', '切割精度±0.1mm', 4 UNION ALL
  SELECT 'SB-YH', 'SB-YH-01', '阳极氧化线', '硫酸阳极氧化', 5 UNION ALL
  SELECT 'SB-CNC', 'SB-CNC-01', 'CNC加工中心', '三轴/四轴/五轴', 6 UNION ALL
  SELECT 'SB-CNC', 'SB-CNC-02', '数控钻攻中心', '钻孔攻牙', 7 UNION ALL
  SELECT 'SB-JY', 'SB-JY-01', '三坐标测量机', '精密尺寸检测', 8 UNION ALL
  SELECT 'SB-JY', 'SB-JY-02', '韦氏硬度计', '型材硬度检测', 9 UNION ALL
  SELECT 'SB-BZ', 'SB-BZ-01', '自动包装线', '型材自动包装', 10
) v ON p.devicetype_code = v.parent;

-- ============================================================
-- 0.5 sx_device_master 设备台账表
-- 列：device_state, point_device, device_no, assets_number, device_name,
--     device_manufacturer, device_model, device_supplier, buy_date,
--     devicetype_id, use_address, use_dpt, device_important, device_status,
--     f_note, two_dimensional_code, device_image, control_panel_no,
--     plc_model, device_ip, port_number, communication_protocol, gateway_ip
-- ============================================================

INSERT INTO sx_device_master
  (F_ID, F_TENANT_ID, F_CREATOR_TIME, F_CREATOR_USER_ID, F_ENABLED_MARK, F_DELETE_MARK, F_SORT_CODE,
   device_state, point_device, device_no, assets_number, device_name,
   device_manufacturer, device_model, device_supplier, buy_date,
   devicetype_id, use_address, use_dpt, device_important, device_status, f_note)
SELECT
  REPLACE(UUID(),'-',''), '0', NOW(), '349057407209541', 1, NULL, v.seq_no,
  '1', 0, v.device_no, v.assets_no, v.device_name,
  v.manufacturer, v.model, v.supplier, v.buy_date,
  t.F_ID, v.address, v.dpt, v.important, '正常', v.note
FROM sx_devicetype_master t
INNER JOIN (
  -- 挤压设备
  SELECT 'SB-EJ-01' AS type_code, 'EJ-001' AS device_no, 'ZC-2020-001' AS assets_no, '600T挤压机' AS device_name, '佛山市德力泰' AS manufacturer, 'DLT-600' AS model, '佛山德力泰机械' AS supplier, '2020-03-15' AS buy_date, '挤压车间' AS address, '挤压班' AS dpt, 'A' AS important, '600T铝型材挤压机，月产能200吨' AS note, 1 AS seq_no UNION ALL
  SELECT 'SB-EJ-01', 'EJ-002', 'ZC-2020-002', '1000T挤压机', '佛山市德力泰', 'DLT-1000', '佛山德力泰机械', '2019-06-20', '挤压车间', '挤压班', 'A', '1000T铝型材挤压机，月产能400吨', 2 UNION ALL
  SELECT 'SB-EJ-01', 'EJ-003', 'ZC-2021-003', '1800T挤压机', '佛山市德力泰', 'DLT-1800', '佛山德力泰机械', '2021-01-10', '挤压车间', '挤压班', 'A', '1800T大型材挤压机', 3 UNION ALL
  SELECT 'SB-EJ-02', 'EJ-L001', 'ZC-2020-010', '铝棒加热炉#1', '苏州热工科技', 'RG-500', '苏州热工科技', '2020-03-15', '挤压车间', '挤压班', 'B', '天然气加热，500mm棒径', 4 UNION ALL
  -- 热处理设备
  SELECT 'SB-RE-01', 'RE-001', 'ZC-2020-020', '时效炉#1', '东莞鑫宏机械', 'SX-1000', '东莞鑫宏机械', '2020-05-10', '时效炉区', '热处理班', 'A', 'T5时效处理，温控精度±5℃', 5 UNION ALL
  SELECT 'SB-RE-01', 'RE-002', 'ZC-2021-021', '时效炉#2', '东莞鑫宏机械', 'SX-1500', '东莞鑫宏机械', '2021-03-15', '时效炉区', '热处理班', 'A', 'T6时效处理，大容量', 6 UNION ALL
  -- 切割设备
  SELECT 'SB-QG-01', 'QG-001', 'ZC-2020-030', '精密双头锯#1', '上海金切机械', 'JC-S500', '上海金切机械', '2020-04-20', '切割区', '切割班', 'B', '切割精度±0.1mm', 7 UNION ALL
  SELECT 'SB-QG-01', 'QG-002', 'ZC-2021-031', '精密双头锯#2', '上海金切机械', 'JC-S500', '上海金切机械', '2021-05-10', '切割区', '切割班', 'B', '备用切割机', 8 UNION ALL
  -- CNC加工设备
  SELECT 'SB-CNC-01', 'CNC-001', 'ZC-2021-040', 'CNC加工中心#1', '大连机床集团', 'DMG-850', '大连机床集团', '2021-02-28', 'CNC车间', 'CNC班', 'A', '三轴联动，行程800x500x500', 9 UNION ALL
  SELECT 'SB-CNC-01', 'CNC-002', 'ZC-2021-041', 'CNC加工中心#2', '大连机床集团', 'DMG-850', '大连机床集团', '2021-02-28', 'CNC车间', 'CNC班', 'A', '同#1，双机并排', 10 UNION ALL
  SELECT 'SB-CNC-02', 'CNC-Z001', 'ZC-2022-050', '数控钻攻中心#1', '北京精雕科技', 'JD-400', '北京精雕科技', '2022-01-15', 'CNC车间', 'CNC班', 'B', '高速钻攻，适合多孔加工', 11 UNION ALL
  -- 表面处理设备
  SELECT 'SB-YH-01', 'YH-001', 'ZC-2019-060', '阳极氧化线', '苏州电镀设备厂', 'YH-2000', '苏州电镀设备厂', '2019-08-20', '氧化车间', '氧化班', 'A', '硫酸阳极氧化，槽长6米', 12 UNION ALL
  -- 检验检测设备
  SELECT 'SB-JY-01', 'JY-001', 'ZC-2022-070', '三坐标测量机', '海克斯康', 'Global S', '海克斯康测量技术', '2022-03-10', '检验区', '品质部', 'A', '精度0.003mm，恒温恒湿环境', 13 UNION ALL
  SELECT 'SB-JY-02', 'JY-002', 'ZC-2020-071', '韦氏硬度计#1', '北京时代之峰', 'TH-300', '北京时代之峰', '2020-06-15', '检验区', '品质部', 'B', '便携式硬度检测', 14 UNION ALL
  SELECT 'SB-JY-02', 'JY-003', 'ZC-2020-072', '韦氏硬度计#2', '北京时代之峰', 'TH-300', '北京时代之峰', '2020-06-15', '检验区', '品质部', 'B', '备用硬度计', 15 UNION ALL
  -- 包装设备
  SELECT 'SB-BZ-01', 'BZ-001', 'ZC-2021-080', '自动包装线', '广州包装机械', 'BZ-A2000', '广州包装机械', '2021-07-20', '包装区', '包装班', 'B', 'PE袋+纸箱自动包装', 16
) v ON t.devicetype_code = v.type_code;

-- ============================================================
-- 1. sx_base_process 基础工序表
-- 列：process_name, process_code, process_type, standard_cycle,
--     work_location, process_content, remark
-- ============================================================

INSERT INTO sx_base_process
  (F_ID, F_TENANT_ID, F_CREATOR_TIME, F_CREATOR_USER_ID, F_ENABLED_MARK, F_DELETE_MARK, F_SORT_CODE,
   process_name, process_code, process_type, standard_cycle, work_location, process_content, remark)
VALUES
  (REPLACE(UUID(),'-',''), '0', NOW(), '349057407209541', 1, NULL, 1,
   '挤压',        'GX01', '1', '30min/支',  '挤压车间', '铝棒加热后通过模具挤压成型，控制挤压温度和速度', '核心工序'),
  (REPLACE(UUID(),'-',''), '0', NOW(), '349057407209541', 1, NULL, 2,
   '时效处理',    'GX02', '3', '120min/炉', '时效炉区', 'T5/T6时效处理，消除内应力，提升硬度', '温度180-200℃'),
  (REPLACE(UUID(),'-',''), '0', NOW(), '349057407209541', 1, NULL, 3,
   '精密切割',    'GX03', '1', '15min/支',  '切割区',   '按客户尺寸精密切割，控制长度公差', '长度公差±0.5mm'),
  (REPLACE(UUID(),'-',''), '0', NOW(), '349057407209541', 1, NULL, 4,
   '阳极氧化',    'GX04', '4', '60min/批',  '氧化车间', '阳极氧化处理，提升表面耐腐蚀性和美观度', '膜厚≥10μm'),
  (REPLACE(UUID(),'-',''), '0', NOW(), '349057407209541', 1, NULL, 5,
   'CNC精加工',   'GX05', '1', '45min/件',  'CNC车间',  'CNC铣削/钻孔/攻牙等精密加工', '精度±0.05mm'),
  (REPLACE(UUID(),'-',''), '0', NOW(), '349057407209541', 1, NULL, 6,
   '去毛刺',      'GX06', '1', '10min/件',  '打磨区',   '去除加工毛刺，保证表面光洁', NULL),
  (REPLACE(UUID(),'-',''), '0', NOW(), '349057407209541', 1, NULL, 7,
   '外观检验',    'GX07', '2', '5min/件',   '检验区',   '目视检查表面质量、颜色、划伤等', 'AQL=1.0'),
  (REPLACE(UUID(),'-',''), '0', NOW(), '349057407209541', 1, NULL, 8,
   '尺寸检验',    'GX08', '2', '8min/件',   '检验区',   '使用卡尺/千分尺测量关键尺寸', '全检/抽检'),
  (REPLACE(UUID(),'-',''), '0', NOW(), '349057407209541', 1, NULL, 9,
   '包装',        'GX09', '1', '20min/箱',  '包装区',   '按客户要求包装，贴标签', 'PE袋+纸箱/木箱'),
  (REPLACE(UUID(),'-',''), '0', NOW(), '349057407209541', 1, NULL, 10,
   '入库',        'GX10', '1', '5min/箱',   '仓库',     '扫码入库，登记库存', NULL);

-- ============================================================
-- 2. sx_routing 工艺路线表
-- ============================================================

INSERT INTO sx_routing
  (F_ID, F_TENANT_ID, F_CREATOR_TIME, F_CREATOR_USER_ID, F_ENABLED_MARK, F_DELETE_MARK, F_SORT_CODE,
   routing_code, routing_name, inventory_id, inventory_code, inventory_name,
   process_count, total_hours, version, status, remark)
VALUES
  (REPLACE(UUID(),'-',''), '0', NOW(), '349057407209541', 1, NULL, 1,
   'GY-EH01-09B-V1', 'EH01-09B标准型材-氧化路线', NULL, 'EH01-09B', 'EH01-09B标准型材',
   7, 275.00, 'V1', 1, '常规氧化型材标准路线'),
  (REPLACE(UUID(),'-',''), '0', NOW(), '349057407209541', 1, NULL, 2,
   'GY-EH02-15A-V1', 'EH02-15A精密件-CNC路线', NULL, 'EH02-15A', 'EH02-15A精密件',
   9, 380.00, 'V1', 1, '精密加工件完整路线'),
  (REPLACE(UUID(),'-',''), '0', NOW(), '349057407209541', 1, NULL, 3,
   'GY-EH03-05C-V1', 'EH03-05C简单截断件路线', NULL, 'EH03-05C', 'EH03-05C截断件',
   4, 80.00, 'V1', 1, '简单截断，无需表面处理');

-- ============================================================
-- 3. sx_routing_process 工艺路线工序表
-- ============================================================

-- 路线A工序
INSERT INTO sx_routing_process
  (F_ID, F_TENANT_ID, F_CREATOR_TIME, F_CREATOR_USER_ID, F_ENABLED_MARK, F_DELETE_MARK, F_SORT_CODE,
   routing_id, process_id, process_code, process_name, seq_no, standard_hours, requirement, is_check_process, remark)
SELECT
  REPLACE(UUID(),'-',''), '0', NOW(), '349057407209541', 1, NULL, v.seq_no,
  r.F_ID, p.F_ID, p.process_code, p.process_name, v.seq_no, v.hours, v.req, v.is_check, ''
FROM sx_routing r
CROSS JOIN sx_base_process p
INNER JOIN (
  SELECT 'GX01' AS code, 1 AS seq_no, 30.00 AS hours, '控制挤压温度和速度' AS req, 0 AS is_check UNION ALL
  SELECT 'GX02', 2, 120.00, '温度180-200℃，保温120min', 0 UNION ALL
  SELECT 'GX03', 3, 15.00,  '长度公差±0.5mm', 0 UNION ALL
  SELECT 'GX04', 4, 60.00,  '膜厚≥10μm，颜色一致', 0 UNION ALL
  SELECT 'GX07', 5, 5.00,   'AQL=1.0，表面无划伤', 1 UNION ALL
  SELECT 'GX08', 6, 8.00,   '关键尺寸全检', 1 UNION ALL
  SELECT 'GX09', 7, 20.00,  'PE袋+纸箱包装', 0
) v ON p.process_code = v.code
WHERE r.routing_code = 'GY-EH01-09B-V1';

-- 路线B工序
INSERT INTO sx_routing_process
  (F_ID, F_TENANT_ID, F_CREATOR_TIME, F_CREATOR_USER_ID, F_ENABLED_MARK, F_DELETE_MARK, F_SORT_CODE,
   routing_id, process_id, process_code, process_name, seq_no, standard_hours, requirement, is_check_process, remark)
SELECT
  REPLACE(UUID(),'-',''), '0', NOW(), '349057407209541', 1, NULL, v.seq_no,
  r.F_ID, p.F_ID, p.process_code, p.process_name, v.seq_no, v.hours, v.req, v.is_check, ''
FROM sx_routing r
CROSS JOIN sx_base_process p
INNER JOIN (
  SELECT 'GX01' AS code, 1 AS seq_no, 30.00 AS hours, '控制挤压温度' AS req, 0 AS is_check UNION ALL
  SELECT 'GX02', 2, 120.00, 'T6时效处理', 0 UNION ALL
  SELECT 'GX03', 3, 15.00,  '预留CNC加工余量', 0 UNION ALL
  SELECT 'GX06', 4, 10.00,  '去除挤压毛刺', 0 UNION ALL
  SELECT 'GX05', 5, 45.00,  'CNC精加工，精度±0.05mm', 0 UNION ALL
  SELECT 'GX06', 6, 10.00,  '去除加工毛刺', 0 UNION ALL
  SELECT 'GX07', 7, 5.00,   'AQL=0.65，外观全检', 1 UNION ALL
  SELECT 'GX08', 8, 8.00,   '关键尺寸100%检测', 1 UNION ALL
  SELECT 'GX09', 9, 20.00,  '精密件单独包装防护', 0
) v ON p.process_code = v.code
WHERE r.routing_code = 'GY-EH02-15A-V1';

-- 路线C工序
INSERT INTO sx_routing_process
  (F_ID, F_TENANT_ID, F_CREATOR_TIME, F_CREATOR_USER_ID, F_ENABLED_MARK, F_DELETE_MARK, F_SORT_CODE,
   routing_id, process_id, process_code, process_name, seq_no, standard_hours, requirement, is_check_process, remark)
SELECT
  REPLACE(UUID(),'-',''), '0', NOW(), '349057407209541', 1, NULL, v.seq_no,
  r.F_ID, p.F_ID, p.process_code, p.process_name, v.seq_no, v.hours, v.req, v.is_check, ''
FROM sx_routing r
CROSS JOIN sx_base_process p
INNER JOIN (
  SELECT 'GX01' AS code, 1 AS seq_no, 30.00 AS hours, '标准挤压' AS req, 0 AS is_check UNION ALL
  SELECT 'GX03', 2, 15.00,  '按客户长度切割', 0 UNION ALL
  SELECT 'GX07', 3, 5.00,   '外观抽检', 1 UNION ALL
  SELECT 'GX09', 4, 20.00,  '标准包装', 0
) v ON p.process_code = v.code
WHERE r.routing_code = 'GY-EH03-05C-V1';

-- ============================================================
-- 4. sx_process_check_item 工序检查项表
-- ============================================================

-- GX07 外观检验 检查项
INSERT INTO sx_process_check_item
  (F_ID, F_TENANT_ID, F_CREATOR_TIME, F_CREATOR_USER_ID, F_ENABLED_MARK, F_DELETE_MARK, F_SORT_CODE,
   process_id, process_code, process_name, check_item, check_standard,
   data_type, option_items, default_value, required, seq_no, remark)
SELECT
  REPLACE(UUID(),'-',''), '0', NOW(), '349057407209541', 1, NULL, v.seq_no,
  p.F_ID, p.process_code, p.process_name, v.item, v.standard,
  v.dtype, v.options, v.defval, 1, v.seq_no, ''
FROM sx_base_process p
INNER JOIN (
  SELECT 'GX07' AS code, '表面划伤' AS item, '无可见划伤' AS standard, 3 AS dtype, 'OK,NG' AS options, 'OK' AS defval, 1 AS seq_no UNION ALL
  SELECT 'GX07', '颜色一致性', '与色卡一致', 3, 'OK,NG', 'OK', 2 UNION ALL
  SELECT 'GX07', '氧化膜完整性', '无脱落/起泡', 3, 'OK,NG', 'OK', 3 UNION ALL
  SELECT 'GX07', '端面质量', '无毛刺/变形', 3, 'OK,NG', 'OK', 4
) v ON p.process_code = v.code
WHERE p.process_code = 'GX07';

-- GX08 尺寸检验 检查项
INSERT INTO sx_process_check_item
  (F_ID, F_TENANT_ID, F_CREATOR_TIME, F_CREATOR_USER_ID, F_ENABLED_MARK, F_DELETE_MARK, F_SORT_CODE,
   process_id, process_code, process_name, check_item, check_standard,
   data_type, option_items, default_value, required, seq_no, remark)
SELECT
  REPLACE(UUID(),'-',''), '0', NOW(), '349057407209541', 1, NULL, v.seq_no,
  p.F_ID, p.process_code, p.process_name, v.item, v.standard,
  v.dtype, v.options, v.defval, 1, v.seq_no, ''
FROM sx_base_process p
INNER JOIN (
  SELECT 'GX08' AS code, '总长度' AS item, '图纸标注±0.5mm' AS standard, 1 AS dtype, CAST(NULL AS CHAR) AS options, CAST(NULL AS CHAR) AS defval, 1 AS seq_no UNION ALL
  SELECT 'GX08', '宽度', '图纸标注±0.3mm', 1, NULL, NULL, 2 UNION ALL
  SELECT 'GX08', '厚度', '图纸标注±0.1mm', 1, NULL, NULL, 3 UNION ALL
  SELECT 'GX08', '孔径', '图纸标注±0.05mm', 1, NULL, NULL, 4 UNION ALL
  SELECT 'GX08', '孔位偏差', '≤0.1mm', 1, NULL, NULL, 5
) v ON p.process_code = v.code
WHERE p.process_code = 'GX08';

-- GX01 挤压工序 关键参数记录
INSERT INTO sx_process_check_item
  (F_ID, F_TENANT_ID, F_CREATOR_TIME, F_CREATOR_USER_ID, F_ENABLED_MARK, F_DELETE_MARK, F_SORT_CODE,
   process_id, process_code, process_name, check_item, check_standard,
   data_type, option_items, default_value, required, seq_no, remark)
SELECT
  REPLACE(UUID(),'-',''), '0', NOW(), '349057407209541', 1, NULL, v.seq_no,
  p.F_ID, p.process_code, p.process_name, v.item, v.standard,
  v.dtype, v.options, v.defval, 1, v.seq_no, ''
FROM sx_base_process p
INNER JOIN (
  SELECT 'GX01' AS code, '铝棒温度' AS item, '420-480℃' AS standard, 1 AS dtype, CAST(NULL AS CHAR) AS options, CAST(NULL AS CHAR) AS defval, 1 AS seq_no UNION ALL
  SELECT 'GX01', '挤压速度', '按工艺卡要求', 2, NULL, NULL, 2 UNION ALL
  SELECT 'GX01', '模具温度', '400-450℃', 1, NULL, NULL, 3
) v ON p.process_code = v.code
WHERE p.process_code = 'GX01';

-- ============================================================
-- 5. sx_process_device 工序设备绑定表
-- ============================================================

-- 挤压工序(GX01) → 绑定挤压机
INSERT INTO sx_process_device
  (F_ID, F_TENANT_ID, F_CREATOR_TIME, F_CREATOR_USER_ID, F_ENABLED_MARK, F_DELETE_MARK, F_SORT_CODE,
   process_id, process_code, process_name, device_id, device_code, device_name, remark)
SELECT
  REPLACE(UUID(),'-',''), '0', NOW(), '349057407209541', 1, NULL, 1,
  p.F_ID, p.process_code, p.process_name, d.F_ID, d.device_no, d.device_name, ''
FROM sx_base_process p
INNER JOIN sx_device_master d ON d.device_name LIKE '%挤压%' AND d.devicetype_id IN (SELECT F_ID FROM sx_devicetype_master WHERE devicetype_code = 'SB-EJ-01')
WHERE p.process_code = 'GX01'
LIMIT 1;

-- 时效工序(GX02) → 绑定时效炉
INSERT INTO sx_process_device
  (F_ID, F_TENANT_ID, F_CREATOR_TIME, F_CREATOR_USER_ID, F_ENABLED_MARK, F_DELETE_MARK, F_SORT_CODE,
   process_id, process_code, process_name, device_id, device_code, device_name, remark)
SELECT
  REPLACE(UUID(),'-',''), '0', NOW(), '349057407209541', 1, NULL, 1,
  p.F_ID, p.process_code, p.process_name, d.F_ID, d.device_no, d.device_name, ''
FROM sx_base_process p
INNER JOIN sx_device_master d ON d.device_name LIKE '%时效%' AND d.devicetype_id IN (SELECT F_ID FROM sx_devicetype_master WHERE devicetype_code = 'SB-RE-01')
WHERE p.process_code = 'GX02'
LIMIT 1;

-- CNC工序(GX05) → 绑定CNC设备
INSERT INTO sx_process_device
  (F_ID, F_TENANT_ID, F_CREATOR_TIME, F_CREATOR_USER_ID, F_ENABLED_MARK, F_DELETE_MARK, F_SORT_CODE,
   process_id, process_code, process_name, device_id, device_code, device_name, remark)
SELECT
  REPLACE(UUID(),'-',''), '0', NOW(), '349057407209541', 1, NULL, 1,
  p.F_ID, p.process_code, p.process_name, d.F_ID, d.device_no, d.device_name, ''
FROM sx_base_process p
INNER JOIN sx_device_master d ON d.device_name LIKE '%CNC%' AND d.devicetype_id IN (SELECT F_ID FROM sx_devicetype_master WHERE devicetype_code = 'SB-CNC-01')
WHERE p.process_code = 'GX05'
LIMIT 1;

-- 阳极氧化工序(GX04) → 绑定氧化线
INSERT INTO sx_process_device
  (F_ID, F_TENANT_ID, F_CREATOR_TIME, F_CREATOR_USER_ID, F_ENABLED_MARK, F_DELETE_MARK, F_SORT_CODE,
   process_id, process_code, process_name, device_id, device_code, device_name, remark)
SELECT
  REPLACE(UUID(),'-',''), '0', NOW(), '349057407209541', 1, NULL, 1,
  p.F_ID, p.process_code, p.process_name, d.F_ID, d.device_no, d.device_name, ''
FROM sx_base_process p
INNER JOIN sx_device_master d ON d.device_name LIKE '%氧化%' AND d.devicetype_id IN (SELECT F_ID FROM sx_devicetype_master WHERE devicetype_code = 'SB-YH-01')
WHERE p.process_code = 'GX04'
LIMIT 1;

-- ============================================================
-- 6. sx_process_sop 工序SOP文件表
-- ============================================================

INSERT INTO sx_process_sop
  (F_ID, F_TENANT_ID, F_CREATOR_TIME, F_CREATOR_USER_ID, F_ENABLED_MARK, F_DELETE_MARK, F_SORT_CODE,
   process_id, process_code, process_name,
   file_name, file_type, file_size, file_path, version, remark)
SELECT
  REPLACE(UUID(),'-',''), '0', NOW(), '349057407209541', 1, NULL, v.seq_no,
  p.F_ID, p.process_code, p.process_name,
  v.fname, v.ftype, v.fsize, v.fpath, v.ver, ''
FROM sx_base_process p
INNER JOIN (
  SELECT 'GX01' AS code, '挤压作业指导书V3.pdf' AS fname, 'PDF' AS ftype, '2.4MB' AS fsize, '/sop/GX01/挤压作业指导书V3.pdf' AS fpath, 'V3' AS ver, 1 AS seq_no UNION ALL
  SELECT 'GX02', '时效处理作业指导书V2.pdf', 'PDF', '1.8MB', '/sop/GX02/时效处理作业指导书V2.pdf', 'V2', 2 UNION ALL
  SELECT 'GX03', '精密切割作业指导书V1.pdf', 'PDF', '1.2MB', '/sop/GX03/精密切割作业指导书V1.pdf', 'V1', 3 UNION ALL
  SELECT 'GX04', '阳极氧化作业指导书V2.pdf', 'PDF', '3.1MB', '/sop/GX04/阳极氧化作业指导书V2.pdf', 'V2', 4 UNION ALL
  SELECT 'GX05', 'CNC精加工作业指导书V4.pdf', 'PDF', '4.5MB', '/sop/GX05/CNC精加工作业指导书V4.pdf', 'V4', 5 UNION ALL
  SELECT 'GX07', '外观检验标准V2.pdf', 'PDF', '2.0MB', '/sop/GX07/外观检验标准V2.pdf', 'V2', 6 UNION ALL
  SELECT 'GX08', '尺寸检验作业指导书V1.pdf', 'PDF', '1.5MB', '/sop/GX08/尺寸检验作业指导书V1.pdf', 'V1', 7
) v ON p.process_code = v.code;

-- ============================================================
-- 验证查询
-- ============================================================

SELECT '设备类型' AS tbl, COUNT(*) AS cnt FROM sx_devicetype_master WHERE F_DELETE_MARK IS NULL
UNION ALL
SELECT '设备台账', COUNT(*) FROM sx_device_master WHERE F_DELETE_MARK IS NULL
UNION ALL
SELECT '工序', COUNT(*) FROM sx_base_process WHERE F_DELETE_MARK IS NULL
UNION ALL
SELECT '工艺路线', COUNT(*) FROM sx_routing WHERE F_DELETE_MARK IS NULL
UNION ALL
SELECT '路线工序', COUNT(*) FROM sx_routing_process WHERE F_DELETE_MARK IS NULL
UNION ALL
SELECT '检查项', COUNT(*) FROM sx_process_check_item WHERE F_DELETE_MARK IS NULL
UNION ALL
SELECT '工序设备', COUNT(*) FROM sx_process_device WHERE F_DELETE_MARK IS NULL
UNION ALL
SELECT 'SOP文件', COUNT(*) FROM sx_process_sop WHERE F_DELETE_MARK IS NULL;

-- 设备类型层级
SELECT t1.devicetype_code AS '一级编码', t1.devicetype_name AS '一级名称',
       t2.devicetype_code AS '二级编码', t2.devicetype_name AS '二级名称'
FROM sx_devicetype_master t1
LEFT JOIN sx_devicetype_master t2 ON t2.parent_id = t1.F_ID AND t2.F_DELETE_MARK IS NULL
WHERE t1.parent_id IS NULL AND t1.F_DELETE_MARK IS NULL
ORDER BY t1.F_SORT_CODE;

-- 设备台账列表
SELECT d.device_no, d.device_name, t.devicetype_name, d.use_address, d.device_status
FROM sx_device_master d
LEFT JOIN sx_devicetype_master t ON d.devicetype_id = t.F_ID
WHERE d.F_DELETE_MARK IS NULL
ORDER BY d.F_SORT_CODE;
