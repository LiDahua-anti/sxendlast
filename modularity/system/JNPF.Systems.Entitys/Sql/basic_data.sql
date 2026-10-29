-- =============================================================================
-- 模块名称：基础数据（供应商管理 / 终端设备管理 / 模具管理 / 汇率管理）
-- 数据库类型：MySQL 5.7+ / 8.0（utf8mb4）
-- 说明：
--   1. 供应商基础档案（编码/简称/联系人等），供到货单、来料检验引用；
--   2. 终端设备（PDA 手持终端）档案，含工位/IP/负责人/状态（启用/锁定/禁用）；
--   3. 模具档案（模具寿命/已用次数/状态/上次保养），供生产领用与寿命跟踪；
--   4. 汇率管理（币种对人民币汇率，按日期维护）。
--   5. 所有表继承 JNPF 框架 CLDSEntityBase 公共字段（创建/修改/删除/启用/租户）。
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 表 1：sx_supplier 供应商档案表
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_supplier`;
CREATE TABLE `sx_supplier` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `supplier_code`          VARCHAR(50)   NOT NULL                COMMENT '供应商编码(业务唯一)',
  `supplier_no`            VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '供应商代码(用友U8同步的外码)',
  `supplier_name`          VARCHAR(200)  NOT NULL                COMMENT '供应商名称',
  `short_name`             VARCHAR(100)  NULL     DEFAULT NULL   COMMENT '供应商简称',
  `contact_person`         VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '联系人',
  `contact_phone`          VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '联系电话',
  `address`                VARCHAR(300)  NULL     DEFAULT NULL   COMMENT '地址',
  `tax_number`             VARCHAR(80)   NULL     DEFAULT NULL   COMMENT '税号',
  `bank_name`              VARCHAR(200)  NULL     DEFAULT NULL   COMMENT '开户行',
  `bank_account`           VARCHAR(80)   NULL     DEFAULT NULL   COMMENT '银行账号',
  `status`                 INT           NULL     DEFAULT 1      COMMENT '状态:0禁用 1启用',
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
  KEY `idx_sup_code` (`supplier_code`),
  KEY `idx_sup_name` (`supplier_name`),
  KEY `idx_sup_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='供应商档案表(基础数据)';

-- -----------------------------------------------------------------------------
-- 表 2：sx_terminal_device 终端设备表（PDA手持终端）
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_terminal_device`;
CREATE TABLE `sx_terminal_device` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `terminal_code`          VARCHAR(50)   NOT NULL                COMMENT '终端设备编码(业务唯一)',
  `station`                VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '工位(如01/02)',
  `terminal_name`          VARCHAR(100)  NULL     DEFAULT NULL   COMMENT '终端设备名称(如手持PDA)',
  `terminal_ip`            VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '终端设备IP',
  `charge_person`          VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '负责人',
  `status`                 INT           NULL     DEFAULT 1      COMMENT '状态:1启用 2锁定 3禁用',
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
  KEY `idx_td_code` (`terminal_code`),
  KEY `idx_td_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='终端设备表(PDA手持终端)';

-- -----------------------------------------------------------------------------
-- 表 3：sx_mold 模具档案表
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_mold`;
CREATE TABLE `sx_mold` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `mold_code`              VARCHAR(50)   NOT NULL                COMMENT '模具编号(业务唯一)',
  `mold_name`              VARCHAR(200)  NULL     DEFAULT NULL   COMMENT '模具名称',
  `product_name`           VARCHAR(200)  NULL     DEFAULT NULL   COMMENT '对应产品(图番/产品名称)',
  `mold_life`              INT           NULL     DEFAULT 0      COMMENT '模具寿命(模)',
  `used_count`             INT           NULL     DEFAULT 0      COMMENT '已用次数(模)',
  `status`                 INT           NULL     DEFAULT 1      COMMENT '状态:1在用 2维修 3报废',
  `last_maintain_date`     DATE          NULL     DEFAULT NULL   COMMENT '上次保养日期',
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
  KEY `idx_mold_code` (`mold_code`),
  KEY `idx_mold_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='模具档案表(基础数据)';

-- -----------------------------------------------------------------------------
-- 表 4：sx_exchange_rate 汇率表
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_exchange_rate`;
CREATE TABLE `sx_exchange_rate` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `currency`               VARCHAR(50)   NOT NULL                COMMENT '币种(人民币/美元/欧元/日元/韩元)',
  `rate_to_cny`            DECIMAL(18,4) NULL     DEFAULT 1      COMMENT '对人民币汇率(4位小数)',
  `rate_date`              DATE          NULL     DEFAULT NULL   COMMENT '汇率日期',
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
  KEY `idx_rate_currency` (`currency`),
  KEY `idx_rate_date` (`rate_date`),
  KEY `idx_rate_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='汇率表(基础数据)';
