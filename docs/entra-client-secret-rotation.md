# Entra Client Secret Rotation

## Purpose

This document defines the safe client secret rotation flow for `SmartTaskManager.Web`.

The current code already enforces a runtime-secret pattern, so the goal is to rotate and inject the secret without putting the real value into tracked source files.

## Validated Repository Facts

The repository currently confirms:

- the web app project is `src/SmartTaskManager.Web`
- the web app expects these `AzureAd` values:
  - `Instance`
  - `TenantId`
  - `ClientId`
  - `ClientSecret`
  - `CallbackPath`
  - `SignedOutCallbackPath`
- the placeholder value is:

```text
__SET_IN_USER_SECRETS_OR_ENVIRONMENT__
```

- startup validation rejects the placeholder and requires a real `AzureAd:ClientSecret` from user secrets or environment variables
- the project already has `UserSecretsId`:

```text
SmartTaskManager.Web-ffdda8ba-1389-4fa9-bba5-b06d14ef55e5
```

- the API project does not require this client secret

Confirmed Entra values from source:

- tenant ID: `e099cebd-5eea-41a3-88db-bcb9a9cba83e`
- web app registration client ID: `ffdda8ba-1389-4fa9-bba5-b06d14ef55e5`

## Why The Old Secret Cannot Be Recovered

Microsoft Entra does not let you retrieve the full value of an existing application password later.

You can list password credential metadata such as:

- `keyId`
- `displayName`
- `startDateTime`
- `endDateTime`
- password hint metadata

But you cannot retrieve the actual secret value again.

That means if you no longer know the secret, the correct action is:

1. create a new client secret
2. apply it to the runtime locations that need it
3. validate sign-in
4. remove the old secret after successful cutover

## Correct Secret Destinations

For this repository, the real secret belongs only in runtime configuration.

Allowed destinations:

- local development:
  `.NET user secrets`
- Azure runtime:
  Azure App Service application setting on `SmartTaskManager.Web`

Not allowed:

- `src/SmartTaskManager.Web/appsettings.json`
- `src/SmartTaskManager.Web/appsettings.Production.json`
- any other tracked file in the repository

## Rotation Strategy

Use `az ad app credential reset --append` first.

Why:

- `reset` without `--append` clears old credentials by default
- that can break existing environments immediately
- `--append` lets you add a new secret first, validate it, and remove the old one afterward

## Safe Rotation Flow

### Step 1: List Existing Credential Metadata

Inspect the current password credentials:

- capture `keyId`
- capture `displayName`
- capture `startDateTime`
- capture `endDateTime`

Do this before rotating anything.

### Step 2: Create A New Secret With `--append`

Create a new password credential using:

- the web app registration client ID
- `--append`
- a clear `--display-name`
- a bounded lifetime such as `--years 1`

Important:

- the output includes sensitive credential material
- capture it once
- do not paste it into source code
- do not keep reprinting it in logs

### Step 3: Store The Secret Locally

Write the new secret to local development configuration with:

```powershell
dotnet user-secrets set "AzureAd:ClientSecret" "<new-secret>" --project .\src\SmartTaskManager.Web\SmartTaskManager.Web.csproj
```

This is the correct local development destination because the project already uses user secrets.

### Step 4: Store The Secret In Azure App Service

If the web app is deployed to Azure App Service, set the secret on the web app only:

```text
AzureAd__ClientSecret
```

Do not set this on `SmartTaskManager.Api`.

### Step 5: Validate Sign-In

After the new secret is in place:

- restart the Azure Web App if necessary
- start the local app if validating locally
- sign in through Microsoft Entra
- verify the web app completes OIDC sign-in
- verify the app can still obtain delegated tokens for the API

### Step 6: Remove The Old Secret

Only after validation succeeds:

- identify the old password credential by `keyId`
- delete only the obsolete secret

Do not delete all password credentials blindly.

## Validation Checklist

After applying the new secret:

- local startup of `SmartTaskManager.Web` succeeds
- no startup error mentions the placeholder secret
- the browser can complete sign-in
- the web app can call the protected API
- the Azure-hosted web app also signs in successfully if Azure runtime was updated

## Recommended Credential Lifetime

Practical recommendation:

- use `--years 1` for a short but manageable lifetime

This is an operational choice, not a code requirement.

If you need a stricter security posture later, consider:

- shorter-lived secrets
- certificate credentials
- Azure Key Vault
- moving away from secrets entirely where possible

## Production Hardening Note

This repository currently expects a client secret, so this document preserves that pattern.

For stronger production posture later, consider:

- Azure Key Vault reference-backed App Service settings
- certificate-based credentials
- redesigning away from client secrets where supported

## Summary

The safe flow for this repository is:

1. list existing password credential metadata
2. append a new secret
3. store it in local user secrets
4. store it in Azure App Service on `SmartTaskManager.Web`
5. validate sign-in and API token acquisition
6. remove the old secret by `keyId`
