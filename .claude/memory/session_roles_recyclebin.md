---
name: session-roles-recyclebin
description: "Changes made to add Role soft-delete support to the Recycle Bin (Index, Restore, Delete + view tab)"
metadata: 
  node_type: memory
  type: project
  originSessionId: 685637db-3874-471d-892e-b3efb915554b
---

Roles were wired into the recycle bin system. Roles are a global entity (not company-scoped).

**Why:** The recycle bin previously had tabs for Permits, Users, Workflow Steps, Companies — but no Roles tab despite `Role` having soft-delete support via `ISoftDeletable`.

**How to apply:** When adding any new entity to the recycle bin, follow the same pattern: add a query in `Index`, add `ViewBag`, add a case to `Restore` switch and `Delete` switch, add the nav tab + pane to the view.

## Files Modified

### `Web/Controllers/RecycleBinController.cs`

**Index action** — added roles query and ViewBag:
```csharp
var roles = await _dbContext.Roles
    .IgnoreQueryFilters()
    .Where(r => r.IsDeleted)
    .OrderByDescending(r => r.DeletedWhen)
    .Select(r => new DeletedItem
    {
        Id = Guid.Parse(r.Id),
        EntityType = "Role",
        DisplayName = r.Name,
        Detail = r.Description,
        CompanyId = Guid.Empty,
        CompanyName = "(global)",
        DeletedWhen = r.DeletedWhen,
        DeletedBy = r.DeletedBy,
    })
    .ToListAsync();
ViewBag.Roles = roles;
```
Also added roles to `allDeletedByIds` concat and name-resolution loop.

**Restore switch** — new "Role" case:
```csharp
"Role" => await _dbContext.Roles.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == id.ToString()),
```

**Delete switch** — same lookup pattern as Restore.

### `Web/Views/RecycleBin/Index.cshtml`

- Added `var roles = (List<DeletedItem>)ViewBag.Roles;`
- Added Roles nav tab (between Users and Companies)
- Added Companies nav tab (was a pre-existing bug — Companies pane existed but had no nav button)
- Added Roles tab pane using `_RecycleBinTable` partial with columns `"Name"`, `"Description"`, `""`

**Note:** Companies tab is conditional on `isSuperUser`.
