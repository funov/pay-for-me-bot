using AutoMapper;
using TelegramBotService.Models;

namespace TelegramBotService.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<ReceiptApiClient.Models.Product, Product>();
    }
}