-- =============================================================================
-- 模块名称：仓库管理（仓库档案 / 货位档案 / 库存查询 / 领料记录 / 入库记录 / 出库记录）
--           + 单据（入库单管理 / 出库单管理）
-- 数据库类型：MySQL 5.7+ / 8.0（utf8mb4）
-- 说明：
--   1. 仓库基础数据从用友U8同步（编码/名称/计价方式/是否货位管理/仓库属性/条形码）；
--      货位按「区-排-列-层」编码（如A1-1-1），支持最大体积/重量校验与条形码绑定，
--      PDA扫码入库时按货位码落位（既有 sx_warehouse_location 表本脚本扩展列）；
--   2. 库存查询 = MES实时库存（在库数量/可用数量/占用数量/安全库存），
--      库存状态=低于安全库存时预警；可用量 = 在库 - 占用；每次出入库审核后
--      自动推送用友U8存货核算变更现有量（模拟，见 sx_yonyou_sync_log）；
--   3. 入库统一按「批次建档 + 货位归位」处理，同存货多批次独立建档（为FIFO准备）；
--      数据校验：到货单号非空的入库单流程卡必填（到货必须先打印流程卡）；
--      无到货单来源（盘盈/退料/其他）才允许无流程卡；
--   4. 出库先入先出：系统自动锁定最早入库批次库位（按 sx_inventory_stock.in_time 排序）；
--      材料出库由PDA领料记录自动生成关联，本表不重复录入；
--   5. 所有表继承 JNPF 框架 CLDSEntityBase 公共字段（创建/修改/删除/启用/租户）。
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 表 1：sx_warehouse 仓库档案表
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_warehouse`;
CREATE TABLE `sx_warehouse` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `repo_code`              VARCHAR(50)   NOT NULL                COMMENT '仓库编码(如01/02/03/09/91/92/93)',
  `repo_name`              VARCHAR(200)  NOT NULL                COMMENT '仓库名称(如成品仓库)',
  `dept_name`              VARCHAR(200)  NULL     DEFAULT NULL   COMMENT '部门名称',
  `address`                VARCHAR(300)  NULL     DEFAULT NULL   COMMENT '仓库地址',
  `charge_person`          VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '负责人',
  `pricing_method`         VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '计价方式(如:全月平均法)',
  `is_location_manage`     INT           NULL     DEFAULT 0      COMMENT '是否货位管理:0否 1是',
  `repo_attr`              VARCHAR(50)   NULL     DEFAULT '普通仓' COMMENT '仓库属性:普通仓/现场仓',
  `barcode`                VARCHAR(100)  NULL     DEFAULT NULL   COMMENT '对应条形码',
  `location_count`         INT           NULL     DEFAULT 0      COMMENT '货位数量',
  `source`                 INT           NULL     DEFAULT 1      COMMENT '来源:1用友U8同步 2手工新增',
  `sync_time`              DATETIME      NULL     DEFAULT NULL   COMMENT '最近同步时间',
  `remark`                 VARCHAR(500)  NULL     DEFAULT NULL   COMMENT '备注',
  `F_SORT_CODE`            BIGINT        NULL     DEFAULT 0      COMMENT '排序码',
  `F_ENABLED_MARK`         INT           NULL     DEFAULT 1      COMMENT '启用标识:0禁用 1启用',
  `F_CREATOR_TIME`         DATETIME      NULL     DEFAULT NULL   COMMENT '创建时间',
  `F_CREATOR_USER_ID`      VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '创建用户',
  `F_LAST_MODIFY_TIME`     DATETIME      NULL     DEFAULT NULL   COMMENT '修改时间',
  `F_LAST_MODIFY_USER_ID`  VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '修改用户',
  `F_DELETE_MARK`          INT           NULL     DEFAULT NULL   COMMENT '删除标志:1已删除',
  `F_DELETE_TIME`          DATETIME      NULL     DEFAULT NULL   COMMENT '删除时间',
  `F_DELETE_USER_ID`       VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '删除用户',
  PRIMARY KEY (`F_ID`),
  KEY `idx_wh_code` (`repo_code`),
  KEY `idx_wh_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='仓库档案表(仓库管理,用友同步)';

-- -----------------------------------------------------------------------------
-- 扩展：sx_warehouse_location 货位档案表增加货位管理列
-- （按「仓库 → 货位」两级结构；编码规则 区-排-列-层 如A1-1-1；支持体积/重量校验+条形码）
-- 注意：ALTER 语句仅需执行一次；若已执行请忽略报错（Duplicate column name）。
-- -----------------------------------------------------------------------------
ALTER TABLE `sx_warehouse_location`
  ADD COLUMN `warehouse_id`   VARCHAR(50)   NULL DEFAULT NULL COMMENT '仓库ID(sx_warehouse.F_ID)' AFTER `warehouse_name`,
  ADD COLUMN `warehouse_code` VARCHAR(50)   NULL DEFAULT NULL COMMENT '仓库编码(冗余)' AFTER `warehouse_id`,
  ADD COLUMN `location_code`  VARCHAR(50)   NULL DEFAULT NULL COMMENT '货位编码(区-排-列-层,如A1-1-1)' AFTER `warehouse_code`,
  ADD COLUMN `area_no`        VARCHAR(20)   NULL DEFAULT NULL COMMENT '区号' AFTER `location_code`,
  ADD COLUMN `row_no`         VARCHAR(20)   NULL DEFAULT NULL COMMENT '排号' AFTER `area_no`,
  ADD COLUMN `column_no`      VARCHAR(20)   NULL DEFAULT NULL COMMENT '列号' AFTER `row_no`,
  ADD COLUMN `layer_no`       VARCHAR(20)   NULL DEFAULT NULL COMMENT '层号' AFTER `column_no`,
  ADD COLUMN `max_volume`     DECIMAL(18,4) NULL DEFAULT 0    COMMENT '最大体积(m³,校验用)' AFTER `layer_no`,
  ADD COLUMN `max_weight`     DECIMAL(18,4) NULL DEFAULT 0    COMMENT '最大重量(kg,校验用)' AFTER `max_volume`,
  ADD COLUMN `barcode`        VARCHAR(100)  NULL DEFAULT NULL COMMENT '条形码(PDA扫码识别)' AFTER `max_weight`,
  ADD COLUMN `status`         INT           NULL DEFAULT 1    COMMENT '状态:1可用 0停用' AFTER `barcode`;

-- -----------------------------------------------------------------------------
-- 表 2：sx_inventory_stock 库存台账表（MES实时库存）
--   （批次建档：同存货多批次独立建档；可用量=在库-占用，出库按in_time最早批次FIFO）
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_inventory_stock`;
CREATE TABLE `sx_inventory_stock` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `inventory_id`           VARCHAR(50)   NOT NULL                COMMENT '存货ID(sx_inventory_master.F_ID)',
  `inventory_code`         VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '存货编码(冗余)',
  `inventory_name`         VARCHAR(200)  NULL     DEFAULT NULL   COMMENT '存货名称(冗余)',
  `spec`                   VARCHAR(200)  NULL     DEFAULT NULL   COMMENT '规格型号(冗余)',
  `unit`                   VARCHAR(20)   NULL     DEFAULT NULL   COMMENT '单位(冗余)',
  `warehouse_id`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '仓库ID(sx_warehouse.F_ID)',
  `warehouse_code`         VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '仓库编码(冗余)',
  `warehouse_name`         VARCHAR(200)  NULL     DEFAULT NULL   COMMENT '仓库名称(冗余)',
  `location_id`            VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '货位ID(sx_warehouse_location.F_ID)',
  `location_code`          VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '货位编码(冗余)',
  `batch_no`               VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '批次号(同存货多批次独立建档)',
  `stock_qty`              DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '在库数量',
  `occupied_qty`           DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '占用数量(可用量=在库-占用)',
  `safe_qty`               DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '安全库存(低于此值预警)',
  `in_time`                DATETIME      NULL     DEFAULT NULL   COMMENT '入库时间(先进先出排序依据)',
  `produce_date`           DATE          NULL     DEFAULT NULL   COMMENT '生产日期',
  `status`                 INT           NULL     DEFAULT 1      COMMENT '状态:1正常 0冻结',
  `remark`                 VARCHAR(500)  NULL     DEFAULT NULL   COMMENT '备注',
  `F_SORT_CODE`            BIGINT        NULL     DEFAULT 0      COMMENT '排序码',
  `F_ENABLED_MARK`         INT           NULL     DEFAULT 1      COMMENT '启用标识:0禁用 1启用',
  `F_CREATOR_TIME`         DATETIME      NULL     DEFAULT NULL   COMMENT '创建时间',
  `F_CREATOR_USER_ID`      VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '创建用户',
  `F_LAST_MODIFY_TIME`     DATETIME      NULL     DEFAULT NULL   COMMENT '修改时间',
  `F_LAST_MODIFY_USER_ID`  VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '修改用户',
  `F_DELETE_MARK`          INT           NULL     DEFAULT NULL   COMMENT '删除标志:1已删除',
  `F_DELETE_TIME`          DATETIME      NULL     DEFAULT NULL   COMMENT '删除时间',
  `F_DELETE_USER_ID`       VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '删除用户',
  PRIMARY KEY (`F_ID`),
  KEY `idx_st_inventory` (`inventory_id`),
  KEY `idx_st_batch` (`inventory_code`, `batch_no`),
  KEY `idx_st_wh` (`warehouse_id`),
  KEY `idx_st_intime` (`in_time`),
  KEY `idx_st_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='库存台账表(MES实时库存,批次独立建档,FIFO)';

-- -----------------------------------------------------------------------------
-- 表 3：sx_stock_flow 库存流水表
--   （出入库审核后写入，记录变动前后数量，审核后推送用友变更现有量）
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_stock_flow`;
CREATE TABLE `sx_stock_flow` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `inventory_id`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '存货ID',
  `inventory_code`         VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '存货编码(冗余)',
  `inventory_name`         VARCHAR(200)  NULL     DEFAULT NULL   COMMENT '存货名称(冗余)',
  `warehouse_name`         VARCHAR(200)  NULL     DEFAULT NULL   COMMENT '仓库名称(冗余)',
  `location_code`          VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '货位编码(冗余)',
  `batch_no`               VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '批次号',
  `biz_type`               INT           NULL     DEFAULT 1      COMMENT '业务类型:1采购入库 2产成品入库 3其它入库 4销售出库 5其他出库 6领料出库',
  `direction`              INT           NULL     DEFAULT 1      COMMENT '方向:1入库 2出库',
  `qty`                    DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '变动数量',
  `before_qty`             DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '变动前在库数量',
  `after_qty`              DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '变动后在库数量',
  `biz_id`                 VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '关联业务单据ID(入库记录/出库记录/领料记录)',
  `biz_no`                 VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '关联业务单号(入库单号/出库单号/领料单号)',
  `order_no`               VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '关联生产订单号(领料/产成品入库)',
  `flow_card_no`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '关联流程卡号',
  `operator_name`          VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '操作人',
  `operate_time`           DATETIME      NULL     DEFAULT NULL   COMMENT '操作时间',
  `remark`                 VARCHAR(500)  NULL     DEFAULT NULL   COMMENT '备注',
  `F_SORT_CODE`            BIGINT        NULL     DEFAULT 0      COMMENT '排序码',
  `F_ENABLED_MARK`         INT           NULL     DEFAULT 1      COMMENT '启用标识:0禁用 1启用',
  `F_CREATOR_TIME`         DATETIME      NULL     DEFAULT NULL   COMMENT '创建时间',
  `F_CREATOR_USER_ID`      VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '创建用户',
  `F_LAST_MODIFY_TIME`     DATETIME      NULL     DEFAULT NULL   COMMENT '修改时间',
  `F_LAST_MODIFY_USER_ID`  VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '修改用户',
  `F_DELETE_MARK`          INT           NULL     DEFAULT NULL   COMMENT '删除标志:1已删除',
  `F_DELETE_TIME`          DATETIME      NULL     DEFAULT NULL   COMMENT '删除时间',
  `F_DELETE_USER_ID`       VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '删除用户',
  PRIMARY KEY (`F_ID`),
  KEY `idx_sf_inventory` (`inventory_code`),
  KEY `idx_sf_biz` (`biz_no`),
  KEY `idx_sf_card` (`flow_card_no`),
  KEY `idx_sf_type` (`biz_type`),
  KEY `idx_sf_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='库存流水表(出入库实时更新)';

-- -----------------------------------------------------------------------------
-- 表 4：sx_picking_record 领料记录表（主数据，材料出库记录由其自动生成）
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_picking_record`;
CREATE TABLE `sx_picking_record` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `picking_no`             VARCHAR(50)   NOT NULL                COMMENT '领料单号(业务唯一,如LL20261010001)',
  `order_id`               VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '生产订单ID',
  `order_no`               VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '生产订单号(冗余)',
  `flow_card_id`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '流程卡ID(sx_flow_card.F_ID)',
  `flow_card_no`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '流程卡号(冗余)',
  `process_id`             VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '工序ID(sx_base_process.F_ID)',
  `process_name`           VARCHAR(100)  NULL     DEFAULT NULL   COMMENT '工序(冗余)',
  `inventory_id`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '物料ID(sx_inventory_master.F_ID)',
  `inventory_code`         VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '物料编码(冗余)',
  `inventory_name`         VARCHAR(200)  NULL     DEFAULT NULL   COMMENT '物料名称(冗余)',
  `should_qty`             DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '应发数量',
  `actual_qty`             DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '实发数量',
  `warehouse_id`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '仓库ID',
  `warehouse_name`         VARCHAR(200)  NULL     DEFAULT NULL   COMMENT '仓库名称(冗余)',
  `location_id`            VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '货位ID',
  `location_code`          VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '库位(先进先出取最早批次库位)',
  `batch_no`               VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '批次号(最早入库批次)',
  `picker_name`            VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '领料人',
  `pick_time`              DATETIME      NULL     DEFAULT NULL   COMMENT '领料时间',
  `status`                 INT           NULL     DEFAULT 1      COMMENT '状态:1待审核 2已审核 3已作废',
  `out_stock_id`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '关联材料出库记录ID(sx_out_stock.F_ID,自动生成)',
  `sync_status`            INT           NULL     DEFAULT 0      COMMENT '用友同步状态:0待同步 1已同步 2同步失败',
  `remark`                 VARCHAR(500)  NULL     DEFAULT NULL   COMMENT '备注',
  `F_SORT_CODE`            BIGINT        NULL     DEFAULT 0      COMMENT '排序码',
  `F_ENABLED_MARK`         INT           NULL     DEFAULT 1      COMMENT '启用标识:0禁用 1启用',
  `F_CREATOR_TIME`         DATETIME      NULL     DEFAULT NULL   COMMENT '创建时间',
  `F_CREATOR_USER_ID`      VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '创建用户',
  `F_LAST_MODIFY_TIME`     DATETIME      NULL     DEFAULT NULL   COMMENT '修改时间',
  `F_LAST_MODIFY_USER_ID`  VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '修改用户',
  `F_DELETE_MARK`          INT           NULL     DEFAULT NULL   COMMENT '删除标志:1已删除',
  `F_DELETE_TIME`          DATETIME      NULL     DEFAULT NULL   COMMENT '删除时间',
  `F_DELETE_USER_ID`       VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '删除用户',
  PRIMARY KEY (`F_ID`),
  UNIQUE KEY `uk_pr_no` (`picking_no`),
  KEY `idx_pr_order` (`order_no`),
  KEY `idx_pr_card` (`flow_card_no`),
  KEY `idx_pr_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='领料记录表(主数据,材料出库自动关联)';

-- -----------------------------------------------------------------------------
-- 表 5：sx_stock_in 入库记录表
--   （采购入库/产成品入库/其他入库；含入库方式与批次、货位归位信息）
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_stock_in`;
CREATE TABLE `sx_stock_in` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `in_no`                  VARCHAR(50)   NOT NULL                COMMENT '入库单号(业务唯一,如RK20261010001)',
  `in_type`                INT           NULL     DEFAULT 1      COMMENT '入库类型:1采购入库 2产成品入库 3其他入库',
  `in_mode`                INT           NULL     DEFAULT 1      COMMENT '入库方式:1扫码流程卡入库 2直接入库',
  `arrival_id`             VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '到货单ID(sx_sx_purchase_arrival_main.F_ID)',
  `arrival_no`             VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '到货单号(非空时流程卡必填)',
  `flow_card_id`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '流程卡ID(sx_flow_card.F_ID)',
  `flow_card_no`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '流程卡号(冗余)',
  `inventory_id`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '存货ID',
  `inventory_code`         VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '存货编码(冗余)',
  `inventory_name`         VARCHAR(200)  NULL     DEFAULT NULL   COMMENT '存货名称(冗余)',
  `qty`                    DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '入库数量',
  `unit`                   VARCHAR(20)   NULL     DEFAULT NULL   COMMENT '单位',
  `warehouse_id`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '仓库ID',
  `warehouse_code`         VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '仓库编码(冗余)',
  `warehouse_name`         VARCHAR(200)  NULL     DEFAULT NULL   COMMENT '仓库名称(冗余)',
  `location_id`            VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '货位ID(扫库位码落位)',
  `location_code`          VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '货位编码(冗余)',
  `batch_no`               VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '批次号(批次建档)',
  `has_odd_box`            INT           NULL     DEFAULT 0      COMMENT '有无零头箱(产成品入库):0无 1有',
  `operator_name`          VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '操作人',
  `in_time`                DATETIME      NULL     DEFAULT NULL   COMMENT '入库时间',
  `status`                 INT           NULL     DEFAULT 1      COMMENT '状态:1待审核 2已入库',
  `sync_status`            INT           NULL     DEFAULT 0      COMMENT '用友同步状态:0待同步 1已同步 2同步失败',
  `remark`                 VARCHAR(500)  NULL     DEFAULT NULL   COMMENT '备注',
  `F_SORT_CODE`            BIGINT        NULL     DEFAULT 0      COMMENT '排序码',
  `F_ENABLED_MARK`         INT           NULL     DEFAULT 1      COMMENT '启用标识:0禁用 1启用',
  `F_CREATOR_TIME`         DATETIME      NULL     DEFAULT NULL   COMMENT '创建时间',
  `F_CREATOR_USER_ID`      VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '创建用户',
  `F_LAST_MODIFY_TIME`     DATETIME      NULL     DEFAULT NULL   COMMENT '修改时间',
  `F_LAST_MODIFY_USER_ID`  VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '修改用户',
  `F_DELETE_MARK`          INT           NULL     DEFAULT NULL   COMMENT '删除标志:1已删除',
  `F_DELETE_TIME`          DATETIME      NULL     DEFAULT NULL   COMMENT '删除时间',
  `F_DELETE_USER_ID`       VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '删除用户',
  PRIMARY KEY (`F_ID`),
  UNIQUE KEY `uk_si_no` (`in_no`),
  KEY `idx_si_arrival` (`arrival_no`),
  KEY `idx_si_card` (`flow_card_no`),
  KEY `idx_si_inventory` (`inventory_code`),
  KEY `idx_si_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='入库记录表(PDA出入库实时更新)';

-- -----------------------------------------------------------------------------
-- 表 6：sx_out_stock 出库记录表
--   （销售出库/其他出库/材料出库；先进先出自动锁定最早入库批次库位）
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_out_stock`;
CREATE TABLE `sx_out_stock` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `out_no`                 VARCHAR(50)   NOT NULL                COMMENT '出库单号(业务唯一,如CK20261010001)',
  `out_type`               INT           NULL     DEFAULT 1      COMMENT '出库类型:1销售出库 2其他出库 3材料出库',
  `order_id`               VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '关联生产订单ID(领料/产成品)',
  `order_no`               VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '关联订单号(冗余)',
  `flow_card_id`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '流程卡ID',
  `flow_card_no`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '流程卡号(冗余)',
  `picking_id`             VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '关联领料记录ID(材料出库由领料自动生成,不重复录入)',
  `inventory_id`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '存货ID',
  `inventory_code`         VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '存货编码(冗余)',
  `inventory_name`         VARCHAR(200)  NULL     DEFAULT NULL   COMMENT '存货名称(冗余)',
  `qty`                    DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '出库数量',
  `unit`                   VARCHAR(20)   NULL     DEFAULT NULL   COMMENT '单位',
  `warehouse_id`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '仓库ID',
  `warehouse_name`         VARCHAR(200)  NULL     DEFAULT NULL   COMMENT '仓库名称(冗余)',
  `location_id`            VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '货位ID(先进先出锁定)',
  `location_code`          VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '库位(冗余)',
  `batch_no`               VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '批次号(最早入库批次)',
  `fifo_check`             INT           NULL     DEFAULT 1      COMMENT '先进先出校验:1已通过',
  `operator_name`          VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '操作人',
  `out_time`               DATETIME      NULL     DEFAULT NULL   COMMENT '出库时间',
  `status`                 INT           NULL     DEFAULT 1      COMMENT '状态:1待审核 2已出库',
  `sync_status`            INT           NULL     DEFAULT 0      COMMENT '用友同步状态:0待同步 1已同步 2同步失败',
  `remark`                 VARCHAR(500)  NULL     DEFAULT NULL   COMMENT '备注',
  `F_SORT_CODE`            BIGINT        NULL     DEFAULT 0      COMMENT '排序码',
  `F_ENABLED_MARK`         INT           NULL     DEFAULT 1      COMMENT '启用标识:0禁用 1启用',
  `F_CREATOR_TIME`         DATETIME      NULL     DEFAULT NULL   COMMENT '创建时间',
  `F_CREATOR_USER_ID`      VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '创建用户',
  `F_LAST_MODIFY_TIME`     DATETIME      NULL     DEFAULT NULL   COMMENT '修改时间',
  `F_LAST_MODIFY_USER_ID`  VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '修改用户',
  `F_DELETE_MARK`          INT           NULL     DEFAULT NULL   COMMENT '删除标志:1已删除',
  `F_DELETE_TIME`          DATETIME      NULL     DEFAULT NULL   COMMENT '删除时间',
  `F_DELETE_USER_ID`       VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '删除用户',
  PRIMARY KEY (`F_ID`),
  UNIQUE KEY `uk_so_no` (`out_no`),
  KEY `idx_so_card` (`flow_card_no`),
  KEY `idx_so_inventory` (`inventory_code`),
  KEY `idx_so_picking` (`picking_id`),
  KEY `idx_so_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='出库记录表(先进先出锁定最早批次)';

-- -----------------------------------------------------------------------------
-- 表 7：sx_in_order 入库单主表（单据-入库单管理）
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_in_order`;
CREATE TABLE `sx_in_order` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `order_no`               VARCHAR(50)   NOT NULL                COMMENT '入库单号(业务唯一)',
  `in_type`                INT           NULL     DEFAULT 1      COMMENT '入库类型:1采购入库 2产成品入库 3其他入库',
  `in_mode`                INT           NULL     DEFAULT 1      COMMENT '入库方式:1扫码流程卡入库 2直接入库',
  `arrival_id`             VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '到货单ID',
  `arrival_no`             VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '到货单号',
  `supplier_name`          VARCHAR(200)  NULL     DEFAULT NULL   COMMENT '供应商(冗余)',
  `flow_card_no`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '流程卡号',
  `warehouse_id`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '仓库ID',
  `warehouse_name`         VARCHAR(200)  NULL     DEFAULT NULL   COMMENT '仓库名称(冗余)',
  `status`                 INT           NULL     DEFAULT 1      COMMENT '状态:1草稿 2待审核 3已入库 4已作废',
  `audit_user`             VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '审核人',
  `audit_time`             DATETIME      NULL     DEFAULT NULL   COMMENT '审核时间',
  `sync_status`            INT           NULL     DEFAULT 0      COMMENT '用友同步状态:0待同步 1已同步 2同步失败',
  `remark`                 VARCHAR(500)  NULL     DEFAULT NULL   COMMENT '备注',
  `F_SORT_CODE`            BIGINT        NULL     DEFAULT 0      COMMENT '排序码',
  `F_ENABLED_MARK`         INT           NULL     DEFAULT 1      COMMENT '启用标识:0禁用 1启用',
  `F_CREATOR_TIME`         DATETIME      NULL     DEFAULT NULL   COMMENT '创建时间',
  `F_CREATOR_USER_ID`      VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '创建用户',
  `F_LAST_MODIFY_TIME`     DATETIME      NULL     DEFAULT NULL   COMMENT '修改时间',
  `F_LAST_MODIFY_USER_ID`  VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '修改用户',
  `F_DELETE_MARK`          INT           NULL     DEFAULT NULL   COMMENT '删除标志:1已删除',
  `F_DELETE_TIME`          DATETIME      NULL     DEFAULT NULL   COMMENT '删除时间',
  `F_DELETE_USER_ID`       VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '删除用户',
  PRIMARY KEY (`F_ID`),
  UNIQUE KEY `uk_io_no` (`order_no`),
  KEY `idx_io_arrival` (`arrival_no`),
  KEY `idx_io_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='入库单主表(单据管理)';

-- -----------------------------------------------------------------------------
-- 表 8：sx_in_order_detail 入库单明细表
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_in_order_detail`;
CREATE TABLE `sx_in_order_detail` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `order_id`               VARCHAR(50)   NOT NULL                COMMENT '入库单主表ID(sx_in_order.F_ID)',
  `inventory_id`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '存货ID',
  `inventory_code`         VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '存货编码(冗余)',
  `inventory_name`         VARCHAR(200)  NULL     DEFAULT NULL   COMMENT '存货名称(冗余)',
  `spec`                   VARCHAR(200)  NULL     DEFAULT NULL   COMMENT '规格型号(冗余)',
  `unit`                   VARCHAR(20)   NULL     DEFAULT NULL   COMMENT '单位(冗余)',
  `qty`                    DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '入库数量',
  `batch_no`               VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '批次号',
  `location_code`          VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '货位编码',
  `remark`                 VARCHAR(500)  NULL     DEFAULT NULL   COMMENT '备注',
  `F_SORT_CODE`            BIGINT        NULL     DEFAULT 0      COMMENT '排序码',
  `F_ENABLED_MARK`         INT           NULL     DEFAULT 1      COMMENT '启用标识:0禁用 1启用',
  `F_CREATOR_TIME`         DATETIME      NULL     DEFAULT NULL   COMMENT '创建时间',
  `F_CREATOR_USER_ID`      VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '创建用户',
  `F_LAST_MODIFY_TIME`     DATETIME      NULL     DEFAULT NULL   COMMENT '修改时间',
  `F_LAST_MODIFY_USER_ID`  VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '修改用户',
  `F_DELETE_MARK`          INT           NULL     DEFAULT NULL   COMMENT '删除标志:1已删除',
  `F_DELETE_TIME`          DATETIME      NULL     DEFAULT NULL   COMMENT '删除时间',
  `F_DELETE_USER_ID`       VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '删除用户',
  PRIMARY KEY (`F_ID`),
  KEY `idx_iod_order` (`order_id`),
  KEY `idx_iod_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='入库单明细表';

-- -----------------------------------------------------------------------------
-- 表 9：sx_out_order 出库单主表（单据-出库单管理）
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_out_order`;
CREATE TABLE `sx_out_order` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `order_no`               VARCHAR(50)   NOT NULL                COMMENT '出库单号(业务唯一)',
  `out_type`               INT           NULL     DEFAULT 1      COMMENT '出库类型:1销售出库 2其他出库 3材料出库',
  `customer`               VARCHAR(200)  NULL     DEFAULT NULL   COMMENT '客户(销售出库)',
  `order_id`               VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '关联生产订单ID',
  `order_no_ref`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '关联生产订单号(冗余)',
  `flow_card_no`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '流程卡号',
  `warehouse_id`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '仓库ID',
  `warehouse_name`         VARCHAR(200)  NULL     DEFAULT NULL   COMMENT '仓库名称(冗余)',
  `status`                 INT           NULL     DEFAULT 1      COMMENT '状态:1草稿 2待审核 3已出库 4已作废',
  `audit_user`             VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '审核人',
  `audit_time`             DATETIME      NULL     DEFAULT NULL   COMMENT '审核时间',
  `sync_status`            INT           NULL     DEFAULT 0      COMMENT '用友同步状态:0待同步 1已同步 2同步失败',
  `remark`                 VARCHAR(500)  NULL     DEFAULT NULL   COMMENT '备注',
  `F_SORT_CODE`            BIGINT        NULL     DEFAULT 0      COMMENT '排序码',
  `F_ENABLED_MARK`         INT           NULL     DEFAULT 1      COMMENT '启用标识:0禁用 1启用',
  `F_CREATOR_TIME`         DATETIME      NULL     DEFAULT NULL   COMMENT '创建时间',
  `F_CREATOR_USER_ID`      VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '创建用户',
  `F_LAST_MODIFY_TIME`     DATETIME      NULL     DEFAULT NULL   COMMENT '修改时间',
  `F_LAST_MODIFY_USER_ID`  VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '修改用户',
  `F_DELETE_MARK`          INT           NULL     DEFAULT NULL   COMMENT '删除标志:1已删除',
  `F_DELETE_TIME`          DATETIME      NULL     DEFAULT NULL   COMMENT '删除时间',
  `F_DELETE_USER_ID`       VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '删除用户',
  PRIMARY KEY (`F_ID`),
  UNIQUE KEY `uk_oo_no` (`order_no`),
  KEY `idx_oo_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='出库单主表(单据管理)';

-- -----------------------------------------------------------------------------
-- 表 10：sx_out_order_detail 出库单明细表
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_out_order_detail`;
CREATE TABLE `sx_out_order_detail` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `order_id`               VARCHAR(50)   NOT NULL                COMMENT '出库单主表ID(sx_out_order.F_ID)',
  `inventory_id`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '存货ID',
  `inventory_code`         VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '存货编码(冗余)',
  `inventory_name`         VARCHAR(200)  NULL     DEFAULT NULL   COMMENT '存货名称(冗余)',
  `spec`                   VARCHAR(200)  NULL     DEFAULT NULL   COMMENT '规格型号(冗余)',
  `unit`                   VARCHAR(20)   NULL     DEFAULT NULL   COMMENT '单位(冗余)',
  `qty`                    DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '出库数量',
  `batch_no`               VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '批次号(先进先出)',
  `location_code`          VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '库位',
  `remark`                 VARCHAR(500)  NULL     DEFAULT NULL   COMMENT '备注',
  `F_SORT_CODE`            BIGINT        NULL     DEFAULT 0      COMMENT '排序码',
  `F_ENABLED_MARK`         INT           NULL     DEFAULT 1      COMMENT '启用标识:0禁用 1启用',
  `F_CREATOR_TIME`         DATETIME      NULL     DEFAULT NULL   COMMENT '创建时间',
  `F_CREATOR_USER_ID`      VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '创建用户',
  `F_LAST_MODIFY_TIME`     DATETIME      NULL     DEFAULT NULL   COMMENT '修改时间',
  `F_LAST_MODIFY_USER_ID`  VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '修改用户',
  `F_DELETE_MARK`          INT           NULL     DEFAULT NULL   COMMENT '删除标志:1已删除',
  `F_DELETE_TIME`          DATETIME      NULL     DEFAULT NULL   COMMENT '删除时间',
  `F_DELETE_USER_ID`       VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '删除用户',
  PRIMARY KEY (`F_ID`),
  KEY `idx_ood_order` (`order_id`),
  KEY `idx_ood_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='出库单明细表';
