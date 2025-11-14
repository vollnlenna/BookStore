using BookStore.Pages;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
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
    /// Логика взаимодействия для BookAddEdit.xaml
    /// </summary>
    public partial class BookAddEdit : Window
    {
        public BookEditViewModel EditModel { get; set; }
        private bookStoreEntities _context;
        private bool _isEditMode;
        private string _selectedImagePath;
        public BookAddEdit(BookViewModel book = null)
        {
            InitializeComponent();
            _context = new bookStoreEntities();

            _isEditMode = book != null;
            this.Title = _isEditMode ? "Редактирование книги" : "Добавление книги";

            EditModel = new BookEditViewModel
            {
                WindowTitle = this.Title,
                IsEditMode = _isEditMode
            };

            if (_isEditMode)
            {
                EditModel.StoreBookID = book.StoreBookID;
                EditModel.BookID = book.BookID;
                EditModel.Title = book.Title;
                EditModel.PublicationYear = book.PublicationYear;
                EditModel.PageCount = book.PageCount;
                EditModel.Description = book.Description;
                EditModel.Cover = book.Cover;
                EditModel.Price = (int?)book.Price;
                EditModel.Quantity = book.Quantity;
                EditModel.IsAvailable = book.IsAvailable;

                if (!string.IsNullOrEmpty(book.Cover))
                {
                    LoadCoverPreview(book.Cover);
                }

                var bookEntity = _context.Books.FirstOrDefault(b => b.BookID == book.BookID);
                if (bookEntity != null)
                {
                    EditModel.AuthorID = bookEntity.AuthorID;
                }
            }
            else
            {
                EditModel.IsAvailable = true;
            }

            DataContext = EditModel;
            LoadAuthors();
        }


        private void LoadCoverPreview(string coverFileName)
        {
            try
            {
                string debugPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "Covers", coverFileName);
                string projectPath = System.IO.Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.FullName, "Images", "Covers", coverFileName);

                string imagePath = File.Exists(debugPath) ? debugPath : projectPath;

                if (File.Exists(imagePath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(imagePath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();

                    EditModel.CoverPreview = bitmap;
                    _selectedImagePath = imagePath;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки превью: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LoadAuthors()
        {
            try
            {
                var authors = _context.Authors
                    .ToList()
                    .Select(a => new
                    {
                        a.AuthorID,
                        FullName = $"{a.LastName} {a.FirstName} {a.MiddleName ?? ""}"
                    })
                    .OrderBy(a => a.FullName)
                    .ToList();

                AuthorComboBox.ItemsSource = authors;

                if (EditModel.AuthorID > 0)
                {
                    AuthorComboBox.SelectedValue = EditModel.AuthorID;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки авторов: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Изображения (*.jpg;*.png;*.jpeg;*.bmp)|*.jpg;*.png;*.jpeg;*.bmp",
                Title = "Выберите обложку книги"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var fileInfo = new FileInfo(dialog.FileName);

                    if (fileInfo.Length > 2 * 1024 * 1024)
                    {
                        MessageBox.Show("Размер файла не должен превышать 2 МБ", "Ошибка",
                                      MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    _selectedImagePath = dialog.FileName;

                    var fileName = Guid.NewGuid().ToString() + System.IO.Path.GetExtension(_selectedImagePath);
                    EditModel.Cover = fileName;

                    LoadSelectedImagePreview();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при выборе файла: {ex.Message}", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadSelectedImagePreview()
        {
            if (!string.IsNullOrEmpty(_selectedImagePath) && File.Exists(_selectedImagePath))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(_selectedImagePath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();

                    EditModel.CoverPreview = bitmap;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки превью: {ex.Message}", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void RemoveCoverButton_Click(object sender, RoutedEventArgs e)
        {
            EditModel.Cover = null;
            EditModel.CoverPreview = null;
            _selectedImagePath = null;
        }

        #region Input Validation & Formatting

        private void NumericOnly(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _);
        }

        private void QuantityTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var tb = sender as TextBox;
            string newText = tb.Text.Insert(tb.SelectionStart, e.Text);

            e.Handled = !Regex.IsMatch(newText, "^[1-9][0-9]*$");
        }

        private void QuantityTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var expr = QuantityTextBox.GetBindingExpression(TextBox.TextProperty);
            Validation.ClearInvalid(expr);
        }

        private void QuantityTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (QuantityTextBox.Text == "0")
                QuantityTextBox.Text = "";
        }

        private void AvailableCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (!EditModel.IsAvailable)
            {
                EditModel.Quantity = null;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm())
                return;

            try
            {
                if (!string.IsNullOrEmpty(_selectedImagePath) && !string.IsNullOrEmpty(EditModel.Cover))
                {
                    CopyCoverImage();
                }

                if (_isEditMode)
                {
                    UpdateBook();
                }
                else
                {
                    AddBook();
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CopyCoverImage()
        {
            var coversDirectory = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "Covers");

            if (!Directory.Exists(coversDirectory))
            {
                Directory.CreateDirectory(coversDirectory);
            }

            var destinationPath = System.IO.Path.Combine(coversDirectory, EditModel.Cover);

            try
            {
                File.Copy(_selectedImagePath, destinationPath, true);

                try
                {
                    LoadCoverPreview(EditModel.Cover);
                }
                catch
                {
                }
            }
            catch
            {
            }
        }

        private void AddBook()
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var newBook = new Books
                    {
                        Title = EditModel.Title,
                        AuthorID = EditModel.AuthorID.Value,
                        PublicationYear = EditModel.PublicationYear.Value,
                        PageCount = EditModel.PageCount.Value,
                        Description = EditModel.Description,
                        Cover = EditModel.Cover
                    };

                    _context.Books.Add(newBook);
                    _context.SaveChanges();

                    var currentStoreId = App.CurrentUser?.StoreID ?? 1;
                    var storeBook = new StoreBooks
                    {
                        StoreID = currentStoreId,
                        BookID = newBook.BookID,
                        Price = (int)EditModel.Price.Value,
                        Quantity = EditModel.IsAvailable ? EditModel.Quantity : null,
                        IsAvailable = EditModel.IsAvailable
                    };

                    _context.StoreBooks.Add(storeBook);
                    _context.SaveChanges();

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        private void UpdateBook()
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var book = _context.Books.FirstOrDefault(b => b.BookID == EditModel.BookID);
                    if (book != null)
                    {
                        book.Title = EditModel.Title;
                        book.AuthorID = EditModel.AuthorID.Value;
                        book.PublicationYear = EditModel.PublicationYear.Value;
                        book.PageCount = EditModel.PageCount.Value;
                        book.Description = EditModel.Description;
                        book.Cover = EditModel.Cover;
                    }

                    var storeBook = _context.StoreBooks.FirstOrDefault(sb => sb.StoreBookID == EditModel.StoreBookID);
                    if (storeBook != null)
                    {
                        storeBook.Price = (int)EditModel.Price.Value;
                        storeBook.Quantity = EditModel.IsAvailable ? EditModel.Quantity : null;
                        storeBook.IsAvailable = EditModel.IsAvailable;
                    }

                    _context.SaveChanges();
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(EditModel.Title))
            {
                MessageBox.Show("Введите название книги", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                TitleTextBox.Focus();
                return false;
            }

            if (!EditModel.AuthorID.HasValue || EditModel.AuthorID <= 0)
            {
                MessageBox.Show("Выберите автора", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                AuthorComboBox.Focus();
                return false;
            }

            if (!EditModel.PublicationYear.HasValue || EditModel.PublicationYear < 1500 || EditModel.PublicationYear > DateTime.Now.Year)
            {
                MessageBox.Show("Введите корректный год издания (от 1500 до текущего года)", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                YearTextBox.Focus();
                return false;
            }

            if (!EditModel.PageCount.HasValue || EditModel.PageCount <= 0)
            {
                MessageBox.Show("Введите корректное количество страниц", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                PagesTextBox.Focus();
                return false;
            }

            if (!EditModel.Price.HasValue || EditModel.Price <= 0)
            {
                MessageBox.Show("Введите корректную цену", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                PriceTextBox.Focus();
                return false;
            }

            if (EditModel.IsAvailable && (!EditModel.Quantity.HasValue))
            {
                MessageBox.Show("Введите количество для книги в наличии", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                QuantityTextBox.Focus();
                return false;
            }

            return true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void PriceTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            Validation.ClearInvalid(PriceTextBox.GetBindingExpression(TextBox.TextProperty));
        }
        #endregion
    }

    public class BookEditViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _title;
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged(nameof(Title));
            }
        }

        private int? _publicationYear;
        public int? PublicationYear
        {
            get => _publicationYear;
            set
            {
                _publicationYear = value;
                OnPropertyChanged(nameof(PublicationYear));
            }
        }

        private int? _pageCount;
        public int? PageCount
        {
            get => _pageCount;
            set
            {
                _pageCount = value;
                OnPropertyChanged(nameof(PageCount));
            }
        }

        private int? _price;
        public int? Price
        {
            get => _price;
            set
            {
                _price = value;
                OnPropertyChanged(nameof(Price));
            }
        }

        private int? _quantity;
        public int? Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged(nameof(Quantity));
            }
        }

        private bool _isAvailable;
        public bool IsAvailable
        {
            get => _isAvailable;
            set
            {
                _isAvailable = value;
                OnPropertyChanged(nameof(IsAvailable));
                OnPropertyChanged(nameof(QuantityVisibility));
            }
        }

        public Visibility QuantityVisibility => IsAvailable ? Visibility.Visible : Visibility.Collapsed;

        private BitmapImage _coverPreview;
        public BitmapImage CoverPreview
        {
            get => _coverPreview;
            set
            {
                _coverPreview = value;
                OnPropertyChanged(nameof(CoverPreview));
            }
        }

        private string _cover;
        public string Cover
        {
            get => _cover;
            set
            {
                _cover = value;
                OnPropertyChanged(nameof(Cover));
            }
        }

        public string WindowTitle { get; set; }
        public bool IsEditMode { get; set; }
        public int StoreBookID { get; set; }
        public int BookID { get; set; }
        public int? AuthorID { get; set; }
        public string Description { get; set; }
    }
}
