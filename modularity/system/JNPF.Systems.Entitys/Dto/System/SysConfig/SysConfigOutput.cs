using JNPF.DependencyInjection;
using JNPF.JsonSerialization;
using Newtonsoft.Json;

namespace JNPF.Systems.Entitys.Dto.SysConfig;

/// <summary>
/// 系统配置输出.
/// </summary>
[SuppressSniffer]
public class SysConfigOutput
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
    /// 系统描述.
    /// </summary>
    public string sysDescription { get; set; }

    /// <summary>
    /// 系统版本.
    /// </summary>
    public string sysVersion { get; set; }

    /// <summary>
    /// 版权信息.
    /// </summary>
    public string copyright { get; set; }

    /// <summary>
    /// 公司名称.
    /// </summary>
    public string companyName { get; set; }

    /// <summary>
    /// 公司简称.
    /// </summary>
    public string companyCode { get; set; }

    /// <summary>
    /// 公司地址.
    /// </summary>
    public string companyAddress { get; set; }

    /// <summary>
    /// 公司法人.
    /// </summary>
    public string companyContacts { get; set; }

    /// <summary>
    /// 公司电话.
    /// </summary>
    public string companyTelePhone { get; set; }

    /// <summary>
    /// 公司邮箱.
    /// </summary>
    public string companyEmail { get; set; }

    /// <summary>
    /// 单一登录方式 （1：后登录踢出先登录 2：同时登录）.
    /// </summary>
    public int singleLogin { get; set; }

    /// <summary>
    /// 超出登出.
    /// </summary>
    public string tokenTimeout { get; set; }

    /// <summary>
    /// 是否开启上次登录提醒.
    /// </summary>
    [JsonConverter(typeof(BoolJsonConverter))]
    public bool lastLoginTimeSwitch { get; set; }

    /// <summary>
    /// 是否开启白名单验证.
    /// </summary>
    [JsonConverter(typeof(BoolJsonConverter))]
    public bool whitelistSwitch { get; set; }

    /// <summary>
    /// 白名单.
    /// </summary>
    public string whiteListIp { get; set; }

    /// <summary>
    /// POP3服务主机地址.
    /// </summary>
    public string emailPop3Host { get; set; }

    /// <summary>
    /// POP3服务端口.
    /// </summary>
    public int emailPop3Port { get; set; }

    /// <summary>
    /// SMTP服务主机地址.
    /// </summary>
    public string emailSmtpHost { get; set; }

    /// <summary>
    /// SMTP服务主端口.
    /// </summary>
    public int emailSmtpPort { get; set; }

    /// <summary>
    /// 邮件显示名称.
    /// </summary>
    public string emailSenderName { get; set; }

    /// <summary>
    /// 邮箱账户.
    /// </summary>
    public string emailAccount { get; set; }

    /// <summary>
    /// 邮箱密码.
    /// </summary>
    public string emailPassword { get; set; }

    /// <summary>
    /// 是否开启SSL服务登录.
    /// </summary>
    [JsonConverter(typeof(BoolJsonConverter))]
    public bool emailSsl { get; set; }

    /// <summary>
    /// 授权密钥.
    /// </summary>
    public string registerKey { get; set; }

    /// <summary>
    /// 最后登录时间.
    /// </summary>
    public string lastLoginTime { get; set; }

    /// <summary>
    /// 分页数.
    /// </summary>
    public string pageSize { get; set; }

    /// <summary>
    /// 系统主题.
    /// </summary>
    public string sysTheme { get; set; }

    /// <summary>
    /// 厂商.
    /// </summary>
    public string smsCompany { get; set; }

    /// <summary>
    /// 签名内容.
    /// </summary>
    public string smsSignName { get; set; }

    /// <summary>
    /// sms用户编号.
    /// </summary>
    public string smsKeyId { get; set; }

    /// <summary>
    /// sms密钥.
    /// </summary>
    public string smsKeySecret { get; set; }

    /// <summary>
    /// 模板编号.
    /// </summary>
    public string smsTemplateId { get; set; }

    /// <summary>
    /// 应用编号.
    /// </summary>
    public string smsAppId { get; set; }

    /// <summary>
    /// 错误密码次数.
    /// </summary>
    public int passwordErrorsNumber { get; set; } = 0;

    /// <summary>
    /// 错误策略（1：账号锁定，2：延时登录）.
    /// </summary>
    public int lockType { get; set; } = 0;

    /// <summary>
    /// 延时登录时间(分钟).
    /// </summary>
    public int lockTime { get; set; } = 10;

    /// <summary>
    /// 是否开启验证码（0：不启用，1：启用）.
    /// </summary>
    [JsonConverter(typeof(BoolJsonConverter))]
    public bool enableVerificationCode { get; set; }

    /// <summary>
    /// 验证码位数.
    /// </summary>
    public int verificationCodeNumber { get; set; } = 3;

    /// <summary>
    /// 访问域名.
    /// </summary>
    public string domain { get; set; } = "sms.tencentcloudapi.com";

    /// <summary>
    /// 支持地域.
    /// </summary>
    public string region { get; set; } = "ap-guangzhou";

    /// <summary>
    /// 短信版本.
    /// </summary>
    public string version { get; set; }

    /// <summary>
    /// 登录图标.
    /// </summary>
    public string loginIcon { get; set; }

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
    /// 新用户密码.
    /// </summary>
    public string newUserDefaultPassword { get; set; }

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
    public string sysPositionName { get; set; }

    #region 短信

    /// <summary>
    /// 阿里云AccessKey.
    /// </summary>
    public string aliAccessKey { get; set; }

    /// <summary>
    ///  阿里云Secret.
    /// </summary>
    public string aliSecret { get; set; }

    /// <summary>
    /// 腾讯云SecretId.
    /// </summary>
    public string tencentSecretId { get; set; }

    /// <summary>
    /// 腾讯云SecretKey.
    /// </summary>
    public string tencentSecretKey { get; set; }

    /// <summary>
    /// 腾讯云AppId.
    /// </summary>
    public string tencentAppId { get; set; }

    /// <summary>
    /// 腾讯云AppKey.
    /// </summary>
    public string tencentAppKey { get; set; }

    #endregion

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

    #region 日程设置

    /// <summary>
    /// 默认视图.
    /// </summary>
    public string defaultView { get; set; } = "dayGridMonth";

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
    public int duration { get; set; } = 60;

    #endregion

    #region 流程设置
    /// <summary>
    /// 审批链接时效性.
    /// </summary>
    public int linkTime { get; set; }

    /// <summary>
    /// 审批链接被点击后失效.
    /// </summary>
    public int isClick { get; set; }

    /// <summary>
    /// 链接点击几次后失效.
    /// </summary>
    public int unClickNum { get; set; }

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
    /// 加签层级.
    /// </summary>
    public int addSignLevel { get; set; }
    #endregion
}