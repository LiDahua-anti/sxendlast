using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.LinqBuilder;
using JNPF.Systems.Entitys.Dto.Custom;
using JNPF.Systems.Entitys.Entity.Custom;
using JNPF.Systems.Interfaces.Custom;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JNPF.Systems.Custom
{
    /// <summary>
    /// 清洗合并码（版本化配置）.
    /// 工序「清洗」由多名员工在同一清洗工位同时工作、合并为一次报工；
    /// 在此维护清洗工位的人员配置（默认4人）、报工人数与绩效均分规则；
    /// 配置变更即升版本（V1→V2→V3），旧版本写失效日期并标记已失效，永久保留可存档追溯；
    /// 历史报工记录仍按变更前的工位人员存档（报表侧按 member_snapshot 追溯）。
    /// </summary>
    [ApiDescriptionSettings(Tag = "CleanMerge", Name = "", Order = 210)]
    [Route("api/[controller]")]
    public class CleanMergeService : ICleanMergeService, IDynamicApiController, ITransient
    {
        /// <summary>
        /// 清洗合并码配置仓储.
        /// </summary>
        private readonly ISqlSugarRepository<CleanMergeConfigEntity> _configRepository;

        /// <summary>
        /// 清洗合并码成员仓储.
        /// </summary>
        private readonly ISqlSugarRepository<CleanMergeMemberEntity> _memberRepository;

        /// <summary>
        /// 报工明细仓储（统计关联报工批次）.
        /// </summary>
        private readonly ISqlSugarRepository<WorkReportEntity> _workReportRepository;

        /// <summary>
        /// 基础工序仓储.
        /// </summary>
        private readonly ISqlSugarRepository<BaseProcessEntity> _baseProcessRepository;

        /// <summary>
        /// 初始化一个<see cref="CleanMergeService"/>类型的新实例.
        /// </summary>
        public CleanMergeService(
            ISqlSugarRepository<CleanMergeConfigEntity> configRepository,
            ISqlSugarRepository<CleanMergeMemberEntity> memberRepository,
            ISqlSugarRepository<WorkReportEntity> workReportRepository,
            ISqlSugarRepository<BaseProcessEntity> baseProcessRepository)
        {
            _configRepository = configRepository;
            _memberRepository = memberRepository;
            _workReportRepository = workReportRepository;
            _baseProcessRepository = baseProcessRepository;
        }

        #region 当前生效配置

        /// <summary>
        /// 获取当前生效配置（工序：清洗，含清洗工位成员）.
        /// </summary>
        /// <param name="processId">工序ID（可空，空=取任一生效配置）.</param>
        /// <returns>当前生效配置（无配置时返回默认模板：4人）.</returns>
        [HttpGet("GetCurrentConfig")]
        public async Task<dynamic> GetCurrentConfig(string processId)
        {
            var query = _configRepository.AsQueryable()
                .Where(m => m.Status == 1 && m.DeleteMark == null);
            if (!processId.IsNullOrEmpty())
            {
                query = query.Where(m => m.ProcessId == processId);
            }

            var config = await query.OrderBy(m => m.EffectDate, OrderByType.Desc).FirstAsync();
            if (config == null)
            {
                // 无生效配置：返回默认模板（报工人数默认4人）
                string processName = null;
                if (!processId.IsNullOrEmpty())
                {
                    var bp = await _baseProcessRepository.GetFirstAsync(m => m.Id == processId && m.DeleteMark == null);
                    processName = bp?.ProcessName;
                }

                return new CleanMergeCurrentOutput
                {
                    ConfigId = null,
                    ProcessId = processId,
                    ProcessName = processName,
                    Version = null,
                    MemberCount = 4,
                    EffectDate = null,
                    Status = null,
                    Members = new List<CleanMergeMemberOutput>()
                };
            }

            var members = await _memberRepository.AsQueryable()
                .Where(m => m.ConfigId == config.Id && m.DeleteMark == null)
                .OrderBy(m => m.CreatorTime, OrderByType.Asc)
                .ToListAsync();

            return new CleanMergeCurrentOutput
            {
                ConfigId = config.Id,
                ProcessId = config.ProcessId,
                ProcessName = config.ProcessName,
                Version = config.Version,
                MemberCount = config.MemberCount,
                EffectDate = config.EffectDate,
                Status = config.Status,
                Members = members.Select(m => new CleanMergeMemberOutput
                {
                    EmployeeNo = m.EmployeeNo,
                    EmployeeName = m.EmployeeName
                }).ToList()
            };
        }

        #endregion

        #region 配置保存（编辑即升版本）与历史存档

        /// <summary>
        /// 保存配置（编辑即升版本：旧版本写失效日期并标记已失效，永久保留可存档追溯）.
        /// </summary>
        /// <param name="input">配置入参.</param>
        /// <returns>新版本信息.</returns>
        [HttpPost("SaveConfig")]
        public async Task<dynamic> SaveConfig([FromBody] CleanMergeSaveInput input)
        {
            if (input.ProcessId.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1018, "请选择所属工序");
            }

            if (input.MemberCount <= 0)
            {
                input.MemberCount = input.Members != null && input.Members.Any() ? input.Members.Count : 4;
            }

            var effectDate = input.EffectDate ?? DateTime.Now.Date;

            // 工序名称回填
            var processName = input.ProcessName;
            if (processName.IsNullOrEmpty())
            {
                var bp = await _baseProcessRepository.GetFirstAsync(m => m.Id == input.ProcessId && m.DeleteMark == null);
                processName = bp?.ProcessName;
            }

            // 既有版本（含已失效，用于版本号递增与归档）
            var existing = await _configRepository.AsQueryable()
                .Where(m => m.ProcessId == input.ProcessId && m.DeleteMark == null).ToListAsync();
            var current = existing.Where(m => m.Status == 1).OrderByDescending(m => m.EffectDate).FirstOrDefault();

            // 归档当前生效版本：写失效日期（新版本生效前一日）+ 状态「已失效」；成员记录与历史报工永久保留
            if (current != null)
            {
                current.Status = 0;
                current.ExpireDate = effectDate.AddDays(-1);
                await _configRepository.AsSugarClient().Updateable(current)
                    .CallEntityMethod(m => m.LastModify())
                    .UpdateColumns(it => new { it.Status, it.ExpireDate, it.LastModifyTime, it.LastModifyUserId })
                    .ExecuteCommandAsync();
            }

            // 新版本（版本号递增：V1→V2→V3）
            var version = "V" + (existing.Count + 1);
            var newConfig = new CleanMergeConfigEntity
            {
                ProcessId = input.ProcessId,
                ProcessName = processName,
                Version = version,
                MemberCount = input.MemberCount,
                EffectDate = effectDate,
                ExpireDate = null,
                Status = 1,
                Remark = input.Remark
            };

            var insertOk = await _configRepository.AsSugarClient().Insertable(newConfig)
                .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            if (!(insertOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1000);
            }

            // 新版本成员（历史版本成员不动，永久保留可追溯）
            if (input.Members != null && input.Members.Any())
            {
                var members = input.Members
                    .Where(m => !m.EmployeeNo.IsNullOrEmpty() || !m.EmployeeName.IsNullOrEmpty())
                    .Select(m => new CleanMergeMemberEntity
                    {
                        ConfigId = newConfig.Id,
                        EmployeeNo = m.EmployeeNo,
                        EmployeeName = m.EmployeeName
                    }).ToList();
                if (members.Any())
                {
                    await _memberRepository.AsSugarClient().Insertable(members)
                        .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                }
            }

            return new
            {
                version,
                configId = newConfig.Id,
                archivedVersion = current?.Version,
                message = current != null
                    ? $"配置已保存为{version}，原配置{current.Version}已归档存档"
                    : $"配置已保存为{version}"
            };
        }

        /// <summary>
        /// 历史配置存档（版本/成员/生效失效日期/关联报工批次）.
        /// </summary>
        /// <param name="input">查询条件.</param>
        /// <returns>分页列表.</returns>
        [HttpPost("GetHistory")]
        public async Task<dynamic> GetHistory([FromBody] CleanMergeHistoryQuery input)
        {
            var whereLambda = LinqExpression.And<CleanMergeConfigEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null);

            if (!string.IsNullOrEmpty(input.ProcessId))
            {
                whereLambda = whereLambda.And(m => m.ProcessId == input.ProcessId);
            }

            var page = await _configRepository.AsQueryable()
                .Where(whereLambda)
                .OrderBy(it => it.EffectDate, OrderByType.Desc)
                .OrderBy(it => it.CreatorTime, OrderByType.Desc)
                .ToPagedListAsync(input.currentPage, input.pageSize);

            var configIds = page.list.Select(m => m.Id).ToList();
            var members = configIds.Any()
                ? await _memberRepository.AsQueryable()
                    .Where(m => configIds.Contains(m.ConfigId) && m.DeleteMark == null).ToListAsync()
                : new List<CleanMergeMemberEntity>();

            // 关联报工批次：按版本统计清洗报工的不同批次号（group_no）数量
            var reports = configIds.Any()
                ? await _workReportRepository.AsQueryable()
                    .Where(m => configIds.Contains(m.MergeConfigId) && m.DeleteMark == null).ToListAsync()
                : new List<WorkReportEntity>();

            var dtos = page.list.Select(m => new CleanMergeHistoryDto
            {
                Id = m.Id,
                ProcessId = m.ProcessId,
                ProcessName = m.ProcessName,
                Version = m.Version,
                MemberCount = m.MemberCount,
                MemberNames = string.Join("/", members.Where(x => x.ConfigId == m.Id).Select(x => x.EmployeeName).Where(x => !x.IsNullOrEmpty())),
                EffectDate = m.EffectDate,
                ExpireDate = m.ExpireDate,
                Status = m.Status,
                ReportBatchCount = reports.Where(r => r.MergeConfigId == m.Id && r.Status == 1).Select(r => r.GroupNo).Distinct().Count()
            }).ToList();

            return new SqlSugarPagedList<CleanMergeHistoryDto>
            {
                list = dtos,
                pagination = page.pagination
            };
        }

        /// <summary>
        /// 查看指定版本配置成员（历史存档查看）.
        /// </summary>
        /// <param name="id">配置主键.</param>
        /// <returns>版本配置与成员.</returns>
        [HttpGet("GetConfigMembers")]
        public async Task<dynamic> GetConfigMembers(string id)
        {
            if (id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var config = await _configRepository.GetFirstAsync(m => m.Id == id && m.DeleteMark == null);
            if (config == null)
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var members = await _memberRepository.AsQueryable()
                .Where(m => m.ConfigId == config.Id && m.DeleteMark == null)
                .OrderBy(m => m.CreatorTime, OrderByType.Asc)
                .ToListAsync();

            return new CleanMergeCurrentOutput
            {
                ConfigId = config.Id,
                ProcessId = config.ProcessId,
                ProcessName = config.ProcessName,
                Version = config.Version,
                MemberCount = config.MemberCount,
                EffectDate = config.EffectDate,
                Status = config.Status,
                Members = members.Select(m => new CleanMergeMemberOutput
                {
                    EmployeeNo = m.EmployeeNo,
                    EmployeeName = m.EmployeeName
                }).ToList()
            };
        }

        #endregion
    }
}
