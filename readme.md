## How to create the Azure search config

1. Use the wizard to create a data source and vecotrise
2. Create extra properties in the index:
   2a. ownerId
3. Update the indexer to link the blob metadata.userId to ownerId on the index
   {
   "sourceFieldName": "metadata_userId",
   "targetFieldName": "ownerId"
   }

## To run apphost locally (run from solution folder):

`dotnet watch run --project ./projectbrain.apphost/projectbrain.apphost.csproj --launch-profile https`

## To Deploy, I can use the aspire cli:

`aspire deploy`

There's no undeploy, but the resource group can be deleted:
`az group delete --name <your-resource-group-name>`

## To access the aspire dashboard on azure, might need to add role assignments to the app env for roles:

1. Owner (also priviledged)
   ~~ 1. Container Apps Contributer ~~
   ~~ 2. Contributor (this is a priviledged role) ~~

## EntityFrameworkCore

To rebuild initial migrations (from from solution folder):

`cd ./projectbrain.api && dotnet ef migrations add InitialCreate --project ../projectbrain.database/projectbrain.database.csproj && cd ..`

Any time the database schema changes, re-run the command above but replace 'InitialCreate' with a meaningful alternative name

## Good article on using custom domains and setting up github deployments

https://www.withaspire.dev/custom-domains-with-aspire/

The process is essentially:

1. Remove customDomain and cerificate from apphost
2. Deploy app using `aspire deploy`
3. Get verification id

```
az containerapp list --query "[].{Name: name, VerificationId: properties.customDomainVerificationId, Fqdn: properties.configuration.ingress.fqdn}" -o table
```

4. Add TXT record to DNS:

```
HOST, TYPE, DATA
asuid.<domain>, TXT, <verificationid>
<domain>, CNAME, <fqdn>
```

5. Re-add the customDomain entries in the apphost
6. Re-deploy

7. Create certificate/s:

```
    az containerapp env certificate create `
    --name <container-env-name>
    --resource-group <resource-group-name>
    --validation-method CNAME
    --hostname <host domain>
```

6. Add certificate names to AppHost
7. Wait for certificates to be created (it takes a few minutes) then re-deploy

8. Add customDomain & certificate name parameters to apphost, leave value empty, and DON'T use these yet. Using `azure deploy` will fail and I can't complete the steps below
9. Deploy using `azure deploy`
10. Now we need to verify ownership by adding a TXT record to the domain. The hexadecimal value comes from the azure portal in the conatainerapp/Custom Domain/Verification ID value, or use following command to get the verification id and fqdn:
    ```
    az containerapp list --query "[].{Name: name, EnvironmentId: properties.environmentId, VerificationId: properties.customDomainVerificationId, Fqdn: properties.configuration.ingress.fqdn}" -o table
    ```
11. Add the TXT record/s for each domain; `HOST=asuid.<sub-domain>` (ie asuid.api.staging), `DATA=<value from azure portal>`
12. Add DNS record to point sub-domain to azure container app/s. Get the container app url and add CNAME record/s, `HOST=subdomain` (ie api.staging), `DATA=container app url`
13. Add the customdomain and certificate
14. Redeploy the app to get the custom domain added to the containerapp
15. Create the certificates using
    ```
    az containerapp env certificate create `
    --name <container-env-name>
    --resource-group <resource-group-name>
    --validation-method CNAME
    --hostname <host domain>
    ```
    9. Add the certificate hexadecimals to the env file;
16. Re-deploy. All should work now

## Auth0 roles

For the webapp I've had to create an action in auth0 to add the roles to the token. This only works because each application has a piece of metadata added to it to define the namespace

## SQL server access for new deployments

For newly deployed azure resources I won't have access to the sql database. To get access, set myself as an admin. In the azure portal:
Sql Server/Settings/Entra ID/Set Admin

## Adding permissions & roles

To give an api permissions, add them in the APIs/Permissions tab in Auth0

## Create infra files

`aspire do infra` or `azd infra gen`
