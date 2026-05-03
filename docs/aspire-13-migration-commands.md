# Aspire 13 Migration Commands

## Purpose

This document records the commands used for the first Aspire migration and keeps the surrounding planning commands for repeatability.

This document covers two paths:

- recommended path: manual AppHost + ServiceDefaults creation
- alternative path: `aspire init`

## Executed Command Snapshot

The migration was executed on `2026-04-30` using the stable Aspire `13.0.0` line already installed on the machine.

Executed commands:

```powershell
dotnet new aspire-apphost -n SmartTaskManager.AppHost -o .\src\SmartTaskManager.AppHost
dotnet new aspire-servicedefaults -n SmartTaskManager.ServiceDefaults -o .\src\SmartTaskManager.ServiceDefaults

dotnet sln .\SmartTaskManager.sln add .\src\SmartTaskManager.AppHost\SmartTaskManager.AppHost.csproj
dotnet sln .\SmartTaskManager.sln add .\src\SmartTaskManager.ServiceDefaults\SmartTaskManager.ServiceDefaults.csproj

dotnet add .\src\SmartTaskManager.AppHost\SmartTaskManager.AppHost.csproj reference .\src\SmartTaskManager.Web\SmartTaskManager.Web.csproj
dotnet add .\src\SmartTaskManager.AppHost\SmartTaskManager.AppHost.csproj reference .\src\SmartTaskManager.Api\SmartTaskManager.Api.csproj

dotnet add .\src\SmartTaskManager.Web\SmartTaskManager.Web.csproj reference .\src\SmartTaskManager.ServiceDefaults\SmartTaskManager.ServiceDefaults.csproj
dotnet add .\src\SmartTaskManager.Api\SmartTaskManager.Api.csproj reference .\src\SmartTaskManager.ServiceDefaults\SmartTaskManager.ServiceDefaults.csproj

dotnet build .\SmartTaskManager.sln
dotnet run --project .\src\SmartTaskManager.AppHost\SmartTaskManager.AppHost.csproj --no-build
```

## Prerequisites

- `.NET 10 SDK` or later
- Aspire CLI on the `stable` channel
- PowerShell
- existing local Microsoft Entra secret for `SmartTaskManager.Web`
- existing local database connectivity for `ConnectionStrings:SmartTaskManager`

Repository observations that affect command choice:

- solution file: `.\SmartTaskManager.sln`
- no `global.json` detected
- no `Directory.Packages.props` detected
- ignore `.\tempTest\tempTest.csproj` because it is not part of the solution

## Check Current Tool Versions

```powershell
dotnet --version
dotnet --list-sdks
aspire --version
az --version
```

Actual observed versions during execution:

```powershell
dotnet --version
# 10.0.202

aspire --version
# 13.0.0+7512c2944094a58904b6c803aa824c4a4ce42e11
```

Optional repo checks:

```powershell
rg --files -g "*.sln" -g "*.csproj" -g "Directory.Packages.props" -g "global.json"
Get-Content .\src\SmartTaskManager.Web\Properties\launchSettings.json
Get-Content .\src\SmartTaskManager.Api\Properties\launchSettings.json
```

## Aspire CLI Install Or Update

If Aspire CLI is not installed:

```powershell
irm https://aspire.dev/install.ps1 | iex
```

If Aspire CLI is already installed:

```powershell
aspire update --self
aspire --version
```

Important:

- use the `stable` channel
- do not use `staging` or `daily`
- do not use `aspire update` yet for this repo because this is not already an Aspire solution

## Alternative Path: `aspire init`

Interactive path:

```powershell
aspire init
```

Version-pinned example if you want explicit stable-version control:

```powershell
$AspireVersion = "<latest-stable-13.x>"
aspire init --channel stable --version $AspireVersion
```

Guidance if using `aspire init`:

- verify it targets `SmartTaskManager.sln`
- verify it does not pull `tempTest\tempTest.csproj` into scope
- verify the generated AppHost and ServiceDefaults project locations before accepting broad edits
- stop if it proposes root-level project placement that conflicts with the desired `src/` structure

## Recommended Path: Manual Template Creation

### Create The New Projects

```powershell
dotnet new aspire-apphost -n SmartTaskManager.AppHost -o .\src\SmartTaskManager.AppHost
dotnet new aspire-servicedefaults -n SmartTaskManager.ServiceDefaults -o .\src\SmartTaskManager.ServiceDefaults
```

### Add The New Projects To The Solution

```powershell
dotnet sln .\SmartTaskManager.sln add .\src\SmartTaskManager.AppHost\SmartTaskManager.AppHost.csproj
dotnet sln .\SmartTaskManager.sln add .\src\SmartTaskManager.ServiceDefaults\SmartTaskManager.ServiceDefaults.csproj
```

### Add Project References To The AppHost

```powershell
dotnet add .\src\SmartTaskManager.AppHost\SmartTaskManager.AppHost.csproj reference .\src\SmartTaskManager.Web\SmartTaskManager.Web.csproj
dotnet add .\src\SmartTaskManager.AppHost\SmartTaskManager.AppHost.csproj reference .\src\SmartTaskManager.Api\SmartTaskManager.Api.csproj
```

### Add ServiceDefaults References To Executable Projects

```powershell
dotnet add .\src\SmartTaskManager.Web\SmartTaskManager.Web.csproj reference .\src\SmartTaskManager.ServiceDefaults\SmartTaskManager.ServiceDefaults.csproj
dotnet add .\src\SmartTaskManager.Api\SmartTaskManager.Api.csproj reference .\src\SmartTaskManager.ServiceDefaults\SmartTaskManager.ServiceDefaults.csproj
```

## Build And Restore Commands

```powershell
dotnet restore .\SmartTaskManager.sln
dotnet build .\SmartTaskManager.sln -c Debug
```

Optional focused builds:

```powershell
dotnet build .\src\SmartTaskManager.AppHost\SmartTaskManager.AppHost.csproj -c Debug
dotnet build .\src\SmartTaskManager.Web\SmartTaskManager.Web.csproj -c Debug
dotnet build .\src\SmartTaskManager.Api\SmartTaskManager.Api.csproj -c Debug
```

## Local Run Commands

Preferred local orchestration entry point after migration:

```powershell
aspire run --project .\src\SmartTaskManager.AppHost\SmartTaskManager.AppHost.csproj
```

Equivalent direct `dotnet` path if needed:

```powershell
dotnet run --project .\src\SmartTaskManager.AppHost\SmartTaskManager.AppHost.csproj --launch-profile https
```

Actual execution command used:

```powershell
dotnet run --project .\src\SmartTaskManager.AppHost\SmartTaskManager.AppHost.csproj --no-build
```

Important validation intent after migration:

- keep the browser-visible web URL on `https://localhost:7036`
- keep the browser-visible API URL on `https://localhost:7081` if possible for stage 1
- if Aspire proxies the services differently, explicitly validate the final visible URLs from AppHost output

Actual runtime result:

- AppHost dashboard: `https://localhost:17184`
- browser-visible web HTTPS URL preserved: `https://localhost:7036`
- browser-visible API HTTPS URL preserved: `https://localhost:7081`
- AppHost proxy also preserved:
  - `http://localhost:5269`
  - `http://localhost:5081`
- child process internal endpoints moved to dynamic ports behind the proxy

## Validation Commands

### Check That The AppHost Starts

```powershell
aspire ps
aspire describe
```

### Check The Local URLs

```powershell
Invoke-WebRequest -Uri https://localhost:7036/ -UseBasicParsing
Invoke-WebRequest -Uri https://localhost:7081/health -UseBasicParsing
Invoke-WebRequest -Uri https://localhost:7081/alive -UseBasicParsing
```

If the visible ports differ after migration, validate against the actual AppHost-reported URLs instead of assuming the old ones were preserved.

Actual validation evidence used during execution:

```powershell
dotnet build .\SmartTaskManager.sln

netstat -ano | findstr /R /C:":7036 " /C:":7081 " /C:":5269 " /C:":5081 " /C:":17184 "

curl.exe -s -D - http://localhost:5081/health -o NUL
curl.exe -s -D - http://localhost:5269/health -o NUL
```

Additional runtime evidence came from Aspire temp logs:

- AppHost startup log confirmed dashboard startup on `https://localhost:17184`
- DCP service logs confirmed:
  - `smarttaskmanager-web-http` ready
  - `smarttaskmanager-web-https` ready
  - `smarttaskmanager-api-http` ready
  - `smarttaskmanager-api-https` ready
- API stdout log confirmed:
  - database connection succeeded
  - EF migration history was checked
  - no pending migrations
  - sample data seeding completed
  - API internal process started listening
- Web stdout log confirmed:
  - web internal process started listening

Important local note:

- direct HTTPS probing from this shell was unreliable because of the local Windows TLS stack in this session
- the migration was therefore validated through AppHost/DCP logs plus confirmed proxy port bindings and HTTP-to-HTTPS redirects

### Check Current Secrets Pattern

```powershell
dotnet user-secrets list --project .\src\SmartTaskManager.Web\SmartTaskManager.Web.csproj
```

### Check Database Connectivity Assumption

```powershell
Get-Content .\src\SmartTaskManager.Api\appsettings.json
```

The execution prompt should verify the real runtime connection source before assuming a specific SQL instance.

## Rollback Commands

### Remove ServiceDefaults References

```powershell
dotnet remove .\src\SmartTaskManager.Web\SmartTaskManager.Web.csproj reference .\src\SmartTaskManager.ServiceDefaults\SmartTaskManager.ServiceDefaults.csproj
dotnet remove .\src\SmartTaskManager.Api\SmartTaskManager.Api.csproj reference .\src\SmartTaskManager.ServiceDefaults\SmartTaskManager.ServiceDefaults.csproj
```

### Remove AppHost References

```powershell
dotnet remove .\src\SmartTaskManager.AppHost\SmartTaskManager.AppHost.csproj reference .\src\SmartTaskManager.Web\SmartTaskManager.Web.csproj
dotnet remove .\src\SmartTaskManager.AppHost\SmartTaskManager.AppHost.csproj reference .\src\SmartTaskManager.Api\SmartTaskManager.Api.csproj
```

### Remove Projects From The Solution

```powershell
dotnet sln .\SmartTaskManager.sln remove .\src\SmartTaskManager.AppHost\SmartTaskManager.AppHost.csproj
dotnet sln .\SmartTaskManager.sln remove .\src\SmartTaskManager.ServiceDefaults\SmartTaskManager.ServiceDefaults.csproj
```

### Remove The Created Project Folders

Verify the exact paths before deletion:

```powershell
Remove-Item -LiteralPath .\src\SmartTaskManager.AppHost -Recurse -Force
Remove-Item -LiteralPath .\src\SmartTaskManager.ServiceDefaults -Recurse -Force
```

### Rebuild After Rollback

```powershell
dotnet restore .\SmartTaskManager.sln
dotnet build .\SmartTaskManager.sln -c Debug
```

## Recommended Command Path

Recommended execution sequence for this repo:

1. update or install Aspire CLI on the `stable` channel
2. create AppHost manually under `src\SmartTaskManager.AppHost`
3. create ServiceDefaults manually under `src\SmartTaskManager.ServiceDefaults`
4. add references explicitly
5. build
6. run AppHost
7. validate Entra-sensitive local URLs and database behavior

## Rollback Reminder

If the migration needs to be reversed, use the rollback commands in this file and also remove these code-level changes:

- `builder.AddServiceDefaults()` from `SmartTaskManager.Web`
- `builder.AddServiceDefaults()` from `SmartTaskManager.Api`
- `app.MapDefaultEndpoints()` from `SmartTaskManager.Web`
- `app.MapDefaultEndpoints()` from `SmartTaskManager.Api`
- the AppHost project resource wiring in `src\SmartTaskManager.AppHost\AppHost.cs`
