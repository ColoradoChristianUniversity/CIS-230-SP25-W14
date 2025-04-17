using System.Net.Http.Json;

using Bank.Logic.Models;

public class BankApiClient : IApiClient
{
    private readonly HttpClient _http;

    public BankApiClient(HttpClient? httpClient, bool validateConnection = false)
    {
        _http = httpClient ?? new HttpClient { BaseAddress = new Uri("http://localhost:1234") }; ;

        if (validateConnection)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "/");
                var response = _http.GetAsync("/", HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();

                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException($"API is unreachable. Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to connect to the Bank API. Is it running?", ex);
            }
        }
    }

    public async Task<List<Account>> GetAccountsAsync()
    {
        var response = await _http.GetAsync("/");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<Account>>() ?? throw new Exception("Failed to deserialize accounts.");
    }

    public async Task<Account> GetAccountAsync(int id)
    {
        return await _http.GetFromJsonAsync<Account>($"/account/{id}") ?? throw new Exception("Failed to deserialize account.");
    }

    public async Task CreateAccountAsync()
    {
        await _http.PostAsync("/account", null);
    }

    public async Task DepositAsync(int accountId, double amount)
    {
        await _http.PostAsync($"/deposit/{accountId}/{amount}", null);
    }

    public async Task WithdrawAsync(int accountId, double amount)
    {
        await _http.PostAsync($"/withdraw/{accountId}/{amount}", null);
    }

    public async Task DeleteAccountAsync(int accountId)
    {
        await _http.DeleteAsync($"/account/{accountId}");
    }
}


