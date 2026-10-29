using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.LinqBuilder;
using JNPF.Systems.Entitys.Dto.Custom;
using JNPF.Systems.Entitys.Entity.Custom;
using JNPF.Systems.Interfaces.Custom;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System;
using System.Threading.Tasks;

namespace JNPF.Systems.Custom
{
    /// <summary>
    /// 设备报警履历.
    /// 设备报警履历面板：报警编号/设备编号/设备名称/报警类型（温度异常/压力异常/通讯中断/急停触发）/报警内容/触发时间/恢复时间/处理人；
    /// 支持按设备、报警类型、时间、处理状态筛选；报警处理登记（恢复时间）。
    /// </summary>
    [ApiDescriptionSettings(Tag = "DeviceAlarm", Name = "", Order = 212)]
    [Route("api/[controller]")]
    public class DeviceAlarmService : IDeviceAlarmService, IDynamicApiController, ITransient
    {
        /// <summary>
        /// 设备报警履历仓储.
        /// </summary>
        private readonly ISqlSugarRepository<DeviceAlarmEntity> _alarmRepository;

        /// <summary>
        /// 设备台账仓储.
        /// </summary>
        private readonly ISqlSugarRepository<DeviceMasterEntity> _deviceRepository;

        /// <summary>
        /// 用户管理器.
        /// </summary>
        private readonly IUserManager _userManager;

        /// <summary>
        /// 初始化一个<see cref="DeviceAlarmService"/>类型的新实例.
        /// </summary>
        public DeviceAlarmService(
            ISqlSugarRepository<DeviceAlarmEntity> alarmRepository,
            ISqlSugarRepository<DeviceMasterEntity> deviceRepository,
            IUserManager userManager)
        {
            _alarmRepository = alarmRepository;
            _deviceRepository = deviceRepository;
            _userManager = userManager;
        }

        /// <summary>
        /// 设备报警履历列表（按设备/报警类型/时间/处理状态筛选）.
        /// </summary>
        /// <param name="input">查询条件.</param>
        /// <returns>分页列表.</returns>
        [HttpPost("GetAlarmList")]
        public async Task<dynamic> GetAlarmList([FromBody] DeviceAlarmQuery input)
        {
            var whereLambda = LinqExpression.And<DeviceAlarmEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null);

            if (!string.IsNullOrEmpty(input.DeviceCode))
            {
                whereLambda = whereLambda.And(m => m.DeviceCode.Contains(input.DeviceCode));
            }

            if (!string.IsNullOrEmpty(input.DeviceName))
            {
                whereLambda = whereLambda.And(m => m.DeviceName.Contains(input.DeviceName));
            }

            if (!string.IsNullOrEmpty(input.AlarmType))
            {
                whereLambda = whereLambda.And(m => m.AlarmType == input.AlarmType);
            }

            if (input.HandleStatus != -1)
            {
                whereLambda = whereLambda.And(m => m.HandleStatus == input.HandleStatus);
            }

            if (input.StartTime != null)
            {
                whereLambda = whereLambda.And(m => m.TriggerTime >= input.StartTime);
            }

            if (input.EndTime != null)
            {
                var endTime = input.EndTime.Value.Date.AddDays(1);
                whereLambda = whereLambda.And(m => m.TriggerTime < endTime);
            }

            return await _alarmRepository.AsQueryable()
                .Where(whereLambda)
                .OrderBy(it => it.TriggerTime, OrderByType.Desc)
                .OrderBy(it => it.CreatorTime, OrderByType.Desc)
                .ToPagedListAsync(input.currentPage, input.pageSize);
        }

        /// <summary>
        /// 设备报警登记/编辑（报警编号自动生成 ALM+日期-序号）.
        /// </summary>
        /// <param name="input">登记入参.</param>
        /// <returns>无.</returns>
        [HttpPost("CruAlarm")]
        public async Task CruAlarm([FromBody] DeviceAlarmInput input)
        {
            if (!input.Id.IsNullOrEmpty())
            {
                // 编辑
                var existing = await _alarmRepository.GetSingleAsync(m => m.Id == input.Id && m.DeleteMark == null);
                if (existing == null)
                {
                    throw Oops.Oh(ErrorCode.COM1007);
                }

                input.Adapt(existing);
                var editOk = await _alarmRepository.AsSugarClient().Updateable(existing)
                    .IgnoreColumns(ignoreAllNullColumns: true)
                    .CallEntityMethod(m => m.LastModify())
                    .ExecuteCommandAsync();
                if (!(editOk > 0))
                {
                    throw Oops.Oh(ErrorCode.COM1001);
                }

                return;
            }

            // 新增：设备台账回填设备编号/名称
            if (!input.DeviceId.IsNullOrEmpty())
            {
                var device = await _deviceRepository.GetFirstAsync(m => m.Id == input.DeviceId && m.DeleteMark == null);
                if (device != null)
                {
                    if (input.DeviceCode.IsNullOrEmpty())
                    {
                        input.DeviceCode = device.DeviceNo;
                    }

                    if (input.DeviceName.IsNullOrEmpty())
                    {
                        input.DeviceName = device.DeviceName;
                    }
                }
            }

            var now = DateTime.Now;
            var prefix = "ALM" + now.ToString("yyyyMMdd");
            var seq = await _alarmRepository.AsQueryable()
                .Where(m => m.AlarmNo.StartsWith(prefix)).CountAsync() + 1;
            var alarmNo = prefix + "-" + seq.ToString("D3");
            while (await _alarmRepository.IsAnyAsync(m => m.AlarmNo == alarmNo))
            {
                seq++;
                alarmNo = prefix + "-" + seq.ToString("D3");
            }

            var entity = input.Adapt<DeviceAlarmEntity>();
            entity.Id = null;
            entity.AlarmNo = alarmNo;
            entity.TriggerTime = input.TriggerTime ?? now;
            entity.HandleStatus = 0; // 未恢复

            var insertOk = await _alarmRepository.AsSugarClient().Insertable(entity)
                .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            if (!(insertOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1000);
            }
        }

        /// <summary>
        /// 设备报警处理（恢复/处理登记）.
        /// </summary>
        /// <param name="input">处理入参.</param>
        /// <returns>无.</returns>
        [HttpPost("HandleAlarm")]
        public async Task HandleAlarm([FromBody] AlarmHandleInput input)
        {
            if (input.Id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var alarm = await _alarmRepository.GetSingleAsync(m => m.Id == input.Id && m.DeleteMark == null);
            if (alarm == null)
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            alarm.HandleStatus = input.HandleStatus;
            if (!input.HandlerName.IsNullOrEmpty())
            {
                alarm.HandlerName = input.HandlerName;
            }
            else
            {
                alarm.HandlerName = _userManager.RealName;
            }

            if (input.RecoverTime != null)
            {
                alarm.RecoverTime = input.RecoverTime;
            }
            else if (input.HandleStatus >= 1 && alarm.RecoverTime == null)
            {
                // 已恢复/已处理且未记录恢复时间时，默认取当前时间
                alarm.RecoverTime = DateTime.Now;
            }

            if (!input.Remark.IsNullOrEmpty())
            {
                alarm.Remark = input.Remark;
            }

            var isOk = await _alarmRepository.AsSugarClient().Updateable(alarm)
                .IgnoreColumns(ignoreAllNullColumns: true)
                .CallEntityMethod(m => m.LastModify())
                .ExecuteCommandAsync();
            if (!(isOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1001);
            }
        }
    }
}
