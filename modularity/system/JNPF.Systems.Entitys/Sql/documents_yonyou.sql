-- =============================================================================
-- 模块名称：系统对接 - 用友U8数据同步日志
-- 数据库类型：MySQL 5.7+ / 8.0（utf8mb4）
-- 说明：
--   1. MES/WMS 所有业务单据（采购入库单、产成品入库单、材料出库单、销售出库单、
--      生产领料单等）审核后自动推送用友U8生成对应凭证（MES→用友方向）；
--      存货档案/生产订单/仓库档案等基础数据从用友同步至MES（用友→MES方向）；
--   2. 支持失败重试与日志追踪（失败原因/重试次数），出入库审核后自动推送
--      现有量变更（本实现为模拟对接：记录请求/响应内容，不直连U8 API）；
--   3. 所有表继承 JNPF 框架 CLDSEntityBase 公共字段（创建/修改/删除/启用/租户）。
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 表 1：sx_yonyou_sync_log 用友同步日志表
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_yonyou_sync_log`;
CREATE TABLE `sx_yonyou_sync_log` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `biz_type`               VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '单据类型:采购入库单/产成品入库单/材料出库单/销售出库单/生产领料单/生产订单/存货档案/仓库档案/现有量推送',
  `biz_no`                 VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '单据号(如RK20260910001/LL20260910001)',
  `direction`              INT           NULL     DEFAULT 1      COMMENT '同步方向:1 MES→用友 2 用友→MES',
  `voucher_no`             VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '用友凭证号(如PZ-00123,成功后回填)',
  `status`                 INT           NULL     DEFAULT 1      COMMENT '状态:1成功 2失败',
  `fail_reason`            VARCHAR(500)  NULL     DEFAULT NULL   COMMENT '失败原因(如:物料编码不存在)',
  `sync_time`              DATETIME      NULL     DEFAULT NULL   COMMENT '同步时间',
  `cost_ms`                INT           NULL     DEFAULT 0      COMMENT '耗时(毫秒)',
  `trigger_type`           INT           NULL     DEFAULT 1      COMMENT '触发方式:1自动(单据审核后) 2手动触发',
  `retry_count`            INT           NULL     DEFAULT 0      COMMENT '重试次数',
  `request_content`        TEXT          NULL     DEFAULT NULL   COMMENT '请求内容(模拟对接,JSON)',
  `response_content`       TEXT          NULL     DEFAULT NULL   COMMENT '响应内容(模拟对接,JSON)',
  `operator_name`          VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '操作人',
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
  KEY `idx_ysl_biz_no` (`biz_no`),
  KEY `idx_ysl_status` (`status`),
  KEY `idx_ysl_time` (`sync_time`),
  KEY `idx_ysl_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='用友U8同步日志表(系统对接)';
