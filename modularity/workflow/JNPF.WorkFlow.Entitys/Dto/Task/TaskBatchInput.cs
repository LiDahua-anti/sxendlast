namespace JNPF.WorkFlow.Entitys.Dto.Task;

public class TaskBatchInput
{
    /// <summary>
    /// id集合.
    /// </summary>
    public List<string> ids { get; set; }

    public int type { get; set; }
}
