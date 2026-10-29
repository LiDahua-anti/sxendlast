using JNPF.Common.Const;
using JNPF.Common.Extension;
using JNPF.Common.Security;
using JNPF.Engine.Entity.Model;
using JNPF.Systems.Entitys.System;
using JNPF.VisualDev.Entitys;

namespace JNPF.VisualDev.Engine.Security;

/// <summary>
/// 代码生成导出字段帮助类.
/// </summary>
public class CodeGenExportFieldHelper
{
    /// <summary>
    /// 获取主表字段名.
    /// </summary>
    /// <param name="list"></param>
    /// <param name="comlexList">复杂表头.</param>
    /// <returns></returns>
    public static string ExportColumnField(List<IndexGridFieldModel>? list, List<ComplexHeaderModel> comlexList)
    {
        var columnSb = new List<string>();
        if (list != null)
        {
            foreach (var item in list)
            {
                if (comlexList.Any(x => x.childColumns.Any(xx => xx.Equals(item.prop))))
                {
                    var columns = comlexList.FirstOrDefault(x => x.childColumns.Any(yy => yy.Equals(item.prop))).childColumns;
                    item.currentIndex = list.IndexOf(list.Find(x => x.id.Equals(columns.FirstOrDefault())));
                    if (columns.FirstOrDefault().Equals(item.id)) item.currentIndex--;
                }
                else
                {
                    item.currentIndex = list.IndexOf(list.Find(x => x.id.Equals(item.id)));
                }
            }

            list = list.OrderBy(x => x.currentIndex).ToList();

            foreach (var item in list)
            {
                if (comlexList.Any(x => x.childColumns.Any(xx => xx.Equals(item.prop))))
                {
                    var comlex = comlexList.FirstOrDefault(x => x.childColumns.Any(xx => xx.Equals(item.prop)));

                    // 复杂表头格式 label 调整
                    var comlexLabel = string.Format("{0}@@{1}@@{2}@@{3}", comlex.id, comlex.fullName, comlex.align, item.label);
                    columnSb.Add(string.Format("{{\\\"value\\\":\\\"{0}\\\",\\\"field\\\":\\\"{1}\\\"}}", comlexLabel, item.prop));
                }
                else
                {
                    if (item.__config__.parentVModel.IsNotEmptyOrNull() && item.__config__.parentVModel.ToLower().Contains("tablefield"))
                    {
                        foreach (var it in list.Where(x => x.prop.Contains(item.__config__.parentVModel)))
                        {
                            var addItem = string.Format("{{\\\"value\\\":\\\"{0}\\\",\\\"field\\\":\\\"{1}\\\"}}", it.label, it.prop);
                            if (!columnSb.Any(x => x.Equals(addItem))) columnSb.Add(addItem);
                        }
                    }
                    else
                    {
                        var addItem = string.Format("{{\\\"value\\\":\\\"{0}\\\",\\\"field\\\":\\\"{1}\\\"}}", item.label, item.prop);
                        if (!columnSb.Any(x => x.Equals(addItem))) columnSb.Add(addItem);
                    }
                }
            }
        }

        return string.Join(",", columnSb);
    }

    /// <summary>
    /// 获取导入字段.
    /// </summary>
    /// <param name="templateEntity"></param>
    /// <param name="configModel"></param>
    /// <param name="dbLink"></param>
    /// <returns></returns>
    public static string ImportColumnField(VisualDevEntity templateEntity, JNPF.Engine.Entity.Model.CodeGen.CodeGenConfigModel configModel, DbLinkEntity dbLink)
    {
        var resDic = new Dictionary<string, string>();

        foreach (var item in configModel.TableField)
        {
            var lowerColumnName = dbLink.DbType.ToLower().Equals("oracle") ? item.OriginalColumnName : item.LowerColumnName;
            var columnName = templateEntity.EnableFlow.Equals(1) ? item.LowerColumnName : item.OriginalColumnName;
            if (item.IsAuxiliary) resDic.Add(string.Format("jnpf_{0}_jnpf_{1}", item.TableName, lowerColumnName), string.Format("jnpf_{0}_jnpf_{1}", item.TableName, columnName));
            else resDic.Add(lowerColumnName, columnName);
        }

        if (configModel.TableRelations != null && configModel.TableRelations.Any())
        {
            foreach (var table in configModel.TableRelations)
            {
                foreach (var item in table.ChilderColumnConfigList)
                {
                    var lowerColumnName = dbLink.DbType.ToLower().Equals("oracle") ? item.OriginalColumnName : item.LowerColumnName;
                    var columnName = templateEntity.EnableFlow.Equals(1) ? lowerColumnName : item.OriginalColumnName;
                    resDic.Add(string.Format("{0}-{1}", table.ControlModel, lowerColumnName), string.Format("{0}-{1}", table.ControlModel, columnName));
                }
            }
        }

        var res = new List<string>();
        if (templateEntity.ColumnData.IsNotEmptyOrNull())
        {
            var columnDesignModel = templateEntity.ColumnData.ToObject<ColumnDesignModel>();
            if (columnDesignModel.type.Equals(3) || columnDesignModel.type.Equals(5)) columnDesignModel.complexHeaderList.Clear();

            if (columnDesignModel.uploaderTemplateJson != null && columnDesignModel.uploaderTemplateJson.selectKey != null)
            {
                var excList = columnDesignModel.uploaderTemplateJson.selectKey.Except(columnDesignModel.defaultColumnList.Select(xx => xx.id));
                foreach (var item in excList)
                {
                    columnDesignModel.defaultColumnList.Insert(columnDesignModel.uploaderTemplateJson.selectKey.IndexOf(item), new IndexGridFieldModel()
                    {
                        id = item,
                        jnpfKey = item,
                    });
                }

                foreach (var vModel in columnDesignModel.defaultColumnList)
                {
                    if (vModel != null && (vModel.jnpfKey.Equals(JnpfKeyConst.CREATETIME) || vModel.jnpfKey.Equals(JnpfKeyConst.CREATEUSER) || vModel.jnpfKey.Equals(JnpfKeyConst.BILLRULE) ||
                        vModel.jnpfKey.Equals(JnpfKeyConst.MODIFYUSER) || vModel.jnpfKey.Equals(JnpfKeyConst.MODIFYTIME) || vModel.jnpfKey.Equals(JnpfKeyConst.CURRDEPT)
                         || vModel.jnpfKey.Equals(JnpfKeyConst.CURRORGANIZE) || vModel.jnpfKey.Equals(JnpfKeyConst.CURRPOSITION))) continue;
                    var item = columnDesignModel.uploaderTemplateJson.selectKey.FirstOrDefault(x => x.Equals(vModel.id));
                    if (item != null)
                    {
                        if (columnDesignModel.complexHeaderList.Any(x => x.childColumns.Any(xx => xx.Equals(item))))
                        {
                            var chItems = columnDesignModel.complexHeaderList.First(x => x.childColumns.Any(xx => xx.Equals(item))).childColumns;
                            chItems.ForEach(it =>
                            {
                                if (columnDesignModel.uploaderTemplateJson.selectKey.Contains(it) && !res.Contains(resDic[it])) res.Add(resDic[it]);
                            });
                        }
                        else
                        {
                            if (!res.Contains(resDic[item]))
                            {
                                if (resDic[item].ToLower().Contains("tablefield") && resDic[item].Contains("-"))
                                {
                                    var ctModel = resDic[item].Split("-").First() + "-";

                                    foreach (var it in columnDesignModel.defaultColumnList.Where(x => x.id.Contains(ctModel)))
                                        if (columnDesignModel.uploaderTemplateJson.selectKey.Any(x => x.Equals(it.id))) res.Add(resDic[it.id]);
                                }
                                else
                                {
                                    res.Add(resDic[item]);
                                }
                            }
                        }
                    }
                }
            }
        }

        return "{\"" + string.Join("\",\"", res) + "\"}";
    }

    /// <summary>
    /// 获取跨库.
    /// </summary>
    /// <param name="DefaultLink"></param>
    /// <returns></returns>
    public static string GetDefaultDbNameByDbType(DbLinkEntity DefaultLink)
    {
        var res = string.Empty;
        switch (DefaultLink.DbType.ToLower())
        {
            case "sqlserver":
                res = string.Format("{0}.dbo", DefaultLink.ServiceName);
                break;
            case "mysql":
                res = DefaultLink.ServiceName;
                break;
        }

        return res;
    }
}
