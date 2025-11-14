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

namespace BookStore.Pages
{
    /// <summary>
    /// Логика взаимодействия для LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Page
    {
        private bookStoreEntities _context;
        public LoginPage()
        {
            InitializeComponent();
            _context = new bookStoreEntities();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string login = EmailTextBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                ShowError("Пожалуйста, заполните все поля");
                return;
            }

            try
            {
                var user = _context.Users
                    .Include("Stores")
                    .FirstOrDefault(u => u.Email == login);

                if (user != null)
                {
                    if (BCrypt.Net.BCrypt.Verify(password, user.Password))
                    {
                        App.CurrentUser = new UserInfo
                        {
                            UserID = user.UserID,
                            Email = user.Email,
                            FullName = $"{user.LastName} {user.FirstName} {user.MiddleName}",
                            RoleID = user.RoleID,
                            StoreID = user.StoreID
                        };

                        ShowError("", false);

                        string roleName = user.RoleID == 1 ? "Сотрудник" :
                                         user.RoleID == 2 ? "Администратор" :
                                         "Неизвестная роль";


                        string message = $"Вы успешно авторизовались!\n\n" +
                                       $"Роль: {roleName}";

                        MessageBox.Show(message, "Успешная авторизация",
                                      MessageBoxButton.OK, MessageBoxImage.Information);

                        NavigationService?.Navigate(new MainPage());
                    }
                    else
                    {
                        ShowError("Неверный пароль");
                    }
                }
                else
                {
                    ShowError("Пользователь с таким email не найден");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Произошла ошибка: {ex.Message}");
            }
        }

        private void ShowError(string message, bool isVisible = true)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _context?.Dispose();
        }

    }
}
