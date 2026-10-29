using JNPF.DependencyInjection;

namespace JNPF.VisualDev.Entitys.Dto.VisualDev;

/// <summary>
/// 功能导出.
/// </summary>
[SuppressSniffer]
public class VisualDevExportOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 状态(0-未发步，1-已发布，2-已修改).
    /// </summary>
    public int? state { get; set; }

    /// <summary>
    /// 类型
    /// 1-自定义表单，2-系统表单.
    /// </summary>
    public int? type { get; set; }

    /// <summary>
    /// 关联的表.
    /// </summary>
    public string tables { get; set; }

    /// <summary>
    /// 分类.
    /// </summary>
    public string category { get; set; }

    /// <summary>
    /// 表单配置JSON.
    /// </summary>
    public string formData { get; set; }

    /// <summary>
    /// 列表配置JSON.
    /// </summary>
    public string columnData { get; set; }

    /// <summary>
    /// 描述或说明.
    /// </summary>
    public string description { get; set; }

    /// <summary>
    /// 启用标识
    /// 0-禁用,1-启用.
    /// </summary>
    public int? enabledMark { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long sortCode { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    public DateTime? creatorTime { get; set; }

    /// <summary>
    /// 创建用户.
    /// </summary>
    public string creatorUserId { get; set; }

    /// <summary>
    /// 修改时间.
    /// </summary>
    public DateTime? lastModifyTime { get; set; }

    /// <summary>
    /// 修改用户.
    /// </summary>
    public string lastModifyUserId { get; set; }

    /// <summary>
    /// 关联数据连接id.
    /// </summary>
    public string dbLinkId { get; set; }

    /// <summary>
    /// 页面类型（1、纯表单，2、表单加列表，4、数据视图）.
    /// </summary>
    public int? webType { get; set; }

    /// <summary>
    /// App列表配置JSON.
    /// </summary>
    public string appColumnData { get; set; }

    /// <summary>
    /// 接口id.
    /// </summary>
    public string interfaceId { get; set; }

    /// <summary>
    /// 接口参数.
    /// </summary>
    public string interfaceParam { get; set; }

    /// <summary>
    /// 发布选中平台.
    /// </summary>
    public string platformRelease { get; set; }

    /// <summary>
    /// Web地址.
    /// </summary>
    public string urlAddress { get; set; }

    /// <summary>
    /// APP地址.
    /// </summary>
    public string appUrlAddress { get; set; }

    /// <summary>
    /// 接口路径.
    /// </summary>
    public string interfaceUrl { get; set; }

    /// <summary>
    /// web页面地址.
    /// </summary>
    public string webAddress { get; set; }

    /// <summary>
    /// app页面地址.
    /// </summary>
    public string appAddress { get; set; }

    /// <summary>
    /// 按钮json.
    /// </summary>
    public string buttonData { get; set; }

    /// <summary>
    /// app按钮json.
    /// </summary>
    public string appButtonData { get; set; }

    /// <summary>
    /// 系统id.
    /// </summary>
    public string systemId { get; set; }

    /// <summary>
    /// 是否启用流程.
    /// </summary>
    public int enableFlow { get; set; }

    /// <summary>
    /// 代码生成命名规范.
    /// </summary>
    public List<VisualAliasEntity> aliasListJson { get; set; }
}
