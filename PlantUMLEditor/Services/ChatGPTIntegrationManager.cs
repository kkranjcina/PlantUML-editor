using Newtonsoft.Json;
using PlantUMLEditor.Models;
using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace PlantUMLEditor.Services
{
    internal class ChatGPTIntegrationManager
    {
        public async Task<string> SendRequestAsync(ObservableCollection<ChatMessage> allMessages, string apiKey)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://api.openai.com/v1/chat/completions");
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var requestData = new
                {
                    model = "gpt-3.5-turbo",
                    messages = allMessages,
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
                string reply = jsonResponse.choices[0].message.content.ToString();

                return reply;
            }
        }
    }
}
