using FluentAssertions;

namespace Bank.Logic.Tests;

public class UtilityTests
{
    [Theory]
    [InlineData(TransactionType.Withdrawal)]
    [InlineData(TransactionType.Fee_Overdraft)]
    [InlineData(TransactionType.Fee_Management)]
    public void InidicatesNegativeAmount_ShouldReturnTrue_ForNegativeTypes(TransactionType type)
    {
        type.InidicatesNegativeAmount().Should().BeTrue($"{type} should indicate a negative amount");
    }

    [Theory]
    [InlineData(TransactionType.Deposit)]
    [InlineData(TransactionType.Interest)]
    [InlineData(TransactionType.Unknown)]
    public void InidicatesNegativeAmount_ShouldReturnFalse_ForNonNegativeTypes(TransactionType type)
    {
        type.InidicatesNegativeAmount().Should().BeFalse($"{type} should not indicate a negative amount");
    }

    [Theory]
    [InlineData(TransactionType.Deposit)]
    [InlineData(TransactionType.Interest)]
    public void InidicatesPositiveAmount_ShouldReturnTrue_ForPositiveTypes(TransactionType type)
    {
        type.InidicatesPositiveAmount().Should().BeTrue($"{type} should indicate a positive amount");
    }

    [Theory]
    [InlineData(TransactionType.Withdrawal)]
    [InlineData(TransactionType.Fee_Overdraft)]
    [InlineData(TransactionType.Fee_Management)]
    [InlineData(TransactionType.Unknown)]
    public void InidicatesPositiveAmount_ShouldReturnFalse_ForNonPositiveTypes(TransactionType type)
    {
        type.InidicatesPositiveAmount().Should().BeFalse($"{type} should not indicate a positive amount");
    }

    [Theory]
    [InlineData(TransactionType.Fee_Overdraft)]
    [InlineData(TransactionType.Fee_Management)]
    [InlineData(TransactionType.Interest)]
    public void IndicatesSystemType_ShouldReturnTrue_ForSystemTypes(TransactionType type)
    {
        type.IndicatesSystemType().Should().BeTrue($"{type} should be considered a system transaction");
    }

    [Theory]
    [InlineData(TransactionType.Deposit)]
    [InlineData(TransactionType.Withdrawal)]
    [InlineData(TransactionType.Unknown)]
    public void IndicatesSystemType_ShouldReturnFalse_ForNonSystemTypes(TransactionType type)
    {
        type.IndicatesSystemType().Should().BeFalse($"{type} should not be considered a system transaction");
    }
}