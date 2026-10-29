using JNPF.Common.Const;
using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Tenant;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Extras.Thirdparty.DingDing;
using JNPF.Extras.Thirdparty.WeChat;
using JNPF.FriendlyException;
using JNPF.LinqBuilder;
using JNPF.Systems.Entitys.Dto.Organize;
using JNPF.Systems.Entitys.Dto.SynThirdInfo;
using JNPF.Systems.Entitys.Dto.SysConfig;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.System;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SqlSugar;
using System.Linq.Expressions;

namespace JNPF.Systems.System;

/// <summary>
/// 第三方同步
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "SynThirdInfo", Order = 210)]
[Route("api/system/[controller]")]
public class SynThirdInfoService : ISynThirdInfoService, IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<SynThirdInfoEntity> _repository;

    /// <summary>
    /// 系统配置服务.
    /// </summary>
    private readonly ISysConfigService _sysConfigService;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 单据.
    /// </summary>
    private readonly IBillRullService _billRuleService;

    /// <summary>
    /// 租户管理.
    /// </summary>
    private readonly ITenantManager _tenantManager;

    /// <summary>
    /// 多租户配置选项.
    /// </summary>
    private readonly TenantOptions _tenant;

    /// <summary>
    /// 初始化一个<see cref="SynThirdInfoService"/>类型的新实例.
    /// </summary>
    public SynThirdInfoService(
        ISqlSugarRepository<SynThirdInfoEntity> repository,
        ISysConfigService sysConfigService,
        IUserManager userManager,
        IBillRullService billRuleService,
        IOptions<TenantOptions> tenantOptions,
        ITenantManager tenantManager)
    {
        _repository = repository;
        _sysConfigService = sysConfigService;
        _userManager = userManager;
        _billRuleService = billRuleService;
        _tenant = tenantOptions.Value;
        _tenantManager = tenantManager;
    }

    #region Get

    /// <summary>
    /// 列表.
    /// </summary>
    /// <param name="thirdType">请求参数.</param>
    /// <returns></returns>
    [HttpGet("getSynThirdTotal/{thirdType}")]
    public async Task<dynamic> GetList(int thirdType)
    {
        var sysConfig = (await _sysConfigService.GetInfoByCategory("SocialsConfig")).ToObject<SaveSocialsInput>();
        var departmentId = thirdType.Equals(1) ? sysConfig.qyhDepartment : sysConfig.dingDepartment;
        var whereLambda = LinqExpression.And<SynThirdInfoEntity>();
        whereLambda = whereLambda.And(x => x.ThirdType == thirdType);
        return await GetListByThirdType(whereLambda, "用户", "组织", departmentId);
    }

    /// <summary>
    /// 钉钉同步组织.
    /// </summary>
    /// <returns></returns>
    [HttpGet("synAllOrganizeSysToDing")]
    public async Task<dynamic> SynAllOrganizeSysToDing(string type, string departmentId)
    {
        if (departmentId.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.D9016);
        await UpConfigDepartmentId(departmentId, 2);
        var sysConfig = (await _sysConfigService.GetInfoByCategory("SocialsConfig")).ToObject<SaveSocialsInput>();
        var flag = "0".Equals(type) ? await SynData(2, 1, sysConfig) : await SynSysData(2, 1, sysConfig);
        var whereLambda = LinqExpression.And<SynThirdInfoEntity>();
        whereLambda = whereLambda.And(x => x.ThirdType == 2 && x.DataType == 1);
        return await GetListByThirdType(whereLambda, "组织", "组织", sysConfig.dingDepartment);
    }

    /// <summary>
    /// 企业微信同步组织.
    /// </summary>
    /// <returns></returns>
    [HttpGet("synAllOrganizeSysToQy")]
    public async Task<dynamic> SynAllOrganizeSysToQy(string type, string departmentId)
    {
        if (departmentId.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.D9016);
        await UpConfigDepartmentId(departmentId, 1);
        var sysConfig = (await _sysConfigService.GetInfoByCategory("SocialsConfig")).ToObject<SaveSocialsInput>();
        var flag = "0".Equals(type) ? await SynData(1, 1, sysConfig) : await SynSysData(1, 1, sysConfig);
        var whereLambda = LinqExpression.And<SynThirdInfoEntity>();
        whereLambda = whereLambda.And(x => x.ThirdType == 1 && x.DataType == 1);
        return await GetListByThirdType(whereLambda, "组织", "组织", sysConfig.qyhDepartment);
    }

    /// <summary>
    /// 钉钉同步用户.
    /// </summary>
    /// <returns></returns>
    [HttpGet("synAllUserSysToDing")]
    public async Task<dynamic> SynAllUserSysToDing(string type, string departmentId)
    {
        await UpConfigDepartmentId(departmentId, 2);
        var sysConfig = (await _sysConfigService.GetInfoByCategory("SocialsConfig")).ToObject<SaveSocialsInput>();
        var flag = "0".Equals(type) ? await SynData(2, 2, sysConfig) : await SynSysData(2, 2, sysConfig);
        var whereLambda = LinqExpression.And<SynThirdInfoEntity>();
        whereLambda = whereLambda.And(x => x.ThirdType == 2 && x.DataType == 2);
        return await GetListByThirdType(whereLambda, "用户", "用户", sysConfig.dingDepartment);
    }

    /// <summary>
    /// 企业微信同步用户.
    /// </summary>
    /// <returns></returns>
    [HttpGet("synAllUserSysToQy")]
    public async Task<dynamic> SynAllUserSysToQy(string type, string departmentId)
    {
        await UpConfigDepartmentId(departmentId, 1);
        var sysConfig = (await _sysConfigService.GetInfoByCategory("SocialsConfig")).ToObject<SaveSocialsInput>();
        var flag = "0".Equals(type) ? await SynData(1, 2, sysConfig) : await SynSysData(1, 2, sysConfig);
        var whereLambda = LinqExpression.And<SynThirdInfoEntity>();
        whereLambda = whereLambda.And(x => x.ThirdType == 1 && x.DataType == 2);
        return await GetListByThirdType(whereLambda, "用户", "用户", sysConfig.qyhDepartment);
    }

    /// <summary>
    /// 解除同步.
    /// </summary>
    /// <returns></returns>
    [HttpGet("clearAllSyn")]
    public async Task ClearAllSyn(int type)
    {
        await UpConfigDepartmentId(string.Empty, type);
        await _repository.AsDeleteable().Where(x => x.ThirdType.Equals(type)).ExecuteCommandAsync();
    }

    /// <summary>
    /// 获取第三方同步未成功列表.
    /// </summary>
    /// <returns></returns>
    [HttpGet("getHandleNum")]
    public async Task<dynamic> GetHandleNum([FromQuery] SynThirdHandleNumInput input)
    {
        var list = await _repository.AsQueryable()
            .Where(x => x.DeleteMark == null)
            .WhereIF(input.thirdType.IsNotEmptyOrNull(), x => x.ThirdType.Equals(input.thirdType))
            .WhereIF(input.type.IsNotEmptyOrNull(), x => x.DataType.Equals(input.type))
            .WhereIF(input.resultType.IsNotEmptyOrNull(), x => x.EnabledMark.Equals(input.resultType))
            .Select(x => new SynThirdHandleNumOutput
            {
                id = x.Id,
                systemObjectName = x.SysObjId,
                thirdName = x.ThirdName,
                description = x.Description
            })
            .ToPagedListAsync(input.currentPage, input.pageSize);

        if (input.type.Equals(1))
        {
            var allOrgList = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.DeleteMark == null).ToListAsync();
            foreach (var item in list.list)
            {
                var org = allOrgList.Find(x => x.Id.Equals(item.systemObjectName));
                if (org.IsNotEmptyOrNull()) item.systemObjectName = org.FullName;
            }
        }
        else
        {
            var allUserList = await _repository.AsSugarClient().Queryable<UserEntity>().Where(x => x.DeleteMark == null).ToListAsync();
            foreach (var item in list.list)
            {
                var user = allUserList.Find(x => x.Id.Equals(item.systemObjectName));
                if (user.IsNotEmptyOrNull()) item.systemObjectName = user.RealName;
            }
        }

        if (input.keyword.IsNotEmptyOrNull())
        {
            list.list = list.list.Where(x => (x.systemObjectName.IsNotEmptyOrNull() && x.systemObjectName.Contains(input.keyword)) || (x.thirdName.IsNotEmptyOrNull() && x.thirdName.Contains(input.keyword))).ToList();
        }

        return PageResult<SynThirdHandleNumOutput>.SqlSugarPageResult(list);
    }

    #endregion

    #region Method

    /// <summary>
    /// 获取同步数据.
    /// </summary>
    /// <param name="whereLambda">条件Lambda表达式.</param>
    /// <param name="synType1"></param>
    /// <param name="synType2"></param>
    /// <param name="departmentId"></param>
    /// <returns></returns>
    private async Task<dynamic> GetListByThirdType(Expression<Func<SynThirdInfoEntity, bool>> whereLambda, string synType1, string synType2, string departmentId)
    {
        var synThirdInfoList = await _repository.AsQueryable().Where(whereLambda).ToListAsync();
        var orgList = new List<OrganizeEntity>();
        var userList = new List<UserEntity>();
        if (departmentId.IsNotEmptyOrNull())
        {
            orgList = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.DeleteMark == null && x.OrganizeIdTree.Contains(departmentId)).ToListAsync();
            var userIds = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => orgList.Select(o => o.Id).Contains(x.ObjectId)).Select(x => x.UserId).Distinct().ToListAsync();
            userList = await _repository.AsSugarClient().Queryable<UserEntity>().Where(x => x.DeleteMark == null && userIds.Contains(x.Id)).ToListAsync();
        }

        if (synType1.Equals(synType2))
        {
            return new SynThirdInfoOutput()
            {
                synType = synType1,
                recordTotal = synType1.Equals("组织") ? orgList.Count : userList.Count,
                synDate = synThirdInfoList.Select(x => x.LastModifyTime).ToList().Max().IsEmpty() ? synThirdInfoList.Select(x => x.CreatorTime).ToList().Max() : synThirdInfoList.Select(x => x.LastModifyTime).ToList().Max(),
                synFailCount = synThirdInfoList.FindAll(x => x.EnabledMark == 2).Count,
                synSuccessCount = synThirdInfoList.FindAll(x => x.EnabledMark == 1).Count,
                unSynCount = synThirdInfoList.FindAll(x => x.EnabledMark == 0).Count,
            };
        }
        else
        {
            var output = new List<SynThirdInfoOutput>();
            var synUserList = synThirdInfoList.FindAll(x => x.DataType == 2);
            var synOrgList = synThirdInfoList.FindAll(x => x.DataType == 1);
            output.Add(new SynThirdInfoOutput()
            {
                synType = synType2,
                recordTotal = synType2.Equals("组织") ? orgList.Count : userList.Count,
                synDate = synOrgList.Select(x => x.LastModifyTime).ToList().Max().IsEmpty() ? synOrgList.Select(x => x.CreatorTime).ToList().Max() : synOrgList.Select(x => x.LastModifyTime).ToList().Max(),
                synFailCount = synOrgList.FindAll(x => x.EnabledMark == 2).Count,
                synSuccessCount = synOrgList.FindAll(x => x.EnabledMark == 1).Count,
                unSynCount = synOrgList.FindAll(x => x.EnabledMark == 0).Count,
            });
            output.Add(new SynThirdInfoOutput()
            {
                synType = synType1,
                recordTotal = synType1.Equals("组织") ? orgList.Count : userList.Count,
                synDate = synUserList.Select(x => x.LastModifyTime).ToList().Max().IsEmpty() ? synUserList.Select(x => x.CreatorTime).ToList().Max() : synUserList.Select(x => x.LastModifyTime).ToList().Max(),
                synFailCount = synUserList.FindAll(x => x.EnabledMark == 2).Count,
                synSuccessCount = synUserList.FindAll(x => x.EnabledMark == 1).Count,
                unSynCount = synUserList.FindAll(x => x.EnabledMark == 0).Count,
            });
            return output;
        }
    }

    /// <summary>
    /// 同步数据(同步到第三方).
    /// </summary>
    /// <param name="thirdType"></param>
    /// <param name="dataType"></param>
    /// <param name="sysConfig"></param>
    /// <returns></returns>
    private async Task<int> SynData(int thirdType, int dataType, SaveSocialsInput sysConfig)
    {
        try
        {
            var departmentId = thirdType.Equals(1) ? sysConfig.qyhDepartment : sysConfig.dingDepartment;
            var synThirdInfo = await _repository.AsQueryable().Where(x => x.ThirdType == thirdType).ToListAsync();
            var orgList = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.DeleteMark == null && x.OrganizeIdTree.Contains(departmentId)).ToListAsync();
            if (dataType == 2)
            {
                var userRelationList = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => orgList.Select(x => x.Id).Contains(x.ObjectId)).ToListAsync();
                var userList = await _repository.AsSugarClient().Queryable<UserEntity>().Where(x => x.DeleteMark == null && userRelationList.Select(s => s.UserId).Contains(x.Id)).ToListAsync();
                foreach (var user in userList)
                {
                    var userRelation = userRelationList.Where(x => x.UserId.Equals(user.Id)).Select(x => x.ObjectId).ToList();
                    user.OrganizeId = string.Join(",", userRelation);
                }

                await SynUser(thirdType, dataType, sysConfig, userList);
            }
            else
            {
                var orgTree = orgList.Adapt<List<OrganizeListOutput>>().ToTree("-1");
                await SynDep(thirdType, dataType, sysConfig, orgTree);
            }

            return 1;
        }
        catch (Exception)
        {
            return 0;
        }
    }

    /// <summary>
    /// 同步数据(同步到系统).
    /// </summary>
    /// <param name="thirdType"></param>
    /// <param name="dataType"></param>
    /// <param name="sysConfig"></param>
    /// <returns></returns>
    private async Task<int> SynSysData(int thirdType, int dataType, SaveSocialsInput sysConfig)
    {
        try
        {
            if (dataType == 2)
                await SynSysUser(thirdType, dataType, sysConfig);
            else
                await SynSysDep(thirdType, dataType, sysConfig);
            return 1;
        }
        catch (Exception)
        {
            return 0;
        }
    }

    /// <summary>
    /// 删除第三方数据.
    /// </summary>
    /// <param name="thirdType"></param>
    /// <param name="dataType"></param>
    /// <param name="sysConfig"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    [NonAction]
    public async Task DelSynData(int thirdType, int dataType, SaveSocialsInput sysConfig, string id)
    {
        string msg = string.Empty;
        try
        {
            var synInfo = await _repository.GetFirstAsync(x => x.ThirdType == thirdType && x.DataType == dataType && x.SysObjId == id);
            if (synInfo.IsNullOrEmpty() || synInfo.ThirdObjId.IsNullOrEmpty())
                throw Oops.Oh(ErrorCode.D9004);
            if (thirdType == 1)
            {
                var weChat = new WeChatUtil(sysConfig.qyhCorpId, sysConfig.qyhAgentSecret);
                if (dataType == 2)
                {
                    weChat.DeleteMember(synInfo.ThirdObjId);
                }
                else
                {
                    if (!synInfo.ThirdObjId.Equals("1"))
                        weChat.DeleteDepartment(synInfo.ThirdObjId.ParseToInt(), ref msg);
                }
            }
            else
            {
                var ding = new DingUtil(sysConfig.dingSynAppKey, sysConfig.dingSynAppSecret);
                if (dataType == 2)
                {
                    ding.DeleteUser(new DingUserParameter() { Userid = synInfo.ThirdObjId }, ref msg);
                }
                else
                {
                    if (!synInfo.ThirdObjId.Equals("1"))
                        ding.DeleteDep(new DingDepartmentParameter() { DeptId = synInfo.ThirdObjId.ParseToInt() }, ref msg);
                }
            }

            await _repository.DeleteAsync(synInfo);
        }
        catch (Exception)
        {
            throw Oops.Oh(msg);
        }
    }

    /// <summary>
    /// 判断是否存在同步成功数据.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="thirdType"></param>
    /// <param name="dataType"></param>
    /// <returns></returns>
    private async Task<bool> IsExistThirdObjId(string id, int thirdType, int dataType)
    {
        return !await _repository.IsAnyAsync(x => x.ThirdType == thirdType && x.DataType == dataType && x.SysObjId.Equals(id) && !SqlFunc.IsNullOrEmpty(x.ThirdObjId));
    }

    /// <summary>
    /// 保存同步数据.
    /// </summary>
    /// <param name="thirdType"></param>
    /// <param name="dataType"></param>
    /// <param name="sysObjId"></param>
    /// <param name="thirdName"></param>
    /// <param name="thirdObjId"></param>
    /// <param name="msg"></param>
    /// <returns></returns>
    private async Task Save(int thirdType, int dataType, string sysObjId, string thirdName, string thirdObjId, string msg)
    {
        var entity = await _repository.GetFirstAsync(x => x.SysObjId == sysObjId && x.ThirdType == thirdType);
        if (entity == null)
        {
            var newEntity = new SynThirdInfoEntity();
            newEntity.ThirdType = thirdType;
            newEntity.DataType = dataType;
            newEntity.SysObjId = sysObjId;
            newEntity.ThirdName = thirdName;
            newEntity.ThirdObjId = thirdObjId;
            newEntity.EnabledMark = thirdObjId.IsNullOrEmpty() ? 2 : 1;
            newEntity.Description = msg;
            var isOk = await _repository.AsInsertable(newEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            if (isOk < 0) throw Oops.Oh(ErrorCode.D9005);
        }
        else
        {
            entity.ThirdName = thirdName;
            entity.ThirdObjId = thirdObjId;
            entity.EnabledMark = thirdObjId.IsEmpty() ? 2 : 1;
            entity.Description = msg;
            var isOk = await _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
            if (isOk < 0) throw Oops.Oh(ErrorCode.D9006);
        }
    }

    /// <summary>
    /// 获取第三方部门.
    /// </summary>
    /// <param name="organizeId"></param>
    /// <param name="thirdType"></param>
    /// <param name="thirdDepList"></param>
    private async Task GetThirdDep(string organizeId, int thirdType, List<int> thirdDepList)
    {
        var info = await _repository.GetFirstAsync(x => x.SysObjId == organizeId && x.ThirdType == thirdType);
        if (info.IsNotEmptyOrNull() && info.ThirdObjId.IsNotEmptyOrNull())
        {
            thirdDepList.Add(Convert.ToInt32(info.ThirdObjId));
        }
    }

    /// <summary>
    /// 根据系统主键获取第三方主键.
    /// </summary>
    /// <param name="ids">系统主键.</param>
    /// <param name="thirdType">第三方类型.</param>
    /// <param name="dataType">数据类型.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<List<string>> GetThirdIdList(List<string> ids, int thirdType, int dataType)
    {
        return await _repository.AsQueryable().Where(x => x.ThirdType == thirdType
        && x.DataType == dataType && !SqlFunc.IsNullOrEmpty(x.ThirdObjId)).
        In(x => x.SysObjId, ids.ToArray()).Select(x => x.ThirdObjId).ToListAsync();
    }

    /// <summary>
    /// 根据同步类型更新配置的部门id.
    /// </summary>
    /// <returns></returns>
    public async Task UpConfigDepartmentId(string departmentId, int thirdType)
    {
        if (thirdType.Equals(1))
            await _repository.AsSugarClient().Updateable<SysConfigEntity>().SetColumns(x => x.Value == departmentId).Where(x => x.Key.Equals("qyhDepartment")).ExecuteCommandAsync();
        else
            await _repository.AsSugarClient().Updateable<SysConfigEntity>().SetColumns(x => x.Value == departmentId).Where(x => x.Key.Equals("dingDepartment")).ExecuteCommandAsync();
    }

    #region 部门同步

    /// <summary>
    /// 同步部门.
    /// </summary>
    /// <param name="thirdType">第三方类型.</param>
    /// <param name="dataType">组织类型.</param>
    /// <param name="sysConfig">系统配置.</param>
    /// <param name="orgList">组织.</param>
    /// <returns></returns>
    [NonAction]
    public async Task SynDep(int thirdType, int dataType, SaveSocialsInput sysConfig, List<OrganizeListOutput> orgList)
    {
        var depList = new List<OrganizeEntity>();
        switch (thirdType)
        {
            case 1:
                var weChat = new WeChatUtil(sysConfig.qyhCorpId, sysConfig.qyhCorpSecret);
                depList = weChat.GetDepartmentList().Adapt<List<OrganizeEntity>>();
                var newWeChat = new WeChatUtil(sysConfig.qyhCorpId, sysConfig.qyhAgentSecret);
                foreach (var item in orgList)
                {
                    await WeChatDep(item, newWeChat, thirdType, dataType, depList);
                }

                break;
            default:
                var ding = new DingUtil(sysConfig.dingSynAppKey, sysConfig.dingSynAppSecret);
                depList = ding.GetDepList().Adapt<List<OrganizeEntity>>();
                foreach (var item in orgList)
                {
                    await DingDep(item, ding, thirdType, dataType, depList);
                }

                break;
        }
    }

    /// <summary>
    /// 同步部门.
    /// </summary>
    /// <param name="thirdType"></param>
    /// <param name="dataType"></param>
    /// <param name="sysConfig"></param>
    /// <returns></returns>
    [NonAction]
    public async Task SynSysDep(int thirdType, int dataType, SaveSocialsInput sysConfig)
    {
        try
        {
            var parentId = string.Empty;
            var depList = new List<OrganizeEntity>();
            switch (thirdType)
            {
                case 1:
                    parentId = sysConfig.qyhDepartment;
                    var weChat = new WeChatUtil(sysConfig.qyhCorpId, sysConfig.qyhCorpSecret);
                    depList = weChat.GetDepartmentList().Adapt<List<OrganizeEntity>>();
                    break;
                default:
                    parentId = sysConfig.dingDepartment;
                    var ding = new DingUtil(sysConfig.dingSynAppKey, sysConfig.dingSynAppSecret);
                    depList = ding.GetDepList().Adapt<List<OrganizeEntity>>();
                    break;
            }

            var orgEntity = await _repository.AsSugarClient().Queryable<OrganizeEntity>().FirstAsync(x => x.Id == parentId);
            await Save(thirdType, dataType, orgEntity.Id, orgEntity.FullName, "1", string.Empty);

            var orgList = new List<OrganizeEntity>() { orgEntity };
            var def = await _repository.AsSugarClient().Queryable<SysConfigEntity>().Where(x => x.Key.Equals("sysPositionName")).Select(x => x.Value).FirstAsync();
            foreach (var item in depList)
            {
                var syncEntity = await _repository.GetFirstAsync(x => x.ThirdObjId == item.Id && x.DataType == dataType);
                if (syncEntity.IsNotEmptyOrNull())
                {
                    if (await _repository.AsSugarClient().Queryable<OrganizeEntity>().AnyAsync(x => x.DeleteMark == null && x.Id.Equals(syncEntity.SysObjId)))
                    {
                        await _repository.AsSugarClient().Updateable<OrganizeEntity>().SetColumns(it => new OrganizeEntity()
                        {
                            FullName = item.FullName,
                            LastModifyUserId = _userManager.UserId,
                            LastModifyTime = SqlFunc.GetDate()
                        }).Where(it => it.Id.Equals(syncEntity.SysObjId)).ExecuteCommandHasChangeAsync();
                    }
                    else
                    {
                        var parentEntity = new OrganizeEntity();
                        if (item.ParentId == "1")
                        {
                            item.ParentId = orgEntity.Id;
                        }
                        else
                        {
                            item.ParentId = (await _repository.GetFirstAsync(x => x.ThirdObjId == item.ParentId && x.DataType == dataType))?.SysObjId;
                            parentEntity = await _repository.AsSugarClient().Queryable<OrganizeEntity>().FirstAsync(x => x.Id == item.ParentId);
                        }

                        var thirdObjId = item.Id;
                        item.Id = SnowflakeIdHelper.NextId();
                        item.OrganizeIdTree = (item.ParentId == orgEntity.Id ? orgEntity.Id : parentEntity.OrganizeIdTree) + "," + item.Id;

                        var orgNameTree = new List<string>();
                        foreach (var id in item.OrganizeIdTree.Split(","))
                        {
                            var data = orgList.Find(x => x.Id.Equals(id))?.FullName;
                            if (data.IsNotEmptyOrNull()) orgNameTree.Add(data);
                            if (id.Equals(item.Id)) orgNameTree.Add(item.FullName);
                        }

                        item.OrganizeNameTree = string.Join("/", orgNameTree);

                        orgList.Add(item);
                        await Save(thirdType, dataType, item.Id, item.FullName, thirdObjId, string.Empty);
                        await _repository.AsDeleteable().Where(x => x.Id.Equals(syncEntity.Id)).ExecuteCommandAsync();

                        var posEntity = new PositionEntity()
                        {
                            Id = SnowflakeIdHelper.NextId(),
                            ParentId = "-1",
                            EnCode = await _billRuleService.GetEnCode(CommonConst.BillRuleCodeTypePosition),
                            FullName = def.IsNullOrEmpty() ? "默认岗位" : def,
                            OrganizeId = item.Id,
                            DefaultMark = 1,
                        };
                        posEntity.PositionIdTree = posEntity.Id;
                        await _repository.AsSugarClient().Insertable(posEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Create()).ExecuteReturnEntityAsync();

                        item.EnCode = await _billRuleService.GetEnCode(CommonConst.BillRuleCodeTypeOrganize);
                        item.DutyPositionId = posEntity.Id;
                        await _repository.AsSugarClient().Insertable(item).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();
                    }
                }
                else
                {
                    var parentEntity = new OrganizeEntity();
                    if (item.ParentId == "1")
                    {
                        item.ParentId = orgEntity.Id;
                    }
                    else
                    {
                        item.ParentId = (await _repository.GetFirstAsync(x => x.ThirdObjId == item.ParentId && x.DataType == dataType))?.SysObjId;
                        parentEntity = await _repository.AsSugarClient().Queryable<OrganizeEntity>().FirstAsync(x => x.Id == item.ParentId);
                    }

                    var thirdObjId = item.Id;
                    var oldData = await _repository.AsSugarClient().Queryable<OrganizeEntity>().FirstAsync(x => x.ParentId == item.ParentId && x.FullName == item.FullName);
                    item.Id = oldData.IsNotEmptyOrNull() ? oldData.Id : SnowflakeIdHelper.NextId();
                    item.OrganizeIdTree = (item.ParentId == orgEntity.Id ? orgEntity.Id : parentEntity.OrganizeIdTree) + "," + item.Id;

                    var orgNameTree = new List<string>();
                    foreach (var id in item.OrganizeIdTree.Split(","))
                    {
                        var data = orgList.Find(x => x.Id.Equals(id))?.FullName;
                        if (data.IsNotEmptyOrNull()) orgNameTree.Add(data);
                        if (id.Equals(item.Id)) orgNameTree.Add(item.FullName);
                    }

                    item.OrganizeNameTree = string.Join("/", orgNameTree);

                    orgList.Add(item);
                    await Save(thirdType, dataType, item.Id, item.FullName, thirdObjId, string.Empty);
                    if (oldData.IsNullOrEmpty())
                    {
                        var posEntity = new PositionEntity()
                        {
                            Id = SnowflakeIdHelper.NextId(),
                            ParentId = "-1",
                            EnCode = await _billRuleService.GetEnCode(CommonConst.BillRuleCodeTypePosition),
                            FullName = def.IsNullOrEmpty() ? "默认岗位" : def,
                            OrganizeId = item.Id,
                            DefaultMark = 1,
                        };
                        posEntity.PositionIdTree = posEntity.Id;
                        await _repository.AsSugarClient().Insertable(posEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Create()).ExecuteReturnEntityAsync();

                        item.EnCode = await _billRuleService.GetEnCode(CommonConst.BillRuleCodeTypeOrganize);
                        item.DutyPositionId = posEntity.Id;
                        await _repository.AsSugarClient().Insertable(item).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();
                    }
                }
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    private async Task WeChatDep(OrganizeListOutput org, WeChatUtil weChatQYHelper, int thirdType, int dataType, List<OrganizeEntity> depList)
    {
        long parentid = 1;
        var entity = await _repository.GetFirstAsync(x => x.SysObjId == org.parentId && x.ThirdType == thirdType && x.EnabledMark == 1);
        if (entity.IsNotEmptyOrNull())
        {
            parentid = Convert.ToInt32(entity.ThirdObjId);
        }

        var thirdObjId = string.Empty;
        var msg = string.Empty;
        if (org.parentId.Equals("-1"))
        {
            thirdObjId = "1";
        }
        else
        {
            if (await IsExistThirdObjId(org.id, thirdType, dataType))
            {
                var dep = depList.Find(x => x.ParentId.Equals(parentid.ToString()) && x.FullName.Equals(org.fullName));
                if (dep.IsNotEmptyOrNull())
                    thirdObjId = dep.Id;
                else
                    thirdObjId = weChatQYHelper.CreateDepartment(org.fullName, parentid, 1, ref msg);
            }
            else
            {
                var synEntity = await _repository.GetFirstAsync(x => org.id == x.SysObjId && thirdType == x.ThirdType);
                if (synEntity.IsNotEmptyOrNull())
                {
                    var dep = depList.Find(x => x.ParentId.Equals(parentid.ToString()) && x.Id.Equals(synEntity.ThirdObjId));
                    if (dep.IsNotEmptyOrNull())
                    {
                        thirdObjId = synEntity.ThirdObjId;
                        var id = Convert.ToInt32(thirdObjId);
                        weChatQYHelper.UpdateDepartment(id, org.fullName, (int)parentid, 1, ref msg);
                    }
                    else
                    {
                        thirdObjId = weChatQYHelper.CreateDepartment(org.fullName, parentid, 1, ref msg);
                    }
                }
            }
        }

        await Save(thirdType, dataType, org.id, org.fullName, thirdObjId, msg);
        if (org.hasChildren)
        {
            foreach (var item in org.children)
            {
                var orgChild = item.Adapt<OrganizeListOutput>();
                await WeChatDep(orgChild, weChatQYHelper, thirdType, dataType, depList);
            }
        }
    }

    private async Task DingDep(OrganizeListOutput org, DingUtil dingHelper, int thirdType, int dataType, List<OrganizeEntity> depList)
    {
        var dingDep = new DingDepartmentParameter();
        dingDep.Name = org.fullName;
        var entity = await _repository.GetFirstAsync(x => x.SysObjId == org.parentId && x.ThirdType == thirdType && x.EnabledMark == 1);
        if (entity != null)
        {
            dingDep.ParentId = Convert.ToInt32(entity.ThirdObjId);
        }

        var thirdObjId = string.Empty;
        var msg = string.Empty;
        if (org.parentId.Equals("-1"))
        {
            thirdObjId = "1";
        }
        else
        {
            if (await IsExistThirdObjId(org.id, thirdType, dataType))
            {

                var dep = depList.Find(x => x.ParentId.Equals(dingDep.ParentId.ToString()) && x.FullName.Equals(dingDep.Name));
                if (dep.IsNotEmptyOrNull())
                    thirdObjId = dep.Id;
                else
                    thirdObjId = dingHelper.CreateDep(dingDep, ref msg);
            }
            else
            {
                var synEntity = await _repository.GetFirstAsync(x => org.id == x.SysObjId && thirdType == x.ThirdType);
                if (synEntity.IsNotEmptyOrNull())
                {
                    var dep = depList.Find(x => x.ParentId.Equals(dingDep.ParentId.ToString()) && x.Id.Equals(synEntity.ThirdObjId));
                    if (dep.IsNotEmptyOrNull())
                    {
                        thirdObjId = synEntity.ThirdObjId;
                        dingDep.DeptId = Convert.ToInt32(thirdObjId);
                        var flag = dingHelper.UpdateDep(dingDep, ref msg);
                        thirdObjId = flag ? thirdObjId : string.Empty;
                    }
                    else
                    {
                        thirdObjId = dingHelper.CreateDep(dingDep, ref msg);
                    }
                }
            }
        }

        await Save(thirdType, dataType, org.id, org.fullName, thirdObjId, msg);
        if (org.hasChildren)
        {
            foreach (var item in org.children)
            {
                var orgChild = item.Adapt<OrganizeListOutput>();
                await DingDep(orgChild, dingHelper, thirdType, dataType, depList);
            }
        }
    }

    #endregion

    #region 用户同步

    /// <summary>
    /// 同步用户.
    /// </summary>
    /// <param name="thirdType"></param>
    /// <param name="dataType"></param>
    /// <param name="sysConfig"></param>
    /// <param name="userList"></param>
    /// <returns></returns>
    [NonAction]
    public async Task SynUser(int thirdType, int dataType, SaveSocialsInput sysConfig, List<UserEntity> userList)
    {
        switch (thirdType)
        {
            case 1:
                var weChat = new WeChatUtil(sysConfig.qyhCorpId, sysConfig.qyhAgentSecret);
                foreach (var item in userList)
                {
                    await WeChatUser(item, weChat, thirdType, dataType);
                }

                break;
            default:
                var ding = new DingUtil(sysConfig.dingSynAppKey, sysConfig.dingSynAppSecret);
                foreach (var item in userList)
                {
                    await DingUser(item, ding, thirdType, dataType);
                }

                break;
        }
    }

    /// <summary>
    /// 同步用户.
    /// </summary>
    /// <param name="thirdType"></param>
    /// <param name="dataType"></param>
    /// <param name="sysConfig"></param>
    /// <returns></returns>
    [NonAction]
    public async Task SynSysUser(int thirdType, int dataType, SaveSocialsInput sysConfig)
    {
        try
        {
            var userList = new List<UserEntity>();
            switch (thirdType)
            {
                case 1:
                    var weChat = new WeChatUtil(sysConfig.qyhCorpId, sysConfig.qyhCorpSecret);
                    userList = weChat.GetDepartmentMember().Adapt<List<UserEntity>>();
                    break;
                default:
                    var ding = new DingUtil(sysConfig.dingSynAppKey, sysConfig.dingSynAppSecret);
                    userList = ding.GetUserList().Adapt<List<UserEntity>>();
                    break;
            }

            foreach (var item in userList)
            {
                var entity = await _repository.AsSugarClient().Queryable<UserEntity>().Where(x => x.DeleteMark == null && x.MobilePhone.Equals(item.MobilePhone)).FirstAsync();
                if (entity.IsNotEmptyOrNull())
                {
                    item.Id = entity.Id;
                    await _repository.AsSugarClient().Updateable<UserEntity>().SetColumns(it => new UserEntity()
                    {
                        RealName = item.RealName,
                        LastModifyUserId = _userManager.UserId,
                        LastModifyTime = SqlFunc.GetDate()
                    }).Where(it => it.Id.Equals(item.Id)).ExecuteCommandHasChangeAsync();
                }
                else
                {
                    if (_tenant.MultiTenancy)
                    {
                        var tenatInfo = await _tenantManager.GetTenant(_userManager.TenantId);

                        // 是否超过租户账号额度
                        if (tenatInfo.accountNum != 0 && tenatInfo.accountNum <= await _repository.AsSugarClient().Queryable<UserEntity>().CountAsync(x => x.DeleteMark == null))
                            throw Oops.Oh(ErrorCode.D1041);
                    }

                    await _repository.AsSugarClient().Insertable(item).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();
                }

                await Save(thirdType, dataType, item.Id, item.RealName, item.Account, "");

                foreach (var org in item.OrganizeId.Split(","))
                {
                    var syncOrgEntity = await _repository.GetFirstAsync(x => x.ThirdObjId == org && x.ThirdType == thirdType && x.DataType == 1);
                    if (syncOrgEntity.IsNotEmptyOrNull())
                    {
                        if (!await _repository.AsSugarClient().Queryable<UserRelationEntity>().AnyAsync(x => x.UserId.Equals(item.Id) && x.ObjectId.Equals(syncOrgEntity.SysObjId)))
                        {
                            var dutyPositionId = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.Id.Equals(syncOrgEntity.SysObjId)).Select(x => x.DutyPositionId).FirstAsync();
                            var userRelationList = new List<UserRelationEntity>()
                            {
                                new UserRelationEntity()
                                {
                                    Id = SnowflakeIdHelper.NextId(),
                                    UserId = item.Id,
                                    ObjectId = syncOrgEntity.SysObjId,
                                    ObjectType = CommonConst.RelationObjectOrganize,
                                    SortCode = 0,
                                    CreatorTime = DateTime.Now,
                                    CreatorUserId = item.Id
                                },
                                new UserRelationEntity()
                                {
                                    Id = SnowflakeIdHelper.NextId(),
                                    UserId = item.Id,
                                    ObjectId = dutyPositionId,
                                    ObjectType = CommonConst.RelationObjectPosition,
                                    SortCode = 0,
                                    CreatorTime = DateTime.Now,
                                    CreatorUserId = item.Id
                                }
                            };

                            await _repository.AsSugarClient().Insertable(userRelationList).ExecuteCommandAsync();
                        }
                    }
                }
            }
        }
        catch (Exception)
        {

            throw;
        }
    }

    private async Task WeChatUser(UserEntity user, WeChatUtil weChatQYHelper, int thirdType, int dataType)
    {
        var msg = string.Empty;
        var qyUser = new QYMember();
        List<int> depList = new List<int>();
        foreach (var item in _userManager.GetUserOrganizeList(user.Id)) await GetThirdDep(item, thirdType, depList);
        if (depList.Count == 0)
        {
            var synInfo = await _repository.GetFirstAsync(x => x.ThirdType == thirdType && x.DataType == dataType && x.SysObjId == user.Id);
            if (synInfo.IsNullOrEmpty() || synInfo.ThirdObjId.IsNullOrEmpty())
                throw Oops.Oh(ErrorCode.D9004);
            weChatQYHelper.DeleteMember(synInfo.ThirdObjId);
            await _repository.DeleteAsync(synInfo);
        }
        else
        {
            qyUser.userid = user.Id;
            qyUser.name = user.RealName;
            qyUser.mobile = user.MobilePhone;
            qyUser.email = user.Email;
            qyUser.department = depList.Select(x => (long)x).ToArray();
            var thirdObjId = string.Empty;
            if (await IsExistThirdObjId(user.Id, thirdType, dataType))
            {
                var flag = weChatQYHelper.CreateMember(qyUser, ref msg);
                thirdObjId = flag ? user.Id : weChatQYHelper.GetUserid(qyUser.mobile);
            }
            else
            {
                var flag = weChatQYHelper.UpdateMember(qyUser, ref msg);
                thirdObjId = flag ? user.Id : weChatQYHelper.GetUserid(qyUser.mobile);
            }

            await Save(thirdType, dataType, user.Id, user.RealName, thirdObjId, msg);
        }
    }

    private async Task DingUser(UserEntity user, DingUtil dingHelper, int thirdType, int dataType)
    {
        var msg = string.Empty;
        var dingUser = new DingUserParameter();
        List<int> depList = new List<int>();
        foreach (var item in _userManager.GetUserOrganizeList(user.Id)) await GetThirdDep(item, thirdType, depList);
        if (depList.Count == 0)
        {
            var synInfo = await _repository.GetFirstAsync(x => x.ThirdType == thirdType && x.DataType == dataType && x.SysObjId == user.Id);
            if (synInfo.IsNullOrEmpty() || synInfo.ThirdObjId.IsNullOrEmpty())
                throw Oops.Oh(ErrorCode.D9004);
            dingHelper.DeleteUser(new DingUserParameter() { Userid = synInfo.ThirdObjId }, ref msg);
            await _repository.DeleteAsync(synInfo);
        }
        else
        {
            dingUser.Name = user.RealName;
            dingUser.Mobile = user.MobilePhone;
            dingUser.Email = user.Email;
            dingUser.DeptIdList = string.Join(",", depList);
            var thirdObjId = string.Empty;
            if (await IsExistThirdObjId(user.Id, thirdType, dataType))
            {
                thirdObjId = dingHelper.CreateUser(dingUser, ref msg);
            }
            else
            {
                thirdObjId = (await _repository.GetFirstAsync(x => x.SysObjId == user.Id && x.ThirdType == thirdType)).ThirdObjId;
                dingUser.Userid = thirdObjId;
                var flag = dingHelper.UpdateUser(dingUser, ref msg);
                thirdObjId = flag ? thirdObjId : string.Empty;
            }

            await Save(thirdType, dataType, user.Id, user.RealName, thirdObjId, msg);
        }
    }

    #endregion

    #endregion
}