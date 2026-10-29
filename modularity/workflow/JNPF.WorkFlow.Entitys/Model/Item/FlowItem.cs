using JNPF.DependencyInjection;

namespace JNPF.WorkFlow.Entitys.Model.Item;

[SuppressSniffer]
public class FlowItem
{
    public string id { get; set; }

    public string fullName { get; set; }

    public int count { get; set; }
}
