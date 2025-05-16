using Avalonia.Controls;
using System;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using Avalonia.Threading;
using Npgsql;
using Avalonia;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Linq;
using System.Threading;

namespace AI_assistent;


public partial class MainWindow : Window
{

    class AuthResponse
    {
        public string access_token { get; set; }
        public int expires_at { get; set; }
    }
    private const string ApiUrl = "https://gigachat.devices.sberbank.ru/api/v1/chat/completions";
    private readonly HttpClient _httpClient;
    
    public class GigaChatAuthResponse
    {
        public string access_token { get; set; }
        public long expires_at { get; set; }
    }
    private NpgsqlConnection con = new NpgsqlConnection(
        connectionString: "Host=localhost;Port=5432;Username=postgres;Password=user;Database=RZD_data;"
    );
    public MainWindow()
    {
        InitializeComponent();
        con.OpenAsync();
        var screen = Screens.ScreenFromVisual(this) ?? Screens.Primary;

        if (screen != null)
        {
            var workArea = screen.WorkingArea;

            this.Width = 500;
            this.Height = 450;

            this.Position = new PixelPoint(
                (int)(workArea.X + workArea.Width - this.Width - 50),
                (int)(workArea.Y + workArea.Height - this.Height - 100)
            );
        }

        btnSendRequest.Click += SendPrompt;
        tbRequest.TextChanged += RequetChanged;
        btnNo.Click += NoPrompt;
        btnYes.Click += YesPrompt;
    }

    private void SendPrompt(object sender, EventArgs args)
    {
        SendRequest();
    }
    private async void SendRequest()
    {
        btnSendRequest.IsEnabled = false;
        await CallGigaChatApiAsync(tbRequest.Text);
    }

    private void RequetChanged(object sender, EventArgs args)
    {
        if (tbRequest.Text == "")
        {
            btnSendRequest.IsEnabled = false;
            btnNo.IsEnabled = false;
            btnYes.IsEnabled = false;
            return;
        }
        btnSendRequest.IsEnabled = true;
    }

    private void NoPrompt(object sender, EventArgs args)
    {
        SendData(false);
    }

    private void YesPrompt(object sender, EventArgs args)
    {
        SendData(true);
    }

    private async Task CallGigaChatApiAsync(string prompt)
    {
        var systemPrompt = @"Ты ассистент диспетчера. Тебе надо не говорить общие вещи, а давать чёткие указания что делать диспетчеру по ситуации. 
Очень важная часть - восстановление графика движения пассажирских поездов.
Тебе не надо писать ситуацию, напиши только алгоритм действий. Ты должен советовать ему что делать, чётко следуя этой инструкции:

1. Действия при возникновении аварийной ситуации:
   - Получить от машиниста информацию об обстоятельствах аварии
   - Уточнить наличие и расположение вагонов с ВМ и опасными грузами
   - При необходимости дать указание энергодиспетчеру о снятии напряжения
   - Координировать действия машиниста по ликвидации ситуации

2. Действия дежурного по станции:
   - Полностью передать информацию диспетчеру поездному
   - Действовать строго по указаниям диспетчера

3. Действия при пожаре:
   - Организовать удаление вагонов с ВМ на расстояние не менее 100 м
   - Обеспечить безопасность прилегающей территории

4. Ликвидация последствий:
   - Сообщить уполномоченному представителю владельца инфраструктуры
   - Принять меры к быстрейшей ликвидации последствий

5. Обеспечение безопасности:
   - Действовать согласно правилам безопасности для опасных грузов
   - Следовать порядку ликвидации аварийных ситуаций

6. Дополнительные меры:
   - Оценить возможность дальнейшего пропуска поездов
   - При необходимости прекратить движение поездов и маневры";

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "eyJjdHkiOiJqd3QiLCJlbmMiOiJBMjU2Q0JDLUhTNTEyIiwiYWxnIjoiUlNBLU9BRVAtMjU2In0.ZCrG6PUG3iOyavPv45kRmfZfA8kfd_Z7tCToQTZ5LokydpI4B_qwYVveXE6UPXqzAr6iz-8PJouYsbVFYDsHblUw4u4c3Wg7PMwNaZBWnjFsAT3pfIH7piCUAIJdJ7NDWEvPBk7NILjozjGSJfzy6ucTgUkKi6LHvSRJYkb45XX1N01sGTeTV28WA56V25aE7oUAGy10vQ6ytdE9OqK6n_cQQM0PELaXru088zZWqidXPerKl1o3kKVkdbWUI7fZLRvSZo-2K2jm5QPfSw6cKi6-KOpZglfvSPiYsN4SdEtztaRUDXr9ai7LBERM6dtmHl3SszcDlZEZXm5yM1oUGQ.caF31P2ZqBsaUo39slfNLw.CSMrE5_fPlLtUNYOAhd8VwfYMUgvFxczKK3KWyKXGz2pDfTOz8Mxayp7fq6e4JVziUVLW3UqG2qSM-wofzo2T9bIEEYPyon7wZEv4nuM2ehyv93IxB2POIanomtgwcG3Mt-KcXHcE7l2yBPactJwfL2RoU-2hnBMCN0NEjtdtSD-UjgNt7-3_vxfO1Ru7ITKBkQSkw29rSF4fgz6114GEoKEbalNFrF4gjbp7woPCBL9I4HZ5xzALGKGliIEnYA-NXwidHsVMSdpqhfaDMlh2bCgnJqwIoBbUZM-6BYEW7YKSAxkgnrbYxnFePtEnwDDbXTYdAmblvwBWlhprep07ELud4ad-5fO0oM18qWHJ_rA9m-kKLCucFbMYRT_xSXQqQMMzMQV-DuwcIlj_I8_PnKWtKEW14etI1gbKWBeDOuLK6j1UIu-jxsUj7niGrteYceUE9_d1UsSWgmjBlXVs7fFO_WtAf78RqznbGjEgLh3lf6KRyekLNI09tQxzYrQilC0e98rGUDbNm1tNUYWeGU6uwHFcWjtJuHa57OHjk3KFySd3JTJHBtb5fe18EFpdGsAbfz8MG9QhVit1Pk4xQq-fAtpu2Aa_icWjK_D7plCjX7DLsoN8Rd5vJseIAapsBmnoZIPel2jwiO5mUTu1jAWLNQ3UNBTHol_a8EId0PKjk5k_4EcquUtKPL8vZqCqsccbj4EddX0E1_jjePE3xTHlyaUT0VBzrZshanIdVI.LJTd7WTuI6M3nPZFZbG9ZGMcnKpLhwjpvIzLk2QIdCA");
        var request = new
        {
            model = "GigaChat",
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content =  prompt}
            },
            temperature = 0.7,
            max_tokens = 1000
        };
        var response = await httpClient.PostAsync(ApiUrl,
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        var responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine(responseContent);
        var jsonDoc = JsonDocument.Parse(responseContent);
        var aiResponse = jsonDoc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();
        Dispatcher.UIThread.Post(() =>
        {
            tbResponse.Text = aiResponse;
            btnNo.IsEnabled = true;
            btnYes.IsEnabled = true;
        });
    }

    private async void SendData(bool IsGood)
    {
        using (var transaction = await con.BeginTransactionAsync())
        {
            using var cmd = new NpgsqlCommand();
            cmd.Connection = con;

            cmd.CommandText = "INSERT INTO responses (request, response, is_good) VALUES (@req, @res, @good)";
            cmd.Parameters.AddWithValue("@req", tbRequest.Text);
            cmd.Parameters.AddWithValue("@res", tbResponse.Text);
            cmd.Parameters.AddWithValue("@good", IsGood);

            await cmd.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
        }
        RefreshView();
    }

    private void RefreshView()
    {
        tbResponse.Text = "";
        tbRequest.Text = "";
    }

    private async Task<string> GetAccessToken()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
        using var httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        var requestUrl = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";

            // Очистка и установка заголовков
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("RqUID", "6f0b1291-c7f3-43c6-bb2e-9f3efb2dc98e");

            // Авторизация Basic
            var authHeaderValue = Convert.ToBase64String(Encoding.ASCII.GetBytes("ZGE4M2JkNWYtOTBiZi00N2M2LTgwNDktMDUxMzNkM2RmYTQ2Ojg3ZjkzYmUwLWVjN2MtNDY0MC05NzUzLTI5NDI2NWFkMjEzMw=="));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeaderValue);

            // Тело запроса
            var formData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("scope", "GIGACHAT_API_PERS")
            });

            Console.WriteLine("Отправка запроса...");
            var response = await httpClient.PostAsync(requestUrl, formData);
            
            Console.WriteLine($"Статус ответа: {response.StatusCode}");
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Ответ сервера: {responseContent}");
        tbResponse.Text = responseContent.Split(":")[1];
        Thread.Sleep(100000);
        return responseContent.Split(":")[1];
    }

}