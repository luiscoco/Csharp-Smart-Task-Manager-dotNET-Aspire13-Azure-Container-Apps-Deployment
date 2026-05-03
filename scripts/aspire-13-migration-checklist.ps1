param()

$checklist = @(
    "[ ] Confirm the repo still uses SmartTaskManager.sln as the source of truth.",
    "[ ] Confirm SmartTaskManager.Web still targets net10.0.",
    "[ ] Confirm SmartTaskManager.Api still targets net10.0.",
    "[ ] Confirm SmartTaskManager.Web still requires AzureAd:ClientSecret from user secrets or environment variables.",
    "[ ] Confirm SmartTaskManager.Web still uses SmartTaskManagerApi:BaseUrl.",
    "[ ] Confirm SmartTaskManager.Api still uses ConnectionStrings:SmartTaskManager.",
    "[ ] Confirm local web HTTPS remains https://localhost:7036.",
    "[ ] Confirm local API HTTPS remains https://localhost:7081.",
    "[ ] Create src\\SmartTaskManager.AppHost manually.",
    "[ ] Create src\\SmartTaskManager.ServiceDefaults manually.",
    "[ ] Add both new projects to SmartTaskManager.sln.",
    "[ ] Add ServiceDefaults references only to SmartTaskManager.Web and SmartTaskManager.Api.",
    "[ ] Add AppHost references only to SmartTaskManager.Web and SmartTaskManager.Api.",
    "[ ] Add builder.AddServiceDefaults() to both executable projects.",
    "[ ] Add app.MapDefaultEndpoints() to both executable projects.",
    "[ ] Model only Web and API in AppHost for stage 1.",
    "[ ] Keep the database external to Aspire for stage 1.",
    "[ ] Keep SmartTaskManagerApi__BaseUrl initially and have AppHost provide the runtime API target.",
    "[ ] Build the solution.",
    "[ ] Run the AppHost locally.",
    "[ ] Validate the browser-visible web URL and Microsoft Entra sign-in callback behavior.",
    "[ ] Validate API startup and external database connectivity.",
    "[ ] Update the Aspire migration docs with the final execution details.",
    "[ ] Stop before changing Azure deployment strategy unless explicitly approved."
)

$checklist | ForEach-Object { Write-Host $_ }
