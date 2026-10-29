using JNPF.Common.Models.User;
using JNPF.Systems.Entitys.Dto.Custom;
using JNPF.Systems.Entitys.Dto.Department;
using JNPF.Systems.Entitys.Dto.Group;
using JNPF.Systems.Entitys.Dto.Organize;
using JNPF.Systems.Entitys.Dto.User;
using JNPF.Systems.Entitys.Entity.Custom;
using JNPF.Systems.Entitys.Permission;
using Mapster;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JNPF.Systems.Entitys.Mapper
{
    public class CustomMapper : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.ForType<DeviceSonTypeInput, DeviceTypeMasterEntity>()
                 .Map(dest => dest.EnabledMark, src => src.F_ENABLED_MARK);
            config.ForType<DeviceRecordsInput, DeviceRepairRecordEntity>();
            config.ForType<DeviceRepairRecordEntity, DeviceRecordsInput>()
                .Ignore(src => src.DeviceId)
                .Ignore(src => src.SpareDatas);
            config.ForType<MaintenanceRecordInsertInput, MaintenanceRecordEntity>();
        }

    }
}
