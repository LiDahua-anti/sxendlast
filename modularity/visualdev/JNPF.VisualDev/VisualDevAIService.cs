using JNPF.Common.Const;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Extras.Thirdparty.AI;
using JNPF.VisualDev.Entitys.Dto.AI;
using Microsoft.AspNetCore.Mvc;

namespace JNPF.VisualDev;

/// <summary>
/// 在线开发 AI.
/// </summary>
[ApiDescriptionSettings(Tag = "VisualDev", Name = "AI", Order = 178)]
[Route("api/visualdev/[controller]")]
public class VisualDevAIService : IDynamicApiController, ITransient
{
    #region Post

    /// <summary>
    /// AI 建表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns>返回列表.</returns>
    [HttpPost("form")]
    public async Task<dynamic> GetForm([FromBody] KeywordInput input)
    {
        var component = "- input - textarea - inputNumber - switch - radio - checkbox - select - datePicker - timePicker - uploadFile - uploadImg - colorPicker - rate - slider - editor - depSelect - posSelect - userSelect - roleSelect - areaSelect - signature - sign - location";
        var systemQuestion = "根据当前业务需求，设计相应的表单结构。请仅返回JSON数据，不包含其他任何形式的内容。预期结果是一个JSON数组，因涉及不同表单需求，故可能包含多个表单对象。请确保命名规避数据库与编程保留字。\n" +
            "所需表单应充分利用以下组件列表进行设计： " + component + "。\n" +
            "参考给定的JSON格式，属性包含：中文名（tableTitle)、英文名(tableName)、字段列表(fields)；字段列表是一个json数组，包含字段英文名(fieldName)、字段中文名(fieldTitle)等；" +
            "创建表单结构，示例如下： [ { \"tableTitle\": \"商城订单\", \"tableName\": \"online_order_form\", \"fields\": [ {\"fieldTitle\": \"订单编号\", \"fieldName\": \"order_id\", \"fieldDbType\": \"varchar\", \"fieldComponent\": \"input\"}, {\"fieldTitle\": \"订单状态\", \"fieldName\": \"order_status\", \"fieldDbType\": \"int\", \"fieldComponent\": \"radio\", \"fieldOptions\":[{\"id\":\"1\", \"fullName\":\"未付款\"},{\"id\":\"2\", \"fullName\":\"已付款\"}]}] }, { \"tableTitle\": \"订单商品明细\", \"tableName\": \"order_item_details\", \"fields\": [ {\"fieldTitle\": \"订单ID（外键）\", \"fieldName\": \"order_id_fk\", \"fieldDbType\": \"varchar\", \"fieldComponent\": \"input\"}, {\"fieldTitle\": \"商品名称\", \"fieldName\": \"product_name\", \"fieldDbType\": \"varchar\", \"fieldComponent\": \"input\"}, {\"fieldTitle\": \"商品数量\", \"fieldName\": \"quantity\", \"fieldDbType\": \"int\", \"fieldComponent\": \"inputNumber\"}] } ]\n" +
            "请依据实际业务逻辑，合理选择组件与字段类型，确保设计的表单既能满足数据收集需求，又便于用户操作。";
        var userQuestion = new List<string>() { string.Format("当前需求：{0}", input.keyword) };

        // 请求 AI
        var result = await AIUtil.SendAIRequestAsync(userQuestion, systemQuestion);

        var output = new VisualDevAIFormOutput();
        if (result.IsNotEmptyOrNull())
        {
            var startIndex = result.IndexOf('[');
            var endIndex = result.LastIndexOf(']');
            if (startIndex != -1 && endIndex != -1 && startIndex < endIndex)
                result = result.Substring(startIndex, endIndex - startIndex + 1);

            var tableList = result.ToString().ToObject<List<AIFormModel>>();
            if (tableList.Count > 0)
            {
                output.fullName = tableList[0].tableTitle;
                output.enCode = tableList[0].tableName.UnderlineToHump();
                foreach (var table in tableList)
                {
                    if (table.Equals(tableList[0])) table.isMain = true;

                    var newFields = new List<AIFormFieldModel>();
                    foreach (var field in table.fields)
                    {
                        if (component.Contains(field.fieldComponent) && !field.fieldName.ToLower().EndsWith("_fk"))
                        {
                            // 子表不能有的控件
                            if (!table.isMain)
                            {
                                if (field.fieldComponent.Equals(JnpfKeyConst.RADIO) || field.fieldComponent.Equals(JnpfKeyConst.CHECKBOX))
                                    field.fieldComponent = JnpfKeyConst.SELECT;

                                if (field.fieldComponent.Equals(JnpfKeyConst.TEXTAREA) || field.fieldComponent.Equals(JnpfKeyConst.LINK) ||
                                    field.fieldComponent.Equals(JnpfKeyConst.BUTTON) || field.fieldComponent.Equals(JnpfKeyConst.ALERT) ||
                                    field.fieldComponent.Equals(JnpfKeyConst.BARCODE) || field.fieldComponent.Equals(JnpfKeyConst.QRCODE) ||
                                    field.fieldComponent.Equals(JnpfKeyConst.EDITOR) || field.fieldComponent.Equals(JnpfKeyConst.JNPFTEXT))
                                {
                                    field.fieldComponent = JnpfKeyConst.COMINPUT;
                                }
                            }

                            field.fieldName = string.Format("{0}_num{1}", field.fieldName, new Random().NextNumberString(3));
                            newFields.Add(field);
                        }
                    }

                    table.fields = newFields;
                }

                output.aiModelList = tableList;
            }
        }

        return output;
    }

    #endregion
}
