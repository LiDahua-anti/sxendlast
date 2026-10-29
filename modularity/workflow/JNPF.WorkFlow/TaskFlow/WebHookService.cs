using JNPF.Common.Configuration;
using JNPF.Common.Const;
using JNPF.Common.Core.EventBus.Sources;
using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Tenant;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Manager;
using JNPF.Common.Models;
using JNPF.Common.Models.WorkFlow;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.EventBus;
using JNPF.FriendlyException;
using JNPF.Logging.Attributes;
using JNPF.Systems.Entitys.Permission;
using JNPF.WorkFlow.Entitys.Entity;
using JNPF.WorkFlow.Entitys.Model.WorkFlow;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using System.Text;

namespace JNPF.WorkFlow.TaskFlow;

[ApiDescriptionSettings(Tag = "workflow", Name = "Hooks", Order = 172)]
[Route("api/workflow/[controller]")]
public class WebHookService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 解析服务作用域工厂服务.
    /// </summary>
    private readonly IServiceScopeFactory _serviceScopeFactory;

    /// <summary>
    /// 缓存管理.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 租户管理.
    /// </summary>
    private readonly ITenantManager _tenantManager;

    private readonly IEventPublisher _eventPublisher;

    /// <summary>
    /// 初始化一个<see cref="WebHookService"/>类型的新实例.
    /// </summary>
    public WebHookService(
        IServiceScopeFactory serviceScopeFactory,
        ICacheManager cacheManager,
        IUserManager userManager,
        ITenantManager tenantManager,
        IEventPublisher eventPublisher)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _cacheManager = cacheManager;
        _userManager = userManager;
        _eventPublisher = eventPublisher;
        _tenantManager = tenantManager;
    }

    #region Get

    /// <summary>
    /// 获取webhookUrl.
    /// </summary>
    /// <param name="id">主键ID.</param>
    /// <returns></returns>
    [HttpGet("getUrl")]
    public async Task<dynamic> GetWebhookUrl([FromQuery] string id)
    {
        var output = new Dictionary<string, object>();
        Random codeRandom = new Random();
        output["enCodeStr"] = Convert.ToBase64String(Encoding.GetEncoding("utf-8").GetBytes(id));
        output["randomStr"] = codeRandom.NextLetterAndNumberString(5);
        output["requestUrl"] = string.Format("/api/workflow/Hooks/{0}/params/{1}", output["enCodeStr"], output["randomStr"]);
        output["webhookUrl"] = string.Format("/api/workflow/Hooks/{0}", output["enCodeStr"]);
        return output;
    }

    /// <summary>
    /// .
    /// </summary>
    /// <param name="id">主键ID.</param>
    /// <param name="randomStr">随机值字符串.</param>
    /// <returns></returns>
    [HttpGet("{id}/start/{randomStr}")]
    public async Task GetWebHookStart(string id, string randomStr)
    {
        await _cacheManager.DelAsync(string.Format("{0}:{1}", CommonConst.INTEGRATEWEBHOOK, randomStr));
        await _cacheManager.SetAsync(string.Format("{0}:{1}:{2}", CommonConst.INTEGRATEWEBHOOK, id, randomStr), _userManager.TenantId, TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// 获取缓存的接口参数.
    /// </summary>
    /// <param name="randomStr">随机值字符串.</param>
    /// <returns></returns>
    [HttpGet("getParams/{randomStr}")]
    public async Task<dynamic> GetRedisParams(string randomStr)
    {
        Dictionary<string, string> resultMap = new Dictionary<string, string>();
        var key = string.Format("{0}:{1}", CommonConst.INTEGRATEWEBHOOK, randomStr);
        if (_cacheManager.Exists(key))
            resultMap = await _cacheManager.GetAsync<Dictionary<string, string>>(key);
        List<StaticData> list = new List<StaticData>();
        foreach (var item in resultMap)
        {
            list.Add(new StaticData
            {
                id = item.Key,
                fullName = item.Value,
            });
        }

        return list;
    }

    /// <summary>
    /// WebHook接口参数获取.
    /// </summary>
    /// <param name="id">主键ID.</param>
    /// <param name="randomStr">随机值字符串.</param>
    /// <returns></returns>
    [AllowAnonymous]
    [IgnoreLog]
    [HttpGet("{id}/params/{randomStr}")]
    public async Task GetWebhookParams(string id, string randomStr)
    {
        await insertRedis(id, randomStr);
    }

    #endregion

    #region Post

    /// <summary>
    /// 获取WebHook触发.
    /// </summary>
    /// <param name="id">主键ID.</param>
    /// <param name="tenantId">租户ID.</param>
    /// <param name="parameter">参数.</param>
    /// <returns></returns>
    [AllowAnonymous]
    [IgnoreLog]
    [HttpPost("{id}")]
    public async Task GetWebHookTrigger(string id, [FromQuery] string tenantId, [FromBody] Dictionary<string, object>? parameter)
    {
        tenantId ??= "default";
        using var scope = _serviceScopeFactory.CreateScope();
        var sqlSugarClient = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var cacheKey = string.Empty;

        if (KeyVariable.MultiTenancy)
        {
            await _tenantManager.ChangTenant(sqlSugarClient, tenantId);
        }

        sqlSugarClient = sqlSugarClient.CopyNew();

        var flowId = Encoding.UTF8.GetString(Convert.FromBase64String(id));
        var model = new TaskFlowEventModel();
        model.TenantId = tenantId;
        var user = await sqlSugarClient.Queryable<UserEntity>().FirstAsync(x => x.Account == "admin");
        model.UserId = user.Id;
        model.ModelId = flowId;
        model.TriggerType = "webhookTrigger";
        model.taskFlowData = new List<Dictionary<string, object>>();
        parameter["id"] = string.Empty;
        model.taskFlowData.Add(parameter);
        var nodeList = await sqlSugarClient.Queryable<WorkFlowNodeEntity, WorkFlowTemplateEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.FlowId == b.FlowId))
              .Where((a, b) => a.FormId == model.ModelId && a.NodeType == model.TriggerType && a.DeleteMark == null && b.EnabledMark == 1 && b.Type == 2 && b.DeleteMark == null)
              .Select((a, b) => new TriggerNodeModel
              {
                  flowName = b.FullName,
                  nodeJson = a.NodeJson,
                  flowId = a.FlowId,
                  engineId = b.FlowableId,
              }).ToListAsync();
        if (!nodeList.Any()) throw Oops.Oh(ErrorCode.WF0067);
        await _eventPublisher.PublishAsync(new TaskFlowEventSource("TaskFlow:CreateTask", model));
    }

    /// <summary>
    /// WebHook接口参数获取.
    /// </summary>
    /// <param name="id">主键ID.</param>
    /// <param name="randomStr">随机值字符串.</param>
    /// <param name="parameter">参数.</param>
    /// <returns></returns>
    [AllowAnonymous]
    [IgnoreLog]
    [HttpPost("{id}/params/{randomStr}")]
    public async Task PostWebhookParams(string id, string randomStr, [FromBody] Dictionary<string, string>? parameter)
    {
        await insertRedis(id, randomStr, parameter);
    }

    #endregion

    #region PrivateMethod

    /// <summary>
    /// 创建缓存.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="randomStr"></param>
    /// <param name="resultMap"></param>
    private async Task insertRedis(string id, string randomStr, Dictionary<string, string>? resultMap = default)
    {
        var inteId = Encoding.UTF8.GetString(Convert.FromBase64String(id));
        var key = string.Format("{0}:{1}:{2}", CommonConst.INTEGRATEWEBHOOK, inteId, randomStr);
        if (!_cacheManager.Exists(key))
            throw Oops.Oh(ErrorCode.D2601);
        var requestParams = App.HttpContext.Request.Query;
        if (requestParams != null && requestParams.Count > 0)
        {
            resultMap ??= new Dictionary<string, string>();
            foreach (var param in requestParams)
            {
                resultMap.Add(param.Key, param.Value);
            }
        }
        if (resultMap != null && resultMap.Count > 0)
        {
            await _cacheManager.DelAsync(key);
            await _cacheManager.SetAsync(string.Format("{0}:{1}", CommonConst.INTEGRATEWEBHOOK, randomStr), resultMap);
        }
    }

    #endregion
}
