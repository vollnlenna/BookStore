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
    /// Логика взаимодействия для AdminProfileEdit.xaml
    /// </summary>
    public partial class AdminProfileEdit : Window
    {
        private bookStoreEntities _context;
        private int _userId;

        public AdminProfileEdit(Users user)
        {
            InitializeComponent();
            _context = new bookStoreEntities();
            _userId = user.UserID;
            LoadUserData();
        }

        private void LoadUserData()
        {
            var user = _context.Users.Find(_userId);
            if (user != null)
            {
                LastNameTextBox.Text = user.LastName;
                FirstNameTextBox.Text = user.FirstName;
                MiddleNameTextBox.Text = user.MiddleName;
                EmailTextBox.Text = user.Email;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            try
            {
                var userToUpdate = _context.Users.Find(_userId);
                if (userToUpdate != null)
                {
                    userToUpdate.LastName = LastNameTextBox.Text.Trim();
                    userToUpdate.FirstName = FirstNameTextBox.Text.Trim();
                    userToUpdate.MiddleName = MiddleNameTextBox.Text.Trim();
                    userToUpdate.Email = EmailTextBox.Text.Trim();

                    if (!string.IsNullOrWhiteSpace(PasswordTextBox.Text))
                    {
                        userToUpdate.Password = BCrypt.Net.BCrypt.HashPassword(PasswordTextBox.Text);
                    }

                    _context.SaveChanges();

                    if (App.CurrentUser != null && App.CurrentUser.UserID == _userId)
                    {
                        App.CurrentUser.LastName = userToUpdate.LastName;
                        App.CurrentUser.FirstName = userToUpdate.FirstName;
                        App.CurrentUser.MiddleName = userToUpdate.MiddleName;
                        App.CurrentUser.Email = userToUpdate.Email;
                    }

                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(LastNameTextBox.Text))
            {
                MessageBox.Show("Введите фамилию", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                LastNameTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text))
            {
                MessageBox.Show("Введите имя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                FirstNameTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
            {
                MessageBox.Show("Введите email", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                EmailTextBox.Focus();
                return false;
            }

            string email = EmailTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(email) && !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                MessageBox.Show("Введен некорректный Email адрес.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                EmailTextBox.Focus();
                return false;
            }

            var currentUser = _context.Users.Find(_userId);
            if (currentUser != null && email != currentUser.Email)
            {
                if (_context.Users.Any(u => u.Email == email && u.UserID != _userId))
                {
                    MessageBox.Show("Пользователь с таким email уже существует", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    EmailTextBox.Focus();
                    return false;
                }
            }

            return true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
