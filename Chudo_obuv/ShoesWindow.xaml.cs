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
            CategoryFilter.SelectedIndex = 0;
            PriceSort.SelectedIndex = 0;
            CatalogControls.Visibility = Visibility.Visible;
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
            var categories = db.Categories.OrderBy(c => c.CategoryName).ToList();
            categories.Insert(0, new Category { CategoryName = "Все категории" });
            CategoryFilter.ItemsSource = categories;
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
                    CategoryId = p.Subcategory.CategoryId,
                    CategoryText = "Категория: " + p.Subcategory.Category.CategoryName,
                    QuantityText = "Количество: " + quantityText,
                    CompositionText = "Состав: " + p.Composition,
                    Price = price,
                    PriceText = price.ToString("N2") + " ₽",
                    CardBackground = background
                };
            }).ToList();

            ShoesList.ItemsSource = cards;

            void UpdateCards()
            {
                var result = cards.AsEnumerable();

                if (CategoryFilter.SelectedIndex > 0 &&
                    CategoryFilter.SelectedItem is Category category)
                {
                    result = result.Where(card => card.CategoryId == category.CategoryId);
                }

                if (PriceSort.SelectedIndex == 1)
                {
                    result = result.OrderBy(card => card.Price);
                }
                else if (PriceSort.SelectedIndex == 2)
                {
                    result = result.OrderByDescending(card => card.Price);
                }

                ShoesList.ItemsSource = result.ToList();
            }

            CategoryFilter.SelectionChanged += (sender, e) => UpdateCards();
            PriceSort.SelectionChanged += (sender, e) => UpdateCards();
        
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
