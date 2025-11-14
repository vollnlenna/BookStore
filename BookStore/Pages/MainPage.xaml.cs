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
    /// Логика взаимодействия для MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        private bookStoreEntities _context;
        public MainPage()
        {
            InitializeComponent();
            _context = new bookStoreEntities();
            LoadUserData();
        }

        private void LoadUserData()
        {
            if (App.CurrentUser != null)
            {
                var user = _context.Users
                    .Include("Stores")
                    .FirstOrDefault(u => u.UserID == App.CurrentUser.UserID);

                if (user != null)
                {
                    UserNameText.Text = App.CurrentUser.FullName;

                    if (user.Stores != null)
                    {
                        StoreAddressText.Text = user.Stores.Address;
                    }
                    else
                    {
                        StoreAddressText.Text = "Адрес не указан";
                    }

                    UsersButton.Visibility = user.RoleID == 2 ? Visibility.Visible : Visibility.Collapsed;

                    EditProfileButton.Visibility = user.RoleID == 2 ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            else
            {
                NavigationService?.Navigate(new LoginPage());
            }
        }

        private void EditProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentUser != null)
            {
                var currentUser = _context.Users.Find(App.CurrentUser.UserID);
                if (currentUser != null)
                {
                    var dialog = new AdminProfileEdit(currentUser);
                    if (dialog.ShowDialog() == true)
                    {
                        LoadUserData();
                        MessageBox.Show("Данные профиля успешно обновлены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            App.CurrentUser = null;

            if (NavigationService != null)
            {
                NavigationService.Navigate(new LoginPage());
            }
        }

        private void BooksButton_Click(object sender, RoutedEventArgs e)
        {
            MainContentFrame.Navigate(new BooksPage());
            SetActiveButton(BooksButton);
        }

        private void AuthorsButton_Click(object sender, RoutedEventArgs e)
        {
            MainContentFrame.Navigate(new AuthorsPage());
            SetActiveButton(AuthorsButton);
        }

        private void OrdersButton_Click(object sender, RoutedEventArgs e)
        {
            MainContentFrame.Navigate(new OrdersPage());
            SetActiveButton(OrdersButton);
        }

        private void CustomersButton_Click(object sender, RoutedEventArgs e)
        {
            MainContentFrame.Navigate(new CustomersPage());
            SetActiveButton(CustomersButton);
        }

        private void UsersButton_Click(object sender, RoutedEventArgs e)
        {
            MainContentFrame.Navigate(new UsersPage());
            SetActiveButton(UsersButton);
        }

        private void SetActiveButton(Button activeButton)
        {
            var buttons = new[] { BooksButton, AuthorsButton, OrdersButton, CustomersButton, UsersButton };

            foreach (var button in buttons)
            {
                if (button != null)
                {
                    button.Background = (SolidColorBrush)Application.Current.Resources["PrimaryBrush"];
                    button.Foreground = Brushes.White;
                }
            }

            if (activeButton != null)
            {
                activeButton.Background = (SolidColorBrush)Application.Current.Resources["AccentBrush"];
                activeButton.Foreground = Brushes.White;
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            BooksButton_Click(null, null);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _context?.Dispose();
        }
    }
}
