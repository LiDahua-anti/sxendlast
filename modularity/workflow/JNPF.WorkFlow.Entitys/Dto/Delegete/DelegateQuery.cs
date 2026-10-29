using JNPF.Common.Filter;

namespace JNPF.WorkFlow.Entitys.Dto.Delegete
{
    public class DelegateQuery : PageInputBase
    {
        /// <summary>
        /// 1:我的委托,2:委托给我,3:我的代理,4:代理给我.
        /// </summary>
        public string type { get; set; }
    }
}
