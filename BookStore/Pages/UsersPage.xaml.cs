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
    /// Логика взаимодействия для UsersPage.xaml
    /// </summary>
    public partial class UsersPage : Page
    {
        private bookStoreEntities _context;
        private List<UserDisplayModel> _allEmployees;

        public UsersPage()
        {
            InitializeComponent();
            _context = new bookStoreEntities();
            LoadEmployees();
        }

        private void ReloadData()
        {
            _context?.Dispose();
            _context = new bookStoreEntities();
            LoadEmployees();
        }

        private void LoadEmployees()
        {
            try
            {
                int currentStoreId = App.CurrentUser?.StoreID ?? 0;

                var usersFromDb = _context.Users
                    .Where(u => u.StoreID == currentStoreId && u.RoleID != 2)
                    .ToList();

                _allEmployees = usersFromDb.Select(u => new UserDisplayModel
                {
                    UserID = u.UserID,
                    LastName = u.LastName,
                    FirstName = u.FirstName,
                    MiddleName = u.MiddleName,
                    Email = u.Email,
                    HasPassword = !string.IsNullOrEmpty(u.Password),
                    OriginalUser = u
                }).ToList();

                FilterEmployees();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки сотрудников: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterEmployees();
        }

        private void FilterEmployees()
        {
            string searchText = SearchTextBox.Text.ToLower();
            if (string.IsNullOrWhiteSpace(searchText))
            {
                EmployeesGrid.ItemsSource = _allEmployees;
            }
            else
            {
                string[] searchTerms = searchText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var filtered = _allEmployees.Where(u =>
                    searchTerms.All(term =>
                        (u.LastName ?? "").ToLower().Contains(term) ||
                        (u.FirstName ?? "").ToLower().Contains(term) ||
                        (u.MiddleName ?? "").ToLower().Contains(term)
                    ) ||
                    (u.Email ?? "").ToLower().Contains(searchText)
                ).ToList();
                EmployeesGrid.ItemsSource = filtered;
            }
        }

        private void AddEmployee_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new UserAddEdit();
            if (dialog.ShowDialog() == true)
            {
                ReloadData();
                MessageBox.Show("Сотрудник успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void EditEmployee_Click(object sender, RoutedEventArgs e)
        {
            if (EmployeesGrid.SelectedItem is UserDisplayModel selectedEmployee)
            {
                var dialog = new UserAddEdit(selectedEmployee.OriginalUser);
                if (dialog.ShowDialog() == true)
                {
                    ReloadData();
                    MessageBox.Show("Сотрудник успешно обновлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void DeleteEmployee_Click(object sender, RoutedEventArgs e)
        {
            if (EmployeesGrid.SelectedItem is UserDisplayModel selectedEmployee)
            {
                if (MessageBox.Show($"Вы уверены, что хотите удалить сотрудника {selectedEmployee.LastName} {selectedEmployee.FirstName}?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        var employeeToDelete = _context.Users.Find(selectedEmployee.OriginalUser.UserID);
                        if (employeeToDelete != null)
                        {
                            _context.Users.Remove(employeeToDelete);
                            _context.SaveChanges();
                            ReloadData();
                            MessageBox.Show("Сотрудник успешно удален!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void EmployeesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool isSelected = EmployeesGrid.SelectedItem != null;
            EditButton.IsEnabled = isSelected;
            DeleteButton.IsEnabled = isSelected;
        }
    }

    public class UserDisplayModel
    {
        public int UserID { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string Email { get; set; }
        public bool HasPassword { get; set; }
        public Users OriginalUser { get; set; }
    }
}
