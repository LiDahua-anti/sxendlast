using JNPF.Systems.Entitys.Dto.Organize;
using JNPF.Systems.Entitys.Dto.User;
using JNPF.Systems.Entitys.Permission;
using Microsoft.AspNetCore.Mvc;

namespace JNPF.Systems.Interfaces.Permission;

/// <summary>
/// 机构管理
/// 组织架构：公司》部门》岗位》用户
/// 版 本：V3.0.0
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021.06.07.
/// </summary>
public interface IOrganizeService
{
    /// <summary>
    /// 是否机构主管.
    /// </summary>
    /// <param name="userId">用户ID.</param>
    /// <returns></returns>
    Task<bool> GetIsManagerByUserId(string userId);

    /// <summary>
    /// 获取机构列表
    /// 提供给其他服务使用.
    /// </summary>
    /// <returns></returns>
    Task<List<OrganizeEntity>> GetListAsync();

    /// <summary>
    /// 获取公司列表
    /// 提供给其他服务使用.
    /// </summary>
    /// <returns></returns>
    Task<List<OrganizeEntity>> GetCompanyListAsync();

    /// <summary>
    /// 下属机构.
    /// </summary>
    /// <param name="organizeId">机构ID.</param>
    /// <param name="isAdmin">是否管理员.</param>
    /// <returns></returns>
    Task<string[]> GetSubsidiary(string organizeId, bool isAdmin);

    /// <summary>
    /// 下属机构.
    /// </summary>
    /// <param name="organizeId">组织ID.</param>
    /// <returns></returns>
    Task<List<string>> GetSubsidiary(string organizeId);

    /// <summary>
    /// 获取机构成员列表.
    /// </summary>
    /// <param name="organizeId">机构ID</param>
    /// <returns></returns>
    Task<List<OrganizeMemberListOutput>> GetOrganizeMemberList(string organizeId);

    /// <summary>
    /// 部门信息.
    /// </summary>
    Task<OrganizeEntity> GetInfoById(string Id);

    /// <summary>
    /// 获取组织下所有子组织.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<List<string>> GetChildOrgId(string id);

    /// <summary>
    /// 获取选中组织、岗位、角色、分组、用户基本信息.
    /// </summary>
    /// <param name="input">参数.</param>
    Task<dynamic> GetSelectedList(UserSelectedInput input);

    /// <summary>
    /// 处理组织树 名称.
    /// </summary>
    /// <returns></returns>
    [NonAction]
    List<OrganizeEntity> GetOrgListTreeName();
}