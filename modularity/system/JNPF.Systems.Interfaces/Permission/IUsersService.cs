using JNPF.Common.Models.User;
using JNPF.Systems.Entitys.Permission;
using System.Linq.Expressions;

namespace JNPF.Systems.Interfaces.Permission;

/// <summary>
/// 业务契约：用户信息.
/// </summary>
public interface IUsersService
{
    /// <summary>
    /// 获取用户信息 根据用户ID.
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="isEnabledMark">是否过滤启用</param>
    /// <returns></returns>
    UserEntity GetInfoByUserId(string userId, bool isEnabledMark = true);

    /// <summary>
    /// 获取用户直属主管.
    /// </summary>
    /// <param name="posId">岗位id.</param>
    /// <returns></returns>
    List<string> GetUserManager(string posId);

    /// <summary>
    /// 获取岗位责任人.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    string GetDutyUserId(string id);

    /// <summary>
    /// 获取用户列表.
    /// </summary>
    Task<List<UserEntity>> GetList(List<string> ids = null);

    /// <summary>
    /// 用户岗位.
    /// </summary>
    /// <param name="organizeId"></param>
    /// <returns></returns>
    Task<List<PositionInfoModel>> GetPosition(string organizeId);

    /// <summary>
    /// 表达式获取用户列表.
    /// </summary>
    /// <param name="expression">where 条件表达式.</param>
    /// <returns></returns>
    Task<List<UserEntity>> GetUserListByExp(Expression<Func<UserEntity, bool>> expression);

    /// <summary>
    /// 表达式获取指定字段的用户列表.
    /// </summary>
    /// <param name="expression">where 条件表达式.</param>
    /// <param name="select">select 选择字段表达式.</param>
    /// <returns></returns>
    Task<List<UserEntity>> GetUserListByExp(Expression<Func<UserEntity, bool>> expression, Expression<Func<UserEntity, UserEntity>> select);
}