using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.WorkFlow.Entitys.Dto.Comment;

[SuppressSniffer]
public class CommentListQuery : PageInputBase
{
    /// <summary>
    /// 任务id.
    /// </summary>
    public string? taskId { get; set; }
}

