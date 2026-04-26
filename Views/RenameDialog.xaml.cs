using System.Windows;
using System.Windows.Input;

namespace TienViewer.Views
{
    public partial class RenameDialog : Window
    {
        public string NewName => NameBox.Text.Trim();

        public RenameDialog(string currentName)
        {
            InitializeComponent();
            NameBox.Text = currentName;
            Loaded += (s, e) =>
            {
                NameBox.Focus();
                // 확장자 앞까지만 선택
                int dot = currentName.LastIndexOf('.');
                NameBox.Select(0, dot > 0 ? dot : currentName.Length);
            };
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewName)) return;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void NameBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) Ok_Click(this, new RoutedEventArgs());
            if (e.Key == Key.Escape) DialogResult = false;
        }
    }
}
