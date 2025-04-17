using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Bank.Api.Logic;
using Bank.Logic.Models;

namespace Bank.Api.Tests;

public class MinimalApiTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly HttpClient _client;
    private readonly Storage _storage;

    public MinimalApiTests(WebApplicationFactory<Program> factory)
    {
        _storage = new Storage();

        var factoryWithTestStorage = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IStorage>(_storage);
            });
        });
        _client = factoryWithTestStorage.CreateClient();
    }

    public void Dispose() => DeleteAllAccounts();

    private void DeleteAllAccounts()
    {
        if (File.Exists(_storage.path))
        {
            File.Delete(_storage.path);
        }
    }

    private Account CreateTestAccount()
    {
        var account = _storage.NewAccount();
        account.Should().NotBeNull();
        return account;
    }

    private int CreateTestAccountId() => CreateTestAccount().Id;

    [Fact]
    public void POST_Account_Create_ReturnsSuccess()
    {
        _storage.ListAccounts().Should().BeEmpty();
        var account = CreateTestAccount();
        account.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GET_Accounts_ListWithoutInsert_ReturnsEmpty()
    {
        DeleteAllAccounts();
        var response = await _client.GetAsync("/");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var accounts = await response.Content.ReadFromJsonAsync<List<int>>();
        accounts.Should().NotBeNull();
    }

    [Fact]
    public async Task GET_Accounts_ListWithInsert_ReturnsInserted()
    {
        DeleteAllAccounts();
        var account = CreateTestAccount();
        var response = await _client.GetAsync("/");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var accounts = await response.Content.ReadFromJsonAsync<IEnumerable<Account>>();
        accounts.Should().ContainEquivalentOf(account);
    }

    [Fact]
    public async Task DELETE_Account_ReturnsSuccess()
    {
        var account = CreateTestAccount();
        var response = await _client.DeleteAsync($"/account/{account.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _storage.TryGetAccount(account.Id, out var deletedAccount);
        deletedAccount.Should().BeNull();
    }

    [Fact]
    public async Task GET_Account_WhenExists_ReturnsAccount()
    {
        var original = CreateTestAccount();
        var response = await _client.GetAsync($"/account/{original.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var account = await response.Content.ReadFromJsonAsync<Account>();
        account.Should().NotBeNull();
        account!.Id.Should().Be(original.Id);
    }

    [Fact]
    public async Task GET_Account_WhenNotExists_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/account/999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_Withdraw_WithSufficientFunds_ReturnsOk()
    {
        var accountId = CreateTestAccountId();
        await _client.PostAsync($"/deposit/{accountId}/200", null);
        var response = await _client.PostAsync($"/withdraw/{accountId}/100", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task POST_Withdraw_WithInsufficientFunds_ReturnsBadRequest()
    {
        var accountId = CreateTestAccountId();
        var response = await _client.PostAsync($"/withdraw/{accountId}/100", null);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_Deposit_WhenAccountExists_ReturnsOk()
    {
        var accountId = CreateTestAccountId();
        var response = await _client.PostAsync($"/deposit/{accountId}/100", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task POST_Deposit_WhenAccountNotExists_ReturnsNotFound()
    {
        var response = await _client.PostAsync("/deposit/999/100", null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_Transactions_WhenAccountHasTransactions_ReturnsTransactions()
    {
        var accountId = CreateTestAccountId();
        await _client.PostAsync($"/transactions/{accountId}/Deposit/100", null);
        await _client.PostAsync($"/transactions/{accountId}/Withdrawal/-50", null);
        var response = await _client.GetAsync($"/transactions/{accountId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var transactions = await response.Content.ReadFromJsonAsync<List<Transaction>>(new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        transactions.Should().NotBeNull();
        transactions!.Count.Should().Be(2);
    }

    [Fact]
    public async Task GET_Transactions_WhenAccountNotExists_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/transactions/999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_Transaction_AddWhenValid_ReturnsOk()
    {
        var accountId = CreateTestAccountId();
        var response = await _client.PostAsync($"/transactions/{accountId}/Deposit/100", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task POST_Transaction_AddWhenInvalidType_ReturnsBadRequest()
    {
        var accountId = CreateTestAccountId();
        var response = await _client.PostAsync($"/transactions/{accountId}/Invalid/100", null);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_Transaction_AddWhenAccountNotExists_ReturnsNotFound()
    {
        var response = await _client.PostAsync("/transactions/999/Deposit/100", null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public void POST_Account_Create_ShouldStartWithCleanState()
    {
        _storage.ListAccounts().Should().BeEmpty();
        var account = CreateTestAccount();
        account.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task POST_Account_Create_ShouldReturnOneAsync()
    {
        _storage.ListAccounts().Should().BeEmpty();
        var response = await _client.PostAsync("/account", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var account = await response.Content.ReadFromJsonAsync<Account>();
        account.Should().NotBeNull();

        _storage.ListAccounts().Should().HaveCount(1);
        _storage.ListAccounts().Should().ContainEquivalentOf(account);
    }
}