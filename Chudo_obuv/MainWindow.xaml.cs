using Chudo_obuv.Helpers;
using Chudo_obuv.ScaffoldedModels;
using Chudo_obuv.Statics;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Chudo_obuv
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ScaffoldedChudoObuvContext _db = new ScaffoldedChudoObuvContext();
        private MessageHelper _mh = new MessageHelper();
        public MainWindow()
        {
            InitializeComponent();
        }

        private void LogInButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginEnter.Text.Trim();
            if (login.Length == 0)
            {
                _mh.ShowError("Введите логин");
                return;
            }

            var user = _db.Users.FirstOrDefault(u => u.Login == login);
            
            if (user == null)
            {
                _mh.ShowError("Пользователь с таким логином не найден");
                return;
            }
            else
            {
                CurrentSession.CurrentUser = user;
                new ShoesWindow(user).Show();
                Close();
            }
        }

        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            CurrentSession.CurrentUser = null;
            new ShoesWindow().Show();
            Close();
        }
    }
}
