using JNPF.Common.Models.Authorize;
using JNPF.Common.Models.User;
using JNPF.Systems.Entitys.Permission;
using SqlSugar;

namespace JNPF.Common.Core.Manager;

/// <summary>
/// 用户管理抽象.
/// </summary>
public interface IUserManager
{
    #region 请求属性
    /// <summary>
    /// 用户ID.
    /// </summary>
    string UserId { get; }

    /// <summary>
    /// 用户账号.
    /// </summary>
    string Account { get; }

    /// <summary>
    /// 用户昵称.
    /// </summary>
    string RealName { get; }

    /// <summary>
    /// 租户ID.
    /// </summary>
    string TenantId { get; }

    /// <summary>
    /// 当前用户 token.
    /// </summary>
    string ToKen { get; }

    /// <summary>
    /// 获取请求端类型 pc 、 app.
    /// </summary>
    string UserOrigin { get; }

    /// <summary>
    /// 应用编码.
    /// </summary>
    string AppCode { get; }

    /// <summary>
    /// 当前pc/app身份.
    /// </summary>
    string? Standing { get; }

    /// <summary>
    /// 获取请求vue版本
    /// 3-vue3,其他-vue2.
    /// </summary>
    int VueVersion { get; }

    /// <summary>
    /// 当前是否主应用.
    /// </summary>
    bool IsMainSystem { get; }
    #endregion

    #region 租户、缓存
    /// <summary>
    /// 租户数据库名称.
    /// </summary>
    string TenantDbName { get; }

    /// <summary>
    /// 当前租户配置.
    /// </summary>
    GlobalTenantCacheModel CurrentTenantInformation { get; }

    /// <summary>
    /// 多租户场景忽略过滤的应用和菜单Ids.
    /// </summary>
    List<string>? TenantIgnoreModuleIdList { get; }

    /// <summary>
    /// 多租户场景忽略过滤的应用和菜单urlList.
    /// </summary>
    List<string>? TenantIgnoreUrlAddressList { get; }

    /// <summary>
    /// 获取公用菜单 编码 .
    /// </summary>
    /// <returns></returns>
    List<string> CommonModuleEnCodeList { get; }

    /// <summary>
    /// 获取全局租户缓存.
    /// </summary>
    /// <returns></returns>
    List<GlobalTenantCacheModel> GetGlobalTenantCache();
    #endregion

    #region 当前用户属性
    /// <summary>
    /// 用户信息.
    /// </summary>
    UserEntity User { get; }

    /// <summary>
    /// 是否是管理员.
    /// </summary>
    bool IsAdministrator { get; }

    /// <summary>
    /// 获取请求端应用 id.
    /// </summary>
    string SystemId { get; }

    /// <summary>
    /// 是否是管理员角色.
    /// </summary>
    bool? IsManageRole { get; }

    /// <summary>
    /// 是否是开发者角色.
    /// </summary>
    bool? IsDevRole { get; }

    /// <summary>
    /// 是否所有身份包含开发者角色.
    /// </summary>
    bool? IsAllStandingDevRole { get; }

    /// <summary>
    /// 是否是使用者角色.
    /// </summary>
    bool? IsUserRole { get; }

    /// <summary>
    /// 是否有后台权限.
    /// </summary>
    bool? HasBackend { get; }

    /// <summary>
    /// 当前用户权限Ids (用户id、角色ids、岗位ids).
    /// </summary>
    List<string> PermissionIds { get; }

    /// <summary>
    /// 当前用户及下属.
    /// </summary>
    List<string> CurrentUserAndSubordinates { get; }

    /// <summary>
    /// 当前用户及子孙下属.
    /// </summary>
    List<string> CurrentUserTree { get; }

    /// <summary>
    /// 当前用户所属组织(所有).
    /// </summary>
    string CurrentUserOrganizeList { get; }

    /// <summary>
    /// 当前用户所属组织(所有).
    /// </summary>
    List<string> CurrentOrganizeList { get; }

    /// <summary>
    /// 当前组织及子组织.
    /// </summary>
    List<string> CurrentOrganizationAndSubOrganizations { get; }

    /// <summary>
    /// 当前组织及子孙组织(所有).
    /// </summary>
    List<string> CurrentOrganizationTree { get; }

    /// <summary>
    /// 当前用户所属岗位(所有).
    /// </summary>
    string CurrentPositionList { get; }

    /// <summary>
    /// 当前所属岗位及子岗位(所有).
    /// </summary>
    string CurrentPositionAndSuborganization { get; }

    /// <summary>
    /// 当前所属岗位及子孙岗位(所有).
    /// </summary>
    string CurrentPositionTree { get; }

    /// <summary>
    /// 直属主管.
    /// </summary>
    List<string> CurrentUserSupervisor { get; }
    #endregion

    #region 公共方法

    /// <summary>
    /// 获取用户登录信息.
    /// </summary>
    /// <returns></returns>
    Task<UserInfoModel> GetUserInfo();

    /// <summary>
    /// 获取数据条件.
    /// </summary>
    /// <typeparam name="T">实体.</typeparam>
    /// <param name="moduleId">模块ID.</param>
    /// <param name="primaryKey">表主键.</param>
    /// <param name="isDataPermissions">是否开启数据权限.</param>
    /// <param name="primaryKeyPolicy">是否自增长Id.</param>
    /// <returns></returns>
    Task<List<IConditionalModel>> GetConditionAsync<T>(string moduleId, string primaryKey = "f_id", bool isDataPermissions = true, int primaryKeyPolicy = 1)
        where T : new();

    /// <summary>
    /// 获取代码生成数据条件 .
    /// </summary>
    /// <typeparam name="T">实体.</typeparam>
    /// <param name="moduleId">模块ID.</param>
    /// <param name="primaryKey">表主键.</param>
    /// <param name="primaryKeyPolicy">是否自增长Id.</param>
    /// <param name="isDataPermissions">是否开启数据权限.</param>
    /// <returns></returns>
    Task<List<CodeGenAuthorizeModuleResourceModel>> GetCodeGenAuthorizeModuleResource<T>(string moduleId, string primaryKey, int primaryKeyPolicy, bool isDataPermissions = true)
        where T : new();

    /// <summary>
    /// 所属机构(所有).
    /// </summary>
    /// <param name="userId">用户ID.</param>
    /// <returns></returns>
    List<string> GetUserOrganizeList(string userId);

    /// <summary>
    /// 所属组织及子组织(所有).
    /// </summary>
    /// <param name="userId">用户ID.</param>
    /// <returns></returns>
    List<string> GetUserOrganizeAndSuborganization(string userId);

    /// <summary>
    /// 所属组织及子孙组织(所有).
    /// </summary>
    /// <param name="userId">用户ID.</param>
    /// <returns></returns>
    List<string> GetUserOrganizeTree(string userId);

    /// <summary>
    /// 所属岗位(所有).
    /// </summary>
    /// <param name="userId">用户ID.</param>
    /// <returns></returns>
    List<string> GetUserPositionList(string userId);

    /// <summary>
    /// 所属岗位及子岗位(所有).
    /// </summary>
    /// <param name="userId">用户ID.</param>
    /// <returns></returns>
    List<string> GetUserPositionAndSuborganization(string userId);

    /// <summary>
    /// 所属岗位及子孙岗位(所有).
    /// </summary>
    /// <param name="userId">用户ID.</param>
    /// <returns></returns>
    List<string> GetUserPositionTree(string userId);

    /// <summary>
    /// 获取组织、岗位配置层级.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    Task<int> GetOrganizeOrPositionLayer(string type);

    /// <summary>
    /// 获取下属.
    /// </summary>
    /// <param name="managerId">主管Id.</param>
    /// <param name="isAllSub">是否子孙级全捞.</param>
    /// <returns></returns>
    string[] GetSubordinates(string managerId, bool isAllSub = false);

    /// <summary>
    /// 获取所有岗位下属.
    /// </summary>
    /// <param name="managerId">主管Id.</param>
    /// <param name="isAllSub">是否子孙级全捞.</param>
    /// <returns></returns>
    List<string> GetSubordinatesAllPos(string managerId, bool isAllSub = false);

    /// <summary>
    /// 用户权限Ids(角色ids、组织ids) 根据用户id.
    /// </summary>
    /// <param name="userId">用户id.</param>
    /// <param name="isCurrStanding">是否身份过滤.</param>
    /// <returns></returns>
    List<string> GetPermissionIds(string userId, bool isCurrStanding = true);

    /// <summary>
    /// 当前用户权限Ids (ItemIds).
    /// </summary>
    /// <param name="userId">用户id.</param>
    /// <returns></returns>
    Dictionary<string, object> GetAuthorizeItemIds(string userId);

    /// <summary>
    /// 获取用户权限项目id,根据项目类型.
    /// </summary>
    /// <param name="objectType">system、module、button、column、resource、standing ...</param>
    /// <param name="userId">指定用户id.(为空则为当前用户id).</param>
    /// <returns></returns>
    Task<List<string>> GetUserAuthorizeItemIdList(string objectType, string userId = "");

    /// <summary>
    /// 根据项目类型获取用户身份的权限id.
    /// <param name="objectType"></param>
    /// </summary>
    /// <returns></returns>
    Task<List<string>> GetUserStandingAuthIdList(string objectType);

    /// <summary>
    /// 获取用户名称.
    /// </summary>
    /// <param name="userId">用户id.</param>
    /// <param name="isAccount">是否带账号.</param>
    /// <returns></returns>
    string GetUserName(string userId, bool isAccount = true);

    /// <summary>
    /// 获取管理员用户id.
    /// </summary>
    string GetAdminUserId();

    /// <summary>
    /// 获取系统参数值.
    /// </summary>
    /// <param name="value">系统参数.</param>
    /// <param name="userId">用户id.</param>
    /// <param name="type">模块类型 0-默认 1-代码生成.</param>
    /// <returns></returns>
    object GetSysParamValue(string value, string userId = null, int type = 0);

    /// <summary>
    /// 直属主管.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    List<string> GetSupervisor(string userId);

    /// <summary>
    /// 用户组件所有id.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    List<string> GetUserRelationAll(string userId);
    #endregion

    #region 在线开发

    /// <summary>
    /// 获取数据权限.
    /// </summary>
    /// <returns></returns>
    Task<List<IConditionalModel>> GetDataPermission(string primaryKey, string menuId, bool isDataPermissions, bool primaryKeyPolicy);

    /// <summary>
    /// 组装查询条件.
    /// </summary>
    /// <returns></returns>
    string AssembleQuery(string superQuery, bool isDataPermissions = false);

    #endregion
}