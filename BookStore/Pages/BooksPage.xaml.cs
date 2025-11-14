using BookStore.Modules;
using System;
using System.Collections.Generic;
using System.IO;
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
    /// Логика взаимодействия для BooksPage.xaml
    /// </summary>
    public partial class BooksPage : Page
    {
        private bookStoreEntities _context;
        private List<BookViewModel> _allBooks;
        private BookViewModel _selectedBook;
        public BooksPage()
        {
            InitializeComponent();
            _context = new bookStoreEntities();
            LoadBooks();
            CheckUserRole();
        }

        private void LoadBooks()
        {
            try
            {
                var currentStoreId = App.CurrentUser?.StoreID ?? 1;

                _allBooks = _context.StoreBooks
                    .Include("Books")
                    .Include("Books.Authors")
                    .Where(sb => sb.StoreID == currentStoreId)
                    .Select(sb => new BookViewModel
                    {
                        StoreBookID = sb.StoreBookID,
                        BookID = sb.BookID,
                        Title = sb.Books.Title,
                        AuthorName = sb.Books.Authors.LastName + " " +
                                   sb.Books.Authors.FirstName + " " +
                                   (sb.Books.Authors.MiddleName ?? ""),
                        PublicationYear = sb.Books.PublicationYear,
                        PageCount = sb.Books.PageCount,
                        Description = sb.Books.Description ?? "Описание отсутствует",
                        Cover = sb.Books.Cover,
                        Price = sb.Price,
                        Quantity = sb.Quantity ?? 0,
                        IsAvailable = sb.IsAvailable
                    })
                    .ToList();

                BooksItemsControl.ItemsSource = _allBooks;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки книг: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CheckUserRole()
        {
            if (App.CurrentUser?.RoleID == 1)
            {
                DeleteBookButton.Visibility = Visibility.Collapsed;
            }
        }

        private void BooksItemsControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var originalSource = e.OriginalSource as DependencyObject;
            var book = FindParentBook(originalSource);

            if (book != null)
            {
                SelectBook(book);
            }
        }

        private BookViewModel FindParentBook(DependencyObject child)
        {
            var parent = VisualTreeHelper.GetParent(child);

            while (parent != null)
            {
                if (parent is FrameworkElement frameworkElement && frameworkElement.DataContext is BookViewModel book)
                {
                    return book;
                }
                parent = VisualTreeHelper.GetParent(parent);
            }

            return null;
        }

        private void SelectBook(BookViewModel book)
        {
            foreach (var item in _allBooks)
            {
                item.IsSelected = false;
            }

            book.IsSelected = true;
            _selectedBook = book;

            RefreshBooksList();
        }

        private void RefreshBooksList()
        {
            var currentItems = BooksItemsControl.ItemsSource;
            BooksItemsControl.ItemsSource = null;
            BooksItemsControl.ItemsSource = currentItems;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = SearchTextBox.Text.ToLower();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                BooksItemsControl.ItemsSource = _allBooks;
            }
            else
            {
                var filteredBooks = _allBooks.Where(b =>
                    b.Title.ToLower().Contains(searchText) ||
                    b.AuthorName.ToLower().Contains(searchText)
                ).ToList();

                BooksItemsControl.ItemsSource = filteredBooks;
            }
        }

        private void AddBookButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addWindow = new BookAddEdit();
                addWindow.Owner = Window.GetWindow(this);

                if (addWindow.ShowDialog() == true)
                {
                    LoadBooks();
                    MessageBox.Show("Книга успешно добавлена!", "Успех",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении книги: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditBookButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedBook == null)
            {
                MessageBox.Show("Выберите книгу для редактирования", "Внимание",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var editWindow = new BookAddEdit(_selectedBook);
                editWindow.Owner = Window.GetWindow(this);

                if (editWindow.ShowDialog() == true)
                {
                    LoadBooks();
                    _selectedBook = null;
                    RefreshBooksList();

                    MessageBox.Show("Книга успешно обновлена!", "Успех",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при редактировании книги: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteBookButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedBook == null)
            {
                MessageBox.Show("Выберите книгу для удаления", "Внимание",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить книгу '{_selectedBook.Title}'?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new bookStoreEntities())
                    {
                        using (var transaction = context.Database.BeginTransaction())
                        {
                            try
                            {
                                var storeBook = context.StoreBooks
                                    .Include("OrderItems")
                                    .FirstOrDefault(sb => sb.StoreBookID == _selectedBook.StoreBookID);

                                if (storeBook == null)
                                {
                                    MessageBox.Show("Книга не найдена в базе данных", "Ошибка",
                                                  MessageBoxButton.OK, MessageBoxImage.Error);
                                    return;
                                }

                                if (storeBook.OrderItems != null && storeBook.OrderItems.Any())
                                {
                                    MessageBox.Show(
                                        "Невозможно удалить книгу, так как с ней связаны заказы.\nСначала удалите связанные заказы.",
                                        "Ошибка удаления",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Error
                                    );
                                    return;
                                }

                                context.StoreBooks.Remove(storeBook);

                                bool usedInOtherStores = context.StoreBooks
                                    .Any(sb => sb.BookID == _selectedBook.BookID && sb.StoreBookID != _selectedBook.StoreBookID);

                                if (!usedInOtherStores)
                                {
                                    var book = context.Books
                                        .Include("StoreBooks")
                                        .FirstOrDefault(b => b.BookID == _selectedBook.BookID);

                                    if (book != null && (book.StoreBooks == null || !book.StoreBooks.Any()))
                                    {
                                        context.Books.Remove(book);

                                        try
                                        {
                                            if (!string.IsNullOrEmpty(book.Cover))
                                            {
                                                var coverPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "Covers", book.Cover);
                                                if (File.Exists(coverPath))
                                                {
                                                    File.Delete(coverPath);
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Ошибка удаления файла обложки: {ex.Message}");
                                        }
                                    }
                                }

                                context.SaveChanges();
                                transaction.Commit();

                                LoadBooks();
                                _selectedBook = null;

                                MessageBox.Show("Книга успешно удалена!", "Успех",
                                              MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            catch (System.Data.Entity.Infrastructure.DbUpdateException dbEx)
                            {
                                transaction.Rollback();
                                var innerException = dbEx.InnerException?.InnerException ?? dbEx.InnerException ?? dbEx;

                                if (innerException.Message.Contains("REFERENCE constraint"))
                                {
                                    MessageBox.Show(
                                        "Невозможно удалить книгу, так как с ней связаны заказы или другие данные.\nСначала удалите связанные записи.",
                                        "Ошибка удаления",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Error
                                    );
                                }
                                else
                                {
                                    MessageBox.Show($"Ошибка базы данных: {innerException.Message}", "Ошибка",
                                                  MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                MessageBox.Show($"Ошибка при удалении книги: {ex.Message}", "Ошибка",
                                              MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении книги: {ex.Message}", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _context?.Dispose();
        }
    }

    public class BookViewModel
    {
        public int StoreBookID { get; set; }
        public int BookID { get; set; }
        public string Title { get; set; }
        public string AuthorName { get; set; }
        public int PublicationYear { get; set; }
        public int PageCount { get; set; }
        public string Description { get; set; }
        public string Cover { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsSelected { get; set; }

        public string StatusText
        {
            get
            {
                if (IsAvailable && Quantity > 0)
                    return $"В наличии: {Quantity}";
                else
                    return "Нет в наличии";
            }
        }

        public SolidColorBrush StatusColor
        {
            get
            {
                if (IsAvailable && Quantity > 0)
                    return new SolidColorBrush(Color.FromRgb(76, 175, 80)); 
                else
                    return new SolidColorBrush(Color.FromRgb(244, 67, 54)); 
            }
        }

        public BitmapImage CoverImage
        {
            get
            {
                try
                {
                    string imagePath;

                    if (string.IsNullOrEmpty(Cover))
                    {
                        string projectDir = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)
                                                     .Parent?.Parent?.FullName
                                                 ?? AppDomain.CurrentDomain.BaseDirectory;

                        imagePath = System.IO.Path.Combine(projectDir, "Images", "default-cover.jpg");
                    }
                    else
                    {
                        imagePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "Covers", Cover);

                        if (!File.Exists(imagePath))
                        {
                            string projectDir = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)
                                                         .Parent?.Parent?.FullName
                                                     ?? AppDomain.CurrentDomain.BaseDirectory;
                            imagePath = System.IO.Path.Combine(projectDir, "Images", "default-cover.jpg");
                        }
                    }

                    if (!File.Exists(imagePath))
                        return null;

                    BitmapImage bitmap;
                    using (var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
                    {
                        bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = stream;
                        bitmap.EndInit();
                        bitmap.Freeze();
                    }

                    return bitmap;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка загрузки обложки: {ex.Message}");
                    return null;
                }
            }
        }


        public SolidColorBrush CardBackground
        {
            get
            {
                if (IsAvailable && Quantity > 0)
                    return new SolidColorBrush(Colors.White); 
                else
                    return new SolidColorBrush(Color.FromRgb(240, 240, 240));
            }
        }

        public double CardOpacity
        {
            get
            {
                if (IsAvailable && Quantity > 0)
                    return 1.0; 
                else
                    return 0.7;
            }
        }

        public SolidColorBrush TextColor
        {
            get
            {
                if (IsAvailable && Quantity > 0)
                    return new SolidColorBrush(Color.FromRgb(26, 26, 26)); 
                else
                    return new SolidColorBrush(Color.FromRgb(100, 100, 100)); 
            }
        }

        public SolidColorBrush BorderColor => IsSelected ?
            new SolidColorBrush(Color.FromRgb(44, 85, 48)) : 
            new SolidColorBrush(Color.FromRgb(224, 224, 224)); 

        public Thickness BorderThickness => IsSelected ? new Thickness(2) : new Thickness(1);
    }
}
