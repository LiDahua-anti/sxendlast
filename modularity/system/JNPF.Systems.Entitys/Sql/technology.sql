-- =============================================================================
-- 模块名称：技术管理（工艺线路管理 / 工序管理[检查内容/设备绑定/作业指导] / 齐套性检查）
-- 数据库类型：MySQL 5.7+ / 8.0（utf8mb4）
-- 说明：
--   1. 工艺路线 = 存货（母件）× 工序 的绑定关系；创建时定义路线编号/名称/版本，
--      选择适用存货，按顺序添加工序（工序编码/名称/标准工时/检查项目自动带出）；
--   2. 工序主表复用既有 sx_base_process（工序编码/名称/类型/标准周期/作业场所），
--      本脚本新建工序的检查内容定义、设备绑定、作业指导（SOP）三张子表；
--      检查内容：项目/标准/数据类型(数值/文本/下拉)/下拉选项/默认值/必填；
--   3. 齐套性检查基于存货管理 BOM（sx_inventory_bom）与实时库存（sx_inventory_stock）计算：
--      需用数量 = 单个用量 × 投产数量 × (1 + 损耗率/100)；可领量 = 在库 - 占用；
--   4. 所有表继承 JNPF 框架 CLDSEntityBase 公共字段（创建/修改/删除/启用/租户）。
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 表 1：sx_routing 工艺路线主表
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_routing`;
CREATE TABLE `sx_routing` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `routing_code`           VARCHAR(50)   NOT NULL                COMMENT '路线编号(如GY-EH01-09B-V1)',
  `routing_name`           VARCHAR(200)  NOT NULL                COMMENT '路线名称',
  `inventory_id`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '适用存货(母件)ID(sx_inventory_master.F_ID)',
  `inventory_code`         VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '母件编码(冗余)',
  `inventory_name`         VARCHAR(200)  NULL     DEFAULT NULL   COMMENT '母件名称(冗余)',
  `process_count`          INT           NULL     DEFAULT 0      COMMENT '工序数',
  `total_hours`            DECIMAL(10,2) NULL     DEFAULT 0      COMMENT '总标准工时(min)',
  `version`                VARCHAR(20)   NULL     DEFAULT 'V1.0' COMMENT '版本',
  `status`                 INT           NULL     DEFAULT 3      COMMENT '状态:1生效 2停用 3草稿',
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
  KEY `idx_rt_code` (`routing_code`),
  KEY `idx_rt_inventory` (`inventory_id`),
  KEY `idx_rt_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='工艺路线主表(技术管理)';

-- -----------------------------------------------------------------------------
-- 表 2：sx_routing_process 工艺路线工序明细表
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_routing_process`;
CREATE TABLE `sx_routing_process` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `routing_id`             VARCHAR(50)   NOT NULL                COMMENT '工艺路线ID(sx_routing.F_ID)',
  `process_id`             VARCHAR(50)   NOT NULL                COMMENT '工序ID(sx_base_process.F_ID)',
  `process_code`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '工序编码(冗余,如GX01)',
  `process_name`           VARCHAR(100)  NULL     DEFAULT NULL   COMMENT '工序名称(冗余)',
  `seq_no`                 INT           NULL     DEFAULT 0      COMMENT '顺序号',
  `standard_hours`         DECIMAL(10,2) NULL     DEFAULT 0      COMMENT '标准工时(min)',
  `requirement`            VARCHAR(500)  NULL     DEFAULT NULL   COMMENT '工序要求',
  `is_check_process`       INT           NULL     DEFAULT 0      COMMENT '是否检查工序(最后一步检查):0否 1是',
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
  KEY `idx_rtp_routing` (`routing_id`),
  KEY `idx_rtp_process` (`process_id`),
  KEY `idx_rtp_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='工艺路线工序明细表';

-- -----------------------------------------------------------------------------
-- 表 3：sx_process_check_item 工序检查内容定义表
--   （PDA报工时按此展示检查项并录入结果）
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_process_check_item`;
CREATE TABLE `sx_process_check_item` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `process_id`             VARCHAR(50)   NOT NULL                COMMENT '工序ID(sx_base_process.F_ID)',
  `process_code`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '工序编码(冗余,如GX01)',
  `process_name`           VARCHAR(100)  NULL     DEFAULT NULL   COMMENT '工序名称(冗余)',
  `check_item`             VARCHAR(200)  NOT NULL                COMMENT '检查项目(如:基准/厚度/螺纹孔通止端)',
  `check_standard`         VARCHAR(500)  NULL     DEFAULT NULL   COMMENT '检查标准(如:3.0±0.05mm)',
  `data_type`              INT           NULL     DEFAULT 1      COMMENT '数据类型:1数值 2文本 3下拉',
  `option_items`           VARCHAR(500)  NULL     DEFAULT NULL   COMMENT '下拉选项(数据类型为下拉时有效,如:OK,NG)',
  `default_value`          VARCHAR(200)  NULL     DEFAULT NULL   COMMENT '默认值',
  `required`               INT           NULL     DEFAULT 1      COMMENT '是否必填:0否 1是',
  `seq_no`                 INT           NULL     DEFAULT 0      COMMENT '顺序号',
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
  KEY `idx_pci_process` (`process_id`),
  KEY `idx_pci_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='工序检查内容定义表(技术管理-工序管理)';

-- -----------------------------------------------------------------------------
-- 表 4：sx_process_device 工序设备绑定表
--   （绑定该工序可用的生产设备，排产时按工序明细自动带出可选设备）
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_process_device`;
CREATE TABLE `sx_process_device` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `process_id`             VARCHAR(50)   NOT NULL                COMMENT '工序ID(sx_base_process.F_ID)',
  `process_code`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '工序编码(冗余)',
  `process_name`           VARCHAR(100)  NULL     DEFAULT NULL   COMMENT '工序名称(冗余)',
  `device_id`              VARCHAR(50)   NOT NULL                COMMENT '设备ID(sx_device_master.F_ID)',
  `device_code`            VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '设备编码(冗余)',
  `device_name`            VARCHAR(200)  NULL     DEFAULT NULL   COMMENT '设备名称(冗余)',
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
  KEY `idx_pd_process` (`process_id`),
  KEY `idx_pd_device` (`device_id`),
  KEY `idx_pd_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='工序设备绑定表(排产自动带出可用设备)';

-- -----------------------------------------------------------------------------
-- 表 5：sx_process_sop 工序作业指导（SOP）文件表
--   （存放本工序的作业指导标准和作业图片，PDA报工时可查看）
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_process_sop`;
CREATE TABLE `sx_process_sop` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `process_id`             VARCHAR(50)   NOT NULL                COMMENT '工序ID(sx_base_process.F_ID)',
  `process_code`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '工序编码(冗余)',
  `process_name`           VARCHAR(100)  NULL     DEFAULT NULL   COMMENT '工序名称(冗余)',
  `file_name`              VARCHAR(200)  NOT NULL                COMMENT '文件名(如:CNC粗加工-作业指导书V3.pdf)',
  `file_type`              VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '文件类型(PDF/图片)',
  `file_size`              VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '文件大小(展示用,如2.4MB)',
  `file_path`              VARCHAR(500)  NULL     DEFAULT NULL   COMMENT '文件存储路径',
  `version`                VARCHAR(20)   NULL     DEFAULT 'V1'   COMMENT '版本',
  `remark`                 VARCHAR(500)  NULL     DEFAULT NULL   COMMENT '备注',
  `F_SORT_CODE`            BIGINT        NULL     DEFAULT 0      COMMENT '排序码',
  `F_ENABLED_MARK`         INT           NULL     DEFAULT 1      COMMENT '启用标识:0禁用 1启用',
  `F_CREATOR_TIME`         DATETIME      NULL     DEFAULT NULL   COMMENT '创建时间(上传时间)',
  `F_CREATOR_USER_ID`      VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '创建用户(上传人)',
  `F_LAST_MODIFY_TIME`     DATETIME      NULL     DEFAULT NULL   COMMENT '修改时间',
  `F_LAST_MODIFY_USER_ID`  VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '修改用户',
  `F_DELETE_MARK`          INT           NULL     DEFAULT NULL   COMMENT '删除标志:1已删除',
  `F_DELETE_TIME`          DATETIME      NULL     DEFAULT NULL   COMMENT '删除时间',
  `F_DELETE_USER_ID`       VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '删除用户',
  PRIMARY KEY (`F_ID`),
  KEY `idx_sop_process` (`process_id`),
  KEY `idx_sop_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='工序作业指导(SOP)文件表';
