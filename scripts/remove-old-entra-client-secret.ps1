param(
    [string]$WebAppClientId = "ffdda8ba-1389-4fa9-bba5-b06d14ef55e5",

    [Parameter(Mandatory = $true)]
    [string]$KeyId
)

$ErrorActionPreference = "Stop"

Write-Host "Current password credential metadata:"
az ad app credential list `
  --id $WebAppClientId `
  --query "[].{keyId:keyId,displayName:displayName,startDateTime:startDateTime,endDateTime:endDateTime,hint:hint}" `
  -o table

Write-Host "Deleting password credential with key ID: $KeyId"
az ad app credential delete --id $WebAppClientId --key-id $KeyId

Write-Host "Remaining password credential metadata:"
az ad app credential list `
  --id $WebAppClientId `
  --query "[].{keyId:keyId,displayName:displayName,startDateTime:startDateTime,endDateTime:endDateTime,hint:hint}" `
  -o table
