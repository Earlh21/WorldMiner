using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WorldMiner.Views
{
    public partial class MainWindow : Window
    {
        public static MainWindow Instance { get; set; }
        
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            Instance = this;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}