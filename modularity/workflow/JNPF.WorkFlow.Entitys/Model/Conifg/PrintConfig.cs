using JNPF.WorkFlow.Entitys.Model.Properties;

namespace JNPF.WorkFlow.Entitys.Model.Conifg;

public class PrintConfig : ConditionProperties
{
    /// <summary>
    /// 是否开启.
    /// </summary>
    public bool on { get; set; }

    /// <summary>
    /// 模板.
    /// </summary>
    public List<string> printIds { get; set; } = new List<string>();

    /// <summary>
    /// 打印条件 1：不限制  2：节点结束  3：流程结束  4：条件设置.
    /// </summary>
    public int conditionType { get; set; } = 1;
}

