# MyPIM - Custom Azure Entra PIM Solution

MyPIM is a mocked Proof of Concept (PoC) for a custom Privileged Identity Management system built with **ASP.NET Core Blazor Server**. It allows users to request temporary access to high-privilege roles, which are then approved by an admin and automatically revoked after a set duration.

## Features

- **Request Workflow**: Users can select roles (e.g., Global Admin) and provide a business justification.
- **Admin Dashboard**: Administrators can view pending requests and Approve or Deny them.
- **Auto-Revocation**: A background service (`RevocationWorker`) monitors active requests and automatically revokes access when the duration expires.
- **Mocked Graph**: Simulates Azure AD interactions (Role Assignment/Revocation) for local development testing.
- **Table Storage**: Uses Azure Table Storage (or Azurite) for persisting configuration and request history.

## Prerequisites

- **.NET 8.0 SDK**
- **Azurite** (Azure Storage Emulator)
    - **VS Code**: Install "Azurite" extension and run `Azurite: Start`.
    - **NPM**: `npx azurite`
    - **Docker**: `docker run -p 10000:10000 mcr.microsoft.com/azure-storage/azurite`

## Getting Started

1.  **Start Azurite**: Ensure the emulator is running.
2.  **Navigate to Source**:
    ```bash
    cd src
    ```
3.  **Run the Application**:
    ```bash
    dotnet run
    ```
4.  **Open Browser**: Go to the URL displayed in the terminal (usually `https://localhost:7xxx`).

## Configuration

The application is configured to use the local development storage by default in `src/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "AzureTableStorage": "UseDevelopmentStorage=true"
  }
}
```

## Project Structure

- `src/`: Main application source code.
    - `Pages/`: Blazor pages (`Requests.razor`, `Admin.razor`).
    - `Services/`: Logic for Table Storage and Mock Graph API.
    - `Workers/`: Background service for auto-revocation.
- `.vscode/`: Workspace configuration (hides Azurite files).
