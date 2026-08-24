using AutoMapper;
using EcommerceApp.Application.Products.DTOs;
using EcommerceApp.Domain.Entities;

namespace EcommerceApp.Application.Products.Mappings;

public class ProductMappingProfile : Profile
{
    public ProductMappingProfile()
    {
        // Entity → DTO (for responses)
        CreateMap<Product, ProductDto>();

        // CreateDto → Entity (for POST)
        CreateMap<CreateProductDto, Product>();

        // UpdateDto → Entity (for PUT — only map non-null fields)
        CreateMap<UpdateProductDto, Product>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        //model->json
    }
}