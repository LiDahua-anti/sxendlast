using JNPF.Common.Dtos.VisualDev;

namespace JNPF.VisualDev.Entitys.Dto.VisualDevModelData;

/// <summary>
/// 在线开发功能模块列表查询输入.
/// </summary>
public class VisualDevModelBatchInput : VisualDevModelDataUpInput
{
    public List<Dictionary<string, object>> dataList { get; set; }

    public bool isCreate { get; set; }
}