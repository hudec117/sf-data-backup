# Contributing

## Required Software

- Latest Node.js
- Latest .NET Core SDK 3.1
- Azure Functions Core Tools v3
- Microsoft Azure Storage Emulator

## Optional Software

- Visual Studio Code
  - Azure Account and Azure Functions extensions

## Project

### Azure Storage Emulator

Start the Azure Storage Emulator before attempting to run/debug the function app.

### local.settings.json

To run/debug the function app, a `src/SfDataBackup/local.settings.json` file is required, use this template:

```json
{
    "IsEncrypted": false,
    "Values": {
        "FUNCTIONS_WORKER_RUNTIME": "dotnet",
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "Schedule": "<insert-cron-expression>",
        "Salesforce:OrganisationUrl": "<insert-org-url>",
        "Salesforce:Username": "<insert-username>",
        "Salesforce:Password": "<insert-password>",
        "Salesforce:ExportService:Page": "/ui/setup/export/DataExportPage/d",
        "Salesforce:ExportService:Regex": "<a\\s+href=\"(?'relurl'\\/servlet\\/servlet\\.OrgExport\\?.+?)\""
    }
}
```

 - `Schedule` is a CRON expression describing when the DownloadWeekly Timer function should be invoked.
   - See [here](https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-timer?tabs=csharp#ncrontab-expressions) for NCRONTAB expressions.
 - `Salesforce:OrganisationUrl` is the URL to your Salesforce Organisation in `https://myorg.my.salesforce` format.
 - `Salesforce:Username` is the username of the user you want to login as. DEVELOPMENT ONLY
 - `Salesforce:Password` is the password of the user you want to login as. DEVELOPMENT ONLY
 - `Salesforce:ExportService:Page` is the relative URL in Salesforce Classic to the Weekly Export Service page.
 - `Salesforce:ExportService:Regex` is the Regex used to extract the relative export download links from the page Weekly Export Service page.
   - The code looks for a capture group called `relurl`