using JNPF.DependencyInjection;

namespace JNPF.Common.Models.User
{
    /// <summary>
    /// 登录者信息
    /// 版 本：V3.2.0
    /// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
    /// 作 者：JNPF开发平台组.
    /// </summary>
    [SuppressSniffer]
    public class UserInfoModel
    {
        /// <summary>
        /// 用户主键.
        /// </summary>
        public string userId { get; set; }

        /// <summary>
        /// 用户账户.
        /// </summary>
        public string userAccount { get; set; }

        /// <summary>
        /// 用户姓名.
        /// </summary>
        public string userName { get; set; }

        /// <summary>
        /// 用户头像.
        /// </summary>
        public string headIcon { get; set; }

        /// <summary>
        /// 用户性别.
        /// </summary>
        public string gender { get; set; }

        /// <summary>
        /// 座机号.
        /// </summary>
        public string landline { get; set; }

        /// <summary>
        /// 电话.
        /// </summary>
        public string telePhone { get; set; }

        /// <summary>
        /// 所属组织.
        /// </summary>
        public string organizeId { get; set; }

        /// <summary>
        /// 组织ids.
        /// </summary>
        public List<string> organizeIds { get; set; }

        /// <summary>
        /// 所属组织列表.
        /// </summary>
        public object organizeList { get; set; }

        /// <summary>
        /// 组织名称.
        /// </summary>
        public string organizeName { get; set; }

        /// <summary>
        /// 我的主管.
        /// </summary>
        public string managerId { get; set; }

        /// <summary>
        /// 岗位信息.
        /// </summary>
        public List<string> positionIds { get; set; }

        /// <summary>
        /// 所属岗位列表.
        /// </summary>
        public object positionList { get; set; }

        /// <summary>
        /// 岗位名称.
        /// </summary>
        public string positionName { get; set; }

        /// <summary>
        /// 岗位主键.
        /// </summary>
        public string positionId { get; set; }

        /// <summary>
        /// 角色主键.
        /// </summary>
        public string roleId { get; set; }

        /// <summary>
        /// 分组Ids.
        /// </summary>
        public List<string> groupIds { get; set; }

        /// <summary>
        /// 分组名称.
        /// </summary>
        public List<string> groupNames { get; set; }

        /// <summary>
        /// 登录时间.
        /// </summary>
        public DateTime? loginTime { get; set; }

        /// <summary>
        /// 登录IP地址.
        /// </summary>
        public string loginIPAddress { get; set; }

        /// <summary>
        /// 登录IP地址所在城市.
        /// </summary>
        public string loginIPAddressName { get; set; }

        /// <summary>
        /// 登录MAC地址.
        /// </summary>
        public string MACAddress { get; set; }

        /// <summary>
        /// 登录平台设备.
        /// </summary>
        public string loginPlatForm { get; set; }

        /// <summary>
        /// 上次登录开启（0:未开启,1:已开启）.
        /// </summary>
        /// <returns></returns>
        public int? prevLogin { get; set; }

        /// <summary>
        /// 上次登录时间.
        /// </summary>
        /// <returns></returns>
        public DateTime? prevLoginTime { get; set; }

        /// <summary>
        /// 上次登录IP地址.
        /// </summary>
        /// <returns></returns>
        public string prevLoginIPAddress { get; set; }

        /// <summary>
        /// 上次登录IP地址所在城市.
        /// </summary>
        /// <returns></returns>
        public string prevLoginIPAddressName { get; set; }

        /// <summary>
        /// 是否超级管理员.
        /// </summary>
        public bool isAdministrator { get; set; }

        /// <summary>
        /// 是否管理角色.
        /// </summary>
        public bool isManageRole { get; set; }

        /// <summary>
        /// 是否开发角色.
        /// </summary>
        public bool isDevRole { get; set; }

        /// <summary>
        /// 是否使用角色.
        /// </summary>
        public bool isUserRole { get; set; }

        /// <summary>
        /// 是否其他角色.
        /// </summary>
        public bool isOtherRole { get; set; }

        /// <summary>
        /// 过期时间.
        /// </summary>
        public TimeSpan? overdueTime { get; set; }

        /// <summary>
        /// 租户编码.
        /// </summary>
        public string tenantId { get; set; }

        /// <summary>
        /// 租户数据库类型.
        /// </summary>
        public string tenantDbType { get; set; }

        /// <summary>
        /// 直属主管.
        /// </summary>
        public string manager { get; set; }

        /// <summary>
        /// 手机号.
        /// </summary>
        public string mobilePhone { get; set; }

        /// <summary>
        /// 邮箱.
        /// </summary>
        public string email { get; set; }

        /// <summary>
        /// 生日.
        /// </summary>
        public DateTime? birthday { get; set; }

        /// <summary>
        /// 部门Id.
        /// </summary>
        public string departmentId { get; set; }

        /// <summary>
        /// 部门名称 结构树.
        /// </summary>
        public string departmentName { get; set; }

        /// <summary>
        /// 默认签名.
        /// </summary>
        public string signImg { get; set; }

        /// <summary>
        /// 默认签名id.
        /// </summary>
        public string signId { get; set; }

        /// <summary>
        /// 默认签名.
        /// </summary>
        public DateTime? changePasswordDate { get; set; }

        /// <summary>
        /// 系统id.
        /// </summary>
        public string systemId { get; set; }

        /// <summary>
        /// pc端身份.
        /// </summary>
        public string? standing { get; set; }

        /// <summary>
        /// app端身份.
        /// </summary>
        public string? appStanding { get; set; }

        /// <summary>
        /// 用户身份集合.
        /// </summary>
        public List<UserStanding> standingList { get; set; } = new List<UserStanding>();

        /// <summary>
        /// 当前应用编码.
        /// </summary>
        public string? systemCode { get; set; }

        /// <summary>
        /// 当前应用颜色.
        /// </summary>
        public string? systemColor { get; set; }

        /// <summary>
        /// 当前应用图标.
        /// </summary>
        public string? systemIcon { get; set; }

        /// <summary>
        /// 当前应用名称.
        /// </summary>
        public string? systemName { get; set; }

        /// <summary>
        /// 门户id.
        /// </summary>
        public string portalId { get; set; }

        /// <summary>
        /// app门户id.
        /// </summary>
        public string appPortalId { get; set; }

        /// <summary>
        /// 文件服务token.
        /// </summary>
        public string securityKey { get; set; }

        /// <summary>
        /// 偏好设置.
        /// </summary>
        public string preferenceJson { get; set; }

        /// <summary>
        /// 是否有后台权限.
        /// </summary>
        public bool hasBackend { get; set; }

        /// <summary>
        /// 是否有流程权限.
        /// </summary>
        public int workflowEnabled { get; set; }
    }
}