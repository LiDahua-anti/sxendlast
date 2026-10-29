using JNPF.Common.Models.User;
using JNPF.DependencyInjection;
using JNPF.Systems.Entitys.Dto.Module;

namespace JNPF.OAuth.Dto;

/// <summary>
/// 当前客户信息输出.
/// </summary>
[SuppressSniffer]
public class CurrentUserOutput
{
    /// <summary>
    /// 用户信息.
    /// </summary>
    public UserInfoModel userInfo { get; set; }

    /// <summary>
    /// 菜单列表.
    /// </summary>
    public List<ModuleNodeOutput> menuList { get; set; }

    /// <summary>
    /// 权限列表.
    /// </summary>
    public List<PermissionModel> permissionList { get; set; }

    /// <summary>
    /// 系统配置信息.
    /// </summary>
    public SysConfigInfo sysConfigInfo { get; set; }
}

/// <summary>
/// 权限.
/// </summary>
[SuppressSniffer]
public class PermissionModel
{
    /// <summary>
    /// 模块ID.
    /// </summary>
    public string modelId { get; set; }

    /// <summary>
    /// 模块名称.
    /// </summary>
    public string moduleName { get; set; }

    /// <summary>
    /// 列.
    /// </summary>
    public List<FunctionalColumnAuthorizeModel> column { get; set; }

    /// <summary>
    /// 按钮.
    /// </summary>
    public List<FunctionalButtonAuthorizeModel> button { get; set; }

    /// <summary>
    /// 表单.
    /// </summary>
    public List<FunctionalFormAuthorizeModel> form { get; set; }

    /// <summary>
    /// 资源.
    /// </summary>
    public List<FunctionalResourceAuthorizeModel> resource { get; set; }
}

/// <summary>
/// 功能权限基类.
/// </summary>
[SuppressSniffer]
public class FunctionalAuthorizeBase
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    public string enCode { get; set; }
}

/// <summary>
/// 功能权限列.
/// </summary>
[SuppressSniffer]
public class FunctionalColumnAuthorizeModel : FunctionalAuthorizeBase
{
}

/// <summary>
/// 功能权限按钮.
/// </summary>
[SuppressSniffer]
public class FunctionalButtonAuthorizeModel : FunctionalAuthorizeBase
{
}

/// <summary>
/// 功能权限表单.
/// </summary>
[SuppressSniffer]
public class FunctionalFormAuthorizeModel : FunctionalAuthorizeBase
{
}

/// <summary>
/// 授权模块资源.
/// </summary>
[SuppressSniffer]
public class FunctionalResourceAuthorizeModel : FunctionalAuthorizeBase
{
}

/// <summary>
/// 系统配置信息.
/// </summary>
[SuppressSniffer]
public class SysConfigInfo
{
    /// <summary>
    /// 窗口标题.
    /// </summary>
    public string title { get; set; }

    /// <summary>
    /// 系统名称.
    /// </summary>
    public string sysName { get; set; }

    /// <summary>
    /// 系统版本.
    /// </summary>
    public string sysVersion { get; set; }

    /// <summary>
    /// 登录图标.
    /// </summary>
    public string loginIcon { get; set; }

    /// <summary>
    /// 版权信息.
    /// </summary>
    public string copyright { get; set; }

    /// <summary>
    /// 公司名称.
    /// </summary>
    public string companyName { get; set; }

    /// <summary>
    /// 导航图标.
    /// </summary>
    public string navigationIcon { get; set; }

    /// <summary>
    /// logo图标.
    /// </summary>
    public string workLogoIcon { get; set; }

    /// <summary>
    /// logo图标.
    /// </summary>
    public string logoIcon { get; set; }

    /// <summary>
    /// App图标.
    /// </summary>
    public string appIcon { get; set; }

    /// <summary>
    /// 项目地址url.
    /// </summary>
    public string jnpfDomain { get; set; }

    /// <summary>
    /// 默认视图.
    /// </summary>
    public string defaultView { get; set; }

    /// <summary>
    /// 显示农历.
    /// </summary>
    public bool showLunarCalendar { get; set; }

    /// <summary>
    /// 周第一天.
    /// </summary>
    public int firstDay { get; set; }

    /// <summary>
    /// 默认时长.
    /// </summary>
    public int duration { get; set; }

    /// <summary>
    /// 流程签收 0-关闭 1-开启.
    /// </summary>
    public int flowSign { get; set; }

    /// <summary>
    /// 流程办理 0-关闭 1-开启.
    /// </summary>
    public int flowTodo { get; set; }

    /// <summary>
    /// 委托范围 1:无附加条件、2:同一部门、3:同一岗位、6:同一公司.
    /// </summary>
    public int delegateScope { get; set; }

    /// <summary>
    /// 委托确认 0-关闭 1-开启.
    /// </summary>
    public int delegateAck { get; set; }

    /// <summary>
    /// 代理范围 1:无附加条件、2:同一部门、3:同一岗位、6:同一公司.
    /// </summary>
    public int proxyScope { get; set; }

    /// <summary>
    /// 代理确认 0-关闭 1-开启.
    /// </summary>
    public int proxyAck { get; set; }

    /// <summary>
    /// 组织层级设置.
    /// </summary>
    public int orgLevel { get; set; }

    /// <summary>
    /// 岗位层级设置.
    /// </summary>
    public int positionLevel { get; set; }

    /// <summary>
    /// 默认岗位.
    /// </summary>
    public int sysPositonName { get; set; }

    /// <summary>
    /// 岗位管理.
    /// </summary>
    public int positionSwitch { get; set; }

    /// <summary>
    /// 身份管理.
    /// </summary>
    public int identitySwitch { get; set; }

    /// <summary>
    /// 分组管理.
    /// </summary>
    public int groupSwitch { get; set; }

    /// <summary>
    /// 组织.
    /// </summary>
    public int orgNotCheck { get; set; }
    #region 密码策略
    /// <summary>
    /// 密码定期更新开关.
    /// </summary>
    public int passwordIsUpdatedRegularly { get; set; }

    /// <summary>
    /// 更新周期.
    /// </summary>
    public int updateCycle { get; set; }

    /// <summary>
    /// 提前N天提醒更新.
    /// </summary>
    public int updateInAdvance { get; set; }

    /// <summary>
    /// 密码强度限制开关.
    /// </summary>
    public int passwordStrengthLimit { get; set; }

    /// <summary>
    /// 最小长度开关.
    /// </summary>
    public int passwordLengthMin { get; set; }

    /// <summary>
    /// 密码最小长度限制.
    /// </summary>
    public int passwordLengthMinNumber { get; set; }

    /// <summary>
    /// 是否包含数字.
    /// </summary>
    public int containsNumbers { get; set; }

    /// <summary>
    /// 是否包含小写字母.
    /// </summary>
    public int includeLowercaseLetters { get; set; }

    /// <summary>
    /// 是否包含大写字母.
    /// </summary>
    public int includeUppercaseLetters { get; set; }

    /// <summary>
    /// 是否包含字符.
    /// </summary>
    public int containsCharacters { get; set; }

    /// <summary>
    /// 是否禁用旧密码开关.
    /// </summary>
    public int disableOldPassword { get; set; }

    /// <summary>
    /// 禁用旧密码个数.
    /// </summary>
    public int disableTheNumberOfOldPasswords { get; set; }

    /// <summary>
    /// 初始密码强制修改开关.
    /// </summary>
    public int mandatoryModificationOfInitialPassword { get; set; }
    #endregion
}

/// <summary>
/// 获取登录用户信息返回值.
/// </summary>
public class UserAllMenu
{
    /// <summary>
    /// 获取节点id.
    /// </summary>
    /// <returns></returns>
    public string id { get; set; }

    /// <summary>
    /// 获取节点父id.
    /// </summary>
    /// <returns></returns>
    public string parentId { get; set; }

    /// <summary>
    /// 是否有子级.
    /// </summary>
    public bool hasChildren { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 图标.
    /// </summary>
    public string icon { get; set; }

    /// <summary>
    /// 菜单地址.
    /// </summary>
    public string urlAddress { get; set; }

    /// <summary>
    /// 链接目标.
    /// </summary>
    public string linkTarget { get; set; }

    /// <summary>
    /// 子集.
    /// </summary>
    public List<UserAllMenu> children { get; set; }

    /// <summary>
    /// 菜单分类【1-类别、2-页面】.
    /// </summary>
    public int type { get; set; }

    public string propertyJson { get; set; }
    public string sortCode { get; set; }
    public string systemId { get; set; }
}