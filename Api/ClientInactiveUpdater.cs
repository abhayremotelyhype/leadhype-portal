using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartleadClientUpdater
{
    public class Client
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Company { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int CampaignCount { get; set; }
        public int ActiveCampaigns { get; set; }
        public int EmailAccountCount { get; set; }
    }

    public class ClientListResponse
    {
        public bool Success { get; set; }
        public List<Client> Data { get; set; } = new();
        public PaginationInfo Pagination { get; set; } = new();
    }

    public class PaginationInfo
    {
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int Limit { get; set; }
        public int TotalPages { get; set; }
    }

    public class UpdateClientRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Company { get; set; }
        public string Status { get; set; } = "active";
        public string? Notes { get; set; }
    }

    public class ClientInactiveUpdater
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _apiKey;

        public ClientInactiveUpdater(string baseUrl, string apiKey)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _apiKey = apiKey;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", _apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task ProcessClientsAsync()
        {
            Console.WriteLine("Starting client processing...");
            
            try
            {
                var allClients = await GetAllClientsAsync();
                Console.WriteLine($"Found {allClients.Count} total clients");

                var clientsToUpdate = new List<Client>();
                
                foreach (var client in allClients)
                {
                    if (client.Name.Contains("Inactive", StringComparison.OrdinalIgnoreCase))
                    {
                        clientsToUpdate.Add(client);
                        Console.WriteLine($"Found client to update: {client.Name} (ID: {client.Id})");
                    }
                }

                Console.WriteLine($"Found {clientsToUpdate.Count} clients to update");

                foreach (var client in clientsToUpdate)
                {
                    try
                    {
                        // Remove "Inactive" from name (case insensitive)
                        var newName = client.Name.Replace("Inactive", "", StringComparison.OrdinalIgnoreCase)
                                                .Replace("  ", " ") // Replace double spaces with single space
                                                .Trim(); // Remove leading/trailing whitespace

                        var updateRequest = new UpdateClientRequest
                        {
                            Name = newName,
                            Email = client.Email,
                            Company = client.Company,
                            Status = "inactive",
                            Notes = client.Notes
                        };

                        var success = await UpdateClientAsync(client.Id, updateRequest);
                        
                        if (success)
                        {
                            Console.WriteLine($"✓ Successfully updated client: '{client.Name}' → '{newName}' (Status: inactive)");
                        }
                        else
                        {
                            Console.WriteLine($"✗ Failed to update client: {client.Name} (ID: {client.Id})");
                        }

                        // Add small delay to avoid rate limiting
                        await Task.Delay(100);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"✗ Error updating client {client.Name} (ID: {client.Id}): {ex.Message}");
                    }
                }

                Console.WriteLine("Client processing completed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during client processing: {ex.Message}");
                throw;
            }
        }

        private async Task<List<Client>> GetAllClientsAsync()
        {
            var allClients = new List<Client>();
            int page = 1;
            const int limit = 100; // Maximum allowed by the API

            while (true)
            {
                Console.WriteLine($"Fetching page {page}...");
                
                var url = $"{_baseUrl}/api/v1/clients?page={page}&limit={limit}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Failed to fetch clients (page {page}): {response.StatusCode} - {errorContent}");
                }

                var json = await response.Content.ReadAsStringAsync();
                var clientResponse = JsonSerializer.Deserialize<ClientListResponse>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (clientResponse?.Success == true && clientResponse.Data.Any())
                {
                    allClients.AddRange(clientResponse.Data);
                    Console.WriteLine($"Retrieved {clientResponse.Data.Count} clients from page {page}");

                    // Check if there are more pages
                    if (page >= clientResponse.Pagination.TotalPages)
                    {
                        break;
                    }
                    
                    page++;
                }
                else
                {
                    break;
                }
            }

            return allClients;
        }

        private async Task<bool> UpdateClientAsync(string clientId, UpdateClientRequest updateRequest)
        {
            var url = $"{_baseUrl}/api/v1/clients/{clientId}";
            var json = JsonSerializer.Serialize(updateRequest, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync(url, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Update failed for client {clientId}: {response.StatusCode} - {errorContent}");
                return false;
            }

            return true;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            // Configuration - Update these values
            const string BASE_URL = "https://your-api-url.com"; // Replace with your actual API URL
            const string API_KEY = "your-api-key-here"; // Replace with your actual API key

            if (BASE_URL == "https://your-api-url.com" || API_KEY == "your-api-key-here")
            {
                Console.WriteLine("Please update the BASE_URL and API_KEY constants in the code before running.");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }

            var updater = new ClientInactiveUpdater(BASE_URL, API_KEY);
            
            try
            {
                await updater.ProcessClientsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Application error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            finally
            {
                updater.Dispose();
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}