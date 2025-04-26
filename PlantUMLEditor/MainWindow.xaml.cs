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
using System.Security.Cryptography;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Windows.Media;

namespace PlantUMLEditor
{
    public partial class MainWindow : Window
    {
        private string _plantUmlJarPath;
        private readonly string _outputDirectory = Path.Combine(Path.GetTempPath(), "PlantUML");
        private System.Windows.Controls.Image _currentImage;
        private readonly string _configFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PlantUMLEditor", "config.dat");

        public MainWindow()
        {
            InitializeComponent();

            var spinnerAnimation = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = new Duration(TimeSpan.FromSeconds(1)),
                RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever
            };
            spinnerRotation.BeginAnimation(RotateTransform.AngleProperty, spinnerAnimation);

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

                ShowStatusNotification("Generiranje u tijeku...");

                btnGenerateDiagram.IsEnabled = false;

                string diagramPath = await Task.Run(() => GeneratePlantUmlDiagram(umlCode));

                HideStatusNotification();

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
            finally
            {
                HideStatusNotification();
                btnGenerateDiagram.IsEnabled = true;
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
                    Arguments = $"-DPLANTUML_LIMIT_SIZE=8192 -jar \"{_plantUmlJarPath}\" \"{umlFilePath}\" -o \"{_outputDirectory}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();


            if (!File.Exists(outputFilePath))
            {
                throw new Exception("Dijagram nije generiran. Detalji: " + error);
            }

            return outputFilePath;
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
                    ShowStatusNotification("Izvoz u tijeku...");

                    if (format == "txt")
                    {
                        await Task.Run(() => File.WriteAllText(saveDialog.FileName, umlCode));
                        HideStatusNotification();

                        Process.Start(new ProcessStartInfo
                        {
                            FileName = saveDialog.FileName,
                            UseShellExecute = true
                        });
                    }
                    else
                    {
                        string tempFilePath = await Task.Run(() => ExportPlantUmlDiagram(umlCode, format));

                        File.Copy(tempFilePath, saveDialog.FileName, true);

                        try { File.Delete(tempFilePath); } catch { }

                        HideStatusNotification();

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
                HideStatusNotification();

                MessageBox.Show($"Došlo je do pogreške prilikom izvoza: {ex.Message}", "Pogreška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnExport.IsEnabled = true;
            }
        }

        private string ExportPlantUmlDiagram(string umlCode, string format)
        {
            Directory.CreateDirectory(_outputDirectory);

            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            string umlFilePath = Path.Combine(_outputDirectory, $"diagram_{timestamp}.puml");

            string fileExtension = format;

            string outputFilePath = Path.Combine(_outputDirectory, $"diagram_{timestamp}.{fileExtension}");

            File.WriteAllText(umlFilePath, umlCode);

            string formatArg = format == "png" ? "" : $"-t{format}";

            string plantUmlDir = Path.GetDirectoryName(_plantUmlJarPath);

            ProcessStartInfo startInfo;

            if (format == "pdf")
            {
                string classpath = $"\"{_plantUmlJarPath}\"";

                foreach (string jarFile in Directory.GetFiles(plantUmlDir, "*.jar"))
                {
                    if (Path.GetFileName(jarFile) != Path.GetFileName(_plantUmlJarPath))
                    {
                        classpath += $";\"{jarFile}\"";
                    }
                }

                startInfo = new ProcessStartInfo
                {
                    FileName = "java",
                    Arguments = $"-Djava.awt.headless=true -cp {classpath} net.sourceforge.plantuml.Run {formatArg} \"{umlFilePath}\" -o \"{_outputDirectory}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
            }
            else
            {
                startInfo = new ProcessStartInfo
                {
                    FileName = "java",
                    Arguments = $"-jar \"{_plantUmlJarPath}\" {formatArg} \"{umlFilePath}\" -o \"{_outputDirectory}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
            }

            Process process = new Process { StartInfo = startInfo };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!File.Exists(outputFilePath))
            {
                throw new Exception($"Dijagram nije generiran. Detalji: {error}");
            }

            return outputFilePath;
        }

        private void ShowStatusNotification(string message)
        {
            statusText.Text = message;
            statusNotification.Visibility = Visibility.Visible;

            statusNotification.Opacity = 0;
            var fadeInAnimation = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = new Duration(TimeSpan.FromMilliseconds(300))
            };
            statusNotification.BeginAnimation(OpacityProperty, fadeInAnimation);
        }

        private void HideStatusNotification()
        {
            var fadeOutAnimation = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = new Duration(TimeSpan.FromMilliseconds(300))
            };
            fadeOutAnimation.Completed += (s, e) =>
            {
                statusNotification.Visibility = Visibility.Collapsed;
            };
            statusNotification.BeginAnimation(OpacityProperty, fadeOutAnimation);
        }

        private async Task ShowTemporaryStatusNotification(string message, int durationMs = 10000)
        {
            ShowStatusNotification(message);
            await Task.Delay(durationMs);
            HideStatusNotification();
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

                string apiKey = GetApiKey();

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

                SaveEncryptedApiKey(apiKey);

                txtApiKeyStatus.Text = "API ključ je uspješno spremljen!";
                txtApiKeyStatus.Visibility = Visibility.Visible;

                txtApiKey.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Pogreška pri spremanju API ključa: {ex.Message}", "Pogreška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveEncryptedApiKey(string apiKey)
        {
            byte[] entropy = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(entropy);
            }

            byte[] encryptedData = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(apiKey),
                entropy,
                DataProtectionScope.CurrentUser);

            using (var fs = new FileStream(_configFilePath, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write(entropy.Length);
                bw.Write(entropy);
                bw.Write(encryptedData.Length);
                bw.Write(encryptedData);
            }
        }

        private string GetApiKey()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                    return null;

                using (var fs = new FileStream(_configFilePath, FileMode.Open))
                using (var br = new BinaryReader(fs))
                {
                    int entropyLength = br.ReadInt32();
                    byte[] entropy = br.ReadBytes(entropyLength);
                    int encryptedDataLength = br.ReadInt32();
                    byte[] encryptedData = br.ReadBytes(encryptedDataLength);

                    byte[] decryptedData = ProtectedData.Unprotect(
                        encryptedData,
                        entropy,
                        DataProtectionScope.CurrentUser);

                    return Encoding.UTF8.GetString(decryptedData);
                }
            }
            catch
            {
                return null;
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
