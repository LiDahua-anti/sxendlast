using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JNPF.Systems.Entitys.Custom
{
    // DeviceTypeWithChildrenDto.cs
    /// <summary>
    /// 设备类型及子设备DTO（子父层级）
    /// </summary>
    public class DeviceTypeWithChildrenDto
    {
        /// <summary>
        /// 设备类型ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 设备类型名称
        /// </summary>
        public string DevicetypeName { get; set; }

        /// <summary>
        /// 排序方向（0:升序 1:降序）
        /// </summary>
        public int? SortDirect { get; set; }

        /// <summary>
        /// 子设备列表
        /// </summary>
        public List<DeviceSortChildDto> Children { get; set; }
    }

    /// <summary>
    /// 设备排序子级DTO
    /// </summary>
    public class DeviceSortChildDto
    {
        /// <summary>
        /// 设备排序配置ID
        /// </summary>
        public string SortConfigId { get; set; }

        /// <summary>
        /// 设备ID
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// 设备编号
        /// </summary>
        public string DeviceNo { get; set; }

        /// <summary>
        /// 第几行
        /// </summary>
        public int DRow { get; set; }

        /// <summary>
        /// 每行顺序
        /// </summary>
        public int RowSort { get; set; }
    }

    /// <summary>
    /// 编辑父级SortDirect请求DTO
    /// </summary>
    public class EditSortDirectInput
    {
        /// <summary>
        /// 设备类型ID
        /// </summary>
        public string DeviceTypeId { get; set; }

        /// <summary>
        /// 排序方向（0:升序 1:降序）
        /// </summary>
        public int SortDirect { get; set; }
    }

    /// <summary>
    /// 新增设备
    /// </summary>
    public class InsertDeviceInput
    {
        /// <summary>
        /// 设备类型ID
        /// </summary>
        public string DeviceTypeId { get; set; }

        /// <summary>
        /// 设备id
        /// </summary>
        public string DeviceId { get; set; }
    }

    /// <summary>
    /// 编辑子级排序请求DTO
    /// </summary>
    public class EditDeviceSortInput
    {
        /// <summary>
        /// 设备排序配置ID
        /// </summary>
        public string SortConfigId { get; set; }

        /// <summary>
        /// 第几行
        /// </summary>
        public int DRow { get; set; }

        /// <summary>
        /// 每行顺序
        /// </summary>
        public int RowSort { get; set; }
    }

    /// <summary>
    /// 分页查询请求DTO
    /// </summary>
    public class DeviceTypePageInput
    {
        /// <summary>
        /// 当前页码
        /// </summary>
        public int CurrentPage { get; set; } = 1;

        /// <summary>
        /// 每页条数
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// 设备类型名称（模糊查询）
        /// </summary>
        public string DevicetypeName { get; set; }
    }

    public class TimeLayoutDto
    {
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string ClassType { get; set; }
    }

}
