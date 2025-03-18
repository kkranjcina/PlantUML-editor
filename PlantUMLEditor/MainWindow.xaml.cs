using System;
using System.Windows;

namespace PlantUMLEditor
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void SendChatGPTQuery_Click(object sender, RoutedEventArgs e)
        {
            string input = ChatInputTextBox.Text;

            if (string.IsNullOrWhiteSpace(input))
            {
                MessageBox.Show("Unesite tekst upita.");
                return;
            }
        }

        private void GenerateDiagram_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string umlCode = PlantUmlCodeTextBox.Text;
                if (string.IsNullOrWhiteSpace(umlCode))
                {
                    MessageBox.Show("Unesite PlantUML kod.", "Pogreška", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Došlo je do pogreške: {ex.Message}", "Pogreška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
