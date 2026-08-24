using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EcommerceApp.Domain.Entities;

namespace EcommerceApp.Domain.Interfaces;

public interface IProductRepository
{
    Task<List<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(Guid id);
    Task AddAsync(Product p);
    Task UpdateAsync(Product p);
    Task DeleteAsync(Guid id);
    Task SaveChangesAsync();
}

