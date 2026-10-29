using JNPF.Common.Filter;
using System;
using System.Collections.Generic;

namespace JNPF.Systems.Entitys.Dto.Custom;

#region 班组管理

/// <summary>
/// 班组（班次）列表查询条件.
/// </summary>
public class TeamQuery : PageInputBase
{
    /// <summary>
    /// 班组名称关键字.
    /// </summary>
    public string ClassName { get; set; }

    /// <summary>
    /// 班组类型（星勤/夜班/休息等）.
    /// </summary>
    public string ClassType { get; set; }
}

/// <summary>
/// 班组新增或编辑入参（复用 sx_base_class）.
/// </summary>
public class TeamCruInput
{
    /// <summary>
    /// 主键（为空时新增，非空时编辑）.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 班组名称.
    /// </summary>
    public string ClassName { get; set; }

    /// <summary>
    /// 班组类型（星勤/夜班/休息）.
    /// </summary>
    public string ClassType { get; set; }

    /// <summary>
    /// 开始时间（HH:mm）.
    /// </summary>
    public string ClassTime { get; set; }

    /// <summary>
    /// 结束时间（HH:mm）.
    /// </summary>
    public string EndTime { get; set; }

    /// <summary>
    /// 是否跨日：0否 1是（夜班跨越零点的班次归属次日）.
    /// </summary>
    public int? CrossDay { get; set; }

    /// <summary>
    /// 启用标识：0禁用 1启用.
    /// </summary>
    public int? EnabledMark { get; set; }

    /// <summary>
    /// 排序码.
    /// </summary>
    public long? SortCode { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }
}

/// <summary>
/// 班组列表输出（映射既有 sx_base_class 的下划线属性名）.
/// </summary>
public class TeamListDto
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 班组名称.
    /// </summary>
    public string ClassName { get; set; }

    /// <summary>
    /// 班组类型.
    /// </summary>
    public string ClassType { get; set; }

    /// <summary>
    /// 开始时间.
    /// </summary>
    public string ClassTime { get; set; }

    /// <summary>
    /// 结束时间.
    /// </summary>
    public string EndTime { get; set; }

    /// <summary>
    /// 是否跨日：0否 1是.
    /// </summary>
    public int? CrossDay { get; set; }

    /// <summary>
    /// 启用标识：0禁用 1启用.
    /// </summary>
    public int? EnabledMark { get; set; }

    /// <summary>
    /// 排序码.
    /// </summary>
    public long? SortCode { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    public DateTime? CreatorTime { get; set; }
}

#endregion

#region 员工技能管理

/// <summary>
/// 员工技能列表查询条件.
/// </summary>
public class EmployeeSkillQuery : PageInputBase
{
    /// <summary>
    /// 工号.
    /// </summary>
    public string EmployeeNo { get; set; }

    /// <summary>
    /// 姓名.
    /// </summary>
    public string EmployeeName { get; set; }

    /// <summary>
    /// 技能名称关键字.
    /// </summary>
    public string SkillName { get; set; }

    /// <summary>
    /// 技能等级：L1/L2/L3.
    /// </summary>
    public string SkillLevel { get; set; }

    /// <summary>
    /// 状态：1有效 0失效，-1为全部.
    /// </summary>
    public int Status { get; set; } = -1;
}

/// <summary>
/// 员工技能新增或编辑入参.
/// </summary>
public class EmployeeSkillCruInput
{
    /// <summary>
    /// 主键（为空时新增，非空时编辑）.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// 工号.
    /// </summary>
    public string EmployeeNo { get; set; }

    /// <summary>
    /// 姓名.
    /// </summary>
    public string EmployeeName { get; set; }

    /// <summary>
    /// 技能名称.
    /// </summary>
    public string SkillName { get; set; }

    /// <summary>
    /// 技能等级：L1/L2/L3.
    /// </summary>
    public string SkillLevel { get; set; }

    /// <summary>
    /// 认证方式：1内 2外.
    /// </summary>
    public int? CertType { get; set; }

    /// <summary>
    /// 证书编号.
    /// </summary>
    public string CertNo { get; set; }

    /// <summary>
    /// 发证单位.
    /// </summary>
    public string CertOrg { get; set; }

    /// <summary>
    /// 状态：1有效 0失效.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 关联工序ID.
    /// </summary>
    public string ProcessId { get; set; }

    /// <summary>
    /// 关联工序名称.
    /// </summary>
    public string ProcessName { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }
}

/// <summary>
/// 员工技能批量导入入参（按行导入技能档案）.
/// </summary>
public class EmployeeSkillImportInput
{
    /// <summary>
    /// 导入行集合.
    /// </summary>
    public List<EmployeeSkillImportItem> Items { get; set; } = new List<EmployeeSkillImportItem>();
}

/// <summary>
/// 员工技能导入单行.
/// </summary>
public class EmployeeSkillImportItem
{
    /// <summary>
    /// 工号.
    /// </summary>
    public string EmployeeNo { get; set; }

    /// <summary>
    /// 姓名.
    /// </summary>
    public string EmployeeName { get; set; }

    /// <summary>
    /// 技能名称.
    /// </summary>
    public string SkillName { get; set; }

    /// <summary>
    /// 技能等级：L1/L2/L3.
    /// </summary>
    public string SkillLevel { get; set; }

    /// <summary>
    /// 认证方式：1内 2外.
    /// </summary>
    public int? CertType { get; set; }

    /// <summary>
    /// 证书编号.
    /// </summary>
    public string CertNo { get; set; }

    /// <summary>
    /// 发证单位.
    /// </summary>
    public string CertOrg { get; set; }

    /// <summary>
    /// 关联工序ID.
    /// </summary>
    public string ProcessId { get; set; }

    /// <summary>
    /// 关联工序名称.
    /// </summary>
    public string ProcessName { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string Remark { get; set; }
}

#endregion

#region 人效考核

/// <summary>
/// 人效考核列表查询条件.
/// </summary>
public class EfficiencyQuery : PageInputBase
{
    /// <summary>
    /// 工号.
    /// </summary>
    public string EmployeeNo { get; set; }

    /// <summary>
    /// 姓名.
    /// </summary>
    public string EmployeeName { get; set; }

    /// <summary>
    /// 班组.
    /// </summary>
    public string TeamName { get; set; }

    /// <summary>
    /// 月份（如2026-09）.
    /// </summary>
    public string Month { get; set; }

    /// <summary>
    /// 绩效得分等级：A/B/C.
    /// </summary>
    public string ScoreLevel { get; set; }
}

/// <summary>
/// 人效考核统计生成入参（从报工记录按月汇总生成）.
/// </summary>
public class EfficiencyGenerateInput
{
    /// <summary>
    /// 生成月份（如2026-09）.
    /// </summary>
    public string Month { get; set; }

    /// <summary>
    /// 班组名称（可选：统一填入生成记录的班组，如"A班"）.
    /// </summary>
    public string TeamName { get; set; }
}

#endregion
