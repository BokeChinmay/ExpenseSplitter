# Evenly - Smart Expense Splitter

A full-stack expense splitting app built with ASP.NET Core and Blazor WebAssembly.
Split bills, track group spending, and settle up with AI receipt parsing and 
a debt-simplification algorithm that minimizes transactions.

## Features

- **JWT Authentication** - secure register/login with BCrypt password hashing
- **Group management** - create groups, invite members by email
- **Expense tracking** - log expenses with equal, percentage, or custom splits
- **Debt simplification** - graph algorithm that minimizes settlement transactions
- **AI receipt parsing** - upload a photo, auto-fill description, amount, and category
- **Spending insights** - category breakdowns, member spending, AI-generated summaries
- **Real-time settlements** - always up to date after every expense

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core 9 Web API |
| Frontend | Blazor WebAssembly |
| Language | C# throughout |
| Database | PostgreSQL + Entity Framework Core |
| Auth | JWT Bearer + BCrypt |
| AI - Receipt Parsing | Groq Vision API |
| AI - Insights | Groq Llama 3.3 70B |
| ORM | EF Core code-first migrations |

## Architecture

```
Evenly/
├── ExpenseSplitter.Api/          # ASP.NET Core backend
│   ├── Controllers/              # AuthController, GroupsController
│   ├── Data/                     # AppDbContext, EF migrations
│   ├── Models/                   # Domain models, DTOs
│   └── Services/                 # ExpenseService, DebtSimplificationService,
│                                 # ReceiptParsingService, InsightsService, AuthService
└── ExpenseSplitter.Web/          # Blazor WASM frontend
    ├── Auth/                     # JwtAuthStateProvider
    ├── Models/                   # Client-side DTOs
    ├── Pages/                    # Login, Dashboard, GroupDetail
    ├── Services/                 # ApiService
    └── Shared/                   # MainLayout, EmptyLayout
```

## How the Debt Simplification Works

Given a group where multiple people paid for different things, the naive approach 
generates one transaction per debt: O(n²) complexity. This app uses a greedy 
algorithm that first nets everyone's balance, then repeatedly matches the largest 
creditor with the largest debtor, minimizing total transactions.

For example, three people with five debts between them can often settle in just 
two transactions instead of five.

## Running Locally

**Prerequisites:** .NET 9 SDK, PostgreSQL, Groq API key

```bash
git clone https://github.com/BokeChinmay/ExpenseSplitter.git
cd ExpenseSplitter

# API secrets
cd ExpenseSplitter.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=expensesplitter;Username=postgres;Password=yourpassword"
dotnet user-secrets set "Jwt:Secret" "your-secret-key-at-least-32-chars"
dotnet user-secrets set "Groq:ApiKey" "your-groq-key"

# Terminal 1 — API
dotnet run

# Terminal 2 — Frontend  
cd ../ExpenseSplitter.Web
dotnet run
```

Then open `http://localhost:5211`.

## Live Demo

[bokechinmay.github.io/expensesplitter](https://bokechinmay.github.io/expensesplitter)