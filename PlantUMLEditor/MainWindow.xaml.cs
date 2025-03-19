using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace PlantUMLEditor
{
    public partial class MainWindow : Window
    {
        private string _plantUmlJarPath;
        private readonly string _outputDirectory = Path.Combine(Path.GetTempPath(), "PlantUML");
        private System.Windows.Controls.Image _currentImage;

        public MainWindow()
        {
            InitializeComponent();
            GetSolutionDirectory();
        }

        private void GetSolutionDirectory()
        {
            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string solutionDirectory = "";

            while (!string.IsNullOrEmpty(currentDirectory))
            {
                if (Directory.GetFiles(currentDirectory, "*.sln").Any())
                {
                    solutionDirectory = currentDirectory;
                    break;
                }

                currentDirectory = Directory.GetParent(currentDirectory)?.FullName;
            }

            _plantUmlJarPath = Path.Combine(solutionDirectory, "plantuml-1.2025.2.jar");

            if (!File.Exists(_plantUmlJarPath))
            {
                MessageBox.Show($"Datoteka PlantUML nije pronađena na lokaciji: {_plantUmlJarPath}", "Pogreška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateDiagram_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string umlCode = txtPlantUmlCode.Text;
                if (string.IsNullOrWhiteSpace(umlCode))
                {
                    MessageBox.Show("Unesite PlantUML kod.", "Pogreška", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_currentImage != null)
                {
                    _currentImage.Source = null;
                }

                string diagramPath = GeneratePlantUmlDiagram(umlCode);

                GC.Collect();
                GC.WaitForPendingFinalizers();

                var bitmapImage = new System.Windows.Media.Imaging.BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.UriSource = new Uri(diagramPath);
                bitmapImage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                DiagramCanvas.Children.Clear();
                _currentImage = new System.Windows.Controls.Image
                {
                    Source = bitmapImage,
                    Stretch = System.Windows.Media.Stretch.Uniform
                };

                DiagramCanvas.Children.Add(_currentImage);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Došlo je do pogreške: {ex.Message}", "Pogreška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GeneratePlantUmlDiagram(string umlCode)
        {
            Directory.CreateDirectory(_outputDirectory);

            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            string umlFilePath = Path.Combine(_outputDirectory, $"diagram_{timestamp}.puml");
            string outputFilePath = Path.Combine(_outputDirectory, $"diagram_{timestamp}.png");

            File.WriteAllText(umlFilePath, umlCode);

            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "java",
                    Arguments = $"-jar \"{_plantUmlJarPath}\" \"{umlFilePath}\" -o \"{_outputDirectory}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit();

            if (!File.Exists(outputFilePath))
                throw new Exception("Dijagram nije generiran. Provjerite je li PlantUML ispravno konfiguriran.");

            return outputFilePath;
        }

        private void SendChatGPTQuery_Click(object sender, RoutedEventArgs e)
        {
            string input = txtChatInput.Text;

            if (string.IsNullOrWhiteSpace(input))
            {
                MessageBox.Show("Unesite tekst upita.");
                return;
            }
        }
    }
}
