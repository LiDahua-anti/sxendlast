using JNPF.Common.Security;
using JNPF.example.Entitys.Dto.SxPurchaseArrivalDetail;
using Mapster;
 
namespace JNPF.example.Entitys.Mapper.SxPurchaseArrivalDetail;

public class Mapper : IRegister
{
	public void Register(TypeAdapterConfig config)
	{
		config.ForType<SxPurchaseArrivalDetailCrInput, SxPurchaseArrivalDetailEntity>()
			.Map(dest => dest.Order_no, src => src.order_no != null ? src.order_no : null)
			.Map(dest => dest.Inventory_code, src => src.inventory_code != null ? src.inventory_code : null)
			.Map(dest => dest.Inventory_name, src => src.inventory_name != null ? src.inventory_name : null)
			.Map(dest => dest.Spec_model, src => src.spec_model != null ? src.spec_model : null)
			.Map(dest => dest.Main_unit, src => src.main_unit != null ? src.main_unit : null)
			.Map(dest => dest.Batch_no, src => src.batch_no != null ? src.batch_no : null)
			.Map(dest => dest.Warehouse_name, src => src.warehouse_name != null ? src.warehouse_name : null)
			.Map(dest => dest.Detail_remark, src => src.detail_remark != null ? src.detail_remark : null)
		;
		config.ForType<SxPurchaseArrivalDetailEntity, SxPurchaseArrivalDetailInfoOutput>()
			.Map(dest => dest.order_no, src => src.Order_no != null ? src.Order_no : null)
			.Map(dest => dest.inventory_code, src => src.Inventory_code != null ? src.Inventory_code : null)
			.Map(dest => dest.inventory_name, src => src.Inventory_name != null ? src.Inventory_name : null)
			.Map(dest => dest.spec_model, src => src.Spec_model != null ? src.Spec_model : null)
			.Map(dest => dest.main_unit, src => src.Main_unit != null ? src.Main_unit : null)
			.Map(dest => dest.batch_no, src => src.Batch_no != null ? src.Batch_no : null)
			.Map(dest => dest.warehouse_name, src => src.Warehouse_name != null ? src.Warehouse_name : null)
			.Map(dest => dest.detail_remark, src => src.Detail_remark != null ? src.Detail_remark : null)
		;
		config.ForType<SxPurchaseArrivalDetailEntity, SxPurchaseArrivalDetailDetailOutput>()
			.Map(dest => dest.order_no, src => src.Order_no != null ? src.Order_no : null)
			.Map(dest => dest.inventory_code, src => src.Inventory_code != null ? src.Inventory_code : null)
			.Map(dest => dest.inventory_name, src => src.Inventory_name != null ? src.Inventory_name : null)
			.Map(dest => dest.spec_model, src => src.Spec_model != null ? src.Spec_model : null)
			.Map(dest => dest.main_unit, src => src.Main_unit != null ? src.Main_unit : null)
			.Map(dest => dest.batch_no, src => src.Batch_no != null ? src.Batch_no : null)
			.Map(dest => dest.warehouse_name, src => src.Warehouse_name != null ? src.Warehouse_name : null)
			.Map(dest => dest.detail_remark, src => src.Detail_remark != null ? src.Detail_remark : null)
		;
	}
}
