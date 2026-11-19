using System;
using System.Collections.Generic;

namespace Chudo_obuv.ScaffoldedModels;

public partial class Category
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; }

    public virtual ICollection<Subcategory> Subcategories { get; set; } = new List<Subcategory>();
}
