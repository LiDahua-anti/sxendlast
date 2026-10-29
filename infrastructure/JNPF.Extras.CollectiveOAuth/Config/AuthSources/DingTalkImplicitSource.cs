using JNPF.Extras.CollectiveOAuth.Enums;

namespace JNPF.Extras.CollectiveOAuth.Config;

/// <summary>
/// 钉钉扫码.
/// </summary>
public class DingTalkImplicitSource : IAuthSource
{
    public string authorize()
    {
        return "https://oapi.dingtalk.com/connect/oauth2/sns_authorize";
    }

    public string accessToken()
    {
        return "https://oapi.dingtalk.com/gettoken";
    }

    public string userInfo()
    {
        return "https://oapi.dingtalk.com/topapi/v2/user/getuserinfo";
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
        return DefaultAuthSourceEnum.DINGTALK.ToString();
    }
}