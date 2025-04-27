using System.Windows;

namespace PlantUMLEditor.Views
{
    public partial class TemplateDialog : Window
    {
        public string TemplateName { get; private set; }
        public string TemplateCode { get; private set; }

        public TemplateDialog(string defaultName)
        {
            InitializeComponent();

            txtTemplateName.Text = defaultName;
        }

        public TemplateDialog(string defaultName, string templateCode)
        {
            InitializeComponent();

            txtTemplateName.Text = defaultName;
            txtTemplateCode.Text = templateCode;
            TemplateCode = templateCode;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTemplateName.Text))
            {
                MessageBox.Show("Molimo unesite ime predloška.", "Upozorenje",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtTemplateCode.Text))
            {
                MessageBox.Show("Molimo unesite kod predloška.", "Upozorenje",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TemplateName = txtTemplateName.Text;
            TemplateCode = txtTemplateCode.Text;
            DialogResult = true;
        }
    }
}
