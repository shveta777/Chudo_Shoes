using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Chudo_obuv.ScaffoldedModels;
using Microsoft.EntityFrameworkCore;
using Chudo_obuv.Statics;

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
            PriceSort.SelectedIndex = 0;
            PriceSort.Visibility = Visibility.Visible;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            CurrentSession.CurrentUser = null;
            new MainWindow().Show();
            Close();
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

            var cards = products.Select(p =>
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
                    Price = price,
                    PriceText = price.ToString("N2") + " ₽",
                    CardBackground = background
                };
            }).ToList();

            ShoesList.ItemsSource = cards;

            PriceSort.SelectionChanged += (sender, e) =>
            {
                if (PriceSort.SelectedIndex == 1)
                    ShoesList.ItemsSource = cards.OrderBy(card => card.Price).ToList();
                else if (PriceSort.SelectedIndex == 2)
                    ShoesList.ItemsSource = cards.OrderByDescending(card => card.Price).ToList();
                else
                    ShoesList.ItemsSource = cards;
            };
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
