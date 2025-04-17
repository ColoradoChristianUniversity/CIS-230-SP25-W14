using FluentAssertions;
using Bank.Logic.Models;

namespace Bank.Logic.Tests;

public class AccountTests
{
    private readonly Account account;

    public AccountTests()
    {
        account = new Account
        {
            Settings = new AccountSettings
            {
                OverdraftFee = 35.00,
                ManagementFee = 10.00
            }
        };
    }

    [Fact]
    public void GetBalance_ShouldBeZeroInitially()
    {
        // Arrange

        // Act
        var balance = account.Balance;

        // Assert
        balance.Should().Be(0);
    }

    [Fact]
    public void Settings_ShouldBeAssignableAndRetrievable()
    {
        // Arrange
        var newSettings = new AccountSettings { OverdraftFee = 50, ManagementFee = 20 };

        // Act
        var updated = account with { Settings = newSettings };

        // Assert
        updated.Settings.Should().Be(newSettings);
    }

    [Fact]
    public void GetTransactions_ShouldBeReadOnly()
    {
        // Arrange
        var transactions = account.Transactions;

        // Act
        var action = () => ((IList<Transaction>)transactions).Add(default!);

        // Assert
        transactions.Should().BeAssignableTo<IReadOnlyList<Transaction>>();
    }

    [Theory]
    [InlineData(TransactionType.Interest)]
    [InlineData(TransactionType.Fee_Overdraft)]
    [InlineData(TransactionType.Fee_Management)]
    public void TryAddTransaction_ShouldRejectSystemTransactionTypes(TransactionType type)
    {
        // Arrange

        // Act
        var result = account.TryAddTransaction(100, type);

        // Assert
        result.Should().BeFalse();
        account.Transactions.Should().BeEmpty();
    }

    [Theory]
    [InlineData(TransactionType.Withdrawal, 100)]
    [InlineData(TransactionType.Fee_Overdraft, 50)]
    public void TryAddTransaction_ShouldRejectPositiveAmountForNegativeTypes(TransactionType type, double amount)
    {
        // Arrange

        // Act
        var result = account.TryAddTransaction(amount, type);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(TransactionType.Deposit, -100)]
    [InlineData(TransactionType.Interest, -50)]
    public void TryAddTransaction_ShouldRejectNegativeAmountForPositiveTypes(TransactionType type, double amount)
    {
        // Arrange

        // Act
        var result = account.TryAddTransaction(amount, type);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TryAddTransaction_ShouldApplyOverdraftFee()
    {
        // Arrange

        // Act
        var result = account.TryAddTransaction(-100, TransactionType.Withdrawal);

        // Assert
        result.Should().BeTrue();
        account.Transactions.Count.Should().Be(2);
        account.Transactions.Any(t => t.Type == TransactionType.Fee_Overdraft).Should().BeTrue();
        account.Balance.Should().Be(-100 - account.Settings.OverdraftFee);
    }

    [Fact]
    public void TryAddTransaction_ShouldAllowValidDeposit()
    {
        // Arrange

        // Act
        var result = account.TryAddTransaction(200, TransactionType.Deposit);

        // Assert
        result.Should().BeTrue();
        account.Balance.Should().Be(200);
        account.Transactions.Count.Should().Be(1);
    }

    [Fact]
    public void TryAddTransaction_ShouldAllowValidWithdrawal()
    {
        // Arrange
        account.TryAddTransaction(200, TransactionType.Deposit);

        // Act
        var result = account.TryAddTransaction(-100, TransactionType.Withdrawal);

        // Assert
        result.Should().BeTrue();
        account.Balance.Should().Be(100);
        account.Transactions.Count.Should().Be(2);
    }

    [Fact]
    public void Transaction_ShouldThrowOnPositiveAmountForNegativeType()
    {
        // Arrange

        // Act
        var result = new Account().TryAddTransaction(10, TransactionType.Withdrawal);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Transaction_ShouldThrowOnNegativeAmountForPositiveType()
    {
        // Arrange

        // Act
        var result = new Account().TryAddTransaction(-10, TransactionType.Deposit);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Transaction_ShouldThrowOnNaNOrInfinity()
    {
        // Arrange
        var now = DateTime.Now;

        // Act
        var nan = new Account().TryAddTransaction(double.NaN, TransactionType.Deposit);
        var posInf = new Account().TryAddTransaction(double.PositiveInfinity, TransactionType.Deposit);
        var negInf = new Account().TryAddTransaction(double.NegativeInfinity, TransactionType.Deposit);

        // Assert
        nan.Should().BeFalse();
        posInf.Should().BeFalse();
        negInf.Should().BeFalse();
    }
}
