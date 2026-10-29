using JNPF.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace JNPF.Common.Const;

/// <summary>
/// 公共常量.
/// </summary>
[SuppressSniffer]
public class CommonConst
{
    // 不带自定义转换器
    public static JsonSerializerSettings options => new JsonSerializerSettings
    {
        MaxDepth = 64,

        // 格式化JSON文本
        Formatting = Formatting.Indented,

        // 默认命名规则
        ContractResolver = new DefaultContractResolver(),

        // 设置时区为 Utc
        DateTimeZoneHandling = DateTimeZoneHandling.Utc,

        // 格式化json输出的日期格式
        DateFormatString = "yyyy-MM-dd HH:mm:ss",

        // 忽略循环引用
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
    };

    /// <summary>
    /// 全局租户缓存.
    /// </summary>
    public const string GLOBALTENANT = "jnpf:global:tenant";

    /// <summary>
    /// 集成助手缓存.
    /// </summary>
    public const string INTEASSISTANT = "jnpf:global:integrate";

    /// <summary>
    /// 集成助手重试缓存.
    /// </summary>
    public const string INTEASSISTANTRETRY = "jnpf:global:integrate:retry";

    /// <summary>
    /// 集成助手WebHook.
    /// </summary>
    public const string INTEGRATEWEBHOOK = "jnpf:global:integrate:webhook";

    /// <summary>
    /// 默认密码.
    /// </summary>
    public const string DEFAULTPASSWORD = "0000";

    /// <summary>
    /// 用户缓存.
    /// </summary>
    public const string CACHEKEYUSER = "jnpf:permission:user";

    /// <summary>
    /// 菜单缓存.
    /// </summary>
    public const string CACHEKEYMENU = "menu_";

    /// <summary>
    /// 权限缓存.
    /// </summary>
    public const string CACHEKEYPERMISSION = "permission_";

    /// <summary>
    /// 数据范围缓存.
    /// </summary>
    public const string CACHEKEYDATASCOPE = "datascope_";

    /// <summary>
    /// 验证码缓存.
    /// </summary>
    public const string CACHEKEYCODE = "vercode_";

    /// <summary>
    /// 单据编码缓存.
    /// </summary>
    public const string CACHEKEYBILLRULE = "billrule_";

    /// <summary>
    /// 在线用户缓存.
    /// </summary>
    public const string CACHEKEYONLINEUSER = "jnpf:user:online";

    /// <summary>
    /// 全局组织树缓存.
    /// </summary>
    public const string CACHEKEYORGANIZE = "jnpf:global:organize";

    /// <summary>
    /// 岗位缓存.
    /// </summary>
    public const string CACHEKEYPOSITION = "position_";

    /// <summary>
    /// 角色缓存.
    /// </summary>
    public const string CACHEKEYROLE = "role_";

    /// <summary>
    /// 在线开发缓存.
    /// </summary>
    public const string VISUALDEV = "visualdev_";

    /// <summary>
    /// 代码生成远端数据缓存.
    /// </summary>
    public const string CodeGenDynamic = "codegendynamic_";

    /// <summary>
    /// 定时任务缓存.
    /// </summary>
    public const string CACHEKEYTIMERJOB = "timerjob_";

    /// <summary>
    /// 第三方登录 票据缓存key.
    /// </summary>
    public const string PARAMS_JNPF_TICKET = "jnpf_ticket";

    /// <summary>
    /// Cas Key.
    /// </summary>
    public const string CAS_Ticket = "ticket";

    /// <summary>
    /// Code.
    /// </summary>
    public const string Code = "code";

    /// <summary>
    /// 外链密码开关(1：开 ,0：关).
    /// </summary>
    public const int OnlineDevData_State_Enable = 1;

    /// <summary>
    /// 门户日程缓存key.
    /// </summary>
    public const string CACHEKEYSCHEDULE = "jnpf:portal:schedule";

    /// <summary>
    /// 系统关键词.
    /// </summary>
    public const string SYSTEMKEY = "PUBLIC,STRING,VOID,BASE,THIS,USING,CLASS,STRUCT,ABSTRACT,INTERFACE,IS,AS,IN,OUT,REF,OBJECT,INT,DECIMAL,DOUBLE,FLOAT,BOOL,PRIVATE,DELEGATE,DEFAULT,DO,IF,ELSE,SWITCH,CASE,FOR,FOREACH,FALSE,FINALLY,FIXED,INTERNAL,NAMESPACE,OVERRIDE,RETURN,NULL,TRUE,WHILE,CONST,BREAK,CONTINUE,VIRTUAL,TENANTID,ID,FOREIGNID,FLOWID,FLOWTASKID,DELETEUSERID,DELETETIME,DELETEMARK,VERSION,TENANT_ID,FOREIGN_ID,FLOW_ID,FLOW_TASK_ID,DELETE_USER_ID,DELETE_TIME,DELETE_MARK,F_TENANT_ID,F_ID,F_FOREIGN_ID,F_FLOW_ID,F_FLOW_TASK_ID,F_DELETE_USER_ID,F_DELETE_TIME,F_DELETE_MARK,F_VERSION";

    /// <summary>
    /// 动态查询标识.
    /// </summary>
    public const string DYNAMICQUERYKEY = "@currentTime,@userId,@userAndSub,@userAndProgeny,@positionId,@positionAndSub,@positionAndProgeny,@organizeId,@organizeAndSub,@organizeAndProgeny";

    #region 关联对象类型

    /// <summary>
    /// 组织关联对象类型.
    /// </summary>
    public const string RelationObjectOrganize = "organize";

    /// <summary>
    /// 角色关联对象类型.
    /// </summary>
    public const string RelationObjectRole = "role";

    /// <summary>
    /// 用户关联对象类型.
    /// </summary>
    public const string RelationObjectUser = "user";

    /// <summary>
    /// 岗位关联对象类型.
    /// </summary>
    public const string RelationObjectPosition = "position";

    /// <summary>
    /// 岗位关联对象类型.
    /// </summary>
    public const string RelationObjectGroup = "group";

    #endregion

    #region 权限项目类型

    /// <summary>
    /// 身份.
    /// </summary>
    public const string AuthorizeItemStanding = "standing";

    /// <summary>
    /// 应用.
    /// </summary>
    public const string AuthorizeItemSystem = "system";

    /// <summary>
    /// 菜单.
    /// </summary>
    public const string AuthorizeItemModule = "module";

    /// <summary>
    /// 列.
    /// </summary>
    public const string AuthorizeItemColumn = "column";

    /// <summary>
    /// 表单.
    /// </summary>
    public const string AuthorizeItemForm = "form";

    /// <summary>
    /// 按钮.
    /// </summary>
    public const string AuthorizeItemButton = "button";

    /// <summary>
    /// 数据.
    /// </summary>
    public const string AuthorizeItemResource = "resource";

    /// <summary>
    /// 流程.
    /// </summary>
    public const string AuthorizeItemFlow = "flow";

    /// <summary>
    /// 打印.
    /// </summary>
    public const string AuthorizeItemPrint = "print";

    #endregion

    #region 组织类型

    /// <summary>
    /// 根组织.
    /// </summary>
    public const string OrganizeTypeBase = "base";

    /// <summary>
    /// 集团.
    /// </summary>
    public const string OrganizeTypeGroup = "group";

    /// <summary>
    /// 公司.
    /// </summary>
    public const string OrganizeTypeCompany = "company";

    /// <summary>
    /// 子公司.
    /// </summary>
    public const string OrganizeTypeSubsidiary = "subsidiary";

    /// <summary>
    /// 部门.
    /// </summary>
    public const string OrganizeTypeDepartment = "department";

    /// <summary>
    /// 机构.
    /// </summary>
    public const string OrganizeTypeInstitution = "agency";

    /// <summary>
    /// 机关.
    /// </summary>
    public const string OrganizeTypeOrgan = "office";

    /// <summary>
    /// 单位.
    /// </summary>
    public const string OrganizeTypeUnit = "unit";

    #endregion

    #region 单据编码类型

    /// <summary>
    /// 组织.
    /// </summary>
    public const string BillRuleCodeTypeOrganize = "ZZ";

    /// <summary>
    /// 岗位.
    /// </summary>
    public const string BillRuleCodeTypePosition = "GW";

    /// <summary>
    /// 用户组.
    /// </summary>
    public const string BillRuleCodeTypeGroup = "YHZ";

    /// <summary>
    /// 用户角色.
    /// </summary>
    public const string BillRuleCodeTypeUserRole = "YHJS";

    /// <summary>
    /// 组织角色.
    /// </summary>
    public const string BillRuleCodeTypeOrgRole = "ZZJS";

    /// <summary>
    /// 岗位角色.
    /// </summary>
    public const string BillRuleCodeTypePosRole = "GWJS";

    /// <summary>
    /// 身份编码.
    /// </summary>
    public const string BillRuleCodeTypeStanding = "SF";

    /// <summary>
    /// 流程.
    /// </summary>
    public const string BillRuleCodeTypeWorkFlow = "LC";

    /// <summary>
    /// 门户.
    /// </summary>
    public const string BillRuleCodeTypePortal = "MH";

    /// <summary>
    /// 表单回传.
    /// </summary>
    public const string BillRuleCodeTypeForm = "BDHC";

    /// <summary>
    /// 应用.
    /// </summary>
    public const string BillRuleCodeTypeSystem = "YY";

    /// <summary>
    /// 在线表单.
    /// </summary>
    public const string BillRuleCodeTypeVisualDev = "ZXBD";

    /// <summary>
    /// 报表.
    /// </summary>
    public const string BillRuleCodeTypeReport = "BB";

    /// <summary>
    /// 打印.
    /// </summary>
    public const string BillRuleCodeTypePrint = "DY";

    /// <summary>
    /// 表单模板.
    /// </summary>
    public const string BillRuleCodeTypeVisualKit = "BDTJ";

    #endregion

    #region 各种类型编码

    /// <summary>
    /// 角色编码-管理员.
    /// </summary>
    public const string RoleCodeTypeManager = "manager";

    /// <summary>
    /// 角色编码-开发者.
    /// </summary>
    public const string RoleCodeTypeDeveloper = "developer";

    /// <summary>
    /// 角色编码-使用者.
    /// </summary>
    public const string RoleCodeTypeUser = "user";

    /// <summary>
    /// 应用编码-主应用.
    /// </summary>
    public const string SystemCodeMain = "mainSystem";

    /// <summary>
    /// 应用编码-流程中心.
    /// </summary>
    public const string SystemCodeWorkFlow = "workFlow";

    /// <summary>
    /// 应用编码-协同办公.
    /// </summary>
    public const string SystemCodeTeamwork = "teamwork";

    /// <summary>
    /// 应用编码-业务示例.
    /// </summary>
    public const string SystemCodeWorkSystem = "workSystem";

    /// <summary>
    /// 应用编码-开发示例.
    /// </summary>
    public const string SystemCodeDevelopSystem = "developSystem";

    /// <summary>
    /// 菜单功能编码-应用设置.
    /// </summary>
    public const string ModuleCodeAppConfig = "appConfig";

    /// <summary>
    /// 菜单功能编码-在线开发.
    /// </summary>
    public const string ModuleCodeOnlineDev = "onlineDev";

    /// <summary>
    /// 菜单功能编码-我的待签.
    /// </summary>
    public const string ModuleCodeFlowToSign = "workFlow.flowToSign";

    /// <summary>
    /// 菜单功能编码-我的待办.
    /// </summary>
    public const string ModuleCodeFlowTodo = "workFlow.flowTodo";

    /// <summary>
    /// 菜单功能编码-审批中心.
    /// </summary>
    public const string ModuleCodeFlow = "workFlow,workFlow.flowQuickLaunch,workFlow.flowLaunch,workFlow.flowTodo,workFlow.flowDone,workFlow.flowCirculate,workFlow.flowToSign,workFlow.flowDoing";

    /// <summary>
    /// 菜单功能编码-协同办公.
    /// </summary>
    public const string ModuleCodeTeam = "teamwork,teamwork.schedule,teamwork.printTemplate,teamwork.document";

    /// <summary>
    /// 菜单功能编码-后台菜单.
    /// </summary>
    public const string ModuleCodeBackend = "appConfig,appConfig.baseConfig,appConfig.appMenu,appConfig.appResource,appConfig.appAuth,onlineDev,onlineDev.formDesign,onlineDev.visualPortal,onlineDev.report,onlineDev.printDev,onlineDev.dataReport,onlineDev.dataScreen,onlineDev.flowEngine,onlineDev.sysForm";

    /// <summary>
    /// 应用授权编码-所有开发者.
    /// </summary>
    public const string SystemAuthCodeAllDevUser = "allDevUser";

    #endregion

    #region 系统参数key

    /// <summary>
    /// 当前时间.
    /// </summary>
    public const string SysParamCurrentTime = "@currentTime";

    /// <summary>
    /// 当前用户.
    /// </summary>
    public const string SysParamUserId = "@userId";

    /// <summary>
    /// 当前用户及下属.
    /// </summary>
    public const string SysParamUserAndSub = "@userAndSub";

    /// <summary>
    /// 当前用户及子孙下属.
    /// </summary>
    public const string SysParamUserTree = "@userAndProgeny";

    /// <summary>
    /// 当前岗位.
    /// </summary>
    public const string SysParamPositionId = "@positionId";

    /// <summary>
    /// 当前岗位及子岗位.
    /// </summary>
    public const string SysParamPositionAndSub = "@positionAndSub";

    /// <summary>
    /// 当前岗位及子孙岗位.
    /// </summary>
    public const string SysParamPositionTree = "@positionAndProgeny";

    /// <summary>
    /// 当前组织.
    /// </summary>
    public const string SysParamOrganizeId = "@organizeId";

    /// <summary>
    /// 当前组织及子组织.
    /// </summary>
    public const string SysParamOrganizeAndSub = "@organizeAndSub";

    /// <summary>
    /// 当前组织及子孙组织.
    /// </summary>
    public const string SysParamOrganizationTree = "@organizeAndProgeny";

    #endregion
}