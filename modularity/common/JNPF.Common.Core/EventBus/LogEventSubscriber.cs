using JNPF.Common.Configuration;
using JNPF.Common.Core.Manager.Tenant;
using JNPF.Common.Extension;
using JNPF.DependencyInjection;
using JNPF.EventBus;
using SqlSugar;

namespace JNPF.EventHandler;

/// <summary>
/// 日记事件订阅.
/// </summary>
public class LogEventSubscriber : IEventSubscriber, ISingleton
{
    /// <summary>
    /// 初始化客户端.
    /// </summary>
    private static SqlSugarScope? _sqlSugarClient;

    /// <summary>
    /// 租户管理.
    /// </summary>
    private readonly ITenantManager _tenantManager;

    /// <summary>
    /// 构造函数.
    /// </summary>
    public LogEventSubscriber(ISqlSugarClient sqlSugarClient, TenantManager tenantManager)
    {
        _sqlSugarClient = (SqlSugarScope)sqlSugarClient;
        _tenantManager = tenantManager;
    }

    /// <summary>
    /// 创建日记.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    [EventSubscribe("Log:CreateReLog")]
    [EventSubscribe("Log:CreateExLog")]
    [EventSubscribe("Log:CreateVisLog")]
    [EventSubscribe("Log:CreateOpLog")]
    public async Task CreateLog(EventHandlerExecutingContext context)
    {
        var log = (LogEventSource)context.Source;

        if (KeyVariable.MultiTenancy && log.TenantId.IsNotEmptyOrNull())
        {
            await _tenantManager.ChangTenant(_sqlSugarClient, log.TenantId);
        }

        try
        {
            if (log.Entity.Id.IsNullOrEmpty()) log.Entity.Id = SnowFlakeSingle.Instance.NextId().ToString();
            await _sqlSugarClient.CopyNew().Insertable(log.Entity).IgnoreColumns(ignoreNullColumn: true).ExecuteCommandAsync();
        }
        catch
        {
            try
            {
                SnowFlakeSingle.WorkId += SnowFlakeSingle.WorkId;
                SnowFlakeSingle.DatacenterId += SnowFlakeSingle.DatacenterId;
                if (log.Entity.Id.IsNullOrEmpty()) log.Entity.Id = SnowFlakeSingle.Instance.NextId().ToString();
                await _sqlSugarClient.CopyNew().Insertable(log.Entity).IgnoreColumns(ignoreNullColumn: true).ExecuteCommandAsync();
            }
            catch
            {

            }
        }
    }
}