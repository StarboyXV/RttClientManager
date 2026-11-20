using System.Net.Http.Json;
using System.Text.Json;
using Rtt.ClientManager.Common.Models;

namespace Rtt.ClientManager.Web.Services
{
    public interface IClientApiClient
    {
        Task<PagedResult<ClientDto>> GetClientsAsync(int page, int pageSize, string? search);
        Task<ClientDto?> GetClientByIdAsync(int id);
        Task<(bool Success, List<string> Errors)> CreateClientAsync(ClientDto client);
        Task<(bool Success, List<string> Errors)> UpdateClientAsync(ClientDto client);
        Task<bool> DeleteClientAsync(int id);
        Task<List<LookupItem>> GetAddressTypesAsync();
        Task<List<LookupItem>> GetContactTypesAsync();
        Task<Stream> DownloadClientsCsvAsync();
    }

    public class ClientApiClient : IClientApiClient
    {
        private readonly HttpClient _httpClient;

        public ClientApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<PagedResult<ClientDto>> GetClientsAsync(int page, int pageSize, string? search)
        {
            string url = $"api/client?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(search))
            {
                url += $"&search={Uri.EscapeDataString(search)}";
            }

            return await _httpClient.GetFromJsonAsync<PagedResult<ClientDto>>(url)
                   ?? new PagedResult<ClientDto>();
        }

        public async Task<ClientDto?> GetClientByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"api/client/{id}");
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<ClientDto>();
        }

        public async Task<(bool Success, List<string> Errors)> CreateClientAsync(ClientDto client)
        {
            var response = await _httpClient.PostAsJsonAsync("api/client", client);
            return await ParseWriteResponseAsync(response);
        }

        public async Task<(bool Success, List<string> Errors)> UpdateClientAsync(ClientDto client)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/client/{client.ClientId}", client);
            return await ParseWriteResponseAsync(response);
        }

        public async Task<bool> DeleteClientAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/client/{id}");
            return response.IsSuccessStatusCode;
        }

        public async Task<List<LookupItem>> GetAddressTypesAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<LookupItem>>("api/lookup/address-types")
                   ?? new List<LookupItem>();
        }

        public async Task<List<LookupItem>> GetContactTypesAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<LookupItem>>("api/lookup/contact-types")
                   ?? new List<LookupItem>();
        }

        public async Task<Stream> DownloadClientsCsvAsync()
        {
            var response = await _httpClient.GetAsync("api/client/export", HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStreamAsync();
        }

        private static async Task<(bool Success, List<string> Errors)> ParseWriteResponseAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
                return (true, new List<string>());

            var errors = new List<string>();
            try
            {
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                if (json.TryGetProperty("errors", out var errorsProp) && errorsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var err in errorsProp.EnumerateArray())
                    {
                        errors.Add(err.GetString() ?? "Validation error.");
                    }
                }
            }
            catch
            {
                errors.Add($"API returned status {(int)response.StatusCode}.");
            }

            if (errors.Count == 0)
                errors.Add("An unexpected error occurred while saving the client.");

            return (false, errors);
        }
    }
}