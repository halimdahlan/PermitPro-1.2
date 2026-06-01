# PermitPro

A multi-tenant, enterprise web application for managing **Permit-to-Work (PTW)** systems and workplace safety permits. Built on ASP.NET Core 8.0 with role-based access control, workflow automation, and full audit logging.

---

## Tech Stack

| Layer | Technology |
|---|---|
| **Runtime** | .NET 8.0 (LTS) |
| **Web Framework** | ASP.NET Core 8.0 (MVC + Razor Pages) |
| **ORM** | Entity Framework Core 8.0.7 (SQL Server) |
| **Auth** | ASP.NET Identity + JWT Bearer |
| **UI Components** | Kendo UI for ASP.NET MVC (Telerik) |
| **Background Jobs** | Hangfire 1.8.14 |
| **PDF Generation** | PuppeteerSharp 18.0.3 |
| **Excel Export** | ClosedXML 0.102.2 |
| **Email** | MailKit 4.15.1 + MimeKit 4.15.1 |
| **Templating** | Scriban.Signed 5.10.0 |
| **3D Visualization** | Three.js |
| **API Docs** | Swashbuckle / Swagger |

---

## Project Structure

```
PermitPro-1.2/
├── Web/                        # ASP.NET Core MVC web application
│   ├── Areas/Identity/         # Scaffolded identity pages (login, register, 2FA)
│   ├── Controllers/            # 18 MVC controllers
│   ├── Models/                 # AJAX request/response models, chart models
│   ├── ViewModels/             # 30+ typed view models
│   ├── Views/                  # Razor templates per feature area
│   └── wwwroot/                # Static assets (CSS, JS, images, themes)
├── Core/                       # Domain logic class library
│   ├── Data/                   # ApplicationDbContext + DTO models
│   ├── Entities/               # 25+ domain entities
│   ├── Enums/                  # PermitStatus, WorkflowStatus, SiteType, etc.
│   ├── Interfaces/             # 10 service interfaces
│   ├── Services/               # 7 core service implementations
│   ├── Filters/                # Authorization filters
│   ├── Helpers/                # Email, PTW settings, license helpers
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
- Permit lifecycle: Draft → Pending → Approved → Suspended → Closed / Overdue
- File attachments (PDF, DOCX, JPG, PNG — up to 5 files, 3 MB each)
- PDF export of permit documents via headless Chrome

### Workflow Engine
- Configurable multi-step approval workflows per company
- Workflow history and full audit trail
- Background jobs for automatic permit closure and auto-resume logic (10-day threshold)

### Multi-Tenancy
- Each company operates in an isolated namespace
- Route pattern: `/{company}/{controller}/{action}`
- All data scoped to the tenant company

### Sites & Location
- Manage work sites with GPS coordinates
- 3D site visualization using Three.js
- Site map dashboard overlay

### Notifications & Reporting
- In-app notifications + SMTP email alerts
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

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
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
       "DefaultConnection": "Server=localhost;Database=PermitPro;Trusted_Connection=True;"
     }
   }
   ```

3. Set required environment variables (via `launchSettings.json` or shell):
   ```
   HANGFIRE_DB_CONNECTION   - Hangfire scheduler database connection string
   SUSPEND_AUTORESUME_DAYS  - Days before suspended permits auto-resume (default: 10)
   UPLOAD_MAX_FILE_SIZE     - Max attachment size in bytes (default: 3 MB)
   UPLOAD_MAX_FILE_COUNT    - Max attachments per permit (default: 5)
   UPLOAD_ALLOWED_FILE_TYPES - Comma-separated allowed types (default: pdf,docx,jpg,jpeg,png)
   USER_CREATE_LIMIT        - Max users per company (default: 15)
   ```

4. Apply database migrations:
   ```bash
   cd Web
   dotnet ef database update
   ```

5. Run the application:
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
- Cookie lifetime: 3 hours
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
| `Notification` | User notification records |
| `AuditLog` | Full CRUD change log |
| `Division` / `Department` | Organisational structure |

Run migrations:
```bash
dotnet ef migrations add <MigrationName> --project Core --startup-project Web
dotnet ef database update --project Core --startup-project Web
```

---

## Deployment

### CI/CD (GitHub Actions)

The workflow at `.github/workflows/deploy-on-merge.yml` triggers on merge to `master`:

1. Checkout code
2. Setup .NET 8.0
3. Restore → Build (Release) → Publish (self-contained)
4. Deploy to **SmarterASP.NET** via WebDeploy

Deployment secrets are stored in GitHub repository settings (not in source).

### WebDeploy (Manual)

Target host: SmarterASP.NET  
Deployment user: `halimdahlan-002-subsite7`

---

## API Documentation

Swagger UI is available at `/swagger` when running in Development mode (configured via Swashbuckle).

---

## Security Notes

- Do not commit credentials, API keys, or connection strings to source control
- Use environment variables or secrets management for sensitive configuration
- The `RESERVED_ROLES` environment variable restricts certain role assignments (SUPERUSER, PORTALADMIN)
- All audit events are written to `AuditLog` and are non-deletable by application users
