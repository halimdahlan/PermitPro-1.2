---
name: session-current-conversation
description: "Full context of current session: PDF discussion, README update, Priority 1 improvements — all uncommitted"
metadata: 
  node_type: memory
  type: project
  originSessionId: ae0f4948-1987-41ca-8af0-8bc3647a93ae
---

Session on 2026-06-29. All changes are uncommitted and ready to be committed on the new machine.

**Why:** User is transporting work to another machine and needs full context continuity.

**How to apply:** On the new machine, run `git status` and `git diff` to see all pending changes, then commit when ready.

## Topics Covered This Session

### 1. PermitService → MessageService company ID question
- Answer: MessageService self-resolves company via `ICurrentUserService.GetCurrentUser()` — no need to pass explicitly for web requests
- Only needed for background jobs without HTTP context

### 2. PDF Generation Discussion
- Current: PuppeteerSharp (headless Chrome) + Scriban HTML templates + Bootstrap CSS
- QuestPDF: NOT viable as drop-in (doesn't render HTML — requires full rewrite of ~1000 lines + 8 certificate templates)
- Best alternatives that keep HTML templates:
  - **DinkToPdf** (free, wkhtmltopdf wrapper, best for SmarterASP.NET hosting) — recommended
  - **Playwright** (MS-maintained, same Chromium rendering, no visual regressions)
  - **IronPDF** (commercial, embedded Chromium)
- html2pdf.js (client-side) — ruled out (rasterized text, no server-side JSON-to-HTML flow)

### 3. README.md Updated
- Controller count 21→23, ViewModels 32→33, Entities 20→26
- Added: Rate Limiting row, enhanced Workflow Engine section, App Settings categories table, System Menu Management section, AGT + Permit Holder roles, missing entities, rate limiting in Security Notes

### 4. Priority 1 Improvements (ALL APPLIED)
See [[session-priority1-improvements]] for details.

### 5. Full Improvement Roadmap Discussed
Priority 2 (not done):
- Split PermitService (1968 lines) into Crud/Pdf/Notification services
- Enable #nullable across 105 files
- Migrate Newtonsoft → System.Text.Json
- Typed request models instead of IFormCollection
- Broader IMemoryCache usage

Priority 3 (not done):
- CSRF coverage audit (AutoValidateAntiforgeryToken)
- Rate limiting on PDF export + file upload
- Singleton browser or DinkToPdf swap
- Email retry with Polly

Priority 4 (not done):
- Unit/integration test project
- Re-enable Hangfire or switch to IHostedService
- API versioning

## Files Modified (Uncommitted)
- `README.md` — full documentation refresh
- `Web/Program.cs` — health checks, response caching, using directive
- `Web/App.csproj` — AspNetCore.HealthChecks.SqlServer package
- `Web/Controllers/DashboardController.cs` — async, CancellationToken, ILogger, ResponseCache
- `Web/Controllers/ReportsController.cs` — async, CancellationToken, AsNoTracking, ILogger, ResponseCache
- `Web/Controllers/WorkflowController.cs` — async, CancellationToken, AsNoTracking
- `Web/Controllers/UsersController.cs` — AsNoTracking on read queries
- `Core/Services/PermitService.cs` — ILogger<PermitService> added
- `Core/Services/MessageService.cs` — ILogger<MessageService> + structured log calls
