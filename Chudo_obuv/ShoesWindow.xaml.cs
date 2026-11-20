using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Chudo_obuv.ScaffoldedModels;
using Microsoft.EntityFrameworkCore;
using Chudo_obuv.Statics;

namespace Chudo_obuv
{
    public partial class ShoesWindow : Window
    {
        private readonly List<CartItem> _cart = new List<CartItem>();
        private Action _updateCards;

        public ShoesWindow()
        {
            InitializeComponent();
            LoadShoes();
            CategoryFilter.SelectionChanged += (sender, e) => _updateCards();
            PriceSort.SelectionChanged += (sender, e) => _updateCards();
            ProductSearch.TextChanged += (sender, e) => _updateCards();
        }

        public ShoesWindow(User user) : this()
        {
            UserNameText.Text =
                $"{user.LastName} {user.FirstName} {user.Patronymic}";
            CategoryFilter.SelectedIndex = 0;
            PriceSort.SelectedIndex = 0;
            CatalogControls.Visibility = Visibility.Visible;
            CartButton.Visibility = Visibility.Visible;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            CurrentSession.CurrentUser = null;
            new MainWindow().Show();
            Close();
        }

        private void ProductCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (CurrentSession.CurrentUser == null ||
                sender is not FrameworkElement card || card.Tag is not int productId)
                return;

            try
            {
                new ProductWindow(productId, _cart) { Owner = this }.ShowDialog();
            }
            catch (Exception)
            {
                MessageBox.Show(this, "Не удалось открыть товар.", "Каталог обуви");
            }
        }

        private void Cart_Click(object sender, RoutedEventArgs e)
        {
            var user = CurrentSession.CurrentUser;
            if (user == null)
                return;

            var cartWindow = new CartWindow(user.UserId, _cart) { Owner = this };
            if (cartWindow.ShowDialog() == true)
            {
                try
                {
                    LoadShoes();
                }
                catch (Exception)
                {
                    MessageBox.Show(this, "Заказ сохранён. Откройте каталог заново, чтобы обновить остатки.",
                        "Каталог обуви");
                }
            }
        }

        private void LoadShoes()
        {
            using var db = new ScaffoldedChudoObuvContext();
            if (CategoryFilter.ItemsSource == null)
            {
                var categories = db.Categories.OrderBy(c => c.CategoryName).ToList();
                categories.Insert(0, new Category { CategoryName = "Все категории" });
                CategoryFilter.ItemsSource = categories;
            }

            var orderedIds = CatalogPricing.OrderedLastMonth(db).ToHashSet();

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
                    price = p.Price * CatalogPricing.DiscountMultiplier;

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
                    ProductId = p.ProductId,
                    Image = "/res/" + imageFile,
                    Title = p.Manufacturer.ManufacturerName + " | " + p.ProductName,
                    CategoryId = p.Subcategory.CategoryId,
                    CategoryText = "Категория: " + p.Subcategory.Category.CategoryName,
                    QuantityText = "Количество: " + quantityText,
                    CompositionText = "Состав: " + p.Composition,
                    Price = price,
                    PriceText = price.ToString("N2") + " ₽",
                    CardBackground = background,
                    ProductName = p.ProductName,
                    Description = p.Description,
                };
            }).ToList();

            void UpdateCards()
            {
                var result = cards.AsEnumerable();

                string searchText = ProductSearch.Text.Trim();

                result = result.Where(card =>
                    card.ProductName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    (card.Description ?? "").Contains(searchText, StringComparison.OrdinalIgnoreCase));

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

            _updateCards = UpdateCards;
            _updateCards();
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
