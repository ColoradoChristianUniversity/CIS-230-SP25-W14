using System.Reflection;

using Bank.Logic.Models;

namespace Bank.Logic;

public static class AccountExtensions
{
    public static bool TryAddTransaction(this Account account, double amount, TransactionType type)
    {
        if (double.IsNaN(amount) || double.IsInfinity(amount))
        {
            return false;
        }

        if (type.IndicatesSystemType())
        {
            return false;
        }

        var isNegative = TransactionTypeExtensions.InidicatesNegativeAmount(type);
        if (isNegative && amount >= 0)
        {
            return false;
        }

        if (!isNegative && amount < 0)
        {
            return false;
        }

        var now = DateTime.Now;
        var transaction = new Transaction(type, amount, now);

        var list = GetWritableTransactionList(account);
        list.Add(transaction);

        var balance = list.Sum(t => t.Amount);

        if (type == TransactionType.Withdrawal && balance < 0)
        {
            var overdraft = new Transaction(TransactionType.Fee_Overdraft, -Math.Abs(account.Settings.OverdraftFee), now);
            list.Add(overdraft);
        }

        return true;
    }

    private static List<Transaction> GetWritableTransactionList(Account account)
    {
        var prop = typeof(Account).GetProperty("txns", BindingFlags.NonPublic | BindingFlags.Instance);
        if (prop is null)
        {
            throw new InvalidOperationException("Unable to access txns property.");
        }

        return (List<Transaction>)prop.GetValue(account)!;
    }
}
