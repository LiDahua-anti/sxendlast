using JNPF.DependencyInjection;

namespace JNPF.WorkFlow.Entitys.Dto.Delegete;

[SuppressSniffer]
public class DelegeteUpInput : DelegeteCrInput
{
    /// <summary>
    /// id.
    /// </summary>
    public string? id { get; set; }
}

