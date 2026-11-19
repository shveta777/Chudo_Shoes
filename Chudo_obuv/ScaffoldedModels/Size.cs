using System;
using System.Collections.Generic;

namespace Chudo_obuv.ScaffoldedModels;

public partial class Size
{
    public int SizeId { get; set; }

    public int SizeValue { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<StockItem> StockItems { get; set; } = new List<StockItem>();
}
