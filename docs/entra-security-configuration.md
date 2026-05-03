# Entra Security Configuration

## Purpose

This document captures the configuration foundation for the Microsoft Entra ID migration.

The runtime now includes:

- Microsoft Entra ID OpenID Connect sign-in for `SmartTaskManager.Web`
- secure cookie-based web sessions in `SmartTaskManager.Web`
- delegated server-side access-token acquisition and forwarding from `SmartTaskManager.Web` to `SmartTaskManager.Api`
- Microsoft Entra JWT bearer validation in `SmartTaskManager.Api`

Deeper claims-based authorization and Swagger auth are still the next implementation steps.

## Project to App Registration Mapping

| Project | Entra app registration | Tenant ID | Client ID | Notes |
| --- | --- | --- | --- | --- |
| `SmartTaskManager.Web` | `SmartTaskManager.Web` | `e099cebd-5eea-41a3-88db-bcb9a9cba83e` | `ffdda8ba-1389-4fa9-bba5-b06d14ef55e5` | Confidential server-side web app using OIDC plus cookie auth later |
| `SmartTaskManager.Api` | `SmartTaskManager.Api` | `e099cebd-5eea-41a3-88db-bcb9a9cba83e` | `3bede5d9-a947-4d25-a3c1-54df15d5ed60` | Protected resource API using JWT bearer later |

## Local Development URIs

The solution is prepared for localhost-first development.

- Blazor Web App base URL: `https://localhost:7036`
- Web API base URL: `https://localhost:7081`
- Web sign-in callback: `https://localhost:7036/signin-oidc`
- Web sign-out callback: `https://localhost:7036/signout-callback-oidc`

These values match the current launch settings for the two projects.

## API Audience and Scope

- API Application ID URI / audience: `api://3bede5d9-a947-4d25-a3c1-54df15d5ed60`
- Delegated scope name: `Tasks.ReadWrite`
- Fully qualified delegated scope: `api://3bede5d9-a947-4d25-a3c1-54df15d5ed60/Tasks.ReadWrite`

## Configuration Sections by Project

### SmartTaskManager.Web

The web project now includes:

- `AzureAd`
  - `Instance`
  - `TenantId`
  - `ClientId`
  - `ClientSecret`
  - `CallbackPath`
  - `SignedOutCallbackPath`
- `SmartTaskManagerApi`
  - `BaseUrl`
  - `Audience`
  - `Scopes`

These names are chosen to align with later `Microsoft.Identity.Web` integration while preserving the current typed API client configuration shape.
They are now actively used by the web app for OpenID Connect sign-in and the secure cookie session.

### SmartTaskManager.Api

The API project now includes:

- `AzureAd`
  - `Instance`
  - `TenantId`
  - `ClientId`
  - `Audience`
- `Authorization`
  - `RequiredScope`

These names are now actively used by the API for Microsoft Entra JWT bearer validation.

## Secret Handling

Do **not** commit the web app client secret to the repository.

Use one of these approaches instead:

- .NET user secrets for local development
- environment variables in the local machine or CI/CD environment
- deployment-time secret injection later

Example local command:

```powershell
dotnet user-secrets set "AzureAd:ClientSecret" "<web-client-secret>" --project .\src\SmartTaskManager.Web\SmartTaskManager.Web.csproj
```

The committed configuration intentionally uses a placeholder instead of the real secret value.

## What Later Prompts Can Assume

- Entra app registration identifiers are already reflected in configuration
- localhost redirect and callback URI assumptions are documented
- the API audience and delegated scope are documented consistently
- the web client secret is expected to come from secure local secret storage
- web sign-in and API bearer validation are already in place
- delegated token forwarding is already in place
- deeper policies and Swagger auth are still pending
