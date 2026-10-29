using JNPF.Common.Contracts;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    [SugarTable("sx_maintenance_project_device")]
    public class MaintenanceProjectDeviceEntity : CLDSEntityBase
    {
        /// <summary>
        /// 维护项目id
        /// </summary>
        [SugarColumn(ColumnName = "maintenance_project_id", IsNullable = false, ColumnDescription = "/// 维护项目id ///")]
        [Description("维护项目id")]
        public string MaintenanceProjectId { get; set; }

        /// <summary>
        /// 设备id
        /// </summary>
        [SugarColumn(ColumnName = "device_id", IsNullable = false, ColumnDescription = "/// 设备id ///")]
        [Description("设备id")]
        public string DeviceId { get; set; }

    }
}
