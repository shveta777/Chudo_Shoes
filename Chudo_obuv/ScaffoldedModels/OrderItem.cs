using System;
using System.Collections.Generic;

namespace Chudo_obuv.ScaffoldedModels;

public partial class OrderItem
{
    public int OrderItemId { get; set; }

    public int OrderId { get; set; }

    public int ProductId { get; set; }

    public int SizeId { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public virtual Order Order { get; set; }

    public virtual Product Product { get; set; }

    public virtual Size Size { get; set; }
}
