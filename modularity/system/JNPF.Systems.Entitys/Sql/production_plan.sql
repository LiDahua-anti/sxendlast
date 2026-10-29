-- =============================================================================
-- 模块名称：生产计划管理（生产订单 / 排产 / 生产进度查询）
-- 数据库类型：MySQL 5.7+ / 8.0（utf8mb4）
-- 说明：
--   1. 生产订单来自用友U8同步或手工新建，状态：待排产/已排产/已发布/生产中/待入库/已完工/逾期；
--   2. 排产数据来源：工艺线路管理 → 工序明细 → 工序可用设备；
--      每道工序可分配多台设备并填写「生产个数」，工序合计与订单数量对比；
--      保存排产时强制校验「流程卡必选」（未选择流程卡不能保存）；
--   3. 发布校验：① 排产方案已保存并绑定流程卡；
--      ② 该流程卡对应来料检验（或供应商预加工首道工序）存在「已完成」记录，
--      未通过则拦截提示「该流程卡来料检验/前道工序未完成，请先完成后再发布」；
--   4. 订单删除影响范围（审计日志记录）：清理排产方案、流程卡解绑保留、
--      领料/报工记录标记作废保留历史、不自动回冲库存（需通过其他出入库调整）。
--   5. 所有表继承 JNPF 框架 CLDSEntityBase 公共字段（创建/修改/删除/启用/租户）。
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 表 1：sx_production_order 生产订单表
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_production_order`;
CREATE TABLE `sx_production_order` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `order_no`               VARCHAR(50)   NOT NULL                COMMENT '订单号(业务唯一,如MO202609001)',
  `drawing_no`             VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '图番(图纸编号,如XC01)',
  `product_name`           VARCHAR(200)  NULL     DEFAULT NULL   COMMENT '产品名称',
  `plan_qty`               DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '计划数量',
  `unit`                   VARCHAR(20)   NULL     DEFAULT NULL   COMMENT '单位(套/件)',
  `current_process`        VARCHAR(100)  NULL     DEFAULT NULL   COMMENT '当前工序(冗余,最新进度)',
  `team_name`              VARCHAR(100)  NULL     DEFAULT NULL   COMMENT '负责班组',
  `plan_finish_date`       DATE          NULL     DEFAULT NULL   COMMENT '计划完工日期',
  `status`                 INT           NULL     DEFAULT 1      COMMENT '订单状态:1待排产 2已排产 3已发布 4生产中 5待入库 6已完工 7逾期',
  `publish_status`         INT           NULL     DEFAULT 0      COMMENT '发布状态:0未发布 1已发布',
  `source`                 INT           NULL     DEFAULT 1      COMMENT '来源:1用友U8同步 2手工新增',
  `customer`               VARCHAR(200)  NULL     DEFAULT NULL   COMMENT '客户',
  `sync_time`              DATETIME      NULL     DEFAULT NULL   COMMENT '最近同步时间',
  `u8_order_id`            VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '用友U8订单内码(同步映射用)',
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
  UNIQUE KEY `uk_po_order_no` (`order_no`),
  KEY `idx_po_status` (`status`),
  KEY `idx_po_drawing` (`drawing_no`),
  KEY `idx_po_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='生产订单表(生产计划管理)';

-- -----------------------------------------------------------------------------
-- 表 2：sx_production_schedule 排产主表（订单 × 流程卡）
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_production_schedule`;
CREATE TABLE `sx_production_schedule` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `order_id`               VARCHAR(50)   NOT NULL                COMMENT '生产订单ID(sx_production_order.F_ID)',
  `order_no`               VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '订单号(冗余)',
  `flow_card_id`           VARCHAR(50)   NOT NULL                COMMENT '流程卡ID(sx_flow_card.F_ID,必填)',
  `flow_card_no`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '流程卡号(冗余)',
  `plan_qty`               DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '投产数量',
  `status`                 INT           NULL     DEFAULT 1      COMMENT '状态:1已保存 2已发布',
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
  KEY `idx_ps_order` (`order_id`),
  KEY `idx_ps_card` (`flow_card_id`),
  KEY `idx_ps_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='排产主表(订单绑定流程卡,保存时强制校验)';

-- -----------------------------------------------------------------------------
-- 表 3：sx_production_schedule_detail 排产明细表（工序 × 设备 × 生产个数）
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_production_schedule_detail`;
CREATE TABLE `sx_production_schedule_detail` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `schedule_id`            VARCHAR(50)   NOT NULL                COMMENT '排产主表ID(sx_production_schedule.F_ID)',
  `order_id`               VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '订单ID(冗余,便于查询)',
  `process_id`             VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '工序ID(sx_base_process.F_ID)',
  `process_name`           VARCHAR(100)  NULL     DEFAULT NULL   COMMENT '工序名称(冗余)',
  `seq_no`                 INT           NULL     DEFAULT 0      COMMENT '工序顺序号',
  `device_id`              VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '设备ID(sx_device_master.F_ID)',
  `device_code`            VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '设备编码(冗余)',
  `device_name`            VARCHAR(200)  NULL     DEFAULT NULL   COMMENT '设备名称(冗余)',
  `produce_qty`            DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '生产个数(该设备分配数量)',
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
  KEY `idx_psd_schedule` (`schedule_id`),
  KEY `idx_psd_order` (`order_id`),
  KEY `idx_psd_process` (`process_id`),
  KEY `idx_psd_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='排产明细表(工序设备生产个数分配)';

-- -----------------------------------------------------------------------------
-- 表 4：sx_production_schedule_log 订单操作审计日志表
--   （排产保存/发布/删除清理的操作快照，删除影响范围可追溯）
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_production_schedule_log`;
CREATE TABLE `sx_production_schedule_log` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `order_id`               VARCHAR(50)   NOT NULL                COMMENT '订单ID',
  `order_no`               VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '订单号(冗余)',
  `operate_type`           INT           NULL     DEFAULT 1      COMMENT '操作类型:1排产保存 2发布 3删除清理',
  `content`                TEXT          NULL     DEFAULT NULL   COMMENT '操作快照内容(JSON)',
  `F_SORT_CODE`            BIGINT        NULL     DEFAULT 0      COMMENT '排序码',
  `F_ENABLED_MARK`         INT           NULL     DEFAULT 1      COMMENT '启用标识:0禁用 1启用',
  `F_CREATOR_TIME`         DATETIME      NULL     DEFAULT NULL   COMMENT '创建时间(操作时间)',
  `F_CREATOR_USER_ID`      VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '创建用户(操作人)',
  `F_LAST_MODIFY_TIME`     DATETIME      NULL     DEFAULT NULL   COMMENT '修改时间',
  `F_LAST_MODIFY_USER_ID`  VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '修改用户',
  `F_DELETE_MARK`          INT           NULL     DEFAULT NULL   COMMENT '删除标志:1已删除',
  `F_DELETE_TIME`          DATETIME      NULL     DEFAULT NULL   COMMENT '删除时间',
  `F_DELETE_USER_ID`       VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '删除用户',
  PRIMARY KEY (`F_ID`),
  KEY `idx_psl_order` (`order_id`),
  KEY `idx_psl_type` (`operate_type`),
  KEY `idx_psl_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='订单操作审计日志表(生产计划管理)';
