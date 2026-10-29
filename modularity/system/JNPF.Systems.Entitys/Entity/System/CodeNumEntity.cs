using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.System;

/// <summary>
/// 编码单据序号.
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2025-03-03.
/// </summary>
[SugarTable("BASE_CODE_NUM")]
public class CodeNumEntity : OnlyCLDEntityBase
{
    /// <summary>
    /// 类型 .
    /// ZZ-组织,GW-岗位，YHZ-用户组，YHJS-用户角色，ZZJS-组织角色，GWJS-岗位角色,SF-身份编码，LC-流程，MH-门户，BDHC-表单回传.
    /// </summary>
    [SugarColumn(ColumnName = "F_TYPE")]
    public string Type { get; set; }

    /// <summary>
    /// 日期：用于判断是否重置.
    /// </summary>
    [SugarColumn(ColumnName = "F_DATE_VALUE")]
    public int DateValue { get; set; }

    /// <summary>
    /// 单据递增序号.
    /// </summary>
    [SugarColumn(ColumnName = "F_NUM")]
    public int Num { get; set; }
}