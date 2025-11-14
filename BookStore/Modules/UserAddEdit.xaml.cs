using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Shapes;

namespace BookStore.Modules
{
    /// <summary>
    /// Логика взаимодействия для UserAddEdit.xaml
    /// </summary>
    public partial class UserAddEdit : Window, INotifyPropertyChanged
    {
        private bookStoreEntities _context;
        private Users _user;
        private bool _isEditMode;
        private string _originalPasswordHash;

        public event PropertyChangedEventHandler PropertyChanged;

        public string WindowTitle => _isEditMode ? "Редактирование сотрудника" : "Добавление сотрудника";

        public bool IsEditMode
        {
            get { return _isEditMode; }
            set
            {
                _isEditMode = value;
                OnPropertyChanged(nameof(IsEditMode));
            }
        }

        public UserAddEdit()
        {
            InitializeComponent();
            _context = new bookStoreEntities();
            IsEditMode = false;
            DataContext = this;
        }

        public UserAddEdit(Users user) : this()
        {
            _user = user;
            IsEditMode = true;
            _originalPasswordHash = user.Password;
            LoadUserData();
        }

        private void LoadUserData()
        {
            if (_user != null)
            {
                LastNameTextBox.Text = _user.LastName;
                FirstNameTextBox.Text = _user.FirstName;
                MiddleNameTextBox.Text = _user.MiddleName;
                EmailTextBox.Text = _user.Email;
                PasswordTextBox.Text = "";
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            try
            {
                if (IsEditMode)
                {
                    var userToUpdate = _context.Users.Find(_user.UserID);
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
                    }
                }
                else
                {
                    var newUser = new Users
                    {
                        LastName = LastNameTextBox.Text.Trim(),
                        FirstName = FirstNameTextBox.Text.Trim(),
                        MiddleName = MiddleNameTextBox.Text.Trim(),
                        Email = EmailTextBox.Text.Trim(),
                        Password = BCrypt.Net.BCrypt.HashPassword(PasswordTextBox.Text),
                        StoreID = App.CurrentUser?.StoreID ?? 0,
                        RoleID = 1
                    };

                    _context.Users.Add(newUser);
                }

                _context.SaveChanges();
                DialogResult = true;
                Close();
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
            if (!string.IsNullOrEmpty(email) && !System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                MessageBox.Show("Введен некорректный Email адрес.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                EmailTextBox.Focus();
                return false;
            }

            if (!IsEditMode && string.IsNullOrWhiteSpace(PasswordTextBox.Text))
            {
                MessageBox.Show("Введите пароль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                PasswordTextBox.Focus();
                return false;
            }

            if (!IsEditMode || (IsEditMode && email != _user.Email))
            {
                if (_context.Users.Any(u => u.Email == email))
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
