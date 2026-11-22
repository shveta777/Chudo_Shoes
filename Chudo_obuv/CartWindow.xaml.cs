using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using Chudo_obuv.ScaffoldedModels;
using Microsoft.EntityFrameworkCore;

namespace Chudo_obuv;

public partial class CartWindow : Window
{
    private readonly int _userId;
    private readonly List<CartItem> _items;

    internal int? SavedOrderId { get; private set; }

    internal CartWindow(int userId, List<CartItem> items, string clientLabel = null)
    {
        InitializeComponent();
        _userId = userId;
        _items = items;
        if (!string.IsNullOrWhiteSpace(clientLabel))
        {
            CartClientText.Text = clientLabel;
            CartClientText.Visibility = Visibility.Visible;
        }
        UpdateCart();
    }

    private void UpdateCart()
    {
        CartList.ItemsSource = _items.ToList();
        TotalText.Text = $"Итого: {_items.Sum(item => item.Total):N2} ₽";
        ConfirmButton.IsEnabled = _items.Count > 0;
        RemoveButton.IsEnabled = ConfirmButton.IsEnabled;
        CancelButton.IsEnabled = ConfirmButton.IsEnabled;
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (CartList.SelectedItem is CartItem item)
        {
            _items.Remove(item);
            UpdateCart();
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        var answer = MessageBox.Show(this, "Очистить корзину и отменить заказ?", "Отмена заказа",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer == MessageBoxResult.Yes)
        {
            _items.Clear();
            DialogResult = false;
        }
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (_items.Count == 0)
            return;

        var answer = MessageBox.Show(this, "Оформить заказ?", "Подтверждение заказа",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes)
            return;

        int orderId;
        try
        {
            using var db = new ScaffoldedChudoObuvContext();
            using var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable);
            orderId = SaveOrder(db);
            transaction.Commit();
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(this, ex.Message, "Не удалось оформить заказ");
            return;
        }
        catch (Exception)
        {
            MessageBox.Show(this, "Не удалось сохранить заказ. Повторите попытку.", "Заказ");
            return;
        }

        _items.Clear();
        SavedOrderId = orderId;
        MessageBox.Show(this, $"Заказ №{orderId} оформлен.", "Заказ");
        DialogResult = true;
    }

    private int SaveOrder(ScaffoldedChudoObuvContext db)
    {
        var order = new Order
        {
            OrderId = (db.Orders.Max(order => (int?)order.OrderId) ?? 0) + 1,
            UserId = _userId,
            OrderDate = DateOnly.FromDateTime(DateTime.Today)
        };
        int itemId = db.OrderItems.Max(item => (int?)item.OrderItemId) ?? 0;

        foreach (var item in _items)
        {
            var stock = db.StockItems.SingleOrDefault(s =>
                s.ProductId == item.ProductId && s.SizeId == item.SizeId);
            int available = stock?.AvailableQuantity ?? 0;
            if (item.Quantity < 1 || item.Quantity > available)
            {
                throw new InvalidOperationException(
                    $"«{item.ProductName}», размер {item.SizeValue}: доступно {available} пар.");
            }

            stock.AvailableQuantity -= item.Quantity;
            order.OrderItems.Add(new OrderItem
            {
                OrderItemId = ++itemId,
                OrderId = order.OrderId,
                ProductId = item.ProductId,
                SizeId = item.SizeId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            });
        }

        db.Orders.Add(order);
        db.SaveChanges();
        return order.OrderId;
    }
}

internal sealed class CartItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public int SizeId { get; set; }
    public int SizeValue { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total => UnitPrice * Quantity;
}
