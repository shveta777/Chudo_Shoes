using System;
using System.Linq;
using Chudo_obuv.ScaffoldedModels;

namespace Chudo_obuv;

internal static class CatalogPricing
{
    public const decimal DiscountMultiplier = 0.75m;

    public static IQueryable<int> OrderedLastMonth(ScaffoldedChudoObuvContext db)
    {
        var today = DateTime.Today;
        var thisMonth = new DateOnly(today.Year, today.Month, 1);
        var lastMonth = thisMonth.AddMonths(-1);

        return db.OrderItems
            .Where(item => item.Order.OrderDate >= lastMonth &&
                           item.Order.OrderDate < thisMonth)
            .Select(item => item.ProductId);
    }
}
