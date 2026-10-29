using JNPF.WorkFlow.Entitys.Model.Conifg;

namespace JNPF.WorkFlow.Entitys.Model.Properties;

public class FuncProperties
{
    /// <summary>
    /// 流程发起事件.
    /// </summary>
    public FuncConfig? initFuncConfig { get; set; }

    /// <summary>
    /// 流程结束事件.
    /// </summary>
    public FuncConfig? endFuncConfig { get; set; }

    /// <summary>
    /// 流程撤回事件.
    /// </summary>
    public FuncConfig? flowRecallFuncConfig { get; set; }

    /// <summary>
    /// 审核同意事件.
    /// </summary>
    public FuncConfig? approveFuncConfig { get; set; } = new FuncConfig();

    /// <summary>
    /// 审核拒绝事件.
    /// </summary>
    public FuncConfig? rejectFuncConfig { get; set; } = new FuncConfig();

    /// <summary>
    /// 审核退回事件.
    /// </summary>
    public FuncConfig? backFuncConfig { get; set; } = new FuncConfig();

    /// <summary>
    /// 审核撤回事件.
    /// </summary>
    public FuncConfig? recallFuncConfig { get; set; } = new FuncConfig();

    /// <summary>
    /// 审核超时事件.
    /// </summary>
    public FuncConfig? overTimeFuncConfig { get; set; } = new FuncConfig();

    /// <summary>
    /// 审核提醒事件.
    /// </summary>
    public FuncConfig? noticeFuncConfig { get; set; } = new FuncConfig();
}
