using JNPF.Common.Const;
using JNPF.Common.Extension;
using JNPF.Common.Security;
using JNPF.Engine.Entity.Model;
using JNPF.Engine.Entity.Model.CodeGen;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace JNPF.VisualDev.Engine.Security;

/// <summary>
/// 代码生成表单控件设计帮助类.
/// </summary>
public class CodeGenFormControlDesignHelper
{
    private static int active = 1;
    private static bool hasSpecialTime = false;
    private static bool hasSpecialDate = false;
    private static bool hasSubTableDataTransfer = false;
    private static List<CodeGenFrontEndFormState> mainLinkageSubTable = new List<CodeGenFrontEndFormState>();

    /// <summary>
    /// 表单控件设计.
    /// </summary>
    /// <param name="fieldList">组件列表.</param>
    /// <param name="realisticControls">真实控件.</param>
    /// <param name="formDataModel">表单模型.</param>
    /// <param name="columnDesignModel">列表显示列列表.</param>
    /// <param name="dataType">数据类型
    /// 1-普通列表,2-左侧树形+普通表格,3-分组表格,4-编辑表格.</param>
    /// <param name="logic">4-PC,5-App.</param>
    /// <param name="vueVersion">vue版本.</param>
    /// <param name="isMain">是否主循环.</param>
    /// <returns></returns>
    public static List<FormControlDesignModel> FormControlDesign(List<FieldsModel> fieldList, List<FieldsModel> realisticControls, FormDataModel formDataModel, List<IndexGridFieldModel> columnDesignModel, int dataType, int logic, int vueVersion, bool isMain = false)
    {
        if (isMain) active = 1;
        List<FormControlDesignModel> list = new List<FormControlDesignModel>();
        foreach (var item in fieldList.ToJsonString().ToObject<List<FieldsModel>>())
        {
            var config = item.__config__;

            // 控件标题 + 后缀
            if (formDataModel.labelSuffix.IsNotEmptyOrNull() && config != null && config.label != null && !config.jnpfKey.Equals(JnpfKeyConst.TABLE) && (config.parentVModel == null || !config.parentVModel.Contains("tableField")))
            {
                if (config.label.IsNullOrEmpty()) config.showLabel = false;
                config.label += formDataModel.labelSuffix;
            }

            var configLabel = logic == 5 && config.labelI18nCode.IsNotEmptyOrNull() ? ":label=\"$t('" + config.labelI18nCode + "', '" + config.label + "')\"" : (config.label.IsNullOrEmpty() ? string.Empty : string.Format("label='{0}'", config.label));

            var needTemplateJson = new List<string>() { JnpfKeyConst.POPUPTABLESELECT, JnpfKeyConst.POPUPSELECT, JnpfKeyConst.AUTOCOMPLETE };
            var specialDateAttribute = new List<string>() { JnpfKeyConst.DATE, JnpfKeyConst.TIME };
            switch (config.jnpfKey)
            {
                case JnpfKeyConst.CALCULATE:
                case JnpfKeyConst.DATECALCULATE:
                    break;
                case JnpfKeyConst.ROW:
                    {
                        List<FormControlDesignModel> childrenCollapseList = FormControlDesign(config.children, realisticControls, formDataModel, columnDesignModel, dataType, logic, 2);

                        list.Add(new FormControlDesignModel()
                        {
                            jnpfKey = config.jnpfKey,
                            Span = config.span,
                            Gutter = formDataModel.gutter,
                            Children = childrenCollapseList,
                            LabelWidth = config?.labelWidth ?? formDataModel.labelWidth,
                            IsRelationForm = childrenCollapseList.Any(x => x.IsRelationForm),
                        });
                    }

                    break;
                case JnpfKeyConst.TABLEGRID:
                    {
                        List<FormControlDesignModel> childrenCollapseList = FormControlDesign(config.children, realisticControls, formDataModel, columnDesignModel, dataType, logic, 2);

                        list.Add(new FormControlDesignModel()
                        {
                            jnpfKey = config.jnpfKey,
                            Style = "{'--borderType':'" + config.borderType + "','--borderColor':'" + config.borderColor + "','--borderWidth':'" + config.borderWidth + "px'}",
                            Children = childrenCollapseList,
                            IsRelationForm = childrenCollapseList.Any(x => x.IsRelationForm),
                        });
                    }

                    break;
                case JnpfKeyConst.TABLEGRIDTR:
                    {
                        List<FormControlDesignModel> childrenCollapseList = FormControlDesign(config.children, realisticControls, formDataModel, columnDesignModel, dataType, logic, 2);
                        list.Add(new FormControlDesignModel()
                        {
                            jnpfKey = config.jnpfKey,
                            Children = childrenCollapseList,
                            IsRelationForm = childrenCollapseList.Any(x => x.IsRelationForm),
                        });
                    }

                    break;
                case JnpfKeyConst.TABLEGRIDTD:
                    {
                        if (!config.merged)
                        {
                            List<FormControlDesignModel> childrenCollapseList = FormControlDesign(config.children, realisticControls, formDataModel, columnDesignModel, dataType, logic, 2);

                            list.Add(new FormControlDesignModel()
                            {
                                jnpfKey = config.jnpfKey,
                                Colspan = config.colspan,
                                Rowspan = config.rowspan,
                                Style = "{'--backgroundColor':'" + config.backgroundColor + "'}",
                                Children = childrenCollapseList,
                                IsRelationForm = childrenCollapseList.Any(x => x.IsRelationForm),
                            });
                        }
                    }

                    break;
                case JnpfKeyConst.TABLE:
                    {
                        List<FormControlDesignModel> childrenTableList = new List<FormControlDesignModel>();
                        var summaryFieldLabelWidth = new List<object>();
                        var childrenRealisticControls = realisticControls.Find(it => it.__vModel__.Equals(item.__vModel__) && it.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE)).__config__.children;
                        var scopeRow = (item.layoutType.Equals("list") && logic == 4) ? string.Format("dataForm.{0}[index]", item.__vModel__) : "scope.row";
                        if (logic == 5)
                        {
                            foreach (var btnItem in item.columnBtnsList)
                                btnItem.label = btnItem.labelI18nCode.IsNotEmptyOrNull() ? "{{this.$t('" + btnItem.labelI18nCode + "','" + btnItem.label + "')}}" : btnItem.label;
                            foreach (var btnItem in item.footerBtnsList)
                                btnItem.label = btnItem.labelI18nCode.IsNotEmptyOrNull() ? "{{this.$t('" + btnItem.labelI18nCode + "','" + btnItem.label + "')}}" : btnItem.label;
                            if (item.layoutType == "table")
                                configLabel = config.labelI18nCode.IsNotEmptyOrNull() ? "{{$t('" + config.labelI18nCode + "', '" + config.label + "')}}" : (config.label.IsNullOrEmpty() ? string.Empty : config.label);
                        }
                        foreach (var children in config.children)
                        {
                            if (logic == 5 && children.__config__.jnpfKey.Equals(JnpfKeyConst.CALCULATE)) continue;
                            var childrenConfig = children.__config__;
                            switch (childrenConfig.jnpfKey)
                            {
                                case JnpfKeyConst.CALCULATE:
                                case JnpfKeyConst.DATECALCULATE:
                                    break;
                                case JnpfKeyConst.RELATIONFORMATTR:
                                case JnpfKeyConst.POPUPATTR:
                                    {
                                        var relationField = Regex.Match(children.relationField, @"^(.+)_jnpfTable_").Groups[1].Value;
                                        relationField = relationField.Replace(string.Format("jnpf_{0}_jnpf_", childrenConfig.relationTable), "");
                                        var relationControl = config.children.Find(it => it.__vModel__ == relationField);
                                        childrenTableList.Add(new FormControlDesignModel()
                                        {
                                            vModel = children.__vModel__.IsNotEmptyOrNull() ? string.Format("v-model=\"{1}.{0}\"", children.__vModel__, scopeRow) : string.Empty,
                                            Style = children.style != null && !children.style.ToString().Equals("{}") ? $":style='{children.style.ToJsonString().Replace("\r\n", "")}' " : string.Empty,
                                            jnpfKey = childrenConfig.jnpfKey,
                                            OriginalName = children.isStorage == 1 ? children.__vModel__ : relationField,
                                            Name = children.isStorage == 1 ? children.__vModel__ : relationField,
                                            RelationField = relationField,
                                            ShowField = children.showField,
                                            NoShow = children.isStorage == 1 ? childrenConfig.noShow : relationControl.__config__.noShow,
                                            Tag = childrenConfig.tag,
                                            Label = logic == 5 && childrenConfig.labelI18nCode.IsNotEmptyOrNull() ? ":label=\"$t('" + childrenConfig.labelI18nCode + "', '" + childrenConfig.label + "')\"" : (childrenConfig.label.IsNullOrEmpty() ? string.Empty : string.Format("label='{0}'", childrenConfig.label)),
                                            TipLabel = logic == 5 && childrenConfig.tipLabel.IsNotEmptyOrNull() && childrenConfig.showLabel && childrenConfig.tipLabelI18nCode.IsNotEmptyOrNull() ? $"$t('{childrenConfig.tipLabelI18nCode}', '{childrenConfig.tipLabel}')" : (childrenConfig.tipLabel.IsNotEmptyOrNull() ? $" '{childrenConfig.tipLabel}' " : null),
                                            Span = childrenConfig.span,
                                            IsStorage = children.isStorage,
                                            LabelWidth = childrenConfig?.labelWidth ?? formDataModel.labelWidth,
                                            ColumnWidth = childrenConfig?.columnWidth != null ? $"width='{childrenConfig.columnWidth}' " : null,
                                            required = childrenConfig.required,
                                            ShowLabel = childrenConfig.showLabel,
                                            Align = childrenConfig.tableAlgin,
                                            TableFixed = childrenConfig.tableFixed,
                                            IsSummary = item.showSummary && item.summaryField.Any(it => it.Equals(children.__vModel__)) ? true : false,
                                        });
                                    }

                                    break;
                                default:
                                    {
                                        var ableIds = string.Empty;
                                        if (childrenConfig.jnpfKey.Equals(JnpfKeyConst.SIGNATURE)) ableIds = children.ableIds.IsNotEmptyOrNull() ? string.Format(":ableIds='{0}' ", children.ableIds.ToJsonString().Replace("\r\n", "")) : string.Empty;
                                        else ableIds = children.selectType != null && children.selectType == "custom" ? string.Format(":ableIds='{0}_{1}_AbleIds' ", item.__vModel__, children.__vModel__) : string.Empty;
                                        var realisticControl = childrenRealisticControls.Find(it => it.__vModel__.Equals(children.__vModel__) && it.__config__.jnpfKey.Equals(childrenConfig.jnpfKey));
                                        realisticControl ??= new FieldsModel();
                                        if (childrenConfig.jnpfKey.Equals(JnpfKeyConst.POPUPSELECT)) children.modelId = children.interfaceId;
                                        childrenTableList.Add(new FormControlDesignModel()
                                        {
                                            jnpfKey = childrenConfig.jnpfKey,
                                            Name = children.__vModel__,
                                            OriginalName = children.__vModel__,
                                            Style = children.style != null && !children.style.ToString().Equals("{}") ? $":style='{children.style.ToJsonString().Replace("\r\n", "")}' " : string.Empty,
                                            Span = childrenConfig.span,
                                            Border = children.border ? "border " : string.Empty,
                                            Placeholder = logic == 5 && children.placeholderI18nCode.IsNotEmptyOrNull() ? $":placeholder=\"$t('{children.placeholderI18nCode}', '{children.placeholder}')\" " : (children.placeholder.IsNotEmptyOrNull() ? $"placeholder='{children.placeholder}' " : string.Empty),
                                            Clearable = children.clearable ? "clearable " : string.Empty,
                                            Readonly = children.@readonly ? "readonly " : string.Empty,
                                            Disabled = children.disabled ? "disabled " : string.Empty,
                                            IsDisabled = string.Format(":disabled=\"judgeWrite('{0}') || judgeWrite('{0}-{1}')\" ", item.__vModel__, children.__vModel__),
                                            ShowWordLimit = children.showWordlimit ? "show-word-limit " : string.Empty,
                                            Type = children.type != null ? $"type='{children.type}' " : string.Empty,
                                            Format = children.format != null ? $"format='{children.format}' " : string.Empty,
                                            ValueFormat = children.valueformat != null ? $"value-format='{children.valueformat}' " : string.Empty,
                                            AutoSize = children.autoSize != null ? $":autosize='{children.autoSize.ToJsonString().Replace("\r\n", "")}' " : string.Empty,
                                            Multiple = (childrenConfig.jnpfKey.Equals(JnpfKeyConst.CASCADER) ? children.multiple : children.multiple) ? $"multiple " : string.Empty,
                                            OptionType = children.optionType != null ? string.Format("optionType=\"{0}\" ", children.optionType) : string.Empty,
                                            PrefixIcon = !string.IsNullOrEmpty(children.prefixIcon) ? $"prefix-icon='{children.prefixIcon}' " : string.Empty,
                                            SuffixIcon = !string.IsNullOrEmpty(children.suffixIcon) ? $"suffix-icon='{children.suffixIcon}' " : string.Empty,
                                            MaxLength = children.maxlength != null ? $":maxlength='{children.maxlength}' " : string.Empty,
                                            ShowPassword = children.showPassword ? "show-password " : string.Empty,
                                            UseScan = children.useScan ? "useScan " : string.Empty,
                                            UseMask = children.useMask ? "useMask " : string.Empty,
                                            MaskConfig = children.maskConfig != null ? $":maskConfig='{children.maskConfig.ToJsonString().Replace("\r\n", "")}' " : string.Empty,
                                            Filterable = children.filterable ? "filterable " : string.Empty,
                                            Label = logic == 5 && childrenConfig.labelI18nCode.IsNotEmptyOrNull() ? ":label=\"$t('" + childrenConfig.labelI18nCode + "', '" + childrenConfig.label + "')\"" : (childrenConfig.label.IsNullOrEmpty() ? string.Empty : string.Format("label='{0}'", childrenConfig.label)),
                                            TipLabel = logic == 5 && childrenConfig.tipLabel.IsNotEmptyOrNull() && childrenConfig.showLabel && childrenConfig.tipLabelI18nCode.IsNotEmptyOrNull() ? $"$t('{childrenConfig.tipLabelI18nCode}', '{childrenConfig.tipLabel}')" : (childrenConfig.tipLabel.IsNotEmptyOrNull() ? $" '{childrenConfig.tipLabel}' " : null),
                                            Props = children.props,
                                            MainProps = children.props != null ? $":props='{string.Format("{0}_{1}", item.__vModel__, children.__vModel__)}Props' " : string.Empty,
                                            Tag = childrenConfig.tag,
                                            Options = children.options != null ? (realisticControl.IsLinkage ? $":options='{string.Format("{1}.{0}", children.__vModel__, scopeRow)}Options' " : $":options='{string.Format("{0}_{1}", item.__vModel__, children.__vModel__)}Options' ") : string.Empty,
                                            ShowAllLevels = children.showAllLevels ? "show-all-levels " : string.Empty,
                                            Separator = !string.IsNullOrEmpty(children.separator) ? $"separator='{children.separator}' " : string.Empty,
                                            RangeSeparator = !string.IsNullOrEmpty(children.rangeseparator) ? $"range-separator='{children.rangeseparator}' " : string.Empty,
                                            StartPlaceholder = !string.IsNullOrEmpty(children.startplaceholder) ? $"start-placeholder='{children.startplaceholder}' " : string.Empty,
                                            EndPlaceholder = !string.IsNullOrEmpty(children.endplaceholder) ? $"end-placeholder='{children.endplaceholder}' " : string.Empty,
                                            PickerOptions = children.pickeroptions != null && children.pickeroptions.ToJsonString() != "null" ? $":picker-options='{children.pickeroptions.ToJsonString()}' " : string.Empty,
                                            Required = childrenConfig.required ? "required " : string.Empty,
                                            Step = children.step != null ? $":step='{children.step}' " : string.Empty,
                                            StepStrictly = children.stepstrictly ? "step-strictly " : string.Empty,
                                            Max = children.max != null && children.max != 0 ? $":max='{children.max}' " : string.Empty,
                                            Min = children.min != null ? $":min='{children.min}' " : string.Empty,
                                            ColumnWidth = childrenConfig.columnWidth != null ? $"width='{childrenConfig.columnWidth}' " : null,
                                            ModelId = children.modelId != null ? children.modelId : string.Empty,
                                            RelationField = children.relationField != null ? $"relationField='{children.relationField}' " : string.Empty,
                                            ColumnOptions = children.columnOptions != null ? $":columnOptions='{string.Format("{0}_{1}", item.__vModel__, children.__vModel__)}Options' " : string.Empty,
                                            TemplateJson = needTemplateJson.Contains(childrenConfig.jnpfKey) ? string.Format(":templateJson='{0}_{1}TemplateJson' ", item.__vModel__, children.__vModel__) : string.Empty,
                                            HasPage = children.hasPage ? "hasPage " : string.Empty,
                                            PageSize = children.pageSize != null ? $":pageSize='{children.pageSize}' " : string.Empty,
                                            PropsValue = children.propsValue != null ? $"propsValue='{children.propsValue}' " : string.Empty,
                                            InterfaceId = children.interfaceId != null ? $"interfaceId='{children.interfaceId}' " : string.Empty,
                                            Precision = children.precision != null ? $":precision='{children.precision}' " : string.Empty,
                                            ActiveText = !string.IsNullOrEmpty(children.activeTxt) ? $"active-text='{children.activeTxt}' " : string.Empty,
                                            InactiveText = !string.IsNullOrEmpty(children.inactiveTxt) ? $"inactive-text='{children.inactiveTxt}' " : string.Empty,
                                            ActiveColor = !string.IsNullOrEmpty(children.activecolor) ? $"active-color='{children.activecolor}' " : string.Empty,
                                            InactiveColor = !string.IsNullOrEmpty(children.inactivecolor) ? $"inactive-color='{children.inactivecolor}' " : string.Empty,
                                            IsSwitch = childrenConfig.jnpfKey == JnpfKeyConst.SWITCH ? $":active-value='{children.activeValue}' :inactive-value='{children.inactiveValue}' " : string.Empty,
                                            ShowStops = children.showstops ? $"show-stops " : string.Empty,
                                            Accept = !string.IsNullOrEmpty(children.accept) ? $"accept='{children.accept}' " : string.Empty,
                                            ShowTip = children.showTip ? $"showTip " : string.Empty,
                                            FileSize = children.fileSize != null && !string.IsNullOrEmpty(children.fileSize.ToString()) ? $":fileSize='{children.fileSize}' " : string.Empty,
                                            SizeUnit = !string.IsNullOrEmpty(children.sizeUnit) ? $"sizeUnit='{children.sizeUnit}' " : string.Empty,
                                            Limit = children.limit != null ? $":limit='{children.limit}' " : string.Empty,
                                            ButtonText = logic == 5 && children.buttonTextI18nCode.IsNotEmptyOrNull() ? $":buttonText=\"$t('{children.buttonTextI18nCode}', '{children.buttonText}')\" " : (children.buttonText.IsNotEmptyOrNull() ? $"buttonText='{children.buttonText}' " : string.Empty),
                                            ShowType = logic == 5 && children.buttonText.IsNotEmptyOrNull() ? $" buttonText='{children.buttonText}' " : string.Empty,
                                            Level = childrenConfig.jnpfKey == JnpfKeyConst.ADDRESS ? $":level='{children.level}' " : string.Empty,
                                            NoShow = childrenConfig.noShow,
                                            Prepend = children.__slot__ != null && !string.IsNullOrEmpty(children.__slot__.prepend) ? children.__slot__.prepend : null,
                                            Append = children.__slot__ != null && !string.IsNullOrEmpty(children.__slot__.append) ? children.__slot__.append : null,
                                            ShowLevel = !string.IsNullOrEmpty(children.showLevel) ? string.Empty : string.Empty,
                                            LabelWidth = childrenConfig?.labelWidth ?? formDataModel.labelWidth,
                                            IsStorage = item.isStorage,
                                            PopupType = !string.IsNullOrEmpty(children.popupType) ? $"popupType='{children.popupType}' " : string.Empty,
                                            PopupTitle = !string.IsNullOrEmpty(children.popupTitle) ? $"popupTitle='{children.popupTitle}' " : string.Empty,
                                            PopupWidth = !string.IsNullOrEmpty(children.popupWidth) ? $"popupWidth='{children.popupWidth}' " : string.Empty,
                                            Field = !item.layoutType.Equals("list") && (childrenConfig.jnpfKey.Equals(JnpfKeyConst.RELATIONFORM) || childrenConfig.jnpfKey.Equals(JnpfKeyConst.POPUPSELECT)) ? $":field=\"'{children.__vModel__}'+scope.$index\" " : string.Empty,
                                            required = childrenConfig.required,
                                            SelectType = children.selectType != null ? children.selectType : string.Empty,
                                            AbleIds = ableIds,
                                            UserRelationAttr = GetUserRelationAttr(children, realisticControls, logic),
                                            IsLinked = realisticControl.IsLinked,
                                            IsLinkage = realisticControl.IsLinkage,
                                            IsRelationForm = childrenConfig.jnpfKey.Equals(JnpfKeyConst.RELATIONFORM),
                                            PathType = !string.IsNullOrEmpty(children.pathType) ? string.Format("pathType=\"{0}\" ", children.pathType) : string.Empty,
                                            IsAccount = children.isAccount != -1 ? string.Format(":isAccount=\"{0}\" ", children.isAccount) : string.Empty,
                                            Folder = !string.IsNullOrEmpty(children.folder) ? string.Format("folder=\"{0}\" ", children.folder) : string.Empty,
                                            DefaultCurrent = childrenConfig.defaultCurrent,
                                            Direction = !string.IsNullOrEmpty(children.direction) ? string.Format("direction=\"{0}\" ", children.direction) : string.Empty,
                                            Total = children.total > 0 ? string.Format(":total=\"{0}\" ", children.total) : string.Empty,
                                            AddonBefore = !string.IsNullOrEmpty(children.addonBefore) ? string.Format("addonBefore=\"{0}\" ", children.addonBefore) : string.Empty,
                                            AddonAfter = !string.IsNullOrEmpty(children.addonAfter) ? string.Format("addonAfter=\"{0}\" ", children.addonAfter) : string.Empty,
                                            Thousands = children.thousands ? string.Format("thousands :controls='false' ", children.addonBefore) : string.Empty,
                                            Controls = children.controls ? "controls " : string.Empty,
                                            AmountChinese = children.isAmountChinese ? string.Format("isAmountChinese ", children.addonBefore) : string.Empty,
                                            ControlsPosition = !string.IsNullOrEmpty(children.controlsPosition) ? $"controlsPosition='{children.controlsPosition}' " : string.Empty,
                                            StartTime = specialDateAttribute.Contains(childrenConfig.jnpfKey) ? SpecialTimeAttributeAssembly(children, realisticControls, 1, false) : string.Empty,
                                            EndTime = specialDateAttribute.Contains(childrenConfig.jnpfKey) ? SpecialTimeAttributeAssembly(children, realisticControls, 2, false) : string.Empty,
                                            TipText = !string.IsNullOrEmpty(children.tipText) ? string.Format("tipText=\"{0}\" ", children.tipText) : string.Empty,
                                            ShowLabel = childrenConfig.showLabel,
                                            Align = childrenConfig.tableAlgin,
                                            TableFixed = childrenConfig.tableFixed,
                                            Count = children.count.ToString(),
                                            EnableLocationScope = string.Format(":enableLocationScope='{0}' ", children.enableLocationScope.ToString().ToLower()),
                                            AutoLocation = string.Format(":autoLocation='{0}' ", children.autoLocation.ToString().ToLower()),
                                            AdjustmentScope = string.Format(":adjustmentScope='{0}' ", children.adjustmentScope),
                                            EnableDesktopLocation = string.Format(":enableDesktopLocation='{0}' ", children.enableDesktopLocation.ToString().ToLower()),
                                            LocationScope = string.Format(":locationScope='{0}' ", children.locationScope.IsNotEmptyOrNull() ? children.locationScope.ToJsonString().Replace("\r\n", "") : "[]"),
                                            ShowCount = childrenConfig.jnpfKey.Equals(JnpfKeyConst.COMINPUT) || childrenConfig.jnpfKey.Equals(JnpfKeyConst.TEXTAREA) ? string.Format(" :showCount='{0}' ", children.showCount.ToString().ToLower()) : string.Empty,
                                            Disaabled = childrenConfig.jnpfKey.Equals(JnpfKeyConst.SIGNATURE) ? string.Format(" :disaabled=\"{0}\" ", children.disaabled.ToString().ToLower()) : string.Empty,
                                            IsInvoke = childrenConfig.jnpfKey.Equals(JnpfKeyConst.SIGN) ? string.Format(" :isInvoke='{0}' ", children.isInvoke.ToString().ToLower()) : string.Empty,
                                            SortRule = children.sortRule.IsNotEmptyOrNull() ? string.Format(" :sortRule='{0}'", children.sortRule.ToJsonString().Replace("\r\n", "")) : string.Empty,
                                            TimeFormat = children.timeFormat.IsNotEmptyOrNull() ? string.Format(" timeFormat='{0}'", children.timeFormat) : string.Empty,
                                            IsSummary = item.showSummary && item.summaryField.Any(it => it.Equals(children.__vModel__)) ? true : false,
                                            QueryType = children.queryType != null ? string.Format(" :queryType={0} ", children.queryType) : string.Empty,
                                        });
                                        if (item.showSummary && item.summaryField.Any(it => it.Equals(children.__vModel__)) ? true : false)
                                            summaryFieldLabelWidth.Add(childrenConfig.labelWidth ?? 0);
                                    }

                                    break;
                            }
                        }

                        // 子表复杂表头
                        var complexList = new List<FormControlDesignModel>();
                        if (logic.Equals(4) && !item.layoutType.Equals("list"))
                        {
                            if (item.__config__.complexHeaderList != null && item.__config__.complexHeaderList.Any())
                            {
                                var tfVModelList = config.children.Where(x => !x.__config__.tableFixed.Equals("none")).Select(x => x.__vModel__).ToList();
                                foreach (var childItem in config.children)
                                {
                                    if (childItem.__config__.tableFixed.Equals("none") && item.__config__.complexHeaderList.Any(x => x.childColumns.Any(xx => xx.Equals(childItem.__vModel__))))
                                    {
                                        var complexPItem = item.__config__.complexHeaderList.FirstOrDefault(x => x.childColumns.Any(xx => xx.Equals(childItem.__vModel__)));
                                        if (!complexList.Any(x => x.Name.Equals(complexPItem.id)))
                                        {
                                            var addItem = new FormControlDesignModel();
                                            addItem.Name = complexPItem.id;
                                            addItem.Align = complexPItem.align;
                                            addItem.Label = complexPItem.fullName;
                                            addItem.ColumnWidth = string.Empty;
                                            addItem.ComplexColumns = new List<FormControlDesignModel>();
                                            addItem.jnpfKey = string.Empty;
                                            complexPItem.childColumns.Where(x => !tfVModelList.Contains(x)).ToList().ForEach(it =>
                                            {
                                                var cItem = childrenTableList.Find(x => x.Name.Equals(it) && x.jnpfKey != JnpfKeyConst.POPUPATTR && x.jnpfKey != JnpfKeyConst.RELATIONFORMATTR);
                                                addItem.ComplexColumns.Add(cItem);
                                            });
                                            complexList.Add(addItem);
                                        }
                                    }
                                    else
                                    {
                                        if (childItem.__vModel__.IsNotEmptyOrNull() && !complexList.Any(x => x.Name.Equals(childItem.__vModel__)))
                                            complexList.Add(childrenTableList.Find(x => x.Name.Equals(childItem.__vModel__)));

                                        if (childItem.__config__.jnpfKey.Equals(JnpfKeyConst.POPUPATTR) || childItem.__config__.jnpfKey.Equals(JnpfKeyConst.RELATIONFORMATTR))
                                        {
                                            var index = config.children.IndexOf(childItem);
                                            complexList.Add(childrenTableList[index]);
                                        }
                                    }
                                }

                                foreach (var complexItem in complexList)
                                {
                                    if (complexItem.ComplexColumns != null && complexItem.ComplexColumns.Any())
                                        complexItem.CurrentIndex = config.children.IndexOf(config.children.Find(x => x.__vModel__.Equals(complexItem.ComplexColumns.FirstOrDefault().Name)));
                                    else
                                        complexItem.CurrentIndex = config.children.IndexOf(config.children.Find(x => x.__vModel__.Equals(complexItem.Name)));
                                }
                            }
                        }

                        item.footerBtnsList.Where(x => x.show).ToList().ForEach(x =>
                        {
                            x.value = x.value.Replace("-", "_");
                            x.actionConfig = x.actionConfig.ToJsonString() + ",";
                        });
                        list.Add(new FormControlDesignModel()
                        {
                            jnpfKey = config.jnpfKey,
                            Name = item.__vModel__,
                            OriginalName = config.tableName,
                            TipLabel = logic == 5 && config.tipLabel.IsNotEmptyOrNull() && config.showTitle && config.tipLabelI18nCode.IsNotEmptyOrNull() ? $"$t('{config.tipLabelI18nCode}', '{config.tipLabel}')" : (config.tipLabel.IsNotEmptyOrNull() ? $" '{config.tipLabel}' " : "''"),
                            Span = config.span,
                            ShowTitle = config.showTitle,
                            Label = logic == 5 && config.labelI18nCode.IsNotEmptyOrNull() ? "$t('" + config.labelI18nCode + "', '" + config.label + "')" : (config.label.IsNullOrEmpty() ? "''" : string.Format("'{0}'", config.label)),
                            ChildTableName = config.tableName.ParseToPascalCase(),
                            Children = complexList.Any() ? complexList : childrenTableList,
                            LabelWidth = config?.labelWidth ?? formDataModel.labelWidth,
                            ShowSummary = item.showSummary,
                            required = childrenTableList.Any(it => it.required.Equals(true)),
                            IsRelationForm = childrenTableList.Any(it => it.IsRelationForm.Equals(true)),
                            DefaultCurrent = childrenTableList.Any(it => it.DefaultCurrent.Equals(true)),
                            ShowLabel = config.showLabel,
                            ComplexColumns = complexList.Any() ? complexList : null,
                            ColumnBtnsList = item.columnBtnsList.Where(x => x.show).ToList(),
                            FooterBtnsList = item.footerBtnsList.Where(x => x.show).ToList(),
                            IsAnyBatchRemove = item.footerBtnsList.Any(x => x.show && x.value.Equals("batchRemove")),
                            LayoutType = item.layoutType,
                            DefaultExpandAll = item.defaultExpandAll,
                            SummaryField = item.summaryField.ToJsonString(),
                            SummaryFieldLabelWidth = summaryFieldLabelWidth.ToJsonString(),
                            IsSummary = item.showSummary,
                            SummaryListStr = childrenTableList.Where(x => x.IsSummary).Select(x => x.Name).ToList().ToJsonString(),
                            IsNumLimit = item.isNumLimit,
                            NumLimit = item.numLimit,
                        });
                    }

                    break;
                case JnpfKeyConst.CARD:
                    {
                        List<FormControlDesignModel> childrenCollapseList = FormControlDesign(config.children, realisticControls, formDataModel, columnDesignModel, dataType, logic, 2);
                        list.Add(new FormControlDesignModel()
                        {
                            jnpfKey = config.jnpfKey,
                            OriginalName = item.__vModel__,
                            Shadow = item.shadow,
                            Children = childrenCollapseList,
                            Span = config.span,
                            Content = logic == 5 && item.headerI18nCode.IsNotEmptyOrNull() ? "$t('" + item.headerI18nCode + "','" + item.header + "')" : (item.header.IsNotEmptyOrNull() ? string.Format("'{0}'", item.header) : string.Empty),
                            LabelWidth = config?.labelWidth ?? formDataModel.labelWidth,
                            TipLabel = logic == 5 && config.tipLabel.IsNotEmptyOrNull() && config.tipLabelI18nCode.IsNotEmptyOrNull() ? $"$t('{config.tipLabelI18nCode}', '{config.tipLabel}')" : (config.tipLabel.IsNotEmptyOrNull() ? string.Format("'{0}'", config.tipLabel) : null),
                            IsRelationForm = childrenCollapseList.Any(x => x.IsRelationForm),
                        });
                    }

                    break;
                case JnpfKeyConst.DIVIDER:
                    {
                        list.Add(new FormControlDesignModel()
                        {
                            Tag = config.tag,
                            jnpfKey = config.jnpfKey,
                            OriginalName = item.__vModel__,
                            Span = config.span,
                            Contentposition = item.contentPosition,
                            Default = item.content,
                            Content = logic == 5 && item.contentI18nCode.IsNotEmptyOrNull() ? $":content=\"$t('{item.contentI18nCode}', '{item.content}')\" " : (item.content.IsNotEmptyOrNull() ? $"content='{item.content}' " : string.Empty),
                            LabelWidth = config?.labelWidth ?? formDataModel.labelWidth,
                        });
                    }

                    break;
                case JnpfKeyConst.COLLAPSE:
                    {
                        // 先加为了防止 children下 还有折叠面板
                        List<FormControlDesignModel> childrenCollapseList = new List<FormControlDesignModel>();
                        foreach (var children in config.children)
                        {
                            var child = FormControlDesign(children.__config__.children, realisticControls, formDataModel, columnDesignModel, dataType, logic, 2);
                            childrenCollapseList.Add(new FormControlDesignModel()
                            {
                                Title = logic == 5 && children.titleI18nCode.IsNotEmptyOrNull() ? $":title=\"$t('{children.titleI18nCode}', '{children.title}')\"" : (children.title.IsNotEmptyOrNull() ? $"title='{children.title}' " : string.Empty),
                                Name = children.name,
                                Gutter = formDataModel.gutter,
                                Children = child,
                                LabelWidth = config?.labelWidth ?? formDataModel.labelWidth,
                                IsRelationForm = child.Any(x => x.IsRelationForm),
                                Open = item.accordion ? ((config.active?.Equals(children.name)).ParseToBool() ? "open" : string.Empty) : ((config.active?.ToObject<List<string>>().Contains(children.name)).ParseToBool() ? "open" : string.Empty)
                            });
                        }

                        var activeKey = string.Format("active{0}", active++);
                        list.Add(new FormControlDesignModel()
                        {
                            jnpfKey = config.jnpfKey,
                            Accordion = item.accordion ? "true" : "false",
                            Name = activeKey,
                            Active = childrenCollapseList.Select(it => it.Name).ToJsonString(),
                            Children = childrenCollapseList,
                            Span = config.span,
                            LabelWidth = config?.labelWidth ?? formDataModel.labelWidth,
                            IsRelationForm = childrenCollapseList.Any(x => x.IsRelationForm)
                        });
                    }

                    break;
                case JnpfKeyConst.TAB:
                case JnpfKeyConst.STEPS:
                    {
                        // 先加为了防止 children下 还有折叠面板
                        List<FormControlDesignModel> childrenCollapseList = new List<FormControlDesignModel>();
                        foreach (var children in config.children)
                        {
                            var child = FormControlDesign(children.__config__.children, realisticControls, formDataModel, columnDesignModel, dataType, logic, 2);
                            var title = logic == 5 && children.titleI18nCode.IsNotEmptyOrNull() ? $":title=\"$t('{children.titleI18nCode}', '{children.title}')\" " : (children.title.IsNotEmptyOrNull() ? $"title='{children.title}' " : string.Empty);
                            if (logic == 5 && config.jnpfKey.Equals(JnpfKeyConst.TAB)) title = children.titleI18nCode.IsNotEmptyOrNull() ? $":tab=\"$t('{children.titleI18nCode}', '{children.title}')\" " : (children.title.IsNotEmptyOrNull() ? $"tab='{children.title}' " : string.Empty);
                            childrenCollapseList.Add(new FormControlDesignModel()
                            {
                                Title = title,
                                Name = children.name,
                                Gutter = formDataModel.gutter,
                                Children = child,
                                LabelWidth = config?.labelWidth ?? formDataModel.labelWidth,
                                IsRelationForm = child.Any(x => x.IsRelationForm),
                                Icon = children.icon,
                            });
                        }

                        var activeKey = string.Format("active{0}", active++);
                        list.Add(new FormControlDesignModel()
                        {
                            jnpfKey = config.jnpfKey,
                            Type = item.type,
                            TabPosition = item.tabPosition,
                            Name = activeKey,
                            Active = config.active.ToString(),
                            Children = childrenCollapseList,
                            Span = config.span,
                            LabelWidth = config?.labelWidth ?? formDataModel.labelWidth,
                            IsRelationForm = childrenCollapseList.Any(x => x.IsRelationForm),
                            Simple = item.simple,
                            ProcessStatus = item.processStatus,
                        });
                    }

                    break;
                case JnpfKeyConst.GROUPTITLE:
                    {
                        list.Add(new FormControlDesignModel()
                        {
                            Tag = config.tag,
                            jnpfKey = config.jnpfKey,
                            Span = config.span,
                            Contentposition = item.contentPosition,
                            Content = logic == 5 && item.contentI18nCode.IsNotEmptyOrNull() ? $":content=\"$t('{item.contentI18nCode}', '{item.content}')\" " : (item.content.IsNotEmptyOrNull() ? $"content='{item.content}' " : string.Empty),
                            LabelWidth = config?.labelWidth ?? formDataModel.labelWidth,
                            TipLabel = logic == 5 && config.tipLabel.IsNotEmptyOrNull() && item.helpMessageI18nCode.IsNotEmptyOrNull() ? $":helpMessage=\"$t('{item.helpMessageI18nCode}', '{item.helpMessage}')\" " : (item.helpMessage.IsNotEmptyOrNull() ? $"helpMessage='{item.helpMessage}' " : null),
                        });
                    }

                    break;
                case JnpfKeyConst.JNPFTEXT:
                    {
                        list.Add(new FormControlDesignModel()
                        {
                            Tag = config.tag,
                            jnpfKey = config.jnpfKey,
                            Span = config.span,
                            DefaultValue = config.defaultValue,
                            Content = logic == 5 && item.contentI18nCode.IsNotEmptyOrNull() ? $":content=\"$t('{item.contentI18nCode}', '{item.content}')\" " : (item.content.IsNotEmptyOrNull() ? $"content='{item.content}' " : string.Empty),
                            TextStyle = item.textStyle != null ? item.textStyle.ToJsonString().Replace("\r\n", "") : string.Empty,
                            LabelWidth = config?.labelWidth ?? formDataModel.labelWidth,
                        });
                    }

                    break;
                case JnpfKeyConst.BUTTON:
                    {
                        list.Add(new FormControlDesignModel()
                        {
                            Tag = config.tag,
                            jnpfKey = config.jnpfKey,
                            Span = config.span,
                            Align = item.align,
                            ButtonText = logic == 5 && item.buttonTextI18nCode.IsNotEmptyOrNull() ? $":buttonText=\"$t('{item.buttonTextI18nCode}', '{item.buttonText}')\" " : (item.buttonText.IsNotEmptyOrNull() ? $"buttonText='{item.buttonText}' " : string.Empty),
                            Type = item.type,
                            Disabled = item.disabled ? "disabled " : string.Empty,
                            LabelWidth = config?.labelWidth ?? formDataModel.labelWidth,
                        });
                    }

                    break;
                case JnpfKeyConst.LINK:
                    {
                        list.Add(new FormControlDesignModel()
                        {
                            Label = configLabel,
                            Tag = config.tag,
                            jnpfKey = config.jnpfKey,
                            Span = config.span,
                            Content = logic == 5 && item.contentI18nCode.IsNotEmptyOrNull() ? $":content=\"$t('{item.contentI18nCode}', '{item.content}')\" " : (item.content.IsNotEmptyOrNull() ? $"content='{item.content}' " : string.Empty),
                            Href = item.href,
                            Target = item.target,
                            LabelWidth = config?.labelWidth ?? formDataModel.labelWidth,
                            TextStyle = item.textStyle != null ? item.textStyle.ToJsonString().Replace("\r\n", "") : string.Empty,
                            Height = item.height?.ToString(),
                        });
                    }

                    break;
                case JnpfKeyConst.IFRAME:
                    {
                        if (logic.Equals(4))
                        {
                            list.Add(new FormControlDesignModel()
                            {
                                TipLabel = logic == 5 && config.tipLabel.IsNotEmptyOrNull() && config.showLabel && config.tipLabelI18nCode.IsNotEmptyOrNull() ? $"$t('{config.tipLabelI18nCode}', '{config.tipLabel}')" : (config.tipLabel.IsNotEmptyOrNull() ? $" '{config.tipLabel}' " : null),
                                Label = configLabel,
                                Tag = config.tag,
                                jnpfKey = config.jnpfKey,
                                Span = config.span,
                                Content = logic == 5 && item.contentI18nCode.IsNotEmptyOrNull() ? $":content=\"$t('{item.contentI18nCode}', '{item.content}')\" " : (item.content.IsNotEmptyOrNull() ? $"content='{item.content}' " : string.Empty),
                                Href = item.href,
                                Target = item.target,
                                LabelWidth = config?.labelWidth ?? formDataModel.labelWidth,
                                TextStyle = item.textStyle != null ? item.textStyle.ToJsonString().Replace("\r\n", "") : string.Empty,
                                Height = item.height?.ToString(),
                                ShowLabel = config.showLabel,
                                BorderColor = string.Format(" borderColor='{0}' ", item.borderColor),
                                BorderType = string.Format(" borderType='{0}' ", item.borderType),
                                BorderWidth = string.Format(" :borderWidth={0} ", item.borderWidth),
                            });
                        }
                    }

                    break;
                case JnpfKeyConst.ALERT:
                    {
                        list.Add(new FormControlDesignModel()
                        {
                            Tag = config.tag,
                            jnpfKey = config.jnpfKey,
                            LabelWidth = config?.labelWidth ?? formDataModel.labelWidth,
                            Span = config.span,
                            Title = logic == 5 && item.titleI18nCode.IsNotEmptyOrNull() ? $":title=\"$t('{item.titleI18nCode}', '{item.title}')\" " : (item.title.IsNotEmptyOrNull() ? $"title='{item.title}' " : string.Empty),
                            Type = item.type,
                            ShowIcon = item.showIcon ? "true" : "false",
                            Description = logic == 5 && item.descriptionI18nCode.IsNotEmptyOrNull() ? $":description=\"$t('{item.descriptionI18nCode}', '{item.description}')\" " : (item.description.IsNotEmptyOrNull() ? $"description='{item.description}' " : string.Empty),
                            CloseText = logic == 5 && item.closeTextI18nCode.IsNotEmptyOrNull() ? $":closeText=\"$t('{item.closeTextI18nCode}', '{item.closeText}')\" " : (item.closeText.IsNotEmptyOrNull() ? $"closeText='{item.closeText}' " : string.Empty),
                            Closable = item.closable,
                        });
                    }

                    break;
                case JnpfKeyConst.QRCODE:
                case JnpfKeyConst.BARCODE:
                    {
                        var staticText = string.Empty;
                        if (item.dataType.Equals("static")) staticText = string.Format("staticText=\"{0}\"", item.staticText);
                        else if (item.relationField.IsNotEmptyOrNull()) staticText = string.Format(":staticText=\"{0}\"", "dataForm." + item.relationField);

                        list.Add(new FormControlDesignModel()
                        {
                            TipLabel = logic == 5 && config.showLabel && config.tipLabel.IsNotEmptyOrNull() && config.tipLabelI18nCode.IsNotEmptyOrNull() ? $"$t('{config.tipLabelI18nCode}', '{config.tipLabel}') " : (config.tipLabel.IsNotEmptyOrNull() ? $" '{config.tipLabel}' " : null),
                            Label = configLabel,
                            ShowLabel = item.__config__.showLabel,
                            Span = item.__config__.span,
                            Tag = config.tag,
                            jnpfKey = config.jnpfKey,
                            StaticText = staticText,
                            LabelWidth = config?.labelWidth ?? formDataModel.labelWidth,
                            DataType = string.Format("dataType=\"{0}\"", item.dataType),
                            RelationField = item.relationField.IsNullOrEmpty() ? string.Empty : string.Format("relationField=\"{0}\"", item.relationField),
                            RelationField_Id = item.relationField.IsNullOrEmpty() ? string.Empty : string.Format("relationField=\"{0}\"", item.relationField + "_id"),
                            ColorDark = string.Format("colorDark=\"{0}\"", item.colorDark),
                            ColorLight = string.Format("colorLight=\"{0}\"", item.colorLight),
                            Width = string.Format(":width='{0}'", item.width),
                            Height = string.Format(":height='{0}'", item.height),
                            Format = string.Format("format=\"{0}\"", item.format),
                            LineColor = string.Format("lineColor=\"{0}\"", item.lineColor),
                            Background = string.Format("background=\"{0}\"", item.background),
                        });
                    }

                    break;
                case JnpfKeyConst.RELATIONFORMATTR:
                case JnpfKeyConst.POPUPATTR:
                    {
                        var relationField = Regex.Match(item.relationField, @"^(.+)_jnpfTable_").Groups[1].Value;
                        var relationControl = realisticControls.Find(it => it.__vModel__ == relationField);
                        var columnDesign = columnDesignModel?.Find(it => it.__vModel__ == item.__vModel__);
                        list.Add(new FormControlDesignModel()
                        {
                            vModel = item.__vModel__.IsNotEmptyOrNull() ? string.Format("v-model=\"dataForm.{0}\"", item.__vModel__) : string.Empty,
                            IsInlineEditor = columnDesignModel != null ? columnDesignModel.Any(it => it.__vModel__ == item.__vModel__) : false,
                            Style = item.style != null && !item.style.ToString().Equals("{}") ? $":style='{item.style.ToJsonString().Replace("\r\n", "")}' " : string.Empty,
                            jnpfKey = config.jnpfKey,
                            OriginalName = item.isStorage == 1 ? item.__vModel__ : relationField,
                            Name = item.isStorage == 1 ? item.__vModel__ : relationField,
                            RelationField = relationField,
                            ShowField = item.showField,
                            NoShow = item.isStorage == 1 ? config.noShow : relationControl.__config__.noShow,
                            Tag = config.tag,
                            Label = configLabel,
                            Span = config.span,
                            IsStorage = item.isStorage,
                            IndexWidth = columnDesign?.width,
                            LabelWidth = config?.labelWidth ?? formDataModel.labelWidth,
                            IndexAlign = columnDesign?.align,
                            TipLabel = logic == 5 && config.showLabel && config.tipLabel.IsNotEmptyOrNull() && config.tipLabelI18nCode.IsNotEmptyOrNull() ? $"$t('{config.tipLabelI18nCode}', '{config.tipLabel}') " : (config.tipLabel.IsNotEmptyOrNull() ? $" '{config.tipLabel}' " : null),
                            ShowLabel = config.showLabel,
                            Align = config.tableAlgin,
                            TableFixed = config.tableFixed,
                        });
                    }

                    break;
                default:
                    {
                        if (config.jnpfKey.Equals(JnpfKeyConst.CALCULATE)) break;
                        var realisticControl = realisticControls.Find(it => it.__vModel__.Equals(item.__vModel__) && it.__config__.jnpfKey.Equals(config.jnpfKey));
                        realisticControl ??= new FieldsModel();
                        var columnDesign = columnDesignModel?.Find(it => it.__vModel__ == item.__vModel__);
                        string vModel = string.Empty;
                        var Model = item.__vModel__;
                        vModel = dataType != 4 ? $"v-model='dataForm.{Model}' " : $"v-model='scope.row.{Model}' ";
                        var ableIds = string.Empty;
                        if (config.jnpfKey.Equals(JnpfKeyConst.SIGNATURE)) ableIds = item.ableIds.IsNotEmptyOrNull() ? string.Format(":ableIds='{0}' ", item.ableIds.ToJsonString().Replace("\r\n", "")) : string.Empty;
                        else ableIds = item.selectType != null && item.selectType == "custom" ? string.Format(":ableIds='{0}_AbleIds' ", item.__vModel__) : string.Empty;
                        if (config.jnpfKey.Equals(JnpfKeyConst.POPUPSELECT)) item.modelId = item.interfaceId;
                        list.Add(new FormControlDesignModel()
                        {
                            IsSort = columnDesign != null ? columnDesign.sortable : false,
                            IsInlineEditor = columnDesignModel != null ? columnDesignModel.Any(it => it.__vModel__ == item.__vModel__) : false,
                            IndexAlign = columnDesign?.align,
                            IndexWidth = columnDesign?.width,
                            Name = item.__vModel__,
                            OriginalName = item.__vModel__,
                            jnpfKey = config.jnpfKey,
                            Border = item.border ? "border " : string.Empty,
                            Style = item.style != null && !item.style.ToString().Equals("{}") ? $":style='{item.style.ToJsonString().Replace("\r\n", "")}' " : string.Empty,
                            Type = !string.IsNullOrEmpty(item.type) ? $"type='{item.type}' " : string.Empty,
                            Span = config.span,
                            Clearable = item.clearable ? "clearable " : string.Empty,
                            Readonly = item.@readonly ? "readonly " : string.Empty,
                            Required = config.required ? "required " : string.Empty,
                            Placeholder = logic == 5 && item.placeholderI18nCode.IsNotEmptyOrNull() ? $":placeholder=\"$t('{item.placeholderI18nCode}', '{item.placeholder}')\" " : (item.placeholder.IsNotEmptyOrNull() ? $"placeholder='{item.placeholder}' " : string.Empty),
                            Disabled = item.disabled ? "disabled " : string.Empty,
                            IsDisabled = $":disabled='judgeWrite(\"{item.__vModel__}\")' ",
                            ShowWordLimit = item.showWordlimit ? "show-word-limit " : string.Empty,
                            Format = !string.IsNullOrEmpty(item.format) ? $"format='{item.format}' " : string.Empty,
                            ValueFormat = !string.IsNullOrEmpty(item.valueformat) ? $"value-format='{item.valueformat}' " : string.Empty,
                            AutoSize = item.autoSize != null && item.autoSize.ToJsonString() != "null" ? $":autosize='{item.autoSize.ToJsonString().Replace("\r\n", "")}' " : string.Empty,
                            Multiple = (config.jnpfKey.Equals(JnpfKeyConst.CASCADER) ? item.multiple : item.multiple) ? $"multiple " : string.Empty,
                            IsRange = item.isrange ? "is-range " : string.Empty,
                            Props = item.props,
                            MainProps = item.props != null ? $":props='{Model}Props' " : string.Empty,
                            OptionType = item.optionType != null ? string.Format("optionType=\"{0}\" ", item.optionType) : string.Empty,
                            Size = !string.IsNullOrEmpty(item.optionType) ? (item.optionType == "default" ? string.Empty : $"size='{item.size}' ") : string.Empty,
                            PrefixIcon = !string.IsNullOrEmpty(item.prefixIcon) ? $"prefix-icon='{item.prefixIcon}' " : string.Empty,
                            SuffixIcon = !string.IsNullOrEmpty(item.suffixIcon) ? $"suffix-icon='{item.suffixIcon}' " : string.Empty,
                            MaxLength = item.maxlength != null ? $":maxlength='{item.maxlength}' " : string.Empty,
                            Step = item.step != null ? $":step='{item.step}' " : string.Empty,
                            StepStrictly = item.stepstrictly ? "step-strictly " : string.Empty,
                            ControlsPosition = !string.IsNullOrEmpty(item.controlsPosition) ? $"controlsPosition='{item.controlsPosition}' " : string.Empty,
                            Controls = item.controls ? "controls" : string.Empty,
                            ShowChinese = item.showChinese ? "showChinese " : string.Empty,
                            ShowPassword = item.showPassword ? "show-password " : string.Empty,
                            UseScan = item.useScan ? "useScan " : string.Empty,
                            UseMask = item.useMask ? "useMask " : string.Empty,
                            MaskConfig = item.maskConfig != null ? $":maskConfig='{item.maskConfig.ToJsonString().Replace("\r\n", "")}' " : string.Empty,
                            Filterable = item.filterable ? "filterable " : string.Empty,
                            ShowAllLevels = item.showAllLevels ? "show-all-levels " : string.Empty,
                            Separator = !string.IsNullOrEmpty(item.separator) ? $"separator='{item.separator}' " : string.Empty,
                            RangeSeparator = !string.IsNullOrEmpty(item.rangeseparator) ? $"range-separator='{item.rangeseparator}' " : string.Empty,
                            StartPlaceholder = !string.IsNullOrEmpty(item.startplaceholder) ? $"start-placeholder='{item.startplaceholder}' " : string.Empty,
                            EndPlaceholder = !string.IsNullOrEmpty(item.endplaceholder) ? $"end-placeholder='{item.endplaceholder}' " : string.Empty,
                            PickerOptions = item.pickeroptions != null && item.pickeroptions.ToJsonString() != "null" ? $":picker-options='{item.pickeroptions.ToJsonString()}' " : string.Empty,
                            Options = item.options != null ? $":options='{item.__vModel__}Options' " : string.Empty,
                            Max = item.max != null && item.max != 0 ? $":max='{item.max}' " : string.Empty,
                            AllowHalf = item.allowHalf ? "allow-half " : string.Empty,
                            ShowTexts = item.showtext ? $"show-text " : string.Empty,
                            ShowScore = item.showScore ? $"show-score " : string.Empty,
                            ShowAlpha = item.showAlpha ? $"show-alpha " : string.Empty,
                            ColorFormat = !string.IsNullOrEmpty(item.colorFormat) ? $"color-format='{item.colorFormat}' " : string.Empty,
                            ActiveText = !string.IsNullOrEmpty(item.activeTxt) ? $"active-text='{item.activeTxt}' " : string.Empty,
                            InactiveText = !string.IsNullOrEmpty(item.inactiveTxt) ? $"inactive-text='{item.inactiveTxt}' " : string.Empty,
                            ActiveColor = !string.IsNullOrEmpty(item.activecolor) ? $"active-color='{item.activecolor}' " : string.Empty,
                            InactiveColor = !string.IsNullOrEmpty(item.inactivecolor) ? $"inactive-color='{item.inactivecolor}' " : string.Empty,
                            IsSwitch = config.jnpfKey == JnpfKeyConst.SWITCH ? $":active-value='{item.activeValue}' :inactive-value='{item.inactiveValue}' " : string.Empty,
                            Min = item.min != null ? $":min='{item.min}' " : string.Empty,
                            ShowStops = item.showstops ? $"show-stops " : string.Empty,
                            Range = item.range ? $"range " : string.Empty,
                            Accept = !string.IsNullOrEmpty(item.accept) ? $"accept='{item.accept}' " : string.Empty,
                            ShowTip = item.showTip ? $"showTip " : string.Empty,
                            FileSize = item.fileSize != null && !string.IsNullOrEmpty(item.fileSize.ToString()) ? $":fileSize='{item.fileSize}' " : string.Empty,
                            SizeUnit = !string.IsNullOrEmpty(item.sizeUnit) ? $"sizeUnit='{item.sizeUnit}' " : string.Empty,
                            Limit = item.limit != null ? $":limit='{item.limit}' " : string.Empty,
                            Contentposition = !string.IsNullOrEmpty(item.contentPosition) ? $"content-position='{item.contentPosition}' " : string.Empty,
                            ButtonText = logic == 5 && item.buttonTextI18nCode.IsNotEmptyOrNull() ? $":buttonText=\"$t('{item.buttonTextI18nCode}', '{item.buttonText}')\" " : (item.buttonText.IsNotEmptyOrNull() ? $"buttonText='{item.buttonText}' " : string.Empty),
                            ShowType = logic == 5 && item.buttonText.IsNotEmptyOrNull() ? $" buttonText='{item.buttonText}' " : string.Empty,
                            Level = config.jnpfKey == JnpfKeyConst.ADDRESS ? $":level='{item.level}' " : string.Empty,
                            Shadow = !string.IsNullOrEmpty(item.shadow) ? $"shadow='{item.shadow}' " : string.Empty,
                            Content = logic == 5 && item.contentI18nCode.IsNotEmptyOrNull() ? $":content=\"$t('{item.contentI18nCode}', '{item.content}')\" " : (item.content.IsNotEmptyOrNull() ? $"content='{item.content}' " : string.Empty),
                            NoShow = config.noShow,
                            Label = configLabel,
                            TipLabel = logic == 5 && config.showLabel && config.tipLabel.IsNotEmptyOrNull() && config.tipLabelI18nCode.IsNotEmptyOrNull() ? $"$t('{config.tipLabelI18nCode}', '{config.tipLabel}') " : (config.tipLabel.IsNotEmptyOrNull() ? $" '{config.tipLabel}' " : null),
                            vModel = vModel,
                            Prepend = item.__slot__ != null && !string.IsNullOrEmpty(item.__slot__.prepend) ? item.__slot__.prepend : null,
                            Append = item.__slot__ != null && !string.IsNullOrEmpty(item.__slot__.append) ? item.__slot__.append : null,
                            Tag = config.tag,
                            Count = item.count.ToString(),
                            ModelId = item.modelId != null ? item.modelId : string.Empty,
                            RelationField = item.relationField != null ? $"relationField='{item.relationField}' " : string.Empty,
                            ColumnOptions = item.columnOptions != null ? $":columnOptions='{item.__vModel__}Options' " : string.Empty,
                            TemplateJson = needTemplateJson.Contains(config.jnpfKey) ? string.Format(":templateJson='{0}TemplateJson' ", item.__vModel__) : string.Empty,
                            HasPage = item.hasPage ? "hasPage " : string.Empty,
                            PageSize = item.pageSize != null ? $":pageSize='{item.pageSize}' " : string.Empty,
                            PropsValue = item.propsValue != null ? $"propsValue='{item.propsValue}' " : string.Empty,
                            InterfaceId = item.interfaceId != null ? $"interfaceId='{item.interfaceId}' " : string.Empty,
                            Precision = item.precision != null ? $":precision='{item.precision}' " : string.Empty,
                            ShowLevel = !string.IsNullOrEmpty(item.showLevel) ? string.Empty : string.Empty,
                            LabelWidth = config?.labelWidth ?? formDataModel.labelWidth,
                            IsStorage = item.isStorage,
                            PopupType = !string.IsNullOrEmpty(item.popupType) ? $"popupType='{item.popupType}' " : string.Empty,
                            PopupTitle = !string.IsNullOrEmpty(item.popupTitle) ? $"popupTitle='{item.popupTitle}' " : string.Empty,
                            PopupWidth = !string.IsNullOrEmpty(item.popupWidth) ? $"popupWidth='{item.popupWidth}' " : string.Empty,
                            Field = config.jnpfKey.Equals(JnpfKeyConst.RELATIONFORM) || config.jnpfKey.Equals(JnpfKeyConst.POPUPSELECT) ? $"field='{item.__vModel__}' " : string.Empty,
                            SelectType = item.selectType != null ? item.selectType : string.Empty,
                            AbleIds = ableIds,
                            UserRelationAttr = GetUserRelationAttr(item, realisticControls, logic),
                            IsLinked = realisticControl.IsLinked,
                            IsLinkage = realisticControl.IsLinkage,
                            IsRelationForm = config.jnpfKey.Equals(JnpfKeyConst.RELATIONFORM),
                            PathType = !string.IsNullOrEmpty(item.pathType) ? string.Format("pathType=\"{0}\" ", item.pathType) : string.Empty,
                            IsAccount = item.isAccount != -1 ? string.Format(":isAccount=\"{0}\" ", item.isAccount) : string.Empty,
                            Folder = !string.IsNullOrEmpty(item.folder) ? string.Format("folder=\"{0}\" ", item.folder) : string.Empty,
                            DefaultCurrent = config.defaultCurrent,
                            Direction = !string.IsNullOrEmpty(item.direction) ? string.Format("direction=\"{0}\" ", item.direction) : string.Empty,
                            Total = item.total > 0 ? string.Format(":total=\"{0}\" ", item.total) : string.Empty,
                            AddonBefore = !string.IsNullOrEmpty(item.addonBefore) ? string.Format("addonBefore=\"{0}\" ", item.addonBefore) : string.Empty,
                            AddonAfter = !string.IsNullOrEmpty(item.addonAfter) ? string.Format("addonAfter=\"{0}\" ", item.addonAfter) : string.Empty,
                            Thousands = item.thousands ? string.Format("thousands :controls='false' ", item.addonBefore) : string.Empty,
                            AmountChinese = item.isAmountChinese ? string.Format("isAmountChinese ", item.addonBefore) : string.Empty,
                            TipText = !string.IsNullOrEmpty(item.tipText) ? string.Format("tipText=\"{0}\" ", item.tipText) : string.Empty,
                            StartTime = specialDateAttribute.Contains(config.jnpfKey) ? SpecialTimeAttributeAssembly(item, fieldList, 1, true) : string.Empty,
                            EndTime = specialDateAttribute.Contains(config.jnpfKey) ? SpecialTimeAttributeAssembly(item, fieldList, 2, true) : string.Empty,
                            ShowLabel = config.showLabel,
                            Align = config.tableAlgin,
                            TableFixed = config.tableFixed,
                            Fixed = columnDesign != null && columnDesign.@fixed.IsNotEmptyOrNull() && columnDesign.@fixed != "none" ? string.Format("fixed='{0}' ", columnDesign.@fixed) : string.Empty,
                            EnableLocationScope = string.Format(":enableLocationScope='{0}' ", item.enableLocationScope.ToString().ToLower()),
                            AutoLocation = string.Format(":autoLocation='{0}' ", item.autoLocation.ToString().ToLower()),
                            AdjustmentScope = string.Format(":adjustmentScope='{0}' ", item.adjustmentScope),
                            EnableDesktopLocation = string.Format(":enableDesktopLocation='{0}' ", item.enableDesktopLocation.ToString().ToLower()),
                            LocationScope = string.Format(":locationScope='{0}' ", item.locationScope.IsNotEmptyOrNull() ? item.locationScope.ToJsonString().Replace("\r\n", "") : "[]"),
                            ShowCount = config.jnpfKey.Equals(JnpfKeyConst.COMINPUT) || config.jnpfKey.Equals(JnpfKeyConst.TEXTAREA) ? string.Format(" :showCount='{0}' ", item.showCount.ToString().ToLower()) : string.Empty,
                            Disaabled = config.jnpfKey.Equals(JnpfKeyConst.SIGNATURE) ? string.Format(" :disaabled=\"{0}\" ", item.disaabled.ToString().ToLower()) : string.Empty,
                            IsInvoke = config.jnpfKey.Equals(JnpfKeyConst.SIGN) ? string.Format(" :isInvoke='{0}' ", item.isInvoke.ToString().ToLower()) : string.Empty,
                            SortRule = item.sortRule.IsNotEmptyOrNull() ? string.Format(" :sortRule='{0}'", item.sortRule.ToJsonString().Replace("\r\n", "")) : string.Empty,
                            TimeFormat = item.timeFormat.IsNotEmptyOrNull() ? string.Format(" timeFormat='{0}'", item.timeFormat) : string.Empty,
                            required = item.__config__.required,
                            ExtraOptions = item.extraOptions != null ? string.Format(":extraOptions='{0}'", item.extraOptions.ToJsonString()) : string.Empty,
                            ExtraOption = new CodeGenFrontEndFormState() { Name = item.__vModel__, Value = item.extraOptions.ToJsonString(CommonConst.options) },
                            InterfaceRes = new CodeGenFrontEndFormState() { Name = item.__vModel__, Value = item.templateJson.ToJsonString() },
                            QueryType = item.queryType != null ? string.Format(" :queryType={0} ", item.queryType) : string.Empty,
                        });
                    }

                    break;
            }
        }
        return list;
    }

    public static List<FormControlDesignModel> GetFormControlDesignByTree(List<FormControlDesignModel> list)
    {
        var res = new List<FormControlDesignModel>();

        list.ForEach(item =>
        {
            if (item.Name.IsNotEmptyOrNull() && item.LowerName.IsNotEmptyOrNull() && item.OriginalName.IsNotEmptyOrNull())
            {
                res.Add(item);
            }
            else if (item.Children != null && item.Children.Any())
            {
                var cList = GetFormControlDesignByTree(item.Children.ToList());
                if (cList != null && cList.Any()) res.AddRange(cList);
            }
        });

        return res;
    }

    /// <summary>
    /// 表单控件设计.
    /// </summary>
    /// <param name="fieldList">组件列表.</param>
    /// <param name="realisticControls">真实控件.</param>
    /// <param name="subTableName">子表名称.</param>
    /// <param name="formDataModel">表单模型.</param>
    /// <param name="columnDesignModel">列表显示列列表.</param>
    /// <param name="dataType">数据类型
    /// 1-普通列表,2-左侧树形+普通表格,3-分组表格,4-编辑表格.</param>
    /// <param name="logic">4-PC,5-App.</param>
    /// <param name="hasFlow">是否开启流程.</param>
    /// <param name="isMain">是否主循环.</param>
    /// <returns></returns>
    public static CodeGenFrontEndFormScriptModel FormScriptDesign(List<FieldsModel> fieldList, List<FieldsModel> realisticControls, string subTableName, FormDataModel formDataModel, List<IndexGridFieldModel> columnDesignModel, int dataType, int logic, bool hasFlow, bool isMain = false)
    {
        var isSubTable = !string.IsNullOrEmpty(subTableName) ? true : false;
        if (isMain)
        {
            active = 1;
            hasSpecialTime = false;
            hasSpecialDate = false;
            hasSubTableDataTransfer = false;
            mainLinkageSubTable = new List<CodeGenFrontEndFormState>();
        }
        CodeGenFrontEndFormScriptModel model = new CodeGenFrontEndFormScriptModel();
        List<FormControlDesignModel> list = new List<FormControlDesignModel>();
        List<CodeGenFrontEndDataRule> dataRules = new List<CodeGenFrontEndDataRule>();
        List<CodeGenFrontEndFormState> options = new List<CodeGenFrontEndFormState>();
        List<CodeGenFrontEndFormState> extraOptions = new List<CodeGenFrontEndFormState>();
        List<CodeGenFrontEndFormState> interfaceRes = new List<CodeGenFrontEndFormState>();
        List<CodeGenFrontEndFormState> collapses = new List<CodeGenFrontEndFormState>();
        List<CodeGenFrontEndFormState> dataForm = new List<CodeGenFrontEndFormState>();
        List<CodeGenFrontEndFormState> inlineEditorDataForm = new List<CodeGenFrontEndFormState>();
        List<CodeGenFrontEndDataOption> dataOptions = new List<CodeGenFrontEndDataOption>();
        List<CodeGenFrontEndSubTableDesignModel> subTableDesign = new List<CodeGenFrontEndSubTableDesignModel>();
        List<CodeGenFrontEndSubTableHeaderModel> subTableHeader = new List<CodeGenFrontEndSubTableHeaderModel>();
        List<CodeGenFrontEndLinkageConfig> linkage = new List<CodeGenFrontEndLinkageConfig>();
        List<CodeGenFrontEndSubTableControlModel> subTableControls = new List<CodeGenFrontEndSubTableControlModel>();
        List<string> subTableLinkageOptions = new List<string>();

        foreach (var item in fieldList.ToJsonString().ToObject<List<FieldsModel>>())
        {
            var config = item.__config__;
            if (config.tipLabel.IsNotEmptyOrNull()) config.tipLabel = config.tipLabel.Replace("\n", string.Empty);

            // 控件标题 + 后缀
            if (formDataModel.labelSuffix.IsNotEmptyOrNull() && config != null && config.label != null && !config.jnpfKey.Equals(JnpfKeyConst.TABLE) && (config.parentVModel == null || !config.parentVModel.Contains("tableField")))
            {
                if (config.label.IsNullOrEmpty()) config.showLabel = false;
                config.label += formDataModel.labelSuffix;
            }

            var configLabel = logic.Equals(4) && config.labelI18nCode.IsNotEmptyOrNull() ? "{{$t('" + config.labelI18nCode + "', '" + config.label + "')}}" : (config.label.IsNullOrEmpty() ? string.Empty : config.label);

            var needTemplateJson = new List<string>() { JnpfKeyConst.POPUPTABLESELECT, JnpfKeyConst.POPUPSELECT, JnpfKeyConst.AUTOCOMPLETE };
            var specialDateAttribute = new List<string>() { JnpfKeyConst.DATE, JnpfKeyConst.TIME };

            // 布局控件
            switch (config.jnpfKey)
            {
                case JnpfKeyConst.GROUPTITLE:
                    {
                        list.Add(new FormControlDesignModel()
                        {
                            jnpfKey = config.jnpfKey,
                            Span = config.span,
                            Contentposition = item.contentPosition,
                            Content = logic.Equals(4) && item.contentI18nCode.IsNotEmptyOrNull() ? $":content=\"$t('{item.contentI18nCode}', '{item.content}')\" " : (item.content.IsNotEmptyOrNull() ? $"content='{item.content}' " : string.Empty),
                            LabelWidth = config?.labelWidth ?? formDataModel.labelWidth,
                            TipLabel = logic.Equals(4) && item.helpMessageI18nCode.IsNotEmptyOrNull() ? $":helpMessage=\"$t('{item.helpMessageI18nCode}', '{item.helpMessage}')\" " : (item.helpMessage.IsNotEmptyOrNull() ? $"helpMessage='{item.helpMessage}' " : string.Empty),
                        });
                    }

                    break;
                case JnpfKeyConst.COLLAPSE:
                    {
                        // 先加为了防止 children下 还有折叠面板
                        List<FormControlDesignModel> childrenCollapseList = new List<FormControlDesignModel>();
                        foreach (var children in config.children)
                        {
                            var formScriptDesign = FormScriptDesign(children.__config__.children, realisticControls, subTableName, formDataModel, columnDesignModel, dataType, logic, hasFlow);
                            childrenCollapseList.Add(new FormControlDesignModel()
                            {
                                Title = logic.Equals(4) && children.titleI18nCode.IsNotEmptyOrNull() ? $":header=\"$t('{children.titleI18nCode}', '{children.title}')\" " : (children.title.IsNotEmptyOrNull() ? $"header='{children.title}' " : string.Empty),
                                Name = children.name,
                                Gutter = formDataModel.gutter,
                                Children = formScriptDesign.FormControlDesign,
                                IsRelationForm = formScriptDesign.FormControlDesign.Any(x => x.IsRelationForm),
                            });
                            collapses.AddRange(formScriptDesign.Collapses);
                            dataRules.AddRange(formScriptDesign.DataRules);
                            options.AddRange(formScriptDesign.Options);
                            extraOptions.AddRange(formScriptDesign.ExtraOptions);
                            dataForm.AddRange(formScriptDesign.DataForm);
                            inlineEditorDataForm.AddRange(formScriptDesign.InlineEditorDataForm);
                            dataOptions.AddRange(formScriptDesign.DataOptions);
                            subTableDesign.AddRange(formScriptDesign.SubTableDesign);
                            linkage.AddRange(formScriptDesign.Linkage);
                            hasSpecialDate = formScriptDesign.HasSpecialDate;
                            hasSpecialTime = formScriptDesign.HasSpecialTime;
                            hasSubTableDataTransfer = formScriptDesign.HasSubTableDataTransfer;
                        }

                        var activeKey = string.Format("active{0}", active++);
                        list.Add(new FormControlDesignModel()
                        {
                            jnpfKey = config.jnpfKey,
                            Accordion = item.accordion ? "true" : "false",
                            Name = activeKey,
                            Active = childrenCollapseList.Select(it => it.Name).ToJsonString(),
                            Children = childrenCollapseList,
                            Span = config.span,
                            LabelWidth = config?.labelWidth ?? formDataModel.labelWidth,
                            IsRelationForm = childrenCollapseList.Any(x => x.IsRelationForm),
                        });
                        collapses.Add(new CodeGenFrontEndFormState
                        {
                            Name = activeKey,
                            Value = childrenCollapseList.Select(it => it.Name).ToJsonString(),
                        });
                    }

                    break;
                case JnpfKeyConst.TAB:
                case JnpfKeyConst.STEPS:
                    {
                        // 先加为了防止 children下 还有折叠面板
                        List<FormControlDesignModel> childrenCollapseList = new List<FormControlDesignModel>();
                        foreach (var children in config.children)
                        {
                            var formScriptDesign = FormScriptDesign(children.__config__.children, realisticControls, subTableName, formDataModel, columnDesignModel, dataType, logic, hasFlow);
                            var title = logic.Equals(4) && children.titleI18nCode.IsNotEmptyOrNull() ? $":title=\"$t('{children.titleI18nCode}', '{children.title}')\" " : (children.title.IsNotEmptyOrNull() ? $"title='{children.title}' " : string.Empty);
                            if (config.jnpfKey.Equals(JnpfKeyConst.TAB)) title = logic.Equals(4) && children.titleI18nCode.IsNotEmptyOrNull() ? $":tab=\"$t('{children.titleI18nCode}', '{children.title}')\" " : (children.title.IsNotEmptyOrNull() ? $"tab='{children.title}' " : string.Empty);
                            childrenCollapseList.Add(new FormControlDesignModel()
                            {
                                Title = title,
                                Name = children.name,
                                Gutter = formDataModel.gutter,
                                Children = formScriptDesign.FormControlDesign,
                                IsRelationForm = formScriptDesign.FormControlDesign.Any(x => x.IsRelationForm),
                                Icon = children.icon,
                            });
                            collapses.AddRange(formScriptDesign.Collapses);
                            dataRules.AddRange(formScriptDesign.DataRules);
                            options.AddRange(formScriptDesign.Options);
                            extraOptions.AddRange(formScriptDesign.ExtraOptions);
                            dataForm.AddRange(formScriptDesign.DataForm);
                            inlineEditorDataForm.AddRange(formScriptDesign.InlineEditorDataForm);
                            dataOptions.AddRange(formScriptDesign.DataOptions);
                            subTableDesign.AddRange(formScriptDesign.SubTableDesign);
                            linkage.AddRange(formScriptDesign.Linkage);
                            hasSpecialDate = formScriptDesign.HasSpecialDate;
                            hasSpecialTime = formScriptDesign.HasSpecialTime;
                            hasSubTableDataTransfer = formScriptDesign.HasSubTableDataTransfer;
                        }

                        var activeKey = string.Format("active{0}", active++);
                        list.Add(new FormControlDesignModel()
                        {
                            jnpfKey = config.jnpfKey,
                            Type = item.type,
                            TabPosition = item.tabPosition,
                            Name = activeKey,
                            Active = config.active.ToString(),
                            Children = childrenCollapseList,
                            Span = config.span,
                            LabelWidth = config?.labelWidth ?? formDataModel.labelWidth,
                            IsRelationForm = childrenCollapseList.Any(x => x.IsRelationForm),
                            Simple = item.simple,
                            ProcessStatus = item.processStatus,
                        });
                        collapses.Add(new CodeGenFrontEndFormState
                        {
                            Name = activeKey,
                            Value = config.active.ToJsonString(),
                        });
                    }

                    break;
                case JnpfKeyConst.DIVIDER:
                    {
                        list.Add(new FormControlDesignModel()
                        {
                            jnpfKey = config.jnpfKey,
                            OriginalName = item.__vModel__,
                            Span = config.span,
                            Contentposition = item.contentPosition,
                            Content = logic.Equals(4) && item.contentI18nCode.IsNotEmptyOrNull() ? $":content=\"$t('{item.contentI18nCode}', '{item.content}')\" " : (item.content.IsNotEmptyOrNull() ? $"content='{item.content}' " : string.Empty),
                            LabelWidth = config?.labelWidth ?? formDataModel.labelWidth,
                        });
                    }

                    break;
                case JnpfKeyConst.ROW:
                    {
                        var formScriptDesign = FormScriptDesign(config.children, realisticControls, subTableName, formDataModel, columnDesignModel, dataType, logic, hasFlow);
                        list.Add(new FormControlDesignModel()
                        {
                            jnpfKey = config.jnpfKey,
                            Span = config.span,
                            Gutter = formDataModel.gutter,
                            Children = formScriptDesign.FormControlDesign,
                        });
                        collapses.AddRange(formScriptDesign.Collapses);
                        dataRules.AddRange(formScriptDesign.DataRules);
                        options.AddRange(formScriptDesign.Options);
                        extraOptions.AddRange(formScriptDesign.ExtraOptions);
                        dataForm.AddRange(formScriptDesign.DataForm);
                        inlineEditorDataForm.AddRange(formScriptDesign.InlineEditorDataForm);
                        dataOptions.AddRange(formScriptDesign.DataOptions);
                        subTableDesign.AddRange(formScriptDesign.SubTableDesign);
                        linkage.AddRange(formScriptDesign.Linkage);
                        hasSpecialDate = formScriptDesign.HasSpecialDate;
                        hasSpecialTime = formScriptDesign.HasSpecialTime;
                        hasSubTableDataTransfer = formScriptDesign.HasSubTableDataTransfer;
                    }

                    break;
                case JnpfKeyConst.CARD:
                    {
                        config.tipLabel = string.IsNullOrEmpty(config.tipLabel) ? null : config.tipLabel.Replace("\n", string.Empty);
                        var formScriptDesign = FormScriptDesign(config.children, realisticControls, subTableName, formDataModel, columnDesignModel, dataType, logic, hasFlow);
                        list.Add(new FormControlDesignModel()
                        {
                            jnpfKey = config.jnpfKey,
                            OriginalName = item.__vModel__,
                            Shadow = item.shadow.Equals("hover") ? "hoverable" : string.Empty,
                            Children = formScriptDesign.FormControlDesign,
                            Span = config.span,
                            Content = logic.Equals(4) && item.headerI18nCode.IsNotEmptyOrNull() ? "{{$t('" + item.headerI18nCode + "', '" + item.header + "')}}" : item.header,
                            LabelWidth = config?.labelWidth ?? formDataModel.labelWidth,
                            TipLabel = logic.Equals(4) && config.tipLabelI18nCode.IsNotEmptyOrNull() ? $"<BasicHelp :text=\"$t('{config.tipLabelI18nCode}', '{config.tipLabel}')\"/> " : (config.tipLabel.IsNotEmptyOrNull() ? $"<BasicHelp text='{config.tipLabel}'/> " : string.Empty),
                        });
                        collapses.AddRange(formScriptDesign.Collapses);
                        dataRules.AddRange(formScriptDesign.DataRules);
                        options.AddRange(formScriptDesign.Options);
                        extraOptions.AddRange(formScriptDesign.ExtraOptions);
                        dataForm.AddRange(formScriptDesign.DataForm);
                        inlineEditorDataForm.AddRange(formScriptDesign.InlineEditorDataForm);
                        dataOptions.AddRange(formScriptDesign.DataOptions);
                        subTableDesign.AddRange(formScriptDesign.SubTableDesign);
                        linkage.AddRange(formScriptDesign.Linkage);
                        hasSpecialDate = formScriptDesign.HasSpecialDate;
                        hasSpecialTime = formScriptDesign.HasSpecialTime;
                        hasSubTableDataTransfer = formScriptDesign.HasSubTableDataTransfer;
                    }

                    break;

                case JnpfKeyConst.TABLEGRID:
                    {
                        var formScriptDesign = FormScriptDesign(config.children, realisticControls, subTableName, formDataModel, columnDesignModel, dataType, logic, hasFlow);
                        list.Add(new FormControlDesignModel()
                        {
                            jnpfKey = config.jnpfKey,
                            Span = config.span,
                            Style = string.Format("{{'--borderType':'{0}','--borderColor':'{1}','--borderWidth':'{2}px'}}", config.borderType, config.borderColor, config.borderWidth),
                            Children = formScriptDesign.FormControlDesign,
                            IsRelationForm = formScriptDesign.FormControlDesign.Any(x => x.IsRelationForm),
                        });
                        collapses.AddRange(formScriptDesign.Collapses);
                        dataRules.AddRange(formScriptDesign.DataRules);
                        options.AddRange(formScriptDesign.Options);
                        extraOptions.AddRange(formScriptDesign.ExtraOptions);
                        dataForm.AddRange(formScriptDesign.DataForm);
                        inlineEditorDataForm.AddRange(formScriptDesign.InlineEditorDataForm);
                        dataOptions.AddRange(formScriptDesign.DataOptions);
                        subTableDesign.AddRange(formScriptDesign.SubTableDesign);
                        linkage.AddRange(formScriptDesign.Linkage);
                        hasSpecialDate = formScriptDesign.HasSpecialDate;
                        hasSpecialTime = formScriptDesign.HasSpecialTime;
                        hasSubTableDataTransfer = formScriptDesign.HasSubTableDataTransfer;
                    }

                    break;
                case JnpfKeyConst.TABLEGRIDTR:
                    {
                        var formScriptDesign = FormScriptDesign(config.children, realisticControls, subTableName, formDataModel, columnDesignModel, dataType, logic, hasFlow);
                        list.Add(new FormControlDesignModel()
                        {
                            jnpfKey = config.jnpfKey,
                            Children = formScriptDesign.FormControlDesign
                        });
                        collapses.AddRange(formScriptDesign.Collapses);
                        dataRules.AddRange(formScriptDesign.DataRules);
                        options.AddRange(formScriptDesign.Options);
                        extraOptions.AddRange(formScriptDesign.ExtraOptions);
                        dataForm.AddRange(formScriptDesign.DataForm);
                        inlineEditorDataForm.AddRange(formScriptDesign.InlineEditorDataForm);
                        dataOptions.AddRange(formScriptDesign.DataOptions);
                        subTableDesign.AddRange(formScriptDesign.SubTableDesign);
                        linkage.AddRange(formScriptDesign.Linkage);
                        hasSpecialDate = formScriptDesign.HasSpecialDate;
                        hasSpecialTime = formScriptDesign.HasSpecialTime;
                        hasSubTableDataTransfer = formScriptDesign.HasSubTableDataTransfer;
                    }

                    break;
                case JnpfKeyConst.TABLEGRIDTD:
                    {
                        if (!config.merged)
                        {
                            var formScriptDesign = FormScriptDesign(config.children, realisticControls, subTableName, formDataModel, columnDesignModel, dataType, logic, hasFlow);
                            list.Add(new FormControlDesignModel()
                            {
                                jnpfKey = config.jnpfKey,
                                Colspan = config.colspan,
                                Rowspan = config.rowspan,
                                Style = ":style=\"{'--backgroundColor':'" + config.backgroundColor + "'}\"",
                                Children = formScriptDesign.FormControlDesign
                            });
                            collapses.AddRange(formScriptDesign.Collapses);
                            dataRules.AddRange(formScriptDesign.DataRules);
                            options.AddRange(formScriptDesign.Options);
                            extraOptions.AddRange(formScriptDesign.ExtraOptions);
                            dataForm.AddRange(formScriptDesign.DataForm);
                            inlineEditorDataForm.AddRange(formScriptDesign.InlineEditorDataForm);
                            dataOptions.AddRange(formScriptDesign.DataOptions);
                            subTableDesign.AddRange(formScriptDesign.SubTableDesign);
                            linkage.AddRange(formScriptDesign.Linkage);
                            hasSpecialDate = formScriptDesign.HasSpecialDate;
                            hasSpecialTime = formScriptDesign.HasSpecialTime;
                            hasSubTableDataTransfer = formScriptDesign.HasSubTableDataTransfer;
                        }
                    }

                    break;
                case JnpfKeyConst.TABLE:
                    {
                        List<FormControlDesignModel> childrenTableList = new List<FormControlDesignModel>();
                        var childrenRealisticControls = realisticControls.Find(it => it.__vModel__.Equals(item.__vModel__) && it.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE)).__config__.children;

                        var childrenFormScript = FormScriptDesign(config.children, childrenRealisticControls, item.__vModel__, formDataModel, columnDesignModel, dataType, logic, hasFlow);
                        collapses.AddRange(childrenFormScript.Collapses);
                        dataRules.AddRange(childrenFormScript.DataRules);
                        options.AddRange(childrenFormScript.Options);
                        extraOptions.AddRange(childrenFormScript.ExtraOptions);
                        dataOptions.AddRange(childrenFormScript.DataOptions);
                        linkage.AddRange(childrenFormScript.Linkage);
                        hasSpecialDate = childrenFormScript.HasSpecialDate;
                        hasSpecialTime = childrenFormScript.HasSpecialTime;

                        // script 层
                        switch (item.addType)
                        {
                            case 1:
                                hasSubTableDataTransfer = true;
                                break;
                        }

                        item.footerBtnsList.Where(x => x.show).ToList().ForEach(x =>
                        {
                            x.value = x.value.Replace("-", "_");
                            x.actionConfig = x.actionConfig.ToJsonString() + ",";
                        });

                        var childrensRegList = new List<RegListModel>();
                        item.__config__.children?.ForEach(x => { if (x.__config__.regList != null && !x.__config__.regList.Any()) childrensRegList.AddRange(x.__config__.regList); });

                        // 按钮 多语言
                        if (logic.Equals(4))
                        {
                            foreach (var btnItem in item.columnBtnsList)
                                btnItem.label = btnItem.labelI18nCode.IsNotEmptyOrNull() ? "{{$t('" + btnItem.labelI18nCode + "','" + btnItem.label + "')}}" : btnItem.label;
                            foreach (var btnItem in item.footerBtnsList)
                                btnItem.label = btnItem.labelI18nCode.IsNotEmptyOrNull() ? "{{$t('" + btnItem.labelI18nCode + "','" + btnItem.label + "')}}" : btnItem.label;
                            if (item.layoutType == "table")
                                configLabel = config.labelI18nCode.IsNotEmptyOrNull() ? ":content=\"$t('" + config.labelI18nCode + "', '" + config.label + "')\"" : (config.label.IsNullOrEmpty() ? string.Empty : string.Format("content=\"{0}\"", config.label));
                        }

                        foreach (var it in item.__config__.complexHeaderList)
                        {
                            if (it.fullNameI18nCode.IsNotEmptyOrNull())
                                it.fullName = string.Format("$t('{0}', '{1}')", it.fullNameI18nCode, it.fullName);
                        }
                        subTableDesign.Add(new CodeGenFrontEndSubTableDesignModel
                        {
                            Label = config.labelI18nCode.IsNotEmptyOrNull() ? "\"$t('" + config.labelI18nCode + "', '" + config.label + "')\"" : (config.label.IsNullOrEmpty() ? string.Empty : string.Format("\"{0}\"", config.label)),
                            Name = item.__vModel__,
                            SummaryField = item.summaryField.ToJsonString(),
                            Header = childrenFormScript.SubTableHeader,
                            DataForm = childrenFormScript.DataForm,
                            HasSummary = item.summaryField?.Count > 0 ? true : false,
                            LinkageOptions = childrenFormScript.SubTableLinkageOptions,
                            Controls = childrenFormScript.SubTableControls,
                            ComplexColumns = item.__config__.complexHeaderList != null && item.__config__.complexHeaderList.Any() ? item.__config__.complexHeaderList.ToJsonString().Replace("\"$t('", "$t('").Replace("')\",", "'),") : "[]",
                            ColumnBtnsList = item.columnBtnsList.Where(x => x.show).ToList(),
                            FooterBtnsList = item.footerBtnsList.Where(x => x.show).ToList(),
                            IsAnyBatchRemove = item.footerBtnsList.Any(x => x.show && x.value.Equals("batchRemove")),
                            IsAnyRequired = item.__config__.children.Any(x => x.__config__.required),
                            RegList = childrensRegList,
                            LayoutType = item.layoutType,
                            DefaultExpandAll = item.defaultExpandAll,
                            IsNumLimit = item.isNumLimit,
                            NumLimit = item.numLimit,
                        });

                        // template层
                        list.Add(new FormControlDesignModel()
                        {
                            jnpfKey = config.jnpfKey,
                            Name = item.__vModel__,
                            OriginalName = config.tableName,
                            HelpMessage = logic.Equals(4) && config.tipLabelI18nCode.IsNotEmptyOrNull() ? $" :helpMessage=\"$t('{config.tipLabelI18nCode}', '{config.tipLabel}')\" " : (config.tipLabel.IsNotEmptyOrNull() ? $" helpMessage='{config.tipLabel}' " : string.Empty),
                            TipLabel = logic.Equals(4) && config.showLabel && config.tipLabelI18nCode.IsNotEmptyOrNull() ? $"<BasicHelp :text=\"$t('{config.tipLabelI18nCode}', '{config.tipLabel}')\"/> " : (config.tipLabel.IsNotEmptyOrNull() ? $"<BasicHelp text='{config.tipLabel}'/> " : string.Empty),
                            Span = config.span,
                            ShowTitle = config.showTitle,
                            Label = configLabel,
                            ChildTableName = config.tableName.ParseToPascalCase(),
                            Children = childrenFormScript.FormControlDesign,
                            LabelWidth = config?.labelWidth ?? formDataModel.labelWidth,
                            ShowSummary = item.summaryField?.Count > 0 ? true : false,
                            ComplexColumns = item.__config__.complexHeaderList != null && item.__config__.complexHeaderList.Any() ? new List<FormControlDesignModel>() : null,
                            ColumnBtnsList = item.columnBtnsList.Where(x => x.show).ToList(),
                            FooterBtnsList = item.footerBtnsList.Where(x => x.show).ToList(),
                            IsAnyBatchRemove = item.footerBtnsList.Any(x => x.show && x.value.Equals("batchRemove")),
                            IsAnyRequired = item.__config__.children.Any(x => x.__config__.required),
                            RegList = childrensRegList,
                            LayoutType = item.layoutType,
                            SummaryField = item.summaryField.ToJsonString(),
                            DefaultExpandAll = item.defaultExpandAll,
                            IsNumLimit = item.isNumLimit,
                            NumLimit = item.numLimit,
                        });
                    }

                    break;
            }

            // 基础控件 高级控件 系统控件

            // 属性控件
            var attrControls = new List<string>() {
                JnpfKeyConst.POPUPATTR,
                JnpfKeyConst.RELATIONFORMATTR
            };

            bool isShow = false;

            // 当前循环 是否属性控件组
            if (attrControls.Contains(config.jnpfKey))
            {
                // 如果是展示数据 时跟随 关联功能 决定 是否展示
                switch (item.isStorage)
                {
                    case 0:
                        var relationField = Regex.Match(item.relationField, @"^(.+)_jnpfTable_").Groups[1].Value;
                        relationField = relationField.Replace(string.Format("jnpf_{0}_jnpf_", config.relationTable), string.Empty);
                        var relationControl = realisticControls.Find(it => it.__vModel__ == relationField);
                        isShow = relationControl.__config__.noShow;
                        break;
                }
            }

            // 控件隐藏的时候不添加
            if (true)
            {
                var formControlDesignModel = new FormControlDesignModel();
                formControlDesignModel.ShowLabel = config.showLabel;
                var realisticControl = realisticControls.Find(it => it.__vModel__.Equals(item.__vModel__) && it.__config__.jnpfKey.Equals(config.jnpfKey));
                realisticControl ??= new FieldsModel() { linkageReverseRelationship = new List<LinkageConfig>() };

                // 忽略生成改变事件列表
                List<string> ignoreChangeEventList = new List<string>
                {
                    JnpfKeyConst.POPUPSELECT,
                    JnpfKeyConst.POPUPTABLESELECT,
                    JnpfKeyConst.AUTOCOMPLETE,
                    JnpfKeyConst.RELATIONFORM
                };

                // 寻找主副表联动子表内的控件绑定__vModel__
                if (!isSubTable && (realisticControl?.linkageReverseRelationship.Any(it => it.isChildren && !ignoreChangeEventList.Contains(it.jnpfKey))).ParseToBool() && !mainLinkageSubTable.Any(it => it.Name.Equals(subTableName) && it.Value.Equals(item.__vModel__)))
                {
                    List<CodeGenFrontEndFormState> state = new List<CodeGenFrontEndFormState>();
                    foreach (var mainConsonant in realisticControl.linkageReverseRelationship.FindAll(it => it.isChildren && !ignoreChangeEventList.Contains(it.jnpfKey)).ToList())
                    {
                        state.Add(new CodeGenFrontEndFormState
                        {
                            Name = mainConsonant.fieldName,
                            Value = mainConsonant.field,
                        });
                    }
                    mainLinkageSubTable.AddRange(state);
                }

                // 控件 通用 属性
                switch (config.jnpfKey)
                {
                    case JnpfKeyConst.COMINPUT:
                    case JnpfKeyConst.TEXTAREA:
                    case JnpfKeyConst.NUMINPUT:
                    case JnpfKeyConst.RADIO:
                    case JnpfKeyConst.CHECKBOX:
                    case JnpfKeyConst.SELECT:
                    case JnpfKeyConst.CASCADER:
                    case JnpfKeyConst.DATE:
                    case JnpfKeyConst.SWITCH:
                    case JnpfKeyConst.TIME:
                    case JnpfKeyConst.UPLOADFZ:
                    case JnpfKeyConst.UPLOADIMG:
                    case JnpfKeyConst.COLORPICKER:
                    case JnpfKeyConst.RATE:
                    case JnpfKeyConst.SLIDER:
                    case JnpfKeyConst.EDITOR:
                    case JnpfKeyConst.COMSELECT:
                    case JnpfKeyConst.DEPSELECT:
                    case JnpfKeyConst.POSSELECT:
                    case JnpfKeyConst.USERSELECT:
                    case JnpfKeyConst.ROLESELECT:
                    case JnpfKeyConst.GROUPSELECT:
                    case JnpfKeyConst.USERSSELECT:
                    case JnpfKeyConst.TREESELECT:
                    case JnpfKeyConst.POPUPTABLESELECT:
                    case JnpfKeyConst.AUTOCOMPLETE:
                    case JnpfKeyConst.ADDRESS:
                    case JnpfKeyConst.BILLRULE:
                    case JnpfKeyConst.RELATIONFORM:
                    case JnpfKeyConst.POPUPSELECT:
                    case JnpfKeyConst.CREATEUSER:
                    case JnpfKeyConst.CREATETIME:
                    case JnpfKeyConst.MODIFYUSER:
                    case JnpfKeyConst.MODIFYTIME:
                    case JnpfKeyConst.CURRORGANIZE:
                    case JnpfKeyConst.CURRPOSITION:
                    case JnpfKeyConst.SIGN:
                    case JnpfKeyConst.SIGNATURE:
                    case JnpfKeyConst.LOCATION:
                        {
                            string vModel = string.Empty;
                            vModel = string.Format("v-model:value=\"dataForm.{0}\" ", item.__vModel__);
                            switch (isSubTable)
                            {
                                case true:
                                    vModel = string.Format("v-model:value=\"record.{0}\" ", item.__vModel__);
                                    break;
                            }
                            formControlDesignModel = new FormControlDesignModel()
                            {
                                jnpfKey = config.jnpfKey,
                                Span = config.span,
                                Name = item.__vModel__,
                                Label = configLabel,
                                ShowLabel = config.showLabel,
                                TipLabel = logic.Equals(4) && config.showLabel && config.tipLabelI18nCode.IsNotEmptyOrNull() ? $"<BasicHelp :text=\"$t('{config.tipLabelI18nCode}', '{config.tipLabel}')\"/> " : (config.tipLabel.IsNotEmptyOrNull() ? $"<BasicHelp text='{config.tipLabel}'/> " : string.Empty),
                                Tag = config.tag,
                                vModel = vModel,
                                OldVModel = item.__vModel__,
                                LabelWidth = config?.labelWidth ?? formDataModel.labelWidth,
                                Style = !string.IsNullOrEmpty(item.style?.ToString()) ? string.Format(":style='{0}' ", item.style.ToJsonString().Replace("\r\n", "")) : string.Empty,
                                Disabled = item.disabled ? string.Format("disabled ") : string.Empty,
                                Hidden = config.noShow ? string.Format("v-show=\"false\" ") : string.Empty,
                                IsLinked = realisticControl.IsLinked,
                                IsLinkage = realisticControl.IsLinkage,
                                EnableLocationScope = string.Format(":enableLocationScope='{0}' ", item.enableLocationScope.ToString().ToLower()),
                                AutoLocation = string.Format(":autoLocation='{0}' ", item.autoLocation.ToString().ToLower()),
                                AdjustmentScope = string.Format(":adjustmentScope='{0}' ", item.adjustmentScope),
                                EnableDesktopLocation = string.Format(":enableDesktopLocation='{0}' ", item.enableDesktopLocation.ToString().ToLower()),
                                LocationScope = string.Format(":locationScope='{0}' ", item.locationScope.IsNotEmptyOrNull() ? item.locationScope.ToJsonString().Replace("\r\n", "") : "[]"),
                                ShowCount = config.jnpfKey.Equals(JnpfKeyConst.COMINPUT) || config.jnpfKey.Equals(JnpfKeyConst.TEXTAREA) ? string.Format(" :showCount='{0}' ", item.showCount.ToString().ToLower()) : string.Empty,
                                Disaabled = config.jnpfKey.Equals(JnpfKeyConst.SIGNATURE) ? string.Format(" :disaabled=\"{0}\" ", item.disaabled.ToString().ToLower()) : string.Empty,
                                IsInvoke = config.jnpfKey.Equals(JnpfKeyConst.SIGN) ? string.Format(" :isInvoke='{0}' ", item.isInvoke.ToString().ToLower()) : string.Empty,
                                AbleIds = item.ableIds.IsNotEmptyOrNull() ? string.Format(" :ableIds='{0}' ", item.ableIds.ToJsonString().Replace("\r\n", "")) : string.Empty,
                                SortRule = item.sortRule.IsNotEmptyOrNull() ? string.Format(" :sortRule='{0}'", item.sortRule.ToJsonString().Replace("\r\n", "")) : string.Empty,
                                TimeFormat = item.timeFormat.IsNotEmptyOrNull() ? string.Format(" timeFormat='{0}'", item.timeFormat) : string.Empty,
                                required = item.__config__.required,
                                QueryType = item.queryType != null ? string.Format(" :queryType={0} ", item.queryType) : string.Empty,
                                ShowType = item.buttonText != null ? $" buttonText='{item.buttonText}' " : string.Empty,
                            };
                            if (hasFlow)
                                formControlDesignModel.Disabled = isSubTable ? string.Format(":disabled=\"judgeWrite('{0}-{1}')\" ", subTableName, item.__vModel__) : string.Format(":disabled=\"judgeWrite('{0}')\" ", item.__vModel__);

                            // 如果联动配置内
                            if (!realisticControl.linkageReverseRelationship.Any(it => !ignoreChangeEventList.Contains(it.jnpfKey)))
                                formControlDesignModel.IsLinked = false;

                            // 表单正则
                            var required = new List<CodeGenFrontEndDataRequiredModel>();
                            var rule = new List<CodeGenFrontEndDataRuleModel>();
                            switch ((config.required || config.regList?.Count > 0) && !isSubTable)
                            {
                                case true:
                                    switch (config.required)
                                    {
                                        case true:
                                            var trigger = config?.trigger;
                                            var label = string.Format("请输入{0}", formDataModel.labelSuffix.IsNotEmptyOrNull() ? config.label.Replace(formDataModel.labelSuffix, "") : config.label);
                                            required.Add(new CodeGenFrontEndDataRequiredModel
                                            {
                                                required = true,
                                                message = logic.Equals(4) && config.labelI18nCode.IsNotEmptyOrNull() ? "$t('" + config.labelI18nCode + "', '" + label + "')" : (string.Format("'{0}'", (label.IsNullOrEmpty() ? string.Empty : label))),
                                                trigger = !string.IsNullOrEmpty(trigger.ToString()) ? (trigger is JArray ? trigger : trigger?.ToString()) : "blur"
                                            });
                                            break;
                                    }

                                    switch (config.regList?.Count > 0)
                                    {
                                        case true:
                                            foreach (var reg in config.regList)
                                            {
                                                var trigger = config?.trigger;
                                                rule.Add(new CodeGenFrontEndDataRuleModel
                                                {
                                                    pattern = reg.pattern,
                                                    message = logic.Equals(4) && reg.messageI18nCode.IsNotEmptyOrNull() ? "$t('" + reg.messageI18nCode + "', '" + reg.message + "')" : (string.Format("'{0}'", reg.message)),
                                                    trigger = !string.IsNullOrEmpty(trigger?.ToString()) ? (trigger is JArray ? trigger : trigger?.ToString()) : "blur"
                                                });
                                            }
                                            break;
                                    }

                                    dataRules.Add(new CodeGenFrontEndDataRule
                                    {
                                        Name = item.__vModel__,
                                        Required = required,
                                        Rule = rule
                                    });
                                    break;
                            }

                            // 需要生成Change事件
                            if (formControlDesignModel.IsLinked)
                            {
                                linkage.Add(new CodeGenFrontEndLinkageConfig
                                {
                                    Name = item.__vModel__,
                                    IsSubTable = isSubTable,
                                    SubTableName = subTableName,
                                    LinkageRelationship = realisticControl.linkageReverseRelationship,
                                });
                            }

                            // 子表表头
                            switch (isSubTable)
                            {
                                case true:
                                    subTableControls.Add(new CodeGenFrontEndSubTableControlModel
                                    {
                                        Name = item.__vModel__,
                                        jnpfKey = config.jnpfKey,
                                        Multiple = item.multiple,
                                        Required = config.required,
                                        RegList = config.regList,
                                        Label = configLabel,
                                    });
                                    var isSystemControl = item.__config__.jnpfKey.Equals(JnpfKeyConst.BILLRULE) || item.__config__.jnpfKey.Equals(JnpfKeyConst.MODIFYUSER)
                                        || item.__config__.jnpfKey.Equals(JnpfKeyConst.CREATEUSER) || item.__config__.jnpfKey.Equals(JnpfKeyConst.MODIFYTIME) || item.__config__.jnpfKey.Equals(JnpfKeyConst.CREATETIME)
                                        || item.__config__.jnpfKey.Equals(JnpfKeyConst.CURRPOSITION) || item.__config__.jnpfKey.Equals(JnpfKeyConst.CURRORGANIZE);
                                    var addItem = new CodeGenFrontEndSubTableHeaderModel
                                    {
                                        Title = logic.Equals(4) && config.labelI18nCode.IsNotEmptyOrNull() ? "$t('" + config.labelI18nCode + "', '" + config.label + "')" : string.Format("\"{0}\"", (config.label.IsNullOrEmpty() ? string.Empty : config.label)),
                                        DataIndex = item.__vModel__,
                                        Key = item.__vModel__,
                                        TipLabel = config.tipLabel,
                                        Required = config.required,
                                        Thousand = item.thousands,
                                        Span = item.__config__.span,
                                        LabelWidth = item.__config__.labelWidth,
                                        Width = item.__config__.columnWidth,
                                        NoShow = config.noShow,
                                        IsSystemControl = isSystemControl,
                                    };

                                    // 子表控件冻结处理
                                    addItem.Align = config.tableAlgin.IsNotEmptyOrNull() ? config.tableAlgin : "left";
                                    addItem.TableFixed = config.tableFixed.IsNullOrEmpty() || config.tableFixed.Equals("none") ? "false" : string.Format("'{0}'", config.tableFixed);
                                    subTableHeader.Add(addItem);
                                    break;
                            }
                        }
                        break;
                    case JnpfKeyConst.RELATIONFORMATTR:
                    case JnpfKeyConst.POPUPATTR:
                        {
                            switch (item.isStorage)
                            {
                                case 0:
                                    var relationField = Regex.Match(item.relationField, @"^(.+)_jnpfTable_").Groups[1].Value;
                                    var relationFieldNoShow = fieldList.Find(x => x.__vModel__.Equals(relationField)).__config__.noShow;
                                    formControlDesignModel = new FormControlDesignModel()
                                    {
                                        jnpfKey = config.jnpfKey,
                                        Span = config.span,
                                        Name = config.formId.ToString(),
                                        Label = configLabel,
                                        ShowLabel = config.showLabel,
                                        TipLabel = logic.Equals(4) && config.tipLabelI18nCode.IsNotEmptyOrNull() ? $"<BasicHelp :text=\"$t('{config.tipLabelI18nCode}', '{config.tipLabel}')\"/> " : (config.tipLabel.IsNotEmptyOrNull() ? $"<BasicHelp text='{config.tipLabel}'/> " : string.Empty),
                                        Tag = config.tag,
                                        Style = !string.IsNullOrEmpty(item.style?.ToString()) ? string.Format(":style='{0}' ", item.style.ToJsonString().Replace("\r\n", "")) : string.Empty,
                                        LabelWidth = config?.labelWidth ?? formDataModel.labelWidth,
                                        Hidden = relationFieldNoShow ? string.Format("v-show=\"false\" ") : string.Empty,
                                    };
                                    break;
                                default:
                                    string vModel = string.Empty;
                                    vModel = string.Format("v-model:value=\"dataForm.{0}\" ", item.__vModel__);
                                    switch (isSubTable)
                                    {
                                        case true:
                                            vModel = string.Format("v-model:value=\"record.{0}\" ", item.__vModel__);
                                            break;
                                    }
                                    formControlDesignModel = new FormControlDesignModel()
                                    {
                                        jnpfKey = config.jnpfKey,
                                        Span = config.span,
                                        Name = item.__vModel__,
                                        Label = configLabel,
                                        ShowLabel = config.showLabel,
                                        TipLabel = logic.Equals(4) && config.tipLabelI18nCode.IsNotEmptyOrNull() ? $"<BasicHelp :text=\"$t('{config.tipLabelI18nCode}', '{config.tipLabel}')\"/> " : (config.tipLabel.IsNotEmptyOrNull() ? $"<BasicHelp text='{config.tipLabel}'/> " : string.Empty),
                                        Tag = config.tag,
                                        vModel = vModel,
                                        OldVModel = item.__vModel__,
                                        LabelWidth = config?.labelWidth ?? formDataModel.labelWidth,
                                        Style = !string.IsNullOrEmpty(item.style?.ToString()) ? string.Format(":style='{0}' ", item.style.ToJsonString().Replace("\r\n", "")) : string.Empty,
                                        Disabled = item.disabled ? string.Format("disabled ") : string.Empty,
                                        Hidden = config.noShow ? string.Format("v-show=\"false\" ") : string.Empty,
                                        IsLinked = realisticControl.IsLinked,
                                        IsLinkage = realisticControl.IsLinkage,
                                    };

                                    if (!realisticControl.linkageReverseRelationship.Any(it => !ignoreChangeEventList.Contains(it.jnpfKey)))
                                        formControlDesignModel.IsLinked = false;

                                    // 需要生成Change事件
                                    if (formControlDesignModel.IsLinked)
                                    {
                                        linkage.Add(new CodeGenFrontEndLinkageConfig
                                        {
                                            Name = item.__vModel__,
                                            IsSubTable = isSubTable,
                                            SubTableName = subTableName,
                                            LinkageRelationship = realisticControl.linkageReverseRelationship,
                                        });
                                    }
                                    break;
                            }

                            // 子表表头
                            switch (isSubTable)
                            {
                                case true:
                                    var isSystemControl = item.__config__.jnpfKey.Equals(JnpfKeyConst.BILLRULE) || item.__config__.jnpfKey.Equals(JnpfKeyConst.MODIFYUSER)
                                        || item.__config__.jnpfKey.Equals(JnpfKeyConst.CREATEUSER) || item.__config__.jnpfKey.Equals(JnpfKeyConst.MODIFYTIME) || item.__config__.jnpfKey.Equals(JnpfKeyConst.CREATETIME)
                                        || item.__config__.jnpfKey.Equals(JnpfKeyConst.CURRPOSITION) || item.__config__.jnpfKey.Equals(JnpfKeyConst.CURRORGANIZE);
                                    subTableHeader.Add(new CodeGenFrontEndSubTableHeaderModel
                                    {
                                        Title = logic.Equals(4) && config.labelI18nCode.IsNotEmptyOrNull() ? "$t('" + config.labelI18nCode + "', '" + config.label + "')" : string.Format("\"{0}\"", (config.label.IsNullOrEmpty() ? string.Empty : config.label)),
                                        DataIndex = item.isStorage == 0 ? config.formId?.ToString() : item.__vModel__,
                                        Key = item.isStorage == 0 ? config.formId?.ToString() : item.__vModel__,
                                        TipLabel = config.tipLabel,
                                        Required = config.required,
                                        Thousand = item.thousands,
                                        Align = config.tableAlgin.IsNotEmptyOrNull() ? config.tableAlgin : "left",
                                        TableFixed = config.tableFixed.IsNullOrEmpty() || config.tableFixed.Equals("none") ? "false" : string.Format("'{0}'", config.tableFixed),
                                        IsSystemControl = isSystemControl,
                                        LabelWidth = config?.labelWidth ?? formDataModel.labelWidth,
                                        Width = config?.columnWidth,
                                        NoShow = config.noShow,
                                    });
                                    break;
                            }
                        }
                        break;
                    case JnpfKeyConst.JNPFTEXT:
                        {
                            list.Add(new FormControlDesignModel()
                            {
                                jnpfKey = config.jnpfKey,
                                Span = config.span,
                                TextStyle = item.textStyle != null ? item.textStyle.ToJsonString().Replace("\r\n", "") : string.Empty,
                                Content = logic.Equals(4) && item.contentI18nCode.IsNotEmptyOrNull() ? $":content=\"$t('{item.contentI18nCode}', '{item.content}')\" " : (item.content.IsNotEmptyOrNull() ? $"content='{item.content}' " : string.Empty),
                                LabelWidth = config?.labelWidth ?? formDataModel.labelWidth,
                                required = item.__config__.required,
                            });
                        }

                        break;
                    case JnpfKeyConst.BUTTON:
                        {
                            list.Add(new FormControlDesignModel()
                            {
                                jnpfKey = config.jnpfKey,
                                Span = config.span,
                                Align = item.align,
                                ButtonText = item.buttonTextI18nCode.IsNotEmptyOrNull() ? $":buttonText=\"$t('{item.buttonTextI18nCode}', '{item.buttonText}')\" " : (item.buttonText.IsNotEmptyOrNull() ? $"buttonText='{item.buttonText}' " : string.Empty),
                                Type = !string.IsNullOrEmpty(item.type) ? string.Format("type=\"{0}\" ", item.type) : string.Empty,
                                Disabled = item.disabled ? "disabled " : string.Empty,
                                LabelWidth = config?.labelWidth ?? formDataModel.labelWidth,
                            });
                        }

                        break;
                    case JnpfKeyConst.ALERT:
                        {
                            list.Add(new FormControlDesignModel()
                            {
                                jnpfKey = config.jnpfKey,
                                Span = config.span,
                                Title = logic.Equals(4) && item.titleI18nCode.IsNotEmptyOrNull() ? $":title=\"$t('{item.titleI18nCode}', '{item.title}')\" " : (item.title.IsNotEmptyOrNull() ? $"title='{item.title}' " : string.Empty),
                                Type = item.type,
                                ShowIcon = item.showIcon ? "true" : "false",
                                Description = logic.Equals(4) && item.descriptionI18nCode.IsNotEmptyOrNull() ? $":description=\"$t('{item.descriptionI18nCode}', '{item.description}')\" " : (item.description.IsNotEmptyOrNull() ? $"description='{item.description}' " : string.Empty),
                                CloseText = logic.Equals(4) && item.closeTextI18nCode.IsNotEmptyOrNull() ? $":closeText=\"$t('{item.closeTextI18nCode}', '{item.closeText}')\" " : (item.closeText.IsNotEmptyOrNull() ? $"closeText='{item.closeText}' " : string.Empty),
                                Closable = item.closable,
                                LabelWidth = config?.labelWidth ?? formDataModel.labelWidth,
                            });
                        }

                        break;
                    case JnpfKeyConst.QRCODE:
                    case JnpfKeyConst.BARCODE:
                        {
                            var staticText = string.Empty;
                            if (item.dataType.Equals("static")) staticText = string.Format("staticText=\"{0}\"", item.staticText);
                            else if (item.relationField.IsNotEmptyOrNull()) staticText = string.Format(":staticText=\"{0}\"", "dataForm." + item.relationField);

                            list.Add(new FormControlDesignModel()
                            {
                                jnpfKey = config.jnpfKey,
                                TipLabel = logic.Equals(4) && config.showLabel && config.tipLabelI18nCode.IsNotEmptyOrNull() ? $"<BasicHelp :text=\"t('{config.tipLabelI18nCode}', '{config.tipLabel}')\"/> " : (config.tipLabel.IsNotEmptyOrNull() ? $"<BasicHelp text='{config.tipLabel}'/> " : string.Empty),
                                Label = configLabel,
                                ShowLabel = item.__config__.showLabel,
                                Span = item.__config__.span,
                                Tag = config.tag,
                                StaticText = staticText,
                                RelationField = item.relationField.IsNullOrEmpty() ? string.Empty : string.Format("relationField=\"{0}\"", item.relationField),
                                RelationField_Id = item.relationField.IsNullOrEmpty() ? string.Empty : string.Format("relationField=\"{0}\"", item.relationField + "_id"),
                                DataType = string.Format("dataType=\"{0}\"", item.dataType),
                                ColorDark = string.Format("colorDark=\"{0}\"", item.colorDark),
                                ColorLight = string.Format("colorLight=\"{0}\"", item.colorLight),
                                Width = string.Format(":width='{0}'", item.width),
                                Height = string.Format(":height='{0}'", item.height),
                                Format = string.Format("format=\"{0}\"", item.format),
                                LineColor = string.Format("lineColor=\"{0}\"", item.lineColor),
                                Background = string.Format("background=\"{0}\"", item.background),
                                LabelWidth = config?.labelWidth ?? formDataModel.labelWidth,
                            });
                        }

                        break;
                    case JnpfKeyConst.LINK:
                    case JnpfKeyConst.IFRAME:
                        {
                            list.Add(new FormControlDesignModel()
                            {
                                jnpfKey = config.jnpfKey,
                                TipLabel = logic.Equals(4) && config.showLabel && config.tipLabelI18nCode.IsNotEmptyOrNull() ? $"<BasicHelp :text=\"t('{config.tipLabelI18nCode}', '{config.tipLabel}')\"/> " : (config.tipLabel.IsNotEmptyOrNull() ? $"<BasicHelp text='{config.tipLabel}'/> " : string.Empty),
                                Label = configLabel,
                                Span = config.span,
                                Content = logic.Equals(4) && item.contentI18nCode.IsNotEmptyOrNull() ? $":content=\"$t('{item.contentI18nCode}', '{item.content}')\" " : (item.content.IsNotEmptyOrNull() ? $"content='{item.content}' " : string.Empty),
                                Href = item.href,
                                Target = item.target,
                                TextStyle = item.textStyle != null ? item.textStyle.ToJsonString().Replace("\r\n", "") : string.Empty,
                                Height = item.height?.ToString(),
                                ShowLabel = config.showLabel,
                                BorderColor = string.Format(" borderColor='{0}' ", item.borderColor),
                                BorderType = string.Format(" borderType='{0}' ", item.borderType),
                                BorderWidth = string.Format(" :borderWidth={0} ", item.borderWidth),
                                LabelWidth = config?.labelWidth ?? formDataModel.labelWidth,
                            });
                        }

                        break;
                }

                // 基础控件 高级控件 系统控件 个别控件特有属性

                // Placeholder 多语言
                var placeholder = logic.Equals(4) && item.placeholderI18nCode.IsNotEmptyOrNull() ? $":placeholder=\"$t('{item.placeholderI18nCode}', '{item.placeholder}')\" " : (item.placeholder.IsNotEmptyOrNull() ? $"placeholder='{item.placeholder}' " : string.Empty);

                switch (config.jnpfKey)
                {
                    case JnpfKeyConst.LOCATION:
                        formControlDesignModel.EnableLocationScope = string.Format(":enableLocationScope='{0}' ", item.enableLocationScope.ToString().ToLower());
                        formControlDesignModel.AutoLocation = string.Format(":autoLocation='{0}' ", item.autoLocation.ToString().ToLower());
                        formControlDesignModel.AdjustmentScope = string.Format(":adjustmentScope='{0}' ", item.adjustmentScope);
                        formControlDesignModel.EnableDesktopLocation = string.Format(":enableDesktopLocation='{0}' ", item.enableDesktopLocation.ToString().ToLower());
                        formControlDesignModel.LocationScope = string.Format(":locationScope='{0}' ", item.locationScope.IsNotEmptyOrNull() ? item.locationScope.ToJsonString().Replace("\r\n", "") : "[]");
                        break;
                    case JnpfKeyConst.COMINPUT:
                        formControlDesignModel.Placeholder = placeholder;
                        formControlDesignModel.AddonBefore = !string.IsNullOrEmpty(item.addonBefore) ? string.Format("addonBefore=\"{0}\" ", item.addonBefore) : string.Empty;
                        formControlDesignModel.AddonAfter = !string.IsNullOrEmpty(item.addonAfter) ? string.Format("addonAfter=\"{0}\" ", item.addonAfter) : string.Empty;
                        formControlDesignModel.PrefixIcon = !string.IsNullOrEmpty(item.prefixIcon) ? string.Format("prefix-icon=\"{0}\" ", item.prefixIcon) : string.Empty;
                        formControlDesignModel.SuffixIcon = !string.IsNullOrEmpty(item.suffixIcon) ? string.Format("suffix-icon=\"{0}\" ", item.suffixIcon) : string.Empty;
                        formControlDesignModel.MaxLength = item.maxlength != null ? string.Format(":maxlength=\"{0}\" ", item.maxlength) : string.Empty;
                        formControlDesignModel.ShowPassword = item.showPassword ? string.Format("show-password ") : string.Empty;
                        formControlDesignModel.UseScan = item.useScan ? "useScan " : string.Empty;
                        formControlDesignModel.UseMask = item.useMask ? "useMask " : string.Empty;
                        formControlDesignModel.MaskConfig = item.maskConfig != null ? $":maskConfig='{item.maskConfig.ToJsonString().Replace("\r\n", "")}' " : string.Empty;
                        formControlDesignModel.Readonly = item.@readonly ? string.Format("readonly ") : string.Empty;
                        formControlDesignModel.Clearable = item.clearable ? string.Format("allowClear ") : string.Empty;
                        break;
                    case JnpfKeyConst.TEXTAREA:
                        formControlDesignModel.Placeholder = placeholder;
                        formControlDesignModel.MaxLength = item.maxlength != null ? string.Format(":maxlength=\"{0}\" ", item.maxlength) : string.Empty;
                        formControlDesignModel.AutoSize = !string.IsNullOrEmpty(item.autoSize?.ToString()) ? string.Format(":autoSize='{0}' ", item.autoSize.ToJsonString().Replace("\r\n", "")) : string.Empty;
                        formControlDesignModel.Clearable = item.clearable ? string.Format("allowClear ") : string.Empty;
                        formControlDesignModel.Readonly = item.@readonly ? string.Format("readonly ") : string.Empty;
                        break;
                    case JnpfKeyConst.NUMINPUT:
                        formControlDesignModel.Placeholder = placeholder;
                        formControlDesignModel.AddonBefore = !string.IsNullOrEmpty(item.addonBefore) ? string.Format("addonBefore=\"{0}\" ", item.addonBefore) : string.Empty;
                        formControlDesignModel.AddonAfter = !string.IsNullOrEmpty(item.addonAfter) ? string.Format("addonAfter=\"{0}\" ", item.addonAfter) : string.Empty;
                        formControlDesignModel.Min = !string.IsNullOrEmpty(item.min.ToString()) ? string.Format(":min=\"{0}\" ", item.min) : string.Empty;
                        formControlDesignModel.Max = !string.IsNullOrEmpty(item.max.ToString()) ? string.Format(":max=\"{0}\" ", item.max) : string.Empty;
                        formControlDesignModel.Precision = !string.IsNullOrEmpty(item.precision.ToString()) ? string.Format(":precision=\"{0}\" ", item.precision) : string.Empty;
                        formControlDesignModel.Step = item.controls ? string.Format(":step=\"{0}\" ", item.step) : string.Empty;
                        formControlDesignModel.Controls = string.Format(":controls=\"{0}\" ", item.controls.ToLower());
                        formControlDesignModel.Thousands = item.thousands ? string.Format("thousands ") : string.Empty;
                        formControlDesignModel.AmountChinese = item.isAmountChinese ? string.Format("isAmountChinese ") : string.Empty;
                        break;
                    case JnpfKeyConst.SWITCH:
                        break;
                    case JnpfKeyConst.RADIO:
                        {
                            var optionsObj = string.Format(":options=\"optionsObj.{0}Options\" ", isSubTable ? string.Format("{0}_{1}", subTableName, item.__vModel__) : item.__vModel__);
                            if ((realisticControl?.IsLinkage).ParseToBool() && isSubTable)
                                optionsObj = string.Format(":options=\"record.{0}Options\" ", string.Format("{0}_{1}", subTableName, item.__vModel__));

                            formControlDesignModel.Options = item.options != null ? optionsObj : string.Empty;
                            formControlDesignModel.MainProps = item.props != null ? string.Format(":fieldNames=\"optionsObj.{0}Props\" ", isSubTable ? string.Format("{0}_{1}", subTableName, item.__vModel__) : item.__vModel__) : string.Empty;
                            formControlDesignModel.Size = item.optionType.Equals("button") ? string.Format("size=\"{0}\" ", item.size) : string.Empty;
                            formControlDesignModel.Direction = string.Format("direction=\"{0}\" ", item.direction);
                            formControlDesignModel.OptionType = string.Format("optionType=\"{0}\" ", item.optionType);
                        }
                        break;
                    case JnpfKeyConst.CHECKBOX:
                        {
                            var optionsObj = string.Format(":options=\"optionsObj.{0}Options\" ", isSubTable ? string.Format("{0}_{1}", subTableName, item.__vModel__) : item.__vModel__);
                            if ((realisticControl?.IsLinkage).ParseToBool() && isSubTable && !mainLinkageSubTable.Any(it => it.Name.Equals(subTableName) && it.Value.Equals(item.__vModel__)))
                                optionsObj = string.Format(":options=\"record.{0}Options\" ", string.Format("{0}_{1}", subTableName, item.__vModel__));
                            formControlDesignModel.Options = item.options != null ? optionsObj : string.Empty;
                            formControlDesignModel.MainProps = item.props != null ? string.Format(":fieldNames=\"optionsObj.{0}Props\" ", isSubTable ? string.Format("{0}_{1}", subTableName, item.__vModel__) : item.__vModel__) : string.Empty;
                            formControlDesignModel.Direction = string.Format("direction=\"{0}\" ", item.direction);
                        }
                        break;
                    case JnpfKeyConst.SELECT:
                    case JnpfKeyConst.CASCADER:
                    case JnpfKeyConst.TREESELECT:
                        {
                            var optionsObj = string.Format(":options=\"optionsObj.{0}Options\" ", isSubTable ? string.Format("{0}_{1}", subTableName, item.__vModel__) : item.__vModel__);
                            if ((realisticControl?.IsLinkage).ParseToBool() && isSubTable && !mainLinkageSubTable.Any(it => it.Name.Equals(subTableName) && it.Value.Equals(item.__vModel__)))
                                optionsObj = string.Format(":options=\"record.{0}Options\" ", string.Format("{0}_{1}", subTableName, item.__vModel__));

                            formControlDesignModel.Options = item.options != null ? optionsObj : string.Empty;
                            formControlDesignModel.Placeholder = placeholder;
                            formControlDesignModel.MainProps = item.props != null ? string.Format(":fieldNames=\"optionsObj.{0}Props\" ", isSubTable ? string.Format("{0}_{1}", subTableName, item.__vModel__) : item.__vModel__) : string.Empty;
                            formControlDesignModel.Clearable = item.clearable ? string.Format("allowClear ") : string.Empty;
                            formControlDesignModel.Filterable = item.filterable ? string.Format("showSearch ") : string.Empty;
                            formControlDesignModel.Multiple = item.multiple ? string.Format("multiple ") : string.Empty;
                        }
                        break;
                    case JnpfKeyConst.DATE:
                    case JnpfKeyConst.TIME:
                        formControlDesignModel.Placeholder = placeholder;
                        formControlDesignModel.Format = !string.IsNullOrEmpty(item.format) ? string.Format("format=\"{0}\" ", item.format) : string.Empty;
                        formControlDesignModel.Clearable = item.clearable ? string.Format("allowClear ") : string.Empty;
                        formControlDesignModel.StartTime = config.startTimeRule ? SpecialTimeAttributeAssembly(item, fieldList, 1, true) : string.Empty;
                        formControlDesignModel.EndTime = config.endTimeRule ? SpecialTimeAttributeAssembly(item, fieldList, 2, true) : string.Empty;
                        if (config.startTimeRule || config.endTimeRule)
                        {
                            switch (config.jnpfKey)
                            {
                                case JnpfKeyConst.DATE:
                                    hasSpecialDate = true;
                                    break;
                                case JnpfKeyConst.TIME:
                                    hasSpecialTime = true;
                                    break;
                            }
                        }
                        break;
                    case JnpfKeyConst.UPLOADFZ:
                        formControlDesignModel.ButtonText = logic.Equals(4) && item.buttonTextI18nCode.IsNotEmptyOrNull() ? $":buttonText=\"$t('{item.buttonTextI18nCode}', '{item.buttonText}')\" " : (item.buttonText.IsNotEmptyOrNull() ? $"buttonText='{item.buttonText}' " : string.Empty);
                        formControlDesignModel.ShowTip = !string.IsNullOrEmpty(item.tipText) ? string.Format("tipText=\"{0}\" ", item.tipText) : string.Empty;
                        formControlDesignModel.Accept = !string.IsNullOrEmpty(item.accept) ? string.Format("accept=\"{0}\" ", item.accept) : string.Empty;
                        formControlDesignModel.PathType = !string.IsNullOrEmpty(item.pathType) ? string.Format("pathType=\"{0}\" ", item.pathType) : string.Empty;
                        formControlDesignModel.IsAccount = item.pathType.Equals("selfPath") ? string.Format(":isAccount=\"{0}\" ", item.isAccount) : string.Empty;
                        formControlDesignModel.Folder = item.pathType.Equals("selfPath") ? string.Format("folder=\"{0}\" ", item.folder) : string.Empty;
                        formControlDesignModel.FileSize = string.Format(":fileSize=\"{0}\" ", item.fileSize);
                        formControlDesignModel.SizeUnit = string.Format("sizeUnit=\"{0}\" ", item.sizeUnit);
                        formControlDesignModel.Limit = string.Format(":limit=\"{0}\" ", item.limit);
                        break;
                    case JnpfKeyConst.UPLOADIMG:
                        formControlDesignModel.FileSize = string.Format(":fileSize=\"{0}\" ", item.fileSize);
                        formControlDesignModel.SizeUnit = string.Format("sizeUnit=\"{0}\" ", item.sizeUnit);
                        formControlDesignModel.Limit = string.Format(":limit=\"{0}\" ", item.limit);
                        formControlDesignModel.PathType = !string.IsNullOrEmpty(item.pathType) ? string.Format("pathType=\"{0}\" ", item.pathType) : string.Empty;
                        formControlDesignModel.IsAccount = item.pathType.Equals("selfPath") ? string.Format(":isAccount=\"{0}\" ", item.isAccount) : string.Empty;
                        formControlDesignModel.Folder = item.pathType.Equals("selfPath") ? string.Format("folder=\"{0}\" ", item.folder) : string.Empty;
                        formControlDesignModel.ShowType = item.showType != null && item.buttonText != null ? string.Format(" showType='{0}' ", item.showType) + $" buttonText='{item.buttonText}' " : string.Empty;
                        break;
                    case JnpfKeyConst.COLORPICKER:
                        formControlDesignModel.ColorFormat = !string.IsNullOrEmpty(item.colorFormat) ? string.Format("color-format=\"{0}\" ", item.colorFormat) : string.Empty;
                        break;
                    case JnpfKeyConst.RATE:
                        var rateCountAttr = logic.Equals(4) ? "count" : "max";
                        formControlDesignModel.Count = string.Format(":{0}=\"{1}\" ", rateCountAttr, item.count);
                        formControlDesignModel.AllowHalf = item.allowHalf ? string.Format("allow-half ") : string.Empty;
                        break;
                    case JnpfKeyConst.SLIDER:
                        formControlDesignModel.Min = item.min >= 0 ? string.Format(":min=\"{0}\" ", item.min) : string.Empty;
                        formControlDesignModel.Max = item.max >= 0 ? string.Format(":max=\"{0}\" ", item.max) : string.Empty;
                        formControlDesignModel.Step = string.Format(":step=\"{0}\" ", item.step);
                        break;
                    case JnpfKeyConst.EDITOR:
                        formControlDesignModel.Placeholder = placeholder;
                        break;
                    case JnpfKeyConst.COMSELECT:
                    case JnpfKeyConst.ROLESELECT:
                    case JnpfKeyConst.GROUPSELECT:
                        formControlDesignModel.Placeholder = placeholder;
                        formControlDesignModel.Multiple = item.multiple ? string.Format("multiple ") : string.Empty;
                        formControlDesignModel.Clearable = item.clearable ? string.Format("allowClear ") : string.Empty; formControlDesignModel.SelectType = !string.IsNullOrEmpty(item.selectType) ? string.Format("selectType=\"{0}\" ", item.selectType) : string.Empty;
                        formControlDesignModel.AbleIds = !string.IsNullOrEmpty(item.selectType) && item.selectType.Equals("custom") ? string.Format(":ableIds=\"optionsObj.{0}_AbleIds\" ", isSubTable ? string.Format("{0}_{1}", subTableName, item.__vModel__) : item.__vModel__) : string.Empty;
                        break;
                    case JnpfKeyConst.USERSELECT:
                        formControlDesignModel.Placeholder = placeholder;
                        formControlDesignModel.Multiple = item.multiple ? string.Format("multiple ") : string.Empty;
                        formControlDesignModel.Clearable = item.clearable ? string.Format("allowClear ") : string.Empty;
                        formControlDesignModel.SelectType = !string.IsNullOrEmpty(item.selectType) ? string.Format("selectType=\"{0}\" ", item.selectType) : string.Empty;
                        formControlDesignModel.AbleIds = !string.IsNullOrEmpty(item.selectType) && item.selectType.Equals("custom") ? string.Format(":ableIds=\"optionsObj.{0}_AbleIds\" ", isSubTable ? string.Format("{0}_{1}", subTableName, item.__vModel__) : item.__vModel__) : string.Empty;
                        switch (item.selectType)
                        {
                            case "custom":
                            case "all":
                                break;
                            default:
                                var relationField = item.relationField;
                                switch (relationField.IsMatch(@"tableField\d{3}-"))
                                {
                                    case true:
                                        relationField = relationField.ReplaceRegex(@"tableField\d{3}-", string.Empty);
                                        formControlDesignModel.AbleRelationIds = string.Format(":ableRelationIds=\"Array.isArray(record.{0}) ? record.{0} : [record.{0}]\" ", relationField);
                                        break;
                                    default:
                                        formControlDesignModel.AbleRelationIds = string.Format(":ableRelationIds=\"Array.isArray(dataForm.{0}) ? dataForm.{0} : [dataForm.{0}]\" ", relationField);
                                        break;
                                }
                                break;
                        }

                        break;
                    case JnpfKeyConst.DEPSELECT:
                    case JnpfKeyConst.POSSELECT:
                    case JnpfKeyConst.USERSSELECT:
                        formControlDesignModel.Placeholder = placeholder;
                        formControlDesignModel.Multiple = item.multiple ? string.Format("multiple ") : string.Empty;
                        formControlDesignModel.Clearable = item.clearable ? string.Format("allowClear ") : string.Empty;
                        formControlDesignModel.SelectType = !string.IsNullOrEmpty(item.selectType) ? string.Format("selectType=\"{0}\" ", item.selectType) : string.Empty;
                        formControlDesignModel.AbleIds = !string.IsNullOrEmpty(item.selectType) && item.selectType.Equals("custom") ? string.Format(":ableIds=\"optionsObj.{0}_AbleIds\" ", isSubTable ? string.Format("{0}_{1}", subTableName, item.__vModel__) : item.__vModel__) : string.Empty;
                        break;
                    case JnpfKeyConst.POPUPTABLESELECT:
                        formControlDesignModel.Placeholder = placeholder;
                        formControlDesignModel.Multiple = item.multiple ? string.Format("multiple ") : string.Empty;
                        formControlDesignModel.Clearable = item.clearable ? string.Format("allowClear ") : string.Empty;
                        formControlDesignModel.TemplateJson = string.Format(":templateJson=\"optionsObj.{0}TemplateJson\" ", isSubTable ? string.Format("{0}_{1}", subTableName, item.__vModel__) : item.__vModel__);
                        formControlDesignModel.Field = string.Format("field=\"{0}\" ", item.__vModel__);
                        formControlDesignModel.ColumnOptions = item.columnOptions != null ? string.Format(":columnOptions=\"optionsObj.{0}Options\" ", isSubTable ? string.Format("{0}_{1}", subTableName, item.__vModel__) : item.__vModel__) : string.Empty;
                        formControlDesignModel.HasPage = item.hasPage ? string.Format("hasPage ") : string.Empty;
                        formControlDesignModel.InterfaceId = !string.IsNullOrEmpty(item.interfaceId) ? string.Format("interfaceId=\"{0}\" ", item.interfaceId) : string.Empty;
                        formControlDesignModel.RelationField = !string.IsNullOrEmpty(item.relationField) ? string.Format("relationField=\"{0}\" ", item.relationField) : string.Empty;
                        formControlDesignModel.PropsValue = !string.IsNullOrEmpty(item.propsValue) ? string.Format("propsValue=\"{0}\" ", item.propsValue) : string.Empty;
                        formControlDesignModel.PageSize = item.pageSize != null ? string.Format(":pageSize=\"{0}\" ", item.pageSize) : string.Empty;
                        formControlDesignModel.PopupType = !string.IsNullOrEmpty(item.popupType) ? string.Format("popupType=\"{0}\" ", item.popupType) : string.Empty;
                        formControlDesignModel.PopupTitle = !string.IsNullOrEmpty(item.popupTitle) ? string.Format("popupTitle=\"{0}\" ", item.popupTitle) : string.Empty;
                        formControlDesignModel.PopupWidth = !string.IsNullOrEmpty(item.popupWidth) ? string.Format("popupWidth=\"{0}\" ", item.popupWidth) : string.Empty;
                        break;
                    case JnpfKeyConst.AUTOCOMPLETE:
                        formControlDesignModel.Placeholder = placeholder;
                        formControlDesignModel.Clearable = item.clearable ? string.Format("allowClear ") : string.Empty;
                        formControlDesignModel.InterfaceId = !string.IsNullOrEmpty(item.interfaceId) ? string.Format("interfaceId=\"{0}\" ", item.interfaceId) : string.Empty;
                        formControlDesignModel.RelationField = !string.IsNullOrEmpty(item.relationField) ? string.Format("relationField=\"{0}\" ", item.relationField) : string.Empty;
                        formControlDesignModel.TemplateJson = string.Format(":templateJson=\"optionsObj.{0}TemplateJson\" ", isSubTable ? string.Format("{0}_{1}", subTableName, item.__vModel__) : item.__vModel__);
                        formControlDesignModel.Total = item.total > 0 ? string.Format(":total=\"{0}\" ", item.total) : string.Empty;
                        break;
                    case JnpfKeyConst.ADDRESS:
                        formControlDesignModel.Placeholder = placeholder;
                        formControlDesignModel.Clearable = item.clearable ? string.Format("allowClear ") : string.Empty;
                        formControlDesignModel.Filterable = item.filterable ? string.Format("showSearch ") : string.Empty;
                        formControlDesignModel.Multiple = item.multiple ? string.Format("multiple ") : string.Empty;
                        formControlDesignModel.Level = string.Format(":level=\"{0}\" ", item.level);
                        break;
                    case JnpfKeyConst.BILLRULE:
                        formControlDesignModel.Placeholder = placeholder;
                        formControlDesignModel.Readonly = item.@readonly ? string.Format("readonly ") : string.Empty;
                        break;
                    case JnpfKeyConst.RELATIONFORM:
                        formControlDesignModel.Placeholder = placeholder;
                        formControlDesignModel.Clearable = item.clearable ? string.Format("allowClear ") : string.Empty;
                        formControlDesignModel.Filterable = item.filterable ? string.Format("showSearch ") : string.Empty;
                        formControlDesignModel.Field = isSubTable ? string.Format(":field=\"'{0}' + index\" ", item.__vModel__) : string.Format("field=\"{0}\" ", item.__vModel__);
                        formControlDesignModel.ModelId = item.modelId;
                        formControlDesignModel.ColumnOptions = item.columnOptions != null ? string.Format(":columnOptions=\"optionsObj.{0}Options\" ", isSubTable ? string.Format("{0}_{1}", subTableName, item.__vModel__) : item.__vModel__) : string.Empty;
                        formControlDesignModel.ExtraOptions = item.extraOptions != null ? string.Format(":extraOptions=\"optionsObj.{0}\" ", isSubTable ? string.Format("{0}_{1}", subTableName, item.__vModel__) : item.__vModel__) : string.Empty;
                        formControlDesignModel.RelationField = !string.IsNullOrEmpty(item.relationField) ? string.Format("relationField=\"{0}\" ", item.relationField) : string.Empty;
                        formControlDesignModel.PopupWidth = !string.IsNullOrEmpty(item.popupWidth) ? string.Format("popupWidth=\"{0}\" ", item.popupWidth) : string.Empty;
                        formControlDesignModel.PageSize = item.pageSize != null ? string.Format(":pageSize=\"{0}\" ", item.pageSize) : string.Empty;
                        formControlDesignModel.HasPage = item.hasPage ? string.Format("hasPage ") : string.Empty;
                        formControlDesignModel.QueryType = item.queryType != null ? string.Format(" :queryType={0} ", item.queryType) : string.Empty;
                        formControlDesignModel.PropsValue = !string.IsNullOrEmpty(item.propsValue) ? string.Format("propsValue=\"{0}\" ", item.propsValue) : string.Empty;
                        break;
                    case JnpfKeyConst.POPUPSELECT:
                        formControlDesignModel.Placeholder = placeholder;
                        formControlDesignModel.TemplateJson = string.Format(":templateJson=\"optionsObj.{0}TemplateJson\" ", isSubTable ? string.Format("{0}_{1}", subTableName, item.__vModel__) : item.__vModel__);
                        formControlDesignModel.Clearable = item.clearable ? string.Format("allowClear ") : string.Empty;
                        formControlDesignModel.Field = isSubTable ? string.Format(":field=\"'{0}' + index\" ", item.__vModel__) : string.Format("field=\"{0}\" ", item.__vModel__);
                        formControlDesignModel.InterfaceId = !string.IsNullOrEmpty(item.interfaceId) ? string.Format("interfaceId=\"{0}\" ", item.interfaceId) : string.Empty;
                        formControlDesignModel.ModelId = item.interfaceId;
                        formControlDesignModel.ColumnOptions = item.columnOptions != null ? string.Format(":columnOptions=\"optionsObj.{0}Options\" ", isSubTable ? string.Format("{0}_{1}", subTableName, item.__vModel__) : item.__vModel__) : string.Empty;
                        formControlDesignModel.ExtraOptions = item.extraOptions != null ? string.Format(":extraOptions=\"optionsObj.{0}\" ", isSubTable ? string.Format("{0}_{1}", subTableName, item.__vModel__) : item.__vModel__) : string.Empty;
                        formControlDesignModel.RelationField = !string.IsNullOrEmpty(item.relationField) ? string.Format("relationField=\"{0}\" ", item.relationField) : string.Empty;
                        formControlDesignModel.PropsValue = !string.IsNullOrEmpty(item.propsValue) ? string.Format("propsValue=\"{0}\" ", item.propsValue) : string.Empty;
                        formControlDesignModel.PageSize = item.pageSize != null ? string.Format(":pageSize=\"{0}\" ", item.pageSize) : string.Empty;
                        formControlDesignModel.PopupType = !string.IsNullOrEmpty(item.popupType) ? string.Format("popupType=\"{0}\" ", item.popupType) : string.Empty;
                        formControlDesignModel.PopupTitle = !string.IsNullOrEmpty(item.popupTitle) ? string.Format("popupTitle=\"{0}\" ", item.popupTitle) : string.Empty;
                        formControlDesignModel.PopupWidth = !string.IsNullOrEmpty(item.popupWidth) ? string.Format("popupWidth=\"{0}\" ", item.popupWidth) : string.Empty;
                        formControlDesignModel.HasPage = item.hasPage ? string.Format("hasPage ") : string.Empty;
                        break;
                    case JnpfKeyConst.RELATIONFORMATTR:
                    case JnpfKeyConst.POPUPATTR:
                        {
                            var relationField = item.relationField.Match("(.+(?=_jnpfTable_))");
                            formControlDesignModel.IsStorage = item.isStorage;
                            formControlDesignModel.RelationField = relationField;
                            formControlDesignModel.ShowField = item.showField;
                        }
                        break;
                    case JnpfKeyConst.CREATEUSER:
                    case JnpfKeyConst.CREATETIME:
                    case JnpfKeyConst.CURRPOSITION:
                        formControlDesignModel.Type = !string.IsNullOrEmpty(item.type) ? string.Format("type=\"{0}\" ", item.type) : string.Empty;
                        formControlDesignModel.Readonly = item.@readonly ? string.Format("readonly ") : string.Empty;
                        break;
                    case JnpfKeyConst.MODIFYUSER:
                    case JnpfKeyConst.MODIFYTIME:
                        formControlDesignModel.Placeholder = placeholder;
                        formControlDesignModel.Readonly = item.@readonly ? string.Format("readonly ") : string.Empty;
                        break;
                    case JnpfKeyConst.CURRORGANIZE:
                        formControlDesignModel.Readonly = item.@readonly ? string.Format("readonly ") : string.Empty;
                        formControlDesignModel.Type = !string.IsNullOrEmpty(item.type) ? string.Format("type=\"{0}\" ", item.type) : string.Empty;
                        formControlDesignModel.ShowLevel = string.Format("showLevel=\"{0}\" ", item.showLevel);
                        break;
                }

                switch (config.jnpfKey)
                {
                    case JnpfKeyConst.COMINPUT:
                    case JnpfKeyConst.TEXTAREA:
                    case JnpfKeyConst.NUMINPUT:
                    case JnpfKeyConst.SWITCH:
                    case JnpfKeyConst.RADIO:
                    case JnpfKeyConst.CHECKBOX:
                    case JnpfKeyConst.SELECT:
                    case JnpfKeyConst.CASCADER:
                    case JnpfKeyConst.DATE:
                    case JnpfKeyConst.TIME:
                    case JnpfKeyConst.UPLOADFZ:
                    case JnpfKeyConst.UPLOADIMG:
                    case JnpfKeyConst.COLORPICKER:
                    case JnpfKeyConst.RATE:
                    case JnpfKeyConst.SLIDER:
                    case JnpfKeyConst.EDITOR:
                    case JnpfKeyConst.COMSELECT:
                    case JnpfKeyConst.DEPSELECT:
                    case JnpfKeyConst.POSSELECT:
                    case JnpfKeyConst.USERSELECT:
                    case JnpfKeyConst.ROLESELECT:
                    case JnpfKeyConst.GROUPSELECT:
                    case JnpfKeyConst.USERSSELECT:
                    case JnpfKeyConst.TREESELECT:
                    case JnpfKeyConst.POPUPTABLESELECT:
                    case JnpfKeyConst.AUTOCOMPLETE:
                    case JnpfKeyConst.ADDRESS:
                    case JnpfKeyConst.BILLRULE:
                    case JnpfKeyConst.RELATIONFORM:
                    case JnpfKeyConst.POPUPSELECT:
                    case JnpfKeyConst.RELATIONFORMATTR:
                    case JnpfKeyConst.POPUPATTR:
                    case JnpfKeyConst.CREATEUSER:
                    case JnpfKeyConst.CREATETIME:
                    case JnpfKeyConst.MODIFYUSER:
                    case JnpfKeyConst.MODIFYTIME:
                    case JnpfKeyConst.CURRORGANIZE:
                    case JnpfKeyConst.CURRPOSITION:
                    case JnpfKeyConst.SIGN:
                    case JnpfKeyConst.SIGNATURE:
                    case JnpfKeyConst.LOCATION:
                        list.Add(formControlDesignModel);
                        break;
                }

                // optionsObj赋值
                switch (config.jnpfKey)
                {
                    case JnpfKeyConst.COMSELECT:
                    case JnpfKeyConst.ROLESELECT:
                    case JnpfKeyConst.GROUPSELECT:
                    case JnpfKeyConst.DEPSELECT:
                    case JnpfKeyConst.POSSELECT:
                    case JnpfKeyConst.USERSELECT:
                    case JnpfKeyConst.USERSSELECT:
                        switch (!string.IsNullOrEmpty(formControlDesignModel.AbleIds))
                        {
                            case true:
                                options.Add(new CodeGenFrontEndFormState()
                                {
                                    Name = string.Format("{0}_AbleIds", isSubTable ? string.Format("{0}_{1}", subTableName, item.__vModel__) : item.__vModel__),
                                    Value = item.ableIds != null ? item.ableIds.ToJsonString() : "[]"
                                });
                                break;
                        }

                        break;
                    case JnpfKeyConst.POPUPTABLESELECT:
                    case JnpfKeyConst.POPUPSELECT:
                        {
                            options.Add(new CodeGenFrontEndFormState()
                            {
                                Name = string.Format("{0}Options", isSubTable ? string.Format("{0}_{1}", subTableName, item.__vModel__) : item.__vModel__),
                                Value = item.columnOptions.ToJsonString(CommonConst.options)
                            });
                            var json = item.templateJson.ToJsonString();
                            options.Add(new CodeGenFrontEndFormState()
                            {
                                Name = string.Format("{0}TemplateJson", isSubTable ? string.Format("{0}_{1}", subTableName, item.__vModel__) : item.__vModel__),
                                Value = json.Equals(string.Empty) ? new List<string>().ToJsonString() : json
                            });
                            if (!isSubTable)
                            {
                                if (item.extraOptions != null)
                                {
                                    extraOptions.Add(new CodeGenFrontEndFormState()
                                    {
                                        Name = string.Format("{0}", isSubTable ? string.Format("{0}_{1}", subTableName, item.__vModel__) : item.__vModel__),
                                        Value = item.extraOptions.ToJsonString(CommonConst.options)
                                    });
                                }
                                interfaceRes.Add(new CodeGenFrontEndFormState()
                                {
                                    Name = string.Format("{0}", isSubTable ? string.Format("{0}_{1}", subTableName, item.__vModel__) : item.__vModel__),
                                    Value = json.Equals(string.Empty) ? new List<string>().ToJsonString() : json
                                });
                            }
                        }

                        break;
                    case JnpfKeyConst.RELATIONFORM:
                        options.Add(new CodeGenFrontEndFormState()
                        {
                            Name = string.Format("{0}Options", isSubTable ? string.Format("{0}_{1}", subTableName, item.__vModel__) : item.__vModel__),
                            Value = item.columnOptions.ToJsonString(CommonConst.options)
                        });
                        if (!isSubTable)
                        {
                            extraOptions.Add(new CodeGenFrontEndFormState()
                            {
                                Name = string.Format("{0}", isSubTable ? string.Format("{0}_{1}", subTableName, item.__vModel__) : item.__vModel__),
                                Value = item.extraOptions.ToJsonString(CommonConst.options)
                            });
                        }
                        break;
                    case JnpfKeyConst.AUTOCOMPLETE:
                        {
                            var json = item.templateJson.ToJsonString();
                            options.Add(new CodeGenFrontEndFormState()
                            {
                                Name = string.Format("{0}TemplateJson", isSubTable ? string.Format("{0}_{1}", subTableName, item.__vModel__) : item.__vModel__),
                                Value = json.Equals(string.Empty) ? new List<string>().ToJsonString() : json
                            });
                        }

                        break;
                    case JnpfKeyConst.CHECKBOX:
                    case JnpfKeyConst.RADIO:
                    case JnpfKeyConst.SELECT:
                        switch (config.dataType)
                        {
                            case "static":
                                options.Add(new CodeGenFrontEndFormState()
                                {
                                    Name = string.Format("{0}Options", isSubTable ? string.Format("{0}_{1}", subTableName, item.__vModel__) : item.__vModel__),
                                    Value = GetCodeGenConvIndexListControlOption(item.options)
                                });
                                options.Add(new CodeGenFrontEndFormState()
                                {
                                    Name = string.Format("{0}Props", isSubTable ? string.Format("{0}_{1}", subTableName, item.__vModel__) : item.__vModel__),
                                    Value = string.Format("{{'label':'{0}','value':'{1}'}}", item.props?.label, item.props?.value)
                                });
                                break;
                            default:
                                if ((!(realisticControl?.IsLinkage).ParseToBool() && isSubTable) || mainLinkageSubTable.Any(it => it.Name.Equals(subTableName) && it.Value.Equals(item.__vModel__)))
                                {
                                    options.Add(new CodeGenFrontEndFormState()
                                    {
                                        Name = string.Format("{0}Options", isSubTable ? string.Format("{0}_{1}", subTableName, item.__vModel__) : item.__vModel__),
                                        Value = new List<string>().ToJsonString()
                                    });
                                }
                                options.Add(new CodeGenFrontEndFormState()
                                {
                                    Name = string.Format("{0}Props", isSubTable ? string.Format("{0}_{1}", subTableName, item.__vModel__) : item.__vModel__),
                                    Value = string.Format("{{'label':'{0}','value':'{1}'}}", item.props?.label, item.props?.value)
                                });

                                // 远端数据与数据字段
                                var dataTypeEnum = (CodeGenFrontEndDataType)System.Enum.Parse(typeof(CodeGenFrontEndDataType), config.dataType);
                                var id = string.Empty;
                                var templateJson = new List<string>().ToJsonString();
                                switch (dataTypeEnum)
                                {
                                    case CodeGenFrontEndDataType.dictionary:
                                        id = config.dictionaryType;
                                        break;
                                    case CodeGenFrontEndDataType.dynamic:
                                        id = config.propsUrl;
                                        templateJson = config.templateJson.ToJsonString();
                                        break;
                                }

                                // 是否需要子表联动时不添加?
                                dataOptions.Add(new CodeGenFrontEndDataOption
                                {
                                    DataType = dataTypeEnum,
                                    Name = isSubTable ? string.Format("{0}_{1}", subTableName, item.__vModel__) : item.__vModel__,
                                    Value = id,
                                    TemplateJson = templateJson,
                                    IsSubTable = isSubTable,
                                    SubTableName = subTableName,
                                    IsLinkage = realisticControl.IsLinkage,
                                    IsSubTableLinkage = !mainLinkageSubTable.Any(it => it.Name.Equals(subTableName) && it.Value.Equals(item.__vModel__)),
                                    IsColumnOption = (columnDesignModel?.Any(it => it.__vModel__.Equals(item.__vModel__) && string.IsNullOrEmpty(it.__config__.parentVModel))).ParseToBool(),
                                });

                                // 子联动子
                                if (realisticControl.IsLinkage && isSubTable && !mainLinkageSubTable.Any(it => it.Name.Equals(subTableName) && it.Value.Equals(item.__vModel__)))
                                {
                                    subTableLinkageOptions.Add(string.Format("get{0}_{1}Options(state.dataForm.{0}.length - 1)", subTableName, item.__vModel__));
                                }

                                // 主联动子
                                if (realisticControl.IsLinkage && isSubTable && mainLinkageSubTable.Any(it => it.Name.Equals(subTableName) && it.Value.Equals(item.__vModel__)))
                                {
                                    subTableLinkageOptions.Add(string.Format("get{0}_{1}Options()", subTableName, item.__vModel__));
                                }

                                break;
                        }
                        break;
                    case JnpfKeyConst.TREESELECT:
                    case JnpfKeyConst.CASCADER:
                        switch (config.dataType)
                        {
                            case "static":
                                options.Add(new CodeGenFrontEndFormState()
                                {
                                    Name = string.Format("{0}Options", isSubTable ? string.Format("{0}_{1}", subTableName, item.__vModel__) : item.__vModel__),
                                    Value = GetCodeGenConvIndexListControlOption(item.options)
                                });
                                options.Add(new CodeGenFrontEndFormState()
                                {
                                    Name = string.Format("{0}Props", isSubTable ? string.Format("{0}_{1}", subTableName, item.__vModel__) : item.__vModel__),
                                    Value = item.props?.ToJsonString(CommonConst.options)
                                });
                                break;
                            default:
                                if (!(realisticControl?.IsLinkage).ParseToBool() && isSubTable)
                                {
                                    options.Add(new CodeGenFrontEndFormState()
                                    {
                                        Name = string.Format("{0}Options", isSubTable ? string.Format("{0}_{1}", subTableName, item.__vModel__) : item.__vModel__),
                                        Value = new List<string>().ToJsonString()
                                    });
                                }

                                options.Add(new CodeGenFrontEndFormState()
                                {
                                    Name = string.Format("{0}Props", isSubTable ? string.Format("{0}_{1}", subTableName, item.__vModel__) : item.__vModel__),
                                    Value = item.props?.ToJsonString(CommonConst.options)
                                });

                                // 远端数据与数据字段
                                var dataTypeEnum = (CodeGenFrontEndDataType)System.Enum.Parse(typeof(CodeGenFrontEndDataType), config.dataType);
                                var id = string.Empty;
                                var templateJson = new List<string>().ToJsonString();
                                switch (dataTypeEnum)
                                {
                                    case CodeGenFrontEndDataType.dictionary:
                                        id = config.dictionaryType;
                                        break;
                                    case CodeGenFrontEndDataType.dynamic:
                                        id = config.propsUrl;
                                        templateJson = config.templateJson.ToJsonString();
                                        break;
                                }

                                dataOptions.Add(new CodeGenFrontEndDataOption
                                {
                                    DataType = dataTypeEnum,
                                    Name = isSubTable ? string.Format("{0}_{1}", subTableName, item.__vModel__) : item.__vModel__,
                                    Value = id,
                                    TemplateJson = templateJson,
                                    IsSubTable = isSubTable,
                                    SubTableName = subTableName,
                                    IsLinkage = realisticControl.IsLinkage,
                                    IsSubTableLinkage = !mainLinkageSubTable.Any(it => it.Name.Equals(subTableName) && it.Value.Equals(item.__vModel__)),
                                    IsColumnOption = (columnDesignModel?.Any(it => it.__vModel__.Equals(item.__vModel__) && string.IsNullOrEmpty(it.__config__.parentVModel))).ParseToBool(),
                                });

                                // 子联动子
                                if (realisticControl.IsLinkage && isSubTable && !mainLinkageSubTable.Any(it => it.Name.Equals(subTableName) && it.Value.Equals(item.__vModel__)))
                                {
                                    subTableLinkageOptions.Add(string.Format("get{0}_{1}Options(state.dataForm.{0}.length - 1)", subTableName, item.__vModel__));
                                }

                                // 主联动子
                                if (realisticControl.IsLinkage && isSubTable && mainLinkageSubTable.Any(it => it.Name.Equals(subTableName) && it.Value.Equals(item.__vModel__)))
                                {
                                    subTableLinkageOptions.Add(string.Format("get{0}_{1}Options()", subTableName, item.__vModel__));
                                }

                                break;
                        }
                        break;
                }

                // dataFrom赋值
                switch (config.jnpfKey)
                {
                    case JnpfKeyConst.CHECKBOX:
                    case JnpfKeyConst.CASCADER:
                    case JnpfKeyConst.UPLOADFZ:
                    case JnpfKeyConst.UPLOADIMG:
                    case JnpfKeyConst.ADDRESS:
                        {
                            var formState = new CodeGenFrontEndFormState();
                            formState.Name = item.__vModel__;
                            formState.Value = config.defaultValue is null ? new List<string>().ToJsonString() : config.defaultValue.ToJsonString();
                            dataForm.Add(formState);
                            if ((columnDesignModel?.Any(it => it.__vModel__.Equals(item.__vModel__))).ParseToBool())
                                inlineEditorDataForm.Add(formState);
                        }

                        break;
                    case JnpfKeyConst.COMSELECT:
                        {
                            var formState = new CodeGenFrontEndFormState();
                            formState.Name = item.__vModel__;
                            switch (config.defaultCurrent)
                            {
                                case true:
                                    formState.Value = string.Format("userInfo.organizeIds.length ? {0}userInfo.organizeIds{1} : {2}", item.multiple ? "[" : string.Empty, item.multiple ? "]" : string.Empty, item.multiple ? new List<string>().ToJsonString() : "\"\"");
                                    break;
                                default:
                                    switch (item.multiple)
                                    {
                                        case true:
                                            formState.Value = config.defaultValue is null ? new List<string>().ToJsonString() : config.defaultValue.ToJsonString();
                                            break;
                                        default:
                                            formState.Value = string.IsNullOrEmpty(config.defaultValue?.ToString()) || config.defaultValue?.ToString() == "[]" ? "undefined" : string.Format("'{0}'", config.defaultValue);
                                            break;
                                    }
                                    break;
                            }

                            dataForm.Add(formState);
                            if ((columnDesignModel?.Any(it => it.__vModel__.Equals(item.__vModel__))).ParseToBool())
                                inlineEditorDataForm.Add(formState);
                        }

                        break;
                    case JnpfKeyConst.POSSELECT:
                    case JnpfKeyConst.ROLESELECT:
                    case JnpfKeyConst.GROUPSELECT:
                    case JnpfKeyConst.DEPSELECT:
                        {
                            var formState = new CodeGenFrontEndFormState();
                            formState.Name = item.__vModel__;
                            switch (config.defaultCurrent)
                            {
                                case true:
                                    switch (config.jnpfKey)
                                    {
                                        case JnpfKeyConst.POSSELECT:
                                            formState.Value = item.multiple ? "userInfo.positionIds.length?userInfo.positionIds:[]" : "userInfo.positionId";
                                            break;
                                        case JnpfKeyConst.ROLESELECT:
                                            formState.Value = item.multiple ? "userInfo.roleIds.length?userInfo.roleIds:[]" : "userInfo.roleIds[0]";
                                            break;
                                        case JnpfKeyConst.GROUPSELECT:
                                            formState.Value = item.multiple ? "userInfo.groupIds.length?userInfo.groupIds:[]" : "userInfo.groupIds[0]";
                                            break;
                                        case JnpfKeyConst.DEPSELECT:
                                            formState.Value = string.Format("userInfo.departmentId ? {0}userInfo.departmentId{1} : {2}", item.multiple ? "[" : string.Empty, item.multiple ? "]" : string.Empty, item.multiple ? new List<string>().ToJsonString() : "\"\"");
                                            break;
                                    }
                                    break;
                                default:
                                    switch (item.multiple)
                                    {
                                        case true:
                                            formState.Value = config.defaultValue is null ? new List<string>().ToJsonString() : config.defaultValue.ToJsonString();
                                            break;
                                        default:
                                            formState.Value = config.defaultValue is null || string.IsNullOrEmpty(config.defaultValue?.ToString()) ? "undefined" : string.Format("'{0}'", config.defaultValue);
                                            break;
                                    }

                                    break;
                            }

                            dataForm.Add(formState);
                            if ((columnDesignModel?.Any(it => it.__vModel__.Equals(item.__vModel__))).ParseToBool())
                                inlineEditorDataForm.Add(formState);
                        }
                        break;
                    case JnpfKeyConst.USERSSELECT:
                    case JnpfKeyConst.USERSELECT:
                        {
                            var formState = new CodeGenFrontEndFormState();
                            formState.Name = item.__vModel__;
                            switch (config.defaultCurrent)
                            {
                                case true:
                                    formState.Value = string.Format("userInfo.userId ? {0}userInfo.userId{1} : {2}", item.multiple ? "[" : string.Empty, item.multiple ? "]" : string.Empty, item.multiple ? new List<string>().ToJsonString() : "\"\"");
                                    break;
                                default:
                                    switch (item.multiple)
                                    {
                                        case true:
                                            formState.Value = config.defaultValue is null ? new List<string>().ToJsonString() : config.defaultValue.ToJsonString();
                                            break;
                                        default:
                                            formState.Value = config.defaultValue is null || string.IsNullOrEmpty(config.defaultValue?.ToString()) ? "undefined" : string.Format("'{0}'", config.defaultValue);
                                            break;
                                    }

                                    break;
                            }

                            dataForm.Add(formState);
                            if ((columnDesignModel?.Any(it => it.__vModel__.Equals(item.__vModel__))).ParseToBool())
                                inlineEditorDataForm.Add(formState);
                        }

                        break;
                    case JnpfKeyConst.LOCATION:
                        {
                            var formState = new CodeGenFrontEndFormState();
                            formState.Name = item.__vModel__;
                            formState.Value = item.__config__.defaultValue != null ? item.__config__.defaultValue.ToJsonString() : "undefined";
                            dataForm.Add(formState);
                            if ((columnDesignModel?.Any(it => it.__vModel__.Equals(item.__vModel__))).ParseToBool())
                                inlineEditorDataForm.Add(formState);
                        }

                        break;
                    case JnpfKeyConst.SIGNATURE:
                    case JnpfKeyConst.SIGN:
                        {
                            var formState = new CodeGenFrontEndFormState();
                            formState.Name = item.__vModel__;
                            switch (config.defaultCurrent)
                            {
                                case true:
                                    formState.Value = string.Format("userInfo.signImg || ''");
                                    break;
                                default:
                                    switch (item.multiple)
                                    {
                                        case true:
                                            formState.Value = config.defaultValue is null ? new List<string>().ToJsonString() : config.defaultValue.ToJsonString();
                                            break;
                                        default:
                                            formState.Value = config.defaultValue is null || string.IsNullOrEmpty(config.defaultValue?.ToString()) ? "undefined" : string.Format("'{0}'", config.defaultValue);
                                            break;
                                    }

                                    break;
                            }

                            dataForm.Add(formState);
                            if ((columnDesignModel?.Any(it => it.__vModel__.Equals(item.__vModel__))).ParseToBool())
                                inlineEditorDataForm.Add(formState);
                        }

                        break;
                    case JnpfKeyConst.SELECT:
                    case JnpfKeyConst.TREESELECT:
                    case JnpfKeyConst.POPUPTABLESELECT:
                        {
                            var formState = new CodeGenFrontEndFormState();
                            formState.Name = item.__vModel__;
                            switch (item.multiple)
                            {
                                case true:
                                    formState.Value = config.defaultValue is null ? new List<string>().ToJsonString() : config.defaultValue.ToJsonString();
                                    break;
                                default:
                                    formState.Value = config.defaultValue is null || string.IsNullOrEmpty(config.defaultValue?.ToString()) ? "undefined" : string.Format("'{0}'", config.defaultValue);
                                    break;
                            }

                            dataForm.Add(formState);
                            if ((columnDesignModel?.Any(it => it.__vModel__.Equals(item.__vModel__))).ParseToBool())
                                inlineEditorDataForm.Add(formState);
                        }

                        break;
                    case JnpfKeyConst.NUMINPUT:
                    case JnpfKeyConst.RATE:
                    case JnpfKeyConst.SLIDER:
                        {
                            var formState = new CodeGenFrontEndFormState();
                            formState.Name = item.__vModel__;
                            formState.Value = config.defaultValue is null ? "undefined" : config.defaultValue.ParseToDecimal();
                            dataForm.Add(formState);
                            if ((columnDesignModel?.Any(it => it.__vModel__.Equals(item.__vModel__))).ParseToBool())
                                inlineEditorDataForm.Add(formState);
                        }

                        break;
                    case JnpfKeyConst.DATE:
                        {
                            var formState = new CodeGenFrontEndFormState();
                            formState.Name = item.__vModel__;
                            switch (config.defaultCurrent)
                            {
                                case true:
                                    formState.Value = string.Format("dayjs(new Date()).startOf(getDateTimeUnit('{0}')).valueOf()", item.format);
                                    break;
                                default:
                                    formState.Value = config.defaultValue is null ? "undefined" : config.defaultValue.ParseToLong();
                                    break;
                            }

                            dataForm.Add(formState);
                            if ((columnDesignModel?.Any(it => it.__vModel__.Equals(item.__vModel__))).ParseToBool())
                                inlineEditorDataForm.Add(formState);
                        }

                        break;
                    case JnpfKeyConst.TIME:
                        {
                            var formState = new CodeGenFrontEndFormState();
                            formState.Name = item.__vModel__;
                            switch (config.defaultCurrent)
                            {
                                case true:
                                    formState.Value = string.Format("dayjs().format('{0}')", item.format);
                                    break;
                                default:
                                    formState.Value = config.defaultValue is null || string.IsNullOrEmpty(config.defaultValue?.ToString()) ? "undefined" : string.Format("'{0}'", config.defaultValue);
                                    break;
                            }

                            dataForm.Add(formState);
                            if ((columnDesignModel?.Any(it => it.__vModel__.Equals(item.__vModel__))).ParseToBool())
                                inlineEditorDataForm.Add(formState);
                        }

                        break;

                    case JnpfKeyConst.COMINPUT:
                    case JnpfKeyConst.TEXTAREA:
                    case JnpfKeyConst.RADIO:
                    case JnpfKeyConst.COLORPICKER:
                    case JnpfKeyConst.EDITOR:
                    case JnpfKeyConst.AUTOCOMPLETE:
                    case JnpfKeyConst.RELATIONFORM:
                    case JnpfKeyConst.POPUPSELECT:
                        {
                            var formState = new CodeGenFrontEndFormState();
                            formState.Name = item.__vModel__;
                            formState.Value = config.defaultValue is null || string.IsNullOrEmpty(config.defaultValue?.ToString()) ? "undefined" : string.Format("'{0}'", config.defaultValue);
                            dataForm.Add(formState);
                            if ((columnDesignModel?.Any(it => it.__vModel__.Equals(item.__vModel__))).ParseToBool())
                                inlineEditorDataForm.Add(formState);
                        }
                        break;
                    case JnpfKeyConst.RELATIONFORMATTR:
                    case JnpfKeyConst.POPUPATTR:
                        if (item.isStorage == 1)
                        {
                            var formState = new CodeGenFrontEndFormState();
                            formState.Name = item.__vModel__;
                            formState.Value = config.defaultValue is null || string.IsNullOrEmpty(config.defaultValue?.ToString()) ? "undefined" : string.Format("'{0}'", config.defaultValue);
                            dataForm.Add(formState);
                            if ((columnDesignModel?.Any(it => it.__vModel__.Equals(item.__vModel__))).ParseToBool())
                                inlineEditorDataForm.Add(formState);
                        }

                        break;
                    case JnpfKeyConst.SWITCH:
                        {
                            dataForm.Add(new CodeGenFrontEndFormState()
                            {
                                Name = item.__vModel__,
                                Value = config.defaultValue.ParseToInt()
                            });
                            if ((columnDesignModel?.Any(it => it.__vModel__.Equals(item.__vModel__))).ParseToBool())
                            {
                                inlineEditorDataForm.Add(new CodeGenFrontEndFormState()
                                {
                                    Name = item.__vModel__,
                                    Value = config.defaultValue.ParseToInt()
                                });
                            }
                        }

                        break;
                    case JnpfKeyConst.TABLE:
                        {
                            dataForm.Add(new CodeGenFrontEndFormState()
                            {
                                Name = item.__vModel__,
                                Value = new List<string>().ToJsonString()
                            });
                            if ((columnDesignModel?.Any(it => it.__vModel__.Equals(item.__vModel__))).ParseToBool())
                            {
                                inlineEditorDataForm.Add(new CodeGenFrontEndFormState()
                                {
                                    Name = item.__vModel__,
                                    Value = new List<string>().ToJsonString()
                                });
                            }
                        }

                        break;
                    case JnpfKeyConst.BILLRULE:
                    case JnpfKeyConst.CREATEUSER:
                    case JnpfKeyConst.CREATETIME:
                    case JnpfKeyConst.MODIFYUSER:
                    case JnpfKeyConst.MODIFYTIME:
                    case JnpfKeyConst.CURRORGANIZE:
                    case JnpfKeyConst.CURRPOSITION:
                        {
                            dataForm.Add(new CodeGenFrontEndFormState()
                            {
                                Name = item.__vModel__,
                                Value = "undefined"
                            });
                            if ((columnDesignModel?.Any(it => it.__vModel__.Equals(item.__vModel__))).ParseToBool())
                            {
                                inlineEditorDataForm.Add(new CodeGenFrontEndFormState()
                                {
                                    Name = item.__vModel__,
                                    Value = "undefined"
                                });
                            }
                        }

                        break;
                }

                // 子表控件内部联动时 添加
                switch (config.jnpfKey)
                {
                    case JnpfKeyConst.SELECT:
                    case JnpfKeyConst.RADIO:
                    case JnpfKeyConst.CHECKBOX:
                    case JnpfKeyConst.TREESELECT:
                    case JnpfKeyConst.CASCADER:
                        if ((realisticControl?.IsLinkage).ParseToBool() && isSubTable && !mainLinkageSubTable.Any(it => it.Name.Equals(subTableName) && it.Value.Equals(item.__vModel__)))
                        {
                            var formState = new CodeGenFrontEndFormState();
                            formState.Name = string.Format("{0}_{1}Options", subTableName, item.__vModel__);
                            formState.Value = new List<string>().ToJsonString();
                            dataForm.Add(formState);
                        }

                        break;
                }
            }
        }
        if (isSubTable)
        {
            model.SubTableHeader = subTableHeader;
            model.SubTableLinkageOptions = subTableLinkageOptions;
        }
        model.FormControlDesign = list;
        model.Collapses = collapses;
        model.HasCollapse = collapses?.Count > 0 ? true : false;
        model.DataRules = dataRules;
        model.Options = options;
        model.HasOptions = options.Any() || extraOptions.Any() ? true : false;
        model.ExtraOptions = extraOptions;
        model.InterfaceRes = interfaceRes;
        model.DataForm = dataForm;
        model.InlineEditorDataForm = inlineEditorDataForm;
        model.HasSpecialDate = hasSpecialDate;
        model.HasSpecialTime = hasSpecialTime;
        model.DataOptions = dataOptions;
        model.HasSubTable = subTableDesign?.Count > 0 ? true : false;
        model.HasSubTableSummary = (subTableDesign?.Any(it => it.HasSummary)).ParseToBool();
        foreach (var linkageItem in linkage)
        {
            linkageItem.LinkageRelationship = linkageItem.LinkageRelationship.DistinctBy(x => x.field).ToList();
        }
        model.Linkage = linkage;
        model.SubTableDesign = subTableDesign;
        model.SubTableControls = subTableControls;
        model.HasSubTableDataTransfer = hasSubTableDataTransfer;
        model.HasDictionary = dataOptions.Any(it => it.DataType.Equals(CodeGenFrontEndDataType.dictionary));
        model.HasDynamic = dataOptions.Any(it => it.DataType.Equals(CodeGenFrontEndDataType.dynamic));
        return model;
    }

    /// <summary>
    /// 特殊时间属性组装.
    /// </summary>
    /// <param name="field">配置模型.</param>
    /// <param name="fieldList">全控件列表.</param>
    /// <param name="type">类型(1-startTime,2-endTime).</param>
    /// <param name="isMainTable">是否主副表.</param>
    /// <returns></returns>
    private static string SpecialTimeAttributeAssembly(FieldsModel field, List<FieldsModel> fieldList, int type, bool isMainTable)
    {
        var time = string.Empty;
        var config = field.__config__;
        switch (config.jnpfKey)
        {
            case JnpfKeyConst.DATE:
                switch (type)
                {
                    case 1:
                        switch (config.startTimeRule)
                        {
                            case true:
                                var relationField = SpecialAttributeAssociatedFields(config.startRelationField, fieldList);
                                time = string.Format(":startTime=\"getRelationDate(true, {0}, {1}, '{2}', '{3}')\" ", config.startTimeType, config.startTimeTarget, config.startTimeValue, config.startTimeType.Equals(2) ? relationField : string.Empty);
                                break;
                        }
                        break;
                    case 2:
                        switch (config.endTimeRule)
                        {
                            case true:
                                var relationField = SpecialAttributeAssociatedFields(config.endRelationField, fieldList);
                                time = string.Format(":endTime=\"getRelationDate(true, {0}, {1}, '{2}', '{3}')\" ", config.endTimeType, config.endTimeTarget, config.endTimeValue, config.endTimeType.Equals(2) ? relationField : string.Empty);
                                break;
                        }
                        break;
                }
                break;
            case JnpfKeyConst.TIME:
                switch (type)
                {
                    case 1:
                        switch (config.startTimeRule)
                        {
                            case true:
                                var relationField = SpecialAttributeAssociatedFields(config.startRelationField, fieldList);
                                time = string.Format(":startTime=\"getRelationTime(true, {0}, {1}, '{2}', '{3}', '{4}')\" ", config.startTimeType, config.startTimeTarget, config.startTimeValue, field.format, config.startTimeType.Equals(2) ? relationField : string.Empty);
                                break;
                        }
                        break;
                    case 2:
                        switch (config.endTimeRule)
                        {
                            case true:
                                var relationField = SpecialAttributeAssociatedFields(config.endRelationField, fieldList);
                                time = string.Format(":endTime=\"getRelationTime(true,{0}, {1}, '{2}', '{3}', '{4}')\" ", config.endTimeType, config.endTimeTarget, config.endTimeValue, field.format, config.endTimeType.Equals(2) ? relationField : string.Empty);
                                break;
                        }
                        break;
                }
                break;
        }
        return time;
    }

    /// <summary>
    /// 特殊属性关联字段.
    /// </summary>
    /// <param name="relationField"></param>
    /// <param name="fieldList"></param>
    /// <returns></returns>
    private static string SpecialAttributeAssociatedFields(string relationField, List<FieldsModel> fieldList)
    {
        var completeResults = string.Empty;
        switch (fieldList.Any(it => it.__vModel__.Equals(relationField)))
        {
            case true:
                completeResults = string.Format("dataForm.{0}", relationField);
                break;
            case false:
                if (relationField.IsMatch(@"tableField\d{3}-"))
                {
                    var subTable = relationField.Matches(@"tableField\d{3}-").FirstOrDefault().Replace("-", "");
                    relationField = relationField.ReplaceRegex("tableField\\d{3}-", "");
                    switch (fieldList.Any(it => it.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE) && it.__config__.children.Any(x => x.__vModel__.Equals(relationField))))
                    {
                        case true:
                            completeResults = string.Format("dataForm.{0}[scope.$index].{1}", subTable, relationField);
                            break;
                    }
                }
                break;
        }
        return completeResults;
    }

    /// <summary>
    /// 表单默认值控件列表.
    /// </summary>
    /// <param name="fieldList">组件列表.</param>
    /// <param name="searchField">查询字段.</param>
    /// <param name="subTableName">子表名称.</param>
    /// <param name="isMain">是否主表.</param>
    /// <returns></returns>
    public static DefaultFormControlModel DefaultFormControlList(List<FieldsModel> fieldList, List<IndexSearchFieldModel> searchField, string subTableName = null, bool isMain = true)
    {
        DefaultFormControlModel model = new DefaultFormControlModel();
        model.DateField = new List<DefaultTimeControl>();
        model.TimeField = new List<DefaultTimeControl>();
        model.SignField = new List<DefaultSignControl>();
        model.ComSelectList = new List<DefaultComSelectControl>();
        model.DepSelectList = new List<DefaultDepSelectControl>();
        model.UserSelectList = new List<DefaultDepSelectControl>();
        model.UsersSelectList = new List<DefaultDepSelectControl>();
        model.RoleSelectList = new List<DefaultDepSelectControl>();
        model.PosSelectList = new List<DefaultDepSelectControl>();
        model.GroupsSelectList = new List<DefaultDepSelectControl>();
        model.SubTabelDefault = new List<DefaultFormControlModel>();

        // 获取表单内存在默认值控件
        foreach (var item in fieldList)
        {
            var config = item.__config__;
            var search = new IndexSearchFieldModel();
            switch (isMain && !config.jnpfKey.Equals(JnpfKeyConst.TABLE))
            {
                case false:
                    switch (!config.jnpfKey.Equals(JnpfKeyConst.TABLE))
                    {
                        case true:
                            search = searchField?.Find(it => it.id.Equals(string.Format("{0}-{1}", subTableName, item.__vModel__)));
                            break;
                    }
                    break;
                default:
                    search = searchField?.Find(it => it.id.Equals(string.Format("{0}", item.__vModel__)));
                    break;
            }

            // 未作为查询条件
            if (search == null)
            {
                search = new IndexSearchFieldModel();
                search.searchMultiple = false;
            }
            switch (config.defaultCurrent)
            {
                case true:
                    switch (config.jnpfKey)
                    {
                        case JnpfKeyConst.TABLE:
                            model.SubTabelDefault.Add(DefaultFormControlList(item.__config__.children, searchField, item.__vModel__, false));
                            break;
                        case JnpfKeyConst.TIME:
                            model.TimeField.Add(new DefaultTimeControl
                            {
                                Field = item.__vModel__,
                                Format = item.format
                            });
                            break;
                        case JnpfKeyConst.DATE:
                            model.DateField.Add(new DefaultTimeControl
                            {
                                Field = item.__vModel__,
                                Format = item.format
                            });
                            break;
                        case JnpfKeyConst.SIGNATURE:
                        case JnpfKeyConst.SIGN:
                            model.SignField.Add(new DefaultSignControl
                            {
                                Field = item.__vModel__
                            });
                            break;
                        case JnpfKeyConst.COMSELECT:
                            model.ComSelectList.Add(new DefaultComSelectControl()
                            {
                                IsMultiple = item.multiple,
                                selectType = item.selectType,
                                IsSearchMultiple = (bool)search?.searchMultiple,
                                Field = item.__vModel__,
                                ableIds = item.ableIds.ToJsonString(),
                            });
                            break;
                        case JnpfKeyConst.DEPSELECT:
                            model.DepSelectList.Add(new DefaultDepSelectControl()
                            {
                                IsMultiple = item.multiple,
                                selectType = item.selectType,
                                IsSearchMultiple = (bool)search?.searchMultiple,
                                Field = item.__vModel__,
                                ableIds = item.ableIds.ToJsonString(),
                            });
                            break;
                        case JnpfKeyConst.USERSELECT:
                            model.UserSelectList.Add(new DefaultDepSelectControl()
                            {
                                IsMultiple = item.multiple,
                                selectType = item.selectType,
                                IsSearchMultiple = (bool)search?.searchMultiple,
                                Field = item.__vModel__,
                                ableIds = item.ableIds.ToJsonString(),
                            });
                            break;
                        case JnpfKeyConst.USERSSELECT:
                            model.UsersSelectList.Add(new DefaultDepSelectControl()
                            {
                                IsMultiple = item.multiple,
                                selectType = item.selectType,
                                IsSearchMultiple = (bool)search?.searchMultiple,
                                Field = item.__vModel__,
                                ableIds = item.ableIds.ToJsonString(),
                            });
                            break;
                        case JnpfKeyConst.ROLESELECT:
                            model.RoleSelectList.Add(new DefaultDepSelectControl()
                            {
                                IsMultiple = item.multiple,
                                selectType = item.selectType,
                                IsSearchMultiple = (bool)search?.searchMultiple,
                                Field = item.__vModel__,
                                ableIds = item.ableIds.ToJsonString(),
                            });
                            break;
                        case JnpfKeyConst.POSSELECT:
                            model.PosSelectList.Add(new DefaultDepSelectControl()
                            {
                                IsMultiple = item.multiple,
                                selectType = item.selectType,
                                IsSearchMultiple = (bool)search?.searchMultiple,
                                Field = item.__vModel__,
                                ableIds = item.ableIds.ToJsonString(),
                            });
                            break;
                        case JnpfKeyConst.GROUPSELECT:
                            model.GroupsSelectList.Add(new DefaultDepSelectControl()
                            {
                                IsMultiple = item.multiple,
                                selectType = item.selectType,
                                IsSearchMultiple = (bool)search?.searchMultiple,
                                Field = item.__vModel__,
                                ableIds = item.ableIds.ToJsonString(),
                            });
                            break;
                    }
                    break;
            }
        }

        switch (isMain)
        {
            case false:
                model.SubTableName = subTableName;
                model.IsExistTime = model.TimeField.Any();
                model.IsExistDate = model.DateField.Any();
                model.IsSignField = model.SignField.Any();
                model.IsExistComSelect = model.ComSelectList.Any();
                model.IsExistDepSelect = model.DepSelectList.Any();
                model.IsExistUserSelect = model.UserSelectList.Any();
                model.IsExistUsersSelect = model.UsersSelectList.Any();
                model.IsExistRoleSelect = model.RoleSelectList.Any();
                model.IsExistPosSelect = model.PosSelectList.Any();
                model.IsExistGroupsSelect = model.GroupsSelectList.Any();
                break;
            default:
                model.IsExistTime = model.TimeField.Any() || model.SubTabelDefault.Any(it => it.TimeField.Any());
                model.IsExistDate = model.DateField.Any() || model.SubTabelDefault.Any(it => it.DateField.Any());
                model.IsSignField = model.SignField.Any() || model.SubTabelDefault.Any(it => it.SignField.Any());
                model.IsExistComSelect = model.ComSelectList.Any() || model.SubTabelDefault.Any(it => it.ComSelectList.Any());
                model.IsExistDepSelect = model.DepSelectList.Any() || model.SubTabelDefault.Any(it => it.DepSelectList.Any());
                model.IsExistUserSelect = model.UserSelectList.Any() || model.SubTabelDefault.Any(it => it.UserSelectList.Any());
                model.IsExistUsersSelect = model.UsersSelectList.Any() || model.SubTabelDefault.Any(it => it.UsersSelectList.Any());
                model.IsExistRoleSelect = model.RoleSelectList.Any() || model.SubTabelDefault.Any(it => it.RoleSelectList.Any());
                model.IsExistPosSelect = model.PosSelectList.Any() || model.SubTabelDefault.Any(it => it.PosSelectList.Any());
                model.IsExistGroupsSelect = model.GroupsSelectList.Any() || model.SubTabelDefault.Any(it => it.GroupsSelectList.Any());
                model.IsExistSubTable = model.SubTabelDefault.Count > 0 ? true : false;
                break;
        }

        return model;
    }

    /// <summary>
    /// 查询列表默认值.
    /// </summary>
    /// <param name="searchField">查询字段.</param>
    /// <returns></returns>
    public static List<DefaultSearchFieldModel> DefaultSearchFieldList(List<IndexSearchFieldModel>? searchField)
    {
        List<DefaultSearchFieldModel> model = new List<DefaultSearchFieldModel>();

        if (searchField != null)
            foreach (var item in searchField)
            {
                switch (item.value is not null)
                {
                    case true:
                        switch (item.value.GetType().FullName.IndexOf("JArray") > 0)
                        {
                            case true:
                                switch (item.value.ToObject<List<object>>().Count > 0)
                                {
                                    case true:
                                        model.Add(new DefaultSearchFieldModel
                                        {
                                            Field = string.Format("{0}{1}", string.IsNullOrEmpty(item.__config__.parentVModel) ? string.Empty : item.__config__.parentVModel + "_", item.__vModel__),
                                            Value = item.value.ToJsonString(),
                                        });
                                        break;
                                }
                                break;
                            default:
                                model.Add(new DefaultSearchFieldModel
                                {
                                    Field = string.Format("{0}{1}", string.IsNullOrEmpty(item.__config__.parentVModel) ? string.Empty : item.__config__.parentVModel + "_", item.__vModel__),
                                    Value = item.value.ToJsonString(),
                                });
                                break;
                        }
                        break;
                }
            }

        // 获取表单存在默认值控件
        return model;
    }

    /// <summary>
    /// 判断控件开启特殊属性.
    /// </summary>
    /// <param name="fieldList">控件列表.</param>
    /// <param name="jnpfKey">控件Key.</param>
    /// <returns></returns>
    public static bool DetermineWhetherTheControlHasEnabledSpecialAttributes(List<FieldsModel> fieldList, string jnpfKey)
    {
        var numberOfControls = 0;
        foreach (var item in fieldList)
        {
            var config = item.__config__;
            switch (config.jnpfKey)
            {
                case JnpfKeyConst.TABLE:
                    numberOfControls += DetermineWhetherSpecialAttributesAreEnabledForControlsWithinASubtable(config.children, jnpfKey);
                    break;
                case JnpfKeyConst.DATE:
                case JnpfKeyConst.TIME:
                    if (config.jnpfKey.Equals(jnpfKey) & (config.startTimeRule || config.endTimeRule))
                    {
                        numberOfControls++;
                    }
                    break;
            }
        }
        return numberOfControls > 0 ? true : false;
    }

    /// <summary>
    /// 子表指定日期格式集合.
    /// </summary>
    /// <param name="fieldList">子表控件.</param>
    /// <returns></returns>
    public static CodeGenSpecifyDateFormatSetModel CodeGenSpecifyDateFormatSetModel(FieldsModel fieldList)
    {
        var config = fieldList.__config__;
        var result = new CodeGenSpecifyDateFormatSetModel
        {
            Field = fieldList.__vModel__,
            Children = new List<CodeGenSpecifyDateFormatSetModel>(),
        };
        foreach (var item in config.children)
        {
            var childrenConfig = item.__config__;
            switch (childrenConfig.jnpfKey)
            {
                case JnpfKeyConst.DATE:
                    switch (item.format)
                    {
                        case "yyyy":
                        case "yyyy-MM":
                        case "yyyy-MM-dd":
                        case "yyyy-MM-dd HH:mm":
                            result.Children.Add(new CodeGenSpecifyDateFormatSetModel
                            {
                                Field = item.__vModel__,
                                Format = item.format,
                            });
                            break;
                    }
                    break;
            }
        }
        if (result.Children.Count == 0) return null;
        return result;
    }

    private static int DetermineWhetherSpecialAttributesAreEnabledForControlsWithinASubtable(List<FieldsModel> fieldList, string jnpfKey)
    {
        var numberOfControls = 0;
        foreach (var item in fieldList)
        {
            var config = item.__config__;
            switch (config.jnpfKey)
            {
                case JnpfKeyConst.DATE:
                case JnpfKeyConst.TIME:
                    if (config.jnpfKey.Equals(jnpfKey) & (config.startTimeRule || config.endTimeRule))
                    {
                        numberOfControls++;
                    }
                    break;
            }
        }
        return numberOfControls;
    }

    /// <summary>
    /// 表单控件选项配置.
    /// </summary>
    /// <param name="fieldList">组件列表.</param>
    /// <param name="realisticControls">真实控件.</param>
    /// <param name="columnDesignModel">列表设计.</param>
    /// <param name="type">1-Web设计,2-App设计,3-流程表单,4-Web表单,5-App表单.</param>
    /// <param name="isMain">是否主循环.</param>
    /// <returns></returns>
    public static List<CodeGenConvIndexListControlOptionDesign> FormControlProps(List<FieldsModel> fieldList, List<FieldsModel> realisticControls, ColumnDesignModel columnDesignModel, int type, bool isMain = false)
    {
        if (isMain) active = 1;
        List<CodeGenConvIndexListControlOptionDesign> list = new List<CodeGenConvIndexListControlOptionDesign>();
        foreach (var item in fieldList)
        {
            var config = item.__config__;
            switch (config.jnpfKey)
            {
                case JnpfKeyConst.CARD:
                case JnpfKeyConst.ROW:
                case JnpfKeyConst.TABLEGRID:
                case JnpfKeyConst.TABLEGRIDTR:
                case JnpfKeyConst.TABLEGRIDTD:
                    {
                        list.AddRange(FormControlProps(config.children, realisticControls, columnDesignModel, type));
                    }

                    break;
                case JnpfKeyConst.TABLE:
                    {
                        var childrenRealisticControls = realisticControls.Find(it => it.__vModel__.Equals(item.__vModel__) && it.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE)).__config__.children;
                        foreach (var children in config.children)
                        {
                            var childrenConfig = children.__config__;
                            var columnDesign = columnDesignModel.searchList?.Find(it => it.id.Equals(string.Format("{0}-{1}", item.__vModel__, children.__vModel__)));
                            switch (childrenConfig.jnpfKey)
                            {
                                case JnpfKeyConst.COMSELECT:
                                case JnpfKeyConst.ROLESELECT:
                                case JnpfKeyConst.GROUPSELECT:
                                case JnpfKeyConst.DEPSELECT:
                                case JnpfKeyConst.POSSELECT:
                                case JnpfKeyConst.USERSELECT:
                                case JnpfKeyConst.USERSSELECT:
                                    if (children.selectType != null && children.selectType == "custom")
                                    {
                                        list.Add(new CodeGenConvIndexListControlOptionDesign()
                                        {
                                            jnpfKey = childrenConfig.jnpfKey,
                                            IsStatic = true,
                                            IsIndex = false,
                                            IsProps = false,
                                            Content = string.Format("{0}_{1}_AbleIds:{2},", item.__vModel__, children.__vModel__, children.ableIds.ToJsonString()),
                                        });
                                    }
                                    break;
                                case JnpfKeyConst.SELECT:
                                    {
                                        var realisticControl = childrenRealisticControls.Find(it => it.__vModel__.Equals(children.__vModel__) && it.__config__.jnpfKey.Equals(childrenConfig.jnpfKey));
                                        realisticControl ??= new FieldsModel();
                                        switch (childrenConfig.dataType)
                                        {
                                            // 静态数据
                                            case "static":
                                                list.Add(new CodeGenConvIndexListControlOptionDesign()
                                                {
                                                    jnpfKey = childrenConfig.jnpfKey,
                                                    Name = string.Format("{0}_{1}", item.__vModel__, children.__vModel__),
                                                    DictionaryType = childrenConfig.dataType == "dictionary" ? childrenConfig.dictionaryType : (childrenConfig.dataType == "dynamic" ? childrenConfig.propsUrl : null),
                                                    DataType = childrenConfig.dataType,
                                                    IsStatic = true,
                                                    IsIndex = true,
                                                    IsProps = true,
                                                    Props = string.Format("{{'label':'{0}','value':'{1}'}}", children.props?.label, children.props?.value),
                                                    IsChildren = true,
                                                    Content = GetCodeGenConvIndexListControlOption(string.Format("{0}_{1}", item.__vModel__, children.__vModel__), children.options),
                                                    QueryProps = children.props.ToJsonString(CommonConst.options),
                                                });
                                                break;
                                            default:
                                                list.Add(new CodeGenConvIndexListControlOptionDesign()
                                                {
                                                    jnpfKey = childrenConfig.jnpfKey,
                                                    Name = string.Format("{0}_{1}", item.__vModel__, children.__vModel__),
                                                    OptionsName = string.Format("dataForm.{0}[i].{1}", item.__vModel__, children.__vModel__),
                                                    DictionaryType = childrenConfig.dataType == "dictionary" ? childrenConfig.dictionaryType : (childrenConfig.dataType == "dynamic" ? childrenConfig.propsUrl : null),
                                                    DataType = childrenConfig.dataType,
                                                    IsStatic = false,
                                                    IsIndex = true,
                                                    IsProps = true,
                                                    Props = string.Format("{{'label':'{0}','value':'{1}'}}", children.props?.label, children.props?.value),
                                                    IsChildren = true,
                                                    Content = string.Format("{0}Options : [],", string.Format("{0}_{1}", item.__vModel__, children.__vModel__)),
                                                    QueryProps = children.props.ToJsonString(CommonConst.options),
                                                    IsLinkage = realisticControl.IsLinkage,
                                                    TemplateJson = childrenConfig.dataType == "dynamic" ? childrenConfig.templateJson.ToJsonString() : "[]"
                                                });
                                                break;
                                        }
                                    }

                                    break;
                                case JnpfKeyConst.TREESELECT:
                                case JnpfKeyConst.CASCADER:
                                    {
                                        var realisticControl = childrenRealisticControls.Find(it => it.__vModel__.Equals(children.__vModel__) && it.__config__.jnpfKey.Equals(childrenConfig.jnpfKey));
                                        realisticControl ??= new FieldsModel();
                                        switch (childrenConfig.dataType)
                                        {
                                            case "static":
                                                list.Add(new CodeGenConvIndexListControlOptionDesign()
                                                {
                                                    jnpfKey = childrenConfig.jnpfKey,
                                                    Name = string.Format("{0}_{1}", item.__vModel__, children.__vModel__),
                                                    DictionaryType = childrenConfig.dataType == "dictionary" ? childrenConfig.dictionaryType : (childrenConfig.dataType == "dynamic" ? childrenConfig.propsUrl : null),
                                                    DataType = childrenConfig.dataType,
                                                    IsStatic = true,
                                                    IsIndex = columnDesign != null ? true : false,
                                                    IsProps = true,
                                                    IsChildren = true,
                                                    Props = children.props?.ToJsonString(CommonConst.options),
                                                    QueryProps = children.props.ToJsonString(CommonConst.options),
                                                    Content = GetCodeGenConvIndexListControlOption(string.Format("{0}_{1}", item.__vModel__, children.__vModel__), children.options.ToObject<List<Dictionary<string, object>>>())
                                                });
                                                break;
                                            default:
                                                list.Add(new CodeGenConvIndexListControlOptionDesign()
                                                {
                                                    jnpfKey = childrenConfig.jnpfKey,
                                                    Name = string.Format("{0}_{1}", item.__vModel__, children.__vModel__),
                                                    OptionsName = string.Format("dataForm.{0}[i].{1}", item.__vModel__, children.__vModel__),
                                                    DictionaryType = childrenConfig.dataType == "dictionary" ? childrenConfig.dictionaryType : (childrenConfig.dataType == "dynamic" ? childrenConfig.propsUrl : null),
                                                    DataType = childrenConfig.dataType,
                                                    IsStatic = false,
                                                    IsIndex = columnDesign != null ? true : false,
                                                    IsProps = true,
                                                    IsChildren = true,
                                                    Props = children.props?.ToJsonString(CommonConst.options),
                                                    QueryProps = children.props.ToJsonString(CommonConst.options),
                                                    Content = string.Format("{0}Options: [],", string.Format("{0}_{1}", item.__vModel__, children.__vModel__)),
                                                    IsLinkage = realisticControl.IsLinkage,
                                                    TemplateJson = childrenConfig.dataType == "dynamic" ? childrenConfig.templateJson.ToJsonString() : "[]"
                                                });
                                                break;
                                        }
                                    }

                                    break;
                                case JnpfKeyConst.POPUPTABLESELECT:
                                case JnpfKeyConst.POPUPSELECT:
                                case JnpfKeyConst.AUTOCOMPLETE:
                                    {
                                        var realisticControl = childrenRealisticControls.Find(it => it.__vModel__.Equals(children.__vModel__) && it.__config__.jnpfKey.Equals(childrenConfig.jnpfKey));
                                        realisticControl ??= new FieldsModel();
                                        list.Add(new CodeGenConvIndexListControlOptionDesign()
                                        {
                                            jnpfKey = childrenConfig.jnpfKey,
                                            Name = string.Format("{0}_{1}", item.__vModel__, children.__vModel__),
                                            OptionsName = string.Format("dataForm.{0}[i].{1}", item.__vModel__, children.__vModel__),
                                            DictionaryType = null,
                                            DataType = null,
                                            IsStatic = true,
                                            IsIndex = columnDesign != null ? true : false,
                                            IsProps = false,
                                            Props = null,
                                            IsChildren = true,
                                            Content = $"{string.Format("{0}_{1}", item.__vModel__, children.__vModel__)}Options: {children.columnOptions.ToJsonString(CommonConst.options)},",
                                            IsLinkage = realisticControl.IsLinkage,
                                            TemplateJson = children.templateJson.ToJsonString()
                                        });
                                    }

                                    break;
                                case JnpfKeyConst.RELATIONFORM:
                                    {
                                        list.Add(new CodeGenConvIndexListControlOptionDesign()
                                        {
                                            jnpfKey = childrenConfig.jnpfKey,
                                            Name = string.Format("{0}_{1}", item.__vModel__, children.__vModel__),
                                            DictionaryType = null,
                                            DataType = null,
                                            IsStatic = true,
                                            IsIndex = columnDesign != null ? true : false,
                                            IsProps = false,
                                            Props = null,
                                            IsChildren = true,
                                            Content = $"{string.Format("{0}_{1}", item.__vModel__, children.__vModel__)}Options: {children.columnOptions.ToJsonString(CommonConst.options)},"
                                        });
                                    }

                                    break;
                            }
                        }
                    }

                    break;
                case JnpfKeyConst.COLLAPSE:
                    {
                        List<CodeGenConvIndexListCollapseTitleModel> title = new List<CodeGenConvIndexListCollapseTitleModel>();
                        List<string> activeList = new List<string>();
                        foreach (var children in config.children)
                        {
                            title.Add(new CodeGenConvIndexListCollapseTitleModel
                            {
                                title = children.title
                            });
                            activeList.Add(children.name);
                            list.AddRange(FormControlProps(children.__config__.children, realisticControls, columnDesignModel, type));
                        }

                        var activeKey = string.Format("active{0}", active++);
                        list.Add(new CodeGenConvIndexListControlOptionDesign()
                        {
                            jnpfKey = config.jnpfKey,
                            Name = activeKey,
                            IsStatic = true,
                            IsIndex = false,
                            IsProps = false,
                            IsChildren = false,
                            Content = config.active.ToJsonString(),
                            Title = title.ToJsonString()
                        });
                    }

                    break;
                case JnpfKeyConst.TAB:
                case JnpfKeyConst.STEPS:
                    {
                        List<CodeGenConvIndexListCollapseTitleModel> title = new List<CodeGenConvIndexListCollapseTitleModel>();
                        string activeIndexOf = string.Empty;
                        foreach (var children in config.children)
                        {
                            title.Add(new CodeGenConvIndexListCollapseTitleModel
                            {
                                title = type == 5 && children.titleI18nCode.IsNotEmptyOrNull() ? "@@this.$t('" + children.titleI18nCode + "','" + children.title + "@@')" : children.title
                            });
                            list.AddRange(FormControlProps(children.__config__.children, realisticControls, columnDesignModel, type));
                            if (children.name.Equals(config.active))
                                activeIndexOf = children.title;
                        }

                        var activeKey = string.Format("active{0}", active++);
                        list.Add(new CodeGenConvIndexListControlOptionDesign()
                        {
                            jnpfKey = config.jnpfKey,
                            Name = activeKey,
                            IsStatic = true,
                            IsIndex = false,
                            IsProps = false,
                            IsChildren = false,
                            Content = config.jnpfKey.Equals(JnpfKeyConst.STEPS) ? config.active.ToString() : (config.children.FindLastIndex(x => x.name != null && x.name.Equals(config.active)) + 1).ToString(),
                            Title = title.ToJsonString().Replace("\"@@this.$t('", "this.$t('").Replace("@@')\"", "')"),
                        });
                    }

                    break;
                case JnpfKeyConst.GROUPTITLE:
                case JnpfKeyConst.DIVIDER:
                case JnpfKeyConst.JNPFTEXT:
                    break;
                case JnpfKeyConst.COMSELECT:
                case JnpfKeyConst.ROLESELECT:
                case JnpfKeyConst.GROUPSELECT:
                case JnpfKeyConst.DEPSELECT:
                case JnpfKeyConst.POSSELECT:
                case JnpfKeyConst.USERSELECT:
                case JnpfKeyConst.USERSSELECT:
                    if (item.selectType != null && item.selectType == "custom")
                    {
                        list.Add(new CodeGenConvIndexListControlOptionDesign()
                        {
                            jnpfKey = config.jnpfKey,
                            IsStatic = true,
                            IsIndex = false,
                            IsProps = false,
                            Content = string.Format("{0}_AbleIds:{1},", item.__vModel__, item.ableIds.ToJsonString()),
                        });
                    }

                    break;
                default:
                    {
                        switch (config.jnpfKey)
                        {
                            case JnpfKeyConst.POPUPTABLESELECT:
                            case JnpfKeyConst.POPUPSELECT:
                            case JnpfKeyConst.AUTOCOMPLETE:
                                {
                                    list.Add(new CodeGenConvIndexListControlOptionDesign()
                                    {
                                        jnpfKey = config.jnpfKey,
                                        Name = item.__vModel__,
                                        DictionaryType = null,
                                        DataType = null,
                                        IsStatic = true,
                                        IsIndex = false,
                                        IsProps = false,
                                        Props = null,
                                        IsChildren = false,
                                        Content = string.Format("{0}Options: {1},", item.__vModel__, item.columnOptions.ToJsonString(CommonConst.options)),
                                        TemplateJson = item.templateJson.ToJsonString()
                                    });
                                }

                                break;
                            case JnpfKeyConst.RELATIONFORM:
                                {
                                    list.Add(new CodeGenConvIndexListControlOptionDesign()
                                    {
                                        jnpfKey = config.jnpfKey,
                                        Name = item.__vModel__,
                                        DictionaryType = null,
                                        DataType = null,
                                        IsStatic = true,
                                        IsIndex = false,
                                        IsProps = false,
                                        Props = null,
                                        IsChildren = false,
                                        Content = string.Format("{0}Options: {1},", item.__vModel__, item.columnOptions.ToJsonString(CommonConst.options))
                                    });
                                }

                                break;
                            case JnpfKeyConst.CHECKBOX:
                            case JnpfKeyConst.RADIO:
                            case JnpfKeyConst.SELECT:
                                {
                                    switch (config.dataType)
                                    {
                                        case "static":
                                            list.Add(new CodeGenConvIndexListControlOptionDesign()
                                            {
                                                jnpfKey = config.jnpfKey,
                                                Name = item.__vModel__,
                                                DictionaryType = config.dataType == "dictionary" ? config.dictionaryType : (config.dataType == "dynamic" ? config.propsUrl : null),
                                                DataType = config.dataType,
                                                IsStatic = true,
                                                IsIndex = true,
                                                IsProps = true,
                                                Props = string.Format("{{'label':'{0}','value':'{1}'}}", item.props?.label, item.props?.value),
                                                QueryProps = item.props.ToJsonString(CommonConst.options),
                                                IsChildren = false,
                                                Content = GetCodeGenConvIndexListControlOption(item.__vModel__, item.options)
                                            });
                                            break;
                                        default:
                                            list.Add(new CodeGenConvIndexListControlOptionDesign()
                                            {
                                                jnpfKey = config.jnpfKey,
                                                Name = item.__vModel__,
                                                DictionaryType = config.dataType == "dictionary" ? config.dictionaryType : (config.dataType == "dynamic" ? config.propsUrl : null),
                                                DataType = config.dataType,
                                                IsStatic = false,
                                                IsIndex = true,
                                                IsProps = true,
                                                QueryProps = item.props.ToJsonString(CommonConst.options),
                                                Props = $"{{'label':'{item.props?.label}','value':'{item.props?.value}'}}",
                                                IsChildren = false,
                                                Content = string.Format("{0}Options: [],", item.__vModel__),
                                                TemplateJson = config.dataType == "dynamic" ? config.templateJson.ToJsonString() : "[]"
                                            });
                                            break;
                                    }
                                }

                                break;
                            case JnpfKeyConst.TREESELECT:
                            case JnpfKeyConst.CASCADER:
                                {
                                    switch (config.dataType)
                                    {
                                        case "static":
                                            list.Add(new CodeGenConvIndexListControlOptionDesign()
                                            {
                                                jnpfKey = config.jnpfKey,
                                                Name = item.__vModel__,
                                                DictionaryType = config.dataType == "dictionary" ? config.dictionaryType : (config.dataType == "dynamic" ? config.propsUrl : null),
                                                DataType = config.dataType,
                                                IsStatic = true,
                                                IsIndex = true,
                                                IsProps = true,
                                                IsChildren = false,
                                                Props = item.props?.ToJsonString(CommonConst.options),
                                                QueryProps = item.props.ToJsonString(CommonConst.options),
                                                Content = GetCodeGenConvIndexListControlOption(item.__vModel__, item.options.ToObject<List<Dictionary<string, object>>>())
                                            });
                                            break;
                                        default:
                                            list.Add(new CodeGenConvIndexListControlOptionDesign()
                                            {
                                                jnpfKey = config.jnpfKey,
                                                Name = item.__vModel__,
                                                DictionaryType = config.dataType == "dictionary" ? config.dictionaryType : (config.dataType == "dynamic" ? config.propsUrl : null),
                                                DataType = config.dataType,
                                                IsStatic = false,
                                                IsIndex = true,
                                                IsProps = true,
                                                IsChildren = false,
                                                Props = item.props?.ToJsonString(CommonConst.options),
                                                QueryProps = item.props.ToJsonString(CommonConst.options),
                                                Content = string.Format("{0}Options: [],", item.__vModel__),
                                                TemplateJson = config.dataType == "dynamic" ? config.templateJson.ToJsonString() : "[]"
                                            });
                                            break;
                                    }
                                }

                                break;
                        }
                    }

                    break;
            }
        }

        return list;
    }

    /// <summary>
    /// 表单真实控件-剔除布局控件后.
    /// </summary>
    /// <param name="fieldList">组件列表</param>
    /// <returns></returns>
    public static List<CodeGenFormRealControlModel> FormRealControl(List<FieldsModel> fieldList)
    {
        var list = new List<CodeGenFormRealControlModel>();
        foreach (var item in fieldList)
        {
            var config = item.__config__;
            switch (config.jnpfKey)
            {
                case JnpfKeyConst.TABLE:
                    list.Add(new CodeGenFormRealControlModel
                    {
                        jnpfKey = config.jnpfKey,
                        vModel = item.__vModel__,
                        children = FormRealControl(config.children)
                    });
                    break;
                default:
                    list.Add(new CodeGenFormRealControlModel
                    {
                        jnpfKey = config.jnpfKey,
                        vModel = item.__vModel__,
                        multiple = item.multiple
                    });
                    break;
            }
        }
        return list;
    }

    /// <summary>
    /// 表单脚本设计.
    /// </summary>
    /// <param name="genModel">生成模式.</param>
    /// <param name="fieldList">组件列表.</param>
    /// <param name="tableColumns">表真实字段.</param>
    /// <returns></returns>
    public static List<FormScriptDesignModel> FormScriptDesign(string genModel, List<FieldsModel> fieldList, List<TableColumnConfigModel> tableColumns, List<IndexGridFieldModel> columnDesignModel)
    {
        var formScript = new List<FormScriptDesignModel>();
        foreach (FieldsModel item in fieldList)
        {
            var config = item.__config__;
            if (config.regList != null && config.regList.Any())
            {
                foreach (var it in config.regList)
                    if (it.messageI18nCode.IsNotEmptyOrNull()) it.message = "this.$t('" + it.messageI18nCode + "','" + it.message + "')"; else it.message = string.Format("'{0}'", it.message);
            }

            switch (config.jnpfKey)
            {
                case JnpfKeyConst.CALCULATE:
                case JnpfKeyConst.DATECALCULATE:
                    break;
                case JnpfKeyConst.TABLE:
                    {
                        var summaryFieldLabelWidth = new List<object>();
                        var childrenFormScript = new List<FormScriptDesignModel>();
                        foreach (var children in config.children)
                        {
                            var childrenConfig = children.__config__;
                            if (config.regList != null && config.regList.Any())
                            {
                                foreach (var it in childrenConfig.regList) if (it.messageI18nCode.IsNotEmptyOrNull()) it.message = "this.$t('" + it.messageI18nCode + "','" + it.message + "')";
                            }

                            switch (childrenConfig.jnpfKey)
                            {
                                case JnpfKeyConst.CALCULATE:
                                case JnpfKeyConst.DATECALCULATE:
                                    break;
                                case JnpfKeyConst.RELATIONFORMATTR:
                                case JnpfKeyConst.POPUPATTR:
                                    {
                                        if (children.isStorage == 1)
                                        {
                                            childrenFormScript.Add(new FormScriptDesignModel()
                                            {
                                                Name = children.__vModel__,
                                                OriginalName = children.__vModel__,
                                                jnpfKey = childrenConfig.jnpfKey,
                                                DataType = childrenConfig.dataType,
                                                DictionaryType = childrenConfig.dataType == "dictionary" ? childrenConfig.dictionaryType : (childrenConfig.dataType == "dynamic" ? childrenConfig.propsUrl : null),
                                                Format = children.format,
                                                Multiple = children.multiple,
                                                BillRule = childrenConfig.rule,
                                                Required = childrenConfig.required,
                                                Placeholder = childrenConfig.labelI18nCode.IsNotEmptyOrNull() ? $"this.$t('{childrenConfig.labelI18nCode}', '{childrenConfig.label + "不能为空"}')" : (childrenConfig.label.IsNotEmptyOrNull() ? string.Format("'{0}不能为空'", childrenConfig.label) : string.Empty),
                                                Label = childrenConfig.labelI18nCode.IsNotEmptyOrNull() ? $"this.$t('{childrenConfig.labelI18nCode}', '{childrenConfig.label}')" : (childrenConfig.label.IsNotEmptyOrNull() ? string.Format("{0}", childrenConfig.label) : string.Empty),
                                                Range = children.range,
                                                RegList = childrenConfig.regList,
                                                DefaultValue = childrenConfig.defaultValue.IsNullOrEmpty() ? "undefined" : childrenConfig.defaultValue.ToJsonStringOld(),
                                                Trigger = string.IsNullOrEmpty(childrenConfig.trigger?.ToString()) ? "blur" : (childrenConfig.trigger is Array ? childrenConfig.trigger.ToJsonString() : childrenConfig.trigger.ToString()),
                                                ChildrenList = null,
                                                IsSummary = item.showSummary && item.summaryField.Any(it => it.Equals(children.__vModel__)) ? true : false,
                                                IsLinked = children.IsLinked,
                                                LinkageRelationship = children.linkageReverseRelationship,
                                                IsLinkage = children.IsLinkage,
                                                isStorage = children.isStorage,
                                            });
                                        }
                                    }
                                    break;
                                case JnpfKeyConst.SWITCH:
                                    {
                                        childrenFormScript.Add(new FormScriptDesignModel()
                                        {
                                            Name = children.__vModel__,
                                            OriginalName = children.__vModel__,
                                            jnpfKey = childrenConfig.jnpfKey,
                                            DataType = childrenConfig.dataType,
                                            DictionaryType = childrenConfig.dataType == "dictionary" ? childrenConfig.dictionaryType : (childrenConfig.dataType == "dynamic" ? childrenConfig.propsUrl : null),
                                            Format = children.format,
                                            Multiple = children.multiple,
                                            BillRule = childrenConfig.rule,
                                            Required = childrenConfig.required,
                                            Placeholder = childrenConfig.labelI18nCode.IsNotEmptyOrNull() ? $"this.$t('{childrenConfig.labelI18nCode}', '{childrenConfig.label + "不能为空"}')" : (childrenConfig.label.IsNotEmptyOrNull() ? string.Format("'{0}不能为空'", childrenConfig.label) : string.Empty),
                                            Label = childrenConfig.labelI18nCode.IsNotEmptyOrNull() ? $"this.$t('{childrenConfig.labelI18nCode}', '{childrenConfig.label}')" : (childrenConfig.label.IsNotEmptyOrNull() ? string.Format("{0}", childrenConfig.label) : string.Empty),
                                            Range = children.range,
                                            RegList = childrenConfig.regList,
                                            DefaultValue = childrenConfig.defaultValue.ParseToBool(),
                                            Trigger = string.IsNullOrEmpty(childrenConfig.trigger?.ToString()) ? "blur" : (childrenConfig.trigger is Array ? childrenConfig.trigger.ToJsonString() : childrenConfig.trigger.ToString()),
                                            ChildrenList = null,
                                            IsSummary = item.showSummary && item.summaryField.Find(it => it.Equals(children.__vModel__)) != null ? true : false,
                                            IsLinked = item.IsLinked,
                                            LinkageRelationship = item.linkageReverseRelationship
                                        });
                                    }

                                    break;
                                case JnpfKeyConst.LOCATION:
                                    {
                                        childrenFormScript.Add(new FormScriptDesignModel()
                                        {
                                            Name = children.__vModel__,
                                            OriginalName = children.__vModel__,
                                            jnpfKey = childrenConfig.jnpfKey,
                                            DataType = childrenConfig.dataType,
                                            DictionaryType = childrenConfig.dataType == "dictionary" ? childrenConfig.dictionaryType : (childrenConfig.dataType == "dynamic" ? childrenConfig.propsUrl : null),
                                            Format = children.format,
                                            Multiple = children.multiple,
                                            BillRule = childrenConfig.rule,
                                            Required = childrenConfig.required,
                                            Placeholder = childrenConfig.labelI18nCode.IsNotEmptyOrNull() ? $"this.$t('{childrenConfig.labelI18nCode}', '{childrenConfig.label + "不能为空"}')" : (childrenConfig.label.IsNotEmptyOrNull() ? string.Format("'{0}不能为空'", childrenConfig.label) : string.Empty),
                                            Label = childrenConfig.labelI18nCode.IsNotEmptyOrNull() ? $"this.$t('{childrenConfig.labelI18nCode}', '{childrenConfig.label}')" : (childrenConfig.label.IsNotEmptyOrNull() ? string.Format("{0}", childrenConfig.label) : string.Empty),
                                            Range = children.range,
                                            RegList = childrenConfig.regList,
                                            DefaultValue = childrenConfig.defaultValue?.ToJsonStringOld(),
                                            Trigger = string.IsNullOrEmpty(childrenConfig.trigger?.ToString()) ? "blur" : (childrenConfig.trigger is Array ? childrenConfig.trigger.ToJsonString() : childrenConfig.trigger.ToString()),
                                            ChildrenList = null,
                                            IsSummary = item.showSummary && item.summaryField.Any(it => it.Equals(children.__vModel__)) ? true : false,
                                            IsLinked = children.IsLinked,
                                            LinkageRelationship = children.linkageReverseRelationship,
                                            IsLinkage = children.IsLinkage,
                                            Thousands = children.thousands,
                                            LocationScope = item.locationScope != null ? item.locationScope.ToJsonString() : "[]",
                                        });
                                    }

                                    break;
                                default:
                                    {
                                        childrenFormScript.Add(new FormScriptDesignModel()
                                        {
                                            Name = children.__vModel__,
                                            OriginalName = children.__vModel__,
                                            jnpfKey = childrenConfig.jnpfKey,
                                            DataType = childrenConfig.dataType,
                                            DictionaryType = childrenConfig.dataType == "dictionary" ? childrenConfig.dictionaryType : (childrenConfig.dataType == "dynamic" ? childrenConfig.propsUrl : null),
                                            Format = children.format,
                                            Multiple = children.multiple,
                                            BillRule = childrenConfig.rule,
                                            Required = childrenConfig.required,
                                            Placeholder = childrenConfig.labelI18nCode.IsNotEmptyOrNull() ? $"this.$t('{childrenConfig.labelI18nCode}', '{childrenConfig.label + "不能为空"}')" : (childrenConfig.label.IsNotEmptyOrNull() ? string.Format("'{0}不能为空'", childrenConfig.label) : string.Empty),
                                            Label = childrenConfig.labelI18nCode.IsNotEmptyOrNull() ? $"this.$t('{childrenConfig.labelI18nCode}', '{childrenConfig.label}')" : (childrenConfig.label.IsNotEmptyOrNull() ? string.Format("{0}", childrenConfig.label) : string.Empty),
                                            Range = children.range,
                                            RegList = childrenConfig.regList,
                                            DefaultValue = childrenConfig.defaultValue.IsNullOrEmpty() ? "undefined" : childrenConfig.defaultValue.ToJsonStringOld(),
                                            Trigger = string.IsNullOrEmpty(childrenConfig.trigger?.ToString()) ? "blur" : (childrenConfig.trigger is Array ? childrenConfig.trigger.ToJsonString() : childrenConfig.trigger.ToString()),
                                            ChildrenList = null,
                                            IsSummary = item.showSummary && item.summaryField.Any(it => it.Equals(children.__vModel__)) ? true : false,
                                            IsLinked = children.IsLinked,
                                            LinkageRelationship = children.linkageReverseRelationship,
                                            IsLinkage = children.IsLinkage,
                                            Thousands = children.thousands,
                                        });
                                        if (item.showSummary && item.summaryField.Any(it => it.Equals(children.__vModel__)) ? true : false)
                                            summaryFieldLabelWidth.Add(childrenConfig.labelWidth ?? 0);
                                    }

                                    break;
                            }
                        }
                        List<RegListModel> childrenRegList = new List<RegListModel>();

                        foreach (var reg in childrenFormScript.FindAll(it => it.RegList != null && it.RegList.Count > 0).Select(it => it.RegList))
                        {
                            childrenRegList.AddRange(reg);
                        }

                        item.footerBtnsList.Where(x => x.show).ToList().ForEach(x =>
                        {
                            x.value = x.value.Replace("-", "_");
                            x.actionConfig = x.actionConfig.ToJsonString() + ",";
                        });
                        formScript.Add(new FormScriptDesignModel()
                        {
                            PrimaryKey = tableColumns.Find(x => x.PrimaryKey)?.ColumnName,
                            Name = config.tableName.ParseToPascalCase(),
                            Placeholder = config.labelI18nCode.IsNotEmptyOrNull() ? $"this.$t('{config.labelI18nCode}', '{config.label}')" : (config.label.IsNotEmptyOrNull() ? config.label : string.Empty),
                            Label = config.labelI18nCode.IsNotEmptyOrNull() ? $"this.$t('{config.labelI18nCode}', '{config.label}')" : $"'{config.label}'",
                            OriginalName = item.__vModel__,
                            jnpfKey = config.jnpfKey,
                            ChildrenList = childrenFormScript,
                            Required = childrenFormScript.Any(it => it.Required.Equals(true)),
                            RegList = childrenRegList,
                            ShowSummary = item.showSummary,
                            SummaryField = item.summaryField.ToJsonString(),
                            SummaryFieldLabelWidth = summaryFieldLabelWidth.ToJsonString(),
                            IsLinked = childrenFormScript.Any(it => it.IsLinked.Equals(true)),
                            Thousands = childrenFormScript.Any(it => it.Thousands.Equals(true)),
                            ChildrenThousandsField = childrenFormScript.FindAll(it => it.Thousands.Equals(true)).Select(it => it.Name).ToList().ToJsonString(),
                            ColumnBtnsList = item.columnBtnsList.Where(x => x.show).ToList(),
                            FooterBtnsList = item.footerBtnsList.Where(x => x.show).ToList(),
                            IsAnyBatchRemove = item.footerBtnsList.Any(x => x.show && x.value.Equals("batchRemove")),
                            LayoutType = item.layoutType,
                            IsSummary = item.showSummary,
                            DefaultExpandAll = item.defaultExpandAll,
                            IsNumLimit = item.isNumLimit,
                            NumLimit = item.numLimit,
                        });
                    }

                    break;
                case JnpfKeyConst.RELATIONFORMATTR:
                case JnpfKeyConst.POPUPATTR:
                    {
                        if (item.isStorage == 1)
                        {
                            var originalName = string.Empty;
                            if (item.__vModel__.Contains("_jnpf_"))
                            {
                                var auxiliaryTableName = item.__vModel__.Matches(@"jnpf_(?<table>[\s\S]*?)_jnpf_", "table").Last();
                                var column = item.__vModel__.Replace(item.__vModel__.Matches(@"jnpf_(?<table>[\s\S]*?)_jnpf_").Last(), string.Empty);
                                var columns = tableColumns.Find(it => it.LowerColumnName.Equals(column) && it.IsAuxiliary.Equals(true) && (bool)it.TableName?.Equals(auxiliaryTableName));
                                if (columns != null)
                                    originalName = columns.OriginalColumnName;
                            }
                            else
                            {
                                var columns = tableColumns.Find(it => it.LowerColumnName.Equals(item.__vModel__));
                                if (columns != null)
                                    originalName = columns.OriginalColumnName;
                            }

                            formScript.Add(new FormScriptDesignModel()
                            {
                                IsInlineEditor = columnDesignModel != null ? columnDesignModel.Any(it => it.__vModel__ == item.__vModel__) : false,
                                Name = item.__vModel__,
                                OriginalName = originalName,
                                jnpfKey = config.jnpfKey,
                                DataType = config.dataType,
                                DictionaryType = config.dataType == "dictionary" ? config.dictionaryType : (config.dataType == "dynamic" ? config.propsUrl : null),
                                Format = item.format,
                                Multiple = item.multiple,
                                BillRule = config.rule,
                                Required = config.required,
                                Placeholder = config.labelI18nCode.IsNotEmptyOrNull() ? $"this.$t('common.inputText')+' '+this.$t('{config.labelI18nCode}', '{config.label}')" : (config.label.IsNotEmptyOrNull() ? string.Format("this.$t('common.inputText')+' '+'{0}'", config.label) : string.Empty),
                                Range = item.range,
                                RegList = config.regList,
                                DefaultValue = config.defaultValue.IsNullOrEmpty() ? "undefined" : config.defaultValue.ToJsonStringOld(),
                                Trigger = !string.IsNullOrEmpty(config.trigger?.ToString()) ? (config?.trigger is Array ? config?.trigger?.ToJsonString() : config?.trigger?.ToString()) : "blur",
                                ChildrenList = null,
                                IsLinked = item.IsLinked,
                                LinkageRelationship = item.linkageReverseRelationship,
                                IsLinkage = item.IsLinkage,
                                isStorage = item.isStorage,
                            });
                        }
                    }
                    break;
                case JnpfKeyConst.SWITCH:
                    {
                        var originalName = string.Empty;
                        if (item.__vModel__.Contains("_jnpf_"))
                        {
                            var auxiliaryTableName = item.__vModel__.Matches(@"jnpf_(?<table>[\s\S]*?)_jnpf_", "table").Last();
                            var column = item.__vModel__.Replace(item.__vModel__.Matches(@"jnpf_(?<table>[\s\S]*?)_jnpf_").Last(), string.Empty);
                            var columns = tableColumns.Find(it => it.LowerColumnName.Equals(column) && it.TableName == auxiliaryTableName && it.IsAuxiliary.Equals(true));
                            if (columns != null)
                                originalName = columns.OriginalColumnName;
                        }
                        else
                        {
                            var columns = tableColumns.Find(it => it.LowerColumnName.Equals(item.__vModel__));
                            if (columns != null)
                                originalName = columns.OriginalColumnName;
                        }

                        formScript.Add(new FormScriptDesignModel()
                        {
                            IsInlineEditor = columnDesignModel != null ? columnDesignModel.Any(it => it.__vModel__ == item.__vModel__) : false,
                            Name = item.__vModel__,
                            OriginalName = originalName,
                            jnpfKey = config.jnpfKey,
                            DataType = config.dataType,
                            DictionaryType = config.dataType == "dictionary" ? config.dictionaryType : (config.dataType == "dynamic" ? config.propsUrl : null),
                            Format = item.format,
                            Multiple = item.multiple,
                            BillRule = config.rule,
                            Required = config.required,
                            Placeholder = config.labelI18nCode.IsNotEmptyOrNull() ? $"this.$t('common.inputText')+' '+this.$t('{config.labelI18nCode}', '{config.label}')" : (config.label.IsNotEmptyOrNull() ? string.Format("this.$t('common.inputText')+' '+'{0}'", config.label) : string.Empty),
                            RegList = config.regList,
                            DefaultValue = config.defaultValue.ParseToBool(),
                            Trigger = string.IsNullOrEmpty(config.trigger?.ToString()) ? "blur" : (config.trigger is Array ? config.trigger.ToJsonString() : config.trigger.ToString()),
                            ChildrenList = null,
                            IsLinked = item.IsLinked,
                            LinkageRelationship = item.linkageReverseRelationship
                        });
                    }

                    break;
                default:
                    {
                        if (config.jnpfKey.Equals(JnpfKeyConst.CALCULATE)) break;
                        string originalName = string.Empty;
                        if (item.__vModel__.Contains("_jnpf_"))
                        {
                            var auxiliaryTableName = item.__vModel__.Matches(@"jnpf_(?<table>[\s\S]*?)_jnpf_", "table").Last();
                            var column = item.__vModel__.Replace(item.__vModel__.Matches(@"jnpf_(?<table>[\s\S]*?)_jnpf_").Last(), string.Empty);
                            var columns = tableColumns.Find(it => it.LowerColumnName.Equals(column) && it.IsAuxiliary.Equals(true) && (bool)it.TableName?.Equals(auxiliaryTableName));
                            if (columns != null)
                                originalName = columns.OriginalColumnName;
                        }
                        else
                        {
                            var columns = tableColumns.Find(it => it.LowerColumnName.Equals(item.__vModel__));
                            if (columns != null)
                                originalName = columns.OriginalColumnName;
                        }

                        var defValue = item.multiple ? (config.defaultValue.IsNullOrEmpty() ? new List<string>().ToJsonString() : config.defaultValue.ToJsonStringOld()) : (config.defaultValue.IsNullOrEmpty() || config.defaultValue?.ToString() == "[]" ? "undefined" : string.Format("'{0}'", config.defaultValue));
                        if (config.jnpfKey.Equals(JnpfKeyConst.COMSELECT))
                        {
                            if (item.multiple) defValue = config.defaultValue.IsNullOrEmpty() ? new List<string>().ToJsonString() : config.defaultValue.ToJsonStringOld();
                            else defValue = config.defaultValue.IsNullOrEmpty() || config.defaultValue.ToString().Equals("[]") ? "undefined" : string.Format("'{0}'", config.defaultValue);
                        }
                        else if (config.jnpfKey.Equals(JnpfKeyConst.CHECKBOX) || config.jnpfKey.Equals(JnpfKeyConst.CASCADER) || config.jnpfKey.Equals(JnpfKeyConst.ADDRESS))
                        {
                            defValue = config.defaultValue.IsNullOrEmpty() ? new List<string>().ToJsonString() : config.defaultValue.ToJsonStringOld();
                        }

                        formScript.Add(new FormScriptDesignModel()
                        {
                            IsInlineEditor = columnDesignModel != null ? columnDesignModel.Any(it => it.__vModel__ == item.__vModel__) : false,
                            Name = item.__vModel__,
                            OriginalName = originalName,
                            jnpfKey = config.jnpfKey,
                            DataType = config.dataType,
                            DictionaryType = config.dataType == "dictionary" ? config.dictionaryType : (config.dataType == "dynamic" ? config.propsUrl : null),
                            Format = item.format,
                            Multiple = item.multiple,
                            BillRule = config.rule,
                            Required = config.required,
                            Placeholder = config.labelI18nCode.IsNotEmptyOrNull() ? $"this.$t('common.inputText')+' '+this.$t('{config.labelI18nCode}', '{config.label}')" : (config.label.IsNotEmptyOrNull() ? string.Format("this.$t('common.inputText')+' '+'{0}'", config.label) : string.Empty),
                            Range = item.range,
                            RegList = config.regList,
                            DefaultValue = defValue,
                            Trigger = !string.IsNullOrEmpty(config.trigger?.ToString()) ? (config?.trigger is Array ? config?.trigger?.ToJsonString() : config?.trigger?.ToString()) : "blur",
                            ChildrenList = null,
                            IsLinked = item.IsLinked,
                            LinkageRelationship = item.linkageReverseRelationship,
                            IsLinkage = item.IsLinkage,
                            Thousands = item.thousands,
                            LocationScope = item.locationScope != null ? item.locationScope.ToJsonString() : "[]",
                        });
                    }

                    break;
            }
        }

        return formScript;
    }

    /// <summary>
    /// 获取常规index列表控件Option.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    private static string GetCodeGenConvIndexListControlOption(string name, List<Dictionary<string, object>> options)
    {
        StringBuilder sb = new StringBuilder();
        if (options != null)
        {
            sb.AppendFormat("{0}Options:", name);
            sb.Append("[");
            foreach (var valueItem in options?.ToObject<List<Dictionary<string, object>>>())
            {
                sb.Append("{");
                foreach (var items in valueItem)
                {
                    sb.AppendFormat("'{0}':{1},", items.Key, items.Value.ToJsonString());
                }

                sb = new StringBuilder(sb.ToString().TrimEnd(','));
                sb.Append("},");
            }

            sb = new StringBuilder(sb.ToString().TrimEnd(','));
            sb.Append("],");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 获取常规index列表控件Option.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    private static string GetCodeGenConvIndexListControlOption(List<Dictionary<string, object>> options)
    {
        StringBuilder sb = new StringBuilder();
        if (options != null)
        {
            sb.Append("[");
            foreach (var valueItem in options?.ToObject<List<Dictionary<string, object>>>())
            {
                sb.Append("{");
                foreach (var items in valueItem)
                {
                    sb.AppendFormat("'{0}':{1},", items.Key, items.Value.ToJsonString());
                }

                sb = new StringBuilder(sb.ToString().TrimEnd(','));
                sb.Append("},");
            }

            sb = new StringBuilder(sb.ToString().TrimEnd(','));
            sb.Append("]");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 查询时将多选关闭.
    /// </summary>
    /// <param name="propsModel"></param>
    /// <returns></returns>
    private static PropsBeanModel GetQueryPropsModel(PropsBeanModel propsModel)
    {
        var model = new PropsBeanModel();
        if (propsModel != null)
        {
            model = propsModel;
        }

        return model;
    }

    /// <summary>
    /// 获取用户控件联动属性.
    /// </summary>
    /// <param name="field">联动控件.</param>
    /// <param name="fieldList">当前控件集合.</param>
    /// <param name="logic">4-PC,5-App.</param>
    /// <returns></returns>
    private static string GetUserRelationAttr(FieldsModel field, List<FieldsModel> fieldList, int logic)
    {
        var res = string.Empty;

        // 用户控件联动
        if (field.__config__.jnpfKey.Equals(JnpfKeyConst.USERSELECT) && field.relationField.IsNotEmptyOrNull())
        {
            var relationField = fieldList.Find(x => x.__vModel__.Equals(field.relationField));
            if (relationField == null && field.relationField.ToLower().Contains("tablefield") && fieldList.Any(x => x.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE)))
            {
                var ctFieldList = fieldList.Find(x => x.__vModel__.Equals(field.relationField.Split("-").FirstOrDefault()));
                if (ctFieldList != null && ctFieldList.__config__.children != null)
                {
                    relationField = ctFieldList.__config__.children.Find(x => x.__vModel__.Equals(field.relationField.Split("-").LastOrDefault()));
                    field.relationField = logic == 4 ? field.relationField.Replace("-", "[scope.$index].") : field.relationField.Replace("-", "[i].");
                }
            }

            if (relationField != null) res = string.Format(" :ableRelationIds=\"dataForm.{0}\" ", field.relationField);
        }

        return res;
    }

    /// <summary>
    /// 表单json.
    /// </summary>
    /// <param name="formScriptDesignModels"></param>
    /// <returns></returns>
    public static string GetPropertyJson(List<FormScriptDesignModel> formScriptDesignModels)
    {
        List<CodeGenExportPropertyJsonModel>? list = new List<CodeGenExportPropertyJsonModel>();
        foreach (var item in formScriptDesignModels)
        {
            switch (item.jnpfKey)
            {
                case JnpfKeyConst.BARCODE:
                case JnpfKeyConst.QRCODE:
                case JnpfKeyConst.CALCULATE:
                    break;
                case JnpfKeyConst.TABLE:
                    list.Add(new CodeGenExportPropertyJsonModel
                    {
                        fieldName = item.Placeholder,
                        jnpfKey = item.jnpfKey,
                        fieldId = item.OriginalName,
                        required = item.Required,
                        multiple = item.Multiple,
                    });
                    foreach (var subtable in item.ChildrenList)
                    {
                        switch (subtable.jnpfKey)
                        {
                            case JnpfKeyConst.CALCULATE:
                                break;
                            case JnpfKeyConst.POPUPATTR:
                            case JnpfKeyConst.RELATIONFORMATTR:
                                switch (subtable.isStorage)
                                {
                                    case 1:
                                        list.Add(new CodeGenExportPropertyJsonModel
                                        {
                                            fieldName = string.Format("{0}-{1}", item.Placeholder, subtable.Placeholder),
                                            jnpfKey = subtable.jnpfKey,
                                            fieldId = string.Format("{0}-{1}", item.OriginalName, subtable.LowerName),
                                            required = subtable.Required,
                                            multiple = subtable.Multiple,
                                        });
                                        break;
                                }
                                break;
                            default:
                                list.Add(new CodeGenExportPropertyJsonModel
                                {
                                    fieldName = string.Format("{0}-{1}", item.Placeholder, subtable.Placeholder),
                                    jnpfKey = subtable.jnpfKey,
                                    fieldId = string.Format("{0}-{1}", item.OriginalName, subtable.LowerName),
                                    required = subtable.Required,
                                    multiple = subtable.Multiple,
                                });
                                break;
                        }
                    }
                    break;
                default:
                    switch (item.jnpfKey)
                    {
                        case JnpfKeyConst.POPUPATTR:
                        case JnpfKeyConst.RELATIONFORMATTR:
                            switch (item.isStorage)
                            {
                                case 1:
                                    list.Add(new CodeGenExportPropertyJsonModel
                                    {
                                        fieldName = item.Placeholder.Split("@@").LastOrDefault(),
                                        jnpfKey = item.jnpfKey,
                                        fieldId = item.LowerName,
                                        required = item.Required,
                                        multiple = item.Multiple,
                                    });
                                    break;
                            }
                            break;
                        default:
                            list.Add(new CodeGenExportPropertyJsonModel
                            {
                                fieldName = item.Placeholder.Split("@@").LastOrDefault(),
                                jnpfKey = item.jnpfKey,
                                fieldId = item.LowerName,
                                required = item.Required,
                                multiple = item.Multiple,
                            });
                            break;
                    }
                    break;
            }
        }
        return list.ToJsonString();
    }

    /// <summary>
    /// 表单json.
    /// </summary>
    /// <param name="realisticControls"></param>
    /// <returns></returns>
    public static string GetPropertyJson(List<FieldsModel> realisticControls)
    {
        List<CodeGenExportPropertyJsonModel>? list = new List<CodeGenExportPropertyJsonModel>();
        foreach (var item in realisticControls)
        {
            var config = item.__config__;
            switch (config.jnpfKey)
            {
                case JnpfKeyConst.DATECALCULATE:
                case JnpfKeyConst.BARCODE:
                case JnpfKeyConst.QRCODE:
                case JnpfKeyConst.CALCULATE:
                    break;
                case JnpfKeyConst.TABLE:
                    list.Add(new CodeGenExportPropertyJsonModel
                    {
                        fieldName = string.Format("{0}", config.label),
                        jnpfKey = config.jnpfKey,
                        fieldId = item.__vModel__,
                        required = false,
                        multiple = config.children.Any(it => it.multiple),
                        tableName = config.tableName,
                    });
                    foreach (var subtable in config.children)
                    {
                        switch (subtable.__config__.jnpfKey)
                        {
                            case JnpfKeyConst.DATECALCULATE:
                            case JnpfKeyConst.BARCODE:
                            case JnpfKeyConst.QRCODE:
                            case JnpfKeyConst.CALCULATE:
                                break;
                            case JnpfKeyConst.POPUPATTR:
                            case JnpfKeyConst.RELATIONFORMATTR:
                                switch (subtable.isStorage)
                                {
                                    case 1:
                                        list.Add(new CodeGenExportPropertyJsonModel
                                        {
                                            fieldName = string.Format("{0}-{1}", config.label, subtable.__config__.label),
                                            jnpfKey = subtable.__config__.jnpfKey,
                                            fieldId = string.Format("{0}-{1}", item.__vModel__, subtable.__vModel__),
                                            required = subtable.__config__.required,
                                            multiple = subtable.multiple,
                                            tableName = config.tableName,
                                        });
                                        break;
                                }
                                break;
                            default:
                                list.Add(new CodeGenExportPropertyJsonModel
                                {
                                    fieldName = string.Format("{0}-{1}", config.label, subtable.__config__.label),
                                    jnpfKey = subtable.__config__.jnpfKey,
                                    fieldId = string.Format("{0}-{1}", item.__vModel__, subtable.__vModel__),
                                    required = subtable.__config__.required,
                                    multiple = subtable.multiple,
                                    tableName = config.tableName,
                                });
                                break;
                        }
                    }
                    break;
                default:
                    switch (config.jnpfKey)
                    {
                        case JnpfKeyConst.POPUPATTR:
                        case JnpfKeyConst.RELATIONFORMATTR:
                            switch (item.isStorage)
                            {
                                case 1:
                                    list.Add(new CodeGenExportPropertyJsonModel
                                    {
                                        fieldName = string.Format("{0}", config.label.Split("@@").LastOrDefault()),
                                        jnpfKey = config.jnpfKey,
                                        fieldId = item.__vModel__,
                                        required = config.required,
                                        multiple = item.multiple,
                                        tableName = config.tableName,
                                    });
                                    break;
                            }
                            break;
                        default:
                            var label = config.label != null ? config.label : item.fullName;
                            list.Add(new CodeGenExportPropertyJsonModel
                            {
                                fieldName = string.Format("{0}", label?.Split("@@").LastOrDefault()),
                                jnpfKey = config.jnpfKey,
                                fieldId = item.__vModel__,
                                required = config.required,
                                multiple = item.multiple,
                                tableName = config.tableName,
                            });
                            break;
                    }
                    break;
            }
        }
        return list.ToJsonString();
    }
}