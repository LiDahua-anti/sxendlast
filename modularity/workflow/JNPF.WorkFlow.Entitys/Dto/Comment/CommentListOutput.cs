using JNPF.DependencyInjection;

namespace JNPF.WorkFlow.Entitys.Dto.Comment;

[SuppressSniffer]
public class CommentListOutput
{
    /// <summary>
    /// 创建时间.
    /// </summary>
    public DateTime? creatorTime { get; set; }

    /// <summary>
    /// 创建用户.
    /// </summary>
    public string? creatorUserId { get; set; }

    /// <summary>
    /// 创建用户名.
    /// </summary>
    public string? creatorUser { get; set; }

    /// <summary>
    /// 创建用户头像.
    /// </summary>
    public string? creatorUserHeadIcon { get; set; }

    /// <summary>
    /// 文本.
    /// </summary>
    public string? text { get; set; }

    /// <summary>
    /// 图片.
    /// </summary>
    public string? image { get; set; }

    /// <summary>
    /// 附件.
    /// </summary>
    public string? file { get; set; }

    /// <summary>
    /// 任务id.
    /// </summary>
    public string? taskId { get; set; }

    /// <summary>
    /// 自然主键.
    /// </summary>
    public string? id { get; set; }

    /// <summary>
    /// 0-删除按钮隐藏 1-删除按钮显示 2-评论被删.
    /// </summary>
    public int isDel { get; set; }

    /// <summary>
    /// 修改时间.
    /// </summary>
    public DateTime? lastModifyTime { get; set; }

    /// <summary>
    /// 回复人员用户名.
    /// </summary>
    public string? replyUser { get; set; }

    /// <summary>
    /// 回复人员内容.
    /// </summary>
    public string? replyText { get; set; }
}

