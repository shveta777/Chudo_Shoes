using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Chudo_obuv.ScaffoldedModels;
using Chudo_obuv.Statics;
using Microsoft.EntityFrameworkCore;

namespace Chudo_obuv;

public partial class OrdersWindow : Window
{
    private readonly User _employee;
    private bool CanEdit => _employee.RoleId == CurrentSession.AdministratorRoleId;

    internal bool OrdersChanged { get; private set; }

    internal OrdersWindow(User employee)
    {
        if (!CurrentSession.CanManageOrders(employee))
            throw new InvalidOperationException("Доступ к заказам ограничен.");

        _employee = employee;
        InitializeComponent();
        if (CanEdit)
            AdminControls.Visibility = Visibility.Visible;

        using var db = new ScaffoldedChudoObuvContext();
        ClientPicker.ItemsSource = db.Users.AsNoTracking()
            .Where(user => user.RoleId == CurrentSession.CustomerRoleId)
            .OrderBy(user => user.LastName).ThenBy(user => user.FirstName)
            .Select(user => new
            {
                Client = user,
                FullName = user.LastName + " " + user.FirstName + " " + user.Patronymic
            })
            .ToList();
        ClientPicker.SelectedValuePath = "Client";
        LoadOrders();
    }

    private void LoadOrders(int? selectedId = null)
    {
        using var db = new ScaffoldedChudoObuvContext();
        OrdersGrid.ItemsSource = db.Orders.AsNoTracking()
            .Include(order => order.User)
            .OrderByDescending(order => order.OrderDate).ThenByDescending(order => order.OrderId)
            .ToList();

        if (selectedId.HasValue)
            OrdersGrid.SelectedValue = selectedId.Value;
        if (OrdersGrid.SelectedIndex < 0 && OrdersGrid.Items.Count > 0)
            OrdersGrid.SelectedIndex = 0;
        if (OrdersGrid.Items.Count == 0)
            ShowSelectedOrder();
    }

    private void OrdersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ShowSelectedOrder();
    }

    private void ShowSelectedOrder()
    {
        var order = OrdersGrid.SelectedItem as Order;
        ItemsGrid.ItemsSource = null;
        OrderItemsTitle.Text = "Состав заказа";
        TotalText.Text = "";
        DeleteOrderButton.IsEnabled = order != null;
        SaveDateButton.IsEnabled = CanEdit && order != null;
        DeleteItemButton.IsEnabled = false;
        OrderDatePicker.SelectedDate = order?.OrderDate.ToDateTime(TimeOnly.MinValue);
        if (order == null)
            return;

        OrderItemsTitle.Text = $"Состав заказа №{order.OrderId}";
        try
        {
            using var db = new ScaffoldedChudoObuvContext();
            var items = db.OrderItems.AsNoTracking()
                .Where(item => item.OrderId == order.OrderId)
                .Include(item => item.Product).ThenInclude(product => product.Manufacturer)
                .Include(item => item.Size)
                .OrderBy(item => item.OrderItemId)
                .ToList();

            ItemsGrid.ItemsSource = items;
            TotalText.Text = $"Итого: {items.Sum(item => item.UnitPrice * item.Quantity):N2} ₽";
        }
        catch (Exception)
        {
            MessageBox.Show(this, "Не удалось загрузить состав заказа.", "Заказы");
        }
    }

    private void ItemsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        DeleteItemButton.IsEnabled = CanEdit && ItemsGrid.SelectedItem is OrderItem;
    }

    private void StartOrder_Click(object sender, RoutedEventArgs e)
    {
        NewOrderPanel.Visibility = Visibility.Visible;
        ClientPicker.Focus();
        ClientPicker.IsDropDownOpen = true;
    }

    private void ClientPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ChooseProductsButton.IsEnabled = ClientPicker.SelectedItem != null;
    }

    private void CancelNewOrder_Click(object sender, RoutedEventArgs e)
    {
        CloseNewOrderPanel();
    }

    private void CloseNewOrderPanel()
    {
        ClientPicker.SelectedIndex = -1;
        NewOrderPanel.Visibility = Visibility.Collapsed;
    }

    private void NewOrder_Click(object sender, RoutedEventArgs e)
    {
        if (ClientPicker.SelectedValue is not User client)
        {
            MessageBox.Show(this, "Выберите клиента.", "Новый заказ");
            return;
        }

        try
        {
            var catalog = new ShoesWindow(_employee, client) { Owner = this };
            if (catalog.ShowDialog() == true)
            {
                OrdersChanged = true;
                CloseNewOrderPanel();
                LoadOrders(catalog.NewOrderId);
            }
        }
        catch (Exception)
        {
            MessageBox.Show(this, "Не удалось открыть каталог или обновить список заказов.", "Заказы");
        }
    }

    private void DeleteOrder_Click(object sender, RoutedEventArgs e)
    {
        if (OrdersGrid.SelectedItem is not Order order || !Confirm($"Удалить заказ №{order.OrderId}?"))
            return;

        SaveChange(db => DeleteOrder(db, order.OrderId));
    }

    private void SaveDate_Click(object sender, RoutedEventArgs e)
    {
        if (!CanEdit || OrdersGrid.SelectedItem is not Order order)
            return;
        if (OrderDatePicker.SelectedDate is not DateTime date)
        {
            MessageBox.Show(this, "Выберите дату заказа.", "Заказы");
            return;
        }

        SaveChange(db => FindOrder(db, order.OrderId).OrderDate = DateOnly.FromDateTime(date), order.OrderId);
    }

    private void DeleteItem_Click(object sender, RoutedEventArgs e)
    {
        if (!CanEdit || OrdersGrid.SelectedItem is not Order order ||
            ItemsGrid.SelectedItem is not OrderItem item || !Confirm("Удалить выбранную позицию?"))
            return;

        SaveChange(db => DeleteItem(db, order.OrderId, item.OrderItemId), order.OrderId);
    }

    private bool Confirm(string text)
    {
        return MessageBox.Show(this, text, "Заказы", MessageBoxButton.YesNo,
            MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes;
    }

    private void SaveChange(Action<ScaffoldedChudoObuvContext> change, int? selectedId = null)
    {
        try
        {
            using var db = new ScaffoldedChudoObuvContext();
            using var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable);
            change(db);
            db.SaveChanges();
            transaction.Commit();
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(this, ex.Message, "Заказы");
            return;
        }
        catch (Exception)
        {
            MessageBox.Show(this, "Не удалось сохранить изменение. Повторите попытку.", "Заказы");
            return;
        }

        OrdersChanged = true;
        try
        {
            LoadOrders(selectedId);
        }
        catch (Exception)
        {
            MessageBox.Show(this, "Изменение сохранено. Откройте список заказов заново.", "Заказы");
        }
    }

    private static Order FindOrder(ScaffoldedChudoObuvContext db, int orderId)
    {
        return db.Orders.Include(order => order.OrderItems).SingleOrDefault(order => order.OrderId == orderId)
            ?? throw new InvalidOperationException("Заказ уже удалён.");
    }

    private static void DeleteOrder(ScaffoldedChudoObuvContext db, int orderId)
    {
        var order = FindOrder(db, orderId);
        foreach (var item in order.OrderItems)
            RestoreStock(db, item);

        db.OrderItems.RemoveRange(order.OrderItems);
        db.Orders.Remove(order);
    }

    private static void DeleteItem(ScaffoldedChudoObuvContext db, int orderId, int itemId)
    {
        var order = FindOrder(db, orderId);
        var item = order.OrderItems.SingleOrDefault(line => line.OrderItemId == itemId)
            ?? throw new InvalidOperationException("Позиция уже удалена.");

        RestoreStock(db, item);
        db.OrderItems.Remove(item);
    }

    private static void RestoreStock(ScaffoldedChudoObuvContext db, OrderItem item)
    {
        var stock = db.StockItems.SingleOrDefault(s => s.ProductId == item.ProductId && s.SizeId == item.SizeId);
        if (stock == null)
            throw new InvalidOperationException("Не найдена запись об остатке для одного из размеров.");

        stock.AvailableQuantity += (int)item.Quantity;
    }
}
