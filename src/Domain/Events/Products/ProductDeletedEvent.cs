using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LotusDharma.Domain.Entities;
namespace LotusDharma.Domain.Events.Products;
public class ProductDeletedEvent : BaseEvent
{
    public ProductDeletedEvent(Product product)
    {
        Product = product;
    }
    public Product Product { get; }
}
