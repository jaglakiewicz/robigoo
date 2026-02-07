# Design Document: UI Consistency, Accessibility & Mobile Responsiveness

## Overview

This design addresses the systematic inconsistencies in the Robigoo Field Sprayer Control Station's CSS architecture. The core strategy is to extract duplicated patterns from component CSS files into shared style modules, standardize design tokens, unify responsive breakpoints, and improve accessibility compliance. The crop-sprayers and clients components serve as the reference standard — their visual patterns (master-detail layout, card structure, badge styling, panel design) will be codified into shared CSS modules that all other components consume.

The approach is CSS-only — no Angular component refactoring is needed. Existing shared Angular components (MasterDetailLayoutComponent, EntityListPanelComponent, EntityDetailPanelComponent) already exist but their CSS patterns need to be extracted into shared modules that component-level CSS can also use directly.

## Architecture

### CSS Module Architecture

The shared styles directory (`Client/src/app/shared/styles/`) will be reorganized from 6 modules to 8 modules:

```
shared/styles/
├── _variables.css      # Design tokens (updated)
├── _reset.css          # CSS reset & base typography (updated)
├── _layouts.css        # NEW: Master-detail, grid layouts, panels
├── _cards.css          # NEW: Entity cards, badges, card patterns
├── _forms.css          # Form elements (updated)
├── _buttons.css        # Button styles (minor updates)
├── _components.css     # Dialogs, tables, step indicators, etc. (trimmed)
├── _utilities.css      # Utility classes (updated)
└── globals.css         # Entry point (updated import order)
```

### Import Order (globals.css)

```css
@import './_variables.css';
@import './_reset.css';
@import './_layouts.css';
@import './_cards.css';
@import './_forms.css';
@import './_buttons.css';
@import './_components.css';
@import './_utilities.css';
```

### Design Decision: CSS Modules vs Angular Component Styles

We chose to extract patterns into shared CSS modules rather than forcing all components to use the shared Angular components (MasterDetailLayoutComponent, etc.) because:
1. The existing components already have working HTML templates that reference specific class names
2. Changing HTML templates would require significant testing of component behavior
3. CSS-only changes are lower risk and can be done incrementally
4. The shared Angular components' CSS will also benefit from importing these same shared modules

## Components and Interfaces

### 1. Updated Design Token System (`_variables.css`)

Changes to the `:root` block:

```css
/* Typography — 4 primary sizes + 1 display */
--font-size-xs: 11px;    /* NEW: for labels, captions */
--font-size-sm: 13px;
--font-size-base: 15px;
--font-size-lg: 18px;
--font-size-xl: 24px;
--font-size-4xl: 48px;

/* Font weights — add medium */
--font-weight-regular: 400;
--font-weight-medium: 500;  /* NEW */
--font-weight-bold: 700;

/* Spacing — differentiated scale */
--space-xs: 4px;    /* CHANGED from 6px */
--space-sm: 8px;    /* CHANGED from 6px */
--space-md: 12px;
--space-lg: 18px;
--space-xl: 24px;
--space-2xl: 36px;
--space-3xl: 48px;

/* Border Radius — differentiated */
--radius: 6px;
--radius-sm: 4px;   /* CHANGED from var(--radius) */
--radius-md: 6px;   /* Stays 6px */
--radius-lg: 10px;  /* CHANGED from var(--radius) */

/* Breakpoints (as reference values, used in media queries) */
--breakpoint-sm: 600px;
--breakpoint-md: 768px;
--breakpoint-lg: 1024px;
```

### 2. New Layouts Module (`_layouts.css`)

Extracted from crop-sprayers/clients pattern:

```css
/* Master-Detail two-column layout */
.md-layout {
  display: grid;
  grid-template-columns: minmax(220px, 1fr) minmax(0, 2.5fr);
  gap: var(--space-lg);
  flex: 1;
  min-height: 0;
}

/* List panel (left side) */
.md-list-panel {
  background: var(--panel);
  border-radius: var(--radius-md);
  border: 1px solid var(--panel-border);
  padding: var(--space-md);
  display: flex;
  flex-direction: column;
  min-height: 0;
  overflow: visible;
}

/* Detail panel (right side) */
.md-detail-panel {
  background: var(--panel);
  border-radius: var(--radius-md);
  border: 1px solid var(--panel-border);
  padding: var(--space-md);
  display: flex;
  flex-direction: column;
  min-height: 0;
  overflow: visible;
}

/* Detail header */
.md-detail-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--space-md);
}

.md-detail-title {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-bold);
}

.md-detail-sub {
  font-size: var(--font-size-sm);
  color: var(--text-light);
}

/* Filters row */
.md-filters-row {
  display: flex;
  gap: var(--space-sm);
  margin-bottom: var(--space-md);
  align-items: center;
}

.md-filters-row .search-input {
  flex: 1;
  min-width: 0;
}

/* Step content & navigation */
.md-step-content {
  flex: 1;
  overflow-y: auto;
  min-height: 0;
}

.md-step-navigation {
  display: flex;
  gap: var(--space-md);
  justify-content: space-between;
  align-items: center;
  margin-top: var(--space-xl);
  padding-top: var(--space-lg);
  border-top: 1px solid var(--panel-border);
}

/* Form grid patterns */
.form-row { display: grid; column-gap: var(--space-lg); row-gap: var(--space-sm); }
.form-row-1 { grid-template-columns: 1fr; max-width: 300px; }
.form-row-2 { grid-template-columns: repeat(2, minmax(0, 1fr)); }
.form-row-3 { grid-template-columns: repeat(3, minmax(0, 1fr)); }

/* Responsive collapse */
@media (max-width: 768px) {
  .md-layout { grid-template-columns: 1fr; }
  .form-row-2, .form-row-3 { grid-template-columns: 1fr; }
}
```

### 3. New Cards Module (`_cards.css`)

Extracted from crop-sprayers/clients card pattern:

```css
/* Entity card (list item) */
.entity-card {
  display: flex;
  gap: var(--space-xs);
  padding: var(--space-sm);
  border-radius: var(--radius-sm);
  border: 1px solid var(--panel-border);
  cursor: pointer;
  transition: all var(--transition-fast);
}

.entity-card:hover {
  background: var(--bg-alt);
  border-color: var(--accent-light);
}

.entity-card.active {
  background: var(--primary-surface);
  border-color: var(--primary-btn-border);
}

/* Card inner structure */
.card-main {
  display: flex;
  flex-direction: column;
  width: 100%;
  position: relative;
  gap: var(--space-xs);
}

.card-badges-row {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-xs);
  justify-content: flex-start;
}

.card-title {
  font-weight: var(--font-weight-bold);
  font-size: var(--font-size-base);
  color: var(--text);
  line-height: 1.2;
}

.card-info-row {
  font-size: var(--font-size-sm);
  color: var(--text-light);
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--space-xs);
  line-height: 1.4;
}

/* Entity card badges */
.entity-card .badge {
  padding: 0 var(--space-xs);
  border-radius: var(--radius-sm);
  font-size: var(--font-size-sm);
  line-height: 1.3;
  border: 1px solid var(--panel-border);
  background: var(--bg-elevated);
}

.entity-card .badge-type {
  border-color: var(--primary-border);
  background: var(--primary-surface);
  color: var(--accent);
}

.entity-card .badge-kind,
.entity-card .badge-success {
  border-color: var(--success-border);
  background: var(--success-surface);
  color: var(--success-dark);
}

.entity-card .badge-danger {
  border-color: var(--error-border);
  background: var(--error-surface);
  color: var(--error-text);
}

.entity-card .badge-secondary {
  border-color: var(--border-light);
  background: var(--bg-alt);
  color: var(--text-muted);
}

/* Entity list (scrollable container) */
.entity-list {
  flex: 1;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  gap: var(--space-xs);
  min-height: 0;
  transition: filter var(--transition-fast), opacity var(--transition-fast);
}

.entity-list.blurred {
  filter: blur(2px);
  opacity: 0.5;
  pointer-events: none;
}
```

### 4. Updated Reset Module (`_reset.css`)

Key changes:
- Standardize breakpoints to 600px / 768px / 1024px
- Add `prefers-color-scheme` media query for auto theme detection
- Add safe-area-inset support for notched devices

```css
/* Auto theme detection */
@media (prefers-color-scheme: dark) {
  :root:not([data-theme]) {
    /* Apply dark theme variables when no explicit preference */
  }
}

/* Safe area for notched devices */
@media (max-width: 600px) {
  body {
    padding-left: env(safe-area-inset-left, 0px);
    padding-right: env(safe-area-inset-right, 0px);
    padding-bottom: env(safe-area-inset-bottom, 0px);
  }
}
```

### 5. Component CSS Refactoring Strategy

Each component CSS file will be refactored to:
1. Keep only component-specific styles (unique to that component)
2. Remove all duplicated patterns that now exist in shared modules
3. Use shared class names where HTML templates already use matching names
4. Add CSS aliases where HTML uses component-specific class names (e.g., `.sprayers-layout` maps to `.md-layout` via shared properties)

#### Inspections Component (`inspections.component.css`)
- Remove: duplicated layout, card, badge, loading-state, dialog, spinner styles (~200 lines)
- Keep: dashboard view, accordion, check items, protocol document preview, section-specific styles
- The dialog styles will be removed since `_components.css` already provides them

#### Settings Component (`settings.component.css`)
- Remove: duplicated form input styles that match `_forms.css`
- Keep: admin section, toggle form, user table, avatar section (unique to settings)
- Standardize heading sizes to match reference components

#### Notifications Component (`notifications.component.css`)
- Already relatively clean; minor alignment to shared card patterns

#### New Inspection Component (`new-inspection.component.css`)
- Keep: protocol-specific styles, check rows, measurement inputs, PDF preview
- Improve: mobile layout for protocol navigation, form stacking

### 6. Accessibility Improvements

#### ARIA Landmarks
Add to `app.component.html`:
- `role="navigation"` on sidebar
- `role="main"` on main content area
- `role="complementary"` on status bar

#### Focus Management
- Ensure all dialogs trap focus using existing Angular dialog patterns
- Verify focus indicators meet 3:1 contrast ratio (already mostly in place via `--focus-ring`)

#### Form Labels
- Audit all `<input>` elements for associated `<label>` or `aria-label`
- Add missing labels where needed

#### Color Contrast
- The existing theme tokens are designed for WCAG AA compliance
- Verify `--text-secondary` and `--text-muted` meet 4.5:1 on their respective backgrounds
- Adjust if needed (particularly `--text-muted` in dark theme)

## Data Models

This feature does not introduce new data models. The changes are purely CSS/HTML presentational. The "data" involved is the design token system:

### Design Token Schema

```
DesignTokens {
  typography: {
    fontSizeXs: 11px
    fontSizeSm: 13px
    fontSizeBase: 15px
    fontSizeLg: 18px
    fontSizeXl: 24px
    fontSize4xl: 48px
    fontWeightRegular: 400
    fontWeightMedium: 500
    fontWeightBold: 700
  }
  spacing: {
    xs: 4px
    sm: 8px
    md: 12px
    lg: 18px
    xl: 24px
    2xl: 36px
    3xl: 48px
  }
  borderRadius: {
    sm: 4px
    md: 6px
    lg: 10px
  }
  breakpoints: {
    sm: 600px
    md: 768px
    lg: 1024px
  }
}
```


## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system — essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

Since this feature is primarily CSS refactoring, the correctness properties focus on structural invariants of the CSS codebase rather than runtime behavior. These properties can be verified by parsing CSS files and checking structural rules.

### Property 1: Design token uniqueness within groups

*For any* set of design tokens within the same group (spacing tokens `--space-*`, border-radius tokens `--radius-*`), each token SHALL map to a distinct resolved pixel value. No two tokens in the same group share the same value.

**Validates: Requirements 1.1, 1.4**

### Property 2: Standardized breakpoint usage

*For any* media query `@media (max-width: Xpx)` or `@media (min-width: Xpx)` found in any component CSS file, the breakpoint value X SHALL be one of the standardized values (600, 768, 1024) or a value used exclusively within print/contrast/motion media queries.

**Validates: Requirements 1.5, 4.1**

### Property 3: All CSS variable references resolve to defined tokens

*For any* `var(--token-name)` reference found in any CSS file within the project, the token `--token-name` SHALL be defined in `_variables.css` or in the same file's `:root`/`:host` scope.

**Validates: Requirements 1.6**

### Property 4: No shared style duplication in component CSS

*For any* CSS class that is defined in the shared style modules (`_layouts.css`, `_cards.css`, `_components.css`, `_buttons.css`, `_forms.css`), no component CSS file SHALL contain a redefinition of that same class selector with identical or near-identical properties.

**Validates: Requirements 2.8, 2.9, 2.10, 6.4, 6.5**

### Property 5: Consistent heading typography

*For any* CSS rule targeting heading elements (h1, h2, h3, h4) across all CSS files, the font-size value SHALL use the standardized token for that heading level (h1/h2: `--font-size-xl`, h3: `--font-size-lg`, h4: `--font-size-base`).

**Validates: Requirements 3.1, 3.2, 3.3**

### Property 6: No hardcoded font sizes in component CSS

*For any* `font-size` declaration in any component CSS file (files matching `*.component.css`), the value SHALL reference a CSS variable (`var(--font-size-*)`) rather than a hardcoded pixel, rem, or em value. Exception: protocol document preview styles that target print output.

**Validates: Requirements 1.2, 3.4**

### Property 7: Master-detail responsive collapse

*For any* CSS file that defines a master-detail grid layout (grid-template-columns with the two-column pattern), there SHALL exist a corresponding `@media (max-width: 768px)` rule that sets `grid-template-columns: 1fr`.

**Validates: Requirements 4.2**

### Property 8: WCAG AA color contrast compliance

*For any* text color / background color pair defined in the theme variables (`_variables.css`), the computed contrast ratio SHALL meet WCAG 2.1 AA minimum: 4.5:1 for normal text (`--text` on `--bg`, `--text-secondary` on `--surface`, etc.) and 3:1 for large text. This SHALL hold for both light and dark theme definitions.

**Validates: Requirements 5.2**

### Property 9: Form input label association

*For any* `<input>`, `<select>`, or `<textarea>` element in any Angular component HTML template, the element SHALL have either an associated `<label>` element (via `for`/`id` or wrapping), an `aria-label` attribute, or an `aria-labelledby` attribute.

**Validates: Requirements 5.4**

### Property 10: No !important overrides on button styles in component CSS

*For any* CSS declaration in a component CSS file that targets button elements or classes containing "btn", the declaration SHALL NOT use the `!important` modifier. Exception: disabled state pointer-events.

**Validates: Requirements 7.5**

## Error Handling

This feature is CSS/HTML only and does not introduce new error states. The primary risk areas are:

1. **CSS Variable Fallbacks**: If a CSS variable is undefined, the browser uses the initial value. The design mitigates this by ensuring all referenced tokens are defined (Property 3).

2. **Breakpoint Mismatches**: If a component uses a non-standard breakpoint, layouts may behave inconsistently. The design mitigates this by standardizing all breakpoints (Property 2).

3. **Theme Switching**: When switching between light and dark themes, all color tokens must be defined in both themes. The existing architecture already handles this with parallel `:root[data-theme="light"]` and `:root[data-theme="dark"]` blocks.

4. **Safe Area Insets**: On devices without notches, `env(safe-area-inset-*)` resolves to 0px, so the fallback is safe.

## Testing Strategy

### Approach

Since this feature is CSS refactoring, traditional unit tests and property-based tests operate on the CSS source files as text/AST rather than on runtime behavior. The testing strategy uses:

1. **CSS Linting / Static Analysis**: A Node.js script that parses CSS files and checks structural properties
2. **Visual Regression Testing**: Manual comparison of component screenshots before/after changes
3. **Accessibility Auditing**: Lighthouse and axe-core checks for WCAG compliance

### Property-Based Tests

Property-based tests will be implemented using `fast-check` (JavaScript property-based testing library) to generate test inputs and verify CSS structural properties.

Each correctness property maps to a test that:
- Parses the relevant CSS files
- Extracts the relevant declarations/rules
- Verifies the property holds across all instances

Configuration:
- Minimum 100 iterations per property test
- Each test tagged with: **Feature: ui-consistency-accessibility, Property {N}: {title}**

### Unit Tests

Unit tests will cover specific examples:
- Verify `_layouts.css` contains expected class definitions
- Verify `_cards.css` contains expected class definitions
- Verify `globals.css` import order is correct
- Verify safe-area-inset rules exist at 600px breakpoint
- Verify `prefers-color-scheme` media query exists in `_reset.css`
- Verify ARIA landmark roles exist in `app.component.html`

### Manual Testing Checklist

- [ ] Verify all screens match crop-sprayers/clients visual pattern
- [ ] Test on iPhone 16 viewport (393×852) in both orientations
- [ ] Test light and dark theme switching
- [ ] Test keyboard navigation through all interactive elements
- [ ] Verify focus indicators are visible in both themes
- [ ] Test sidebar behavior on mobile (full-width expand/collapse)
