using JNPF.Extras.CollectiveOAuth.Config;
using JNPF.Extras.CollectiveOAuth.Enums;
using JNPF.Extras.CollectiveOAuth.Models;
using JNPF.Extras.CollectiveOAuth.Utils;

namespace JNPF.Extras.CollectiveOAuth.Request;

public class QywxImplicitRequest : DefaultImplicitRequest
{
    public QywxImplicitRequest(ClientConfig config) : base(config, new QywxImplicitSource())
    {
    }

    public override string GetAuthLoginLink()
    {
        return UrlBuilder.fromBaseUrl(this.source.authorize())
                .queryParam("appid", this.config.clientId)
                .queryParam("redirect_uri", this.config.redirectUri)
                .queryParam("response_type", "code")
                .queryParam("scope", "snsapi_privateinfo")
                .queryParam("agentid", this.config.agentId)
                .queryParam("state", "STATE#wechat_redirect")
                .build();
    }

    public override AuthResponse GetUserId(string code)
    {
        try
        {
            var tokenUrl = UrlBuilder.fromBaseUrl(source.accessToken())
                .queryParam("corpid", config.clientId)
                .queryParam("corpsecret", config.clientSecret)
                .build();
            var tokenResponse = HttpUtils.RequestGet(tokenUrl);
            var tokenData = tokenResponse.parseObject();
            this.checkResponse(tokenData);

            var url = UrlBuilder.fromBaseUrl(this.source.userInfo())
                .queryParam("access_token", tokenData["access_token"])
                .queryParam("code", code)
                .build();

            var response = HttpUtils.RequestGet(url);
            var data = response.parseObject();
            this.checkResponse(data);

            var authUser = new AuthUser();
            authUser.uuid = data.getString("userid");

            authUser.originalUserStr = response;
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