using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Chudo_obuv.ScaffoldedModels;
using Microsoft.EntityFrameworkCore;

namespace Chudo_obuv
{
    public partial class ShoesWindow : Window
    {
        public ShoesWindow()
        {
            InitializeComponent();
            LoadShoes();
        }

        public ShoesWindow(User user) : this()
        {
            UserNameText.Text =
                $"{user.LastName} {user.FirstName} {user.Patronymic}";
        }

        private void LoadShoes()
        {
            using var db = new ScaffoldedChudoObuvContext();

            var thisMonth = new DateOnly(
                DateTime.Today.Year, DateTime.Today.Month, 1);
            var lastMonth = thisMonth.AddMonths(-1);

            var orderedIds = db.OrderItems
                .Where(x => x.Order.OrderDate >= lastMonth &&
                            x.Order.OrderDate < thisMonth)
                .Select(x => x.ProductId)
                .ToList();

            var products = db.Products
                .Include(p => p.Manufacturer)
                .Include(p => p.Subcategory).ThenInclude(s => s.Category)
                .Include(p => p.StockItems)
                .ToList();

            ShoesList.ItemsSource = products.Select(p =>
            {
                int count = p.StockItems.Sum(x => x.AvailableQuantity);

                decimal price = p.Price;
                if (!orderedIds.Contains(p.ProductId))
                    price = p.Price * 0.75m;

                string imageFile = p.ImageFile;
                if (string.IsNullOrWhiteSpace(imageFile))
                    imageFile = "picture.png";
                else
                    imageFile = imageFile.Trim();

                string quantityText = "мало";
                if (count > 5)
                    quantityText = "много";

                string background = "#FFFFFF";
                if (count <= 3)
                    background = "#FF8080";

                return new
                {
                    Image = "/res/" + imageFile,
                    Title = p.Manufacturer.ManufacturerName + " | " + p.ProductName,
                    CategoryText = "Категория: " + p.Subcategory.Category.CategoryName,
                    QuantityText = "Количество: " + quantityText,
                    CompositionText = "Состав: " + p.Composition,
                    PriceText = price.ToString("N2") + " ₽",
                    CardBackground = background
                };
            }).ToList();
        }

        private void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            if (sender is Image image)
            {
                image.Source = new BitmapImage(
                    new Uri("/res/picture.png", UriKind.Relative));
            }
        }
    }
}