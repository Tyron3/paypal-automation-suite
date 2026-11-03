# PayPal Automation Tests

Automated API test suite for the PayPal Transfer Handler service, built with **NUnit**, **Playwright**, and **.NET 9.0**.

## Project Structure

```
PaypalTests/
├── Infrastructure/          # Test plumbing (base fixtures, helpers, global setup)
├── Models/                  # Request/response DTOs
├── Payloads/                # Test data factories
├── Tests/                   # Test classes (one per feature/endpoint)
├── Endpoints.cs             # Centralised API route constants
├── JsonDefaults.cs          # Shared JSON serializer options
└── TestReporter.cs          # Console, JSON, and HTML report generator
```

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Playwright browsers (installed automatically — see below)

## Local Setup

1. **Clone the repository:**
   ```bash
   git clone git@gitlab.ss-prd.mukuru.io:domains/remittances/payments/paypal-automation.git
   cd paypal-automation
   ```

2. **Create your `.env` file:**
   ```bash
   cp PaypalTests/.env.example PaypalTests/.env
   ```
   Fill in the real credentials. The `.env` file is in `.gitignore` and will **never** be committed.

3. **Restore dependencies:**
   ```bash
   dotnet restore PaypalTests
   ```

4. **Install Playwright browsers:**
   ```bash
   dotnet build PaypalTests
   pwsh PaypalTests/bin/Debug/net9.0/playwright.ps1 install chromium
   ```

5. **Run all tests:**
   ```bash
   dotnet test PaypalTests
   ```

6. **Run a specific test:**
   ```bash
   dotnet test PaypalTests --filter "LinkWallet_ShouldReturn201"
   ```

## CI/CD (GitLab)

The pipeline (`.gitlab-ci.yml`) runs automatically on:

| Trigger | What runs |
|---|---|
| Push / MR | `build` → `test` |
| Manual (Run Pipeline) | `build` → `test` |
| Schedule (nightly) | `build` → `test` → `report` (GitLab Pages) |
| Default branch merge | `build` → `test` → `report` (GitLab Pages) |

### Required CI/CD Variables

Configure these in **GitLab > Settings > CI/CD > Variables** (masked + protected):

| Variable | Description |
|---|---|
| `BASEURL` | PayPal Transfer Handler base URL |
| `PAYINREFBASEURL` | Pay-In Reference service base URL |
| `KEYCLOAKURL` | Keycloak token endpoint |
| `CLIENTID` | OAuth client ID |
| `CLIENTSECRET` | OAuth client secret |
| `SCOPE` | OAuth scope |
| `PAYINREFCLIENTID` | Pay-In Ref OAuth client ID |
| `PAYINREFCLIENTSECRET` | Pay-In Ref OAuth client secret |
| `GRANTTYPE` | OAuth grant type (`client_credentials`) |
| `RETURNURL` | Wallet link return URL |

### Test Reports

- **TRX reports** are parsed by GitLab and shown in the **Tests** tab on merge requests.
- **HTML reports** are published to GitLab Pages on the default branch.
- Reports are retained for 30 days (90 days for nightly runs).

## Adding a New Test

1. Create a DTO in `Models/` if the endpoint has a new request/response shape.
2. Create a payload factory in `Payloads/` with the test data.
3. Add the endpoint path to `Endpoints.cs`.
4. Create a test class in `Tests/` (or add to an existing one for the same endpoint group).

