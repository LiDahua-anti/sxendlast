using JNPF.Systems.Entitys.Dto.Custom;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace JNPF.Systems.Interfaces.Custom
{
    /// <summary>
    /// 工序管理（技术管理）服务接口.
    /// </summary>
    public interface IProcessService
    {
        /// <summary>
        /// 获取工序列表（分页，含检查项目数/关联设备数/是否有作业指导）.
        /// </summary>
        Task<dynamic> GetProcessList([FromBody] ProcessQuery input);

        /// <summary>
        /// 获取工序详情.
        /// </summary>
        Task<dynamic> GetProcessById(string id);

        /// <summary>
        /// 新增/编辑工序（复用 sx_base_process）.
        /// </summary>
        Task CruProcess([FromBody] ProcessCruInput input);

        /// <summary>
        /// 删除工序（级联软删除检查项/设备绑定/SOP）.
        /// </summary>
        Task DeleteProcess(string id);

        /// <summary>
        /// 获取工序检查内容定义.
        /// </summary>
        Task<dynamic> GetCheckItems(string processId);

        /// <summary>
        /// 保存工序检查内容定义（整体覆盖）.
        /// </summary>
        Task SaveCheckItems([FromBody] ProcessCheckItemSaveInput input);

        /// <summary>
        /// 获取工序设备绑定列表（含设备类型/状态）.
        /// </summary>
        Task<dynamic> GetProcessDevices(string processId);

        /// <summary>
        /// 保存工序设备绑定（整体覆盖）.
        /// </summary>
        Task SaveProcessDevices([FromBody] ProcessDeviceSaveInput input);

        /// <summary>
        /// 获取工序SOP作业指导文件列表.
        /// </summary>
        Task<dynamic> GetSopFiles(string processId);

        /// <summary>
        /// 新增工序SOP文件记录（上传后登记）.
        /// </summary>
        Task AddSopFile([FromBody] ProcessSopAddInput input);

        /// <summary>
        /// 删除工序SOP文件记录（软删除）.
        /// </summary>
        Task DeleteSopFile(string id);

        /// <summary>
        /// 工序下拉（供工艺路线选择工序）.
        /// </summary>
        Task<dynamic> GetProcessSelected(string keyword);
    }
}
