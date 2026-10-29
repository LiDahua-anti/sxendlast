using JNPF.Common.Contracts;
using SqlSugar;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Entity.Custom
{
    /// <summary>
    /// 流程卡追溯关系表（合并/拆分/重印）.
    /// 新卡与旧卡的关联：合并=多旧→一新，拆分=一旧→多新，重印=关联原卡；
    /// 新流程卡可通过本表上溯/下钻追溯来源旧卡。
    /// </summary>
    [SugarTable("sx_flow_card_relation")]
    public class FlowCardRelationEntity : CLDSEntityBase
    {
        /// <summary>
        /// 新流程卡ID（sx_flow_card.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "new_card_id", IsNullable = false, ColumnDescription = "/// 新流程卡ID（sx_flow_card.F_ID）. ///")]
        [Description("新流程卡ID")]
        public string NewCardId { get; set; }

        /// <summary>
        /// 新流程卡号（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "new_card_no", IsNullable = true, ColumnDescription = "/// 新流程卡号（冗余）. ///")]
        [Description("新流程卡号(冗余)")]
        public string NewCardNo { get; set; }

        /// <summary>
        /// 旧流程卡ID（sx_flow_card.F_ID）.
        /// </summary>
        [SugarColumn(ColumnName = "old_card_id", IsNullable = false, ColumnDescription = "/// 旧流程卡ID（sx_flow_card.F_ID）. ///")]
        [Description("旧流程卡ID")]
        public string OldCardId { get; set; }

        /// <summary>
        /// 旧流程卡号（冗余）.
        /// </summary>
        [SugarColumn(ColumnName = "old_card_no", IsNullable = true, ColumnDescription = "/// 旧流程卡号（冗余）. ///")]
        [Description("旧流程卡号(冗余)")]
        public string OldCardNo { get; set; }

        /// <summary>
        /// 关系类型：1合并（多旧→一新） 2拆分（一旧→多新） 3重印（关联原卡）.
        /// </summary>
        [SugarColumn(ColumnName = "relation_type", IsNullable = true, ColumnDescription = "/// 关系类型：1合并（多旧→一新） 2拆分（一旧→多新） 3重印（关联原卡）. ///")]
        [Description("关系类型:1合并 2拆分 3重印")]
        public int? RelationType { get; set; }

        /// <summary>
        /// 备注.
        /// </summary>
        [SugarColumn(ColumnName = "remark", IsNullable = true, ColumnDescription = "/// 备注. ///")]
        [Description("备注")]
        public string Remark { get; set; }
    }
}
