using JNPF.Common.Const;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace JNPF.Common.Core.Attribute;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class PermissionAuthorizeAttribute : ActionFilterAttribute
{
    /// <summary>
    /// 验证参数字段值.
    /// </summary>
    public string ResourceIdParam { get; set; }

    /// <summary>
    /// 菜单编码.
    /// </summary>
    public string ModuleCode { get; set; }

    /// <summary>
    /// 验证类型.
    /// </summary>
    public string type { get; set; } = string.Empty;

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var isPass = true;
        // 管理员跳过判断
        if (App.User.FindFirst(ClaimConst.CLAINMADMINISTRATOR)?.Value != ((int)AccountType.Administrator).ToString())
        {
            var userId = App.User.FindFirst(ClaimConst.CLAINMUSERID)?.Value;
            isPass = Vertical(context, userId) && Standard(context, userId);
        }

        if (isPass)
        {
            await next();
        }
        else
        {
            context.Result = new ForbidResult();
            return;
        }
    }

    /// <summary>
    /// 垂直鉴权.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    private bool Vertical(ActionExecutingContext context, string userId)
    {
        var enCodeList = new List<string>();
        if (userId.IsNotEmptyOrNull())
        {
            var _repository = context.HttpContext.RequestServices.GetService<ISqlSugarRepository<UserEntity>>();
            var posIdList = _repository.AsSugarClient().Queryable<UserRelationEntity>()
           .Where(x => (x.UserId.Equals(userId) && x.ObjectType.Equals(CommonConst.RelationObjectPosition)))
           .Select(x => x.ObjectId).ToList();

            var relationList = _repository.AsSugarClient().Queryable<RoleRelationEntity>()
                .Where(x => (x.ObjectId.Equals(userId) || posIdList.Contains(x.ObjectId)) && !x.ObjectType.Equals(CommonConst.RelationObjectOrganize))
                .Select(x => x.RoleId).ToList();

            relationList.AddRange(posIdList);
            var menuIds = _repository.AsSugarClient().Queryable<ModuleEntity>().Where(x => x.EnCode == ModuleCode && x.DeleteMark == null).Select(x => x.Id).ToList();
            return _repository.AsSugarClient().Queryable<AuthorizeEntity>().Any(x => relationList.Contains(x.ObjectId) && menuIds.Contains(x.ItemId));
        }
        return false;
    }

    /// <summary>
    /// 水平鉴权.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    private bool Standard(ActionExecutingContext context, string userId)
    {
        if (ResourceIdParam.IsNotEmptyOrNull())
        {
            var targetId = context.HttpContext?.Request.RouteValues[ResourceIdParam]?.ToString();
            if (targetId.IsNotEmptyOrNull() && userId.IsNotEmptyOrNull())
            {
                switch (type)
                {
                    case "user":
                        // 此处根据类型编写校验逻辑代码
                        return true;
                    default:
                        return targetId == userId;
                }
            }
        }
        return true;
    }
}
