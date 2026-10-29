namespace JNPF.WorkFlow.Entitys.Model.Conifg;

public class AuxiliaryConfig
{
    /// <summary>
    /// 启用开关.
    /// </summary>
    public int on { get; set; }

    /// <summary>
    /// 提示内容.
    /// </summary>
    public string content { get; set; }

    /// <summary>
    /// 连接.
    /// </summary>
    public List<object> linkList { get; set; }

    /// <summary>
    /// 归档文件.
    /// </summary>
    public dynamic fileList { get; set; }

    /// <summary>
    /// 数据.
    /// </summary>
    public List<object> dataList { get; set; }
}
