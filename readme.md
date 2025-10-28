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

3. Owner (also priviledged)
   ~~ 1. Container Apps Contributer ~~
   ~~ 2. Contributor (this is a priviledged role) ~~

## EntityFrameworkCore

### To rebuild initial migrations (from from solution folder):

`cd ./projectbrain.api && dotnet ef migrations add InitialCreate --project ../projectbrain.database/projectbrain.database.csproj && cd ..`

### Any time the database schema changes, re-run the command above but replace 'InitialCreate' with a meaningful alternative name

### Good article on using custom domains and setting up github deployments

https://www.withaspire.dev/custom-domains-with-aspire/

## For the webapp I've had to create an action in auth0 to add the roles to the token. This only works because

## each application has a piece of metadata added to it to define the namespace
