using JNPF.DependencyInjection;

namespace JNPF.Extend.Entitys.Dto.Employee;

[SuppressSniffer]
public class ImportDataInput
{
    /// <summary>
    /// 导入数据.
    /// </summary>
    public List<EmployeeListOutput>? list { get; set; }

    /// <summary>
    /// 文件名称.
    /// </summary>
    public string fileName { get; set; }

    /// <summary>
    /// 是否跳过预览.
    /// </summary>
    public bool type { get; set; }
}
