using JNPF.DependencyInjection;

namespace JNPF.Engine.Entity.Model;

/// <summary>
/// 表单数据模型.
/// </summary>
[SuppressSniffer]
public class FormDataModel
{
    /// <summary>
    /// 模块.
    /// </summary>
    public string areasName { get; set; }

    /// <summary>
    /// 功能名称.
    /// </summary>
    public List<string> className { get; set; }

    /// <summary>
    /// 后端目录.
    /// </summary>
    public string serviceDirectory { get; set; }

    /// <summary>
    /// 所属模块.
    /// </summary>
    public string module { get; set; }

    /// <summary>
    /// 子表名称集合.
    /// </summary>
    public string subClassName { get; set; }

    /// <summary>
    /// 表单.
    /// </summary>
    public string formRef { get; set; }

    /// <summary>
    /// 表单模型.
    /// </summary>
    public string formModel { get; set; }

    /// <summary>
    /// 尺寸.
    /// </summary>
    public string size { get; set; }

    /// <summary>
    /// 布局方式-文本定位.
    /// </summary>
    public string labelPosition { get; set; }

    /// <summary>
    /// 布局方式-文本宽度.
    /// </summary>
    public int labelWidth { get; set; }

    /// <summary>
    /// 表单控件 标题后缀.
    /// </summary>
    public string labelSuffix { get; set; }

    /// <summary>
    /// 表单规则.
    /// </summary>
    public string formRules { get; set; }

    /// <summary>
    /// 间距.
    /// </summary>
    public int gutter { get; set; }

    /// <summary>
    /// 是否禁用.
    /// </summary>
    public bool disabled { get; set; }

    /// <summary>
    /// 宽度.
    /// </summary>
    public int? span { get; set; }

    /// <summary>
    /// 组件数组.
    /// </summary>
    public List<FieldsModel> fields { get; set; }

    /// <summary>
    /// 弹窗类型.
    /// </summary>
    public string popupType { get; set; }

    /// <summary>
    /// 子级.
    /// </summary>
    public FieldsModel children { get; set; }

    /// <summary>
    /// 取消按钮文本.
    /// </summary>
    public string cancelButtonText { get; set; }

    /// <summary>
    /// 取消按钮文本多语言.
    /// </summary>
    public string cancelButtonTextI18nCode { get; set; }

    /// <summary>
    /// 确认按钮文本.
    /// </summary>
    public string confirmButtonText { get; set; }

    /// <summary>
    /// 确认按钮文本.
    /// </summary>
    public string confirmButtonTextI18nCode { get; set; }

    /// <summary>
    /// 普通弹窗表单宽度.
    /// </summary>
    public string generalWidth { get; set; }

    /// <summary>
    /// 全屏弹窗表单宽度.
    /// </summary>
    public string fullScreenWidth { get; set; }

    /// <summary>
    /// drawer宽度.
    /// </summary>
    public string drawerWidth { get; set; }

    /// <summary>
    /// 是否开启打印.
    /// </summary>
    public bool hasPrintBtn { get; set; }

    /// <summary>
    /// 打印按钮文本.
    /// </summary>
    public string printButtonText { get; set; }

    /// <summary>
    /// 打印按钮文本.
    /// </summary>
    public string printButtonTextI18nCode { get; set; }

    /// <summary>
    /// 打印模板ID.
    /// </summary>
    public List<string> printId { get; set; }

    /// <summary>
    /// 表单样式.
    /// </summary>
    public string formStyle { get; set; }

    /// <summary>
    /// 并发锁定.
    /// </summary>
    public bool concurrencyLock { get; set; }

    /// <summary>
    /// 主键策略(1 雪花ID 2 自增长ID).
    /// </summary>
    public int primaryKeyPolicy { get; set; } = 1;

    /// <summary>
    /// 逻辑删除.
    /// </summary>
    public bool logicalDelete { get; set; }

    /// <summary>
    /// 是否 确定并继续操作.
    /// </summary>
    public bool hasConfirmAndAddBtn { get; set; }

    /// <summary>
    /// 确定并继续操作 文本.
    /// </summary>
    public string confirmAndAddText { get; set; }

    /// <summary>
    /// 是否开启业务主键.
    /// </summary>
    public bool useBusinessKey { get; set; }

    /// <summary>
    /// 业务主键字段.
    /// </summary>
    public List<string> businessKeyList { get; set; }

    /// <summary>
    /// 业务主键提示语.
    /// </summary>
    public string businessKeyTip { get; set; }

    /// <summary>
    /// 数据日志.
    /// </summary>
    public bool dataLog { get; set; }

    /// <summary>
    /// 主表自定义表名.
    /// </summary>
    public string tableName { get; set; }
}