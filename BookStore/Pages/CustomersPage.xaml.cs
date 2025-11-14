using System;
using System.Collections.Generic;
using System.Linq;
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
using BookStore.Modules;

namespace BookStore.Pages
{
    /// <summary>
    /// Логика взаимодействия для CustomersPage.xaml
    /// </summary>
    public partial class CustomersPage : Page
    {
        private bookStoreEntities _context;
        private List<Customers> _allCustomers;
        private bool CanDelete => App.CurrentUser?.RoleID == 2;

        public CustomersPage()
        {
            InitializeComponent();
            _context = new bookStoreEntities();
            LoadCustomers();
            UpdateUI();
        }

        private void ReloadData()
        {
            _context?.Dispose();
            _context = new bookStoreEntities();
            LoadCustomers();
        }

        private void LoadCustomers()
        {
            try
            {
                _allCustomers = _context.Customers.ToList();
                FilterCustomers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки клиентов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateUI()
        {
            DeleteButton.Visibility = CanDelete ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterCustomers();
        }

        private void FilterCustomers()
        {
            string searchText = SearchTextBox.Text.ToLower();
            if (string.IsNullOrWhiteSpace(searchText))
            {
                CustomersGrid.ItemsSource = _allCustomers;
            }
            else
            {
                string[] searchTerms = searchText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var filtered = _allCustomers.Where(c =>
                    searchTerms.All(term =>
                        (c.LastName ?? "").ToLower().Contains(term) ||
                        (c.FirstName ?? "").ToLower().Contains(term) ||
                        (c.MiddleName ?? "").ToLower().Contains(term)
                    ) ||
                    (c.Phone ?? "").ToLower().Contains(searchText) ||
                    (c.Email ?? "").ToLower().Contains(searchText)
                ).ToList();
                CustomersGrid.ItemsSource = filtered;
            }
        }

        private void AddCustomer_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CustomerAddEdit();
            if (dialog.ShowDialog() == true)
            {
                ReloadData();
                MessageBox.Show("Клиент успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void EditCustomer_Click(object sender, RoutedEventArgs e)
        {
            if (CustomersGrid.SelectedItem is Customers selectedCustomer)
            {
                var dialog = new CustomerAddEdit(selectedCustomer);
                if (dialog.ShowDialog() == true)
                {
                    ReloadData();
                    MessageBox.Show("Клиент успешно обновлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void DeleteCustomer_Click(object sender, RoutedEventArgs e)
        {
            if (!CanDelete) return;

            if (CustomersGrid.SelectedItem is Customers selectedCustomer)
            {
                if (MessageBox.Show($"Вы уверены, что хотите удалить клиента {selectedCustomer.LastName} {selectedCustomer.FirstName}?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        if (_context.Orders.Any(o => o.CustomerID == selectedCustomer.CustomerID))
                        {
                            MessageBox.Show("Нельзя удалить клиента, у которого есть заказы.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        var customerToDelete = _context.Customers.Find(selectedCustomer.CustomerID);
                        if (customerToDelete != null)
                        {
                            _context.Customers.Remove(customerToDelete);
                            _context.SaveChanges();
                            ReloadData();
                            MessageBox.Show("Клиент успешно удален!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void CustomersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool isSelected = CustomersGrid.SelectedItem != null;
            EditButton.IsEnabled = isSelected;
            DeleteButton.IsEnabled = isSelected && CanDelete;
        }
    }
}
