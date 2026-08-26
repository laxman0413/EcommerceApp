using AutoMapper;
using EcommerceApp.Application.Auth.Mappings;
using EcommerceApp.Application.Cart.Mappings;
using EcommerceApp.Application.Payments.Mappings;
using EcommerceApp.Application.Products.Mappings;

namespace EcommerceApp.Application.Tests.TestSupport;

public static class MapperFactory
{
    public static IMapper Create()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AuthMappingProfile>();
            cfg.AddProfile<ProductMappingProfile>();
            cfg.AddProfile<CartMappingProfile>();
            cfg.AddProfile<PaymentMappingProfile>();
        });

        return configuration.CreateMapper();
    }
}
