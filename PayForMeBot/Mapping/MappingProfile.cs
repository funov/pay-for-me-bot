using AutoMapper;
using PayForMeBot.ReceiptApiClient.JsonObjects;
using PayForMeBot.ReceiptApiClient.Models;

namespace PayForMeBot.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Item, Product>();
        CreateMap<ReceiptData, Receipt>()
            .ForMember(destination => destination.Products,
                options => options.MapFrom(source => source.Items))
            .ForMember(destination => destination.TotalPriceSum,
                options => options.MapFrom(source => source.TotalSum));
    }
}