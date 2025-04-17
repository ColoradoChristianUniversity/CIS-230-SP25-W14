using Bank.Logic.Models;

namespace Bank.Api.Logic;

public interface IStorage
{
    IEnumerable<Account> ListAccounts();
    bool TryGetAccount(int id, out Account account);
    Account NewAccount();
    void RemoveAccount(int id);
    Account UpdateAccount(Account account);
    IReadOnlyList<Transaction> GetTransactions(int accountId);
}
