using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Chudo_obuv.ScaffoldedModels;
using Microsoft.EntityFrameworkCore;

namespace Chudo_obuv;

public partial class ProductWindow : Window
{
    private readonly List<CartItem> _cart;
    private readonly Product _product;
    private readonly decimal _price;

    internal ProductWindow(int productId, List<CartItem> cart)
    {
        InitializeComponent();
        _cart = cart;

        using var db = new ScaffoldedChudoObuvContext();
        _product = db.Products
            .Include(p => p.Manufacturer)
            .Include(p => p.Subcategory).ThenInclude(s => s.Category)
            .Include(p => p.StockItems).ThenInclude(s => s.Size)
            .Single(p => p.ProductId == productId);

        _price = _product.Price;
        if (!CatalogPricing.OrderedLastMonth(db).Contains(productId))
            _price *= CatalogPricing.DiscountMultiplier;

        string imageFile = _product.ImageFile;
        if (string.IsNullOrWhiteSpace(imageFile))
            imageFile = "picture.png";

        DataContext = new
        {
            Product = _product,
            Image = "/res/" + imageFile.Trim(),
            PriceText = $"Цена: {_price:N2} ₽"
        };

        var stock = _product.StockItems.OrderBy(item => item.Size.SizeValue).ToList();
        SizeRangeText.Text = "Размерный ряд: " + string.Join(", ", stock.Select(item => item.Size.SizeValue).Distinct());
        AvailableSizes.ItemsSource = stock.Where(item => AvailableToAdd(item) > 0).ToList();
        AvailableSizes.SelectedIndex = 0;
        if (AvailableSizes.Items.Count == 0)
        {
            AvailableQuantityText.Text = "Нет размеров, доступных для добавления.";
            AddButton.IsEnabled = false;
        }
    }

    private CartItem FindCartItem(StockItem stock)
    {
        return _cart.Find(item => item.ProductId == stock.ProductId && item.SizeId == stock.SizeId);
    }

    private int AvailableToAdd(StockItem stock)
    {
        int inCart = FindCartItem(stock)?.Quantity ?? 0;
        return stock.AvailableQuantity - inCart;
    }

    private void AvailableSizes_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AvailableSizes.SelectedItem is StockItem stock)
            AvailableQuantityText.Text = $"Можно добавить: {AvailableToAdd(stock)} пар";
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        if (AvailableSizes.SelectedItem is not StockItem stock)
            return;

        int available = AvailableToAdd(stock);
        if (!int.TryParse(QuantityInput.Text, out int quantity) || quantity < 1 || quantity > available)
        {
            MessageBox.Show(this, $"Введите количество от 1 до {available}.", "Количество товара");
            return;
        }

        var line = FindCartItem(stock);
        if (line == null)
        {
            _cart.Add(new CartItem
            {
                ProductId = _product.ProductId,
                ProductName = _product.ProductName,
                SizeId = stock.SizeId,
                SizeValue = stock.Size.SizeValue,
                Quantity = quantity,
                UnitPrice = _price
            });
        }
        else
        {
            line.Quantity += quantity;
        }

        DialogResult = true;
    }

    private void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e)
    {
        if (sender is Image image)
        {
            image.ImageFailed -= Image_ImageFailed;
            image.Source = new BitmapImage(new Uri("/res/picture.png", UriKind.Relative));
        }
    }
}
