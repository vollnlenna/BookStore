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
using System.Windows.Shapes;

namespace BookStore.Modules
{
    /// <summary>
    /// Логика взаимодействия для AuthorAddEdit.xaml
    /// </summary>
    public partial class AuthorAddEdit : Window
    {
        private readonly bookStoreEntities _context = new bookStoreEntities();
        private readonly Authors _editingAuthor; 
        public string WindowTitle { get; set; }

        public AuthorAddEdit()
        {
            InitializeComponent();
            DataContext = this;
            WindowTitle = "Добавление автора";
        }

        public AuthorAddEdit(Authors author)
        {
            InitializeComponent();
            DataContext = this;
            WindowTitle = "Редактирование автора";
            _editingAuthor = author; 
            LoadAuthorData(); 
        }

        private void LoadAuthorData()
        {
            if (_editingAuthor != null)
            {
                LastNameTextBox.Text = _editingAuthor.LastName;
                FirstNameTextBox.Text = _editingAuthor.FirstName;
                MiddleNameTextBox.Text = _editingAuthor.MiddleName;
                BirthYearTextBox.Text = _editingAuthor.BirthYear.ToString();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(LastNameTextBox.Text) ||
                string.IsNullOrWhiteSpace(FirstNameTextBox.Text) ||
                string.IsNullOrWhiteSpace(BirthYearTextBox.Text))
            {
                MessageBox.Show("Поля 'Фамилия', 'Имя' и 'Год рождения' обязательны для заполнения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(BirthYearTextBox.Text, out int birthYear))
            {
                MessageBox.Show("Год рождения должен быть числом.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            int currentYear = DateTime.Now.Year;
            if (birthYear < 1500 || birthYear > currentYear)
            {
                MessageBox.Show($"Год рождения должен быть в диапазоне от 1500 до {currentYear}.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string newLastName = LastNameTextBox.Text.Trim();
            string newFirstName = FirstNameTextBox.Text.Trim();
            string newMiddleName = MiddleNameTextBox.Text.Trim();

            if (_editingAuthor == null)
            {
                bool authorExists = _context.Authors.Any(a =>
                    a.LastName.ToLower() == newLastName.ToLower() &&
                    a.FirstName.ToLower() == newFirstName.ToLower() &&
                    (a.MiddleName ?? "").ToLower() == newMiddleName.ToLower()
                );

                if (authorExists)
                {
                    MessageBox.Show("Автор с таким ФИО уже существует в базе данных.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            try
            {
                if (_editingAuthor == null) 
                {
                    var newAuthor = new Authors
                    {
                        LastName = newLastName,
                        FirstName = newFirstName,
                        MiddleName = newMiddleName,
                        BirthYear = birthYear
                    };
                    _context.Authors.Add(newAuthor);
                }
                else 
                {
                    var authorInDb = _context.Authors.Find(_editingAuthor.AuthorID);
                    if (authorInDb != null)
                    {
                        authorInDb.LastName = newLastName;
                        authorInDb.FirstName = newFirstName;
                        authorInDb.MiddleName = newMiddleName;
                        authorInDb.BirthYear = birthYear;
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

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
        }
    }
}
