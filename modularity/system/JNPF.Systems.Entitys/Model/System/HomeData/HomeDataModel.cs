namespace JNPF.Systems.Entitys.Model.System.HomeData;

public class HomeDataModel
{
    public string id { get; set; }
    public string fullName { get; set; }
    public string enCode { get; set; }
    public string icon { get; set; }
    public string urlAddress { get; set; }
    public int count { get; set; }
}

public class HomeDataNoticeItem
{
    /// <summary>
    /// ID.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 标题.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    public DateTime? creatorTime { get; set; }

    /// <summary>
    /// 分类.
    /// </summary>
    public string category { get; set; }

    /// <summary>
    /// 图片.
    /// </summary>
    public string coverImage { get; set; }

    /// <summary>
    /// 创建人.
    /// </summary>
    public string creatorUser { get; set; }

    /// <summary>
    /// 摘要.
    /// </summary>
    public string excerpt { get; set; }

    /// <summary>
    /// 发布时间.
    /// </summary>
    public DateTime? releaseTime { get; set; }

    /// <summary>
    /// 发布人.
    /// </summary>
    public string releaseUser { get; set; }
}

public class HomeDataMessageItem
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 标题.
    /// </summary>
    public string title { get; set; }

    /// <summary>
    /// 正文内容.
    /// </summary>
    public int? type { get; set; }

    /// <summary>
    /// 正文内容.
    /// </summary>
    public string typeName { get; set; }

    /// <summary>
    /// 发送人员.
    /// </summary>
    public string releaseUser { get; set; }

    /// <summary>
    /// 发送时间.
    /// </summary>
    public DateTime? releaseTime { get; set; }

    /// <summary>
    /// 是否已读(0-未读，1-已读).
    /// </summary>
    public int? isRead { get; set; }

    /// <summary>
    /// 流程消息类型(1-审批，2-委托).
    /// </summary>
    public int? flowType { get; set; }
}

