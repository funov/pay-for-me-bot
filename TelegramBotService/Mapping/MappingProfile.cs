using AutoMapper;

namespace PayForMeBot.Models;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<ReceiptApiClient.Models.Product, Product>();
        CreateMap<Product, SqliteProvider.Models.Product>();
    }
}