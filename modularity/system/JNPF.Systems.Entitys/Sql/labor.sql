-- =============================================================================
-- 模块名称：人工管理（班组管理 / 员工技能管理 / 人效考核）
-- 数据库类型：MySQL 5.7+ / 8.0（utf8mb4）
-- 说明：
--   1. 班组管理复用既有班次表 sx_base_class，本脚本扩展「是否跨日」字段；
--      夜班（如 18:00 ~ 次日 2:50）跨日标记为是，用于报工工时归属计算；
--   2. 员工技能档案（技能等级/认证/证书/关联工序）；
--   3. 人效考核按月汇总（合格产量/不良率/工时利用率/绩效得分），
--      数据由报工记录统计生成（合格产量=Σ报工良品；不良率=Σ不良/Σ报工；
--      工时利用率=有效工时/出勤工时；绩效得分按等级 A/B/C 归档）。
--   4. 所有表继承 JNPF 框架 CLDSEntityBase 公共字段（创建/修改/删除/启用/租户）。
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 扩展 1：sx_base_class 增加「是否跨日」字段
-- 注意：ALTER 语句仅需执行一次；若已执行请忽略报错（Duplicate column name）。
-- -----------------------------------------------------------------------------
ALTER TABLE `sx_base_class` ADD COLUMN `cross_day` INT NULL DEFAULT 0 COMMENT '是否跨日:0否 1是(夜班跨越零点的班次归属次日)' AFTER `end_time`;

-- -----------------------------------------------------------------------------
-- 表 1：sx_employee_skill 员工技能表
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_employee_skill`;
CREATE TABLE `sx_employee_skill` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `employee_no`            VARCHAR(50)   NOT NULL                COMMENT '工号(关联平台用户/员工档案)',
  `employee_name`          VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '姓名(冗余)',
  `skill_name`             VARCHAR(200)  NOT NULL                COMMENT '技能名称(如:机器人焊接操作)',
  `skill_level`            VARCHAR(20)   NULL     DEFAULT NULL   COMMENT '技能等级:L1/L2/L3',
  `cert_type`              INT           NULL     DEFAULT 1      COMMENT '认证方式:1内(内部认证) 2外(外部认证)',
  `cert_no`                VARCHAR(80)   NULL     DEFAULT NULL   COMMENT '证书编号',
  `cert_org`               VARCHAR(200)  NULL     DEFAULT NULL   COMMENT '发证单位',
  `status`                 INT           NULL     DEFAULT 1      COMMENT '状态:1有效 0失效',
  `process_id`             VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '关联工序ID(sx_base_process.F_ID)',
  `process_name`           VARCHAR(100)  NULL     DEFAULT NULL   COMMENT '关联工序名称(冗余)',
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
  KEY `idx_es_employee` (`employee_no`),
  KEY `idx_es_process` (`process_id`),
  KEY `idx_es_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='员工技能表(人工管理)';

-- -----------------------------------------------------------------------------
-- 表 2：sx_efficiency 人效考核月汇总表
--   （由报工记录按月统计生成：合格产量/不良率/工时利用率/绩效得分）
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_efficiency`;
CREATE TABLE `sx_efficiency` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `employee_no`            VARCHAR(50)   NOT NULL                COMMENT '工号',
  `employee_name`          VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '姓名',
  `team_name`              VARCHAR(100)  NULL     DEFAULT NULL   COMMENT '班组(A班/B班等,取班次名称)',
  `month`                  VARCHAR(20)   NOT NULL                COMMENT '月份(如2026-09)',
  `attend_days`            INT           NULL     DEFAULT 0      COMMENT '出勤天数',
  `qualified_qty`          INT           NULL     DEFAULT 0      COMMENT '合格产量(Σ报工良品数,均分后)',
  `bad_rate`               DECIMAL(10,4) NULL     DEFAULT 0      COMMENT '不良率(%)',
  `work_hours_rate`        DECIMAL(10,2) NULL     DEFAULT 0      COMMENT '工时利用率(%)',
  `score_level`            VARCHAR(10)   NULL     DEFAULT NULL   COMMENT '绩效得分等级:A/B/C',
  `score`                  DECIMAL(10,2) NULL     DEFAULT 0      COMMENT '绩效得分(百分制)',
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
  KEY `idx_eff_employee` (`employee_no`),
  KEY `idx_eff_month` (`month`),
  KEY `idx_eff_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='人效考核月汇总表(人工管理)';
