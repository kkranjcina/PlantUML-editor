using System;
using System.Diagnostics;
using System.IO;
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
        private string _plantUmlJarPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "jarFiles", "plantuml-1.2025.2.jar");
        private readonly string _outputDirectory = Path.Combine(Path.GetTempPath(), "PlantUML");
        private readonly string _configFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PlantUMLEditor", "config.dat");
        private StatusNotificationService _statusNotificationService;
        private ApiKeyManager _apiKeyManager;
        private SpinnerService _spinnerService;

        private const double ZoomStep = 0.1;
        private const double MaxZoom = 3.5;
        private const double MinZoom = 0.4;
        private Point _scrollStartPoint;
        private Point _scrollStartOffset;
        private bool _isDragging = false;

        public MainWindow()
        {
            InitializeComponent();
            
            _statusNotificationService = new StatusNotificationService(statusNotification, statusText);
            _apiKeyManager = new ApiKeyManager(_configFilePath);
            _spinnerService = new SpinnerService(spinnerRotation);
            _spinnerService.StartAnimation();

            Directory.CreateDirectory(Path.GetDirectoryName(_configFilePath));

            PlantUmlTemplates.Initialize();

            foreach (var template in PlantUmlTemplates.Templates)
            {
                cmbDiagramType.Items.Add(new ComboBoxItem { Content = template.Key });
            }
        }

        private void txtPlantUmlCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtPlantUmlPlaceholder.Visibility = string.IsNullOrEmpty(txtPlantUmlCode.Text)
                ? Visibility.Visible
                : Visibility.Hidden;
        }

        private void txtChatInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtChatPlaceholder.Visibility = string.IsNullOrEmpty(txtChatInput.Text)
                ? Visibility.Visible
                : Visibility.Hidden;
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

                DiagramImage.Source = bitmapImage;
                DiagramImageScale.ScaleX = 1;
                DiagramImageScale.ScaleY = 1;
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

        private void DiagramImage_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (DiagramImage.Source == null) return;

            double zoom = e.Delta > 0 ? (1 + ZoomStep) : (1 - ZoomStep);
            double newScale = DiagramImageScale.ScaleX * zoom;

            if (newScale < MinZoom) newScale = MinZoom;
            if (newScale > MaxZoom) newScale = MaxZoom;

            var position = e.GetPosition(DiagramImage);

            DiagramImageScale.ScaleX = newScale;
            DiagramImageScale.ScaleY = newScale;

            var scrollViewer = DiagramScrollViewer;
            if (scrollViewer != null && DiagramImage.ActualWidth > 0 && DiagramImage.ActualHeight > 0)
            {
                double relativeX = position.X / DiagramImage.ActualWidth;
                double relativeY = position.Y / DiagramImage.ActualHeight;

                double targetX = (DiagramImage.ActualWidth * newScale) * relativeX - scrollViewer.ViewportWidth / 2;
                double targetY = (DiagramImage.ActualHeight * newScale) * relativeY - scrollViewer.ViewportHeight / 2;

                scrollViewer.ScrollToHorizontalOffset(targetX);
                scrollViewer.ScrollToVerticalOffset(targetY);
            }

            e.Handled = true;
        }

        private void DiagramImage_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DiagramImage.Source == null) return;

            _isDragging = true;
            _scrollStartPoint = e.GetPosition(DiagramScrollViewer);
            _scrollStartOffset = new Point(DiagramScrollViewer.HorizontalOffset, DiagramScrollViewer.VerticalOffset);
            DiagramImage.CaptureMouse();
        }

        private void DiagramImage_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isDragging && DiagramImage.IsMouseCaptured)
            {
                Point currentPoint = e.GetPosition(DiagramScrollViewer);
                double dX = currentPoint.X - _scrollStartPoint.X;
                double dY = currentPoint.Y - _scrollStartPoint.Y;
                DiagramScrollViewer.ScrollToHorizontalOffset(_scrollStartOffset.X - dX);
                DiagramScrollViewer.ScrollToVerticalOffset(_scrollStartOffset.Y - dY);
            }
        }

        private void DiagramImage_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isDragging = false;
            DiagramImage.ReleaseMouseCapture();
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
                        await Task.Run(() => File.WriteAllText(saveDialog.FileName, umlCode, Encoding.UTF8));

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
                        "komentare, uvode ili pitanja. Odgovori samo s validnim PlantUML kodom." },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 1000,
                    temperature = 0.7
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
