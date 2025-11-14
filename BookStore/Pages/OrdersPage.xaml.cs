using BookStore.Modules;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using Document = iTextSharp.text.Document;

namespace BookStore.Pages
{
    /// <summary>
    /// Логика взаимодействия для OrdersPage.xaml
    /// </summary>
    public partial class OrdersPage : Page
    {
        private bookStoreEntities _context;
        private List<OrderViewModel> _allOrders;
        private OrderViewModel _selectedOrder;

        public OrdersPage()
        {
            InitializeComponent();
            _context = new bookStoreEntities();
            LoadOrders();

            StatusComboBox.ItemsSource = new List<string> { "Все", "Создан", "Ожидает получения", "Получен" };
            StatusComboBox.SelectedIndex = 0;
        }

        private void LoadOrders()
        {
            try
            {
                var currentStoreId = App.CurrentUser?.StoreID ?? 1;

                _allOrders = _context.Orders
                    .Include("Customers")
                    .Include("OrderItems")
                    .Include("OrderItems.StoreBooks")
                    .Include("OrderItems.StoreBooks.Books")
                    .Include("OrderItems.StoreBooks.Books.Authors")
                    .Include("PaymentType")
                    .Where(o => o.OrderItems.Any(oi => oi.StoreBooks.StoreID == currentStoreId))
                    .ToList()
                    .Select(o => new OrderViewModel
                    {
                        OrderID = o.OrderID,
                        CustomerName = $"{o.Customers.LastName} {o.Customers.FirstName} {o.Customers.MiddleName ?? ""}".Trim(),
                        CustomerPhone = o.Customers.Phone,
                        PaymentType = o.PaymentType.PaymentName,
                        OrderDate = o.OrderDate,
                        DeliveryDate = GetDeliveryDate(o.OrderDate),
                        Status = o.Status,
                        OrderItems = o.OrderItems
                            .Where(oi => oi.StoreBooks.StoreID == currentStoreId)
                            .Select(oi => new OrderItemViewModel
                            {
                                BookTitle = oi.StoreBooks.Books.Title,
                                AuthorName = $"{oi.StoreBooks.Books.Authors.LastName} " +
                                             $"{(string.IsNullOrEmpty(oi.StoreBooks.Books.Authors.FirstName) ? "" : oi.StoreBooks.Books.Authors.FirstName.Substring(0, 1) + ".")} " +
                                             $"{(string.IsNullOrEmpty(oi.StoreBooks.Books.Authors.MiddleName) ? "" : oi.StoreBooks.Books.Authors.MiddleName.Substring(0, 1) + ".")}",
                                Quantity = oi.Quantity,
                                Price = oi.StoreBooks.Price
                            })
                            .ToList(),
                        TotalQuantity = o.OrderItems
                            .Where(oi => oi.StoreBooks.StoreID == currentStoreId)
                            .Sum(oi => oi.Quantity),
                        TotalAmount = o.OrderItems
                            .Where(oi => oi.StoreBooks.StoreID == currentStoreId)
                            .Sum(oi => oi.Quantity * oi.StoreBooks.Price)
                    })
                    .ToList();

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заказов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private DateTime GetDeliveryDate(DateTime orderDate)
        {
            return orderDate.DayOfWeek == DayOfWeek.Friday ? orderDate.AddDays(3) : orderDate.AddDays(1);
        }

        private void ApplyFilters()
        {
            if (_allOrders == null) return;

            var filtered = _allOrders.AsEnumerable();

            string status = StatusComboBox.SelectedItem as string;
            if (!string.IsNullOrEmpty(status) && status != "Все")
            {
                filtered = filtered.Where(o => o.Status == status);
            }

            string search = SearchTextBox.Text.ToLower();
            if (!string.IsNullOrWhiteSpace(search))
            {
                string[] searchTerms = search.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                filtered = filtered.Where(o =>
                    o.OrderID.ToString().Contains(search) ||
                    (o.CustomerPhone ?? "").Contains(search) ||
                    searchTerms.All(term => (o.CustomerName ?? "").ToLower().Contains(term))
                );
            }

            OrdersItemsControl.ItemsSource = filtered.ToList();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();
        private void StatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();

        private void OrdersItemsControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.Source is FrameworkElement element && element.DataContext is OrderViewModel clickedOrder)
            {
                SelectOrder(clickedOrder);
            }
            else if (e.OriginalSource is DependencyObject source)
            {
                var order = FindParentOrder(source);
                if (order != null)
                {
                    SelectOrder(order);
                }
            }
        }

        private OrderViewModel FindParentOrder(DependencyObject child)
        {
            var parent = VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is FrameworkElement fe && fe.DataContext is OrderViewModel order)
                    return order;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        private void SelectOrder(OrderViewModel order)
        {
            if (order == _selectedOrder) return;

            if (_selectedOrder != null)
            {
                _selectedOrder.IsSelected = false;
            }

            order.IsSelected = true;
            _selectedOrder = order;
        }


        private void ReloadData()
        {
            _context?.Dispose();
            _context = new bookStoreEntities();
            LoadOrders();
        }

        private void AddOrderButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new OrderAddEdit();
            if (window.ShowDialog() == true)
            {
                ReloadData();
            }
        }

        private void EditOrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedOrder == null)
            {
                MessageBox.Show("Выберите заказ для редактирования", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var window = new OrderAddEdit(_selectedOrder.OrderID);
            if (window.ShowDialog() == true)
            {
                ReloadData();
            }
        }

        private void DeleteOrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedOrder == null)
            {
                MessageBox.Show("Выберите заказ для удаления", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (MessageBox.Show($"Удалить заказ №{_selectedOrder.OrderID}?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    var order = _context.Orders.FirstOrDefault(o => o.OrderID == _selectedOrder.OrderID);
                    if (order != null)
                    {
                        _context.OrderItems.RemoveRange(order.OrderItems);
                        _context.Orders.Remove(order);
                        _context.SaveChanges();

                        ReloadData();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void GenerateReportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var start = StartDatePicker.SelectedDate ?? DateTime.MinValue;
                var end = EndDatePicker.SelectedDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.MaxValue;

                if (start > end)
                {
                    MessageBox.Show("Дата начала не может быть больше даты окончания", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var currentStoreId = App.CurrentUser?.StoreID ?? 1;
                var orderItemsInPeriod = _context.Orders
                    .Where(o => o.OrderDate >= start && o.OrderDate <= end &&
                                o.OrderItems.Any(oi => oi.StoreBooks.StoreID == currentStoreId))
                    .SelectMany(o => o.OrderItems)
                    .Where(oi => oi.StoreBooks.StoreID == currentStoreId)
                    .ToList();

                if (!orderItemsInPeriod.Any())
                {
                    MessageBox.Show("Нет данных за выбранный период", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var reportData = orderItemsInPeriod
                    .GroupBy(oi => new
                    {
                        Title = oi.StoreBooks.Books.Title,
                        Author = oi.StoreBooks.Books.Authors.LastName + " " +
                                 (!string.IsNullOrEmpty(oi.StoreBooks.Books.Authors.FirstName) ? oi.StoreBooks.Books.Authors.FirstName.Substring(0, 1) + "." : "")
                    })
                    .Select(g => new
                    {
                        g.Key.Title,
                        g.Key.Author,
                        TotalSold = g.Sum(x => x.Quantity),
                        TotalRevenue = g.Sum(x => x.Quantity * x.StoreBooks.Price)
                    })
                    .OrderByDescending(x => x.TotalRevenue)
                    .ToList();

                decimal totalPeriodRevenue = orderItemsInPeriod.Sum(oi => oi.Quantity * oi.StoreBooks.Price);

                string filePath = $"Отчет_продажи_{DateTime.Now:dd_MM_yyyy_HH_mm}.pdf";
                using (var fs = new FileStream(filePath, FileMode.Create))
                {
                    Document doc = new Document(PageSize.A4, 50, 50, 50, 50);
                    PdfWriter.GetInstance(doc, fs);
                    doc.Open();

                    var fontPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
                    BaseFont bf = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                    var font = new Font(bf, 12);
                    var boldFont = new Font(bf, 12, Font.BOLD);
                    var headerFont = new Font(bf, 16, Font.BOLD);

                    doc.Add(new iTextSharp.text.Paragraph($"Отчет по продажам c {start:dd.MM.yyyy} по {end:dd.MM.yyyy}", headerFont) { Alignment = Element.ALIGN_CENTER, SpacingAfter = 20 });

                    PdfPTable table = new PdfPTable(4) { WidthPercentage = 100, SpacingAfter = 20 };
                    table.SetWidths(new float[] { 3f, 2f, 1f, 1.5f });

                    table.AddCell(new PdfPCell(new Phrase("Название", boldFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });
                    table.AddCell(new PdfPCell(new Phrase("Автор", boldFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });
                    table.AddCell(new PdfPCell(new Phrase("Продано", boldFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });
                    table.AddCell(new PdfPCell(new Phrase("Выручка (₽)", boldFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });

                    foreach (var item in reportData)
                    {
                        table.AddCell(new Phrase(item.Title, font));
                        table.AddCell(new Phrase(item.Author, font));
                        table.AddCell(new Phrase(item.TotalSold.ToString(), font));
                        table.AddCell(new Phrase(item.TotalRevenue.ToString("N2"), font));
                    }
                    doc.Add(table);

                    doc.Add(new iTextSharp.text.Paragraph($"Общая выручка за период: {totalPeriodRevenue:N2} ₽", boldFont) { Alignment = Element.ALIGN_RIGHT });

                    doc.Close();
                }

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(System.IO.Path.GetFullPath(filePath)) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка формирования отчета: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class OrderViewModel : INotifyPropertyChanged
    {
        public int OrderID { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string PaymentType { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime DeliveryDate { get; set; }
        public string Status { get; set; }
        public List<OrderItemViewModel> OrderItems { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalAmount { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public class OrderItemViewModel
    {
        public string BookTitle { get; set; }
        public string AuthorName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
