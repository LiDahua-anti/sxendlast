using JNPF.Extras.CollectiveOAuth.Cache;
using JNPF.Extras.CollectiveOAuth.Config;
using JNPF.Extras.CollectiveOAuth.Models;

namespace JNPF.Extras.CollectiveOAuth.Request;

public partial class DefaultImplicitRequest : IImplicitRequest
{
    protected ClientConfig config;
    protected IAuthSource source;
    protected IAuthStateCache authStateCache { set; get; }

    public DefaultImplicitRequest(ClientConfig config, IAuthSource source)
    {
        this.config = config;
        this.source = source;
        this.authStateCache = new DefaultAuthStateCache();
    }

    /// <summary>
    /// 获取登录授权链接.
    /// </summary>
    /// <returns></returns>
    public virtual string GetAuthLoginLink()
    {
        return null;
    }

    /// <summary>
    /// 获取账号Id.
    /// </summary>
    /// <param name="authCallback"></param>
    /// <returns></returns>
    public virtual AuthResponse GetUserId(string code)
    {
        return null;
    }

}