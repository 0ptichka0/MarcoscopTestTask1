using System.Windows;

namespace MarcoscopTestTask
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Устанавливаем DataContext в code-behind
            DataContext = new MainViewModel();
        }
    }
}