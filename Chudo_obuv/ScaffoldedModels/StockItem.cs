using System;
using System.Collections.Generic;

namespace Chudo_obuv.ScaffoldedModels;

public partial class StockItem
{
    public int StockItemId { get; set; }

    public int ProductId { get; set; }

    public int SizeId { get; set; }

    public int AvailableQuantity { get; set; }

    public virtual Product Product { get; set; }

    public virtual Size Size { get; set; }
}
