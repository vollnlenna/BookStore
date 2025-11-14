using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BookStore.Modules
{
    /// <summary>
    /// Логика взаимодействия для CustomerAddEdit.xaml
    /// </summary>
    public partial class CustomerAddEdit : Window
    {
        private readonly bookStoreEntities _context = new bookStoreEntities();
        private readonly Customers _editingCustomer;
        public string WindowTitle { get; set; }

        public CustomerAddEdit()
        {
            InitializeComponent();
            DataContext = this;
            WindowTitle = "Добавление клиента";
        }

        public CustomerAddEdit(Customers customer)
        {
            InitializeComponent();
            DataContext = this;
            WindowTitle = "Редактирование клиента";
            _editingCustomer = customer;
            LoadCustomerData();
        }

        private void LoadCustomerData()
        {
            if (_editingCustomer != null)
            {
                LastNameTextBox.Text = _editingCustomer.LastName;
                FirstNameTextBox.Text = _editingCustomer.FirstName;
                MiddleNameTextBox.Text = _editingCustomer.MiddleName;
                PhoneTextBox.Text = _editingCustomer.Phone;
                EmailTextBox.Text = _editingCustomer.Email;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(LastNameTextBox.Text) ||
                string.IsNullOrWhiteSpace(FirstNameTextBox.Text) ||
                string.IsNullOrWhiteSpace(PhoneTextBox.Text))
            {
                MessageBox.Show("Поля 'Фамилия', 'Имя' и 'Телефон' обязательны для заполнения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string email = EmailTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(email) && !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                MessageBox.Show("Введен некорректный Email адрес.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string newPhone = PhoneTextBox.Text.Trim();
            if (!Regex.IsMatch(newPhone, @"^(\+7\d{10}|8\d{10})$"))
            {
                MessageBox.Show("Номер телефона должен быть в формате +7XXXXXXXXXX (12 символов) или 8XXXXXXXXXX (11 символов).", "Ошибка формата", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string newLastName = LastNameTextBox.Text.Trim();
            string newFirstName = FirstNameTextBox.Text.Trim();
            string newMiddleName = MiddleNameTextBox.Text.Trim();


            string phoneSuffix = newPhone.Length == 12 ? newPhone.Substring(2) : newPhone.Substring(1);

            string variantWith8 = "8" + phoneSuffix;
            string variantWithPlus7 = "+7" + phoneSuffix;

            var duplicateQuery = _context.Customers
                .Where(c => c.Phone == variantWith8 || c.Phone == variantWithPlus7);

            if (_editingCustomer != null)
            {
                duplicateQuery = duplicateQuery.Where(c => c.CustomerID != _editingCustomer.CustomerID);
            }

            if (duplicateQuery.Any())
            {
                MessageBox.Show("Клиент с таким номером телефона (возможно, в другом формате) уже существует.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                if (_editingCustomer == null)
                {
                    var newCustomer = new Customers
                    {
                        LastName = newLastName,
                        FirstName = newFirstName,
                        MiddleName = newMiddleName,
                        Phone = newPhone,
                        Email = email
                    };
                    _context.Customers.Add(newCustomer);
                }
                else
                {
                    var customerInDb = _context.Customers.Find(_editingCustomer.CustomerID);
                    if (customerInDb != null)
                    {
                        customerInDb.LastName = newLastName;
                        customerInDb.FirstName = newFirstName;
                        customerInDb.MiddleName = newMiddleName;
                        customerInDb.Phone = newPhone;
                        customerInDb.Email = email;
                    }
                }

                _context.SaveChanges();
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
