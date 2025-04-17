using Bank.Api.Logic;

namespace Bank.Api;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddSingleton<IEndpointHandler, EndpointHandler>();
        builder.Services.AddSingleton<IStorage, Storage>();

        var app = builder.Build();
        app.UseSwagger();
        app.UseSwaggerUI();

        ConfigureEndpoints(app);

        app.Run();
    }

    private static void ConfigureEndpoints(WebApplication app)
    {
        app.MapGet("/", async (IEndpointHandler handler) => 
            await handler.ListAccountsAsync());

        app.MapPost("/account", async (IEndpointHandler handler) => 
            await handler.CreateAccountAsync());

        app.MapDelete("/account/{accountId}", async (IEndpointHandler handler, int accountId) => 
            await handler.DeleteAccountAsync(accountId));

        app.MapGet("/account/{accountId}", async (IEndpointHandler handler, int accountId) => 
            await handler.GetAccountAsync(accountId));

        app.MapPost("/withdraw/{accountId}/{amount}", async (IEndpointHandler handler, int accountId, double amount) =>
            await handler.WithdrawAsync(accountId, amount));

        app.MapPost("/deposit/{accountId}/{amount}", async (IEndpointHandler handler, int accountId, double amount) => 
            await handler.DepositAsync(accountId, amount));

        app.MapGet("/transactions/{accountId}", async (IEndpointHandler handler, int accountId) => 
            await handler.GetTransactionHistoryAsync(accountId));

        app.MapPost("/transactions/{accountId}/{type}/{amount}", async (IEndpointHandler handler, int accountId, string type, double amount) => 
            await handler.AddTransactionAsync(accountId, type, amount));
    }
}