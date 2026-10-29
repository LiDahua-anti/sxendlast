-- =============================================================================
-- 模块名称：技术管理 - 存货管理（MES-WMS）
-- 数据库类型：MySQL 5.7+ / 8.0（utf8mb4）
-- 说明：
--   1. 存货档案基础数据从用友 U8 同步（存货编码/名称/规格/大类/计量单位等）；
--   2. 本模块维护「存货与子件的 BOM 绑定关系」（子件数量、损耗率）；
--   3. 维护「存货在各工序的检查项目配置」（PDA 报工时展示并录入结果）。
--   4. 所有表继承 JNPF 框架 CLDSEntityBase 公共字段（创建/修改/删除/启用/租户）。
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 表 1：sx_inventory_master 存货档案主表
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_inventory_master`;
CREATE TABLE `sx_inventory_master` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `inventory_code`         VARCHAR(50)   NOT NULL                COMMENT '存货编码(用友U8同步,业务唯一)',
  `inventory_name`         VARCHAR(200)  NULL     DEFAULT NULL   COMMENT '存货名称',
  `spec`                   VARCHAR(200)  NULL     DEFAULT NULL   COMMENT '规格型号',
  `category`               VARCHAR(100)  NULL     DEFAULT NULL   COMMENT '存货大类(产成品/主生产材料等)',
  `unit`                   VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '主计量单位(PCS/KG等)',
  `make_type`              INT           NULL     DEFAULT 1      COMMENT '生产方式:1自制 2委外 3采购',
  `pricing_method`         VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '计价方式(如:全月平均法)',
  `batch_manage`           INT           NULL     DEFAULT 0      COMMENT '是否批次管理:0否 1是',
  `source`                 INT           NULL     DEFAULT 1      COMMENT '数据来源:1用友U8同步 2手工新增',
  `sync_time`              DATETIME      NULL     DEFAULT NULL   COMMENT '最近一次从用友U8同步的时间',
  `u8_inventory_id`        VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '用友U8存货内码(同步映射用)',
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
  KEY `idx_inv_code` (`inventory_code`),
  KEY `idx_inv_category` (`category`),
  KEY `idx_inv_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='存货档案主表(技术管理-存货管理)';

-- -----------------------------------------------------------------------------
-- 表 2：sx_inventory_bom BOM子件明细表（母件存货 × 子件存货 绑定关系）
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_inventory_bom`;
CREATE TABLE `sx_inventory_bom` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `parent_inventory_id`    VARCHAR(50)   NOT NULL                COMMENT '母件存货ID(sx_inventory_master.F_ID)',
  `parent_inventory_code`  VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '母件存货编码(冗余,便于查询)',
  `child_inventory_id`     VARCHAR(50)   NOT NULL                COMMENT '子件存货ID(sx_inventory_master.F_ID)',
  `child_inventory_code`   VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '子件存货编码(冗余,便于查询)',
  `quantity`               DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '使用数量(单件母件消耗子件数)',
  `loss_rate`              DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '损耗率(%)',
  `position_no`            VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '位置号/行号',
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
  KEY `idx_bom_parent` (`parent_inventory_id`),
  KEY `idx_bom_child` (`child_inventory_id`),
  KEY `idx_bom_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='存货BOM子件明细表';

-- -----------------------------------------------------------------------------
-- 表 3：sx_inventory_check_item 存货工序检查项目配置表
--   （存货 × 工序 的检查项目绑定，PDA 报工时展示并录入结果）
--   工序关联 sx_base_process（工序管理主数据）
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_inventory_check_item`;
CREATE TABLE `sx_inventory_check_item` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `inventory_id`           VARCHAR(50)   NOT NULL                COMMENT '存货ID(sx_inventory_master.F_ID)',
  `process_id`             VARCHAR(50)   NOT NULL                COMMENT '工序ID(sx_base_process.F_ID)',
  `process_code`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '工序编码(冗余,如GX01)',
  `process_name`           VARCHAR(100)  NULL     DEFAULT NULL   COMMENT '工序名称(冗余,如CNC粗加工)',
  `check_item`             VARCHAR(200)  NOT NULL                COMMENT '检查项目名称(如:基准/厚度/外观)',
  `check_standard`         VARCHAR(500)  NULL     DEFAULT NULL   COMMENT '检查标准(如:3.0±0.05mm)',
  `data_type`              INT           NULL     DEFAULT 1      COMMENT '数据类型:1数值 2文本 3下拉',
  `option_items`           VARCHAR(500)  NULL     DEFAULT NULL   COMMENT '下拉选项(数据类型为下拉时有效,如:OK,NG)',
  `default_value`          VARCHAR(200)  NULL     DEFAULT NULL   COMMENT '默认值',
  `required`               INT           NULL     DEFAULT 1      COMMENT '是否必填:0否 1是',
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
  KEY `idx_ci_inventory` (`inventory_id`),
  KEY `idx_ci_process` (`process_id`),
  KEY `idx_ci_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='存货工序检查项目配置表';
