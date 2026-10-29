-- ================================================================
-- 到货明细表(sx_purchase_arrival_detail)增加流程卡相关字段
-- 执行前请备份
-- ================================================================

ALTER TABLE `sx_purchase_arrival_detail`
  ADD COLUMN `arrival_qty`    DECIMAL(18,4)  DEFAULT NULL    COMMENT '到货数量' AFTER `main_unit`,
  ADD COLUMN `print_count`    INT            DEFAULT 0       COMMENT '流程卡打印次数' AFTER `arrival_qty`,
  ADD COLUMN `total_qty`      DECIMAL(18,4)  DEFAULT 0       COMMENT '投产数量（总定量）' AFTER `print_count`,
  ADD COLUMN `total_pallets`  INT            DEFAULT 0       COMMENT '总托数' AFTER `total_qty`,
  ADD COLUMN `work_order_no`  VARCHAR(100)   DEFAULT NULL    COMMENT '工作令号' AFTER `total_pallets`,
  ADD COLUMN `make_no`        VARCHAR(100)   DEFAULT NULL    COMMENT '制造编号' AFTER `work_order_no`,
  ADD COLUMN `type_text`      VARCHAR(50)    DEFAULT NULL    COMMENT '类型' AFTER `make_no`;
