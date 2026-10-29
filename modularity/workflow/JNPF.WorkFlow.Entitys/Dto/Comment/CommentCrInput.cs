using JNPF.DependencyInjection;

namespace JNPF.WorkFlow.Entitys.Dto.Comment;

[SuppressSniffer]
public class CommentCrInput
{
    /// <summary>
    /// 附件.
    /// </summary>
    public string? file { get; set; }

    /// <summary>
    /// 图片.
    /// </summary>
    public string? image { get; set; }

    /// <summary>
    /// 任务id.
    /// </summary>
    public string? taskId { get; set; }

    /// <summary>
    /// 评论内容.
    /// </summary>
    public string? text { get; set; }

    /// <summary>
    /// 回复评论id.
    /// </summary>
    public string? replyId { get; set; }
}

