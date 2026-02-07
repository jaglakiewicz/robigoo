# Implementation Plan: UI Consistency, Accessibility & Mobile Responsiveness

## Overview

Incremental CSS refactoring of the Robigoo Field Sprayer Control Station. We start by updating design tokens, then create new shared CSS modules, refactor each component CSS to use shared patterns, improve mobile responsiveness, and add accessibility improvements. Each step builds on the previous and is validated by structural CSS tests.

## Tasks

- [x] 1. Update design tokens in `_variables.css`
  - [x] 1.1 Update spacing scale to differentiated values (xs: 4px, sm: 8px, md: 12px, lg: 18px, xl: 24px, 2xl: 36px, 3xl: 48px)
    - Change `--space-xs` from 6px to 4px
    - Change `--space-sm` from 6px to 8px
    - _Requirements: 1.1_
  - [x] 1.2 Add missing typography and weight tokens
    - Add `--font-size-xs: 11px`
    - Add `--font-weight-medium: 500`
    - _Requirements: 1.2, 1.3, 1.6_
  - [x] 1.3 Update border-radius tokens to differentiated values
    - Change `--radius-sm` to 4px (was `var(--radius)`)
    - Keep `--radius-md` at 6px
    - Change `--radius-lg` to 10px (was `var(--radius)`)
    - _Requirements: 1.4_
  - [x] 1.4 Add breakpoint reference custom properties
    - Add `--breakpoint-sm: 600px`, `--breakpoint-md: 768px`, `--breakpoint-lg: 1024px` as documentation tokens
    - _Requirements: 1.5_
  - [ ]* 1.5 Write property test: design token uniqueness within groups
    - **Property 1: Design token uniqueness within groups**
    - Parse `_variables.css`, extract spacing and radius token groups, verify all values are unique within each group
    - **Validates: Requirements 1.1, 1.4**

- [x] 2. Create new shared CSS modules
  - [x] 2.1 Create `_layouts.css` with master-detail layout patterns
    - Extract `.md-layout`, `.md-list-panel`, `.md-detail-panel`, `.md-detail-header`, `.md-detail-title`, `.md-detail-sub`, `.md-filters-row`, `.md-step-content`, `.md-step-navigation`, `.form-row`, `.form-row-1/2/3` from crop-sprayers and clients component CSS
    - Add responsive collapse at 768px breakpoint
    - _Requirements: 2.1, 2.3, 2.5, 2.6, 2.7, 6.1_
  - [x] 2.2 Create `_cards.css` with entity card patterns
    - Extract `.entity-card`, `.card-main`, `.card-title`, `.card-badges-row`, `.card-info-row`, `.entity-list`, badge variant classes from crop-sprayers and clients component CSS
    - _Requirements: 2.2, 6.2_
  - [x] 2.3 Update `globals.css` to import new modules in correct order
    - Import order: variables, reset, layouts, cards, forms, buttons, components, utilities
    - _Requirements: 6.3_
  - [ ]* 2.4 Write property test: all CSS variable references resolve to defined tokens
    - **Property 3: All CSS variable references resolve to defined tokens**
    - Parse all CSS files, extract `var(--*)` references, verify each token is defined in `_variables.css`
    - **Validates: Requirements 1.6**

- [x] 3. Checkpoint - Verify shared modules
  - Ensure all tests pass, ask the user if questions arise.

- [x] 4. Refactor component CSS files to use shared patterns
  - [x] 4.1 Refactor `inspections.component.css`
    - Remove duplicated layout classes (`.inspections-layout`, `.inspections-list-panel`, `.inspections-detail-panel` — replace with shared `.md-layout`, `.md-list-panel`, `.md-detail-panel` or keep component class names but remove duplicated property declarations)
    - Remove duplicated card classes (`.card-main`, `.card-title`, `.card-badges-row`, `.card-info-row`)
    - Remove duplicated badge classes (`.protocol-card .badge`, `.badge-success`, `.badge-danger`, `.badge-secondary`)
    - Remove duplicated loading/spinner/empty-state styles
    - Remove duplicated dialog styles (`.dialog-backdrop`, `.dialog`, `.dialog-header`, `.dialog-body`, `.dialog-footer`)
    - Keep: dashboard view, accordion, check items, protocol document preview, section-specific styles
    - Standardize `.detail-title` from `font-size-xl` to `font-size-lg` to match reference components
    - Replace `--font-size-xs` and `--font-weight-medium` references with the now-defined tokens
    - Update breakpoint from 768px to 768px (already correct, just verify consistency)
    - _Requirements: 2.8, 3.3, 7.1_
  - [x] 4.2 Refactor `settings.component.css`
    - Remove duplicated form input styles that match `_forms.css`
    - Standardize admin section h4 from `font-size-xl` to `font-size-base` to match heading hierarchy
    - Replace hardcoded pixel values with design token variables where present
    - Keep: admin section, toggle form, user table, avatar section (unique to settings)
    - _Requirements: 2.10, 3.1, 7.2_
  - [x] 4.3 Refactor `notifications.component.css`
    - Align notification card styling with shared card patterns
    - Ensure consistent use of design tokens for spacing and typography
    - _Requirements: 2.9, 7.3_
  - [x] 4.4 Refactor `new-inspection.component.css`
    - Remove `!important` declarations on button and input styles where possible
    - Replace hardcoded pixel values with design token variables
    - Keep: protocol-specific styles, check rows, measurement inputs, PDF preview/print styles
    - _Requirements: 7.4, 7.5_
  - [x] 4.5 Refactor `generic-list.component.css`
    - Align `.component-title` styling with shared patterns
    - Remove duplicated `.data-table` styles that exist in `_components.css`
    - _Requirements: 3.2_
  - [ ]* 4.6 Write property test: no shared style duplication in component CSS
    - **Property 4: No shared style duplication in component CSS**
    - Parse shared modules to get list of defined class selectors, then scan component CSS files to verify none redefine those same selectors
    - **Validates: Requirements 2.8, 2.9, 2.10, 6.4, 6.5**
  - [ ]* 4.7 Write property test: no hardcoded font sizes in component CSS
    - **Property 6: No hardcoded font sizes in component CSS**
    - Parse all `*.component.css` files, find `font-size` declarations, verify they use `var(--font-size-*)` not hardcoded values. Exception: protocol document preview styles.
    - **Validates: Requirements 1.2, 3.4**

- [x] 5. Checkpoint - Verify component refactoring
  - Ensure all tests pass, ask the user if questions arise.

- [x] 6. Standardize breakpoints and mobile responsiveness
  - [x] 6.1 Update `_reset.css` with standardized breakpoints and safe-area support
    - Change mobile breakpoint from 767px to 768px (use `max-width: 767px` → keep as-is since 767px means "below 768px")
    - Add `prefers-color-scheme` media query for auto theme detection
    - Add safe-area-inset padding at 600px breakpoint
    - _Requirements: 4.1, 4.3, 5.1_
  - [x] 6.2 Update `styles.css` breakpoints to use standardized values
    - Change 900px breakpoint to 1024px or 768px as appropriate
    - Change 600px breakpoint references to be consistent
    - Ensure sidebar responsive behavior uses standardized breakpoints
    - _Requirements: 4.1, 4.6_
  - [x] 6.3 Update component CSS breakpoints to standardized values
    - `inspections.component.css`: verify 768px breakpoint
    - `new-inspection.component.css`: change 900px to 768px for protocol layout collapse, change 767px to 768px
    - `settings.component.css`: verify 767px breakpoints
    - `notifications.component.css`: verify 767px breakpoints
    - _Requirements: 4.1, 4.2, 4.7_
  - [x] 6.4 Improve mobile form layouts
    - Add responsive collapse for `.form-row-2` and `.form-row-3` at 768px in `_layouts.css`
    - Ensure touch targets meet 44px minimum on mobile via `_utilities.css`
    - _Requirements: 4.4, 4.5_
  - [ ]* 6.5 Write property test: standardized breakpoint usage
    - **Property 2: Standardized breakpoint usage**
    - Parse all CSS files, extract media query breakpoint values, verify they are in the allowed set (600, 768, 1024) or are print/contrast/motion queries
    - **Validates: Requirements 1.5, 4.1**
  - [ ]* 6.6 Write property test: master-detail responsive collapse
    - **Property 7: Master-detail responsive collapse**
    - Parse all CSS files that define two-column grid layouts, verify each has a corresponding 768px media query with single-column fallback
    - **Validates: Requirements 4.2**

- [x] 7. Improve accessibility
  - [x] 7.1 Add ARIA landmark roles to `app.component.html`
    - Add `role="navigation"` to sidebar element
    - Add `role="main"` to main content area
    - Add `role="complementary"` to status bar
    - _Requirements: 5.6_
  - [x] 7.2 Audit and fix form input labels across all component HTML templates
    - Scan all component HTML files for `<input>`, `<select>`, `<textarea>` without associated labels
    - Add `aria-label` attributes where visible labels are not practical
    - _Requirements: 5.4_
  - [x] 7.3 Verify and adjust color contrast for WCAG AA compliance
    - Check `--text-muted` contrast ratio against `--surface` and `--bg` in both themes
    - Check `--text-secondary` contrast ratio in both themes
    - Adjust HSL lightness values if any pair falls below 4.5:1
    - _Requirements: 5.2_
  - [x] 7.4 Verify focus indicators in both themes
    - Ensure `--focus-ring` color has 3:1 contrast against adjacent backgrounds in both themes
    - Verify all interactive elements have `:focus-visible` styles
    - _Requirements: 5.3_
  - [ ]* 7.5 Write property test: WCAG AA color contrast compliance
    - **Property 8: WCAG AA color contrast compliance**
    - Parse `_variables.css`, extract text/background color pairs from both themes, compute contrast ratios, verify all meet 4.5:1 minimum
    - **Validates: Requirements 5.2**
  - [ ]* 7.6 Write property test: form input label association
    - **Property 9: Form input label association**
    - Parse all `*.component.html` files, find `<input>`, `<select>`, `<textarea>` elements, verify each has an associated label (via `for`/`id`, wrapping `<label>`, `aria-label`, or `aria-labelledby`)
    - **Validates: Requirements 5.4**

- [x] 8. Clean up and final consistency pass
  - [x] 8.1 Remove `!important` overrides from component button styles
    - Scan `new-inspection.component.css` and other component CSS for `!important` on button/input properties
    - Replace with properly scoped selectors or remove if shared styles handle it
    - _Requirements: 7.5_
  - [x] 8.2 Clean up `_components.css`
    - Remove patterns that have been moved to `_layouts.css` and `_cards.css`
    - Ensure no duplicate class definitions between modules
    - _Requirements: 6.4, 6.5_
  - [ ]* 8.3 Write property test: no !important on button styles in component CSS
    - **Property 10: No !important overrides on button styles in component CSS**
    - Parse all `*.component.css` files, find declarations targeting button elements or `.btn*` classes, verify none use `!important` (exception: disabled pointer-events)
    - **Validates: Requirements 7.5**

- [x] 9. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate structural CSS invariants by parsing source files
- The protocol document preview styles in `new-inspection.component.css` and `inspections.component.css` are exempt from some token rules since they target print output with fixed dimensions
