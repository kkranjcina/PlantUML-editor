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
            GetSolutionDirectory();

            Directory.CreateDirectory(Path.GetDirectoryName(_configFilePath));
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
