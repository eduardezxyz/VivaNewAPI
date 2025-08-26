using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NewVivaApi.Services
{
    public class WebHookService
    {
        // Reuse a single HttpClient to avoid socket exhaustion
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        /// <summary>
        /// Posts webhook payload to the given URL with an optional Bearer token.
        /// Returns true on 2xx status codes, false otherwise.
        /// </summary>
        public async Task<bool> PostWebHookDataAsync(
            string url,
            int payapp_id,
            string vivapayapp_id,
            string bearerToken,
            string status,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url);

                if (!string.IsNullOrWhiteSpace(bearerToken))
                {
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", bearerToken);
                }

                var payload = new
                {
                    changed = new
                    {
                        payapp_id = payapp_id,
                        vivapayapp_id = vivapayapp_id,
                        status = status,
                        dates = DateTime.UtcNow
                    }
                };

                string json = JsonConvert.SerializeObject(payload);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch (OperationCanceledException) // includes TaskCanceledException on timeout
            {
                Console.WriteLine("Webhook request canceled or timed out.");
                return false;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine("HttpRequestException: " + ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unexpected exception: " + ex.Message);
                return false;
            }
        }
    }
}
