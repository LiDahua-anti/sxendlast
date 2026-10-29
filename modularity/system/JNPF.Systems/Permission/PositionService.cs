using JNPF.Common.Configuration;
using JNPF.Common.Const;
using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Files;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Helper;
using JNPF.Common.Manager;
using JNPF.Common.Models;
using JNPF.Common.Models.NPOI;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DataEncryption;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Dto.Position;
using JNPF.Systems.Entitys.Dto.User;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.Permission;
using JNPF.Systems.Interfaces.System;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.Data;
using System.Text.RegularExpressions;

namespace JNPF.Systems;

/// <summary>
/// 业务实现：岗位管理.
/// 版 本：V3.2.0
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021.06.07.
/// </summary>
[ApiDescriptionSettings(Tag = "Permission", Name = "Position", Order = 162)]
[Route("api/Permission/[controller]")]
public class PositionService : IPositionService, IDynamicApiController, ITransient
{
    /// <summary>
    /// 基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<PositionEntity> _repository;

    /// <summary>
    /// 组织管理.
    /// </summary>
    private readonly IOrganizeService _organizeService;

    /// <summary>
    /// 单据.
    /// </summary>
    private readonly IBillRullService _billRuleService;

    /// <summary>
    /// 文件服务.
    /// </summary>
    private readonly IFileManager _fileManager;

    /// <summary>
    /// 缓存管理.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// 用户管理器.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 初始化一个<see cref="PositionService"/>类型的新实例.
    /// </summary>
    public PositionService(
        ISqlSugarRepository<PositionEntity> repository,
        IOrganizeService organizeService,
        IBillRullService billRuleService,
        ICacheManager cacheManager,
        IFileManager fileService,
        IUserManager userManager)
    {
        _repository = repository;
        _organizeService = organizeService;
        _billRuleService = billRuleService;
        _cacheManager = cacheManager;
        _fileManager = fileService;
        _userManager = userManager;
    }

    #region GET

    /// <summary>
    /// 获取列表 根据organizeId.
    /// </summary>
    /// <param name="organizeId">参数.</param>
    /// <returns></returns>
    [HttpGet("getList/{organizeId}")]
    public async Task<dynamic> GetListByOrganizeId(string organizeId)
    {
        StructurePositionIdTree();
        var dutyPositionId = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.Id.Equals(organizeId)).Select(x => x.DutyPositionId).FirstAsync();
        List<PositionListOutput>? data = await _repository.AsSugarClient().Queryable<PositionEntity>()
            .Where(a => a.DeleteMark == null).OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime)
            .Select(a => new PositionListOutput
            {
                id = a.Id,
                parentId = a.ParentId,
                fullName = a.FullName,
                enCode = a.EnCode,
                defaultMark = a.DefaultMark,
                organizeId = a.OrganizeId,
                creatorTime = a.CreatorTime,
                icon = "icon-ym icon-ym-tree-position1",
            }).ToListAsync();
        foreach (var item in data)
        {
            item.allowDuty = dutyPositionId.Equals(item.id) ? null : 1;
            item.isDutyPosition = dutyPositionId.Equals(item.id) ? 1 : null;
        }

        return data.ToList();
    }

    /// <summary>
    /// 获取列表.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] PositionListQuery input)
    {
        StructurePositionIdTree();
        var dutyPositionId = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.Id.Equals(input.organizeId)).Select(x => x.DutyPositionId).FirstAsync();
        var data = await _repository.AsSugarClient().Queryable<PositionEntity>()
            .WhereIF(input.organizeId.IsNotEmptyOrNull(), a => a.OrganizeId.Equals(input.organizeId))
            .WhereIF(!input.keyword.IsNullOrEmpty(), a => a.FullName.Contains(input.keyword) || a.EnCode.Contains(input.keyword))
            .WhereIF(!input.enabledMark.IsNullOrEmpty(), a => a.EnabledMark.Equals(input.enabledMark))
            .WhereIF(input.defaultMark.IsNotEmptyOrNull(), a => a.DefaultMark.Equals(input.defaultMark) || a.DefaultMark == null)
            .Where(a => a.DeleteMark == null).OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime)
            .Select(a => new PositionListOutput
            {
                id = a.Id,
                parentId = a.ParentId,
                fullName = a.FullName,
                enCode = a.EnCode,
                defaultMark = a.DefaultMark,
                organizeId = a.OrganizeId,
                creatorTime = a.CreatorTime,
                icon = "icon-ym icon-ym-tree-position1",
            }).ToListAsync();

        foreach (var item in data.Where(x => x.parentId.IsNullOrEmpty()))
        {
            item.parentId = "-1";
            item.isDutyPosition = dutyPositionId.Equals(item.id) ? 1 : null;
            if (item.isDutyPosition.IsNullOrEmpty()) item.allowDuty = 1;
        }

        var defaultMark = data.Find(x => x.defaultMark.Equals(1));
        if (defaultMark != null)
        {
            data.Remove(defaultMark);
            data.Insert(0, defaultMark);
        }
        return new { list = data.ToTree("-1") };
    }

    /// <summary>
    /// 获取列表.
    /// </summary>
    /// <returns></returns>
    [HttpGet("All")]
    public async Task<dynamic> GetList()
    {
        List<PositionListOutput>? data = await _repository.AsSugarClient().Queryable<PositionEntity, OrganizeEntity>((a, b) => new JoinQueryInfos(JoinType.Left, b.Id == a.OrganizeId))
            .Where(a => a.DeleteMark == null && a.EnabledMark == 1).OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime)
            .Select((a, b) => new PositionListOutput
            {
                id = a.Id,
                parentId = a.ParentId,
                fullName = a.FullName,
                enCode = a.EnCode,
                defaultMark = a.DefaultMark,
                organizeId = a.OrganizeId,
                creatorTime = a.CreatorTime,
                icon = "icon-ym icon-ym-tree-position1",
            }).ToListAsync();
        return new { list = data.ToList() };
    }

    /// <summary>
    /// 获取下拉框（公司+部门+岗位）.
    /// </summary>
    /// <returns></returns>
    [HttpGet("Selector")]
    public async Task<dynamic> GetSelector([FromQuery] PositionListQuery input)
    {
        var positionList = await _repository.AsQueryable().Where(t => t.EnabledMark == 1 && t.DeleteMark == null)
            .WhereIF(input.keyword.IsNotEmptyOrNull(), t => t.FullName.Contains(input.keyword))
            .Select(t => new PositionSelectorOutput
            {
                id = t.Id,
                parentId = t.OrganizeId,
                fullName = t.FullName,
                orgNameTree = SqlFunc.MergeString(SqlFunc.Subqueryable<OrganizeEntity>().EnableTableFilter().Where(x => x.Id.Equals(t.OrganizeId)).Select(x => x.OrganizeNameTree), "/", t.FullName),
                enabledMark = t.EnabledMark,
                icon = "icon-ym icon-ym-tree-position1",
                type = CommonConst.RelationObjectPosition,
                sortCode = t.SortCode
            }).ToPagedListAsync(input.currentPage, input.pageSize);

        return positionList;
    }

    /// <summary>
    /// 获取下拉框根据组织id.
    /// </summary>
    /// <returns></returns>
    [HttpGet("SelectorTree")]
    public async Task<dynamic> GetSelectorTreeByOrgId(string organizeId)
    {
        var data = await _repository.AsQueryable().Where(t => t.DeleteMark == null && t.OrganizeId.Equals(organizeId))
            .OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime).Select(a => new PositionSelectorOutput()
            {
                id = a.Id,
                parentId = a.ParentId,
                fullName = a.FullName,
                orgNameTree = SqlFunc.Subqueryable<OrganizeEntity>().EnableTableFilter().Where(o => o.Id == a.OrganizeId).Select(o => SqlFunc.MergeString(o.OrganizeNameTree, "/", a.FullName)),
                icon = "icon-ym icon-ym-tree-position1",
                organizeId = a.OrganizeId,
            }).ToListAsync();

        return new { list = data.ToTree("-1") };
    }

    /// <summary>
    /// 获取信息.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        PositionEntity? entity = await _repository.GetSingleAsync(p => p.Id == id);
        var res = entity.Adapt<PositionInfoOutput>();
        if (res.isCondition.Equals(0)) res.conditionJson = null;
        res.organizeName = (await _organizeService.GetInfoById(entity.OrganizeId)).FullName;
        var perList = await _repository.GetListAsync(x => x.IsCondition == 1 && x.ConditionJson.Contains(id));
        if (perList.Any())
        {
            foreach (var item in perList)
            {
                var cond = item.ConditionJson.ToObject<PositionConstraintModel>();
                if (cond.prerequisite != null && cond.prerequisite.Contains(id))
                {
                    res.isPrePosition = true;
                    continue;
                }
            }
        }
        return res;
    }

    #endregion

    #region POST

    /// <summary>
    /// 获取下拉框.
    /// </summary>
    /// <returns></returns>
    [HttpPost("SelectedList")]
    public async Task<dynamic> SelectedList([FromBody] UserSelectedInput input)
    {
        return await _organizeService.GetSelectedList(input);
    }

    /// <summary>
    /// 获取岗位列表 根据组织Id集合.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("getListByOrgIds")]
    public async Task<dynamic> GetListByOrgIds([FromBody] PositionListQuery input)
    {
        var dictionaryTypeEntity = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().FirstAsync(x => x.EnCode == "PositionType" && x.DeleteMark == null);
        List<PositionTreeOutput>? data = await _repository.AsSugarClient().Queryable<PositionEntity, OrganizeEntity, DictionaryDataEntity>(
            (a, b, c) => new JoinQueryInfos(JoinType.Left, b.Id == a.OrganizeId, JoinType.Left, a.Type == c.EnCode && c.DictionaryTypeId == dictionaryTypeEntity.Id))
            .Where(a => input.organizeIds.Contains(a.OrganizeId) && a.DeleteMark == null && a.EnabledMark == 1).OrderBy(a => a.SortCode)
            .Select((a, b, c) => new PositionTreeOutput
            {
                id = a.Id,
                type = CommonConst.RelationObjectPosition,
                parentId = b.Id,
                fullName = a.FullName,
                enabledMark = a.EnabledMark,
                creatorTime = a.CreatorTime,
                sortCode = -2,
                isLeaf = true
            }).ToListAsync();

        // 处理组织树 名称
        List<OrganizeEntity>? allOrgList = _organizeService.GetOrgListTreeName();
        List<PositionTreeOutput>? organizeList = allOrgList.Where(x => input.organizeIds.Contains(x.Id)).Select(x => new PositionTreeOutput()
        {
            id = x.Id,
            type = x.Category,
            parentId = "0",
            fullName = x.Description,
            num = data.Count(x => x.parentId == x.id)
        }).ToList();

        return new { list = organizeList.Union(data).OrderBy(x => x.sortCode).ToList().ToTree("0") };
    }

    /// <summary>
    /// 通过公司、部门、岗位id获取岗位列表.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("PositionCondition")]
    public async Task<dynamic> PositionCondition([FromBody] UserSelectedInput input)
    {
        var oIdList = new List<string>();
        foreach (var x in input.ids)
        {
            if (x.Equals("@positionId"))
            {
                oIdList.Add(_userManager.User.PositionId);
                continue;
            }

            if (x.Equals("@positionAndSub"))
            {
                if (_userManager.User.PositionId.IsNotEmptyOrNull())
                {
                    var res = await _repository.AsQueryable()
                        .Where(x => x.ParentId.Equals(_userManager.User.PositionId) || x.Id.Equals(_userManager.User.PositionId))
                        .Select(x => x.Id).ToListAsync();
                    oIdList.AddRange(res);
                }
                continue;
            }

            if (x.Equals("@positionAndProgeny"))
            {
                if (_userManager.User.PositionId.IsNotEmptyOrNull())
                {
                    var res = await _repository.AsQueryable().Where(x => x.PositionIdTree.Contains(_userManager.User.PositionId)).Select(x => x.Id).ToListAsync();
                    oIdList.AddRange(res);
                }
                continue;
            }

            var oid = x.Split("--").First();
            if (x.Contains("--pos"))
            {
                oIdList.Add(oid);
            }
            else if (x.Contains("--subPos"))
            {
                var res = await _repository.AsQueryable()
                    .Where(x => x.ParentId.Equals(oid) || x.Id.Equals(oid))
                    .Select(x => x.Id).ToListAsync();
                oIdList.AddRange(res);
            }
            else if (x.Contains("--progenyPos"))
            {
                var res = await _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => x.PositionIdTree.Contains(oid)).Select(x => x.Id).ToListAsync();
                oIdList.AddRange(res);
            }
            else
            {
                oIdList.Add(x);
            }
        }

        var data = await _repository.AsQueryable().Where(t => t.DeleteMark == null && oIdList.Contains(t.Id))
            .OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime).Select(a => new PositionTreeOutput()
            {
                id = a.Id,
                parentId = a.ParentId,
                enCode = a.EnCode,
                fullName = a.FullName,
                orgNameTree = SqlFunc.Subqueryable<OrganizeEntity>().EnableTableFilter().Where(x => x.Id.Equals(a.OrganizeId)).Select(x => x.OrganizeNameTree),
                icon = "icon-ym icon-ym-tree-position1",
            }).ToListAsync();

        data.ForEach(item =>
        {
            if (!data.Any(x => x.id.Equals(item.parentId))) item.parentId = "0";
            item.orgNameTree = item.orgNameTree + "/" + item.fullName;
            item.fullName = item.orgNameTree;
        });
        return new { list = data.ToTree("0") };
    }

    /// <summary>
    /// 新建.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create([FromBody] PositionCrInput input)
    {
        if (await _repository.IsAnyAsync(p => p.OrganizeId == input.organizeId && p.FullName == input.fullName && p.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D6005);
        if (await _repository.IsAnyAsync(p => p.EnCode == input.enCode && p.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D6000);

        PositionEntity? entity = input.Adapt<PositionEntity>();
        entity.Id = SnowflakeIdHelper.NextId();
        #region 约束验证

        if (input.isCondition.Equals(1))
        {
            var constraint = input.conditionJson.ToObject<PositionConstraintModel>();
            if (constraint.constraintType.Contains(0) && constraint.constraintType.Contains(2) && constraint.mutualExclusion.Any(x => constraint.prerequisite.Contains(x))) throw Oops.Oh(ErrorCode.D6020);
            if (constraint.constraintType.Contains(0) && constraint.mutualExclusion.Any())
            {
                var mePosList = await _repository.AsQueryable().Where(x => constraint.mutualExclusion.Contains(x.Id)).ToListAsync();
                foreach (var moPos in mePosList)
                {
                    if (moPos.ConditionJson.IsNullOrEmpty()) moPos.ConditionJson = new PositionConstraintModel() { constraintType = new List<int>(), mutualExclusion = new List<string>() }.ToJsonString();
                    var newCond = moPos.ConditionJson.ToObject<PositionConstraintModel>();
                    if (newCond.mutualExclusion == null) newCond.mutualExclusion = new List<string>();
                    if (newCond.constraintType == null) newCond.constraintType = new List<int>();
                    if (!newCond.constraintType.Contains(0)) newCond.constraintType.Insert(0, 0);
                    if (!newCond.mutualExclusion.Contains(entity.Id)) newCond.mutualExclusion.Add(entity.Id);
                    moPos.IsCondition = 1;
                    moPos.ConditionJson = newCond.ToJsonString();
                }

                await _repository.AsSugarClient().Updateable(mePosList).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();
            }

            if (constraint.constraintType.Contains(2) && constraint.prerequisite.Any())
            {
                var preList = await _repository.AsQueryable().Where(x => constraint.prerequisite.Contains(x.Id)).ToListAsync();

                foreach (var pre in preList)
                {
                    if (pre.IsCondition.Equals(1) && pre.ConditionJson.IsNotEmptyOrNull())
                    {
                        var preConstraint = pre.ConditionJson.ToObject<PositionConstraintModel>();
                        if (preConstraint.constraintType != null && preConstraint.constraintType.Contains(2)) throw Oops.Oh(ErrorCode.D6029);
                    }
                }
            }
        }

        #endregion

        entity.EnCode = entity.EnCode.IsNullOrEmpty() ? await _billRuleService.GetEnCode(CommonConst.BillRuleCodeTypePosition) : entity.EnCode;
        if (entity.ParentId.IsNullOrEmpty())
        {
            entity.PositionIdTree = entity.Id;
        }
        else
        {
            var tree = await _repository.GetFirstAsync(x => x.Id == entity.ParentId);
            List<string>? ids = tree.PositionIdTree.Split(",").ToList();
            ids.Add(entity.Id);
            entity.PositionIdTree = string.Join(",", ids);
            var layer = await _userManager.GetOrganizeOrPositionLayer(CommonConst.RelationObjectPosition);
            if (ids.Count > layer) throw Oops.Oh(ErrorCode.D6024, layer);
        }

        int isOk = await _repository.AsSugarClient().Insertable(entity).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();
        if (!(isOk > 0)) throw Oops.Oh(ErrorCode.D6001);
        await DelPosition(string.Format("{0}_{1}", _userManager.TenantId, _userManager.UserId));
    }

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        PositionEntity? entity = await _repository.GetSingleAsync(p => p.Id == id && p.DeleteMark == null);

        // 岗位下有数据权限不能删
        List<string>? items = await GetAuthorizeItemIds(CommonConst.AuthorizeItemResource, entity.Id);
        if (items.Count > 0)
            throw Oops.Oh(ErrorCode.D6010);

        // 岗位下有表单不能删
        items = await GetAuthorizeItemIds(CommonConst.AuthorizeItemForm, entity.Id);
        if (items.Count > 0)
            throw Oops.Oh(ErrorCode.D6013);

        // 岗位下有列不能删除
        items = await GetAuthorizeItemIds(CommonConst.AuthorizeItemColumn, entity.Id);
        if (items.Count > 0)
            throw Oops.Oh(ErrorCode.D6012);

        // 岗位下有按钮不能删除
        items = await GetAuthorizeItemIds(CommonConst.AuthorizeItemButton, entity.Id);
        if (items.Count > 0)
            throw Oops.Oh(ErrorCode.D6011);

        // 岗位下有菜单不能删
        items = await GetAuthorizeItemIds(CommonConst.AuthorizeItemModule, entity.Id);
        if (items.Count > 0)
            throw Oops.Oh(ErrorCode.D6013);

        // 岗位下有用户不能删
        var posIds = await _repository.AsQueryable().Where(p => p.PositionIdTree.Contains(id) && p.DeleteMark == null).Select(p => p.Id).ToListAsync();
        if (await _repository.AsSugarClient().Queryable<UserRelationEntity>().AnyAsync(u => u.ObjectType == CommonConst.RelationObjectPosition && posIds.Contains(u.ObjectId)))
            throw Oops.Oh(ErrorCode.D6027);

        int isOk = await _repository.AsSugarClient().Updateable(entity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
        if (!(isOk > 0))
            throw Oops.Oh(ErrorCode.D6002);
        var orgEntity = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.Id.Equals(entity.OrganizeId)).FirstAsync();
        if (orgEntity != null && orgEntity.DutyPositionId.Equals(entity.Id))
        {
            orgEntity.DutyPositionId = null;
            await _repository.AsSugarClient().Updateable(orgEntity).UpdateColumns(it => new { it.DutyPositionId }).ExecuteCommandAsync();
        }

        await DelPosition(string.Format("{0}_{1}", _userManager.TenantId, _userManager.UserId));
    }

    /// <summary>
    /// 更新.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Update(string id, [FromBody] PositionUpInput input)
    {
        PositionEntity? oldEntity = await _repository.GetSingleAsync(it => it.Id == id);

        if (await _repository.IsAnyAsync(p => p.OrganizeId == input.organizeId && p.FullName == input.fullName && p.DeleteMark == null && p.Id != id))
            throw Oops.Oh(ErrorCode.D6005);
        if (await _repository.IsAnyAsync(p => p.EnCode == input.enCode && p.DeleteMark == null && p.Id != id))
            throw Oops.Oh(ErrorCode.D6000);
        if (input.parentId.IsNotEmptyOrNull() && input.parentId.Equals(id))
            throw Oops.Oh(ErrorCode.D6014);
        if (input.parentId.IsNotEmptyOrNull() && input.parentId != oldEntity.ParentId
            && await _repository.IsAnyAsync(p => p.PositionIdTree.Contains(id) && p.Id.Equals(input.parentId) && p.Id != id))
            throw Oops.Oh(ErrorCode.D6014);

        #region 约束验证

        if (input.isCondition.Equals(1))
        {
            var constraint = input.conditionJson.ToObject<PositionConstraintModel>();
            if (constraint.constraintType.Contains(0) && constraint.mutualExclusion.Contains(id)) throw Oops.Oh(ErrorCode.D6015);
            if (constraint.constraintType.Contains(2) && constraint.prerequisite.Contains(id)) throw Oops.Oh(ErrorCode.D6016);
            if (constraint.constraintType.Contains(0) && constraint.constraintType.Contains(2) && constraint.mutualExclusion.Any(x => constraint.prerequisite.Contains(x))) throw Oops.Oh(ErrorCode.D6020);

            if (constraint.constraintType.Contains(0) && constraint.mutualExclusion.Any())
            {
                var mePosList = await _repository.AsQueryable().Where(x => constraint.mutualExclusion.Contains(x.Id)).ToListAsync();
                foreach (var moPos in mePosList)
                {
                    if (moPos.ConditionJson.IsNullOrEmpty()) moPos.ConditionJson = new PositionConstraintModel() { constraintType = new List<int>(), mutualExclusion = new List<string>(), prerequisite = new List<string>() }.ToJsonString();
                    var newCond = moPos.ConditionJson.ToObject<PositionConstraintModel>();
                    if (newCond.mutualExclusion == null) newCond.mutualExclusion = new List<string>();
                    if (newCond.constraintType == null) newCond.constraintType = new List<int>();
                    if (!newCond.constraintType.Contains(0)) newCond.constraintType.Insert(0, 0);
                    if (!newCond.mutualExclusion.Contains(id)) newCond.mutualExclusion.Add(id);
                    moPos.IsCondition = 1;
                    moPos.ConditionJson = newCond.ToJsonString();
                    if (newCond.constraintType.Contains(0) && newCond.constraintType.Contains(2) && newCond.mutualExclusion.Any(x => newCond.prerequisite.Contains(x))) throw Oops.Oh(ErrorCode.D6020);
                }

                await _repository.AsSugarClient().Updateable(mePosList).ExecuteCommandAsync();
            }
            else
            {
                if (constraint.constraintType.Contains(2) && constraint.prerequisite.Any())
                {
                    var preList = await _repository.AsQueryable().Where(x => constraint.prerequisite.Contains(x.Id)).ToListAsync();

                    foreach (var pre in preList)
                    {
                        if (pre.IsCondition.Equals(1) && pre.ConditionJson.IsNotEmptyOrNull())
                        {
                            var preConstraint = pre.ConditionJson.ToObject<PositionConstraintModel>();
                            if (preConstraint.constraintType != null && preConstraint.constraintType.Contains(2)) throw Oops.Oh(ErrorCode.D6029);
                        }
                    }
                }
                if (oldEntity.ConditionJson.IsNotEmptyOrNull())
                {
                    var oldConstraint = oldEntity.ConditionJson.ToObject<PositionConstraintModel>();
                    var mePosList = await _repository.AsQueryable().Where(x => oldConstraint.mutualExclusion.Contains(x.Id)).ToListAsync();
                    foreach (var moPos in mePosList)
                    {
                        if (moPos.ConditionJson.IsNullOrEmpty()) moPos.ConditionJson = new PositionConstraintModel() { constraintType = new List<int>(), mutualExclusion = new List<string>() }.ToJsonString();
                        var newCond = moPos.ConditionJson.ToObject<PositionConstraintModel>();
                        if (newCond.mutualExclusion == null) newCond.mutualExclusion = new List<string>();
                        if (newCond.constraintType == null) newCond.constraintType = new List<int>();
                        if (newCond.mutualExclusion.Contains(id)) newCond.mutualExclusion.Remove(id);
                        if (newCond.constraintType.Contains(0) && !newCond.mutualExclusion.Any()) newCond.constraintType.Remove(0);
                        if (newCond.constraintType.Any()) moPos.IsCondition = 1;
                        else moPos.IsCondition = 0;
                        moPos.ConditionJson = newCond.ToJsonString();
                    }

                    await _repository.AsSugarClient().Updateable(mePosList).ExecuteCommandAsync();
                }
                if (constraint.mutualExclusion != null) constraint.mutualExclusion.Clear();
                input.conditionJson = constraint.ToJsonString();
            }

            // 验证是否存在已有互斥岗位用户
            if (constraint.constraintType.Contains(0))
            {
                var userIds = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => constraint.mutualExclusion.Contains(x.ObjectId) || x.ObjectId.Equals(id)).ToListAsync();
                if (userIds.Count(x => x.ObjectId.Equals(id)) > 0)
                {
                    var uIds = await _repository.AsSugarClient().Queryable<UserEntity>().Where(x => userIds.Where(xx => xx.ObjectId.Equals(id)).Select(xx => xx.UserId).Distinct().Contains(x.Id)).Select(x => x.Id).ToListAsync();
                    foreach (var userId in uIds)
                    {
                        var objIds = userIds.Where(x => x.UserId.Equals(userId) && constraint.mutualExclusion.Contains(x.ObjectId)).Select(x => x.ObjectId).ToList();
                        if (objIds.Any()) throw Oops.Oh(ErrorCode.D6025);
                    }
                }
            }

            // 验证是否不存在已有先决岗位用户
            if (constraint.constraintType.Contains(2))
            {
                var reIds = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => constraint.prerequisite.Contains(x.ObjectId) || x.ObjectId.Equals(id)).ToListAsync();
                if (reIds.Count(x => x.ObjectId.Equals(id)) > 0)
                {
                    var userIds = new List<string>();

                    foreach (var userId in reIds.Where(x => x.ObjectId.Equals(id)).Select(x => x.UserId).Distinct())
                    {
                        var objIds = reIds.Where(x => x.UserId.Equals(userId)).Select(x => x.ObjectId).ToList();
                        if (!constraint.prerequisite.All(x => objIds.Contains(x))) userIds.Add(userId);
                    }

                    if (userIds.Any())
                    {
                        throw Oops.Oh(ErrorCode.D6026);
                    }
                }
            }
        }
        else
        {
            if (input.conditionJson.IsNotEmptyOrNull())
            {
                var constraint = input.conditionJson.ToObject<PositionConstraintModel>();
                if (constraint.mutualExclusion != null) constraint.mutualExclusion.Clear();
                input.conditionJson = constraint.ToJsonString();
            }
            if (oldEntity.ConditionJson.IsNotEmptyOrNull())
            {
                var oldConstraint = oldEntity.ConditionJson.ToObject<PositionConstraintModel>();
                var mePosList = await _repository.AsQueryable().Where(x => oldConstraint.mutualExclusion.Contains(x.Id)).ToListAsync();
                foreach (var moPos in mePosList)
                {
                    var newCond = moPos.ConditionJson.ToObject<PositionConstraintModel>();
                    if (newCond.mutualExclusion == null) newCond.mutualExclusion = new List<string>();
                    if (newCond.mutualExclusion.Contains(id)) newCond.mutualExclusion.Remove(id);
                    if (newCond.constraintType.Contains(0) && !newCond.mutualExclusion.Any()) newCond.constraintType.Remove(0);
                    if (newCond.constraintType.Any()) moPos.IsCondition = 1;
                    else moPos.IsCondition = 0;
                    moPos.ConditionJson = newCond.ToJsonString();
                }

                await _repository.AsSugarClient().Updateable(mePosList).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();
            }
        }

        #endregion

        PositionEntity? entity = input.Adapt<PositionEntity>();
        entity.EnCode = entity.EnCode.IsNullOrEmpty() ? await _billRuleService.GetEnCode(CommonConst.BillRuleCodeTypeOrganize) : entity.EnCode;
        entity.DefaultMark = oldEntity.DefaultMark;
        entity.CreatorUserId = oldEntity.CreatorUserId;
        entity.CreatorTime = oldEntity.CreatorTime;

        // 如果上级结构 变动 ，需要更改所有包含 该岗位的id 的结构
        List<PositionEntity> oldEntityList = null;
        if (entity.ParentId != oldEntity.ParentId)
        {
            // 验证涉及流程相关逻辑拦截
            if (await _repository.AsSugarClient().Queryable<WorkFlow.Entitys.Entity.WorkFlowLaunchUserEntity>().Where(x => x.PositionId.Contains(entity.Id) && x.Type.Equals(2)).AnyAsync())
                throw Oops.Oh(ErrorCode.WF0081);

            if (entity.ParentId.IsNullOrEmpty())
            {
                entity.PositionIdTree = entity.Id;
            }
            else
            {
                var tree = await _repository.GetFirstAsync(x => x.Id == entity.ParentId);
                List<string>? ids = tree.PositionIdTree.Split(",").ToList();
                ids.Add(entity.Id);
                entity.PositionIdTree = string.Join(",", ids);
                var layer = await _userManager.GetOrganizeOrPositionLayer(CommonConst.RelationObjectPosition);
                if (ids.Count > layer) throw Oops.Oh(ErrorCode.D2021, layer);
            }

            // 处理所有子集 树id
            oldEntityList = await _repository.AsQueryable().Where(x => x.PositionIdTree.Contains(oldEntity.PositionIdTree + ",") && x.Id != oldEntity.Id).ToListAsync();
            foreach (var item in oldEntityList)
            {
                string? childList = item.PositionIdTree.Split(oldEntity.Id).LastOrDefault();
                item.PositionIdTree = entity.PositionIdTree + childList;
            }
        }
        int isOk = await _repository.AsSugarClient().Updateable(entity).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
        if (!(isOk > 0))
            throw Oops.Oh(ErrorCode.D6003);

        if (oldEntityList != null)
            await _repository.AsSugarClient().Updateable(oldEntityList).UpdateColumns(x => new { x.PositionIdTree }).ExecuteCommandAsync(); // 批量修改子集

        // 变更所属组织 处理联动数据
        if (!oldEntity.OrganizeId.Equals(input.organizeId)) await UpdateOrgRelation(id, oldEntity.OrganizeId, input.organizeId);

        await DelPosition(string.Format("{0}_{1}", _userManager.TenantId, _userManager.UserId));
    }

    /// <summary>
    /// 更新责任岗位.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <param name="organizeId">组织id.</param>
    /// <returns></returns>
    [HttpPost("{id}/Actions/DutyPosition")]
    public async Task DutyPosition(string id, string organizeId)
    {
        if (!await _repository.IsAnyAsync(r => r.Id == id && r.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D6006);
        if (!await _repository.AsSugarClient().Queryable<OrganizeEntity>().AnyAsync(r => r.Id == organizeId && r.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D2002);

        var orgEntity = await _repository.AsSugarClient().Queryable<OrganizeEntity>().FirstAsync(x => x.Id.Equals(organizeId));
        orgEntity.DutyPositionId = id;
        var isOk = await _repository.AsSugarClient().Updateable(orgEntity).CallEntityMethod(x => x.LastModify()).ExecuteCommandAsync();
        if (!(isOk > 0))
            throw Oops.Oh(ErrorCode.D6003);
    }

    /// <summary>
    /// 更新岗位责任人.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <param name="userId">用户id.</param>
    /// <returns></returns>
    [HttpPost("{id}/Actions/Dutyuser")]
    public async Task Dutyuser(string id, string userId)
    {
        if (!await _repository.IsAnyAsync(r => r.Id == id && r.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D6006);

        var posEntity = await _repository.AsSugarClient().Queryable<PositionEntity>().FirstAsync(x => x.Id.Equals(id));
        posEntity.DutyUserId = userId;
        var isOk = await _repository.AsSugarClient().Updateable(posEntity).CallEntityMethod(x => x.LastModify()).ExecuteCommandAsync();
        if (!(isOk > 0))
            throw Oops.Oh(ErrorCode.D6003);
    }

    #endregion

    #region PublicMethod

    /// <summary>
    /// 获取信息.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<PositionEntity> GetInfoById(string id)
    {
        return await _repository.GetSingleAsync(p => p.Id == id);
    }

    /// <summary>
    /// 获取岗位列表.
    /// </summary>
    /// <returns></returns>
    [NonAction]
    public async Task<List<PositionEntity>> GetListAsync()
    {
        return await _repository.AsQueryable().Where(u => u.DeleteMark == null).ToListAsync();
    }

    /// <summary>
    /// 名称.
    /// </summary>
    /// <param name="ids">岗位ID组</param>
    /// <returns></returns>
    [NonAction]
    public string GetName(string ids)
    {
        if (ids.IsNullOrEmpty())
            return string.Empty;
        List<string>? idList = ids.Split(",").ToList();
        List<string>? nameList = new List<string>();
        List<PositionEntity>? roleList = _repository.AsQueryable().Where(x => x.DeleteMark == null && x.EnabledMark == 1).ToList();
        foreach (string item in idList)
        {
            var info = roleList.Find(x => x.Id == item);
            if (info != null && info.FullName.IsNotEmptyOrNull())
            {
                nameList.Add(info.FullName);
            }
        }

        return string.Join(",", nameList);
    }

    #endregion

    #region PrivateMethod

    /// <summary>
    /// 变更所属组织 处理联动数据.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="oldOrganizeId"></param>
    /// <param name="newOrganizeId"></param>
    /// <returns></returns>
    private async Task UpdateOrgRelation(string id, string oldOrganizeId, string newOrganizeId)
    {
        var orgEntity = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.Id.Equals(oldOrganizeId)).FirstAsync();
        if (orgEntity != null && orgEntity.DutyPositionId.IsNotEmptyOrNull() && orgEntity.DutyPositionId.Equals(id))
        {
            orgEntity.DutyPositionId = null;
            await _repository.AsSugarClient().Updateable(orgEntity).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
        }

        // 处理用户关联组织
        var userReOrgList = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => x.ObjectId.Equals(oldOrganizeId) && x.ObjectType.Equals(CommonConst.RelationObjectOrganize)).ToListAsync();

        var delIds = new Dictionary<string, string>();
        foreach (var item in userReOrgList) if (!delIds.ContainsKey(item.ObjectId + item.UserId)) delIds[item.ObjectId + item.UserId] = item.Id;
        var updateReList = userReOrgList.Where(x => delIds.Select(xx => xx.Value).Contains(x.Id)).ToList();
        foreach (var it in updateReList) it.ObjectId = newOrganizeId;
        await _repository.AsSugarClient().Updateable(updateReList).ExecuteCommandAsync();

        // 处理子岗位
        var childList = await _repository.AsQueryable().Where(x => x.ParentId.Equals(id)).ToListAsync();
        foreach (var child in childList)
        {
            await UpdateOrgRelation(child.Id, child.OrganizeId, newOrganizeId);
            child.OrganizeId = newOrganizeId;
        }
        await _repository.AsSugarClient().Updateable(childList).ExecuteCommandAsync();
    }

    /// <summary>
    /// 删除岗位缓存.
    /// </summary>
    /// <param name="userId">适配多租户模式(userId:tenantId_userId).</param>
    /// <returns></returns>
    private async Task<bool> DelPosition(string userId)
    {
        string? cacheKey = string.Format("{0}{1}", CommonConst.CACHEKEYPOSITION, userId);
        await _cacheManager.DelAsync(cacheKey);
        return await Task.FromResult(true);
    }

    /// <summary>
    /// 获取权限项ids.
    /// </summary>
    /// <param name="roleId">角色id.</param>
    /// <param name="itemType">项类型.</param>
    /// <returns></returns>
    private async Task<List<string>> GetAuthorizeItemIds(string roleId, string itemType)
    {
        var data = await _repository.AsSugarClient().Queryable<AuthorizeEntity>().Where(a => a.ObjectId == roleId && a.ItemType == itemType).GroupBy(it => new { it.ItemId }).Select(it => new { it.ItemId }).ToListAsync();
        return data.Select(it => it.ItemId).ToList();
    }

    /// <summary>
    /// 构造 PositionIdTree.
    /// </summary>
    private void StructurePositionIdTree()
    {

        if (_repository.IsAny(x => SqlFunc.IsNullOrEmpty(SqlFunc.ToString(x.PositionIdTree))))
        {
            var orgList = _repository.GetList(x => SqlFunc.IsNullOrEmpty(x.PositionIdTree));

            foreach (var item in orgList)
            {
                if (item.ParentId == "-1")
                {
                    item.PositionIdTree = item.Id;
                }
                else
                {
                    var plist = GetOrganizeIdTree(item.Id);
                    plist.Reverse();
                    item.PositionIdTree = string.Join(",", plist);
                }
            }

            _repository.AsUpdateable(orgList).ExecuteCommand();
        }
    }
    private List<string> GetOrganizeIdTree(string id)
    {
        var res = new List<string>() { id };
        var entity = _repository.AsQueryable().First(x => x.Id.Equals(id));
        if (entity != null && entity.ParentId != null) res.AddRange(GetOrganizeIdTree(entity.ParentId));
        return res;
    }

    #endregion

    #region 导出和导入

    /// <summary>
    /// 导出Excel.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("ExportData")]
    public async Task<dynamic> ExportData([FromQuery] PositionExportDataInput input)
    {
        var data = await _repository.AsSugarClient().Queryable<PositionEntity>()
            .Where(a => a.DeleteMark == null).Where(a => a.DefaultMark != 1 || a.DefaultMark == null).OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime)
            .Select(a => new PositionListImportDataInput
            {
                id = a.Id,
                fullName = a.FullName,
                enCode = a.EnCode,
                organizeId = SqlFunc.Subqueryable<OrganizeEntity>().EnableTableFilter().Where(x => x.Id.Equals(a.OrganizeId)).Select(x => SqlFunc.MergeString(x.FullName, "/", x.EnCode)),
                parentId = SqlFunc.Subqueryable<PositionEntity>().EnableTableFilter().Where(x => x.Id.Equals(a.ParentId)).Select(x => SqlFunc.MergeString(x.FullName, "/", x.EnCode)),
                description = a.Description,
                isCondition = SqlFunc.IIF(a.IsCondition == 1, "启用", "关闭"),
                constraintType = a.ConditionJson,
                sortCode = SqlFunc.ToString(a.SortCode)
            }).ToListAsync();
        data = data.Where(x => x.organizeId.IsNotEmptyOrNull()).ToList();
        foreach (var item in data)
        {
            if (item.isCondition == "启用" && item.constraintType.IsNotEmptyOrNull())
            {
                var cons = item.constraintType.ToObject<PositionConstraintModel>();
                var consTypeList = new List<string>();
                if (cons.constraintType.Contains(0))
                {
                    consTypeList.Add("互斥约束");
                    var pList = data.Where(x => cons.mutualExclusion.Contains(x.id)).Select(x => x.fullName + "/" + x.enCode).ToList();
                    item.mutualExclusion = string.Join(",", pList);
                }
                if (cons.constraintType.Contains(1))
                {
                    consTypeList.Add("基数约束");
                    item.userNum = cons.userNum?.ToString();
                    item.permissionNum = cons.permissionNum?.ToString();
                }
                if (cons.constraintType.Contains(2))
                {
                    consTypeList.Add("先决约束");
                    var pList = data.Where(x => cons.prerequisite.Contains(x.id)).Select(x => x.fullName + "/" + x.enCode).ToList();
                    item.prerequisite = string.Join(",", pList);
                }

                item.constraintType = string.Join(",", consTypeList);
            }
            else
            {
                item.constraintType = null;
                item.userNum = null;
                item.permissionNum = null;
            }
        }

        ExcelConfig excelconfig = new ExcelConfig();
        excelconfig.FileName = string.Format("岗位信息_{0:yyyyMMddhhmmss}.xls", DateTime.Now);
        excelconfig.HeadFont = "微软雅黑";
        excelconfig.HeadPoint = 10;
        excelconfig.IsAllSizeColumn = true;
        excelconfig.IsBold = true;
        excelconfig.IsAllBorder = true;
        excelconfig.ColumnModel = new List<ExcelColumnModel>();
        var columnList = GetInfoFieldToTitle();
        foreach (var item in columnList)
        {
            excelconfig.ColumnModel.Add(new ExcelColumnModel() { Column = item.ColumnKey, ExcelColumn = item.ColumnValue, Required = item.Required });
        }

        string? addPath = Path.Combine(FileVariable.TemporaryFilePath, excelconfig.FileName);
        var fs = ExcelExportHelper<PositionListImportDataInput>.ExportMemoryStream(data, excelconfig);
        var flag = await _fileManager.UploadFileByType(fs, FileVariable.TemporaryFilePath, excelconfig.FileName);
        if (flag)
        {
            fs.Flush();
            fs.Close();
        }

        _cacheManager.Set(excelconfig.FileName, string.Empty);
        return new { name = excelconfig.FileName, url = "/api/file/Download?encryption=" + DESEncryption.Encrypt(_userManager.UserId + "|" + excelconfig.FileName + "|" + addPath, "JNPF") };
    }

    /// <summary>
    /// 模板下载.
    /// </summary>
    /// <returns></returns>
    [HttpGet("TemplateDownload")]
    public async Task<dynamic> TemplateDownload()
    {
        // 初始化 一条空数据
        List<PositionListImportDataInput>? dataList = new List<PositionListImportDataInput>() { new PositionListImportDataInput(){
            organizeId = "组织名称/编码",
            parentId = "岗位名称/编码",
            isCondition = "启用/关闭",
            constraintType = "互斥约束,基数约束,先决约束：多选组合",
            mutualExclusion = "岗位名称/编码",
            prerequisite = "岗位名称/编码" }
        };

        ExcelConfig excelconfig = new ExcelConfig();
        excelconfig.FileName = "岗位信息导入模板.xls";
        excelconfig.HeadFont = "微软雅黑";
        excelconfig.HeadPoint = 10;
        excelconfig.IsAllSizeColumn = true;
        excelconfig.IsBold = true;
        excelconfig.IsAllBorder = true;
        excelconfig.IsAnnotation = true;
        excelconfig.ColumnModel = new List<ExcelColumnModel>();
        var infoFields = GetInfoFieldToTitle();
        infoFields.RemoveAll(x => x.ColumnKey.Equals("errorsInfo"));
        foreach (var item in infoFields)
        {
            excelconfig.ColumnModel.Add(new ExcelColumnModel() { Column = item.ColumnKey, ExcelColumn = item.ColumnValue, Required = item.Required, SelectList = item.SelectList });
        }

        string? addPath = Path.Combine(FileVariable.TemporaryFilePath, excelconfig.FileName);
        var stream = ExcelExportHelper<PositionListImportDataInput>.ToStream(dataList, excelconfig);
        await _fileManager.UploadFileByType(stream, FileVariable.TemporaryFilePath, excelconfig.FileName);
        _cacheManager.Set(excelconfig.FileName, string.Empty);
        return new { name = excelconfig.FileName, url = "/api/file/Download?encryption=" + DESEncryption.Encrypt(_userManager.UserId + "|" + excelconfig.FileName + "|" + addPath, "JNPF") };
    }

    /// <summary>
    /// 上传文件.
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    [HttpPost("Uploader")]
    public async Task<dynamic> Uploader(IFormFile file)
    {
        var _filePath = _fileManager.GetPathByType(string.Empty);
        var _fileName = DateTime.Now.ToString("yyyyMMdd") + "_" + SnowflakeIdHelper.NextId() + Path.GetExtension(file.FileName);
        var stream = file.OpenReadStream();
        await _fileManager.UploadFileByType(stream, _filePath, _fileName);
        return new { name = _fileName, url = string.Format("/api/File/Image/{0}/{1}", string.Empty, _fileName) };
    }

    /// <summary>
    /// 导入预览.
    /// </summary>
    /// <returns></returns>
    [HttpGet("ImportPreview")]
    public async Task<dynamic> ImportPreview(string fileName)
    {
        var excelData = await GetImportDataList(fileName);
        return new { dataRow = excelData };
    }

    /// <summary>
    /// 导出错误报告.
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    [HttpPost("ExportExceptionData")]
    [UnitOfWork]
    public async Task<dynamic> ExportExceptionData([FromBody] PositionImportDataInput list)
    {
        //list.list.ForEach(it => it.errorsInfo = string.Empty);
        //object[]? res = await DataImport(list.list);

        // 错误数据
        List<PositionListImportDataInput>? errorlist = list.list;

        ExcelConfig excelconfig = new ExcelConfig();
        excelconfig.FileName = string.Format("岗位信息导入模板错误报告_{0}.xls", DateTime.Now.ToString("yyyyMMddHHmmss"));
        excelconfig.HeadFont = "微软雅黑";
        excelconfig.HeadPoint = 10;
        excelconfig.IsAllSizeColumn = true;
        excelconfig.IsBold = true;
        excelconfig.IsAllBorder = true;
        excelconfig.IsAnnotation = true;
        excelconfig.ColumnModel = new List<ExcelColumnModel>();
        foreach (var item in GetInfoFieldToTitle(true))
        {
            excelconfig.ColumnModel.Add(new ExcelColumnModel() { Column = item.ColumnKey, ExcelColumn = item.ColumnValue, Required = item.Required, FreezePane = item.FreezePane, SelectList = item.SelectList });
        }

        string? addPath = Path.Combine(FileVariable.TemporaryFilePath, excelconfig.FileName);
        ExcelExportHelper<PositionListImportDataInput>.Export(errorlist, excelconfig, addPath);

        _cacheManager.Set(excelconfig.FileName, string.Empty);
        return new { name = excelconfig.FileName, url = "/api/file/Download?encryption=" + DESEncryption.Encrypt(_userManager.UserId + "|" + excelconfig.FileName + "|" + addPath, "JNPF") };
    }

    /// <summary>
    /// 导入数据.
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    [HttpPost("ImportData")]
    [UnitOfWork]
    public async Task<dynamic> ImportData([FromBody] PositionImportDataInput list)
    {
        if (list.type)
        {
            var excelData = await GetImportDataList(list.fileName);
            list.list = excelData.ToObject<List<PositionListImportDataInput>>();
        }

        list.list.ForEach(x => x.errorsInfo = string.Empty);
        object[]? res = await DataImport(list.list);
        List<PositionEntity>? addlist = res.First() as List<PositionEntity>;
        List<PositionListImportDataInput>? errorlist = res.Last() as List<PositionListImportDataInput>;
        return new { snum = addlist.Count, fnum = errorlist.Count, failResult = errorlist, resultType = errorlist.Count < 1 ? 0 : 1 };
    }

    /// <summary>
    /// 获取导入数据.
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    private async Task<DataTable> GetImportDataList(string fileName)
    {
        DataTable? excelData = new DataTable();
        try
        {
            var FileEncode = GetInfoFieldToTitle();

            string? filePath = FileVariable.TemporaryFilePath;
            string? savePath = Path.Combine(filePath, fileName);

            // 得到数据
            var sr = await _fileManager.GetFileStream(savePath);
            excelData = ExcelImportHelper.ToDataTable(savePath, sr);
            foreach (var it in excelData.Columns)
            {
                var item = it as DataColumn;
                if (item.ColumnName.Equals("errorsInfo")) throw Oops.Oh(ErrorCode.D1807);
                if (!FileEncode.Any(x => x.ColumnKey == item.ToString() && x.ColumnValue.Equals(item.Caption.Replace("*", "")))) throw Oops.Oh(ErrorCode.D1807);
                excelData.Columns[item.ToString()].ColumnName = FileEncode.Where(x => x.ColumnKey == item.ToString()).FirstOrDefault().ColumnKey;
            }

            if (excelData.Rows.Count > 0) excelData.Rows.RemoveAt(0);
        }
        catch (Exception)
        {
            throw Oops.Oh(ErrorCode.D1807);
        }

        if (excelData.Rows.Count < 1)
            throw Oops.Oh(ErrorCode.D5019);
        if (excelData.Rows.Count > 1000)
            throw Oops.Oh(ErrorCode.D5029);

        // 返回结果
        return excelData;
    }

    /// <summary>
    /// 导入数据函数.
    /// </summary>
    /// <param name="list">list.</param>
    /// <returns>[成功列表,失败列表].</returns>
    private async Task<object[]> DataImport(List<PositionListImportDataInput> inputList)
    {
        if (inputList == null || inputList.Count() < 1) throw Oops.Oh(ErrorCode.D5019);
        if (inputList.Count > 1000) throw Oops.Oh(ErrorCode.D5029);

        var orgList = _organizeService.GetOrgListTreeName();
        var pList = _repository.GetList(x => x.DeleteMark == null);

        foreach (var it in inputList) it.id = SnowflakeIdHelper.NextId();
        var list = inputList.ToObject<List<PositionListImportDataInput>>();
        foreach (var it in list) if (it.parentId.IsNotEmptyOrNull()) it.parentId = it.parentId.Split("/").LastOrDefault();
        foreach (var it in list)
        {
            if (it.parentId.IsNotEmptyOrNull())
            {
                var pEntity = pList.FirstOrDefault(x => x.EnCode.Equals(it.parentId));
                if (pEntity != null)
                {
                    it.parentId = pEntity.Id;
                }
                else
                {
                    var pItem = list.FirstOrDefault(x => x.enCode.IsNotEmptyOrNull() && x.enCode.Equals(it.parentId));
                    if (pItem != null) it.parentId = pItem.id;
                }
            }
        }
        foreach (var it in list) it.positionIdTree = ImportInitIdTree(it, list);

        var addList = new List<PositionEntity>();
        var layer = await _userManager.GetOrganizeOrPositionLayer(CommonConst.RelationObjectOrganize);
        var regexEnCode = @"^[a-zA-Z0-9.]*$";
        foreach (var item in list.OrderBy(x => x.positionIdTree.Count()).ToList())
        {
            if (item.parentId.IsNotEmptyOrNull())
            {
                if (!list.Any(x => x.id.Equals(item.parentId) && x.errorsInfo.IsNullOrEmpty()) && !pList.Any(x => x.Id.Equals(item.parentId)))
                {
                    item.errorsInfo += "找不到该上级岗位,";
                    continue;
                }
            }

            if (item.organizeId.IsNullOrWhiteSpace()) item.errorsInfo += "所属组织不能为空,";
            if (item.fullName.IsNullOrWhiteSpace()) item.errorsInfo += "岗位名称不能为空,";
            else if (item.fullName.Length > 50) item.errorsInfo += "岗位名称值超出最多输入字符限制,";
            if (item.enCode.IsNullOrWhiteSpace()) item.enCode = await _billRuleService.GetEnCode(CommonConst.BillRuleCodeTypePosition);
            if (item.enCode.Length > 50) item.errorsInfo += "岗位编码值超出最多输入字符限制,";
            if (item.enCode.IsNotEmptyOrNull() && (!Regex.IsMatch(item.enCode, regexEnCode) || item.enCode.First().Equals('.') || item.enCode.Last().Equals('.')))
                item.errorsInfo += "岗位编码值只能输入英文、数字和小数点且小数点不能放在首尾,";

            if (item.sortCode.IsNotEmptyOrNull())
            {
                try
                {
                    var sortCode = long.Parse(item.sortCode);
                    if (sortCode < 0) item.errorsInfo += "排序值不能小于0,";
                    else if (sortCode > 999999) item.errorsInfo += "排序值不能大于999999,";
                }
                catch
                {
                    item.errorsInfo += "排序值不正确,";
                }
            }

            if (item.fullName.IsNotEmptyOrNull() && item.organizeId.IsNotEmptyOrNull())
            {
                if (list.Count(x => x.fullName == item.fullName && x.organizeId.IsNotEmptyOrNull() && x.organizeId.Split("/").Last() == item.organizeId.Split("/").Last()) > 1)
                {
                    var errorItems = list.Where(x => x.fullName.IsNotEmptyOrNull() && x.fullName == item.fullName).ToList();
                    for (var i = 1; i < errorItems.Count; i++) if (!errorItems[i].errorsInfo.Contains("岗位名称值已存在")) errorItems[i].errorsInfo += "岗位名称值已存在,";
                }
            }

            if (item.enCode.IsNotEmptyOrNull())
            {
                if (list.Count(x => x.enCode == item.enCode) > 1)
                {
                    var errorItems = list.Where(x => x.enCode.IsNotEmptyOrNull() && x.enCode == item.enCode).ToList();
                    for (var i = 1; i < errorItems.Count; i++) if (!errorItems[i].errorsInfo.Contains("岗位编码值已存在")) errorItems[i].errorsInfo += "岗位编码值已存在,";
                }
            }

            PositionEntity entity = new PositionEntity();
            entity.Id = item.id;
            entity.ParentId = item.parentId;
            entity.EnCode = item.enCode;
            entity.FullName = item.fullName;
            entity.Description = item.description;
            entity.SortCode = item.sortCode.IsNotEmptyOrNull() ? item.sortCode.ParseToLong() : 0;
            if (item.isCondition.IsNotEmptyOrNull())
            {
                if (!item.isCondition.Equals("启用") && !item.isCondition.Equals("关闭")) item.errorsInfo += string.Format("找不到该 [{0}] 岗位约束值,", item.isCondition);
                if (item.isCondition.Equals("启用"))
                {
                    if (item.constraintType.IsNotEmptyOrNull())
                    {
                        entity.IsCondition = 1;
                        var constraint = new PositionConstraintModel() { constraintType = new List<int>(), mutualExclusion = new List<string>(), prerequisite = new List<string>() };
                        foreach (var it in item.constraintType.Split(","))
                        {
                            switch (it)
                            {
                                case "互斥约束":
                                    {
                                        constraint.constraintType.Add(0);
                                        var mList = new List<string>();
                                        if (item.mutualExclusion.IsNullOrEmpty())
                                        {
                                            item.errorsInfo += "互斥岗位不能为空,";
                                        }
                                        else
                                        {
                                            foreach (var m in item.mutualExclusion.Split(","))
                                            {
                                                var mutual = pList.Find(x => x.EnCode.Equals(m.Split("/").Last()));
                                                if (mutual != null) mList.Add(mutual.Id);
                                                else item.errorsInfo += string.Format("找不到该 [{0}] 互斥岗位,", m);
                                            }
                                        }
                                        constraint.mutualExclusion = mList;
                                    }

                                    break;
                                case "基数约束":
                                    {
                                        constraint.constraintType.Add(1);
                                        if (item.userNum.IsNullOrEmpty())
                                        {
                                            item.errorsInfo += "用户基数值不能为空,";
                                        }
                                        else
                                        {
                                            try
                                            {
                                                constraint.userNum = int.Parse(item.userNum);
                                            }
                                            catch
                                            {
                                                item.errorsInfo += "用户基数值不正确,";
                                            }
                                        }
                                        if (item.permissionNum.IsNullOrEmpty())
                                        {
                                            item.errorsInfo += "权限基数值不能为空,";
                                        }
                                        else
                                        {
                                            try
                                            {
                                                constraint.permissionNum = int.Parse(item.permissionNum);
                                            }
                                            catch
                                            {
                                                item.errorsInfo += "权限基数值不正确,";
                                            }
                                        }
                                    }

                                    break;
                                case "先决约束":
                                    {
                                        constraint.constraintType.Add(2);
                                        var mList = new List<string>();
                                        if (item.prerequisite.IsNullOrEmpty())
                                        {
                                            item.errorsInfo += "先决岗位不能为空,";
                                        }
                                        else
                                        {
                                            foreach (var m in item.prerequisite.Split(","))
                                            {
                                                var mutual = pList.Find(x => x.EnCode.Equals(m.Split("/").Last()));
                                                if (mutual != null) mList.Add(mutual.Id);
                                                else item.errorsInfo += string.Format("找不到该 [{0}] 先决岗位,", m);
                                            }
                                        }
                                        constraint.prerequisite = mList;
                                    }

                                    break;
                                default:
                                    item.errorsInfo += string.Format("找不到该 [{0}] 岗位约束类型,", it);
                                    break;
                            }
                        }

                        if (constraint.constraintType.Contains(0) && constraint.constraintType.Contains(2) && constraint.mutualExclusion.Any(x => constraint.prerequisite.Contains(x)))
                            item.errorsInfo += "互斥和先决对象不能是同一个对象";
                        entity.ConditionJson = constraint.ToJsonStringOld();
                    }
                    else
                    {
                        item.errorsInfo += "岗位约束类型不能为空,";
                    }
                }
                else
                {
                    entity.IsCondition = 0;
                }
            }
            else
            {
                entity.IsCondition = 0;
            }

            if (item.organizeId.IsNotEmptyOrNull() && !orgList.Any(x => x.EnCode.Equals(item.organizeId.Split("/").Last())))
            {
                item.errorsInfo += "找不到该所属组织,";
            }

            if (item.organizeId.IsNotEmptyOrNull())
            {
                entity.OrganizeId = orgList.Find(x => x.EnCode.Equals(item.organizeId.Split("/").Last()))?.Id;

                if (pList.Any(o => o.OrganizeId.Equals(entity.OrganizeId) && o.FullName == item.fullName && o.DeleteMark == null))
                {
                    if (!item.errorsInfo.Contains("岗位名称值已存在")) item.errorsInfo += "岗位名称值已存在,";
                }
            }

            if (pList.Any(o => o.EnCode == item.enCode && o.DeleteMark == null))
            {
                if (!item.errorsInfo.Contains("岗位编码值已存在")) item.errorsInfo += "岗位编码值已存在,";
            }
            entity.EnabledMark = 1;

            var parentEntity = pList.Where(x => x.EnCode.Equals(item.enCode)).FirstOrDefault();
            if (parentEntity == null)
            {
                var pe = list.Where(x => x.id.Equals(item.parentId)).FirstOrDefault();
                if (pe != null)
                {
                    parentEntity = new PositionEntity();
                    parentEntity.Id = pe.id;
                    parentEntity.ParentId = pe.parentId;
                    parentEntity.PositionIdTree = pe.positionIdTree;
                }
            }

            // 层级处理
            List<string>? idList = new List<string>();
            if (parentEntity != null)
            {
                List<string>? ids = parentEntity.PositionIdTree.Split(",").ToList();
                idList.AddRange(ids);
                idList.Add(entity.Id);
            }
            else
            {
                idList.Add(entity.Id);
            }
            entity.PositionIdTree = string.Join(",", idList);
            if (idList.Count > layer) item.errorsInfo += string.Format("该岗位限制{0}级,已达上限,", layer);
            if (item.errorsInfo.IsNullOrEmpty()) addList.Add(entity);
        }

        if (addList.Any())
        {
            foreach (var addItem in addList.Where(x => x.IsCondition.Equals(1) && x.ConditionJson.IsNotEmptyOrNull()))
            {
                var constraint = addItem.ConditionJson.ToObject<PositionConstraintModel>();
                if (constraint.constraintType.Contains(0) && constraint.mutualExclusion.Any())
                {
                    var mePosList = await _repository.AsQueryable().Where(x => constraint.mutualExclusion.Contains(x.Id)).ToListAsync();
                    if (mePosList.Any())
                    {
                        foreach (var moPos in mePosList)
                        {
                            var newCond = new PositionConstraintModel() { constraintType = new List<int>(), mutualExclusion = new List<string>(), prerequisite = new List<string>() };
                            if (moPos.ConditionJson != null) newCond = moPos.ConditionJson.ToObject<PositionConstraintModel>();
                            if (!newCond.constraintType.Contains(0)) newCond.constraintType.Insert(0, 0);
                            if (!newCond.mutualExclusion.Contains(addItem.Id)) newCond.mutualExclusion.Add(addItem.Id);
                            moPos.IsCondition = 1;
                            moPos.ConditionJson = newCond.ToJsonString();
                        }

                        await _repository.AsSugarClient().Updateable(mePosList).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();
                    }

                    var inputPosList = addList.Where(x => constraint.mutualExclusion.Contains(x.Id)).ToList();
                    if (inputPosList.Any())
                    {
                        foreach (var moPos in inputPosList)
                        {
                            var newCond = new PositionConstraintModel() { constraintType = new List<int>(), mutualExclusion = new List<string>(), prerequisite = new List<string>() };
                            if (moPos.ConditionJson != null) newCond = moPos.ConditionJson.ToObject<PositionConstraintModel>();
                            if (!newCond.constraintType.Contains(0)) newCond.constraintType.Insert(0, 0);
                            if (!newCond.mutualExclusion.Contains(addItem.Id)) newCond.mutualExclusion.Add(addItem.Id);
                            moPos.IsCondition = 1;
                            moPos.ConditionJson = newCond.ToJsonString();
                        }
                    }
                }
            }

            try
            {
                // 新增记录
                var newEntity = await _repository.AsInsertable(addList).CallEntityMethod(m => m.Create()).ExecuteReturnEntityAsync();

            }
            catch (Exception)
            {
                list.ForEach(item => item.errorsInfo = "系统异常");
                list = new List<PositionListImportDataInput>();
            }
        }

        var errorList = new List<PositionListImportDataInput>();
        foreach (var item in inputList)
        {
            var it = list.Find(x => x.id.Equals(item.id));
            if (it.errorsInfo.IsNotEmptyOrNull())
            {
                item.errorsInfo = it.errorsInfo.TrimEnd(',');
                errorList.Add(item);
            }
        }
        return new object[] { addList, errorList };
    }

    private string ImportInitIdTree(PositionListImportDataInput dataItem, List<PositionListImportDataInput> list)
    {
        var parentItem = list.FirstOrDefault(x => x.id.Equals(dataItem.parentId));
        if (parentItem != null)
        {
            dataItem.parentId = parentItem.id;
            return ImportInitIdTree(parentItem, list) + "," + dataItem.id;
        }
        else
        {
            return dataItem.id;
        }
    }

    /// <summary>
    /// 字段对应 列名称.
    /// </summary>
    /// <param name="isErrorsInfo">是否错误信息列.</param>
    /// <returns></returns>
    private List<ExportImportHelperModel> GetInfoFieldToTitle(bool isErrorsInfo = false)
    {
        var res = new List<ExportImportHelperModel>();
        if (isErrorsInfo) res.Add(new ExportImportHelperModel() { ColumnKey = "errorsInfo", ColumnValue = "异常原因", FreezePane = true });
        res.Add(new ExportImportHelperModel() { ColumnKey = "organizeId", ColumnValue = "所属组织", Required = true });
        res.Add(new ExportImportHelperModel() { ColumnKey = "parentId", ColumnValue = "上级岗位" });
        res.Add(new ExportImportHelperModel() { ColumnKey = "fullName", ColumnValue = "岗位名称", Required = true });
        res.Add(new ExportImportHelperModel() { ColumnKey = "enCode", ColumnValue = "岗位编码" });
        res.Add(new ExportImportHelperModel() { ColumnKey = "isCondition", ColumnValue = "岗位约束" });
        res.Add(new ExportImportHelperModel() { ColumnKey = "constraintType", ColumnValue = "约束类型" });
        res.Add(new ExportImportHelperModel() { ColumnKey = "mutualExclusion", ColumnValue = "互斥岗位" });
        res.Add(new ExportImportHelperModel() { ColumnKey = "userNum", ColumnValue = "用户基数" });
        res.Add(new ExportImportHelperModel() { ColumnKey = "permissionNum", ColumnValue = "权限基数" });
        res.Add(new ExportImportHelperModel() { ColumnKey = "prerequisite", ColumnValue = "先决岗位" });
        res.Add(new ExportImportHelperModel() { ColumnKey = "sortCode", ColumnValue = "排序" });
        res.Add(new ExportImportHelperModel() { ColumnKey = "description", ColumnValue = "说明" });

        return res;
    }

    #endregion

}