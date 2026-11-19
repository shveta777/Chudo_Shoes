using System;
using System.Collections.Generic;

namespace Chudo_obuv.ScaffoldedModels;

public partial class Manufacturer
{
    public int ManufacturerId { get; set; }

    public string ManufacturerName { get; set; }

    public string City { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
