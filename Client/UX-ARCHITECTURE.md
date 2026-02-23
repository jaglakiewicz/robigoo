## Frontend UX & structure plan

This document describes how to evolve the Angular frontend into a clearer, more reusable, and mobile-friendly UI while keeping the existing visual identity.

---

## 1. Shell, navigation, and layout

### 1.1 Current state

- `AppComponent` (`app.component.ts/html`) acts as:
  - Authentication gate (shows `<app-login>` vs main app).
  - Layout shell (sidebar + top bar + main content).
  - Tab manager using an internal `tabs` array and `NavigationService`.
- Navigation is **tab-based**, not URL-based:
  - No `RouterModule`; tabs map to types like `'new'`, `'inspections'`, `'clients'`, `'marks'`, `'notifications'`, `'types'`, `'settings'`.
  - `NavigationService` exposes `navigation$` with `NavigationRequest` to open/focus tabs.
- Global layout and responsive behavior are defined in `styles.css` and shared layout CSS (`globals.css`, `_layouts.css`).

### 1.2 Target structure

Introduce a clearer separation of concerns:

- `ShellModule` (or `LayoutModule`):
  - Owns the **app shell**: sidebar, top bar, user menu, and tab strip.
  - Contains:
    - `ShellComponent` (current `AppComponent` responsibilities).
    - `SidebarComponent` (menu + theme toggle + user menu).
    - `TabsComponent` (tab bar and tab actions).
  - Uses `NavigationService` for tab events and shells.

- `Feature modules`:
  - `InspectionsModule`, `ClientsModule`, `MachinesModule`, `SettingsModule`, etc.
  - Each exposes the main page component used in tabs:
    - `NewInspectionPage`, `InspectionsPage`, `ClientsPage`, `InspectionMarksPage`, `NotificationsPage`, `CropSprayersPage`, `SettingsPage`.

- Optional: introduce `RouterModule` gradually:
  - Routes like:
    - `/inspections`, `/clients`, `/machines`, `/settings`.
  - Each route opens/activates the corresponding tab, so URL and tab state stay in sync.

The immediate step is to keep the **tab system** but extract shell pieces into smaller components for clarity and reusability.

---

## 2. Component reuse and design system

### 2.1 Current shared components

- Shared components under `app/shared/components`:
  - `master-detail-layout`, `entity-toolbar`, `entity-list-panel`, `entity-detail-panel`, `generic-list`, `filter-panel`, `date-picker`, `custom-select`, `numeric-input`, `client-autocomplete`, `history-dialog`, `step-indicator`, `toast-notification`, `busy-indicator`, etc.
- Shared directives (`app/shared/directives`):
  - `scroll-fade`, `scroll-center`, `busy-overlay`.
- Global layout utilities and tokens in `app/shared/styles`.

### 2.2 Target reuse patterns

Standardize feature screens to use the shared building blocks consistently:

- **Master–detail screens** (e.g. clients, inspections, crop sprayers):
  - Use `master-detail-layout` with:
    - `entity-list-panel` (list, filters).
    - `entity-detail-panel` (forms, history).
  - Encourage a **single pattern** per area so that users recognize the interaction model.

- **Commands and toolbars**:
  - Use `entity-toolbar` for primary and secondary actions.
  - Avoid custom per-screen button bars unless they provide unique functionality.

- **Forms and inputs**:
  - Use `custom-select`, `numeric-input`, and shared form styles for consistency.
  - Define reusable **form layouts** (e.g. two-column desktop, one-column mobile) in shared CSS.

- **History and audit**:
  - Use `history-dialog` consistently to display the generic history from `/api/history/...`:
    - Provide the appropriate `entityName` and `entityId` so users can see who changed what and when for any record.

---

## 3. Mobile and tablet UX

### 3.1 Current behavior

- `styles.css` defines responsive behavior:
  - **<= 1024px**:
    - Sidebar narrows; app-shell margin adjusted.
    - Tabs and type scale down slightly.
  - **<= 600px**:
    - Sidebar becomes an **off-canvas overlay**.
    - Tabs shrink and become horizontally scrollable.
    - Main content padding and border-radius reduced.
- `AppComponent.handleMenuClick`:
  - Closes the sidebar automatically on small screens after opening a tab.

### 3.2 Target improvements

Short-term mobile refinements:

- Ensure **main-area** is always readable on small screens:
  - Confirm that complex forms (new inspection, clients, crop sprayers) use:
    - One-column layouts under 600px.
    - Clear grouping and spacing between sections.
- Increase **touch targets**:
  - Confirm that buttons and inputs respect a minimum `var(--touch-size)` height (already used in some places).
  - Avoid very small icons without accompanying hit areas.
- Simplify **tab bar** on phones:
  - Limit visible text length and rely on consistent icons (already partially handled via ellipsis and max-width).

Medium-term improvements:

- Introduce small **utility classes** for mobile:
  - `.hide-on-mobile`, `.show-on-mobile`, `.stack-on-mobile` to be used by feature templates where needed.
- For master–detail screens:
  - On small screens, switch to a pattern where:
    - List is shown first.
    - Detail slides over or replaces list (back button at top).
  - This can be achieved using `master-detail-layout` with media-query-driven CSS changes.

---

## 4. Alignment with backend audit trail

To fully exploit the backend’s audit capabilities:

- Use a common helper or service on the frontend (e.g. `HistoryService`) that:
  - Builds the correct URL for `/api/history/{entityName}/{entityId}`.
  - Opens `history-dialog` with entries from the backend `ChangeLogs`.
- Ensure each screen that edits data (clients, crop sprayers, inspections, protocols) exposes:
  - A **“View history”** action near the main toolbar or detail header.

This gives users a consistent way to inspect **who changed what and when** for every important entity, matching the backend design.

---

## 5. Summary

- The current Angular app already has a solid shell, design system, and responsive base.
- The main improvements are:
  - Structuring the shell into dedicated layout components/modules.
  - Enforcing consistent use of shared components for list/detail, forms, and history.
  - Refining mobile layouts and touch interactions.
  - Surfacing backend audit history in a uniform way across all entities.

These changes make the frontend easier to extend, more predictable for users, and ready to sit cleanly in front of the layered backend for cloud deployment.

