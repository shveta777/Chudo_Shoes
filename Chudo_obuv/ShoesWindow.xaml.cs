using System;
using System.Collections.Generic;
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
        private readonly List<CartItem> _cart = new List<CartItem>();
        private readonly User _user;
        private readonly int? _orderClientId;
        private Action _updateCards;

        internal int? NewOrderId { get; private set; }

        public ShoesWindow() : this(null) { }

        public ShoesWindow(User user)
        {
            _user = user;
            InitializeComponent();
            LoadShoes();
            CategoryFilter.SelectionChanged += (sender, e) => _updateCards();
            PriceSort.SelectionChanged += (sender, e) => _updateCards();
            ProductSearch.TextChanged += (sender, e) => _updateCards();

            if (user == null)
                return;

            UserNameText.Text =
                $"{user.LastName} {user.FirstName} {user.Patronymic}";
            CategoryFilter.SelectedIndex = 0;
            PriceSort.SelectedIndex = 0;
            CatalogControls.Visibility = Visibility.Visible;
            CartButton.Visibility = Visibility.Visible;
            if (CurrentSession.CanManageOrders(user))
                OrdersButton.Visibility = Visibility.Visible;
            UpdateCartButton();
        }

        internal ShoesWindow(User employee, User client) : this(employee)
        {
            _orderClientId = client.UserId;
            UserNameText.Text = $"Заказ для: {client.LastName} {client.FirstName} {client.Patronymic}";
            OrdersButton.Visibility = Visibility.Collapsed;
            ExitButton.Content = "К заказам";
            UpdateCartButton();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            if (_orderClientId.HasValue)
            {
                Close();
                return;
            }

            CurrentSession.CurrentUser = null;
            new MainWindow().Show();
            Close();
        }

        private void ProductCard_Click(object sender, RoutedEventArgs e)
        {
            if (_user == null ||
                sender is not FrameworkElement card || card.Tag is not int productId)
                return;

            e.Handled = true;
            try
            {
                new ProductWindow(productId, _cart) { Owner = this }.ShowDialog();
                UpdateCartButton();
            }
            catch (Exception)
            {
                MessageBox.Show(this, "Не удалось открыть товар.", "Каталог обуви");
            }
        }

        private void Cart_Click(object sender, RoutedEventArgs e)
        {
            if (_user == null || _cart.Count == 0)
                return;

            string clientLabel = null;
            if (_orderClientId.HasValue)
                clientLabel = UserNameText.Text;

            var cartWindow = new CartWindow(_orderClientId ?? _user.UserId, _cart, clientLabel) { Owner = this };
            bool confirmed = cartWindow.ShowDialog() == true;
            UpdateCartButton();
            if (!confirmed)
                return;

            if (_orderClientId.HasValue)
            {
                NewOrderId = cartWindow.SavedOrderId;
                DialogResult = true;
                return;
            }

            RefreshCatalog();
        }

        private void UpdateCartButton()
        {
            string title = "Корзина";
            if (_orderClientId.HasValue)
                title = "К оформлению";

            CartButton.Content = $"{title} ({_cart.Count})";
            CartButton.IsEnabled = _cart.Count > 0;
        }

        private void Orders_Click(object sender, RoutedEventArgs e)
        {
            if (_orderClientId.HasValue || !CurrentSession.CanManageOrders(_user))
                return;

            try
            {
                var orders = new OrdersWindow(_user) { Owner = this };
                orders.ShowDialog();
                if (orders.OrdersChanged)
                    RefreshCatalog();
            }
            catch (Exception)
            {
                MessageBox.Show(this, "Не удалось открыть заказы.", "Заказы");
            }
        }

        private void RefreshCatalog()
        {
            try
            {
                LoadShoes();
            }
            catch (Exception)
            {
                MessageBox.Show(this, "Откройте каталог заново, чтобы обновить остатки.",
                    "Каталог обуви");
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

            var productActions = Visibility.Collapsed;
            if (_user != null)
                productActions = Visibility.Visible;

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
                    ProductActions = productActions,
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
