using JNPF.DependencyInjection;

namespace JNPF.WorkFlow.Entitys.Dto.Comment
{
    [SuppressSniffer]
    public class CommentUpInput : CommentCrInput
    {
        /// <summary>
        /// id.
        /// </summary>
        public string? id { get; set; }
    }
}
