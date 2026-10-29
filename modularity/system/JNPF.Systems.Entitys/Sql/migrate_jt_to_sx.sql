-- =============================================================================
-- 迁移脚本：jt_ 前缀表重命名为 sx_ 前缀
-- 数据库类型：MySQL 5.7+ / 8.0
-- 执行时机：在启动后端项目之前执行（确保数据库表名与实体 SugarTable 定义一致）
-- 说明：
--   1. 本脚本将所有 jt_ 前缀的业务表重命名为 sx_ 前缀；
--   2. sx_purchase_arrival_main / sx_purchase_arrival_detail 已是 sx_ 前缀，无需重命名；
--   3. 使用 RENAME TABLE 语句，数据完整保留，不丢失任何记录；
--   4. 执行前请先备份数据库。
-- =============================================================================

-- ---------------------------------------------------------------------------
-- 基础数据模块（4 张）
-- ---------------------------------------------------------------------------
RENAME TABLE `jt_supplier`           TO `sx_supplier`;
RENAME TABLE `jt_terminal_device`    TO `sx_terminal_device`;
RENAME TABLE `jt_mold`               TO `sx_mold`;
RENAME TABLE `jt_exchange_rate`      TO `sx_exchange_rate`;

-- ---------------------------------------------------------------------------
-- 人工管理模块（3 张）
-- ---------------------------------------------------------------------------
RENAME TABLE `jt_base_class`         TO `sx_base_class`;
RENAME TABLE `jt_employee_skill`     TO `sx_employee_skill`;
RENAME TABLE `jt_efficiency`         TO `sx_efficiency`;

-- ---------------------------------------------------------------------------
-- 生产基础数据模块（9 张）
-- ---------------------------------------------------------------------------
RENAME TABLE `jt_base_process`           TO `sx_base_process`;
RENAME TABLE `jt_base_quality`           TO `sx_base_quality`;
RENAME TABLE `jt_base_process_record`    TO `sx_base_process_record`;
RENAME TABLE `jt_base_model`             TO `sx_base_model`;
RENAME TABLE `jt_base_model_process`     TO `sx_base_model_process`;
RENAME TABLE `jt_base_model_bom`         TO `sx_base_model_bom`;
RENAME TABLE `jt_base_order_stop`        TO `sx_base_order_stop`;
RENAME TABLE `jt_line_infos`             TO `sx_line_infos`;
RENAME TABLE `jt_ct_infos`               TO `sx_ct_infos`;

-- ---------------------------------------------------------------------------
-- 生产订单/排产模块（4 张）
-- ---------------------------------------------------------------------------
RENAME TABLE `jt_production_order`           TO `sx_production_order`;
RENAME TABLE `jt_production_schedule`        TO `sx_production_schedule`;
RENAME TABLE `jt_production_schedule_detail` TO `sx_production_schedule_detail`;
RENAME TABLE `jt_production_schedule_log`    TO `sx_production_schedule_log`;

-- ---------------------------------------------------------------------------
-- 工艺路线/工序模块（5 张）
-- ---------------------------------------------------------------------------
RENAME TABLE `jt_routing`               TO `sx_routing`;
RENAME TABLE `jt_routing_process`        TO `sx_routing_process`;
RENAME TABLE `jt_process_check_item`     TO `sx_process_check_item`;
RENAME TABLE `jt_process_device`         TO `sx_process_device`;
RENAME TABLE `jt_process_sop`            TO `sx_process_sop`;

-- ---------------------------------------------------------------------------
-- 存货/BOM 模块（3 张）
-- ---------------------------------------------------------------------------
RENAME TABLE `jt_inventory_master`       TO `sx_inventory_master`;
RENAME TABLE `jt_inventory_bom`          TO `sx_inventory_bom`;
RENAME TABLE `jt_inventory_check_item`   TO `sx_inventory_check_item`;

-- ---------------------------------------------------------------------------
-- 流程卡模块（3 张）
-- ---------------------------------------------------------------------------
RENAME TABLE `jt_flow_card`              TO `sx_flow_card`;
RENAME TABLE `jt_flow_card_process`      TO `sx_flow_card_process`;
RENAME TABLE `jt_flow_card_relation`     TO `sx_flow_card_relation`;

-- ---------------------------------------------------------------------------
-- 报工模块（2 张）
-- ---------------------------------------------------------------------------
RENAME TABLE `jt_work_report`            TO `sx_work_report`;
RENAME TABLE `jt_report_check_detail`    TO `sx_report_check_detail`;

-- ---------------------------------------------------------------------------
-- 清洗合并模块（2 张）
-- ---------------------------------------------------------------------------
RENAME TABLE `jt_clean_merge_config`     TO `sx_clean_merge_config`;
RENAME TABLE `jt_clean_merge_member`     TO `sx_clean_merge_member`;

-- ---------------------------------------------------------------------------
-- 质量检验模块（4 张）
-- ---------------------------------------------------------------------------
RENAME TABLE `jt_iqc`                    TO `sx_iqc`;
RENAME TABLE `jt_iqc_detail`            TO `sx_iqc_detail`;
RENAME TABLE `jt_oqc`                    TO `sx_oqc`;
RENAME TABLE `jt_oqc_detail`            TO `sx_oqc_detail`;

-- ---------------------------------------------------------------------------
-- 设备管理模块（13 张）
-- ---------------------------------------------------------------------------
RENAME TABLE `jt_device_master`              TO `sx_device_master`;
RENAME TABLE `jt_devicetype_master`          TO `sx_devicetype_master`;
RENAME TABLE `jt_devicetype_releation`       TO `sx_devicetype_releation`;
RENAME TABLE `jt_v_device_sort_config`       TO `sx_v_device_sort_config`;
RENAME TABLE `jt_device_item_main`           TO `sx_device_item_main`;
RENAME TABLE `jt_device_item_sub`            TO `sx_device_item_sub`;
RENAME TABLE `jt_device_item_allot`          TO `sx_device_item_allot`;
RENAME TABLE `jt_device_alarm`               TO `sx_device_alarm`;
RENAME TABLE `jt_device_repair`              TO `sx_device_repair`;
RENAME TABLE `jt_device_repair_record`       TO `sx_device_repair_record`;
RENAME TABLE `jt_device_check`               TO `sx_device_check`;
RENAME TABLE `jt_device_check_task`          TO `sx_device_check_task`;
RENAME TABLE `jt_device_check_task_version`  TO `sx_device_check_task_version`;

-- ---------------------------------------------------------------------------
-- 设备保养模块（5 张）
-- ---------------------------------------------------------------------------
RENAME TABLE `jt_maintenance_projects`        TO `sx_maintenance_projects`;
RENAME TABLE `jt_maintenance_project_device`  TO `sx_maintenance_project_device`;
RENAME TABLE `jt_maintenance_plan`            TO `sx_maintenance_plan`;
RENAME TABLE `jt_maintenance_record`          TO `sx_maintenance_record`;
RENAME TABLE `jt_maintenance_record_spare`    TO `sx_maintenance_record_spare`;

-- ---------------------------------------------------------------------------
-- 仓库/库存模块（8 张）
-- ---------------------------------------------------------------------------
RENAME TABLE `jt_warehouse`              TO `sx_warehouse`;
RENAME TABLE `jt_warehouse_location`     TO `sx_warehouse_location`;
RENAME TABLE `jt_warehouse_bom`          TO `sx_warehouse_bom`;
RENAME TABLE `jt_warehouse_receive`      TO `sx_warehouse_receive`;
RENAME TABLE `jt_inventory_stock`        TO `sx_inventory_stock`;
RENAME TABLE `jt_stock_flow`             TO `sx_stock_flow`;
RENAME TABLE `jt_stock_in`               TO `sx_stock_in`;
RENAME TABLE `jt_out_stock`              TO `sx_out_stock`;

-- ---------------------------------------------------------------------------
-- 领料/入库单/出库单模块（5 张）
-- ---------------------------------------------------------------------------
RENAME TABLE `jt_picking_record`         TO `sx_picking_record`;
RENAME TABLE `jt_in_order`               TO `sx_in_order`;
RENAME TABLE `jt_in_order_detail`        TO `sx_in_order_detail`;
RENAME TABLE `jt_out_order`              TO `sx_out_order`;
RENAME TABLE `jt_out_order_detail`       TO `sx_out_order_detail`;

-- ---------------------------------------------------------------------------
-- 排产计划（JNPF 代码生成实体，jt_base_order → sx_base_order）
-- ---------------------------------------------------------------------------
RENAME TABLE `jt_base_order`             TO `sx_base_order`;

-- ---------------------------------------------------------------------------
-- 用友同步模块（1 张）
-- ---------------------------------------------------------------------------
RENAME TABLE `jt_yonyou_sync_log`        TO `sx_yonyou_sync_log`;

-- =============================================================================
-- 以下为已是 sx_ 前缀的表（无需重命名，仅做确认）：
--   sx_purchase_arrival_main
--   sx_purchase_arrival_detail
-- =============================================================================

-- 执行完成后验证：
-- SELECT TABLE_NAME FROM information_schema.TABLES
-- WHERE TABLE_SCHEMA = '你的数据库名' AND TABLE_NAME LIKE 'jt_%';
-- 结果应为空（无 jt_ 前缀表残留）。
