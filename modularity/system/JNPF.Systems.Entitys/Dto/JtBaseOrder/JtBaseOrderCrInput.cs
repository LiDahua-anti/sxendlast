using JNPF.Common.Filter;
using JNPF.JsonSerialization;
using Newtonsoft.Json;

namespace JNPF.example.Entitys.Dto.JtBaseOrder;

public class ActuralOrdesInput : PageInputBase
{
    /// <summary>
    /// 开始时间
    /// </summary>
    public string startTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public string endTime { get; set; }

    /// <summary>
    /// 产线名称
    /// </summary>
    public string typeName { get; set; }

    /// <summary>
    /// 型号
    /// </summary>
    public string code { get; set; }


}

/// <summary>
/// 更改实际产量输入参数
/// </summary>
public class UpdateActuralOutputInput
{
    /// <summary>
    /// 订单ID
    /// </summary>
    public string lineId { get; set; }

    /// <summary>
    /// 订单ID
    /// </summary>
    public string orderId { get; set; }

    /// <summary>
    /// 实际产量
    /// </summary>
    public string num { get; set; }

}

/// <summary>
/// 生产数据采集输出 DTO（字符串版本）
/// </summary>
public class ProductionDataCollectOutput
{
    /// <summary>
    /// 主键
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 日期 (格式：yyyy-MM-dd)
    /// </summary>
    public string Date { get; set; }

    /// <summary>
    /// 产线名称
    /// </summary>
    public string ProductionLine { get; set; }

    /// <summary>
    /// 型号/产品型号
    /// </summary>
    public string Model { get; set; }

    /// <summary>
    /// 实际CT（实际周期时间）
    /// </summary>
    public string ActualCT { get; set; }

    /// <summary>
    /// 产量/生产数量
    /// </summary>
    public string Output { get; set; }

    /// <summary>
    /// 采集区间（如：08:00-10:00）
    /// </summary>
    public string CollectInterval { get; set; }
}


public class JtBaseOrderStartInput
{
    /// <summary>
    /// 
    /// </summary>
    public string Id { get; set; }

}

public class JtBaseOrderAllInput
{
    /// <summary>
    /// 
    /// </summary>
    public int orderType { get; set; }

}

/// <summary>
/// 排产计划（二开）修改输入参数.
/// </summary>
public class JtBaseOrderCrInput
{
    /// <summary>
    /// PPH（小时产能）V.
    /// </summary>
    public string? pph_value { get; set; }

    /// <summary>
    /// CT  V.
    /// </summary>
    public string? ct_value { get; set; }

    /// <summary>
    /// MPPH  V.
    /// </summary>
    public string? mpph_value { get; set; }

    /// <summary>
    /// 产线V.
    /// </summary>
    public string production_type { get; set; }

    /// <summary>
    /// 班次V.
    /// </summary>
    public string? work_shift { get; set; }

    /// <summary>
    /// 开工日期V.
    /// </summary>
    public DateTime? start_date { get; set; }

    /// <summary>
    /// 完成品编码/型番V.
    /// </summary>
    public string? product_code { get; set; }

    /// <summary>
    /// 完成品型号V.
    /// </summary>
    public string? product_name { get; set; }

    /// <summary>
    /// 营业口座 V.
    /// </summary>
    public string? open_no { get; set; }

    /// <summary>
    /// NC物料编码 V.
    /// </summary>
    public string? nc_bomNo { get; set; }

    /// <summary>
    /// 计划数量V.
    /// </summary>
    public string? quantity { get; set; }

    /// <summary>
    /// 生产状态(0未生产 1生产中 2已完成).
    /// </summary>
    public string production_status { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>

    /// <summary>
    /// 创建用户.
    /// </summary>
    public string? F_CREATOR_USER_ID { get; set; }

    /// <summary>
    /// 排序码.
    /// </summary>
    public long? F_SORT_CODE { get; set; }

    /// <summary>
    /// 班次类型.
    /// </summary>
    public string? class_type { get; set; }

    /// <summary>
    /// 时间.
    /// </summary>
    public string? class_time { get; set; }

    /// <summary>
    /// 计划开始时间.
    /// </summary>
    public string? start_time { get; set; }

    /// <summary>
    /// 计划结束时间.
    /// </summary>
    public string? end_time { get; set; }

    /// <summary>
    /// 实际开始时间.
    /// </summary>
    public DateTime? actural_start_time { get; set; }

    /// <summary>
    /// 实际结束时间.
    /// </summary>
    public DateTime? actural_end_time { get; set; }

}