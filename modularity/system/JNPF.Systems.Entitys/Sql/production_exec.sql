-- =============================================================================
-- 模块名称：生产制造管理（流程卡打印 / 报工记录 / 清洗合并码）
-- 数据库类型：MySQL 5.7+ / 8.0（utf8mb4）
-- 说明：
--   1. 流程卡：由到货单流程卡打印生成（原卡），生产过程中支持合并/拆分/重印，
--      新卡与旧卡建立追溯关系（合并=多旧→一新，拆分=一旧→多新，重印=关联原卡）；
--   2. 报工记录三层级：流程卡 → 工序流转（sx_flow_card_process）→ 报工明细（sx_work_report）；
--      检查为工序最后一步（末道检查工序报工需填写检包检验记录 sx_report_check_detail）；
--      同一流程卡同一工序多人报工按报工人数均分（如报工400件→每人计100件）；
--      清洗工序按清洗合并码配置人数均分（默认4人），配置变更后历史报工保留原配置人员；
--   3. 清洗合并码：工序「清洗」多人在同一清洗工位合并为一次报工，配置支持版本化
--      （V1/V2/V3 + 生效/失效日期），历史版本永久保留可存档追溯；
--   4. 流程卡定量勾稽：Σ各工序报工良品 = Σ产成品入库 = Σ销售/其他出库 ≤ 流程卡定量，
--      超出拦截；全检良品计入末道工序报工；
--   5. 结束生产时未完工流程卡保留，次日可继续报工。
--   6. 所有表继承 JNPF 框架 CLDSEntityBase 公共字段（创建/修改/删除/启用/租户）。
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 表 1：sx_flow_card 流程卡表
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_flow_card`;
CREATE TABLE `sx_flow_card` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `card_no`                VARCHAR(50)   NOT NULL                COMMENT '流程卡号(业务唯一,如LCK0001)',
  `card_type`              INT           NULL     DEFAULT 1      COMMENT '类型:1原卡 2合并卡 3拆分卡 4重印卡',
  `order_id`               VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '生产订单ID(sx_production_order.F_ID)',
  `order_no`               VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '订单号(冗余)',
  `arrival_id`             VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '到货单主表ID(sx_sx_purchase_arrival_main.F_ID,可空)',
  `arrival_detail_id`      VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '到货单明细ID(可空)',
  `product_name`           VARCHAR(200)  NULL     DEFAULT NULL   COMMENT '产品(图番/产品名称)',
  `drawing_no`             VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '图番',
  `qty`                    DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '数量(流程卡定量,勾稽上限)',
  `unit`                   VARCHAR(20)   NULL     DEFAULT NULL   COMMENT '单位(套/件)',
  `print_user`             VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '打印人',
  `print_time`             DATETIME      NULL     DEFAULT NULL   COMMENT '打印时间',
  `status`                 INT           NULL     DEFAULT 1      COMMENT '状态:1未使用 2生产中 3已完工 4已入库 5作废',
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
  UNIQUE KEY `uk_fc_card_no` (`card_no`),
  KEY `idx_fc_order` (`order_id`),
  KEY `idx_fc_arrival` (`arrival_id`),
  KEY `idx_fc_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='流程卡表(生产制造管理)';

-- -----------------------------------------------------------------------------
-- 表 2：sx_flow_card_relation 流程卡追溯关系表
--   （新卡与旧卡的关联：合并/拆分/重印，新流程卡可追溯到来源旧卡）
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_flow_card_relation`;
CREATE TABLE `sx_flow_card_relation` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `new_card_id`            VARCHAR(50)   NOT NULL                COMMENT '新流程卡ID(sx_flow_card.F_ID)',
  `new_card_no`            VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '新流程卡号(冗余)',
  `old_card_id`            VARCHAR(50)   NOT NULL                COMMENT '旧流程卡ID(sx_flow_card.F_ID)',
  `old_card_no`            VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '旧流程卡号(冗余)',
  `relation_type`          INT           NULL     DEFAULT 1      COMMENT '关系类型:1合并(多旧→一新) 2拆分(一旧→多新) 3重印(关联原卡)',
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
  KEY `idx_fcr_new` (`new_card_id`),
  KEY `idx_fcr_old` (`old_card_id`),
  KEY `idx_fcr_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='流程卡追溯关系表(合并/拆分/重印)';

-- -----------------------------------------------------------------------------
-- 表 3：sx_flow_card_process 流程卡工序流转表
--   （报工记录第2层级：流程卡下各工序的流转状态与完成数量）
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_flow_card_process`;
CREATE TABLE `sx_flow_card_process` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `flow_card_id`           VARCHAR(50)   NOT NULL                COMMENT '流程卡ID(sx_flow_card.F_ID)',
  `flow_card_no`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '流程卡号(冗余)',
  `process_id`             VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '工序ID(sx_base_process.F_ID)',
  `process_code`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '工序编码(冗余)',
  `process_name`           VARCHAR(100)  NULL     DEFAULT NULL   COMMENT '工序名称(冗余)',
  `seq_no`                 INT           NULL     DEFAULT 0      COMMENT '工序顺序号',
  `is_check_process`       INT           NULL     DEFAULT 0      COMMENT '是否检查工序(最后一步检查):0否 1是',
  `status`                 INT           NULL     DEFAULT 1      COMMENT '状态:1未开始 2生产中 3已完成',
  `target_qty`             DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '目标数量(流程卡定量)',
  `complete_qty`           DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '完成数量(Σ报工良品)',
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
  KEY `idx_fcp_card` (`flow_card_id`),
  KEY `idx_fcp_process` (`process_id`),
  KEY `idx_fcp_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='流程卡工序流转表(报工记录第2层级)';

-- -----------------------------------------------------------------------------
-- 表 4：sx_work_report 报工明细表
--   （报工记录第3层级；多人同组报工按人数均分：组内每人一条记录）
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_work_report`;
CREATE TABLE `sx_work_report` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `card_process_id`        VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '流程卡工序流转ID(sx_flow_card_process.F_ID)',
  `flow_card_id`           VARCHAR(50)   NOT NULL                COMMENT '流程卡ID(sx_flow_card.F_ID)',
  `flow_card_no`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '流程卡号(冗余)',
  `order_id`               VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '订单ID(冗余)',
  `order_no`               VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '订单号(冗余)',
  `process_id`             VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '工序ID(sx_base_process.F_ID)',
  `process_name`           VARCHAR(100)  NULL     DEFAULT NULL   COMMENT '工序名称(冗余)',
  `device_id`              VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '设备ID(sx_device_master.F_ID)',
  `device_code`            VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '设备编码(冗余)',
  `device_name`            VARCHAR(200)  NULL     DEFAULT NULL   COMMENT '设备名称(冗余)',
  `station`                VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '工位',
  `worker_id`              VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '报工人ID(平台用户ID)',
  `worker_no`              VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '报人工号',
  `worker_name`            VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '报工人姓名',
  `is_temp_worker`         INT           NULL     DEFAULT 0      COMMENT '是否临时作业员:0否 1是',
  `group_no`               VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '报工批次号(同一次多人报工共享)',
  `group_qty`              DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '批次报工总数量(均分前)',
  `member_count`           INT           NULL     DEFAULT 1      COMMENT '批次数(均分人数)',
  `good_qty`               DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '良品数(本记录均分后计入)',
  `bad_qty`                DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '不良数',
  `start_time`             DATETIME      NULL     DEFAULT NULL   COMMENT '开始时间',
  `end_time`               DATETIME      NULL     DEFAULT NULL   COMMENT '结束时间',
  `work_hours`             DECIMAL(10,2) NULL     DEFAULT 0      COMMENT '工时(小时,均分后)',
  `is_clean_merge`         INT           NULL     DEFAULT 0      COMMENT '是否清洗合并报工:0否 1是(按清洗合并码配置均分)',
  `merge_config_id`        VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '清洗合并码配置ID(历史报工保留原配置版本)',
  `member_snapshot`        VARCHAR(1000) NULL     DEFAULT NULL   COMMENT '报工人员快照(配置变更后可追溯原人员,JSON)',
  `status`                 INT           NULL     DEFAULT 1      COMMENT '状态:1有效 0作废(订单删除时标记作废保留历史)',
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
  KEY `idx_wr_card` (`flow_card_id`),
  KEY `idx_wr_card_process` (`card_process_id`),
  KEY `idx_wr_worker` (`worker_no`),
  KEY `idx_wr_group` (`group_no`),
  KEY `idx_wr_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='报工明细表(报工记录第3层级,多人均分)';

-- -----------------------------------------------------------------------------
-- 表 5：sx_report_check_detail 检包检验记录表
--   （检查为工序最后一步；末道检查工序报工时填写，材料不良4项/加工不良2项/返工5项）
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_report_check_detail`;
CREATE TABLE `sx_report_check_detail` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `report_id`              VARCHAR(50)   NOT NULL                COMMENT '报工明细ID(sx_work_report.F_ID)',
  `flow_card_id`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '流程卡ID(冗余)',
  `flow_card_no`           VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '流程卡号(冗余)',
  `check_date`             DATE          NULL     DEFAULT NULL   COMMENT '检验日期',
  `work_hours`             DECIMAL(10,2) NULL     DEFAULT 0      COMMENT '工时',
  `drawing_no`             VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '图番',
  `make_no`                VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '制造编号',
  `check_content`          VARCHAR(500)  NULL     DEFAULT NULL   COMMENT '检验内容',
  `check_qty`              DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '检验数量',
  `good_qty`               DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '良品数',
  `mat_bad_pull`           DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '材料不良-材料拉伤',
  `mat_bad_bubble`         DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '材料不良-气泡',
  `mat_bad_joint`          DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '材料不良-接头',
  `mat_bad_thin`           DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '材料不良-厚度薄',
  `proc_bad_injury`        DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '加工不良-伤',
  `proc_bad_other`         DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '加工不良-其它',
  `rework_clean`           DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '返工-返清洗',
  `rework_mill`            DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '返工-返铣面',
  `rework_repair`          DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '返工-返修品',
  `rework_fix`             DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '返工-修伤',
  `rework_dirty`           DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '返工-脏污',
  `duty_person`            VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '责任人',
  `is_temp_worker`         INT           NULL     DEFAULT 0      COMMENT '是否临时作业员:0否 1是',
  `operator_name`          VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '操作员',
  `qty`                    DECIMAL(18,4) NULL     DEFAULT 0      COMMENT '数量',
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
  KEY `idx_rcd_report` (`report_id`),
  KEY `idx_rcd_card` (`flow_card_id`),
  KEY `idx_rcd_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='检包检验记录表(检查工序最后一步)';

-- -----------------------------------------------------------------------------
-- 表 6：sx_clean_merge_config 清洗合并码配置表（版本化）
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_clean_merge_config`;
CREATE TABLE `sx_clean_merge_config` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `process_id`             VARCHAR(50)   NOT NULL                COMMENT '工序ID(sx_base_process.F_ID,工序:清洗)',
  `process_name`           VARCHAR(100)  NULL     DEFAULT NULL   COMMENT '工序名称(冗余)',
  `version`                VARCHAR(20)   NOT NULL                COMMENT '版本号(如V1/V2/V3)',
  `member_count`           INT           NULL     DEFAULT 4      COMMENT '报工人数(绩效均分口径,默认4人)',
  `effect_date`            DATE          NULL     DEFAULT NULL   COMMENT '生效日期',
  `expire_date`            DATE          NULL     DEFAULT NULL   COMMENT '失效日期(变更后旧版本写失效日期,永久保留)',
  `status`                 INT           NULL     DEFAULT 1      COMMENT '状态:1生效中 0已失效',
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
  KEY `idx_cmc_process` (`process_id`),
  KEY `idx_cmc_status` (`status`),
  KEY `idx_cmc_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='清洗合并码配置表(版本化,历史保留)';

-- -----------------------------------------------------------------------------
-- 表 7：sx_clean_merge_member 清洗合并码成员表
--   （配置变更生成新版本成员记录，历史版本成员永久保留可追溯）
-- -----------------------------------------------------------------------------
DROP TABLE IF EXISTS `sx_clean_merge_member`;
CREATE TABLE `sx_clean_merge_member` (
  `F_ID`                   VARCHAR(50)   NOT NULL                COMMENT '主键(雪花ID)',
  `F_TENANT_ID`            VARCHAR(50)   NOT NULL DEFAULT '0'    COMMENT '租户ID',
  `config_id`              VARCHAR(50)   NOT NULL                COMMENT '清洗合并码配置ID(sx_clean_merge_config.F_ID)',
  `employee_no`            VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '工号(如E001)',
  `employee_name`          VARCHAR(50)   NULL     DEFAULT NULL   COMMENT '姓名(如张伟)',
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
  KEY `idx_cmm_config` (`config_id`),
  KEY `idx_cmm_delete` (`F_DELETE_MARK`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='清洗合并码成员表';
