using Bank.Logic;
using Bank.Logic.Models;

namespace Bank.Api.Logic;

public class EndpointHandler(IStorage storage) : IEndpointHandler
{
    public async Task<IResult> AddTransactionAsync(int accountId, string type, double amount)
    {
        return await WrapperAsync(Do);

        IResult Do()
        {
            if (!storage.TryGetAccount(accountId, out var account))
            {
                return Results.NotFound($"Account {accountId} not found");
            }

            if (!Enum.TryParse<TransactionType>(type, true, out var transactionType))
            {
                return Results.BadRequest($"Invalid transaction type: {type}");
            }

            if (!account.TryAddTransaction(amount, transactionType))
            {
                return Results.BadRequest($"Transaction of type {transactionType} failed for account {accountId}");
            }

            storage.UpdateAccount(account);
            return Results.Ok();
        }
    }

    public async Task<IResult> CreateAccountAsync()
    {
        return await WrapperAsync(Do);

        IResult Do()
        {
            return Results.Ok(storage.NewAccount());
        }
    }

    public async Task<IResult> DeleteAccountAsync(int accountId)
    {
        return await WrapperAsync(Do);

        IResult Do()
        {
            storage.RemoveAccount(accountId);
            return Results.Ok();
        }
    }

    public async Task<IResult> DepositAsync(int accountId, double amount)
    {
        return await WrapperAsync(Do);

        IResult Do()
        {
            if (!storage.TryGetAccount(accountId, out var account))
            {
                return Results.NotFound($"Account {accountId} not found");
            }

            if (!account.TryAddTransaction(amount, TransactionType.Deposit))
            {
                return Results.BadRequest($"Deposit of {amount} failed for account {accountId}");
            }

            storage.UpdateAccount(account);
            return Results.Ok();
        }
    }

    public async Task<IResult> GetAccountAsync(int accountId)
    {
        return await WrapperAsync(Do);

        IResult Do()
        {
            if (!storage.TryGetAccount(accountId, out var account))
            {
                return Results.NotFound($"Account {accountId} not found");
            }

            return Results.Ok(account);
        }
    }

    public IResult GetDefaultSettings()
    {
        return Results.Ok(new AccountSettings());
    }

    public async Task<IResult> GetTransactionHistoryAsync(int accountId)
    {
        return await WrapperAsync(Do);

        IResult Do()
        {
            if (!storage.TryGetAccount(accountId, out var account))
            {
                return Results.NotFound($"Account {accountId} not found");
            }

            var transactions = storage.GetTransactions(accountId);
            return Results.Json(transactions);
        }
    }

    public async Task<IResult> ListAccountsAsync()
    {
        return await WrapperAsync(Do);

        IResult Do()
        {
            var accounts = storage.ListAccounts();
            return Results.Ok(accounts);
        }
    }

    public async Task<IResult> WithdrawAsync(int accountId, double amount)
    {
        return await WrapperAsync(Do);

        IResult Do()
        {
            if (!storage.TryGetAccount(accountId, out var account))
            {
                return Results.NotFound($"Account {accountId} not found");
            }

            var balance = account.Balance;
            if (balance < amount)
            {
                return Results.BadRequest("Insufficient funds");
            }

            account.TryAddTransaction(-Math.Abs(amount), TransactionType.Withdrawal);
            return Results.Ok();
        }

    }

    public static async Task<IResult> WrapperAsync(Func<IResult> action)
    {
        try
        {
            return await Task.Run(action);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem($"An error occurred: {ex.Message}");
        }
    }
}
