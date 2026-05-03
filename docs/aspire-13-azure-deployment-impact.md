# Aspire 13 Azure Deployment Impact

## Purpose

This document explains how a local Aspire 13 migration affects the current Azure deployment model.

## Current Validated Azure Deployment Model

The repository already has a working non-Aspire Azure deployment path based on:

- Azure App Service Plan
- separate Azure Web Apps for:
  - `SmartTaskManager.Web`
  - `SmartTaskManager.Api`
- external Azure SQL Database
- Microsoft Entra application registrations
- runtime secrets and app settings injected in Azure

The current low-cost deployed path was optimized around:

- one shared `B1` App Service plan
- two Web Apps
- existing Azure SQL resources

## What Local Aspire Adoption Changes Immediately

For the short term, local Aspire adoption changes:

- local orchestration
- developer startup workflow
- local observability
- local service composition

It does **not** require immediate changes to:

- Azure App Service
- Azure SQL
- Microsoft Entra app registrations
- current production-like deployment scripts

Actual migration result:

- the local Aspire migration completed without changing Azure resources
- the current Azure App Service and Azure SQL resources remain the active deployment path
- no Azure deployment scripts were replaced by AppHost

## Why Local Aspire Adoption Does Not Require Immediate Azure Hosting Changes

Aspire AppHost and ServiceDefaults can be adopted as local development concerns first.

That means the repo can gain:

- one-command local startup
- telemetry defaults
- health endpoints
- local topology awareness

without immediately switching the cloud deployment model.

This is the recommended order for this repository.

Actual validation result:

- the Web and API now run locally under Aspire while still preserving the same external local auth-sensitive ports
- this keeps the existing Azure deployment model decoupled from the local orchestration model

## Official Aspire Azure App Service Considerations

Official Aspire Azure App Service guidance currently matters, but it should not drive the first migration step here.

Important facts from official documentation:

- Aspire Azure App Service integration is currently marked `Preview`
- the App Service integration can provision additional Azure resources automatically
- default App Service hosting through Aspire is oriented around AppHost-managed deployment workflows
- App Service deployment through Aspire uses a public website model and container publishing behavior

Official default provisioning details called out by Aspire docs include:

- Premium `P0V3` Linux App Service plan by default
- Azure Container Registry
- user-assigned managed identity
- role assignments
- Aspire Dashboard resource

## Cost And Risk Impact Compared To The Current Manual Path

For this repo, moving Azure deployment itself into Aspire immediately would likely:

- increase infrastructure complexity
- increase cost relative to the current low-cost `B1` manual App Service setup
- introduce preview risk into the deployment path
- change the hosting model toward container-backed deployment

That is not aligned with the current project goals.

## Recommended Short-Term Azure Strategy

Recommended short-term strategy:

- adopt Aspire locally only
- keep the current manual Azure App Service + Azure SQL deployment path
- keep the current Azure docs and scripts as the deployment source of truth

Why:

- it preserves the already validated low-cost Azure path
- it isolates Aspire risk to development-time workflow first
- it avoids coupling a local orchestration migration with a hosting-model migration

Actual short-term outcome after migration:

- keep deploying `SmartTaskManager.Web` and `SmartTaskManager.Api` directly to Azure App Service
- do not deploy `SmartTaskManager.AppHost`
- do not deploy `SmartTaskManager.ServiceDefaults` as standalone Azure applications
- keep `ConnectionStrings__SmartTaskManager` and `AzureAd__ClientSecret` in Azure runtime settings as before

## Medium-Term Strategy

After local Aspire adoption is stable, a later separate evaluation can compare:

- keep manual Azure deployment permanently
- selectively introduce Aspire Azure deployment only in a separate branch or environment
- explore Aspire Azure App Service only if the extra resources, container model, and preview status become acceptable

That later review should explicitly evaluate:

- App Service plan SKU changes
- registry cost
- managed identity requirements
- dashboard deployment
- Microsoft Entra redirect implications if URLs change

## Recommendation

Best short-term recommendation for this repository:

- use Aspire for local orchestration and development only
- do not change the current Azure App Service + Azure SQL production-like deployment path yet

## Short Conclusion

Local Aspire migration is compatible with the current Azure deployment model.

The best short-term approach is:

- migrate locally to Aspire 13 first
- keep the current manual Azure deployment path unchanged
- revisit Azure-side Aspire deployment only as a later, separate decision

This remains the correct recommendation after the executed migration.
