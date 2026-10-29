namespace JNPF.example.Entitys.Dto.JtBaseOrder;

/// <summary>
/// 排产计划（二开）输入参数.
/// </summary>
public class JtBaseOrderListOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string? id { get; set; }

    /// <summary>
    /// PPH（小时产能）V.
    /// </summary>
    public string? pph_value { get; set; }

    /// <summary>
    /// CT  V.
    /// </summary>
    public string? ct_value { get; set; }

    /// <summary>
    /// 是否处于暂停中（有未结束的暂停记录）
    /// </summary>
    public bool is_paused { get; set; }

    /// <summary>
    /// MPPH  V.
    /// </summary>
    public string? mpph_value { get; set; }

    /// <summary>
    /// 产线V.
    /// </summary>
    public string? production_type { get; set; }

    /// <summary>
    /// 班次V.
    /// </summary>
    public string? work_shift { get; set; }

    /// <summary>
    /// 开工日期V.
    /// </summary>
    public string start_date { get; set; }

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
    public string F_CREATOR_TIME { get; set; }

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
    public string actural_start_time { get; set; }

    /// <summary>
    /// 实际结束时间.
    /// </summary>
    public string actural_end_time { get; set; }

}