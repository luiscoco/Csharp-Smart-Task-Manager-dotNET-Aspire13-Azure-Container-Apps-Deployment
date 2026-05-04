# SmartTaskManager

SmartTaskManager is a portfolio-ready `.NET 10` task management solution built with Clean Architecture, an ASP.NET Core Web API, and a modern Blazor Web App front end.

The solution demonstrates how to separate domain logic, application workflows, infrastructure concerns, HTTP endpoints, and UI composition while keeping the code approachable for a beginner-to-intermediate .NET course.

Security status:

- `SmartTaskManager.Web` now uses Microsoft Entra ID with OpenID Connect sign-in and a secure cookie session
- `SmartTaskManager.Api` now validates Microsoft Entra JWT bearer tokens
- `SmartTaskManager.Web` now acquires delegated access tokens server-side and forwards them to `SmartTaskManager.Api`
- the Entra setup assumptions are documented in [docs/entra-security-configuration.md](docs/entra-security-configuration.md)
- Azure Container Apps deployment notes are documented in [docs/azure-container-apps-deployment-notes.md](docs/azure-container-apps-deployment-notes.md)

## Solution Overview

SmartTaskManager is organized around two main executable projects:

- `SmartTaskManager.Api`: the backend HTTP API that exposes users, tasks, dashboard, history, and task-action endpoints
- `SmartTaskManager.Web`: the Blazor Web App front end that consumes the API through typed `HttpClient` services

Supporting those projects are the core Clean Architecture layers:

- `SmartTaskManager.Domain`
- `SmartTaskManager.Application`
- `SmartTaskManager.Infrastructure`

## Solution Structure

```text
SmartTaskManager/
├─ SmartTaskManager.sln
├─ README.md
├─ docs/
│  ├─ architecture.md
│  └─ web-architecture.md
├─ src/
│  ├─ SmartTaskManager.Domain/
│  ├─ SmartTaskManager.Application/
│  ├─ SmartTaskManager.Infrastructure/
│  ├─ SmartTaskManager.Api/
│  └─ SmartTaskManager.Web/
└─ tests/
   ├─ SmartTaskManager.Domain.Tests/
   ├─ SmartTaskManager.Application.Tests/
   └─ SmartTaskManager.Infrastructure.Tests/
```

## Project Roles

### SmartTaskManager.Api

`SmartTaskManager.Api` is the backend boundary of the system.

Its responsibilities include:

- exposing REST endpoints for users and tasks
- returning dashboard summary data
- handling task actions such as priority updates, completion, and archiving
- serving as the protected resource that will move to Microsoft Entra JWT bearer validation
- coordinating the Application and Infrastructure layers without leaking UI concerns

### SmartTaskManager.Web

`SmartTaskManager.Web` is the front-end project built with the modern Blazor Web App template using **server interactivity only**.

Its responsibilities include:

- layout, navigation, and page composition
- signing interactive users in with Microsoft Entra ID
- maintaining the authenticated web session with a secure cookie
- calling the existing API through typed `HttpClient` services
- acquiring delegated access tokens for the protected API in the service layer
- handling form validation and user feedback
- presenting dashboard metrics, lists, filters, details, and task actions in a clean UI

There is **no separate `.Client` project**, which keeps the setup simpler for learning while still using the current Blazor programming model.

## How the Front End Communicates with the API

Current implementation:

The front end does not duplicate business logic and does not call backend services directly.

Instead, `SmartTaskManager.Web` communicates with `SmartTaskManager.Api` over HTTP through typed service classes:

- `UsersApiClient`
- `TasksApiClient`

The API base URL is read from configuration:

- Development: `src/SmartTaskManager.Web/appsettings.Development.json`
- Production: `src/SmartTaskManager.Web/appsettings.Production.json`

Current security flow:

1. The user opens a protected page in `SmartTaskManager.Web`.
2. The web app challenges with Microsoft Entra ID over OpenID Connect.
3. After a successful sign-in, the web app stores the session in a secure ASP.NET Core authentication cookie.
4. The web app acquires a delegated access token from the server-side session and attaches it to outgoing API calls.
5. `SmartTaskManager.Api` accepts only Microsoft Entra bearer tokens for protected endpoints.

This keeps the architecture clean:

```text
Browser
  -> SmartTaskManager.Web
      -> SmartTaskManager.Api
          -> Application / Domain / Infrastructure
```

Target security direction:

- `SmartTaskManager.Web` signs users in with Microsoft Entra ID over OpenID Connect
- the web app keeps a secure cookie-based session
- the web app acquires delegated access tokens for `SmartTaskManager.Api` in the service layer
- `SmartTaskManager.Api` validates Microsoft Entra JWT bearer tokens

The current repository already contains the sign-in, delegated token forwarding, and bearer-validation foundation. Claims-based authorization remains the next implementation step.

## Entra Configuration Preparation

The repository now includes Entra-ready configuration sections for both executable projects.

- Web app registration: `SmartTaskManager.Web`
- API registration: `SmartTaskManager.Api`
- Web redirect URI for local development: `https://localhost:7036/signin-oidc`
- Web signed-out callback URI: `https://localhost:7036/signout-callback-oidc`
- API audience: `api://3bede5d9-a947-4d25-a3c1-54df15d5ed60`
- API delegated scope: `api://3bede5d9-a947-4d25-a3c1-54df15d5ed60/Tasks.ReadWrite`

Important:

- do not commit the web app client secret to source control
- store the secret outside the repository with user secrets or environment variables
- see [docs/entra-security-configuration.md](docs/entra-security-configuration.md) for the exact configuration layout

## Main UI Features

The Blazor front end currently includes:

- Microsoft Entra sign-in with a secure cookie-based web session
- dashboard summary with total, completed, pending, and overdue tasks
- local user review and workspace selection UI for development data
- task list with status and priority filtering
- task cards with clear status and priority badges
- task details view with history entries
- create task form with client-side and API-backed validation
- task actions for:
  - update priority
  - mark as completed
  - archive
- loading, empty, success, and error states designed for a polished portfolio presentation

## Technologies Used

- `.NET 10`
- `C#`
- `ASP.NET Core Web API`
- `Blazor Web App`
- `HttpClient` typed API clients
- `SQL Server`
- Microsoft Entra ID configuration prepared for Web OIDC and API bearer validation
- Clean Architecture

## How to Run Locally

### Prerequisites

- `.NET 10 SDK`
- local SQL Server available for the API connection string
- trusted local development HTTPS certificate

If needed, trust the local HTTPS certificate once:

```powershell
dotnet dev-certs https --trust
```

### 1. Restore and Build

From the solution root:

```powershell
dotnet restore .\SmartTaskManager.sln
dotnet build .\SmartTaskManager.sln
```

### 1.1 Configure the Web App Secret for Local Development

The web app client secret must stay outside source control.

Set it with user secrets:

```powershell
dotnet user-secrets set "AzureAd:ClientSecret" "<web-client-secret>" --project .\src\SmartTaskManager.Web\SmartTaskManager.Web.csproj
```

### 2. Run the API

Open a terminal and run:

```powershell
dotnet run --project .\src\SmartTaskManager.Api\SmartTaskManager.Api.csproj --launch-profile https
```

Expected development URLs:

- `https://localhost:7081`
- `http://localhost:5081`

Swagger:

- `https://localhost:7081/swagger`

### 3. Run the Blazor Web App

Open a second terminal and run:

```powershell
dotnet run --project .\src\SmartTaskManager.Web\SmartTaskManager.Web.csproj --launch-profile https
```

Expected development URLs:

- `https://localhost:7036`
- `http://localhost:5269`

### 4. Open the App

- Web front end: `https://localhost:7036`
- API Swagger: `https://localhost:7081/swagger`

Important:

- `SmartTaskManager.Web` expects `SmartTaskManager.Api` to be running
- if the web app reports that it cannot reach the API, check that the API is listening on `https://localhost:7081`
- when prompted, sign in with a user from the configured Microsoft Entra tenant

## Front-End Notes

The Blazor front end is intentionally simple in architecture:

- one server-interactive UI project
- typed `HttpClient` services for API access
- thin pages
- reusable shared components for layout, feedback, loading, empty states, and task presentation

This keeps the code easy to follow while still reflecting real-world front-end patterns.

## Future Improvements

- add integration tests covering API-to-UI flows
- persist user session state across browser refreshes
- add authentication and authorization for real multi-user scenarios
- introduce pagination and sorting for larger task lists
- add deployment configuration for cloud hosting
- improve observability with structured logging and health checks
- extend the dashboard with charts or trend summaries

## Why This Project Works Well in a Portfolio

SmartTaskManager shows more than a basic CRUD app:

- layered architecture with clear boundaries
- API-first backend design
- modern Blazor front end
- typed service-based HTTP integration
- realistic forms, validation, and task workflows
- UI polish without excessive front-end complexity

It demonstrates both software design discipline and practical full-stack `.NET` implementation.

## Author

**Name:** _Your Name Here_

**GitHub:** _Your GitHub Profile_

**LinkedIn:** _Your LinkedIn Profile_
