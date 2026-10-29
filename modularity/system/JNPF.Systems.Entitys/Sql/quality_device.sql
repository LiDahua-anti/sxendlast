-- =============================================================================
-- 模块名称：品质管理（来料检验记录 IQC / 质量检验 OQC）+ 设备管理（设备报警履历）
-- 数据库类型：MySQL 5.7+ / 8.0（utf8mb4）
-- 说明：
--   1. IQC 来料检验：PDA「采购入库」扫码时的来料检查记录（按到货单/批次关联），
--      每个检查项目记录实测值与判定结果（OK/NG）；免检规则（供应商/存货）自动跳过检查项，
--      检验方式标记「免检」；不合格批次自动触发不良处理流程；
--   2. OQC 质量检验：PDA「品质抽检」扫码（员工码+流程卡+工序选项卡）产生的抽检记录；
--      口径区分：抽检（按批抽样判定）与报工记录中末道工序全检（检包检验记录）两套并行，
--      数据来源不同；检查合格后订单状态变为「待入库」；
--   3. 设备报警履历：设备报警编号/类型/内容/触发时间/恢复时间/处理人记录。
--   4. 所有表继承 JNPF 框架 CLDSEntityBase 公共字段（创建/修改/删除/启用/租户）。
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 表 1：sx_iqc 来料检验（IQC）主表
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_iqc`;
CREATE TABLE `sx_iqc` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `check_no`               VARCHAR(50)   NOT NULL                COMMENT '检验单号(业务唯一,如IQC20261010001)',
  `arrival_id`             VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '到货单主表ID(sx_sx_purchase_arrival_main.F_ID)',
  `arrival_no`             VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '到货单号(冗余)',
  `arrival_detail_id`      VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '到货单明细ID',
  `supplier_id`            VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '供应商ID(sx_supplier.F_ID)',
  `supplier_name`          VARCHAR(200)  NULL     DEFAULT NULL   COMMENT '供应商名称(冗余)',
  `batch_no`               VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '批次号',
  `inventory_id`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '存货ID(sx_inventory_master.F_ID)',
  `inventory_code`         VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '存货编码(冗余)',
  `inventory_name`         VARCHAR(200)  NULL     DEFAULT NULL   COMMENT '存货名称(冗余)',
  `check_type`             INT           NULL     DEFAULT 1      COMMENT '检验方式:1扫码检查 2免检',
  `check_items_summary`    VARCHAR(1000) NULL     DEFAULT NULL   COMMENT '检查项目摘要(如:外观 OK / 高度 OK)',
  `check_qty`              DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '检验数量',
  `bad_qty`                DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '不良数',
  `check_result`           INT           NULL     DEFAULT 1      COMMENT '检验结果:1合格 2不合格 3免检',
  `checker_name`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '检验员',
  `check_time`             DATETIME      NULL     DEFAULT NULL   COMMENT '检验时间',
  `bad_handle_status`      INT           NULL     DEFAULT 0      COMMENT '不良处理状态:0无需处理 1待处理 2处理中 3已处理',
  `bad_handle_remark`      VARCHAR(500)  NULL     DEFAULT NULL   COMMENT '不良处理说明',
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
  UNIQUE KEY `uk_iqc_check_no` (`check_no`),
  KEY `idx_iqc_arrival` (`arrival_no`),
  KEY `idx_iqc_batch` (`batch_no`),
  KEY `idx_iqc_result` (`check_result`),
  KEY `idx_iqc_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='来料检验(IQC)主表(品质管理)';

-- -----------------------------------------------------------------------------
-- 表 2：sx_iqc_detail 来料检验明细表（检查项目实测值与判定）
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_iqc_detail`;
CREATE TABLE `sx_iqc_detail` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `iqc_id`                 VARCHAR(50)   NOT NULL                COMMENT 'IQC主表ID(sx_iqc.F_ID)',
  `check_item`             VARCHAR(200)  NULL     DEFAULT NULL   COMMENT '检查项目(如:外观/高度)',
  `check_standard`         VARCHAR(500)  NULL     DEFAULT NULL   COMMENT '检查标准',
  `actual_value`           VARCHAR(500)  NULL     DEFAULT NULL   COMMENT '实测值',
  `check_result`           VARCHAR(20)   NULL     DEFAULT NULL   COMMENT '判定结果:OK/NG',
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
  KEY `idx_iqcd_iqc` (`iqc_id`),
  KEY `idx_iqcd_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='来料检验(IQC)明细表';

-- -----------------------------------------------------------------------------
-- 表 3：sx_oqc 质量检验（OQC抽检）主表
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_oqc`;
CREATE TABLE `sx_oqc` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `check_no`               VARCHAR(50)   NOT NULL                COMMENT '抽检单号(业务唯一,如OQC20261010001)',
  `flow_card_id`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '流程卡ID(sx_flow_card.F_ID)',
  `flow_card_no`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '流程卡号(冗余)',
  `order_id`               VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '订单ID(sx_production_order.F_ID)',
  `order_no`               VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '订单号(冗余)',
  `process_id`             VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '工序ID(sx_base_process.F_ID)',
  `process_name`           VARCHAR(100)  NULL     DEFAULT NULL   COMMENT '工序(CNC/清洗/检包/入库)',
  `check_type`             INT           NULL     DEFAULT 2      COMMENT '检验类型:1全检 2抽检(本表默认抽检,按批抽样)',
  `check_items_summary`    VARCHAR(1000) NULL     DEFAULT NULL   COMMENT '检查项目摘要(如:基准 OK / 厚度 OK)',
  `sample_qty`             DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '抽检数(按批抽样)',
  `bad_qty`                DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '不良数',
  `check_result`           INT           NULL     DEFAULT 1      COMMENT '判定:1合格 2不合格',
  `checker_name`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '质检员',
  `check_time`             DATETIME      NULL     DEFAULT NULL   COMMENT '抽检时间',
  `report_id`              VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '关联报工记录ID(sx_work_report.F_ID)',
  `bad_handle_status`      INT           NULL     DEFAULT 0      COMMENT '不良处理状态:0无需处理 1待处理 2处理中 3已处理',
  `bad_handle_remark`      VARCHAR(500)  NULL     DEFAULT NULL   COMMENT '不良处理说明',
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
  UNIQUE KEY `uk_oqc_check_no` (`check_no`),
  KEY `idx_oqc_card` (`flow_card_no`),
  KEY `idx_oqc_order` (`order_no`),
  KEY `idx_oqc_result` (`check_result`),
  KEY `idx_oqc_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='质量检验(OQC抽检)主表(品质管理)';

-- -----------------------------------------------------------------------------
-- 表 4：sx_oqc_detail 质量检验明细表（检查项目实测值与判定）
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_oqc_detail`;
CREATE TABLE `sx_oqc_detail` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `oqc_id`                 VARCHAR(50)   NOT NULL                COMMENT 'OQC主表ID(sx_oqc.F_ID)',
  `check_item`             VARCHAR(200)  NULL     DEFAULT NULL   COMMENT '检查项目',
  `check_standard`         VARCHAR(500)  NULL     DEFAULT NULL   COMMENT '检查标准',
  `actual_value`           VARCHAR(500)  NULL     DEFAULT NULL   COMMENT '实测值',
  `check_result`           VARCHAR(20)   NULL     DEFAULT NULL   COMMENT '判定结果:OK/NG',
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
  KEY `idx_oqcd_oqc` (`oqc_id`),
  KEY `idx_oqcd_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='质量检验(OQC抽检)明细表';

-- -----------------------------------------------------------------------------
-- 表 5：sx_device_alarm 设备报警履历表
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_device_alarm`;
CREATE TABLE `sx_device_alarm` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `alarm_no`               VARCHAR(50)   NOT NULL                COMMENT '报警编号(如ALM20261010-001)',
  `device_id`              VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '设备ID(sx_device_master.F_ID)',
  `device_code`            VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '设备编号(如CNC-01)',
  `device_name`            VARCHAR(200)  NULL     DEFAULT NULL   COMMENT '设备名称(如数控车床1号)',
  `alarm_type`             VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '报警类型:温度异常/压力异常/通讯中断/急停触发',
  `alarm_content`          VARCHAR(500)  NULL     DEFAULT NULL   COMMENT '报警内容(如:主轴温度超过85℃)',
  `trigger_time`           DATETIME      NULL     DEFAULT NULL   COMMENT '触发时间',
  `recover_time`           DATETIME      NULL     DEFAULT NULL   COMMENT '恢复时间',
  `handler_name`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '处理人(如:维修-陈工)',
  `handle_status`          INT           NULL     DEFAULT 0      COMMENT '处理状态:0未恢复 1已恢复 2已处理',
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
  KEY `idx_da_no` (`alarm_no`),
  KEY `idx_da_device` (`device_code`),
  KEY `idx_da_type` (`alarm_type`),
  KEY `idx_da_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='设备报警履历表(设备管理)';
