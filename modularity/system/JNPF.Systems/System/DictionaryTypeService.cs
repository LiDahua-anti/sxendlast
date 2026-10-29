using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Dto.DictionaryType;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.System;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Systems;

/// <summary>
/// 字典分类
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "DictionaryType", Order = 202)]
[Route("api/system/[controller]")]
public class DictionaryTypeService : IDictionaryTypeService, IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基本仓储.
    /// </summary>
    private readonly ISqlSugarRepository<DictionaryTypeEntity> _repository;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 初始化一个<see cref="DictionaryTypeService"/>类型的新实例.
    /// </summary>
    public DictionaryTypeService(
        ISqlSugarRepository<DictionaryTypeEntity> repository,
        IUserManager userManager)
    {
        _repository = repository;
        _userManager = userManager;
    }

    #region Get

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="id">请求参数.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo_Api(string id)
    {
        var data = await GetInfo(id);
        if (data.ParentId.Equals("-1"))
            data.ParentId = data.Type.ToString();

        return data.Adapt<DictionaryTypeInfoOutput>();
    }

    /// <summary>
    /// 列表.
    /// </summary>
    [HttpGet("")]
    public async Task<dynamic> GetList_Api()
    {
        var data = await GetList();

        var businessDictionary = new DictionaryTypeEntity()
        {
            Id = "0",
            FullName = "业务字典",
            Type = 0,
            ParentId = "-1"
        };
        var systemDictionary = new DictionaryTypeEntity()
        {
            Id = "1",
            FullName = "系统字典",
            Type = 1,
            ParentId = "-1"
        };
        foreach (var item in data)
        {
            if (item.Type.Equals(0) && item.ParentId.Equals("-1"))
                item.ParentId = businessDictionary.Id;
            else if (item.Type.Equals(1) && item.ParentId.Equals("-1"))
                item.ParentId = systemDictionary.Id;
        }
        data.Add(businessDictionary);
        data.Add(systemDictionary);

        var output = data.Adapt<List<DictionaryTypeListOutput>>();

        return new { list = output.ToTree("-1") };
    }

    /// <summary>
    /// 列表.
    /// </summary>
    /// <returns></returns>
    [HttpGet("Selector/{id}")]
    public async Task<dynamic> GetSelector(string id)
    {
        var data = await GetList();
        if (!id.Equals("0"))
            data.RemoveAll(x => x.Id == id);

        var businessDictionary = new DictionaryTypeEntity()
        {
            Id = "0",
            FullName = "业务字典",
            Type = 0,
            ParentId = "-1"
        };
        var systemDictionary = new DictionaryTypeEntity()
        {
            Id = "1",
            FullName = "系统字典",
            Type = 1,
            ParentId = "-1"
        };
        foreach (var item in data)
        {
            if (item.Type.Equals(0) && item.ParentId.Equals("-1"))
                item.ParentId = businessDictionary.Id;
            else if (item.Type.Equals(1) && item.ParentId.Equals("-1"))
                item.ParentId = systemDictionary.Id;
        }
        data.Add(businessDictionary);
        data.Add(systemDictionary);

        var output = data.Adapt<List<DictionaryTypeSelectorOutput>>();
        return new { list = output.ToTree("-1") };
    }

    #endregion

    #region Post

    /// <summary>
    /// 新增.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create_Api([FromBody] DictionaryTypeCrInput input)
    {
        if (await _repository.IsAnyAsync(x => x.EnCode == input.enCode && x.DeleteMark == null) || await _repository.IsAnyAsync(x => x.FullName == input.fullName && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D3001);
        var entity = input.Adapt<DictionaryTypeEntity>();
        if (entity.ParentId.Equals("0") || entity.ParentId.Equals("1"))
        {
            entity.Type = entity.ParentId.ParseToInt();
            entity.ParentId = "-1";
        }
        else
        {
            entity.Type = await _repository.AsQueryable()
                .Where(it => it.Id.Equals(entity.ParentId))
                .Select(it => it.Type)
                .FirstAsync();
        }

        var isOk = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1000);
    }

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">请求参数.</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task Delete_Api(string id)
    {
        if (!await _repository.IsAnyAsync(x => x.Id == id && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D3000);
        if (await AllowDelete(id))
        {
            var isOk = await _repository.AsUpdateable().SetColumns(it => new DictionaryTypeEntity()
            {
                DeleteTime = DateTime.Now,
                DeleteMark = 1,
                DeleteUserId = _userManager.UserId
            }).Where(x => x.Id == id).ExecuteCommandAsync();
            if (isOk < 1)
                throw Oops.Oh(ErrorCode.COM1002);
        }
        else
        {
            throw Oops.Oh(ErrorCode.D3002);
        }
    }

    /// <summary>
    /// 修改.
    /// </summary>
    /// <param name="id">id.</param>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Update_Api(string id, [FromBody] DictionaryTypeUpInput input)
    {
        if (await _repository.IsAnyAsync(x => x.Id != id && x.EnCode == input.enCode && x.DeleteMark == null) || await _repository.IsAnyAsync(x => x.Id != id && x.FullName == input.fullName && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D3001);
        var entity = input.Adapt<DictionaryTypeEntity>();
        if (entity.ParentId.Equals("0") || entity.ParentId.Equals("1"))
        {
            entity.Type = entity.ParentId.ParseToInt();
            entity.ParentId = "-1";
        }
        else
        {
            entity.Type = await _repository.AsQueryable()
                .Where(it => it.Id.Equals(entity.ParentId))
                .Select(it => it.Type)
                .FirstAsync();
        }

        var isOk = await _repository.AsUpdateable(entity).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1001);
    }
    #endregion

    #region PublicMethod

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="id">请求参数.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<DictionaryTypeEntity> GetInfo(string id)
    {
        return await _repository.GetFirstAsync(x => (x.Id == id || x.EnCode == id) && x.DeleteMark == null);
    }

    /// <summary>
    /// 列表.
    /// </summary>
    /// <returns></returns>
    [NonAction]
    public async Task<List<DictionaryTypeEntity>> GetList()
    {
        return await _repository.AsQueryable().Where(x => x.DeleteMark == null).OrderBy(x => x.SortCode).OrderBy(x => x.CreatorTime, OrderByType.Desc).ToListAsync();
    }

    /// <summary>
    /// 递归获取所有分类.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="typeList"></param>
    /// <returns></returns>
    [NonAction]
    public async Task GetListAllById(string id, List<DictionaryTypeEntity> typeList)
    {
        var entity = await GetInfo(id);
        if (entity != null)
        {
            typeList.Add(entity);
            if (await _repository.IsAnyAsync(x => x.ParentId == entity.Id && x.DeleteMark == null))
            {
                var list = await _repository.AsQueryable().Where(x => x.ParentId == entity.Id && x.DeleteMark == null).ToListAsync();
                if (list.Count > 0)
                {
                    foreach (var item in list)
                    {
                        await GetListAllById(item.Id, typeList);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 是否存在上级.
    /// </summary>
    /// <param name="Entities"></param>
    /// <returns></returns>
    public bool IsExistParent(List<DictionaryTypeEntity> Entities)
    {
        foreach (var item in Entities)
        {
            if (_repository.IsAny(x => x.Id == item.ParentId && x.DeleteMark == null))
                return true;
        }

        return false;
    }

    #endregion

    #region PrivateMethod

    /// <summary>
    /// 是否可以删除.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    private async Task<bool> AllowDelete(string id)
    {
        var flag = true;
        if (await _repository.IsAnyAsync(o => o.ParentId.Equals(id) && o.DeleteMark == null))
            return false;
        if (await _repository.AsSugarClient().Queryable<DictionaryDataEntity>().AnyAsync(p => p.DictionaryTypeId.Equals(id) && p.DeleteMark == null))
            return false;
        return flag;
    }

    #endregion
}