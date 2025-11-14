using BookStore.Modules;
using System;
using System.Collections.Generic;
using System.Data.Entity;
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
    /// Логика взаимодействия для AuthorsPage.xaml
    /// </summary>
    public partial class AuthorsPage : Page
    {
        private bookStoreEntities _context;
        private List<Authors> _allAuthors;
        private bool CanDelete => App.CurrentUser?.RoleID == 2;

        public AuthorsPage()
        {
            InitializeComponent();
            _context = new bookStoreEntities();
            LoadAuthors();
            UpdateUI();
        }

        private void ReloadData()
        {
            _context?.Dispose();
            _context = new bookStoreEntities();
            LoadAuthors();
        }

        private void LoadAuthors()
        {
            try
            {
                _allAuthors = _context.Authors.ToList();
                FilterAuthors(); 
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки авторов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateUI()
        {
            DeleteButton.Visibility = CanDelete ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterAuthors();
        }

        private void FilterAuthors()
        {
            string searchText = SearchTextBox.Text.ToLower();
            if (string.IsNullOrWhiteSpace(searchText))
            {
                AuthorsGrid.ItemsSource = _allAuthors;
            }
            else
            {
                var filtered = _allAuthors.Where(a =>
                    (a.LastName ?? "").ToLower().Contains(searchText) ||
                    (a.FirstName ?? "").ToLower().Contains(searchText) ||
                    (a.MiddleName ?? "").ToLower().Contains(searchText) ||
                    a.BirthYear.ToString().Contains(searchText)
                ).ToList();
                AuthorsGrid.ItemsSource = filtered;
            }
        }

        private void AddAuthor_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AuthorAddEdit();
            if (dialog.ShowDialog() == true)
            {
                ReloadData();
                MessageBox.Show("Автор успешно добавлен!", "Успех",
                                 MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void EditAuthor_Click(object sender, RoutedEventArgs e)
        {
            if (AuthorsGrid.SelectedItem is Authors selectedAuthor)
            {
                var dialog = new AuthorAddEdit(selectedAuthor);
                if (dialog.ShowDialog() == true)
                {
                    ReloadData();
                    MessageBox.Show("Автор успешно обновлен!", "Успех",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void DeleteAuthor_Click(object sender, RoutedEventArgs e)
        {
            if (!CanDelete) return;

            if (AuthorsGrid.SelectedItem is Authors selectedAuthor)
            {
                if (MessageBox.Show($"Вы уверены, что хотите удалить автора {selectedAuthor.LastName} {selectedAuthor.FirstName}?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        if (_context.Books.Any(b => b.AuthorID == selectedAuthor.AuthorID))
                        {
                            MessageBox.Show("Нельзя удалить автора, у которого есть книги.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        var authorToDelete = _context.Authors.Find(selectedAuthor.AuthorID);
                        if (authorToDelete != null)
                        {
                            _context.Authors.Remove(authorToDelete);
                            _context.SaveChanges();
                            ReloadData();
                            MessageBox.Show("Автор успешно удален!", "Успех",
                                            MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void AuthorsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool isSelected = AuthorsGrid.SelectedItem != null;
            EditButton.IsEnabled = isSelected;
            DeleteButton.IsEnabled = isSelected && CanDelete;
        }
    }
}
