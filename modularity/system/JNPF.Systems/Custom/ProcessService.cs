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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JNPF.Systems.Custom
{
    /// <summary>
    /// 工序管理（技术管理）.
    /// 复用 sx_base_process 基础字段，扩展三大子功能：
    /// ①检查内容定义（项目/标准/数据类型/下拉选项/默认值/必填）；
    /// ②工序可用设备绑定（排产时按工序自动带出可选设备）；
    /// ③SOP 作业指导文件（图片/PDF，PDA 报工可查看）。
    /// </summary>
    [ApiDescriptionSettings(Tag = "Process", Name = "", Order = 205)]
    [Route("api/[controller]")]
    public class ProcessService : IProcessService, IDynamicApiController, ITransient
    {
        /// <summary>
        /// 基础工序仓储（sx_base_process）.
        /// </summary>
        private readonly ISqlSugarRepository<BaseProcessEntity> _baseProcessRepository;

        /// <summary>
        /// 工序检查内容定义仓储.
        /// </summary>
        private readonly ISqlSugarRepository<ProcessCheckItemEntity> _checkItemRepository;

        /// <summary>
        /// 工序设备绑定仓储.
        /// </summary>
        private readonly ISqlSugarRepository<ProcessDeviceEntity> _processDeviceRepository;

        /// <summary>
        /// 工序SOP文件仓储.
        /// </summary>
        private readonly ISqlSugarRepository<ProcessSopEntity> _sopRepository;

        /// <summary>
        /// 设备台账仓储.
        /// </summary>
        private readonly ISqlSugarRepository<DeviceMasterEntity> _deviceRepository;

        /// <summary>
        /// 设备类型仓储.
        /// </summary>
        private readonly ISqlSugarRepository<DeviceTypeMasterEntity> _deviceTypeRepository;

        /// <summary>
        /// 初始化一个<see cref="ProcessService"/>类型的新实例.
        /// </summary>
        public ProcessService(
            ISqlSugarRepository<BaseProcessEntity> baseProcessRepository,
            ISqlSugarRepository<ProcessCheckItemEntity> checkItemRepository,
            ISqlSugarRepository<ProcessDeviceEntity> processDeviceRepository,
            ISqlSugarRepository<ProcessSopEntity> sopRepository,
            ISqlSugarRepository<DeviceMasterEntity> deviceRepository,
            ISqlSugarRepository<DeviceTypeMasterEntity> deviceTypeRepository)
        {
            _baseProcessRepository = baseProcessRepository;
            _checkItemRepository = checkItemRepository;
            _processDeviceRepository = processDeviceRepository;
            _sopRepository = sopRepository;
            _deviceRepository = deviceRepository;
            _deviceTypeRepository = deviceTypeRepository;
        }

        /// <summary>
        /// 获取工序列表（分页，含检查项目数/关联设备数/是否有作业指导）.
        /// </summary>
        /// <param name="input">查询条件.</param>
        /// <returns>分页列表.</returns>
        [HttpPost("GetProcessList")]
        public async Task<dynamic> GetProcessList([FromBody] ProcessQuery input)
        {
            var whereLambda = LinqExpression.And<BaseProcessEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null);

            if (!string.IsNullOrEmpty(input.ProcessCode))
            {
                whereLambda = whereLambda.And(m => m.ProcessCode.Contains(input.ProcessCode));
            }

            if (!string.IsNullOrEmpty(input.ProcessName))
            {
                whereLambda = whereLambda.And(m => m.ProcessName.Contains(input.ProcessName));
            }

            // 有无检查项筛选
            if (input.HasCheckItem != -1)
            {
                var hasItemProcessIds = await _checkItemRepository.AsQueryable()
                    .Where(c => c.DeleteMark == null)
                    .Select(c => c.ProcessId)
                    .Distinct()
                    .ToListAsync();
                if (input.HasCheckItem == 1)
                {
                    whereLambda = whereLambda.And(m => hasItemProcessIds.Contains(m.Id));
                }
                else
                {
                    whereLambda = whereLambda.And(m => !hasItemProcessIds.Contains(m.Id));
                }
            }

            var page = await _baseProcessRepository.AsQueryable()
                .Where(whereLambda)
                .OrderBy(it => it.CreatorTime, OrderByType.Desc)
                .ToPagedListAsync(input.currentPage, input.pageSize);

            // 统计当前页工序的检查项数/设备数/是否有SOP
            var ids = page.list.Select(x => x.Id).ToList();
            var checkCountDict = new Dictionary<string, int>();
            var deviceCountDict = new Dictionary<string, int>();
            var sopProcessIds = new List<string>();
            if (ids.Any())
            {
                var checkRows = await _checkItemRepository.AsQueryable()
                    .Where(c => c.DeleteMark == null && ids.Contains(c.ProcessId))
                    .Select(c => c.ProcessId)
                    .ToListAsync();
                checkCountDict = checkRows.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());

                var deviceRows = await _processDeviceRepository.AsQueryable()
                    .Where(d => d.DeleteMark == null && ids.Contains(d.ProcessId))
                    .Select(d => d.ProcessId)
                    .ToListAsync();
                deviceCountDict = deviceRows.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());

                sopProcessIds = await _sopRepository.AsQueryable()
                    .Where(s => s.DeleteMark == null && ids.Contains(s.ProcessId))
                    .Select(s => s.ProcessId)
                    .Distinct()
                    .ToListAsync();
            }

            var dtos = page.list.Select(m => new ProcessListDto
            {
                Id = m.Id,
                ProcessCode = m.ProcessCode,
                ProcessName = m.ProcessName,
                ProcessType = m.ProcessType,
                CheckItemCount = checkCountDict.TryGetValue(m.Id, out var cc) ? cc : 0,
                DeviceCount = deviceCountDict.TryGetValue(m.Id, out var dc) ? dc : 0,
                HasSop = sopProcessIds.Contains(m.Id) ? 1 : 0,
                EnabledMark = m.EnabledMark,
                SortCode = m.SortCode,
                Remark = m.Remark,
                CreatorTime = m.CreatorTime
            }).ToList();

            return new SqlSugarPagedList<ProcessListDto>
            {
                list = dtos,
                pagination = page.pagination
            };
        }

        /// <summary>
        /// 获取工序详情.
        /// </summary>
        /// <param name="id">工序主键.</param>
        /// <returns>工序详情.</returns>
        [HttpGet("GetProcessById")]
        public async Task<dynamic> GetProcessById(string id)
        {
            if (id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var entity = await _baseProcessRepository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
            if (entity == null)
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            return entity;
        }

        /// <summary>
        /// 新增/编辑工序（复用 sx_base_process）.
        /// </summary>
        /// <param name="input">工序信息.</param>
        /// <returns>无.</returns>
        [HttpPost("CruProcess")]
        public async Task CruProcess([FromBody] ProcessCruInput input)
        {
            if (input.ProcessCode.IsNullOrEmpty() || input.ProcessName.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1018, "工序编号和工序名称不能为空");
            }

            int isOk;
            if (!string.IsNullOrEmpty(input.Id))
            {
                var existing = await _baseProcessRepository.GetSingleAsync(m => m.Id == input.Id && m.DeleteMark == null);
                if (existing == null)
                {
                    throw Oops.Oh(ErrorCode.COM1007);
                }

                // 工序编号唯一性校验（排除自身）
                if (await _baseProcessRepository.IsAnyAsync(p => p.ProcessCode == input.ProcessCode && p.Id != input.Id && p.DeleteMark == null))
                {
                    throw Oops.Oh(ErrorCode.COM1004);
                }

                existing = input.Adapt(existing);
                isOk = await _baseProcessRepository.AsSugarClient().Updateable(existing)
                    .IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
            }
            else
            {
                // 工序编号唯一性校验
                if (await _baseProcessRepository.IsAnyAsync(p => p.ProcessCode == input.ProcessCode && p.DeleteMark == null))
                {
                    throw Oops.Oh(ErrorCode.COM1004);
                }

                var entity = input.Adapt<BaseProcessEntity>();
                entity.EnabledMark = input.EnabledMark ?? 1;
                isOk = await _baseProcessRepository.AsSugarClient().Insertable(entity)
                    .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            }

            if (!(isOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1000);
            }
        }

        /// <summary>
        /// 删除工序（级联软删除检查项/设备绑定/SOP）.
        /// </summary>
        /// <param name="id">工序主键.</param>
        /// <returns>无.</returns>
        [HttpGet("DeleteProcess")]
        public async Task DeleteProcess(string id)
        {
            if (id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var entity = await _baseProcessRepository.GetSingleAsync(p => p.Id == id && p.DeleteMark == null);
            if (entity == null)
            {
                throw Oops.Oh(ErrorCode.COM1002);
            }

            int isOk = await _baseProcessRepository.AsSugarClient().Updateable(entity)
                .CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
            if (!(isOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1002);
            }

            // 级联软删除检查项
            var checkItems = await _checkItemRepository.AsQueryable()
                .Where(m => m.ProcessId == id && m.DeleteMark == null).ToListAsync();
            if (checkItems.Any())
            {
                await _checkItemRepository.AsSugarClient().Updateable(checkItems)
                    .CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
            }

            // 级联软删除设备绑定
            var devices = await _processDeviceRepository.AsQueryable()
                .Where(m => m.ProcessId == id && m.DeleteMark == null).ToListAsync();
            if (devices.Any())
            {
                await _processDeviceRepository.AsSugarClient().Updateable(devices)
                    .CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
            }

            // 级联软删除SOP文件记录
            var sops = await _sopRepository.AsQueryable()
                .Where(m => m.ProcessId == id && m.DeleteMark == null).ToListAsync();
            if (sops.Any())
            {
                await _sopRepository.AsSugarClient().Updateable(sops)
                    .CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// 获取工序检查内容定义.
        /// </summary>
        /// <param name="processId">工序主键.</param>
        /// <returns>检查项列表.</returns>
        [HttpGet("GetCheckItems")]
        public async Task<dynamic> GetCheckItems(string processId)
        {
            if (processId.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            return await _checkItemRepository.AsQueryable()
                .Where(m => m.ProcessId == processId && m.DeleteMark == null)
                .OrderBy(m => m.SeqNo, OrderByType.Asc)
                .ToListAsync();
        }

        /// <summary>
        /// 保存工序检查内容定义（整体覆盖）.
        /// </summary>
        /// <param name="input">检查项集合.</param>
        /// <returns>无.</returns>
        [HttpPost("SaveCheckItems")]
        public async Task SaveCheckItems([FromBody] ProcessCheckItemSaveInput input)
        {
            if (input.ProcessId.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var process = await _baseProcessRepository.GetFirstAsync(m => m.Id == input.ProcessId && m.DeleteMark == null);
            if (process == null)
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            if (input.Items == null)
            {
                input.Items = new List<ProcessCheckItemDto>();
            }

            // 检查项目名称非空校验
            var emptyItem = input.Items.Any(m => m.CheckItem.IsNullOrEmpty());
            if (emptyItem)
            {
                throw Oops.Oh(ErrorCode.COM1018, "检查项目名称不能为空");
            }

            // 旧项整体软删除
            var oldItems = await _checkItemRepository.AsQueryable()
                .Where(m => m.ProcessId == input.ProcessId && m.DeleteMark == null).ToListAsync();
            if (oldItems.Any())
            {
                await _checkItemRepository.AsSugarClient().Updateable(oldItems)
                    .CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
            }

            if (!input.Items.Any())
            {
                return;
            }

            var entities = new List<ProcessCheckItemEntity>();
            var seqNo = 1;
            foreach (var item in input.Items)
            {
                entities.Add(new ProcessCheckItemEntity
                {
                    ProcessId = input.ProcessId,
                    ProcessCode = process.ProcessCode,
                    ProcessName = process.ProcessName,
                    CheckItem = item.CheckItem,
                    CheckStandard = item.CheckStandard,
                    DataType = item.DataType ?? 2,
                    OptionItems = item.OptionItems,
                    DefaultValue = item.DefaultValue,
                    Required = item.Required ?? 0,
                    SeqNo = seqNo++,
                    Remark = item.Remark
                });
            }

            await _checkItemRepository.AsSugarClient().Insertable(entities)
                .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        }

        /// <summary>
        /// 获取工序设备绑定列表（含设备类型/状态）.
        /// </summary>
        /// <param name="processId">工序主键.</param>
        /// <returns>绑定设备列表.</returns>
        [HttpGet("GetProcessDevices")]
        public async Task<dynamic> GetProcessDevices(string processId)
        {
            if (processId.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var binds = await _processDeviceRepository.AsQueryable()
                .Where(m => m.ProcessId == processId && m.DeleteMark == null)
                .OrderBy(m => m.CreatorTime, OrderByType.Asc)
                .ToListAsync();

            var deviceIds = binds.Select(m => m.DeviceId).Distinct().ToList();
            var devices = new List<DeviceMasterEntity>();
            if (deviceIds.Any())
            {
                devices = await _deviceRepository.AsQueryable()
                    .Where(m => deviceIds.Contains(m.Id) && m.DeleteMark == null).ToListAsync();
            }

            var typeIds = devices.Select(m => m.DeviceTypeId).Where(m => !m.IsNullOrEmpty()).Distinct().ToList();
            var types = new List<DeviceTypeMasterEntity>();
            if (typeIds.Any())
            {
                types = await _deviceTypeRepository.AsQueryable()
                    .Where(m => typeIds.Contains(m.Id) && m.DeleteMark == null).ToListAsync();
            }

            return binds.Select(m =>
            {
                var device = devices.FirstOrDefault(d => d.Id == m.DeviceId);
                var deviceType = device == null ? null : types.FirstOrDefault(t => t.Id == device.DeviceTypeId);
                return new
                {
                    id = m.Id,
                    deviceId = m.DeviceId,
                    deviceCode = m.DeviceCode,
                    deviceName = m.DeviceName,
                    deviceTypeName = deviceType?.DevicetypeName,
                    deviceState = device?.Device_State,
                    deviceStatus = device?.DeviceStatus
                };
            }).ToList();
        }

        /// <summary>
        /// 保存工序设备绑定（整体覆盖）.
        /// </summary>
        /// <param name="input">设备绑定集合.</param>
        /// <returns>无.</returns>
        [HttpPost("SaveProcessDevices")]
        public async Task SaveProcessDevices([FromBody] ProcessDeviceSaveInput input)
        {
            if (input.ProcessId.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var process = await _baseProcessRepository.GetFirstAsync(m => m.Id == input.ProcessId && m.DeleteMark == null);
            if (process == null)
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            if (input.DeviceIds == null)
            {
                input.DeviceIds = new List<string>();
            }

            // 旧绑定整体软删除
            var oldBinds = await _processDeviceRepository.AsQueryable()
                .Where(m => m.ProcessId == input.ProcessId && m.DeleteMark == null).ToListAsync();
            if (oldBinds.Any())
            {
                await _processDeviceRepository.AsSugarClient().Updateable(oldBinds)
                    .CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
            }

            if (!input.DeviceIds.Any())
            {
                return;
            }

            // 回填设备编码/名称
            var deviceIds = input.DeviceIds.Distinct().ToList();
            var devices = await _deviceRepository.AsQueryable()
                .Where(m => deviceIds.Contains(m.Id) && m.DeleteMark == null).ToListAsync();

            var entities = new List<ProcessDeviceEntity>();
            foreach (var deviceId in deviceIds)
            {
                var device = devices.FirstOrDefault(m => m.Id == deviceId);
                entities.Add(new ProcessDeviceEntity
                {
                    ProcessId = input.ProcessId,
                    ProcessCode = process.ProcessCode,
                    ProcessName = process.ProcessName,
                    DeviceId = deviceId,
                    DeviceCode = device?.DeviceNo,
                    DeviceName = device?.DeviceName
                });
            }

            await _processDeviceRepository.AsSugarClient().Insertable(entities)
                .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        }

        /// <summary>
        /// 获取工序SOP作业指导文件列表.
        /// </summary>
        /// <param name="processId">工序主键.</param>
        /// <returns>SOP文件列表.</returns>
        [HttpGet("GetSopFiles")]
        public async Task<dynamic> GetSopFiles(string processId)
        {
            if (processId.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            return await _sopRepository.AsQueryable()
                .Where(m => m.ProcessId == processId && m.DeleteMark == null)
                .OrderBy(m => m.CreatorTime, OrderByType.Desc)
                .ToListAsync();
        }

        /// <summary>
        /// 新增工序SOP文件记录（上传后登记）.
        /// </summary>
        /// <param name="input">SOP文件信息.</param>
        /// <returns>无.</returns>
        [HttpPost("AddSopFile")]
        public async Task AddSopFile([FromBody] ProcessSopAddInput input)
        {
            if (input.ProcessId.IsNullOrEmpty() || input.FileName.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1018, "工序和文件名不能为空");
            }

            var process = await _baseProcessRepository.GetFirstAsync(m => m.Id == input.ProcessId && m.DeleteMark == null);
            if (process == null)
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var entity = input.Adapt<ProcessSopEntity>();
            entity.ProcessCode = process.ProcessCode;
            entity.ProcessName = process.ProcessName;
            int isOk = await _sopRepository.AsSugarClient().Insertable(entity)
                .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            if (!(isOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1000);
            }
        }

        /// <summary>
        /// 删除工序SOP文件记录（软删除）.
        /// </summary>
        /// <param name="id">SOP记录主键.</param>
        /// <returns>无.</returns>
        [HttpGet("DeleteSopFile")]
        public async Task DeleteSopFile(string id)
        {
            if (id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var entity = await _sopRepository.GetSingleAsync(p => p.Id == id && p.DeleteMark == null);
            if (entity == null)
            {
                throw Oops.Oh(ErrorCode.COM1002);
            }

            int isOk = await _sopRepository.AsSugarClient().Updateable(entity)
                .CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
            if (!(isOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1002);
            }
        }

        /// <summary>
        /// 工序下拉（供工艺路线选择工序）.
        /// </summary>
        /// <param name="keyword">编号/名称关键字.</param>
        /// <returns>工序选择项.</returns>
        [HttpGet("GetProcessSelected")]
        public async Task<dynamic> GetProcessSelected(string keyword)
        {
            var query = _baseProcessRepository.AsQueryable()
                .Where(m => m.DeleteMark == null && m.EnabledMark == 1);

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(m => m.ProcessCode.Contains(keyword)
                    || m.ProcessName.Contains(keyword));
            }

            return await query
                .OrderBy(m => m.SortCode, OrderByType.Asc)
                .Take(100)
                .Select(m => new ProcessSelectedOutput
                {
                    id = m.Id,
                    code = m.ProcessCode,
                    name = m.ProcessName,
                    processType = m.ProcessType,
                    workLocation = m.WorkLocation
                })
                .ToListAsync();
        }
    }
}
