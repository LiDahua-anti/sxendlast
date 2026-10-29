using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.User;

/// <summary>
/// 批量选择用户输入.
/// </summary>
[SuppressSniffer]
public class UserBatchSelectedInput : UserSelectedInput
{
    /// <summary>
    /// 更改状态.
    /// </summary>
    public int enabledMark { get; set; }
}