using DingTalk.Api;
using DingTalk.Api.Request;
using JNPF.Extras.CollectiveOAuth.Config;
using JNPF.Extras.CollectiveOAuth.Enums;
using JNPF.Extras.CollectiveOAuth.Models;
using JNPF.Extras.CollectiveOAuth.Utils;

namespace JNPF.Extras.CollectiveOAuth.Request;

public class DingTalkImplicitRequest : DefaultImplicitRequest
{
    public DingTalkImplicitRequest(ClientConfig config) : base(config, new DingTalkImplicitSource())
    {
    }

    public override string GetAuthLoginLink()
    {
        return UrlBuilder.fromBaseUrl(this.source.authorize())
                .queryParam("appid", this.config.clientId)
                .queryParam("redirect_uri", this.config.redirectUri)
                .queryParam("response_type", "code")
                .queryParam("scope", "snsapi_auth")
                .queryParam("state", "STATE")
                .build();
    }

    public override AuthResponse GetUserId(string code)
    {
        try
        {
            var client = new DefaultDingTalkClient("https://oapi.dingtalk.com/sns/getuserinfo_bycode");
            var req = new OapiSnsGetuserinfoBycodeRequest();
            req.TmpAuthCode = code;
            var res = client.Execute(req, config.clientId, config.clientSecret);
            checkResponse(res.Body.parseObject());
            var userInfo = res.UserInfo;

            var authUser = new AuthUser();
            authUser.uuid = userInfo.Unionid;
            authUser.nickname = userInfo.Nick;
            authUser.username = userInfo.Nick;
            authUser.token = new AuthToken();
            authUser.token.openId = userInfo.Openid;
            authUser.token.unionId = userInfo.Unionid;

            return new AuthResponse(Convert.ToInt32(AuthResponseStatus.SUCCESS), null, authUser);
        }
        catch (Exception e)
        {
            return this.responseError(e);
        }
    }

    /**
     * 校验请求结果
     *
     * @param response 请求结果
     * @return 如果请求结果正常，则返回JSONObject
     */
    private void checkResponse(Dictionary<string, object> dic)
    {
        if (dic.ContainsKey("errcode") && dic.getInt32("errcode") != 0)
        {
            throw new Exception($"errcode: {dic.getString("errcode")}, errmsg: {dic.getString("errmsg")}");
        }
    }

    /**
     * 处理{@link AuthDefaultRequest#login(AuthCallback)} 发生异常的情况，统一响应参数
     *
     * @param e 具体的异常
     * @return AuthResponse
     */
    private AuthResponse responseError(Exception e)
    {
        int errorCode = Convert.ToInt32(AuthResponseStatus.FAILURE);
        string errorMsg = e.Message;
        return new AuthResponse(errorCode, errorMsg);
    }
}