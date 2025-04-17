using Bank.Logic.Models;

using Spectre.Console;

class Program
{
    static async Task Main()
    {
        var apiClient = Create();
        var app = new ConsoleApp(apiClient);
        await app.RunAsync();
    }

    static BankApiClient Create(int retries = 3, int delayMs = 2000)
    {
        for (int i = 0; i < retries; i++)
        {
            try
            {
                return new BankApiClient(default, validateConnection: true);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");

                if (i < retries - 1)
                {
                    AnsiConsole.MarkupLine($"[yellow]Retrying in 2 seconds... ({i + 1}/{retries})[/]");
                    Thread.Sleep(delayMs);
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]Fail.[/]");
                    Console.ReadKey();
                    Environment.Exit(1);
                }
            }
        }

        return default!; // unreachable, but required
    }
}

public class ConsoleApp(IApiClient apiClient)
{
    public async Task RunAsync()
    {
        while (true)
        {
            var mainMenu = new MainMenuScreen(apiClient);
            var selectedAccount = await mainMenu.ShowAsync();

            if (selectedAccount is null)
            {
                continue;
            }

            var accountMenu = new AccountMenuScreen(apiClient, selectedAccount.Id);
            await accountMenu.ShowAsync();
        }
    }
}

public class MainMenuScreen
{
    private readonly IApiClient _apiClient;

    public MainMenuScreen(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<Account?> ShowAsync()
    {
        Console.Clear();
        var accounts = await _apiClient.GetAccountsAsync();

        var table = new Table();
        table.AddColumn("ID");
        table.AddColumn("Balance");
        foreach (var acc in accounts)
        {
            table.AddRow(acc.Id.ToString(), $"${acc.Balance:0.00}");
        }

        AnsiConsole.Write(table);

        var choices = accounts.Select(a => $"Open Account: {a.Id}").ToList();
        choices.Add("Create");
        choices.Add("Exit");

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Choose an account")
                .AddChoices(choices));

        if (choice == "Exit")
        {
            return null;
        }

        if (choice == "Create")
        {
            await _apiClient.CreateAccountAsync();
            return await ShowAsync();
        }

        var id = int.Parse(choice.Split(':')[1].Trim());
        return accounts.First(a => a.Id == id);
    }
}

public class AccountMenuScreen
{
    private readonly IApiClient _apiClient;
    private readonly int _accountId;

    public AccountMenuScreen(IApiClient apiClient, int accountId)
    {
        _apiClient = apiClient;
        _accountId = accountId;
    }

    public async Task ShowAsync()
    {
        while (true)
        {
            Console.Clear();
            var account = await _apiClient.GetAccountAsync(_accountId);
            AnsiConsole.MarkupLine($"[green]Account ID:[/] {account.Id} [green]Balance:[/] ${account.Balance:0.00}");

            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Choose an action")
                    .AddChoices("Deposit", "Withdraw", "View All Transactions", "Delete Account", "Back"));

            if (action == "Back")
            {
                break;
            }

            if (action == "Delete Account")
            {
                var confirm = AnsiConsole.Confirm("Are you sure you want to delete this account?");
                if (confirm)
                {
                    await _apiClient.DeleteAccountAsync(_accountId);
                    AnsiConsole.MarkupLine("[red]Account deleted.[/]");
                    Thread.Sleep(1000);
                    break;
                }
                continue;
            }
            else if (action == "View All Transactions")
            {
                var transactions = account.Transactions;

                if (transactions.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No transactions found.[/]");
                }
                else
                {
                    var txTable = new Table();
                    txTable.AddColumn("Date");
                    txTable.AddColumn("Type");
                    txTable.AddColumn("Amount");

                    foreach (var tx in transactions)
                    {
                        txTable.AddRow(
                            tx.Date.ToString("g"),
                            tx.Type.ToString(),
                            $"${tx.Amount:0.00}");
                    }

                    AnsiConsole.Write(txTable);
                }

                AnsiConsole.MarkupLine("[grey]Press any key to return...[/]");
                Console.ReadKey();
                continue;
            }

            var amount = AnsiConsole.Ask<double>("Enter amount:");

            if (action == "Deposit")
            {
                await _apiClient.DepositAsync(_accountId, amount);
            }
            else if (action == "Withdraw")
            {
                await _apiClient.WithdrawAsync(_accountId, amount);
            }
        }
    }
}


