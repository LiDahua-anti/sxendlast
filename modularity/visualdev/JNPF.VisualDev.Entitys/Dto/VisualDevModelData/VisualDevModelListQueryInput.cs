using JNPF.Common.Filter;

namespace JNPF.VisualDev.Entitys.Dto.VisualDevModelData;

/// <summary>
/// 在线开发功能模块列表查询输入.
/// </summary>
public class VisualDevModelListQueryInput : PageInputBase
{
    /// <summary>
    /// 选择导出数据ids.
    /// </summary>
    public virtual List<string> selectIds { get; set; }

    /// <summary>
    /// 选择导出数据key.
    /// </summary>
    public virtual List<string> selectKey { get; set; }

    /// <summary>
    /// 导出类型.
    /// </summary>
    public string dataType { get; set; } = "0";

    /// <summary>
    /// 数据过滤.
    /// </summary>
    public string dataRuleJson { get; set; }

    /// <summary>
    /// 高级查询.
    /// </summary>
    public virtual string superQueryJson { get; set; }

    /// <summary>
    /// 页签查询.
    /// </summary>
    public virtual string extraQueryJson { get; set; }

    /// <summary>
    /// 数据权限.
    /// </summary>
    public string dataPermissionJson { get; set; }

    /// <summary>
    /// 菜单ID.
    /// </summary>
    public string? menuId { get; set; }

    /// <summary>
    /// 集成助手使用.
    /// 是否为流程审核完成.
    /// 0：不是,1：是.
    /// </summary>
    public int isProcessReviewCompleted { get; set; }

    /// <summary>
    /// 集成助手使用.
    /// 是否只给id.
    /// 0：不是,1：是.
    /// </summary>
    public int isOnlyId { get; set; }

    /// <summary>
    /// 集成助手使用.
    /// 是否转换数据.
    /// 0：转换,1：不转换.
    /// </summary>
    public int isConvertData { get; set; }

    /// <summary>
    /// 集成助手使用就一定会有值.
    /// 流程ids.
    /// </summary>
    public string flowIds { get; set; }

    /// <summary>
    /// 查询设置
    /// 0：简易查询，1：全字段查询.
    /// </summary>
    public int? queryType { get; set; }
}

/// <summary>
/// 代码生成视图 导出 输入.
/// </summary>
public class VisualDevCodeGenQueryInput : VisualDevModelListQueryInput
{
    /// <summary>
    /// 选择导出数据ids.
    /// </summary>
    public object selectIds { get; set; }

    /// <summary>
    /// 选择导出数据key.
    /// </summary>
    public object selectKey { get; set; }

}