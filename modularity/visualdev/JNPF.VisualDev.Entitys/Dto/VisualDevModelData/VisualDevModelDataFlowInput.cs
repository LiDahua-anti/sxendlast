using JNPF.DependencyInjection;

namespace JNPF.VisualDev.Entitys.Dto.VisualDevModelData;

/// <summary>
/// 在线开发功能模块列表查询输入.
/// </summary>
[SuppressSniffer]
public class VisualDevModelDataFlowInput
{
    /// <summary>
    /// 流程id.
    /// </summary>
    public string template { get; set; }

    /// <summary>
    /// 按钮编码.
    /// </summary>
    public string btnCode { get; set; }

    /// <summary>
    /// 是否当前用户.
    /// </summary>
    public int? currentUser { get; set; }

    /// <summary>
    /// 是否自定义用户.
    /// </summary>
    public int? customUser { get; set; }

    /// <summary>
    /// 自定义列表.
    /// </summary>
    public List<string> initiator { get; set; }

    /// <summary>
    /// 数据列表.
    /// </summary>
    public List<List<TransferModel>> dataList { get; set; }

    /// <summary>
    /// 检验发起权限.
    /// </summary>
    public bool hasPermission { get; set; }
}

public class TransferModel
{
    public bool required { get; set; }

    public int? sourceType { get; set; }

    public string sourceValue { get; set; }

    public string targetField { get; set; }

    public string targetFieldLabel { get; set; }

    public object defaultValue { get; set; }
}