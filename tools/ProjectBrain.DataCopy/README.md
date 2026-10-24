# Copy users from Azure SQL to Neon

A one-off tool that copies every user, and everything linked to them, from the old staging Azure SQL database into Neon Postgres. It isn't part of the solution and doesn't run in CI.

## What it does

- **Replaces the user data in the target.**
  - Every row in the user tables in the target is deleted.
  - The source rows are then copied in with their original IDs.
  - Users who signed up on Neon after the move, and anything they created, are removed.
  - Keeping the IDs keeps uploaded files in Blob Storage and documents in AI Search linked to their resources.
- **Which tables count as user tables:**
  - `Users`;
  - every table with a `UserId` column;
  - every table that references those through a foreign key, which includes `ApplicationSettings` through `UpdatedBy`.

  The tool prints the full list, in load order.
- **Leaves reference data as the seeder created it.**
  - Achievements, community channels and system tags are seeded with new GUIDs in each database. The tool matches them by `Key` or `Slug` and rewrites the user rows to the target's IDs.
  - Quizzes and their questions are created by admins rather than seeded. Any the target doesn't have are copied in.
  - Roles and subscription tiers have the same keys in both databases and aren't touched.
- **Converts the data:**
  - SQL Server `datetime2` values are stored as UTC `timestamptz`. The app always wrote UTC.
  - ID sequences are moved past the highest copied ID.
- **Runs as one transaction.** Without `--apply`, the whole copy runs and is then rolled back. That's a full rehearsal against the real databases that changes nothing. With `--apply`, it commits, but only if every table copied and the row counts match. Any failure rolls everything back.

## Before you run it

1. **Sign in to Azure.** Run `az login` with an account that can reach the SQL server. The server only accepts Entra logins, so make yourself its admin, and allow your IP address:
   ```bash
   az sql server ad-admin create -g rg-projectbrain-staging -s projectbrain-gcua27g3hrkhi \
     --display-name "<your name>" --object-id $(az ad signed-in-user show --query id -o tsv)
   az sql server firewall-rule create -g rg-projectbrain-staging -s projectbrain-gcua27g3hrkhi \
     -n data-copy --start-ip-address <your-ip> --end-ip-address <your-ip>
   ```
   The database is serverless, so the first connection can take a minute while it resumes.
2. **Make a backup in Neon.** Create a branch of `staging` in the Neon console to keep as a backup.
3. **Get the Neon connection string.** Use the direct one, whose host has no `-pooler`, in Npgsql keyword format.

## Run it

```bash
export SOURCE_CONNECTION_STRING="Server=tcp:projectbrain-gcua27g3hrkhi.database.windows.net,1433;Database=projectbraindb;Encrypt=True"
export TARGET_CONNECTION_STRING="Host=<neon-direct-host>;Database=neondb;Username=neondb_owner;Password=<password>;SSL Mode=VerifyFull"

# Rehearse: copies everything, prints what it did, then rolls back
dotnet run --project tools/ProjectBrain.DataCopy

# Commit
dotnet run --project tools/ProjectBrain.DataCopy -- --apply
```

The source connection string has no password and no `Authentication` keyword, so the tool signs in with your `az login` session.

Read the rehearsal output before applying. It lists:
- the user tables and how many rows each would get;
- columns that exist on only one side (expected where the model changed after the last SQL Server migration);
- reference rows found only in the source;
- any problems, such as a foreign key the target can't satisfy.

## Afterwards

- Sign in to staging and check the copied data. Rerunning with `--apply` is safe: it replaces the user data again.
- Uploaded files and AI Search documents need nothing, because the IDs were kept.
- Devices re-register for push notifications the next time the app opens.
- Once you're happy, delete the SQL server, its admin identity and the backup branch:
  ```bash
  az sql server delete -n projectbrain-gcua27g3hrkhi -g rg-projectbrain-staging
  az identity delete -n projectbrain-admin-gcua27g3hrkhi -g rg-projectbrain-staging
  ```
