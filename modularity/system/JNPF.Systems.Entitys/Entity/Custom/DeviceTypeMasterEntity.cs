using JNPF.Common.Contracts;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    [SugarTable("sx_devicetype_master")]
    public class DeviceTypeMasterEntity : CLDSEntityBase
    {
        // 父级id
        [SugarColumn(ColumnName = "parent_id")]
        public string ParentId { get; set; }

        /// <summary>
        ///设备类型编码
        /// </summary>
        [SugarColumn(ColumnName = "devicetype_code", ColumnDescription = "///设备类型编码 ///")]
        public string DevicetypeCode { get; set; }

        /// <summary>
        /// 设备类型名称
        /// </summary>
        [SugarColumn(ColumnName = "devicetype_name", ColumnDescription = "/// 设备类型名称 ///")]
        public string DevicetypeName { get; set; }

        /// <summary>
        ///备注
        /// </summary>
        [SugarColumn(ColumnName = "f_note", ColumnDescription = "///备注 ///")]
        public string? FNote { get; set; }

        /// <summary>
        /// 执行方向
        /// </summary>
        [SugarColumn(ColumnName = "sort_direct", IsNullable = false, ColumnDescription = "/// 执行方向 ///")]
        public int SortDirect { get; set; }

    }
}
