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
        "Salesforce:OrganisationUrl": "<insert-org-url>",
        "Salesforce:OrganisationId": "<insert-org-id>",
        "Salesforce:OrganisationUser": "<insert-org-username>",
        "Salesforce:AppClientId": "<insert-connected-app-client-id>",
        "Salesforce:AppCertPath": "<insert-path-to-connected-app-cert>",
        "Salesforce:ExportService:Page": "/ui/setup/export/DataExportPage/d",
        "Salesforce:ExportService:Regex": "<a\\s+href=\"(?'relurl'\\/servlet\\/servlet\\.OrgExport\\?.+?)\""
    }
}
```

### Generating Certificates using OpenSSL

`openssl req -x509 -newkey rsa:4096 -keyout private-key.pem -out public-cert.pem -days 365 -nodes`