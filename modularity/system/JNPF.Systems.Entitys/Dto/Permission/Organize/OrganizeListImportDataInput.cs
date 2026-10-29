namespace JNPF.Systems.Entitys.Dto.Organize;

public class OrganizeListImportDataInput
{
    public string id { get; set; }
    public string fullName { get; set; }
    public string enCode { get; set; }
    //public string shortName { get; set; }

    /// <summary>
    /// 公司性质.
    /// </summary>
    //public string? enterpriseNature { get; set; }

    /// <summary>
    /// 所属行业.
    /// </summary>
    //public string industry { get; set; }
    //public string foundedTime { get; set; }
    //public string telePhone { get; set; }
    //public string fax { get; set; }
    //public string webSite { get; set; }
    //public string address { get; set; }
    public string sortCode { get; set; }
    public string description { get; set; }
    //public string managerName { get; set; }
    //public string managerTelePhone { get; set; }
    //public string managerMobilePhone { get; set; }
    //public string manageEmail { get; set; }
    //public string bankName { get; set; }
    //public string bankAccount { get; set; }
    //public string businessscope { get; set; }

    /// <summary>
    /// 所属组织.
    /// </summary>
    public string parentId { get; set; }

    /// <summary>
    /// 组织类型.
    /// </summary>
    public string category { get; set; }

    /// <summary>
    /// 部门主管.
    /// </summary>
    //public string managerId { get; set; }

    /// <summary>
    /// 组织Id树.
    /// </summary>
    public string organizeIdTree { get; set; }

    /// <summary>
    /// 组织名称树.
    /// </summary>
    public string organizeNameTree { get; set; }

    /// <summary>
    /// 异常错误原因.
    /// </summary>
    public string errorsInfo { get; set; } = string.Empty;
}

public class OrganizeImportDataInput
{
    /// <summary>
    /// 导入的数据列表.
    /// </summary>
    public List<OrganizeListImportDataInput> list { get; set; }

    /// <summary>
    /// 文件名称.
    /// </summary>
    public string fileName { get; set; }

    /// <summary>
    /// 是否跳过预览.
    /// </summary>
    public bool type { get; set; }
}
