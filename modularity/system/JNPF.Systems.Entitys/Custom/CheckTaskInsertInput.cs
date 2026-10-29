using JNPF.Common.Filter;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JNPF.Systems.Entitys.Dto.Custom
{
    /// <summary>
    /// 点检任务添加输入
    /// </summary>
    public class CheckTaskInsertInput
    {
        /// <summary>
        /// 主键
        /// </summary>
        //public string Id { get; set; }

        /// <summary>
        /// 项目主表id
        /// </summary>
        public string MainId { get; set; }

        /// <summary>
        /// 设备id
        /// </summary>
        public string? DeviceId { get; set; }

        /// <summary>
        /// 日期
        /// </summary>
        public string CheckDate { get; set; }

        /// <summary>
        /// 类型（0日、1月、2年点检）
        /// </summary>
        public int? CheckType { get; set; }

    }

    /// <summary>
    /// 点检任务查询输入
    /// </summary>
    public class CheckTaskQuery : PageInputBase
    {
        /// <summary>
        /// 类型（0日、1月、2年点检）
        /// </summary>
        public int? CheckType { get; set; }

        /// <summary>
        /// 设备id
        /// </summary>
        public string? DeviceId { get; set; }

        /// <summary>
        /// 开始日期
        /// </summary>
        public string CheckStartDate { get; set; }

        /// <summary>
        /// 结束日期
        /// </summary>
        public string CheckEndDate { get; set; }

    }

    /// <summary>
    /// 点检任务查询返回
    /// </summary>
    public class CheckTaskOutput
    {
        /// <summary>
        /// 主键
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 项目主表id
        /// </summary>
        public string MainId { get; set; }

        /// <summary>
        /// 设备id
        /// </summary>
        public string? DeviceName { get; set; }

        /// <summary>
        /// 日期
        /// </summary>
        public string CheckDate { get; set; }

        /// <summary>
        /// 项目名称
        /// </summary>
        public string? ItemName { get; set; }

        /// <summary>
        /// 类型（0日、1月、2年点检）
        /// </summary>
        public int? CheckType { get; set; }

        /// <summary>
        /// 获取或设置 启用标识
        /// 0-禁用,1-启用.
        /// </summary>
        public int? EnabledMark { get; set; }

        public string? CreatorTime { get; set; }

        /// <summary>
        /// 获取或设置 创建用户.
        /// </summary>
        public string? CreatorUserName { get; set; }

        /// <summary>
        /// 版本名称
        /// </summary>
        public string? VersionName { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string? Note { get; set; }

    }

    /// <summary>
    /// 点检任务版本检查返回
    /// </summary>
    public class CheckTaskVersionOutput
    {
        /// <summary>
        /// 主键
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 点检任务id
        /// </summary>
        public string TaskId { get; set; }

        /// <summary>
        /// 改版
        /// </summary>
        public string Revision { get; set; }

        /// <summary>
        /// 年月日
        /// </summary>
        public string RevisionDate { get; set; }

        /// <summary>
        /// 理由
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// 做成
        /// </summary>
        public string CreatedBy { get; set; }

        /// <summary>
        /// 承认者
        /// </summary>
        public string Approver { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public string CreatorTime { get; set; }

        /// <summary>
        /// 创建人ID
        /// </summary>
        public string CreatorUserId { get; set; }
    }

    /// <summary>
    /// 点检任务版本检查输入
    /// </summary>
    public class CheckTaskVersionInput
    {
        /// <summary>
        /// 主键
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 点检任务id
        /// </summary>
        public string TaskId { get; set; }

        /// <summary>
        /// 改版
        /// </summary>
        public string? Revision { get; set; }

        /// <summary>
        /// 年月日
        /// </summary>
        public string RevisionDate { get; set; }

        /// <summary>
        /// 理由
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// 做成
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// 承认者
        /// </summary>
        public string? Approver { get; set; }
    }

    /// <summary>
    /// 点检任务内容返回
    /// </summary>
    public class CheckTaskContentOutput
    {
        /// <summary>
        /// id
        /// </summary>]
        public string Id { get; set; }

        /// <summary>
        /// 项目主表id
        /// </summary>]

        public string Belongs { get; set; }
        /// <summary>
        /// 项目主表id
        /// </summary>]
        public string MainId { get; set; }

        /// <summary>
        /// 设备id
        /// </summary>
        public string? DeviceId { get; set; }

        /// <summary>
        /// 设备名称
        /// </summary>
        public string DeviceName { get; set; }

        /// <summary>
        /// 设备编号
        /// </summary>
        public string DeviceNo { get; set; }

        /// <summary>
        /// 设备厂家
        /// </summary>
        public string DeviceManufacturer { get; set; }

        /// <summary>
        /// 设备厂家
        /// </summary>
        public string ImportantLevel { get; set; }

        /// <summary>
        /// 使用部门(所属)
        /// </summary>
        public string UseDpt { get; set; }

        /// <summary>
        /// 日期
        /// </summary>
        public string CheckDate { get; set; }

        /// <summary>
        /// 类型（0日、1月、2年点检）
        /// </summary>
        public int? CheckType { get; set; }

        /// <summary>
        /// 课长人
        /// </summary>
        public string? SectionChiefId { get; set; }

        /// <summary>
        /// 班长人
        /// </summary>
        public string? TeamLeader { get; set; }

        /// <summary>
        /// 担当人
        /// </summary>
        public string? ResponsiblePerson { get; set; }

        /// <summary>
        /// 工时
        /// </summary>
        public string? WorkHour { get; set; }

        /// <summary>
        /// 保存年限
        /// </summary>
        public string? SaveYears { get; set; }

        /// <summary>
        /// 表单编号
        /// </summary>
        public string? FormNumber { get; set; }

        /// <summary>
        /// 特记事项
        /// </summary>
        public string? SpecialNotes { get; set; }

        /// <summary>
        /// 设备缩略图
        /// </summary>
        public string? DeviceThumbnail { get; set; }

        /// <summary>
        /// 综合判定结果
        /// </summary>
        public string? OverallJudgment { get; set; }

        /// <summary>
        /// 点检者
        /// </summary>
        public string? CheckPerson { get; set; }

        /// <summary>
        /// 点检日
        /// </summary>
        public string? CheckDay { get; set; }

        // 追加上checkperson1-31字段
        public string? CheckPerson1 { get; set; }
        public string? CheckPerson2 { get; set; }
        public string? CheckPerson3 { get; set; }
        public string? CheckPerson4 { get; set; }
        public string? CheckPerson5 { get; set; }
        public string? CheckPerson6 { get; set; }
        public string? CheckPerson7 { get; set; }
        public string? CheckPerson8 { get; set; }
        public string? CheckPerson9 { get; set; }
        public string? CheckPerson10 { get; set; }
        public string? CheckPerson11 { get; set; }
        public string? CheckPerson12 { get; set; }
        public string? CheckPerson13 { get; set; }
        public string? CheckPerson14 { get; set; }
        public string? CheckPerson15 { get; set; }
        public string? CheckPerson16 { get; set; }
        public string? CheckPerson17 { get; set; }
        public string? CheckPerson18 { get; set; }
        public string? CheckPerson19 { get; set; }
        public string? CheckPerson20 { get; set; }
        public string? CheckPerson21 { get; set; }
        public string? CheckPerson22 { get; set; }
        public string? CheckPerson23 { get; set; }
        public string? CheckPerson24 { get; set; }
        public string? CheckPerson25 { get; set; }
        public string? CheckPerson26 { get; set; }
        public string? CheckPerson27 { get; set; }
        public string? CheckPerson28 { get; set; }
        public string? CheckPerson29 { get; set; }
        public string? CheckPerson30 { get; set; }
        public string? CheckPerson31 { get; set; }

        /// <summary>
        /// 点检项目id
        /// </summary>
        public List<DeviceCheckItemResult> Parents { get; set; }

    }
    public class DeviceCheckItemResult
    {

        /// <summary>
        /// parentid
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 点检项
        /// </summary>
        public string? CheckItem { get; set; }

        /// <summary>
        /// 点检项目id
        /// </summary>
        public List<DeviceCheckItemSubResult> Sons { get; set; }
    }
    public class DeviceCheckItemSubResult
    {
        /// <summary>
        /// subid
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 点检项
        /// </summary>
        public string? CheckItem { get; set; }

        /// <summary>
        /// 点检场所
        /// </summary>
        public string? CheckPlace { get; set; }

        /// <summary>
        /// 点检方法
        /// </summary>
        public string? CheckMethod { get; set; }

        /// <summary>
        /// 判定基准
        /// </summary>
        public string? JudgmentStandard { get; set; }

        /// <summary>
        /// No
        /// </summary>
        public string? ItemNo { get; set; }

        /// <summary>
        /// 点检者
        /// </summary>
        public string? CheckPerson { get; set; }

        /// <summary>
        /// 点检结果
        /// </summary>
        public string? CheckResult { get; set; }

        /// <summary>
        /// 判定
        /// </summary>
        public string? Judgment { get; set; }

        /// <summary>
        /// 异常项目描述
        /// </summary>
        public string? ExceptionDesc { get; set; }

        // 值字段
        public string? Value1 { get; set; }
        public string? Value2 { get; set; }
        public string? Value3 { get; set; }
        public string? Value4 { get; set; }
        public string? Value5 { get; set; }
        public string? Value6 { get; set; }
        public string? Value7 { get; set; }
        public string? Value8 { get; set; }
        public string? Value9 { get; set; }
        public string? Value10 { get; set; }
        public string? Value11 { get; set; }
        public string? Value12 { get; set; }
        public string? Value13 { get; set; }
        public string? Value14 { get; set; }
        public string? Value15 { get; set; }
        public string? Value16 { get; set; }
        public string? Value17 { get; set; }
        public string? Value18 { get; set; }
        public string? Value19 { get; set; }
        public string? Value20 { get; set; }
        public string? Value21 { get; set; }
        public string? Value22 { get; set; }
        public string? Value23 { get; set; }
        public string? Value24 { get; set; }
        public string? Value25 { get; set; }

        public string? Value26 { get; set; }

        public string? Value27 { get; set; }

        public string? Value28 { get; set; }

        public string? Value29 { get; set; }

        public string? Value30 { get; set; }

        public string? Value31 { get; set; }
    }

    /// <summary>
    /// 点检任务内容输入
    /// </summary>
    public class CheckTaskContentInput
    {
        /// <summary>
        /// id
        /// </summary>]
        public string Id { get; set; }

        /// <summary>
        /// 课长人
        /// </summary>
        public string? SectionChiefId { get; set; }

        /// <summary>
        /// 班长人
        /// </summary>
        public string? TeamLeader { get; set; }

        /// <summary>
        /// 担当人
        /// </summary>
        public string? ResponsiblePerson { get; set; }

        /// <summary>
        /// 工时
        /// </summary>
        public string? WorkHour { get; set; }
        /// <summary>
        /// 保存年限
        /// </summary>
        public string? SaveYears { get; set; }

        /// <summary>
        /// 表单编号
        /// </summary>
        public string? FormNumber { get; set; }

        /// <summary>
        /// 特记事项
        /// </summary>
        public string? SpecialNotes { get; set; }

        /// <summary>
        /// 设备缩略图
        /// </summary>
        public string? DeviceThumbnail { get; set; }

        /// <summary>
        /// 综合判定结果
        /// </summary>
        public string? OverallJudgment { get; set; }

        /// <summary>
        /// 点检者
        /// </summary>
        public string? CheckPerson { get; set; }

        /// <summary>
        /// 点检日
        /// </summary>
        public string? CheckDay { get; set; }

        // 追加上checkperson1-31字段
        public string? CheckPerson1 { get; set; }
        public string? CheckPerson2 { get; set; }
        public string? CheckPerson3 { get; set; }
        public string? CheckPerson4 { get; set; }
        public string? CheckPerson5 { get; set; }
        public string? CheckPerson6 { get; set; }
        public string? CheckPerson7 { get; set; }
        public string? CheckPerson8 { get; set; }
        public string? CheckPerson9 { get; set; }
        public string? CheckPerson10 { get; set; }
        public string? CheckPerson11 { get; set; }
        public string? CheckPerson12 { get; set; }
        public string? CheckPerson13 { get; set; }
        public string? CheckPerson14 { get; set; }
        public string? CheckPerson15 { get; set; }
        public string? CheckPerson16 { get; set; }
        public string? CheckPerson17 { get; set; }
        public string? CheckPerson18 { get; set; }
        public string? CheckPerson19 { get; set; }
        public string? CheckPerson20 { get; set; }
        public string? CheckPerson21 { get; set; }
        public string? CheckPerson22 { get; set; }
        public string? CheckPerson23 { get; set; }
        public string? CheckPerson24 { get; set; }
        public string? CheckPerson25 { get; set; }
        public string? CheckPerson26 { get; set; }
        public string? CheckPerson27 { get; set; }
        public string? CheckPerson28 { get; set; }
        public string? CheckPerson29 { get; set; }
        public string? CheckPerson30 { get; set; }
        public string? CheckPerson31 { get; set; }
    }

    /// <summary>
    /// 点检任务内容子表输入
    /// </summary>
    public class CheckTaskContentItemInput
    {
        /// <summary>
        /// id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 检查项id
        /// </summary>
        public string SubId { get; set; }

        /// <summary>
        /// 点检者
        /// </summary>
        public string? CheckPerson { get; set; }

        /// <summary>
        /// 点检结果
        /// </summary>
        public string? CheckResult { get; set; }

        /// <summary>
        /// 判定
        /// </summary>
        public string? Judgment { get; set; }

        /// <summary>
        /// 异常项目描述
        /// </summary>
        public string? ExceptionDesc { get; set; }

        /// <summary>
        /// 属性日期
        /// </summary>
        public string PropDateName { get; set; }

        // 值字段
        public string? Value { get; set; }

    }
}
