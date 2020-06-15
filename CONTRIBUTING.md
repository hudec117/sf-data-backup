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
        "SALESFORCE_ORG_URL": "<insert-org-url>",
        "SALESFORCE_ORG_ID": "<insert-org-id>",
        "SALESFORCE_ORG_USER": "<insert-org-username>",
        "SALESFORCE_APP_CLIENT_ID": "<insert-connected-app-client-id>",
        "SALESFORCE_APP_CERT": "<insert-path-to-connected-app-cert>",
        "EXPORT_SERVICE_PATH": "/ui/setup/export/DataExportPage/d",
        "EXPORT_SERVICE_REGEX": "<a\\s+href=\"(?'relurl'\\/servlet\\/servlet\\.OrgExport\\?.+?)\""
    }
}
```