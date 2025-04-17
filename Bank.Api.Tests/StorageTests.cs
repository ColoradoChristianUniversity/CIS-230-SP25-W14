using FluentAssertions;
using Bank.Logic;
using Bank.Api.Logic;

namespace Bank.Api.Tests;

public class StorageTests : IDisposable
{
    private readonly string testFilePath = Path.Combine(AppContext.BaseDirectory, "test_store.json");
    private readonly Storage storage;

    public StorageTests()
    {
        Dispose();

        storage = new Storage(testFilePath);
    }

    public void Dispose()
    {
        if (File.Exists(testFilePath))
        {
            File.Delete(testFilePath);
        }
    }

    [Fact]
    public void Constructor_InitializesEmptyStorage()
    {
        // Arrange & Act
        var result = storage;

        // Assert
        result.Should().NotBeNull();
        result.ListAccounts().Should().BeEmpty();
    }

    [Fact]
    public void NewAccount_AssignsUniqueIds()
    {
        // Arrange & Act
        var account1 = storage.NewAccount();
        var account2 = storage.NewAccount();

        // Assert
        account1.Id.Should().NotBe(account2.Id);
    }

    [Fact]
    public void TryGetAccount_ReturnsTrue_WhenAccountExists()
    {
        // Arrange
        var account = storage.NewAccount();

        // Act
        var result = storage.TryGetAccount(account.Id, out var retrieved);

        // Assert
        result.Should().BeTrue();
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(account.Id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(9999)]
    public void TryGetAccount_ReturnsFalse_WhenAccountDoesNotExist(int id)
    {
        // Arrange & Act
        var result = storage.TryGetAccount(id, out var account);

        // Assert
        result.Should().BeFalse();
        account.Should().BeNull();
    }

    [Fact]
    public void RemoveAccount_DeletesAccount()
    {
        // Arrange
        var account = storage.NewAccount();

        // Act
        storage.RemoveAccount(account.Id);

        // Assert
        storage.TryGetAccount(account.Id, out _).Should().BeFalse();
    }

    [Fact]
    public void RemoveAccount_DoesNothing_WhenAccountDoesNotExist()
    {
        // Arrange
        var maxId = storage.ListAccounts().Select(x => x.Id).DefaultIfEmpty(0).Max();
        var nonExistentId = maxId + 1;

        // Act
        storage.RemoveAccount(nonExistentId);

        // Assert
        storage.TryGetAccount(nonExistentId, out _).Should().BeFalse();
    }

    [Fact]
    public void Storage_PersistsDataBetweenInstances()
    {
        // Arrange
        var account = storage.NewAccount();

        // Act
        var reopened = new Storage(testFilePath);
        var result = reopened.TryGetAccount(account.Id, out var loaded);

        // Assert
        result.Should().BeTrue();
        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(account.Id);
    }

    [Fact]
    public void ListAccounts_ReturnsAllAccountIds()
    {
        // Arrange
        var a1 = storage.NewAccount();
        var a2 = storage.NewAccount();

        // Act
        var list = storage.ListAccounts();

        // Assert
        list.Should().Contain(a1);
        list.Should().Contain(a2);
        list.Should().HaveCount(2);
    }

    [Fact]
    public void AddTransactionToAccount_ShouldUpdateAccount()
    {
        // Arrange
        var account = storage.NewAccount();

        // Act
        account.TryAddTransaction(100.0, TransactionType.Deposit).Should().BeTrue();
        var updated = storage.UpdateAccount(account);

        // Assert
        updated.Transactions.Should().ContainSingle(t =>
            t.Amount == 100.0 && t.Type == TransactionType.Deposit);
    }

    [Fact]
    public void GetTransactions_ReturnsEmptyList_WhenAccountMissing()
    {
        // Arrange
        int fakeId = storage.ListAccounts().Any() ? storage.ListAccounts().Max(x => x.Id) + 1 : 1;

        // Act
        var transactions = storage.GetTransactions(fakeId);

        // Assert
        transactions.Should().BeEmpty();
    }

    [Fact]
    public void GetTransactions_ReturnsAllTransactions()
    {
        // Arrange
        var account = storage.NewAccount();
        account.TryAddTransaction(200.0, TransactionType.Deposit).Should().BeTrue();
        account.TryAddTransaction(-50.0, TransactionType.Withdrawal).Should().BeTrue();
        storage.UpdateAccount(account);

        // Act
        var transactions = storage.GetTransactions(account.Id);

        // Assert
        transactions.Should().HaveCount(2);
        transactions.Should().Contain(t => t.Type == TransactionType.Deposit && t.Amount == 200.0);
        transactions.Should().Contain(t => t.Type == TransactionType.Withdrawal && t.Amount == -50.0);
    }

    [Fact]
    public void Constructor_HandlesInvalidJson()
    {
        // Arrange
        File.WriteAllText(testFilePath, "not valid json");

        // Act
        var reloaded = new Storage(testFilePath);

        // Assert
        reloaded.ListAccounts().Should().BeEmpty();
    }
}
