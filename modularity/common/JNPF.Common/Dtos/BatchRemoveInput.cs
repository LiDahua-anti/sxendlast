namespace JNPF.Common.Dtos;

/// <summary>
/// 批量删除输入.
/// </summary>
public class BatchRemoveInput
{
    /// <summary>
    /// 流程Id.
    /// </summary>
    public string flowId { get; set; }

    /// <summary>
    /// 删除id.
    /// </summary>
    public List<object> ids { get; set; }
}