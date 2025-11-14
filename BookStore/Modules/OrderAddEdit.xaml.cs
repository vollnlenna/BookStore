using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
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
using System.Windows.Shapes;

namespace BookStore
{
    public partial class Customers
    {
        public string FullName
        {
            get
            {
                string lastName = LastName ?? "";
                string firstNameInitial = !string.IsNullOrEmpty(FirstName) ? $"{FirstName[0]}." : "";
                string middleNameInitial = !string.IsNullOrEmpty(MiddleName) ? $"{MiddleName[0]}." : "";

                return $"{lastName} {firstNameInitial}{middleNameInitial}".Trim();
            }
        }
    }
}

namespace BookStore.Modules
{
    public partial class OrderAddEdit : Window, INotifyPropertyChanged
    {
        private readonly bookStoreEntities _context = new bookStoreEntities();
        private Orders _editingOrder;

        public OrderAddEdit(int? orderId = null)
        {
            InitializeComponent();
            DataContext = this;
            try
            {
                Load(orderId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Критическая ошибка при загрузке окна: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        #region Свойства для привязки
        public string WindowTitle { get; private set; }
        public ObservableCollection<OrderItemViewModel> Items { get; } = new ObservableCollection<OrderItemViewModel>();
        public List<Customers> AllCustomers { get; private set; }
        public List<StoreBookLookup> AllStoreBooks { get; private set; }
        private Customers _selectedCustomer;
        public Customers SelectedCustomer { get => _selectedCustomer; set => SetProperty(ref _selectedCustomer, value); }
        public List<PaymentType> AllPaymentTypes { get; private set; }
        public int SelectedPaymentTypeId { get; set; }
        public List<string> AllStatuses { get; } = new List<string> { "Создан", "Ожидает получения", "Получен" };
        public string SelectedStatus { get; set; }
        public DateTime OrderDate { get; private set; }
        public DateTime DeliveryDate { get; private set; }
        public int TotalQuantity => Items.Sum(i => i.Quantity);
        public decimal TotalAmount => Items.Sum(i => i.Price * i.Quantity);
        #endregion

        private void Load(int? orderId)
        {
            var currentStoreId = App.CurrentUser?.StoreID ?? 1;

            AllCustomers = _context.Customers.OrderBy(c => c.LastName).ToList();

            AllPaymentTypes = _context.PaymentType.ToList();

            AllStoreBooks = _context.StoreBooks
                .Where(sb => sb.StoreID == currentStoreId && (sb.IsAvailable || sb.Quantity > 0))
                .Select(sb => new
                {
                    sb.StoreBookID,
                    sb.Books.Title,
                    sb.Books.Authors.LastName,
                    sb.Books.Authors.FirstName, 
                    sb.Books.Authors.MiddleName, 
                    sb.Price,
                    Quantity = sb.Quantity ?? 0
                })
                .ToList()
                .Select(sb => new StoreBookLookup(sb.StoreBookID, sb.Title, sb.LastName, sb.FirstName, sb.MiddleName, sb.Price, sb.Quantity)) 
                .ToList();

            Items.CollectionChanged += (s, e) => OnTotalsChanged();

            if (orderId.HasValue)
            {
                WindowTitle = $"Редактирование заказа №{orderId.Value}";
                _editingOrder = _context.Orders.Include("OrderItems.StoreBooks.Books.Authors").FirstOrDefault(o => o.OrderID == orderId.Value);
                if (_editingOrder == null) throw new Exception("Заказ не найден.");

                SelectedCustomer = AllCustomers.FirstOrDefault(c => c.CustomerID == _editingOrder.CustomerID);
                if (SelectedCustomer == null)
                {
                    var customerToAdd = _context.Customers.Find(_editingOrder.CustomerID);
                    if (customerToAdd != null)
                    {
                        AllCustomers.Add(customerToAdd);
                        AllCustomers = AllCustomers.OrderBy(c => c.LastName).ToList();
                        SelectedCustomer = customerToAdd;
                    }
                }

                SelectedPaymentTypeId = _editingOrder.PaymentTypeID;
                SelectedStatus = _editingOrder.Status;
                OrderDate = _editingOrder.OrderDate;
                DeliveryDate = GetDeliveryDate(OrderDate);

                foreach (var dbItem in _editingOrder.OrderItems)
                {
                    var bookLookup = AllStoreBooks.FirstOrDefault(b => b.StoreBookID == dbItem.StoreBookID);
                    if (bookLookup == null)
                    {
                        bookLookup = new StoreBookLookup(dbItem.StoreBookID, dbItem.StoreBooks.Books.Title, dbItem.StoreBooks.Books.Authors.LastName, dbItem.StoreBooks.Books.Authors.FirstName, dbItem.StoreBooks.Books.Authors.MiddleName, dbItem.StoreBooks.Price, 0, true);
                        AllStoreBooks.Add(bookLookup);
                    }
                    var itemVM = new OrderItemViewModel { OriginalOrderItemId = dbItem.OrderItemID, SelectedBook = bookLookup, Quantity = dbItem.Quantity };
                    itemVM.PropertyChanged += Item_PropertyChanged;
                    Items.Add(itemVM);
                }
                AllStoreBooks = AllStoreBooks.OrderBy(b => b.Title).ToList();
            }
            else
            {
                WindowTitle = "Добавление нового заказа";
                OrderDate = DateTime.Now;
                DeliveryDate = GetDeliveryDate(OrderDate);
                SelectedStatus = AllStatuses.First();
                SelectedPaymentTypeId = AllPaymentTypes.Any() ? AllPaymentTypes.First().PaymentTypeID : 0;
            }

            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(AllCustomers));
            OnPropertyChanged(nameof(AllPaymentTypes));
            OnPropertyChanged(nameof(AllStoreBooks));
        }

        #region Обработчики кнопок и сохранение
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedCustomer == null || !Items.Any() || Items.Any(i => i.SelectedBook == null))
            {
                MessageBox.Show("Необходимо выбрать клиента и заполнить позиции заказа.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            foreach (var item in Items)
            {
                var originalItem = _editingOrder?.OrderItems.FirstOrDefault(oi => oi.OrderItemID == item.OriginalOrderItemId);
                int originalQuantity = originalItem?.Quantity ?? 0;
                if (item.Quantity > (item.SelectedBook.AvailableQuantity + originalQuantity))
                {
                    MessageBox.Show($"Для книги \"{item.SelectedBook.Title}\" доступно только {item.SelectedBook.AvailableQuantity + originalQuantity} шт.", "Ошибка количества");
                    return;
                }
            }
            try
            {
                if (_editingOrder == null) CreateNewOrder();
                else UpdateExistingOrder();
                _context.SaveChanges();
                MessageBox.Show("Заказ успешно сохранен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.InnerException?.Message ?? ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
        private void AddItem_Click(object sender, RoutedEventArgs e)
        {
            var newItem = new OrderItemViewModel();
            newItem.PropertyChanged += Item_PropertyChanged;
            Items.Add(newItem);
        }
        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if ((e.Source as FrameworkElement)?.Tag is OrderItemViewModel item)
            {
                item.PropertyChanged -= Item_PropertyChanged;
                Items.Remove(item);
            }
        }
        private void DecreaseQuantity_Click(object sender, RoutedEventArgs e) { if ((e.Source as FrameworkElement)?.Tag is OrderItemViewModel item && item.Quantity > 1) item.Quantity--; }
        private void IncreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            if ((e.Source as FrameworkElement)?.Tag is OrderItemViewModel item && item.SelectedBook != null)
            {
                var originalItem = _editingOrder?.OrderItems.FirstOrDefault(oi => oi.OrderItemID == item.OriginalOrderItemId);
                int originalQuantity = originalItem?.Quantity ?? 0;
                if (item.Quantity < (item.SelectedBook.AvailableQuantity + originalQuantity)) item.Quantity++;
                else MessageBox.Show($"На складе доступно только {item.SelectedBook.AvailableQuantity + originalQuantity} шт.", "Внимание");
            }
        }
        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e) => OnTotalsChanged();
        #endregion
        #region Вспомогательная логика
        private void CreateNewOrder()
        {
            var newOrder = new Orders { CustomerID = SelectedCustomer.CustomerID, OrderDate = OrderDate, PaymentTypeID = SelectedPaymentTypeId, Status = SelectedStatus };
            foreach (var itemVM in Items)
            {
                newOrder.OrderItems.Add(new OrderItems { StoreBookID = itemVM.SelectedBook.StoreBookID, Quantity = itemVM.Quantity });
                var storeBook = _context.StoreBooks.Find(itemVM.SelectedBook.StoreBookID);
                if (storeBook != null) storeBook.Quantity -= itemVM.Quantity;
            }
            _context.Orders.Add(newOrder);
        }
        private void UpdateExistingOrder()
        {
            _editingOrder.CustomerID = SelectedCustomer.CustomerID;
            _editingOrder.PaymentTypeID = SelectedPaymentTypeId;
            _editingOrder.Status = SelectedStatus;


            var originalItemsFromDb = _editingOrder.OrderItems.ToList();

            var itemsToRemove = originalItemsFromDb
                .Where(dbItem => !Items.Any(vmItem => vmItem.OriginalOrderItemId == dbItem.OrderItemID))
                .ToList();

            foreach (var item in itemsToRemove)
            {
                var storeBook = _context.StoreBooks.Find(item.StoreBookID);
                if (storeBook != null)
                {
                    storeBook.Quantity += item.Quantity;
                }
                _context.OrderItems.Remove(item);
            }

            foreach (var itemVM in Items)
            {
                var originalItem = originalItemsFromDb.FirstOrDefault(orig => orig.OrderItemID == itemVM.OriginalOrderItemId);

                if (originalItem != null)
                {
                    int quantityDifference = itemVM.Quantity - originalItem.Quantity;
                    if (originalItem.StoreBookID != itemVM.SelectedBook.StoreBookID)
                    {
                        var oldBook = _context.StoreBooks.Find(originalItem.StoreBookID);
                        if (oldBook != null) oldBook.Quantity += originalItem.Quantity;

                        var newBook = _context.StoreBooks.Find(itemVM.SelectedBook.StoreBookID);
                        if (newBook != null) newBook.Quantity -= itemVM.Quantity;

                        originalItem.StoreBookID = itemVM.SelectedBook.StoreBookID;
                    }
                    else if (quantityDifference != 0)
                    {
                        var storeBook = _context.StoreBooks.Find(originalItem.StoreBookID);
                        if (storeBook != null) storeBook.Quantity -= quantityDifference;
                    }
                    originalItem.Quantity = itemVM.Quantity;
                }
                else
                {
                    var newDbItem = new OrderItems { StoreBookID = itemVM.SelectedBook.StoreBookID, Quantity = itemVM.Quantity };
                    _editingOrder.OrderItems.Add(newDbItem);

                    var storeBook = _context.StoreBooks.Find(itemVM.SelectedBook.StoreBookID);
                    if (storeBook != null) storeBook.Quantity -= itemVM.Quantity;
                }
            }
        }
        private DateTime GetDeliveryDate(DateTime orderDate) => orderDate.DayOfWeek == DayOfWeek.Friday ? orderDate.AddDays(3) : orderDate.AddDays(1);
        private void OnTotalsChanged() { OnPropertyChanged(nameof(TotalQuantity)); OnPropertyChanged(nameof(TotalAmount)); }
        #endregion
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion
    }

    public class OrderItemViewModel : INotifyPropertyChanged
    {
        public int? OriginalOrderItemId { get; set; }
        private StoreBookLookup _selectedBook;
        public StoreBookLookup SelectedBook { get => _selectedBook; set { if (SetProperty(ref _selectedBook, value)) Price = value?.Price ?? 0; } }
        private int _quantity = 1;
        public int Quantity { get => _quantity; set => SetProperty(ref _quantity, value); }
        private decimal _price;
        public decimal Price { get => _price; private set => SetProperty(ref _price, value); }
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return false;
            storage = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
        #endregion
    }

    public class StoreBookLookup
    {
        public int StoreBookID { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public decimal Price { get; set; }
        public int AvailableQuantity { get; set; }
        public string Display { get; private set; }

        public StoreBookLookup(int id, string title, string lastName, string firstName, string middleName, decimal price, int quantity, bool isOutOfStock = false)
        {
            StoreBookID = id;
            Title = title;
            Price = price;
            AvailableQuantity = quantity;

            string firstNameInitial = !string.IsNullOrEmpty(firstName) ? $"{firstName[0]}." : "";
            string middleNameInitial = !string.IsNullOrEmpty(middleName) ? $"{middleName[0]}." : "";
            Author = $"{lastName} {firstNameInitial}{middleNameInitial}".Trim();

            if (isOutOfStock)
                Display = $"{Title} ({Author}) - [НЕТ В НАЛИЧИИ]";
            else
                Display = $"{Title} ({Author})";
        }
    }
}
