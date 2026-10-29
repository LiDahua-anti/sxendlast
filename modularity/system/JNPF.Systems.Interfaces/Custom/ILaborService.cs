using JNPF.Systems.Entitys.Dto.Custom;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace JNPF.Systems.Interfaces.Custom
{
    /// <summary>
    /// 人工管理（班组/员工技能/人效考核）服务接口.
    /// </summary>
    public interface ILaborService
    {
        #region 班组管理

        /// <summary>
        /// 获取班组列表（分页）.
        /// </summary>
        Task<dynamic> GetTeamList([FromBody] TeamQuery input);

        /// <summary>
        /// 获取班组详情.
        /// </summary>
        Task<dynamic> GetTeamById(string id);

        /// <summary>
        /// 新增/编辑班组（含是否跨日）.
        /// </summary>
        Task CruTeam([FromBody] TeamCruInput input);

        /// <summary>
        /// 删除班组（软删除）.
        /// </summary>
        Task DeleteTeam(string id);

        /// <summary>
        /// 班组下拉（供排产/报工选择负责班组）.
        /// </summary>
        Task<dynamic> GetTeamSelected();

        #endregion

        #region 员工技能管理

        /// <summary>
        /// 获取员工技能列表（分页）.
        /// </summary>
        Task<dynamic> GetEmployeeSkillList([FromBody] EmployeeSkillQuery input);

        /// <summary>
        /// 获取员工技能详情.
        /// </summary>
        Task<dynamic> GetEmployeeSkillById(string id);

        /// <summary>
        /// 新增/编辑员工技能.
        /// </summary>
        Task CruEmployeeSkill([FromBody] EmployeeSkillCruInput input);

        /// <summary>
        /// 删除员工技能（软删除）.
        /// </summary>
        Task DeleteEmployeeSkill(string id);

        /// <summary>
        /// 批量导入员工技能档案.
        /// </summary>
        Task ImportEmployeeSkill([FromBody] EmployeeSkillImportInput input);

        #endregion

        #region 人效考核

        /// <summary>
        /// 获取人效考核列表（分页）.
        /// </summary>
        Task<dynamic> GetEfficiencyList([FromBody] EfficiencyQuery input);

        /// <summary>
        /// 生成人效考核统计（从报工记录按月按人汇总：合格产量/不良率/工时利用率→绩效得分）.
        /// </summary>
        Task GenerateEfficiency([FromBody] EfficiencyGenerateInput input);

        /// <summary>
        /// 删除人效考核记录（软删除）.
        /// </summary>
        Task DeleteEfficiency(string id);

        #endregion
    }
}
