using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using System.Windows.Controls;
using Microsoft.Win32;
using PlantUMLEditor.Services;

namespace PlantUMLEditor.Views
{
    public partial class MainWindow : Window
    {
        private string _plantUmlJarPath;
        private readonly string _outputDirectory = Path.Combine(Path.GetTempPath(), "PlantUML");
        private Image _currentImage;
        private readonly string _configFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PlantUMLEditor", "config.dat");
        private StatusNotificationService _statusNotificationService;
        private ApiKeyManager _apiKeyManager;
        private SpinnerService _spinnerService;

        public MainWindow()
        {
            InitializeComponent();
            
            _statusNotificationService = new StatusNotificationService(statusNotification, statusText);
            _apiKeyManager = new ApiKeyManager(_configFilePath);
            _spinnerService = new SpinnerService(spinnerRotation);
            _spinnerService.StartAnimation();

            GetSolutionDirectory();
            Directory.CreateDirectory(Path.GetDirectoryName(_configFilePath));

            PlantUmlTemplates.Initialize();

            foreach (var template in PlantUmlTemplates.Templates)
            {
                cmbDiagramType.Items.Add(new ComboBoxItem { Content = template.Key });
            }
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

            _plantUmlJarPath = Path.Combine(solutionDirectory, "jarFiles", "plantuml-1.2025.2.jar");

            if (!File.Exists(_plantUmlJarPath))
            {
                MessageBox.Show($"Datoteka PlantUML nije pronađena na lokaciji: {_plantUmlJarPath}", "Pogreška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void GenerateDiagram_Click(object sender, RoutedEventArgs e)
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

                _statusNotificationService.ShowStatusNotification("Generiranje u tijeku...");

                btnGenerateDiagram.IsEnabled = false;

                string diagramPath = await Task.Run(() => DiagramGenerator.GeneratePlantUmlDiagram(umlCode, _outputDirectory, _plantUmlJarPath));

                GC.Collect();
                GC.WaitForPendingFinalizers();

                var bitmapImage = new System.Windows.Media.Imaging.BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.UriSource = new Uri(diagramPath);
                bitmapImage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                DiagramCanvas.Children.Clear();
                _currentImage = new Image
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
            finally
            {
                _statusNotificationService.HideStatusNotification();
                btnGenerateDiagram.IsEnabled = true;
            }
        }

        private async void ExportDiagram_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string umlCode = txtPlantUmlCode.Text;
                if (string.IsNullOrWhiteSpace(umlCode))
                {
                    MessageBox.Show("Unesite PlantUML kod.", "Pogreška", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ComboBoxItem selectedItem = cmbExportFormat.SelectedItem as ComboBoxItem;
                string format = selectedItem.Content.ToString().ToLower();

                SaveFileDialog saveDialog = new SaveFileDialog();
                saveDialog.Title = "Spremi dijagram";

                switch (format)
                {
                    case "png":
                        saveDialog.Filter = "PNG slika (*.png)|*.png";
                        break;
                    case "svg":
                        saveDialog.Filter = "SVG vektorska slika (*.svg)|*.svg";
                        break;
                    case "pdf":
                        saveDialog.Filter = "PDF dokument (*.pdf)|*.pdf";
                        break;
                    case "eps":
                        saveDialog.Filter = "EPS dokument (*.eps)|*.eps";
                        break;
                    case "txt":
                        saveDialog.Filter = "TXT tekst (*.txt)|*.txt";
                        break;
                }

                saveDialog.DefaultExt = format;
                saveDialog.FileName = $"diagram_{DateTime.Now.ToString("yyyyMMdd")}";

                bool? result = saveDialog.ShowDialog();

                if (result == true)
                {
                    btnExport.IsEnabled = false;
                    _statusNotificationService.ShowStatusNotification("Izvoz u tijeku...");

                    if (format == "txt")
                    {
                        await Task.Run(() => File.WriteAllText(saveDialog.FileName, umlCode));

                        Process.Start(new ProcessStartInfo
                        {
                            FileName = saveDialog.FileName,
                            UseShellExecute = true
                        });
                    }
                    else
                    {
                        string tempFilePath = await Task.Run(() => DiagramGenerator.ExportPlantUmlDiagram(umlCode, format, _outputDirectory, _plantUmlJarPath));

                        File.Copy(tempFilePath, saveDialog.FileName, true);

                        try { File.Delete(tempFilePath); } catch { }

                        Process.Start(new ProcessStartInfo
                        {
                            FileName = saveDialog.FileName,
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show($"Došlo je do pogreške prilikom izvoza: {ex.Message}", "Pogreška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _statusNotificationService.HideStatusNotification();
                btnExport.IsEnabled = true;
            }
        }

        private void DiagramType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbDiagramType.SelectedItem != null)
            {
                string selectedDiagramType = (cmbDiagramType.SelectedItem as ComboBoxItem).Content.ToString();

                if (PlantUmlTemplates.Templates.ContainsKey(selectedDiagramType))
                {
                    txtPlantUmlCode.Text = PlantUmlTemplates.Templates[selectedDiagramType];
                }
            }
        }

        private void AddTemplate_Click(object sender, RoutedEventArgs e)
        {
            TemplateDialog dialog = new TemplateDialog("Moj predložak");
            dialog.Owner = this;

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                string templateName = dialog.TemplateName;
                string templateCode = dialog.TemplateCode;

                if (PlantUmlTemplates.Templates.ContainsKey(templateName))
                {
                    MessageBoxResult overwriteResult = MessageBox.Show(
                        $"Predložak s imenom '{templateName}' već postoji. Želite li ga zamijeniti?",
                        "Predložak već postoji",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (overwriteResult != MessageBoxResult.Yes)
                        return;
                }

                PlantUmlTemplates.AddTemplate(templateName, templateCode);

                bool exists = false;
                foreach (ComboBoxItem item in cmbDiagramType.Items)
                {
                    if (item.Content.ToString() == templateName)
                    {
                        exists = true;
                        cmbDiagramType.SelectedItem = item;
                        break;
                    }
                }

                if (!exists)
                {
                    ComboBoxItem newItem = new ComboBoxItem { Content = templateName };
                    cmbDiagramType.Items.Add(newItem);
                    cmbDiagramType.SelectedItem = newItem;
                }
            }
        }

        private void EditTemplate_Click(object sender, RoutedEventArgs e)
        {
            if (cmbDiagramType.SelectedItem == null)
            {
                MessageBox.Show("Odaberite predložak koji želite izmijeniti.",
                    "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string templateName = (cmbDiagramType.SelectedItem as ComboBoxItem).Content.ToString();
            string currentCode = PlantUmlTemplates.Templates[templateName];

            TemplateDialog dialog = new TemplateDialog(templateName, currentCode);
            dialog.Owner = this;
            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                string newName = dialog.TemplateName;
                string newCode = dialog.TemplateCode;

                if (newName != templateName)
                {
                    PlantUmlTemplates.RemoveTemplate(templateName);

                    cmbDiagramType.Items.Remove(cmbDiagramType.SelectedItem);

                    PlantUmlTemplates.AddTemplate(newName, newCode);

                    ComboBoxItem newItem = new ComboBoxItem { Content = newName };
                    cmbDiagramType.Items.Add(newItem);
                    cmbDiagramType.SelectedItem = newItem;

                    txtPlantUmlCode.Text = newCode;
                    cmbDiagramType.Items.Refresh();
                }
                else
                {
                    PlantUmlTemplates.AddTemplate(templateName, newCode);
                    txtPlantUmlCode.Text = newCode;
                    cmbDiagramType.Items.Refresh();
                }
            }
        }

        private void RemoveTemplate_Click(object sender, RoutedEventArgs e)
        {
            if (cmbDiagramType.SelectedItem == null)
            {
                MessageBox.Show("Odaberite predložak koji želite ukloniti.",
                    "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string templateName = (cmbDiagramType.SelectedItem as ComboBoxItem).Content.ToString();

            MessageBoxResult result = MessageBox.Show(
                $"Jeste li sigurni da želite ukloniti predložak '{templateName}'?",
                "Potvrda brisanja",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            PlantUmlTemplates.RemoveTemplate(templateName);

            cmbDiagramType.Items.Remove(cmbDiagramType.SelectedItem);

            txtPlantUmlCode.Text = string.Empty;
        }

        private async void SendChatGPTQuery_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string input = txtChatInput.Text;

                if (string.IsNullOrWhiteSpace(input))
                {
                    MessageBox.Show("Unesite tekst upita.");
                    return;
                }

                btnSendQuery.IsEnabled = false;
                txtChatResponse.Text = "Učitavanje odgovora...";

                string apiKey = _apiKeyManager.GetApiKey();

                if (string.IsNullOrEmpty(apiKey))
                {
                    MessageBox.Show("API ključ nije postavljen. Postavite ga u postavkama aplikacije.");
                    btnSendQuery.IsEnabled = true;
                    return;
                }

                string response = await SendChatGPTRequestAsync(input, apiKey);

                txtChatResponse.Text = response;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Došlo je do pogreške: {ex.Message}", "Pogreška", MessageBoxButton.OK, MessageBoxImage.Error);
                txtChatResponse.Text = string.Empty;
            }
            finally
            {
                btnSendQuery.IsEnabled = true;
            }
        }

        private void SaveApiKey_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string apiKey = txtApiKey.Password;

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    MessageBox.Show("Unesite API ključ.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _apiKeyManager.SaveApiKey(apiKey);

                txtApiKeyStatus.Text = "API ključ je uspješno spremljen!";
                txtApiKeyStatus.Visibility = Visibility.Visible;

                txtApiKey.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Pogreška pri spremanju API ključa: {ex.Message}", "Pogreška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<string> SendChatGPTRequestAsync(string prompt, string apiKey)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://api.openai.com/v1/chat/completions");
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var requestData = new
                {
                    model = "gpt-3.5-turbo",
                    messages = new[]
                    {
                        new { role = "system", content = "Generiraj samo PlantUML kod. Ne dodavaj nikakva objašnjenja, " +
                        "komentare, uvode ili pitanja. Odgovori samo s validnim PlantUML kodom koji počinje s @startuml i završava s @enduml." },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 1000
                };

                var content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");

                var response = await client.PostAsync("", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"API greška: {response.StatusCode}, {responseContent}");
                }

                dynamic jsonResponse = JsonConvert.DeserializeObject(responseContent);
                return jsonResponse.choices[0].message.content.ToString();
            }
        }
    }
}
