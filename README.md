# PermitPro - Permit-to-Work (PTW)

A multi-tenant, enterprise web application for managing **Permit-to-Work (PTW)** systems and workplace safety permits. Built on ASP.NET Core 10.0 with role-based access control, workflow automation, real-time notifications, and full audit logging.

---

## Tech Stack

| Layer | Technology |
|---|---|
| **Runtime** | .NET 10.0 |
| **Web Framework** | ASP.NET Core 10.0 (MVC + Razor Pages) |
| **ORM** | Entity Framework Core 10.0.8 (SQL Server) |
| **Auth** | ASP.NET Identity + JWT Bearer |
| **UI Components** | Kendo UI for ASP.NET MVC (Telerik) |
| **Real-time** | ASP.NET Core SignalR |
| **Background Jobs** | Hangfire 1.8.14 (currently disabled) |
| **PDF Generation** | PuppeteerSharp 18.0.3 |
| **Excel Export** | ClosedXML 0.105.0 |
| **Email** | MailKit 4.17.0 + MimeKit 4.17.0 |
| **Templating** | Scriban.Signed 5.10.0 |
| **API Docs** | Swashbuckle / Swagger (referenced, not configured) |

---

## Project Structure

```
PermitPro-1.2/
├── Web/                        # ASP.NET Core MVC web application
│   ├── Areas/Identity/         # Scaffolded identity pages (login, register, 2FA)
│   ├── Controllers/            # 19 active MVC controllers
│   ├── Hubs/                   # SignalR hubs (NotificationHub)
│   ├── Models/                 # AJAX request/response models, chart models
│   ├── Services/               # Web-layer services (NotificationPushService)
│   ├── ViewModels/             # 30+ typed view models
│   ├── Views/                  # Razor templates per feature area
│   └── wwwroot/                # Static assets (CSS, JS, images, themes)
├── Core/                       # Domain logic class library
│   ├── Data/                   # ApplicationDbContext + soft-delete/restore logic
│   ├── Entities/               # 18 active domain entities
│   ├── Enums/                  # PermitStatus, WorkflowStatus, SiteType, etc.
│   ├── Interfaces/             # 12 service and entity interfaces
│   ├── Services/               # 7 core service implementations
│   ├── Filters/                # Authorization filters
│   ├── Helpers/                # Email, JWT, PTW settings helpers
│   ├── Interceptors/           # EF Core audit interceptor
│   ├── Extensions/             # DI registration (AddPermitProServices)
│   ├── Migrations/             # EF Core database migrations
│   └── PTW/                    # Permit-to-Work template data
├── Telerik/                    # Kendo UI / Telerik DLL references
├── .github/workflows/          # GitHub Actions CI/CD
└── PermitPro.sln
```

---

## Features

### Permit Management
- Create and manage safety permits across 8 certificate types:
  - Hot Work, Confined Space, Radiation, Excavation, Isolation
  - Method Statement, Lifting & Hoisting, Override
- Permit lifecycle: Draft → Pending → Approved → Suspended → KIV → Closed / ClosedNoAction / Overdue / Rejected
- File attachments (PDF, DOCX, JPG, PNG — up to 5 files, 3 MB each)
- PDF export of permit documents via headless Chrome

### Workflow Engine
- Configurable multi-step approval workflows per company
- Workflow history and full audit trail
- Background jobs for automatic permit closure and auto-resume logic (configurable threshold via `SUSPEND_AUTORESUME_DAYS`)

### Multi-Tenancy
- Each company operates in an isolated namespace
- Route pattern: `/{company}/{controller}/{action}`
- All data scoped to the tenant company

### Real-time Notifications
- SignalR `NotificationHub` pushes live notifications to connected users
- In-app notification list with Title, Message, read/archived state
- SMTP email alerts via MailKit

### Sites & Location
- Manage work sites with GPS coordinates
- Site map dashboard overlay

### Recycle Bin & Soft Delete
- All core entities implement `ISoftDeletable` — deletions are soft by default
- Cascade soft-delete: removing a Company cascades to Sites, Workflows, Permits, Attachments, and WorkflowHistories
- `RecycleBinController` exposes restore functionality for soft-deleted records
- Cascade restore recovers children deleted within the same cascade operation (10-second window)

### Reporting & Export
- Kendo UI grid-based reporting with Excel export
- Permit and workflow summary dashboards

### Audit & Security
- All CRUD operations logged with timestamp and user (`AuditLog` entity)
- Automatic audit interception via EF Core interceptor
- Session-based access control with multi-tenant data isolation

---

## User Roles

| Role | Description |
|---|---|
| **Super User** | Full system access across all tenants |
| **Portal Admin** | Company-level administration |
| **User** | Standard access — view and submit permits |
| **Permit Issuer** | Can issue and approve permits |
| **Lead Permit Issuer** | Senior issuer with elevated approval authority |
| **Worker** | Field worker — limited to assigned permits |
| **Contractor** | External contractor — restricted access |

---

## Getting Started

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server (local or remote instance)
- Node.js (optional, for asset builds)

### Local Setup

1. Clone the repository:
   ```bash
   git clone <repo-url>
   cd PermitPro-1.2
   ```

2. Configure the connection string in `Web/appsettings.Development.json`:
   ```json
   {
     "ConnectionStrings": {
       "Development": "Server=localhost;Database=PermitPro;Trusted_Connection=True;"
     }
   }
   ```

3. Set required environment variables (via `launchSettings.json` or shell):
   ```
   HANGFIRE_DB_CONNECTION    - Hangfire scheduler database connection string
   SUSPEND_AUTORESUME_DAYS   - Days before suspended permits auto-resume (default: 10)
   UPLOAD_MAX_FILE_SIZE      - Max attachment size in bytes (default: 3 MB)
   UPLOAD_MAX_FILE_COUNT     - Max attachments per permit (default: 5)
   UPLOAD_ALLOWED_FILE_TYPES - Comma-separated allowed types (default: pdf,docx,jpg,jpeg,png)
   USER_CREATE_LIMIT         - Max users per company (default: 15)
   RESERVED_ROLES            - Roles restricted from assignment (e.g. SUPERUSER,PORTALADMIN)
   ```

4. Configure `appsettings.json` sections:
   ```json
   {
     "JwtSettings": {
       "Issuer": "<issuer>",
       "Audience": "<audience>",
       "SecretKey": "<secret>"
     },
     "EmailSettings": { ... },
     "PTWSettings": { ... }
   }
   ```

5. Apply database migrations:
   ```bash
   cd Web
   dotnet ef database update
   ```

6. Run the application:
   ```bash
   dotnet run
   ```

   Default URLs:
   - HTTP: `http://localhost:5144`
   - HTTPS: `https://localhost:7079`

### Docker / MSSQL

To restore a SQL Server backup into a Docker container:
```bash
docker cp sql.bak <container-id>:/var/opt/mssql/data
```

Then restore via `sqlcmd` or SQL Server Management Studio.

---

## Configuration

### Password Policy (enforced via ASP.NET Identity)
- Minimum 12 characters
- Requires uppercase, lowercase, digit, and non-alphanumeric character
- Email confirmation required before account activation
- 3 failed login attempts triggers a 10-minute lockout

### Session & Cookie Policy
- Default cookie lifetime: 3 hours (sliding expiration)
- Persistent "Remember Me" cookie: 30 days
- Session timeout: 60 minutes
- Cookies: HttpOnly, SameSite=Strict
- Data protection tokens expire after 30 minutes

### PTW Certificate Types (`appsettings.json` → `PTWSettings`)
Configurable certificate types mapped to permit form workflows.

---

## Database

Entity Framework Core with SQL Server. Key entities:

| Entity | Purpose |
|---|---|
| `Company` | Multi-tenant root |
| `Site` | Work sites with GPS coordinates |
| `Permit` | Safety permits with status lifecycle |
| `Workflow` / `WorkflowStep` | Approval workflow definitions |
| `WorkflowHistory` | Audit trail of workflow transitions |
| `UserInfo` | Extended ASP.NET Identity user |
| `Certificate` | Safety certifications per permit |
| `Attachment` | Files attached to permits |
| `Notification` | User notification records (Title, Message, read/archived state) |
| `AuditLog` | Full CRUD change log |
| `Division` / `Department` | Organisational structure |
| `Address` | Company address records |
| `Contact` | Company contact records |
| `PermitNumber` | Sequential permit numbering per company |
| `SystemMenu` | Role-based menu visibility configuration |

All core entities support soft-delete via `ISoftDeletable`. Hard deletes are not performed by the application by default (`UseSoftDelete = true`).

Run migrations:
```bash
dotnet ef migrations add <MigrationName> --project Core --startup-project Web
dotnet ef database update --project Core --startup-project Web
```

---

## Deployment

### CI/CD (GitHub Actions)

The workflow at `.github/workflows/deploy-on-merge.yml` triggers when a pull request is merged into `master`:

1. Checkout code
2. Setup .NET 10.0
3. Restore → Build (Release) → Publish (self-contained, win-x64, ReadyToRun)
4. Deploy to **SmarterASP.NET** via WebDeploy

Deployment secrets are stored in GitHub repository settings (not in source).

### WebDeploy (Manual)

Target host: SmarterASP.NET  
Deployment user: `[YOUR_WEB_DEPLOY_USERNAME]`

---

## Security Notes

- Do not commit credentials, API keys, or connection strings to source control
- Use environment variables or secrets management for sensitive configuration
- The `RESERVED_ROLES` environment variable restricts certain role assignments (SUPERUSER, PORTALADMIN)
- All audit events are written to `AuditLog` and are non-deletable by application users
- JWT `SecretKey` must be stored securely and never committed to source control
