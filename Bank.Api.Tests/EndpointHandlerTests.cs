using Bank.Api.Logic;
using Bank.Logic;
using Microsoft.AspNetCore.Http.HttpResults;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Bank.Logic.Models;

namespace Bank.Api.Tests;

public class EndpointHandlerTests : IDisposable
{
    private readonly EndpointHandler handler;
    private readonly string testFile = "Test.json";

    public EndpointHandlerTests()
    {
        Dispose();

        handler = new EndpointHandler(new Storage(testFile));
    }

    public void Dispose()
    {
        if (File.Exists(testFile))
        {
            File.Delete(testFile);
        }
    }

    [Fact]
    public async Task CreateAccount_NoInput_ReturnsOk()
    {
        // Arrange

        // Act
        var result = await handler.CreateAccountAsync();

        // Assert
        result.Should().BeOfType<Ok<Account>>();
        var account = (result as Ok<Account>)!.Value;
        account.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAccount_ValidId_RemovesAccount()
    {
        // Arrange
        var createResult = await handler.CreateAccountAsync();
        var accountId = ((Ok<Account>)createResult).Value!.Id;

        // Act
        var deleteResult = await handler.DeleteAccountAsync(accountId);

        // Assert
        deleteResult.Should().BeOfType<Ok>();
        var notFound = await handler.GetAccountAsync(accountId);
        notFound.Should().BeOfType<NotFound<string>>();
    }

    [Fact]
    public async Task GetAccount_ValidId_ReturnsOk()
    {
        // Arrange
        var createResult = await handler.CreateAccountAsync();
        var accountId = ((Ok<Account>)createResult).Value!.Id;

        // Act
        var getResult = await handler.GetAccountAsync(accountId);

        // Assert
        getResult.Should().BeOfType<Ok<Account>>();
        var account = ((Ok<Account>)getResult).Value;
        account.Should().NotBeNull();
        account.Id.Should().Be(accountId);
    }

    [Fact]
    public async Task GetAccount_InvalidId_ReturnsNotFound()
    {
        // Arrange

        // Act
        var result = await handler.GetAccountAsync(999);

        // Assert
        result.Should().BeOfType<NotFound<string>>();
    }

    [Fact]
    public async Task ListAccounts_NoInput_ReturnsAllAccounts()
    {
        // Arrange
        await handler.CreateAccountAsync();
        await handler.CreateAccountAsync();

        // Act
        var result = await handler.ListAccountsAsync();

        // Assert
        result.Should().BeOfType<Ok<IEnumerable<Account>>>();
        var ids = ((Ok<IEnumerable<Account>>)result).Value!.Select(x => x.Id);
        ids.Should().HaveCount(2);
    }

    [Fact]
    public async Task AddTransaction_ValidInput_ReturnsOk()
    {
        // Arrange
        var createResult = await handler.CreateAccountAsync();
        var accountId = ((Ok<Account>)createResult).Value!.Id;

        // Act
        var txResult = await handler.AddTransactionAsync(accountId, "Deposit", 100);

        // Assert
        txResult.Should().BeOfType<Ok>();
    }

    [Fact]
    public async Task AddTransaction_InvalidType_ReturnsBadRequest()
    {
        // Arrange
        var createResult = await handler.CreateAccountAsync();
        var accountId = ((Ok<Account>)createResult).Value!.Id;

        // Act
        var txResult = await handler.AddTransactionAsync(accountId, "InvalidType", 100);

        // Assert
        txResult.Should().BeOfType<BadRequest<string>>();
    }

    [Fact]
    public async Task AddTransaction_SystemType_ReturnsBadRequest()
    {
        // Arrange
        var createResult = await handler.CreateAccountAsync();
        var accountId = ((Ok<Account>)createResult).Value!.Id;

        // Act
        var txResult = await handler.AddTransactionAsync(accountId, TransactionType.Fee_Overdraft.ToString(), 100);

        // Assert
        txResult.Should().BeOfType<BadRequest<string>>();
    }

    [Fact]
    public async Task AddTransaction_InvalidAccount_ReturnsNotFound()
    {
        // Arrange

        // Act
        var result = await handler.AddTransactionAsync(999, "Deposit", 100);

        // Assert
        result.Should().BeOfType<NotFound<string>>();
    }

    [Fact]
    public async Task Withdraw_InvalidAccount_ReturnsNotFound()
    {
        // Arrange

        // Act
        var result = await handler.WithdrawAsync(999, 100);

        // Assert
        result.Should().BeOfType<NotFound<string>>();
    }

    [Fact]
    public async Task Withdraw_InsufficientFunds_ReturnsBadRequest()
    {
        // Arrange
        var createResult = await handler.CreateAccountAsync();
        var accountId = ((Ok<Account>)createResult).Value!.Id;

        // Act
        var withdrawal = await handler.WithdrawAsync(accountId, 100);

        // Assert
        withdrawal.Should().BeOfType<BadRequest<string>>();
    }

    [Fact]
    public async Task Withdraw_SufficientFunds_ReturnsOk()
    {
        // Arrange
        var createResult = await handler.CreateAccountAsync();
        var accountId = ((Ok<Account>)createResult).Value!.Id;
        await handler.DepositAsync(accountId, 200);

        // Act
        var withdrawal = await handler.WithdrawAsync(accountId, 100);

        // Assert
        withdrawal.Should().BeOfType<Ok>();
    }

    [Fact]
    public async Task Deposit_InvalidAccount_ReturnsNotFound()
    {
        // Arrange

        // Act
        var result = await handler.DepositAsync(999, 100);

        // Assert
        result.Should().BeOfType<NotFound<string>>();
    }

    [Fact]
    public async Task Deposit_ValidAccount_ReturnsOk()
    {
        // Arrange
        var createResult = await handler.CreateAccountAsync();
        var accountId = ((Ok<Account>)createResult).Value!.Id;

        // Act
        var deposit = await handler.DepositAsync(accountId, 100);

        // Assert
        deposit.Should().BeOfType<Ok>();
    }

    [Fact]
    public async Task Deposit_InvalidAmount_ReturnsBadRequest()
    {
        // Arrange
        var createResult = await handler.CreateAccountAsync();
        var accountId = ((Ok<Account>)createResult).Value!.Id;

        // Act
        var deposit = await handler.DepositAsync(accountId, -100);

        // Assert
        deposit.Should().BeOfType<BadRequest<string>>();
    }

    [Fact]
    public async Task GetTransactionHistory_ValidAccount_ReturnsJson()
    {
        // Arrange
        var createResult = await handler.CreateAccountAsync();
        var accountId = ((Ok<Account>)createResult).Value!.Id;
        await handler.AddTransactionAsync(accountId, "Deposit", 100);

        // Act
        var history = await handler.GetTransactionHistoryAsync(accountId);

        // Assert
        history.Should().BeOfType<JsonHttpResult<IReadOnlyList<Transaction>>>();
        var transactions = ((JsonHttpResult<IReadOnlyList<Transaction>>)history).Value;
        transactions.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetTransactionHistory_InvalidAccount_ReturnsNotFound()
    {
        // Arrange

        // Act
        var result = await handler.GetTransactionHistoryAsync(999);

        // Assert
        result.Should().BeOfType<NotFound<string>>();
    }

    [Fact]
    public void GetDefaultSettings_NoInput_ReturnsSettings()
    {
        // Arrange

        // Act
        var result = handler.GetDefaultSettings();

        // Assert
        result.Should().BeOfType<Ok<AccountSettings>>();
        var settings = ((Ok<AccountSettings>)result).Value;
        settings.Should().NotBeNull();
    }

    [Fact]
    public async Task Wrapper_Success_ReturnsOk()
    {
        // Arrange

        // Act
        var result = await EndpointHandler.WrapperAsync(() => Results.Ok("yay"));

        // Assert
        result.Should().BeOfType<Ok<string>>().Which.Value.Should().Be("yay");
    }

    [Fact]
    public async Task Wrapper_ArgumentException_ReturnsBadRequest()
    {
        // Arrange

        // Act
        var result = await EndpointHandler.WrapperAsync(() => throw new ArgumentException("bad"));

        // Assert
        result.Should().BeOfType<BadRequest<string>>().Which.Value.Should().Be("bad");
    }

    [Fact]
    public async Task Wrapper_InvalidOperationException_ReturnsConflict()
    {
        // Arrange

        // Act
        var result = await EndpointHandler.WrapperAsync(() => throw new InvalidOperationException("fail"));

        // Assert
        result.Should().BeOfType<Conflict<string>>().Which.Value.Should().Be("fail");
    }

    [Fact]
    public async Task Wrapper_UnhandledException_ReturnsProblem()
    {
        // Arrange

        // Act
        var result = await EndpointHandler.WrapperAsync(() => throw new Exception("boom"));

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        ((ProblemHttpResult)result).ProblemDetails.Detail.Should().Contain("boom");
    }
}
