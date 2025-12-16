![MyPIM Logo](./img/MyPIM.png)

# MyPIM - Custom Azure Entra PIM Solution

MyPIM is a Proof of Concept (PoC) for a custom Privileged Identity Management system built with **ASP.NET Core Blazor Server**. It allows users to request temporary access to high-privilege roles, which are then approved by an admin and automatically revoked after a set duration.

## Features

- **Request Workflow**: Users can select roles (e.g., Global Admin) and provide a business justification.
- **Admin Dashboard**: Administrators can view pending requests and Approve or Deny them.
- **Auto-Revocation**: A background service (`RevocationWorker`) monitors active requests and automatically revokes access when the duration expires.
- **Graph Api**: Azure AD interactions (Role Assignment/Revocation) .
- **Table Storage**: Uses Azure Table Storage (or Azurite) for persisting configuration and request history.
- **EventGrid**: To allow notification for approvers

## Prerequisites

- **.NET 8.0 SDK**
- **Azurite** (Azure Storage Emulator)

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

- `infra/`: Infrastructure-related files (bicep)
- `src/`: Main application source code.
    - `Pages/`: Blazor pages (`Requests.razor`, `Admin.razor`).
    - `Services/`: Logic for Table Storage and Mock Graph API.
    - `Workers/`: Background service for auto-revocation.

## Admin Authorization

The Admin dashboard is protected by an authorization policy that requires membership in a specific Azure AD (Entra ID) security group. Configure it as follows:

- Set the group Object ID in configuration (use User Secrets for local dev):

  ```bash
  cd src
  dotnet user-secrets set AdminSecurityGroupId "<security-group-object-id>"
  ```

- In Azure portal, enable group claims on the app registration used by this app (the `AzureAd:ClientId`):
  - Azure Active Directory → App registrations → your app → Token configuration → Add groups claim
  - Select "Security groups" and choose to emit group IDs
  - Save


Notes:
- If a user is a member of many groups, Azure AD may omit group IDs and emit an overage indicator (`hasgroups`). In that case, enable a Graph fallback (OBF) to check group membership. Open an issue if you need this wired up.
