using JNPF.DependencyInjection;
using Microsoft.AspNetCore.Http;

namespace JNPF.Systems.Entitys.Dto.Document;

[SuppressSniffer]
public class DocumentUploaderInput
{
    /// <summary>
    /// 上级文件.
    /// </summary>
    public IFormFile? file { get; set; }

    /// <summary>
    /// 流程任务id.
    /// </summary>
    public string taskId { get; set; }
}