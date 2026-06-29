---
name: session-workflow-revamp
description: "New workflow section: Overview dashboard, Manage list, Builder page — original views untouched"
metadata:
  type: project
  originSessionId: 2b686528-d582-4c20-9cf2-d4029e6d7ca5
---

A new enhanced workflow section was built alongside the existing one. The original `Index.cshtml` and `Edit.cshtml` were not touched.

**Why:** The existing workflow UX was fragmented — steps table used DataTables (inconsistent), no stats, no submenu, edit panel appeared/disappeared as a floating side panel. New section builds a clean three-page flow with Kendo grids throughout.

**How to apply:** All new workflow routes are at `/workflow/overview`, `/workflow/manage`, `/workflow/builder/{id}`. Original routes (`/workflow`, `/workflow/edit/{id}`) still exist and work.

## New Routes

| Route | Action | View |
|---|---|---|
| `GET /{company}/workflow/overview` | `Overview()` | `Overview.cshtml` |
| `GET /{company}/workflow/manage` | `Manage()` | `Manage.cshtml` |
| `GET /{company}/workflow/builder/{id}` | `Builder(string id)` | `Builder.cshtml` |
| `GET /{company}/workflow/overview/stats` | `GetOverviewStats(Guid)` | JSON |
| `GET /{company}/workflow/manage/grid` | `GetManageGrid(Guid)` | JSON (Kendo format) |

## New Files

### ViewModels
- `WorkflowManageGridViewModel.cs` — Id, Name, Description, IsActive, HasCertificates, StepCount, PermitCount, CreatedWhen, ActionIcons
- `WorkflowBuilderViewModel.cs` — WorkflowId, WorkflowName, WorkflowDescription, WorkflowIsActive, WorkflowHasCertificate, StepCount, PermitCount
- `WorkflowApproverViewModel.cs` — id, firstName, lastName, email (lowercase props to match JSON from `/users/workflowusers/all` endpoint)

### Views
- `Overview.cshtml` — 4 KPI cards (Total, Active, Inactive, Permits Assigned) + doughnut chart (status) + horizontal bar chart (permits by workflow, top 6) + Kendo summary grid
- `Manage.cshtml` — Filter pills (ALL/ACTIVE/INACTIVE) + Kendo Grid with StepCount + PermitCount columns + "New Workflow" modal → redirects to Builder on create
- `Builder.cshtml` — Two-column: left = workflow info form + mini stat cards (Steps, Permits); right = visual step pipeline with inline Bootstrap collapse panels per step for edit (General + Approvers tabs), Kendo Grid for approver picker modal

## Modified Files

### `WorkflowController.cs`
Added two new regions:
- `#region "Views - Enhanced"` — Overview, Manage, Builder actions
- `#region "API - Enhanced"` — GetOverviewStats, GetManageGrid
- `#region "Private static functions/methods"` — added `WorkflowManageGridActionIcons` (builder link), existing `WorkflowsGridActionIcons` unchanged

### `_SideMenuPartial.cshtml`
"Workflows" single nav item → collapsible submenu:
- Overview → `/{company}/workflow/overview`
- All Workflows → `/{company}/workflow/manage`
Submenu auto-expands server-side when `currentRoute == "workflow"`. Uses `data-bs-toggle="collapse"`.

### `workflow.css`
Added: `.builder-step`, `.builder-step-locked`, `.step-order-badge`, `.step-badge-active`, `.step-badge-system`, `.step-edit-panel`, `.step-edit-body`, `.step-connector`, `.step-action-btn`, `.step-action-danger`, grid styles for `#gridManage`/`#gridOverview`/`#gridUsers`, `.chart-wrap-sm`.

## Key Implementation Notes

- **Step pipeline is JS-rendered**: steps loaded via AJAX from existing `GET /{company}/workflow/workflows/{workflowId}/steps`, rendered as HTML cards with inline Bootstrap collapse edit panels
- **All existing API endpoints are reused** by the Builder (create step, update step, move step, delete step, update approvers, delete approver)
- **Approver "add" flow**: loads current approvers from server, merges with `pendingApprovers[]`, submits full list to `PUT /{company}/workflow/steps/{id}/approvers` (endpoint replaces all)
- **`WorkflowApproverViewModel` lowercase props**: the `/users/workflowusers/all` endpoint returns camelCase JSON (`id`, `firstName`, `lastName`, `email`), so props must match exactly
- **Build fix**: `Grid<dynamic>` with `.Template("").ClientTemplate(...)` causes CS1061 — must use typed ViewModel + `c.Bound(f => f.field).ClientTemplate(...)`
