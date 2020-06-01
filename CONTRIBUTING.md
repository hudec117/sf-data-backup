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

To run/debug the function app, a `local.settings.json` file is required, use this template:

```json
{
    "IsEncrypted": false,
    "Values": {
        "FUNCTIONS_WORKER_RUNTIME": "dotnet",
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "SALESFORCE_ORG_URL": "<insert-org-url>",
        "SALESFORCE_ORG_ID": "<inserr-org-id>",
        "EXPORT_SERVICE_PATH": "/ui/setup/export/DataExportPage/d",
        "EXPORT_SERVICE_REGEX": ""
    }
}
```