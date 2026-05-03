# SmartTaskManager Web Architecture

## Chosen Blazor Setup

`SmartTaskManager.Web` uses the modern **Blazor Web App** template with **server interactivity only**.

This means:

- there is **no** separate `.Client` project
- interactive components run on the web server through Blazor Server
- the browser connects to `SmartTaskManager.Web`
- `SmartTaskManager.Web` calls `SmartTaskManager.Api` over HTTP

This is the simplest setup that still feels modern and scalable for a portfolio project.

## Solution Structure

```text
src/
|-- SmartTaskManager.Domain
|-- SmartTaskManager.Application
|-- SmartTaskManager.Infrastructure
|-- SmartTaskManager.Api
`-- SmartTaskManager.Web
    |-- Components
    |   |-- Layout
    |   |-- Pages
    |   `-- Shared
    |-- Models
    |   |-- Forms
    |   `-- Requests
    |-- Options
    |-- Services
    `-- wwwroot
```

## Dependency Direction

```text
Browser
  -> SmartTaskManager.Web
      -> SmartTaskManager.Api
          -> Application / Domain / Infrastructure
```

Important boundary:

- `SmartTaskManager.Web` does **not** call application services directly
- `SmartTaskManager.Web` does **not** reference the API project
- communication happens through typed `HttpClient` services and HTTP contracts

## Front-End Responsibilities

`SmartTaskManager.Web` is responsible for:

- layout, navigation, and page composition
- signing interactive users in with Microsoft Entra ID
- maintaining a secure cookie-based web session
- managing the selected user in the current Blazor circuit
- calling user, dashboard, task, history, and task-creation endpoints
- presenting loading, empty, success, and error states

## Current Security Flow

1. The user opens a protected page in `SmartTaskManager.Web`.
2. The web app challenges with Microsoft Entra ID over OpenID Connect.
3. After the sign-in callback returns to `/signin-oidc`, ASP.NET Core stores the authenticated session in a secure cookie.
4. The authenticated Blazor circuit can then acquire a delegated access token in the service layer and attach it to outgoing API requests.
5. `SmartTaskManager.Api` validates Microsoft Entra bearer tokens on protected endpoints.

## Why Server Interactivity Was Chosen

This setup was chosen because it keeps the course project simpler:

- one UI project instead of a `.Web` + `.Client` pair
- no browser-side WebAssembly bootstrapping or duplicated client models
- easy server-side access to configuration and typed services
- still modern because it uses the Blazor Web App programming model

If the project grows later, the UI can still evolve toward a WebAssembly or hybrid approach without changing the API contract.
