using System;
using System.Collections.Generic;

namespace Chudo_obuv.ScaffoldedModels;

public partial class Product
{
    public int ProductId { get; set; }

    public int SubcategoryId { get; set; }

    public int ManufacturerId { get; set; }

    public string ProductName { get; set; }

    public string ImageFile { get; set; }

    public string Description { get; set; }

    public string Composition { get; set; }

    public decimal Price { get; set; }

    public virtual Manufacturer Manufacturer { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<StockItem> StockItems { get; set; } = new List<StockItem>();

    public virtual Subcategory Subcategory { get; set; }
}
