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
    /// 检测主表byid输出
    /// </summary>
    public class CheckMainByIdOutput
    {
        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark { get; set; }

        /// <summary>
        /// 项目名称
        /// </summary>
        public string? ItemName { get; set; }

        /// <summary>
        /// 项目类型（0日 1月 2年）
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// 所属
        /// </summary>
        public string? Belongs { get; set; }

        /// <summary>
        /// 主键
        /// </summary>
        public string? Id { get; set; }

        public List<CheckSubByMainIdOutput> SubList { get; set; }

    }

    public class CheckSubByMainIdOutput
    {
        /// <summary>
        /// sub主键
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// 父级id
        /// </summary>
        public string? ParentId { get; set; }

        /// <summary>
        /// 所属
        /// </summary>
        public string? Belongs { get; set; }

        /// <summary>
        /// 点检项
        /// </summary>
        public string? CheckItem { get; set; }

        public List<CheckSubSonByParentIdOutput> SonList { get; set; }
    }

    public class CheckSubSonByParentIdOutput
    {
        /// <summary>
        /// 主键
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// 项目主表id
        /// </summary>
        public string MainId { get; set; }

        /// <summary>
        /// 父级id
        /// </summary>
        public string? ParentId { get; set; }

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
    }

    /// <summary>
    /// 检测主表新增输入
    /// </summary>
    public class CheckMainInsertInput
    {
        /// <summary>
        /// 项目名称
        /// </summary>
        public string? ItemName { get; set; }

        /// <summary>
        /// 项目类型（0日 1月 2年）
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark { get; set; }

        /// <summary>
        /// 所属
        /// </summary>
        public string? Belongs { get; set; }

        /// <summary>
        /// 主键
        /// </summary>
        public string? Id { get; set; }
    }

    /// <summary>
    /// 检查子表父级新增输入
    /// </summary>
    public class CheckSubParentInsertInput
    {
        /// <summary>
        /// 主键
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// 点检项
        /// </summary>
        public string? CheckItem { get; set; }

        /// <summary>
        /// 项目主表id
        /// </summary>
        public string MainId { get; set; }

    }
    public class DeviceInfoDto
    {
        public string DeviceId { get; set; }
        public string DeviceNo { get; set; }
        public string DeviceName { get; set; }
        public string DeviceTypeId { get; set; }
        public string DeviceTypeName { get; set; }
    }

    /// <summary>
    /// Excel导入数据模型
    /// </summary>
    public class DeviceItemImportDto
    {
        /// <summary>
        /// Sheet名称
        /// </summary>
        public string SheetName { get; set; }

        /// <summary>
        /// 表单编号
        /// </summary>
        public string FormNo { get; set; }
        public string DeviceNo { get; set; }

        /// <summary>
        /// 设备ID
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// 设备类型ID
        /// </summary>
        public string DeviceTypeId { get; set; }

        /// <summary>
        /// 点检类型 (0日 1月 2年)
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 主项目/分类
        /// </summary>
        public string MainItem { get; set; }

        /// <summary>
        /// 点检场所
        /// </summary>
        public string CheckPlace { get; set; }

        /// <summary>
        /// 序号
        /// </summary>
        public string ItemNo { get; set; }

        /// <summary>
        /// 点检项目
        /// </summary>
        public string CheckItem { get; set; }

        /// <summary>
        /// 点检方法
        /// </summary>
        public string CheckMethod { get; set; }

        /// <summary>
        /// 判定基准
        /// </summary>
        public string JudgmentStandard { get; set; }
    }

    /// <summary>
    /// 检查子表子级新增输入
    /// </summary>
    public class CheckSubSonInsertInput
    {
        /// <summary>
        /// 主键
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// 项目主表id
        /// </summary>
        public string MainId { get; set; }

        /// <summary>
        /// 父级id
        /// </summary>
        public string? ParentId { get; set; }

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

    }

    /// <summary>
    /// 分配设备输入
    /// </summary>
    public class CheckDeviceAllotInput
    {
        /// <summary>
        /// 点检项目主表id
        /// </summary>
        public string MainId { get; set; }

        /// <summary>
        /// 设备id
        /// </summary>
        public List<string> DeviceId { get; set; }
    }

    /// <summary>
    /// 点检项目主表输出实体
    /// </summary>
    public class DeviceItemMainOutput
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 项目名称
        /// </summary>
        public string ItemName { get; set; }

        /// <summary>
        /// 项目类型（0日 1月 2年）
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// 表单编号
        /// </summary>
        public string Belongs { get; set; }

        /// <summary>
        /// 创建人姓名
        /// </summary>
        public string CreatorUserId { get; set; }

        /// <summary>
        /// 创建时间（字符串格式）
        /// </summary>
        public string CreatorTime { get; set; }
    }

    /// <summary>
    /// 检测主表查询
    /// </summary>
    public class MainTypeQuery : PageInputBase
    {
        /// <summary>
        /// 项目类型（0日 1月 2年）
        /// </summary>
        public string? Type { get; set; }
    }

}
