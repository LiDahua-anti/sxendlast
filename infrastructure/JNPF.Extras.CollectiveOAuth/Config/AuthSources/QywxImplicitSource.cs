using JNPF.Extras.CollectiveOAuth.Enums;

namespace JNPF.Extras.CollectiveOAuth.Config;

/// <summary>
/// 企业微信扫码.
/// </summary>
public class QywxImplicitSource : IAuthSource
{
    public string authorize()
    {
        return "https://open.weixin.qq.com/connect/oauth2/authorize";
    }

    public string accessToken()
    {
        return "https://qyapi.weixin.qq.com/cgi-bin/gettoken";
    }

    public string userInfo()
    {
        return "https://qyapi.weixin.qq.com/cgi-bin/auth/getuserinfo";
    }

    public string revoke()
    {
        throw new System.NotImplementedException();
    }

    public string refresh()
    {
        throw new System.NotImplementedException();
    }

    public string getName()
    {
        return DefaultAuthSourceEnum.WECHAT_ENTERPRISE.ToString();
    }
}