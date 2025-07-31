# Instagram Automation API

This project provides a minimal Web API for managing Instagram accounts and automation rules. It is written in **.NET 8** and uses **MariaDB** for storage.

## Requirements
- .NET 8 SDK
- MariaDB server

## Configuration
1. Copy `InstagramAutomation.Api/appsettings.json` to `InstagramAutomation.Api/appsettings.Development.json` and update the following values:
   - `ConnectionStrings:DefaultConnection` – connection string for your MariaDB database.
   - `JwtSettings:SecretKey` – secret key used to sign JWT tokens.
   - `Instagram` section – credentials from your Meta application (client id, secret and webhook verify token).
2. Alternatively, these values can be supplied via environment variables.

## Running the API
```
dotnet run --project InstagramAutomation.Api/InstagramAutomation.Api.csproj
```
The first run will create the MariaDB schema if it does not already exist. Swagger UI is available at `/swagger` when running in development mode.

## Tests
```
dotnet test
```

## Webhooks
Configure your Instagram App webhook to the endpoint:
```
https://<host>/api/webhook
```
The verification token must match `Instagram:WebhookVerifyToken`.
