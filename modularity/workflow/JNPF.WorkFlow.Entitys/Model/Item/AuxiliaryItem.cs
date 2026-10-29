using JNPF.WorkFlow.Entitys.Model.Conifg;

namespace JNPF.WorkFlow.Entitys.Model.Item;

public class AuxiliaryItem
{
    public int id { get; set; }

    public string fullName { get; set; }

    public AuxiliaryConfig config { get; set; }
}
