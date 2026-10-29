
namespace JNPF.Systems.Entitys.Dto.Role;

public class RoleListImportDataInput
{
    public string id { get; set; }

    public string fullName { get; set; }
    public string enCode { get; set; }
    public string sortCode { get; set; }
    public string description { get; set; }

    /// <summary>
    /// 状态.
    /// </summary>
    public string type { get; set; }

    /// <summary>
    /// 角色类型.
    /// </summary>
    public int? globalMark { get; set; }

    /// <summary>
    /// 所属组织.
    /// </summary>
    public string organizeId { get; set; }

    public DateTime? creatorTime { get; set; }

    /// <summary>
    /// 异常错误原因.
    /// </summary>
    public string errorsInfo { get; set; } = string.Empty;
}

public class RoleImportDataInput
{
    /// <summary>
    /// 导入的数据列表.
    /// </summary>
    public List<RoleListImportDataInput> list { get; set; }

    /// <summary>
    /// 文件名称.
    /// </summary>
    public string fileName { get; set; }

    /// <summary>
    /// 是否跳过预览.
    /// </summary>
    public bool type { get; set; }
}
