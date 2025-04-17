using System.Net;
using System.Text.Json;

using Bank.Logic.Models;

using FluentAssertions;

namespace Bank.App.Shared.Tests;

public class BankApiClientTests
{
    private const string BaseAddress = "http://localhost:1234";

    private static HttpClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var handler = new FakeHttpMessageHandler(responder);
        return new HttpClient(handler) { BaseAddress = new Uri(BaseAddress) };
    }

    [Fact]
    public async Task GetAccountsAsync_ReturnsAccounts()
    {
        var expected = new List<Account>
        {
            new() { Id = 1 },
            new() { Id = 2 }
        };

        var client = CreateClient(req =>
        {
            req.Method.Should().Be(HttpMethod.Get);
            req.RequestUri!.ToString().Should().EndWith("/");
            var json = JsonSerializer.Serialize(expected);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        });

        var api = new BankApiClient(client);
        var result = await api.GetAccountsAsync();
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetAccountAsync_ReturnsSingleAccount()
    {
        var expected = new Account { Id = 1 };

        var client = CreateClient(req =>
        {
            req.Method.Should().Be(HttpMethod.Get);
            req.RequestUri!.ToString().Should().EndWith("/account/1");
            var json = JsonSerializer.Serialize(expected);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        });

        var api = new BankApiClient(client);
        var result = await api.GetAccountAsync(1);
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task CreateAccountAsync_SendsPost()
    {
        bool called = false;

        var client = CreateClient(req =>
        {
            called = true;
            req.Method.Should().Be(HttpMethod.Post);
            req.RequestUri!.ToString().Should().EndWith("/account");
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var api = new BankApiClient(client);
        await api.CreateAccountAsync();
        called.Should().BeTrue();
    }

    [Fact]
    public async Task DepositAsync_SendsPostWithCorrectRoute()
    {
        var client = CreateClient(req =>
        {
            req.Method.Should().Be(HttpMethod.Post);
            req.RequestUri!.ToString().Should().EndWith("/deposit/1/100");
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var api = new BankApiClient(client);
        await api.DepositAsync(1, 100);
    }

    [Fact]
    public async Task WithdrawAsync_SendsPostWithCorrectRoute()
    {
        var client = CreateClient(req =>
        {
            req.Method.Should().Be(HttpMethod.Post);
            req.RequestUri!.ToString().Should().EndWith("/withdraw/2/50");
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var api = new BankApiClient(client);
        await api.WithdrawAsync(2, 50);
    }

    [Fact]
    public async Task DeleteAccountAsync_SendsDeleteRequest()
    {
        var client = CreateClient(req =>
        {
            req.Method.Should().Be(HttpMethod.Delete);
            req.RequestUri!.ToString().Should().EndWith("/account/99");
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var api = new BankApiClient(client);
        await api.DeleteAccountAsync(99);
    }

    [Fact]
    public void Constructor_WithValidation_ThrowsIfUnreachable()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var client = new HttpClient(handler) { BaseAddress = new Uri(BaseAddress) };

        var act = () => new BankApiClient(client, validateConnection: true);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Failed to connect to the Bank API*");
    }

    [Fact]
    public void Constructor_WithValidation_SucceedsIfReachable()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK));

        var client = new HttpClient(handler) { BaseAddress = new Uri(BaseAddress) };

        var act = () => new BankApiClient(client, validateConnection: true);
        act.Should().NotThrow();
    }

    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_handler(request));
    }
}
