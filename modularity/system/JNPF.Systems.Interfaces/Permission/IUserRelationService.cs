using JNPF.Systems.Entitys.Dto.User;
using JNPF.Systems.Entitys.Permission;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;

namespace JNPF.Systems.Interfaces.Permission;

/// <summary>
/// 业务契约：用户关系.
/// </summary>
public interface IUserRelationService
{
    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">用户ID.</param>
    /// <returns></returns>
    Task Delete(string id);

    /// <summary>
    /// 创建用户关系.
    /// </summary>
    /// <param name="userId">用户ID.</param>
    /// <param name="ids">对象ID组.</param>
    /// <param name="relationType">关系类型(岗位-Position;角色-Role;组织-Organize;分组-Group;).</param>
    /// <returns></returns>
    List<UserRelationEntity> CreateUserRelation(string userId, string ids, string relationType);

    /// <summary>
    /// 根据表达式获取列表.
    /// </summary>
    /// <param name="userId">用户主键.</param>
    /// <returns></returns>
    [NonAction]
    List<UserRelationEntity> GetListByExp(Expression<Func<UserRelationEntity, bool>> expression);

    /// <summary>
    /// 获取用户(分页).
    /// </summary>
    /// <param name="userIds">用户ID组.</param>
    /// <param name="objIds">对象ID组.</param>
    /// <param name="pageInputBase">分页参数.</param>
    /// <returns></returns>
    dynamic GetUserPage(UserConditionInput input);

    /// <summary>
    /// 获取用户(分页,userIds纯用户id).
    /// </summary>
    /// <param name="userIds">用户ID组.</param>
    /// <param name="objIds">对象ID组.</param>
    /// <param name="pageInputBase">分页参数.</param>
    /// <returns></returns>
    dynamic GetUserListPage(UserConditionInput input);

    List<UserListOutput> GetUserList(UserConditionInput input);

    /// <summary>
    /// 新用户组件获取人员.
    /// </summary>
    /// <param name="Ids"></param>
    /// <returns></returns>
    List<string> GetUserId(List<string> Ids);
}