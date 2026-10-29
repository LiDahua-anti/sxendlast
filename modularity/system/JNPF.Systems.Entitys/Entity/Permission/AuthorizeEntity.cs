using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.Permission;

/// <summary>
/// 实体类：操作权限.
/// </summary>
[SugarTable("BASE_AUTHORIZE")]
public class AuthorizeEntity : CLDEntityBase
{
    /// <summary>
    /// 项目类型：system、module、button、column、resource、standing.
    /// </summary>
    [SugarColumn(ColumnName = "F_ITEM_TYPE")]
    public string ItemType { get; set; }

    /// <summary>
    /// 项目主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_ITEM_ID")]
    public string ItemId { get; set; }

    /// <summary>
    /// 对象类型：role、organize、position.
    /// </summary>
    [SugarColumn(ColumnName = "F_OBJECT_TYPE")]
    public string ObjectType { get; set; }

    /// <summary>
    /// 对象主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_OBJECT_ID")]
    public string ObjectId { get; set; }

    /// <summary>
    /// 组织主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_ORG_ID")]
    public string OrganizeId { get; set; }

    /// <summary>
    /// A集合是否存在B集合.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object obj)
    {
        if (obj is AuthorizeEntity)
        {
            AuthorizeEntity authorizeEntity = obj as AuthorizeEntity;
            return ItemType == authorizeEntity.ItemType && ItemId == authorizeEntity.ItemId && ObjectId == authorizeEntity.ObjectId && ObjectType == authorizeEntity.ObjectType;
        }

        return false;
    }

    /// <summary>
    /// 实体哈希值.
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode()
    {
        return ItemType.GetHashCode() ^ ItemId.GetHashCode() ^ ObjectId.GetHashCode() ^ ObjectType.GetHashCode();
    }
}