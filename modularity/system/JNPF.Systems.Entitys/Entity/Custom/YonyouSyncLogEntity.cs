using JNPF.Common.Contracts;
using SqlSugar;
using System;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 用友U8同步日志表（系统对接）.
    /// MES/WMS 业务单据审核后自动推送用友（MES→用友方向）；
    /// 基础数据（存货/生产订单/仓库档案）从用友同步至MES（用友→MES方向）；
    /// 模拟对接：记录请求/响应内容，支持失败重试与日志追踪。
    /// </summary>
    [SugarTable("sx_yonyou_sync_log")]
    public class YonyouSyncLogEntity : CLDSEntityBase
    {
        /// <summary>
        /// 单据类型（采购入库单/产成品入库单/材料出库单/销售出库单/生产领料单/生产订单/存货档案/仓库档案/现有量推送）.
        /// </summary>
        [SugarColumn(ColumnName = "biz_type", IsNullable = true, ColumnDescription = "/// 单据类型（采购入库单/产成品入库单/材料出库单/销售出库单/生产领料单/生产订单/存货档案/仓库档案/现有量推送）. ///")]
        [Description("单据类型")]
        public string BizType { get; set; }

        /// <summary>
        /// 单据号（如RK20260910001/LL20260910001）.
        /// </summary>
        [SugarColumn(ColumnName = "biz_no", IsNullable = true, ColumnDescription = "/// 单据号（如RK20260910001/LL20260910001）. ///")]
        [Description("单据号")]
        public string BizNo { get; set; }

        /// <summary>
        /// 同步方向：1 MES→用友 2 用友→MES.
        /// </summary>
        [SugarColumn(ColumnName = "direction", IsNullable = true, ColumnDescription = "/// 同步方向：1 MES→用友 2 用友→MES. ///")]
        [Description("同步方向:1 MES→用友 2 用友→MES")]
        public int? Direction { get; set; }

        /// <summary>
        /// 用友凭证号（如PZ-00123，成功后回填）.
        /// </summary>
        [SugarColumn(ColumnName = "voucher_no", IsNullable = true, ColumnDescription = "/// 用友凭证号（如PZ-00123，成功后回填）. ///")]
        [Description("用友凭证号")]
        public string VoucherNo { get; set; }

        /// <summary>
        /// 状态：1成功 2失败.
        /// </summary>
        [SugarColumn(ColumnName = "status", IsNullable = true, ColumnDescription = "/// 状态：1成功 2失败. ///")]
        [Description("状态:1成功 2失败")]
        public int? Status { get; set; }

        /// <summary>
        /// 失败原因（如:物料编码不存在）.
        /// </summary>
        [SugarColumn(ColumnName = "fail_reason", IsNullable = true, ColumnDescription = "/// 失败原因（如:物料编码不存在）. ///")]
        [Description("失败原因")]
        public string FailReason { get; set; }

        /// <summary>
        /// 同步时间.
        /// </summary>
        [SugarColumn(ColumnName = "sync_time", IsNullable = true, ColumnDescription = "/// 同步时间. ///")]
        [Description("同步时间")]
        public DateTime? SyncTime { get; set; }

        /// <summary>
        /// 耗时（毫秒）.
        /// </summary>
        [SugarColumn(ColumnName = "cost_ms", IsNullable = true, ColumnDescription = "/// 耗时（毫秒）. ///")]
        [Description("耗时(毫秒)")]
        public int? CostMs { get; set; }

        /// <summary>
        /// 触发方式：1自动（单据审核后） 2手动触发.
        /// </summary>
        [SugarColumn(ColumnName = "trigger_type", IsNullable = true, ColumnDescription = "/// 触发方式：1自动（单据审核后） 2手动触发. ///")]
        [Description("触发方式:1自动 2手动触发")]
        public int? TriggerType { get; set; }

        /// <summary>
        /// 重试次数.
        /// </summary>
        [SugarColumn(ColumnName = "retry_count", IsNullable = true, ColumnDescription = "/// 重试次数. ///")]
        [Description("重试次数")]
        public int? RetryCount { get; set; }

        /// <summary>
        /// 请求内容（模拟对接，JSON）.
        /// </summary>
        [SugarColumn(ColumnName = "request_content", IsNullable = true, ColumnDescription = "/// 请求内容（模拟对接，JSON）. ///")]
        [Description("请求内容(模拟对接,JSON)")]
        public string RequestContent { get; set; }

        /// <summary>
        /// 响应内容（模拟对接，JSON）.
        /// </summary>
        [SugarColumn(ColumnName = "response_content", IsNullable = true, ColumnDescription = "/// 响应内容（模拟对接，JSON）. ///")]
        [Description("响应内容(模拟对接,JSON)")]
        public string ResponseContent { get; set; }

        /// <summary>
        /// 操作人.
        /// </summary>
        [SugarColumn(ColumnName = "operator_name", IsNullable = true, ColumnDescription = "/// 操作人. ///")]
        [Description("操作人")]
        public string OperatorName { get; set; }

        /// <summary>
        /// 备注.
        /// </summary>
        [SugarColumn(ColumnName = "remark", IsNullable = true, ColumnDescription = "/// 备注. ///")]
        [Description("备注")]
        public string Remark { get; set; }
    }
}
