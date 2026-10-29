using JNPF.Systems.Entitys.Dto.Custom;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace JNPF.Systems.Interfaces.Custom
{
    /// <summary>
    /// 设备报警履历服务接口.
    /// </summary>
    public interface IDeviceAlarmService
    {
        /// <summary>
        /// 设备报警履历列表（按设备/报警类型/时间/处理状态筛选）.
        /// </summary>
        Task<dynamic> GetAlarmList([FromBody] DeviceAlarmQuery input);

        /// <summary>
        /// 设备报警登记/编辑（报警编号自动生成 ALM+日期-序号）.
        /// </summary>
        Task CruAlarm([FromBody] DeviceAlarmInput input);

        /// <summary>
        /// 设备报警处理（恢复/处理登记）.
        /// </summary>
        Task HandleAlarm([FromBody] AlarmHandleInput input);
    }
}
