namespace JNPF.WorkFlow.Entitys.Dto.Template;

public class TemplateUpDownInput
{
    /// <summary>
    /// 0-上架 1-下架.
    /// </summary>
    public int isUp { get; set; }

    /// <summary>
    /// 0-继续审批 1-隐藏审批.
    /// </summary>
    public int isHidden { get; set; }
}
