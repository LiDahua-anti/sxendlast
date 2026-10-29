using JNPF.Extras.CollectiveOAuth.Models;

namespace JNPF.Extras.CollectiveOAuth.Request;

/// <summary>
/// 免登录接口.
/// </summary>
public interface IImplicitRequest
{
    /// <summary>
    /// 获取登录授权链接.
    /// </summary>
    /// <returns></returns>
    string GetAuthLoginLink();

    /// <summary>
    /// 获取账号Id.
    /// </summary>
    /// <param name="authCallback"></param>
    /// <returns></returns>
    AuthResponse GetUserId(string code);
}