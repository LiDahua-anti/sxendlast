using JNPF.DependencyInjection;
using JNPF.JsonSerialization;
using Newtonsoft.Json;

namespace JNPF.Systems.Entitys.Dto.SysConfig;

/// <summary>
/// 保存同步配置输入.
/// </summary>
[SuppressSniffer]
public class SaveSocialsInput
{
    #region 钉钉

    /// <summary>
    /// 应用凭证.
    /// </summary>
    public string dingAgentId { get; set; }

    /// <summary>
    /// 同步组织.
    /// </summary>
    public string dingDepartment { get; set; }

    /// <summary>
    /// 企业号Id.
    /// </summary>
    public string dingSynAppKey { get; set; }

    /// <summary>
    /// 凭证密钥.
    /// </summary>
    public string dingSynAppSecret { get; set; }

    /// <summary>
    /// 启用同步钉钉组织（0：不启用，1：启用）.
    /// </summary>
    [JsonConverter(typeof(BoolJsonConverter))]
    public bool dingSynIsSynOrg { get; set; }

    /// <summary>
    /// 启用同步钉钉用户（0：不启用，1：启用）.
    /// </summary>
    [JsonConverter(typeof(BoolJsonConverter))]
    public bool dingSynIsSynUser { get; set; }

    #endregion

    #region 企微

    /// <summary>
    /// 应用凭证.
    /// </summary>
    public string qyhAgentId { get; set; }

    /// <summary>
    /// 凭证密钥.
    /// </summary>
    public string qyhAgentSecret { get; set; }

    /// <summary>
    /// 企业号Id.
    /// </summary>
    public string qyhCorpId { get; set; }

    /// <summary>
    /// 同步密钥.
    /// </summary>
    public string qyhCorpSecret { get; set; }

    /// <summary>
    /// 同步组织.
    /// </summary>
    public string qyhDepartment { get; set; }

    /// <summary>
    /// 启用同步企微组织（0：不启用，1：启用）.
    /// </summary>
    [JsonConverter(typeof(BoolJsonConverter))]
    public bool qyhIsSynOrg { get; set; }

    /// <summary>
    /// 启用同步企微用户（0：不启用，1：启用）.
    /// </summary>
    [JsonConverter(typeof(BoolJsonConverter))]
    public bool qyhIsSynUser { get; set; }

    #endregion
}
