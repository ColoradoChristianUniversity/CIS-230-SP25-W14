# Assignment 12

I have created the Console version of the UI. You are going to create the Web version of the UI. The functionality needs to be the same - create accounts, list accounts, deposit and withdraw, etc. The colors and tables in the Console are accomplished by the NuGet package `Spectre` which was created to make Console apps nice - doing much of the hard work we did in the early weeks of class. There is no requirement that the Console and the Web apps look alike, just that they behave alike. Here's a walkthrough of the Console functionality.

![](bank.gif)

## Code Walkthrough

### Projects and how they relate

As we discussed in class, we have several projects in this solution. Each has only a few classes, dedicated to the purpose of the project. For example, the `Bank.Api` project (the Web API) is the central project used by both our Console and Web projects. Those Client projects are only "coupled" to the Web API project over HTTP, that is, when they call to one of the Web API endpoints. Conversely, the Web API can never call the Console or Web projects as a Web API can only listen for requests it receives. The `Bank.Api` project (the Web API) uses the code in `Bank.Logic`. Look closely, the Console and Web projects also use the code in the `Bank.Logic` project. These references are simple project references.  

```mermaid
---
config:
  flowchart:
    defaultRenderer: elk  
    theme: default
  look: simple
  layout: fixed
---
flowchart TD
 subgraph Bank.App.Console["Bank.App.Console"]
        CFiles@{ label: "<div style=\"text-align:left;\"><li>AccountMenuScreen.cs</li><li>MainMenuScreen.cs</li><li>Program.cs</li></div>" }
  end
 subgraph Bank.Api["Bank.Api"]
        AFiles@{ label: "<div style=\"text-align:left;\"><li>EndpointHandler.cs</li><li>Program.cs</li><li>Storage.cs</li></div>" }
  end
 subgraph Bank.Logic["Bank.Logic"]
    direction LR
        LFiles@{ label: "<div style=\"text-align:left;\"><li>Account.cs</li><li>AccountSettings.cs</li><li>Transaction.cs</li><li>TransactionType.cs</li></div>" }
  end
 subgraph Bank.App.Web["Bank.App.Web"]
    direction TB
        WFiles@{ label: "<div style=\"text-align:left;\"><li>Pages/Index.cshtml</li></div>" }
  end
 subgraph Bank.App.Shared["Bank.App.Shared"]
        E1@{ label: "<div style=\"text-align:left;\"><li>BankApiClient.cs</li></div>" }
  end
    Bank.App.Console ==> Bank.Api
    Bank.App.Console -.-> Bank.App.Shared
    Bank.Api -.-> Bank.Logic & Bank.App.Shared
    Bank.App.Web ==> Bank.Api
    Bank.App.Web -.-> Bank.App.Shared
    T1{{"Bank.App.Console.Tests"}} -.-> Bank.App.Console
    T2{{"Bank.Api.Tests"}} -.-> Bank.Api
    T3{{"Bank.Logic.Tests"}} -.-> Bank.Logic
    Bank.Logic -.-> Bank.App.Console & Bank.App.Web
    CFiles@{ shape: rect}
    AFiles@{ shape: rect}
    LFiles@{ shape: rect}
    WFiles@{ shape: rect}
    E1@{ shape: rect}
     T1:::Ash
     T2:::Ash
     T3:::Ash
    classDef Ash stroke-width:1px, stroke-dasharray:none, stroke:#999999, fill:#EEEEEE, color:#000000
    style Bank.Api fill:#BBDEFB
```

### Sequence: Get a List of Accounts

As we discussed in class, a sequence diagram shows you how the flow of code works across different classes. In this case, we start in the Bank.App.Console project and invoke the operation to get a list of accounts. This operation flows through the MainMenuScreen and BankApiClient in the console project, makes an HTTP request to the Bank.Api project, and is routed to the EndpointHandler. The EndpointHandler interacts with the Storage layer to retrieve the accounts, which are deserialized from the JSON file using the Account model. The data is then returned back through the layers to be displayed in the console.

```mermaid
---
config:
  theme: neo
  look: neo
---
sequenceDiagram
    box "Bank.App.Console"
        participant ConsoleApp as Program.cs
        participant AccountMenu as AccountMenuScreen.cs
    end
    box "Bank.App.Shared"
        participant ApiClient as BankApiClient.cs
    end
    box "Bank.Api"
        participant Api as Program.cs
        participant Endpoint as EndpointHandler.cs
        participant Storage as Storage.cs
    end
    box "Bank.App.Logic"
        participant Account as Account.cs
    end

    ConsoleApp->>AccountMenu: ShowAsync()
    note right of ConsoleApp: The UI 'game loop' uses UI wrapper<br/>to keep Program.cs clean and simple
    AccountMenu->>ApiClient: DepositAsync(accountId, amount)
    note right of AccountMenu: This uses API wrapper<br/>to keep all API logic<br/>in one place
    ApiClient->>Api: POST "/deposit/{accountId}/{amount}"
    note right of ApiClient: This sends a request<br/>to the Web API over HTTP
    Api->>Endpoint: Route Request to EndpointHandler
    note right of Api: This uses Endpoint wrapper<br/>to keep Program.cs clean and simple
    Endpoint->>Storage: TryGetAccount(accountId)
    note right of Endpoint: Retrieves the account<br/>from the storage layer
    alt Account Found
        Storage-->>Endpoint: Account (if found)
        Endpoint->>Account: TryAddTransaction(amount, TransactionType.Deposit)
        note right of Endpoint: Adds the deposit transaction<br/>to the account
        alt Transaction Added Successfully
            Account-->>Endpoint: Transaction added successfully (Boolean)
            Endpoint->>Storage: UpdateAccount(account)
            note right of Endpoint: Updates the account<br/>in the storage layer
            Storage-->>Endpoint: Account updated
            Endpoint-->>Api: Success response
            Api-->>ApiClient: Success response
            note right of Api: There is a standard "Success"<br/>HTTP status, so we'll use that
            ApiClient-->>AccountMenu: Deposit successful
            AccountMenu-->>ConsoleApp: Return to menu
        else Transaction Failed
            Account-->>Endpoint: Transaction failed (e.g., invalid amount or type)
            Endpoint-->>Api: Error response (e.g., 400 Bad Request)
            note right of Api: There is no standard "Transaction Failed"<br/>HTTP status, so we'll use Bad Request
            Api-->>ApiClient: Error response (e.g., Transaction failed)
            ApiClient-->>AccountMenu: Display error message<br/>("Transaction failed")
            AccountMenu-->>ConsoleApp: Return to menu
        end
    else Account Not Found
        Storage-->>Endpoint: Account not Found (null)
        Endpoint-->>Api: Error response (e.g., 404 Not Found)
        note right of Api: There is a standard "Not Found"<br/>type of response, so we'll use that
        Api-->>ApiClient: Error response (e.g., Account not found)
        ApiClient-->>AccountMenu: Display error message<br/>("Account not found")
        AccountMenu-->>ConsoleApp: Return to menu
    end
```

### Sequence: Add a Deposit to an Account

Here's another sequence diagram. It's the only other one I will make, but this helps demonstrate simple and somewhat complex operations. Here, we again start in the Bank.App.Console project and invoke the operation to add a deposit to an account. This operation flows through the AccountMenuScreen and BankApiClient in the console project, makes an HTTP POST request to the Bank.Api project, and is routed to the EndpointHandler. The EndpointHandler retrieves the account from the Storage layer, validates the transaction using the Account model, and updates the account with the new transaction. The updated account is then saved back to the Storage layer, and returned. This sequence diagram is special because it includes ALT sections. These show you what might happen depending on the result. In this case one case is Success while another is when the Account is not found or the Deposit Transaction fails. 

```mermaid
sequenceDiagram

    box "Bank.App.Console"
        participant ConsoleApp as Program.cs
        participant AccountMenu as AccountMenuScreen.cs
        participant ApiClient as BankApiClient.cs
    end

    box "Bank.Api"
        participant Api as Program.cs
        participant Endpoint as EndpointHandler.cs
        participant Storage as Storage.cs
        participant Account as Bank.Logic.Models<br/>Account.cs
    end

    ConsoleApp->>AccountMenu: ShowAsync()
    note right of ConsoleApp: The UI 'game loop' uses UI wrapper<br/>to keep Program.cs clean and simple
    AccountMenu->>ApiClient: DepositAsync(accountId, amount)
    note right of AccountMenu: This uses API wrapper<br/>to keep all API logic<br/>in one place
    ApiClient->>Api: POST "/deposit/{accountId}/{amount}"
    note right of ApiClient: This sends a request<br/>to the Web API over HTTP
    Api->>Endpoint: Route Request to EndpointHandler
    note right of Api: This uses Endpoint wrapper<br/>to keep Program.cs clean and simple
    Endpoint->>Storage: TryGetAccount(accountId)
    note right of Endpoint: Retrieves the account<br/>from the storage layer
    alt Account Found
        Storage-->>Endpoint: Account (if found)
        Endpoint->>Account: TryAddTransaction(amount, TransactionType.Deposit)
        note right of Endpoint: Adds the deposit transaction<br/>to the account
        alt Transaction Added Successfully
            Account-->>Endpoint: Transaction added successfully (Boolean)
            Endpoint->>Storage: UpdateAccount(account)
            note right of Endpoint: Updates the account<br/>in the storage layer
            Storage-->>Endpoint: Account updated
            Endpoint-->>Api: Success response
            Api-->>ApiClient: Success response
            note right of Api: There is a standard "Success"<br/>HTTP status, so we'll use that
            ApiClient-->>AccountMenu: Deposit successful
            AccountMenu-->>ConsoleApp: Return to menu
        else Transaction Failed
            Account-->>Endpoint: Transaction failed (e.g., invalid amount or type)
            Endpoint-->>Api: Error response (e.g., 400 Bad Request)
            note right of Api: There is no standard "Transaction Failed"<br/>HTTP status, so we'll use Bad Request
            Api-->>ApiClient: Error response (e.g., Transaction failed)
            ApiClient-->>AccountMenu: Display error message<br/>("Transaction failed")
            AccountMenu-->>ConsoleApp: Return to menu
        end
    else Account Not Found
        Storage-->>Endpoint: Account not Found (null)
        Endpoint-->>Api: Error response (e.g., 404 Not Found)
        note right of Api: There is a standard "Not Found"<br/>type of response, so we'll use that
        Api-->>ApiClient: Error response (e.g., Account not found)
        ApiClient-->>AccountMenu: Display error message<br/>("Account not found")
        AccountMenu-->>ConsoleApp: Return to menu
    end

```

## Universal Acceptance Criteria

1. You must understand every single line of your solution.
2. Your code must compile and run without errors.
3. You must submit your repository URL in Brightspace.

## Assignment Requirements

1. Copy the `Console App` into a `Web App`. 

    1. Use the minimal Razor Pages web UI called `Bank.App.Web`.

2. Ensure basic functionality (at least):

_Use the Console as reference._

    - List Accounts
        - List Accounts with Balances
        - Select & View an Account
        - Create a New Account
    - View Account
        - Always Show: Name & Balance
        - Allow Deposit
            - Ask Dollar Value
        - Allow Withdraw
            - Ask Dollar Value
        - Allow Delete Account
            - Prompt the user: "Are you sure?"
        - View All Transactions
            - Show Table of Transactions
        - Delete Account

## Optional Bonus (40 points)

1. Add `Nickname` to `Account.cs`
2. Allow user to edit `Nickname` in Web
3. Provide a Sequence diagram of the operation.

**Good luck.**
