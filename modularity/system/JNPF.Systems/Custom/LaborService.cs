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
    /// 人工管理（班组/员工技能/人效考核）.
    /// 班组复用既有班次表 sx_base_class（含是否跨日：夜班跨越零点的班次归属次日，用于报工工时归属计算）；
    /// 员工技能档案含技能等级（L1-L3）与认证方式（内/外）；
    /// 人效考核按月从报工记录汇总生成（合格产量=Σ报工良品；不良率=Σ不良/Σ报工；工时利用率=有效工时/出勤工时）。
    /// </summary>
    [ApiDescriptionSettings(Tag = "Labor", Name = "", Order = 203)]
    [Route("api/[controller]")]
    public class LaborService : ILaborService, IDynamicApiController, ITransient
    {
        /// <summary>
        /// 班组（班次）仓储.
        /// </summary>
        private readonly ISqlSugarRepository<BaseClassEntity> _baseClassRepository;

        /// <summary>
        /// 员工技能仓储.
        /// </summary>
        private readonly ISqlSugarRepository<EmployeeSkillEntity> _employeeSkillRepository;

        /// <summary>
        /// 人效考核仓储.
        /// </summary>
        private readonly ISqlSugarRepository<EfficiencyEntity> _efficiencyRepository;

        /// <summary>
        /// 报工明细仓储（人效统计数据来源）.
        /// </summary>
        private readonly ISqlSugarRepository<WorkReportEntity> _workReportRepository;

        /// <summary>
        /// 每日标准工时（小时），用于工时利用率分母计算.
        /// </summary>
        private const decimal StandardHoursPerDay = 8m;

        /// <summary>
        /// 月产量基准（件），用于绩效得分产量项满分基准.
        /// </summary>
        private const decimal MonthlyQtyBase = 500m;

        /// <summary>
        /// 初始化一个<see cref="LaborService"/>类型的新实例.
        /// </summary>
        public LaborService(
            ISqlSugarRepository<BaseClassEntity> baseClassRepository,
            ISqlSugarRepository<EmployeeSkillEntity> employeeSkillRepository,
            ISqlSugarRepository<EfficiencyEntity> efficiencyRepository,
            ISqlSugarRepository<WorkReportEntity> workReportRepository)
        {
            _baseClassRepository = baseClassRepository;
            _employeeSkillRepository = employeeSkillRepository;
            _efficiencyRepository = efficiencyRepository;
            _workReportRepository = workReportRepository;
        }

        #region 班组管理

        /// <summary>
        /// 获取班组列表（分页）.
        /// </summary>
        /// <param name="input">查询条件.</param>
        /// <returns>分页列表.</returns>
        [HttpPost("GetTeamList")]
        public async Task<dynamic> GetTeamList([FromBody] TeamQuery input)
        {
            var whereLambda = LinqExpression.And<BaseClassEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null);

            if (!string.IsNullOrEmpty(input.ClassName))
            {
                whereLambda = whereLambda.And(m => m.ClassName.Contains(input.ClassName));
            }

            if (!string.IsNullOrEmpty(input.ClassType))
            {
                whereLambda = whereLambda.And(m => m.Class_type == input.ClassType);
            }

            return await _baseClassRepository.AsQueryable()
                .Where(whereLambda)
                .OrderBy(m => m.SortCode)
                .OrderBy(it => it.CreatorTime, OrderByType.Desc)
                .Select(m => new TeamListDto
                {
                    Id = m.Id,
                    ClassName = m.ClassName,
                    ClassType = m.Class_type,
                    ClassTime = m.Class_time,
                    EndTime = m.End_time,
                    CrossDay = m.CrossDay,
                    EnabledMark = m.EnabledMark,
                    SortCode = m.SortCode,
                    Remark = m.Remark,
                    CreatorTime = m.CreatorTime
                })
                .ToPagedListAsync(input.currentPage, input.pageSize);
        }

        /// <summary>
        /// 获取班组详情.
        /// </summary>
        /// <param name="id">班组主键.</param>
        /// <returns>班组详情.</returns>
        [HttpGet("GetTeamById")]
        public async Task<dynamic> GetTeamById(string id)
        {
            if (id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var entity = await _baseClassRepository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
            if (entity == null)
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            return new
            {
                id = entity.Id,
                className = entity.ClassName,
                classType = entity.Class_type,
                classTime = entity.Class_time,
                endTime = entity.End_time,
                crossDay = entity.CrossDay,
                enabledMark = entity.EnabledMark,
                sortCode = entity.SortCode,
                remark = entity.Remark
            };
        }

        /// <summary>
        /// 新增/编辑班组（含是否跨日）.
        /// </summary>
        /// <param name="input">班组信息.</param>
        /// <returns>无.</returns>
        [HttpPost("CruTeam")]
        public async Task CruTeam([FromBody] TeamCruInput input)
        {
            if (input.ClassName.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            int isOk;
            if (!string.IsNullOrEmpty(input.Id))
            {
                var existing = await _baseClassRepository.GetSingleAsync(m => m.Id == input.Id && m.DeleteMark == null);
                if (existing == null)
                {
                    throw Oops.Oh(ErrorCode.COM1007);
                }

                FillTeamEntity(existing, input);
                isOk = await _baseClassRepository.AsSugarClient().Updateable(existing)
                    .IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
            }
            else
            {
                var entity = new BaseClassEntity();
                FillTeamEntity(entity, input);
                isOk = await _baseClassRepository.AsSugarClient().Insertable(entity)
                    .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            }

            if (!(isOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1000);
            }
        }

        /// <summary>
        /// 删除班组（软删除）.
        /// </summary>
        /// <param name="id">班组主键.</param>
        /// <returns>无.</returns>
        [HttpGet("DeleteTeam")]
        public async Task DeleteTeam(string id)
        {
            if (id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var entity = await _baseClassRepository.GetSingleAsync(p => p.Id == id && p.DeleteMark == null);
            if (entity == null)
            {
                throw Oops.Oh(ErrorCode.COM1002);
            }

            int isOk = await _baseClassRepository.AsSugarClient().Updateable(entity)
                .CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
            if (!(isOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1002);
            }
        }

        /// <summary>
        /// 班组下拉（供排产/报工选择负责班组）.
        /// </summary>
        /// <returns>班组选择项.</returns>
        [HttpGet("GetTeamSelected")]
        public async Task<dynamic> GetTeamSelected()
        {
            return await _baseClassRepository.AsQueryable()
                .Where(m => m.DeleteMark == null && m.EnabledMark == 1)
                .OrderBy(m => m.SortCode)
                .Select(m => new
                {
                    id = m.Id,
                    name = m.ClassName,
                    classType = m.Class_type,
                    classTime = m.Class_time,
                    endTime = m.End_time,
                    crossDay = m.CrossDay
                })
                .ToListAsync();
        }

        /// <summary>
        /// 填充班组实体（Dto 属性名与既有实体下划线命名不一致，手动映射）.
        /// </summary>
        /// <param name="entity">班组实体.</param>
        /// <param name="input">班组入参.</param>
        private static void FillTeamEntity(BaseClassEntity entity, TeamCruInput input)
        {
            entity.ClassName = input.ClassName;
            entity.Class_type = input.ClassType;
            entity.Class_time = input.ClassTime;
            entity.End_time = input.EndTime;
            entity.CrossDay = input.CrossDay ?? 0;
            entity.EnabledMark = input.EnabledMark ?? 1;
            entity.SortCode = input.SortCode;
            entity.Remark = input.Remark;
        }

        #endregion

        #region 员工技能管理

        /// <summary>
        /// 获取员工技能列表（分页）.
        /// </summary>
        /// <param name="input">查询条件.</param>
        /// <returns>分页列表.</returns>
        [HttpPost("GetEmployeeSkillList")]
        public async Task<dynamic> GetEmployeeSkillList([FromBody] EmployeeSkillQuery input)
        {
            var whereLambda = LinqExpression.And<EmployeeSkillEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null);

            if (!string.IsNullOrEmpty(input.EmployeeNo))
            {
                whereLambda = whereLambda.And(m => m.EmployeeNo.Contains(input.EmployeeNo));
            }

            if (!string.IsNullOrEmpty(input.EmployeeName))
            {
                whereLambda = whereLambda.And(m => m.EmployeeName.Contains(input.EmployeeName));
            }

            if (!string.IsNullOrEmpty(input.SkillName))
            {
                whereLambda = whereLambda.And(m => m.SkillName.Contains(input.SkillName));
            }

            if (!string.IsNullOrEmpty(input.SkillLevel))
            {
                whereLambda = whereLambda.And(m => m.SkillLevel == input.SkillLevel);
            }

            if (input.Status != -1)
            {
                whereLambda = whereLambda.And(m => m.Status == input.Status);
            }

            return await _employeeSkillRepository.AsQueryable()
                .Where(whereLambda)
                .OrderBy(it => it.CreatorTime, OrderByType.Desc)
                .ToPagedListAsync(input.currentPage, input.pageSize);
        }

        /// <summary>
        /// 获取员工技能详情.
        /// </summary>
        /// <param name="id">技能主键.</param>
        /// <returns>技能详情.</returns>
        [HttpGet("GetEmployeeSkillById")]
        public async Task<dynamic> GetEmployeeSkillById(string id)
        {
            if (id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var entity = await _employeeSkillRepository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
            if (entity == null)
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            return entity;
        }

        /// <summary>
        /// 新增/编辑员工技能.
        /// </summary>
        /// <param name="input">技能信息.</param>
        /// <returns>无.</returns>
        [HttpPost("CruEmployeeSkill")]
        public async Task CruEmployeeSkill([FromBody] EmployeeSkillCruInput input)
        {
            if (input.EmployeeNo.IsNullOrEmpty() || input.SkillName.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            int isOk;
            if (!string.IsNullOrEmpty(input.Id))
            {
                var existing = await _employeeSkillRepository.GetSingleAsync(m => m.Id == input.Id && m.DeleteMark == null);
                if (existing == null)
                {
                    throw Oops.Oh(ErrorCode.COM1007);
                }

                // 同工号同技能唯一性校验（排除自身）
                if (await _employeeSkillRepository.IsAnyAsync(p => p.EmployeeNo == input.EmployeeNo
                    && p.SkillName == input.SkillName && p.Id != input.Id && p.DeleteMark == null))
                {
                    throw Oops.Oh(ErrorCode.COM1004);
                }

                existing = input.Adapt(existing);
                isOk = await _employeeSkillRepository.AsSugarClient().Updateable(existing)
                    .IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
            }
            else
            {
                // 同工号同技能唯一性校验
                if (await _employeeSkillRepository.IsAnyAsync(p => p.EmployeeNo == input.EmployeeNo
                    && p.SkillName == input.SkillName && p.DeleteMark == null))
                {
                    throw Oops.Oh(ErrorCode.COM1004);
                }

                var entity = input.Adapt<EmployeeSkillEntity>();
                entity.Status = input.Status ?? 1;
                isOk = await _employeeSkillRepository.AsSugarClient().Insertable(entity)
                    .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            }

            if (!(isOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1000);
            }
        }

        /// <summary>
        /// 删除员工技能（软删除）.
        /// </summary>
        /// <param name="id">技能主键.</param>
        /// <returns>无.</returns>
        [HttpGet("DeleteEmployeeSkill")]
        public async Task DeleteEmployeeSkill(string id)
        {
            if (id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var entity = await _employeeSkillRepository.GetSingleAsync(p => p.Id == id && p.DeleteMark == null);
            if (entity == null)
            {
                throw Oops.Oh(ErrorCode.COM1002);
            }

            int isOk = await _employeeSkillRepository.AsSugarClient().Updateable(entity)
                .CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
            if (!(isOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1002);
            }
        }

        /// <summary>
        /// 批量导入员工技能档案.
        /// </summary>
        /// <param name="input">导入行集合.</param>
        /// <returns>无.</returns>
        [HttpPost("ImportEmployeeSkill")]
        public async Task ImportEmployeeSkill([FromBody] EmployeeSkillImportInput input)
        {
            if (input.Items == null || !input.Items.Any())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var entities = new List<EmployeeSkillEntity>();
            foreach (var item in input.Items)
            {
                if (item.EmployeeNo.IsNullOrEmpty() || item.SkillName.IsNullOrEmpty())
                {
                    continue;
                }

                // 跳过与库内重复的同工号同技能记录
                if (await _employeeSkillRepository.IsAnyAsync(p => p.EmployeeNo == item.EmployeeNo
                    && p.SkillName == item.SkillName && p.DeleteMark == null))
                {
                    continue;
                }

                var entity = item.Adapt<EmployeeSkillEntity>();
                entity.Status = 1;
                entities.Add(entity);
            }

            if (!entities.Any())
            {
                throw Oops.Oh(ErrorCode.COM1018, "导入数据为空或全部与现有数据重复");
            }

            int isOk = await _employeeSkillRepository.AsSugarClient().Insertable(entities)
                .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            if (!(isOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1000);
            }
        }

        #endregion

        #region 人效考核

        /// <summary>
        /// 获取人效考核列表（分页）.
        /// </summary>
        /// <param name="input">查询条件.</param>
        /// <returns>分页列表.</returns>
        [HttpPost("GetEfficiencyList")]
        public async Task<dynamic> GetEfficiencyList([FromBody] EfficiencyQuery input)
        {
            var whereLambda = LinqExpression.And<EfficiencyEntity>();
            whereLambda = whereLambda.And(x => x.DeleteMark == null);

            if (!string.IsNullOrEmpty(input.EmployeeNo))
            {
                whereLambda = whereLambda.And(m => m.EmployeeNo.Contains(input.EmployeeNo));
            }

            if (!string.IsNullOrEmpty(input.EmployeeName))
            {
                whereLambda = whereLambda.And(m => m.EmployeeName.Contains(input.EmployeeName));
            }

            if (!string.IsNullOrEmpty(input.TeamName))
            {
                whereLambda = whereLambda.And(m => m.TeamName == input.TeamName);
            }

            if (!string.IsNullOrEmpty(input.Month))
            {
                whereLambda = whereLambda.And(m => m.Month == input.Month);
            }

            if (!string.IsNullOrEmpty(input.ScoreLevel))
            {
                whereLambda = whereLambda.And(m => m.ScoreLevel == input.ScoreLevel);
            }

            return await _efficiencyRepository.AsQueryable()
                .Where(whereLambda)
                .OrderBy(m => m.Month, OrderByType.Desc)
                .OrderBy(m => m.Score, OrderByType.Desc)
                .ToPagedListAsync(input.currentPage, input.pageSize);
        }

        /// <summary>
        /// 生成人效考核统计（从报工记录按月按人汇总：合格产量/不良率/工时利用率→绩效得分）.
        /// 规则：合格产量=Σ报工良品（多人报工均分后）；不良率=Σ不良/Σ报工；
        /// 出勤天数=报工日期去重；工时利用率=Σ工时/(出勤天数×8h)；
        /// 绩效得分=产量得分×40% + 质量得分×40% + 工时得分×20%（≥90为A，≥75为B，否则C）。
        /// 同一月份重复生成时先软删除旧记录再插入，保证幂等.
        /// </summary>
        /// <param name="input">生成入参（月份+可选班组）.</param>
        /// <returns>无.</returns>
        [HttpPost("GenerateEfficiency")]
        public async Task GenerateEfficiency([FromBody] EfficiencyGenerateInput input)
        {
            if (input.Month.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            if (!DateTime.TryParse(input.Month + "-01", out var monthStart))
            {
                throw Oops.Oh(ErrorCode.COM1018, "月份格式不正确，应为yyyy-MM");
            }

            var monthEnd = monthStart.AddMonths(1);

            // 查询该月有效报工记录（每条记录为均分后的个人记录）
            var reports = await _workReportRepository.AsQueryable()
                .Where(w => w.DeleteMark == null && w.Status == 1
                    && w.StartTime >= monthStart && w.StartTime < monthEnd)
                .ToListAsync();

            if (!reports.Any())
            {
                throw Oops.Oh(ErrorCode.COM1018, "该月份无有效报工记录，无法生成人效统计");
            }

            // 软删除该月旧记录（重新生成）
            var oldList = await _efficiencyRepository.GetListAsync(e => e.Month == input.Month && e.DeleteMark == null);
            if (oldList.Any())
            {
                await _efficiencyRepository.AsSugarClient().Updateable(oldList)
                    .CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
            }

            var entities = new List<EfficiencyEntity>();
            var groups = reports
                .Where(w => !string.IsNullOrEmpty(w.WorkerNo))
                .GroupBy(w => w.WorkerNo);

            foreach (var g in groups)
            {
                var goodSum = g.Sum(x => x.GoodQty ?? 0);
                var badSum = g.Sum(x => x.BadQty ?? 0);
                var hoursSum = g.Sum(x => x.WorkHours ?? 0);
                var attendDays = g.Where(x => x.StartTime != null)
                    .Select(x => x.StartTime.Value.Date).Distinct().Count();

                // 不良率(%)
                var totalQty = goodSum + badSum;
                var badRate = totalQty > 0 ? Math.Round(badSum / totalQty * 100, 4) : 0;

                // 工时利用率(%)，封顶100
                var standardHours = attendDays * StandardHoursPerDay;
                var hoursRate = standardHours > 0
                    ? Math.Round(Math.Min(hoursSum / standardHours * 100, 100), 2)
                    : 0;

                // 绩效得分：产量得分（月产量基准封顶100）×40% + 质量得分×40% + 工时得分×20%
                var qtyScore = Math.Min(goodSum / MonthlyQtyBase * 100, 100m);
                var qualityScore = Math.Max(100m - badRate, 0m);
                var score = Math.Round(qtyScore * 0.4m + qualityScore * 0.4m + hoursRate * 0.2m, 2);
                var level = score >= 90 ? "A" : score >= 75 ? "B" : "C";

                entities.Add(new EfficiencyEntity
                {
                    EmployeeNo = g.Key,
                    EmployeeName = g.Select(x => x.WorkerName).FirstOrDefault(x => !string.IsNullOrEmpty(x)),
                    TeamName = input.TeamName,
                    Month = input.Month,
                    AttendDays = attendDays,
                    QualifiedQty = (int)Math.Round(goodSum),
                    BadRate = badRate,
                    WorkHoursRate = hoursRate,
                    ScoreLevel = level,
                    Score = score
                });
            }

            if (!entities.Any())
            {
                throw Oops.Oh(ErrorCode.COM1018, "无有效的报工人信息，无法生成人效统计");
            }

            int isOk = await _efficiencyRepository.AsSugarClient().Insertable(entities)
                .CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            if (!(isOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1000);
            }
        }

        /// <summary>
        /// 删除人效考核记录（软删除）.
        /// </summary>
        /// <param name="id">记录主键.</param>
        /// <returns>无.</returns>
        [HttpGet("DeleteEfficiency")]
        public async Task DeleteEfficiency(string id)
        {
            if (id.IsNullOrEmpty())
            {
                throw Oops.Oh(ErrorCode.COM1007);
            }

            var entity = await _efficiencyRepository.GetSingleAsync(p => p.Id == id && p.DeleteMark == null);
            if (entity == null)
            {
                throw Oops.Oh(ErrorCode.COM1002);
            }

            int isOk = await _efficiencyRepository.AsSugarClient().Updateable(entity)
                .CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
            if (!(isOk > 0))
            {
                throw Oops.Oh(ErrorCode.COM1002);
            }
        }

        #endregion
    }
}
