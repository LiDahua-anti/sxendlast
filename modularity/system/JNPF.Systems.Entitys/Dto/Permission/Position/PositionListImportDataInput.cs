
namespace JNPF.Systems.Entitys.Dto.Position;

public class PositionListImportDataInput
{
    public string id { get; set; }
    public string fullName { get; set; }
    public string enCode { get; set; }

    /// <summary>
    /// 所属组织.
    /// </summary>
    public string organizeId { get; set; }

    /// <summary>
    /// 上级岗位.
    /// </summary>
    public string parentId { get; set; }

    /// <summary>
    /// 岗位id 树.
    /// </summary>
    public string positionIdTree { get; set; }

    /// <summary>
    /// 岗位约束 是否开启.
    /// </summary>
    public string isCondition { get; set; }

    /// <summary>
    /// 0 互斥约束、1 基数约束、先决约束.
    /// </summary>
    public string constraintType { get; set; }

    /// <summary>
    /// 互斥岗位.
    /// </summary>
    public string mutualExclusion { get; set; }

    /// <summary>
    /// 用户基数.
    /// </summary>
    public string? userNum { get; set; }

    /// <summary>
    /// 权限基数.
    /// </summary>
    public string? permissionNum { get; set; }

    /// <summary>
    /// 先决岗位.
    /// </summary>
    public string prerequisite { get; set; }

    public string sortCode { get; set; }
    public string description { get; set; }

    /// <summary>
    /// 异常错误原因.
    /// </summary>
    public string errorsInfo { get; set; } = string.Empty;
}

public class PositionImportDataInput
{
    /// <summary>
    /// 导入的数据列表.
    /// </summary>
    public List<PositionListImportDataInput> list { get; set; }

    /// <summary>
    /// 文件名称.
    /// </summary>
    public string fileName { get; set; }

    /// <summary>
    /// 是否跳过预览.
    /// </summary>
    public bool type { get; set; }
}
