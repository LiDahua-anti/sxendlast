using JNPF.Systems.Entitys.Dto.Custom;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JNPF.Systems.Interfaces.Custom
{
    public interface IDeviceService
    {
        Task<dynamic> GetDeviceTypeDicts();

        Task<dynamic> GetDeviceTypes([FromBody] DeviceTypeQuery input);
    }
}
