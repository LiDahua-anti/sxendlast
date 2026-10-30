using JNPF;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.Logging;
using JNPF.Systems.Entitys.Entity.Custom;
using JNPF.example.Entitys;
using Mapster;
using SqlSugar;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// SqlSugar配置拓展.
/// </summary>
public static class SqlSugarConfigureExtensions
{
    public static void SqlSugarConfigure(this IServiceCollection services)
    {
        // 获取选项
        var dbOptions = App.GetOptions<ConnectionStringsOptions>();
        dbOptions.ConnectionConfigs.ForEach(SetDbConfig);

        List<ConnectionConfig> connectConfigList = new List<ConnectionConfig>();

        SqlSugarScope sqlSugar = new(dbOptions.ConnectionConfigs.Adapt<List<ConnectionConfig>>(), db =>
        {
            dbOptions.ConnectionConfigs.ForEach(config =>
            {
                var dbProvider = db.GetConnectionScope(config.ConfigId);
                SetDbAop(dbProvider);
            });

            //// 创建数据表 —— 基础数据
            //db.CodeFirst.InitTables(typeof(SupplierEntity));
            //db.CodeFirst.InitTables(typeof(TerminalDeviceEntity));
            //db.CodeFirst.InitTables(typeof(MoldEntity));
            //db.CodeFirst.InitTables(typeof(ExchangeRateEntity));

            //// 人工管理
            //db.CodeFirst.InitTables(typeof(BaseClassEntity));
            //db.CodeFirst.InitTables(typeof(EmployeeSkillEntity));
            //db.CodeFirst.InitTables(typeof(EfficiencyEntity));

            //// 设备管理
            //db.CodeFirst.InitTables(typeof(DeviceMasterEntity));
            //db.CodeFirst.InitTables(typeof(DeviceTypeMasterEntity));
            //db.CodeFirst.InitTables(typeof(DeviceItemMainEntity));
            //db.CodeFirst.InitTables(typeof(DeviceItemSubEntity));
            //db.CodeFirst.InitTables(typeof(DeviceItemAllotEntity));
            //db.CodeFirst.InitTables(typeof(DeviceAlarmEntity));
            //db.CodeFirst.InitTables(typeof(DeviceRepairEntity));
            //db.CodeFirst.InitTables(typeof(DeviceRepairRecordEntity));
            //db.CodeFirst.InitTables(typeof(DeviceCheckEntity));
            //db.CodeFirst.InitTables(typeof(DeviceCheckTaskEntity));
            //db.CodeFirst.InitTables(typeof(DeviceCheckTaskVersionEntity));

            //// 设备保养
            //db.CodeFirst.InitTables(typeof(MaintenanceProjectsEntity));
            //db.CodeFirst.InitTables(typeof(MaintenanceProjectDeviceEntity));
            //db.CodeFirst.InitTables(typeof(MaintenancePlanEntity));
            //db.CodeFirst.InitTables(typeof(MaintenanceRecordEntity));
            //db.CodeFirst.InitTables(typeof(MaintenanceRecordSpareEntity));

            //// 生产基础数据
            //db.CodeFirst.InitTables(typeof(BaseProcessEntity));
            //db.CodeFirst.InitTables(typeof(BaseQualityEntity));
            //db.CodeFirst.InitTables(typeof(BaseProcessRecordEntity));
            //db.CodeFirst.InitTables(typeof(BaseModelEntity));
            //db.CodeFirst.InitTables(typeof(BaseModelProcessEntity));
            //db.CodeFirst.InitTables(typeof(BaseModelBomEntity));
            //db.CodeFirst.InitTables(typeof(BaseOrderStopEntity));
            //db.CodeFirst.InitTables(typeof(LineInfosEntity));

            //// 生产订单/排产
            //db.CodeFirst.InitTables(typeof(ProductionOrderEntity));
            //db.CodeFirst.InitTables(typeof(ProductionScheduleEntity));
            //db.CodeFirst.InitTables(typeof(ProductionScheduleDetailEntity));
            //db.CodeFirst.InitTables(typeof(ProductionScheduleLogEntity));

            //// 工艺路线/工序
            //db.CodeFirst.InitTables(typeof(RoutingEntity));
            //db.CodeFirst.InitTables(typeof(RoutingProcessEntity));
            //db.CodeFirst.InitTables(typeof(ProcessCheckItemEntity));
            //db.CodeFirst.InitTables(typeof(ProcessDeviceEntity));
            //db.CodeFirst.InitTables(typeof(ProcessSopEntity));

            //// 存货/BOM
            //db.CodeFirst.InitTables(typeof(InventoryMasterEntity));
            //db.CodeFirst.InitTables(typeof(InventoryBomEntity));
            //db.CodeFirst.InitTables(typeof(InventoryCheckItemEntity));

            //// 流程卡
            //db.CodeFirst.InitTables(typeof(FlowCardEntity));
            //db.CodeFirst.InitTables(typeof(FlowCardProcessEntity));
            //db.CodeFirst.InitTables(typeof(FlowCardRelationEntity));
            //db.CodeFirst.InitTables(typeof(FlowCardDefectEntity));

            //// 报工
            //db.CodeFirst.InitTables(typeof(WorkReportEntity));
            //db.CodeFirst.InitTables(typeof(ReportCheckDetailEntity));

            //// 清洗合并
            //db.CodeFirst.InitTables(typeof(CleanMergeConfigEntity));
            //db.CodeFirst.InitTables(typeof(CleanMergeMemberEntity));

            //// 质量检验(IQC/OQC)
            //db.CodeFirst.InitTables(typeof(IqcEntity));
            //db.CodeFirst.InitTables(typeof(IqcDetailEntity));
            //db.CodeFirst.InitTables(typeof(OqcEntity));
            //db.CodeFirst.InitTables(typeof(OqcDetailEntity));

            //// 仓库/库存
            //db.CodeFirst.InitTables(typeof(WarehouseEntity));
            //db.CodeFirst.InitTables(typeof(WarehouseLocationEntity));
            //db.CodeFirst.InitTables(typeof(WarehouseBomEntity));
            //db.CodeFirst.InitTables(typeof(WarehouseReceiveEntity));
            //db.CodeFirst.InitTables(typeof(InventoryStockEntity));
            //db.CodeFirst.InitTables(typeof(StockFlowEntity));

            //// 入库/出库/领料
            //db.CodeFirst.InitTables(typeof(StockInEntity));
            //db.CodeFirst.InitTables(typeof(OutStockEntity));
            //db.CodeFirst.InitTables(typeof(PickingRecordEntity));

            //// 入库单/出库单
            //db.CodeFirst.InitTables(typeof(InOrderEntity));
            //db.CodeFirst.InitTables(typeof(InOrderDetailEntity));
            //db.CodeFirst.InitTables(typeof(OutOrderEntity));
            //db.CodeFirst.InitTables(typeof(OutOrderDetailEntity));

            //// 到货单
            //db.CodeFirst.InitTables(typeof(SxPurchaseArrivalMainEntity));
            //db.CodeFirst.InitTables(typeof(SxPurchaseArrivalDetailEntity));

            //// 用友同步
            //db.CodeFirst.InitTables(typeof(YonyouSyncLogEntity));

        });

        services.AddSingleton<ISqlSugarClient>(sqlSugar); // 单例注册
        services.AddScoped(typeof(ISqlSugarRepository<>), typeof(SqlSugarRepository<>)); // 仓储注册
        services.AddUnitOfWork<SqlSugarUnitOfWork>(); // 事务与工作单元注册

    }

    /// <summary>
    /// 配置连接属性.
    /// </summary>
    /// <param name="config"></param>
    private static void SetDbConfig(DbConnectionConfig config)
    {
        Log.Information(config.ToJsonString());
        config.ConnectionString = JNPFTenantExtensions.ToConnectionString(config);
        config.ConfigureExternalServices = new ConfigureExternalServices
        {
            EntityService = (type, column) => // 处理列
            {
                if (new NullabilityInfoContext().Create(type).WriteState is NullabilityState.Nullable)
                    column.IsNullable = true;

                if (config.DbType == SqlSugar.DbType.Oracle)
                {
                    if (type.PropertyType == typeof(long) || type.PropertyType == typeof(long?))
                        column.DataType = "number(18)";
                    if (type.PropertyType == typeof(bool) || type.PropertyType == typeof(bool?))
                        column.DataType = "number(1)";
                }
            },
        };
        config.IsAutoCloseConnection = true;
        config.MoreSettings = new ConnMoreSettings
        {
            IsAutoRemoveDataCache = true,
            SqlServerCodeFirstNvarchar = true, // 采用Nvarchar
            IsAutoToUpper = false,
            IsAutoDeleteQueryFilter = true,
            IsAutoUpdateQueryFilter = true,
            //IsWithNoLockQuery = false,
            //IsWithNoLockSubquery = false,
        };
    }

    /// <summary>
    /// 配置Aop.
    /// </summary>
    /// <param name="db"></param>
    public static void SetDbAop(SqlSugarScopeProvider db)
    {
        var config = db.CurrentConnectionConfig;

        // 设置超时时间
        db.Ado.CommandTimeOut = 30;

        // 打印SQL语句
        db.Aop.OnLogExecuting = (sql, pars) =>
        {
            var originColor = Console.ForegroundColor;
            if (sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                Console.ForegroundColor = ConsoleColor.Green;
            if (sql.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase) || sql.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
                Console.ForegroundColor = ConsoleColor.Yellow;
            if (sql.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase))
                Console.ForegroundColor = ConsoleColor.Red;

            if (!sql.Contains("BASE_SYS_LOG"))
            {
                Console.WriteLine("【" + DateTime.Now + "——执行SQL】\r\n" + UtilMethods.GetSqlString(config.DbType, sql, pars) + "\r\n");
                Console.ForegroundColor = originColor;
                App.PrintToMiniProfiler("SqlSugar", "Info", sql + "\r\n" + db.Utilities.SerializeObject(pars.ToDictionary(it => it.ParameterName, it => it.Value)));
            }
        };
        db.Aop.OnError = ex =>
        {
            if (ex.Parametres == null) return;
            var originColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkRed;
            var pars = db.Utilities.SerializeObject(((SugarParameter[])ex.Parametres).ToDictionary(it => it.ParameterName, it => it.Value));
            Console.WriteLine("【" + DateTime.Now + "——错误SQL】\r\n" + UtilMethods.GetSqlString(config.DbType, ex.Sql, (SugarParameter[])ex.Parametres) + "\r\n");
            Console.ForegroundColor = originColor;
            App.PrintToMiniProfiler("SqlSugar", "Error", $"{ex.Message}{Environment.NewLine}{ex.Sql}{pars}{Environment.NewLine}");
        };
    }
}