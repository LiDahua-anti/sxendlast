using JNPF.Common.Security;
using JNPF.example.Entitys.Dto.JtBaseOrder;
using Mapster;
 
namespace JNPF.example.Entitys.Mapper.JtBaseOrder;

public class Mapper : IRegister
{
	public void Register(TypeAdapterConfig config)
	{
		config.ForType<JtBaseOrderCrInput, JtBaseOrderEntity>()
			.Map(dest => dest.Pph_value, src => src.pph_value != null ? src.pph_value : null)
			.Map(dest => dest.Ct_value, src => src.ct_value != null ? src.ct_value : null)
			.Map(dest => dest.Mpph_value, src => src.mpph_value != null ? src.mpph_value : null)
			.Map(dest => dest.Production_type, src => src.production_type != null ? src.production_type : null)
			.Map(dest => dest.Work_shift, src => src.work_shift != null ? src.work_shift : null)
			.Map(dest => dest.Start_date, src => src.start_date != null ? src.start_date : null)
			.Map(dest => dest.Product_code, src => src.product_code != null ? src.product_code : null)
			.Map(dest => dest.Product_name, src => src.product_name != null ? src.product_name : null)
			.Map(dest => dest.Open_no, src => src.open_no != null ? src.open_no : null)
			.Map(dest => dest.Nc_bomNo, src => src.nc_bomNo != null ? src.nc_bomNo : null)
			.Map(dest => dest.Quantity, src => src.quantity != null ? src.quantity : null)
			.Map(dest => dest.Production_status, src => src.production_status != null ? src.production_status : null)
			.Map(dest => dest.F_CREATOR_USER_ID, src => src.F_CREATOR_USER_ID != null ? src.F_CREATOR_USER_ID : null)
			.Map(dest => dest.F_SORT_CODE, src => src.F_SORT_CODE != null ? src.F_SORT_CODE : null)
			.Map(dest => dest.Class_type, src => src.class_type != null ? src.class_type : null)
			.Map(dest => dest.Class_time, src => src.class_time != null ? src.class_time : null)
			.Map(dest => dest.Start_time, src => src.start_time != null ? src.start_time : null)
			.Map(dest => dest.End_time, src => src.end_time != null ? src.end_time : null)
			.Map(dest => dest.Actural_start_time, src => src.actural_start_time != null ? src.actural_start_time : null)
			.Map(dest => dest.Actural_end_time, src => src.actural_end_time != null ? src.actural_end_time : null)
    // 忽略不需要映射的字段
    .Ignore(dest => dest.FlowId)  // 忽略流程引擎ID
    .Ignore(dest => dest.TenantId) // 忽略租户ID
    .Ignore(dest => dest.F_CREATOR_TIME) // 忽略创建时间（让数据库自动生成）
    ;
        ;
		config.ForType<JtBaseOrderEntity, JtBaseOrderInfoOutput>()
			.Map(dest => dest.pph_value, src => src.Pph_value != null ? src.Pph_value : null)
			.Map(dest => dest.ct_value, src => src.Ct_value != null ? src.Ct_value : null)
			.Map(dest => dest.mpph_value, src => src.Mpph_value != null ? src.Mpph_value : null)
			.Map(dest => dest.production_type, src => src.Production_type != null ? src.Production_type : null)
			.Map(dest => dest.work_shift, src => src.Work_shift != null ? src.Work_shift : null)
			.Map(dest => dest.start_date, src => src.Start_date != null ? src.Start_date : null)
			.Map(dest => dest.product_code, src => src.Product_code != null ? src.Product_code : null)
			.Map(dest => dest.product_name, src => src.Product_name != null ? src.Product_name : null)
			.Map(dest => dest.open_no, src => src.Open_no != null ? src.Open_no : null)
			.Map(dest => dest.nc_bomNo, src => src.Nc_bomNo != null ? src.Nc_bomNo : null)
			.Map(dest => dest.quantity, src => src.Quantity != null ? src.Quantity : null)
			.Map(dest => dest.production_status, src => src.Production_status != null ? src.Production_status : null)
			.Map(dest => dest.F_CREATOR_TIME, src => string.Format("{0:yyyy-MM-dd HH:mm:ss}", src.F_CREATOR_TIME))
			.Map(dest => dest.F_CREATOR_USER_ID, src => src.F_CREATOR_USER_ID != null ? src.F_CREATOR_USER_ID : null)
			.Map(dest => dest.F_SORT_CODE, src => src.F_SORT_CODE != null ? src.F_SORT_CODE : null)
			.Map(dest => dest.class_type, src => src.Class_type != null ? src.Class_type : null)
			.Map(dest => dest.class_time, src => src.Class_time != null ? src.Class_time : null)
			.Map(dest => dest.start_time, src => src.Start_time != null ? src.Start_time : null)
			.Map(dest => dest.end_time, src => src.End_time != null ? src.End_time : null)
			.Map(dest => dest.actural_start_time, src => src.Actural_start_time != null ? src.Actural_start_time : null)
			.Map(dest => dest.actural_end_time, src => src.Actural_end_time != null ? src.Actural_end_time : null)
        ;
	}
}
