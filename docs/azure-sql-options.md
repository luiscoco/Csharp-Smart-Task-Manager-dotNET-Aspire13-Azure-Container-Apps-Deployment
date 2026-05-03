# Azure SQL Options

## Scope

This document selects the lowest-cost Azure SQL path for this repository and compares it with the safest low-cost paid fallback.

The target workload is the API database behind:

- `src/SmartTaskManager.Api`

## Validated Repository Facts

The repository confirms:

- `SmartTaskManager.Api` uses `ConnectionStrings:SmartTaskManager`.
- The committed local runtime connection string is:

```text
Server=localhost;Database=SmartTaskManagerDb;Integrated Security=True;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True
```

- `SmartTaskManager.Web` does not connect directly to SQL Server.
- The API applies EF Core migrations on startup.
- The schema shown by the current migration is simple and Azure SQL-compatible:
  - `Users`
  - `Tasks`
  - `TaskHistoryEntries`
- The schema uses standard SQL Server/Azure SQL types:
  - `uniqueidentifier`
  - `nvarchar`
  - `datetime2`
  - `int`
- No repository-visible blocker such as SQL Server Agent objects, CLR, filegroups, cross-database queries, or unsupported server-level features was found.

## Migration Fit Assessment

Primary migration strategy: offline `SqlPackage` export/import using a `.bacpac`.

Why this fits:

- the schema is small and conventional
- the app appears to use only ordinary tables, keys, and indexes
- Azure SQL Database fully supports this style of workload
- `SqlPackage` is the standard Microsoft portability path for small-to-medium database moves

Important constraint:

- `SqlPackage` import targets a new or empty database
- therefore the Azure SQL Database created for import must remain empty before the import step

## Option 1: Azure SQL Database Free Offer

### Recommendation Status

This is the selected recommendation if your subscription and target region support it.

### Why It Is The Best Cost Choice

- it is the lowest-cost option because it can be free
- it is a fully managed Azure SQL Database offering
- it fits a small dev/test or portfolio workload like `SmartTaskManagerDb`
- it supports the Azure SQL single-database model the app expects
- it can still be created with Azure CLI

### Relevant Limits

Current documented limits for the free offer:

- up to `10` free databases per subscription
- `100,000` vCore seconds of compute per month per database
- `32 GB` data storage per database
- `32 GB` backup storage per database

### Free Offer Tradeoffs

- it is best for dev/test, proofs of concept, and small portfolio hosting
- the free limit behavior must be chosen explicitly
- if you use `AutoPause` when the limit is exhausted, the database can pause until the next month
- backup behavior is more limited than standard paid deployments
- region behavior for free databases is constrained by the subscription's free-offer rules
- not suitable if you need stronger availability options, elastic pools, or more predictable sustained throughput

### Best Free-Offer Behavior For This Repo

Use:

- General Purpose
- Serverless
- Gen5
- free limit enabled
- exhaustion behavior `AutoPause`
- local backup storage redundancy

This is the cheapest viable Azure SQL target for this solution.

## Option 2: Lowest-Friction Paid Fallback

### Recommendation Status

Use this only if the free offer is unavailable, already exhausted for the subscription, or too restrictive for expected activity.

### Selected Paid Fallback

Recommended paid fallback:

- Azure SQL Database single database
- General Purpose
- Serverless
- Gen5
- `GP_S_Gen5_1` class behavior
- minimum capacity `0.5`
- maximum capacity `1`
- auto-pause enabled
- local backup storage redundancy

### Why This Fallback Was Chosen

- it stays close to the free-offer architecture
- it retains serverless auto-pause behavior
- it is operationally simple for an intermittently used app
- it avoids over-provisioning a very small schema
- it is a better default than jumping immediately to larger provisioned or business-critical tiers

### Pricing Note

Exact paid price is region-dependent and can change over time.

Inference:

- this fallback is chosen as the safest minimal paid serverless path for a small app
- if you want the absolute cheapest paid SKU in a specific region, verify current regional pricing at execution time before creating the database

## Final Recommendation

Selected recommendation:

1. Prefer the Azure SQL Database free offer.
2. If free is unavailable, use the smallest General Purpose serverless paid configuration with auto-pause.

For this repository, the free offer is a good fit because:

- the database is small by design
- the migration path is simple
- the app is a learning or portfolio application
- no repository-level compatibility blocker was found

## Execution Snapshot

Actual values used in Azure:

- resource group: `rg-smarttaskmanager-data-dev-weu`
- region: `westeurope`
- logical server: `sql-stm-dev-weu-01`
- database: `SmartTaskManagerDb`

Actual resulting database shape:

- edition: `GeneralPurpose`
- service objective: `GP_S_Gen5_1`
- compute model: serverless
- min capacity: `0.5`
- max capacity: `1`
- auto-pause delay: `60`

Important outcome:

- the final created database does not show `useFreeLimit`
- the final backup redundancy is not the local free-offer shape
- inference: the free-offer path did not stick, and the environment ended up on the paid serverless fallback profile

## Future Azure SQL Connection String Shape

When the database is provisioned, the API should later receive:

```text
Server=tcp:<sql-server-name>.database.windows.net,1433;Initial Catalog=<database-name>;Persist Security Info=False;User ID=<sql-admin-user>;Password=<sql-admin-password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

This should later be set on the API App Service as:

```text
ConnectionStrings__SmartTaskManager
```

## Fallback If BACPAC Migration Fails

If execution later finds an export/import blocker that is not visible from the repository alone, use:

- Azure Data Studio with the Azure SQL migration extension
- or Azure Database Migration Service offline migration

That fallback is not the default because nothing in this repository currently justifies the additional complexity.
