using System.Windows;

namespace PlantUMLEditor
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

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTemplateName.Text))
            {
                MessageBox.Show("Molimo unesite ime predloška.", "Upozorenje",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TemplateName = txtTemplateName.Text;
            TemplateCode = txtTemplateCode.Text;
            DialogResult = true;
        }
    }
}
