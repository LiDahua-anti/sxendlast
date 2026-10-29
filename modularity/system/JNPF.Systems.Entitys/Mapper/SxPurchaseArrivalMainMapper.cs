using JNPF.Common.Security;
using JNPF.example.Entitys.Dto.SxPurchaseArrivalMain;
using JNPF.example.Entitys.Dto.SxPurchaseArrivalDetail;
using Mapster;

namespace JNPF.example.Entitys.Mapper.SxPurchaseArrivalMain;
 
public class Mapper : IRegister
{
	public void Register(TypeAdapterConfig config)
	{
		config.ForType<SxPurchaseArrivalMainCrInput, SxPurchaseArrivalMainEntity>()
			.Map(dest => dest.Business_type, src => src.business_type != null ? src.business_type : null)
			.Map(dest => dest.Purchase_type, src => src.purchase_type != null ? src.purchase_type : null)
			.Map(dest => dest.Arrival_date, src => src.arrival_date != null ? src.arrival_date : null)
			.Map(dest => dest.Bill_no, src => src.bill_no != null ? src.bill_no : null)
			.Map(dest => dest.Supplier_name, src => src.supplier_name != null ? src.supplier_name : null)
			.Map(dest => dest.Purchase_remark, src => src.purchase_remark != null ? src.purchase_remark : null)
		;
		config.ForType<SxPurchaseArrivalMainEntity, SxPurchaseArrivalMainInfoOutput>()
			.Map(dest => dest.business_type, src => src.Business_type != null ? src.Business_type : null)
			.Map(dest => dest.purchase_type, src => src.Purchase_type != null ? src.Purchase_type : null)
			.Map(dest => dest.arrival_date, src => src.Arrival_date != null ? src.Arrival_date : null)
			.Map(dest => dest.bill_no, src => src.Bill_no != null ? src.Bill_no : null)
			.Map(dest => dest.supplier_name, src => src.Supplier_name != null ? src.Supplier_name : null)
			.Map(dest => dest.purchase_remark, src => src.Purchase_remark != null ? src.Purchase_remark : null)
            .Map(dest => dest.tableField7c2932, src => src.SxPurchaseArrivalDetailList.Adapt<List<SxPurchaseArrivalDetailInfoOutput>>())
		;
		config.ForType<SxPurchaseArrivalMainEntity, SxPurchaseArrivalMainDetailOutput>()
            .Map(dest => dest.tableField7c2932, src => src.SxPurchaseArrivalDetailList.Adapt<List<SxPurchaseArrivalDetailDetailOutput>>())
		;
	}
}
