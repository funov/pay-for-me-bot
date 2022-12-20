using AutoMapper;
using ReceiptApiClient.JsonObjects;
using ReceiptApiClient.Models;

namespace ReceiptApiClient.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Item, Product>()
            .ForMember(destination => destination.Price,
                options => options.MapFrom(source => GetRublesPrice(source.Price)))
            .ForMember(destination => destination.TotalPrice,
                options => options.MapFrom(source => GetRublesPrice(source.TotalPrice)));
        CreateMap<ReceiptData, Receipt>()
            .ForMember(destination => destination.Products,
                options => options.MapFrom(source => source.Items))
            .ForMember(destination => destination.TotalPriceSum,
                options => options.MapFrom(source => GetRublesPrice(source.TotalSum)));
    }

    private static double GetRublesPrice(int kopecksPrice)
    {
        var kopecks = kopecksPrice % 100;
        var rubles = kopecksPrice / 100;

        return Math.Round(rubles + kopecks / 100.0, 2);
    }
}