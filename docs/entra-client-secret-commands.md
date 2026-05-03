# Entra Client Secret Commands

## Purpose

These commands are PowerShell-friendly examples for rotating the Microsoft Entra client secret used by `SmartTaskManager.Web`.

Do not run them blindly. Replace placeholders first.

## Working Variables

```powershell
$WebAppClientId = "ffdda8ba-1389-4fa9-bba5-b06d14ef55e5"
$TenantId = "e099cebd-5eea-41a3-88db-bcb9a9cba83e"
$ProjectPath = ".\src\SmartTaskManager.Web\SmartTaskManager.Web.csproj"

$ResourceGroup = "<resource-group>"
$WebAppName = "<web-app-name>"
$DisplayName = "SmartTaskManager.Web rotated secret"
```

## List Existing Credential Metadata

List password credential metadata for the app registration:

```powershell
az ad app credential list --id $WebAppClientId -o jsonc
```

Show a narrower view:

```powershell
az ad app credential list `
  --id $WebAppClientId `
  --query "[].{keyId:keyId,displayName:displayName,startDateTime:startDateTime,endDateTime:endDateTime,hint:hint}" `
  -o table
```

Important:

- the actual secret value is not retrievable from this command

## Create A New Secret With `--append`

Capture the full JSON response once:

```powershell
$SecretResponseJson = az ad app credential reset `
  --id $WebAppClientId `
  --append `
  --display-name $DisplayName `
  --years 1 `
  -o json
```

Convert the response to an object:

```powershell
$SecretResponse = $SecretResponseJson | ConvertFrom-Json
```

Capture the new secret value in memory:

```powershell
$NewClientSecret = $SecretResponse.password
```

Guard clause:

```powershell
if ([string]::IsNullOrWhiteSpace($NewClientSecret)) {
    throw "The new client secret was not returned by az ad app credential reset."
}
```

Important:

- do not write `$NewClientSecret` into tracked files
- do not echo it repeatedly into terminal history or logs

## Record Credential Metadata Before And After Rotation

Capture credential metadata before rotation:

```powershell
$BeforeCredentials = az ad app credential list --id $WebAppClientId -o json | ConvertFrom-Json
```

Capture credential metadata after rotation:

```powershell
$AfterCredentials = az ad app credential list --id $WebAppClientId -o json | ConvertFrom-Json
```

Find the newly added key ID:

```powershell
$BeforeKeyIds = @($BeforeCredentials | ForEach-Object { $_.keyId })
$NewCredential = $AfterCredentials | Where-Object { $_.keyId -notin $BeforeKeyIds } | Select-Object -First 1
$NewCredentialKeyId = $NewCredential.keyId
```

## Set Local User Secrets

Write the new secret to local development:

```powershell
dotnet user-secrets set "AzureAd:ClientSecret" $NewClientSecret --project $ProjectPath
```

Optional verification:

```powershell
dotnet user-secrets list --project $ProjectPath
```

Note:

- this may print the local secret value
- do not use the verification command if you want to avoid exposing the value in terminal output

## Set The Azure Web App App Setting

Apply the secret only to `SmartTaskManager.Web`:

```powershell
az webapp config appsettings set `
  --resource-group $ResourceGroup `
  --name $WebAppName `
  --settings AzureAd__ClientSecret="$NewClientSecret"
```

Do not set the secret on `SmartTaskManager.Api`.

## Restart The Web App If Needed

```powershell
az webapp restart --resource-group $ResourceGroup --name $WebAppName
```

## Delete The Old Secret By `keyId`

Only after successful validation:

```powershell
$OldCredentialKeyId = "<old-key-id>"

az ad app credential delete `
  --id $WebAppClientId `
  --key-id $OldCredentialKeyId
```

## Suggested Full Rotation Sequence

```powershell
$WebAppClientId = "ffdda8ba-1389-4fa9-bba5-b06d14ef55e5"
$ProjectPath = ".\src\SmartTaskManager.Web\SmartTaskManager.Web.csproj"
$ResourceGroup = "<resource-group>"
$WebAppName = "<web-app-name>"

$BeforeCredentials = az ad app credential list --id $WebAppClientId -o json | ConvertFrom-Json

$SecretResponseJson = az ad app credential reset `
  --id $WebAppClientId `
  --append `
  --display-name "SmartTaskManager.Web rotated secret" `
  --years 1 `
  -o json

$SecretResponse = $SecretResponseJson | ConvertFrom-Json
$NewClientSecret = $SecretResponse.password

if ([string]::IsNullOrWhiteSpace($NewClientSecret)) {
    throw "The new client secret was not returned."
}

dotnet user-secrets set "AzureAd:ClientSecret" $NewClientSecret --project $ProjectPath

az webapp config appsettings set `
  --resource-group $ResourceGroup `
  --name $WebAppName `
  --settings AzureAd__ClientSecret="$NewClientSecret"

$AfterCredentials = az ad app credential list --id $WebAppClientId -o json | ConvertFrom-Json
$BeforeKeyIds = @($BeforeCredentials | ForEach-Object { $_.keyId })
$NewCredential = $AfterCredentials | Where-Object { $_.keyId -notin $BeforeKeyIds } | Select-Object -First 1
$NewCredentialKeyId = $NewCredential.keyId

az webapp restart --resource-group $ResourceGroup --name $WebAppName
```

## Important Notes

- `az ad app credential reset` without `--append` can remove existing password credentials
- for this repository, always start with `--append`
- the old secret value cannot be recovered later
- the correct Azure App Service setting key is:
  `AzureAd__ClientSecret`
- the correct local secret key is:
  `AzureAd:ClientSecret`
