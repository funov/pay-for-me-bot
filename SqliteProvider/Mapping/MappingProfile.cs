using AutoMapper;
using SqliteProvider.Models;
using SqliteProvider.Tables;

namespace SqliteProvider.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<ProductTable, Product>();
        CreateMap<Product, ProductTable>();
        CreateMap<UserProductBindingTable, UserProductBinding>();
    }
}