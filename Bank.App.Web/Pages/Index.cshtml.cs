using Bank.Logic.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bank.App.Web.Pages;

public class IndexModel : PageModel
{
    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            Accounts = await GetAccountsAsync();
            return Page();
        }
        catch 
        {
            return RedirectToPage("/Error");
        }
    }

    public IEnumerable<Account> Accounts { get; set; } = [];

    public async Task<IEnumerable<Account>> GetAccountsAsync()
    {
        var apiClient = new BankApiClient(default, validateConnection: true);
        var accounts = await apiClient.GetAccountsAsync();
        return accounts;
    }
}
