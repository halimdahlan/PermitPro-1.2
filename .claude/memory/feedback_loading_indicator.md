---
name: feedback-loading-indicator
description: User prefers localized inline loading spinners over full-page overlays
metadata: 
  node_type: memory
  type: feedback
  originSessionId: 685637db-3874-471d-892e-b3efb915554b
---

Do NOT use the global full-page loading overlay (`$('.loading').show()`) when triggering data loads from filter forms or buttons unless the entire page is navigating away.

**Why:** User explicitly rejected this: "i do not need a full page overlay. just in the area where the cards and charts be displayed."

**How to apply:** When an action loads data into a specific section of the page (e.g., a stats area, a chart container, a tab), show a localized spinner *inside that section* instead. Use a `<div id="...Loading">` with a Font Awesome `fa-loader fa-spin` icon positioned in the target area. Hide it in the AJAX `complete` callback.

Example pattern used in Reports:
```html
<div id="reportLoading" class="text-center py-5" style="display:none">
    <i class="fa-solid fa-loader fa-spin text-secondary" style="font-size:2rem"></i>
    <p class="text-secondary mt-2 mb-0 small">Loading statistics…</p>
</div>
```
```js
$('#reportLoading').show();
// ... AJAX call ...
complete: function() { $('#reportLoading').hide(); }
```
