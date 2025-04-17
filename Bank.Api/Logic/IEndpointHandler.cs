namespace Bank.Api.Logic
{
    public interface IEndpointHandler
    {
        Task<IResult> CreateAccountAsync();
        Task<IResult> DeleteAccountAsync(int accountId);
        Task<IResult> GetAccountAsync(int accountId);
        IResult GetDefaultSettings();
        Task<IResult> WithdrawAsync(int accountId, double amount);
        Task<IResult> DepositAsync(int accountId, double amount);
        Task<IResult> GetTransactionHistoryAsync(int accountId);
        Task<IResult> AddTransactionAsync(int accountId, string type, double amount);
        Task<IResult> ListAccountsAsync();
    }
}
