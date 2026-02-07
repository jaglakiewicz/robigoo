# Requirements Document

## Introduction

This feature improves UI consistency, accessibility, and mobile responsiveness across the Robigoo Field Sprayer Control Station Angular application. The crop-sprayers and clients components serve as the reference standard. All other components (inspections, settings, notifications, new-inspection, inspection-marks, generic-list) will be aligned to match their patterns. The work includes consolidating duplicated CSS into shared modules, standardizing design tokens (spacing, typography, border-radius, breakpoints), improving mobile layouts for small screens (iPhone 16), and ensuring WCAG 2.1 AA compliance for both light and dark themes.

## Glossary

- **Design_Token_System**: The set of CSS custom properties defined in `_variables.css` that control typography, spacing, border-radius, colors, and transitions across the application
- **Reference_Components**: The crop-sprayers and clients components, which represent the target visual and structural standard for all other components
- **Master_Detail_Layout**: A two-column grid layout pattern with a list panel on the left and a detail panel on the right, collapsing to single column on mobile
- **Shared_Style_Modules**: The CSS files in `Client/src/app/shared/styles/` that provide reusable styles across all components
- **Component_CSS**: Individual CSS files scoped to specific Angular components (e.g., `inspections.component.css`)
- **WCAG_AA**: Web Content Accessibility Guidelines 2.1 Level AA compliance standard
- **Breakpoint_System**: The set of media query breakpoints used for responsive layout changes
- **Touch_Target**: An interactive element sized at minimum 44x44px per WCAG 2.5.5

## Requirements

### Requirement 1: Standardize Design Token System

**User Story:** As a developer, I want a consistent and complete set of design tokens, so that all components use the same spacing, typography, border-radius, and color values without ambiguity.

#### Acceptance Criteria

1. THE Design_Token_System SHALL define a differentiated spacing scale where each named token (xs, sm, md, lg, xl, 2xl, 3xl) maps to a unique pixel value
2. THE Design_Token_System SHALL define exactly four primary font sizes (sm, base, lg, xl) plus one display size (4xl), and all component text SHALL reference only these tokens
3. THE Design_Token_System SHALL define a font-weight-medium token (500) in addition to the existing regular (400) and bold (700) weights
4. THE Design_Token_System SHALL define differentiated border-radius tokens where radius-sm, radius-md, and radius-lg map to distinct values
5. THE Design_Token_System SHALL define a standardized set of responsive breakpoints (sm, md, lg) that all components reference consistently
6. WHEN a component references a design token that does not exist (e.g., --font-size-xs, --font-weight-medium), THE Design_Token_System SHALL provide that token with a defined value

### Requirement 2: Eliminate CSS Duplication Across Components

**User Story:** As a developer, I want shared layout and card patterns extracted into reusable CSS modules, so that I can maintain styles in one place and reduce file sizes.

#### Acceptance Criteria

1. THE Shared_Style_Modules SHALL provide a master-detail layout class that implements the two-column grid pattern used by Reference_Components (minmax(220px, 1fr) / minmax(0, 2.5fr) with var(--space-lg) gap)
2. THE Shared_Style_Modules SHALL provide shared card classes (card-main, card-title, card-badges-row, card-info-row, badge variants) that match the Reference_Components card pattern
3. THE Shared_Style_Modules SHALL provide shared list-panel and detail-panel classes with the panel background, border, padding, and flex layout matching Reference_Components
4. THE Shared_Style_Modules SHALL provide shared loading-state and empty-state classes including the spinner animation
5. THE Shared_Style_Modules SHALL provide shared detail-header, detail-title, and detail-sub classes matching Reference_Components
6. THE Shared_Style_Modules SHALL provide shared step-navigation and step-content classes matching Reference_Components
7. THE Shared_Style_Modules SHALL provide shared filters-row class matching Reference_Components
8. WHEN the inspections Component_CSS is refactored, THE inspections Component_CSS SHALL remove all duplicated layout, card, badge, loading-state, and dialog styles that exist in Shared_Style_Modules
9. WHEN the notifications Component_CSS is refactored, THE notifications Component_CSS SHALL use shared card and layout patterns from Shared_Style_Modules
10. WHEN the settings Component_CSS is refactored, THE settings Component_CSS SHALL use shared form and layout patterns from Shared_Style_Modules

### Requirement 3: Standardize Heading and Typography Hierarchy

**User Story:** As a user, I want consistent heading sizes and text styles across all screens, so that the application feels cohesive and professional.

#### Acceptance Criteria

1. THE Application SHALL use a consistent heading hierarchy where h1 uses font-size-xl, h2 uses font-size-xl, h3 uses font-size-lg, and h4 uses font-size-base across all components
2. WHEN a component displays a panel title, THE component SHALL use the shared panel-title class with font-size-lg and font-weight-bold
3. WHEN a component displays a detail title, THE component SHALL use the shared detail-title class with font-size-lg and font-weight-bold consistently (not font-size-xl in some components and font-size-lg in others)
4. THE Application SHALL not use hardcoded pixel values for font sizes in any Component_CSS; all font sizes SHALL reference Design_Token_System variables

### Requirement 4: Improve Mobile Responsiveness

**User Story:** As a user on a smartphone (iPhone 16), I want to view and interact with all fields and controls comfortably, so that I can use the application on a small screen.

#### Acceptance Criteria

1. THE Breakpoint_System SHALL use three consistent breakpoints: 600px (sm), 768px (md), and 1024px (lg) across all components
2. WHEN the viewport width is below the md breakpoint, THE Master_Detail_Layout SHALL collapse to a single-column layout in all components that use it
3. WHEN the viewport width is below the sm breakpoint, THE Application SHALL apply safe-area-inset padding for notched devices
4. WHEN form grids (form-row-2, form-row-3) are displayed below the md breakpoint, THE forms SHALL stack to single-column layout
5. WHEN the viewport width is below the md breakpoint, THE Application SHALL increase touch targets to a minimum of 44px for all interactive elements
6. WHEN the viewport width is below the sm breakpoint, THE sidebar SHALL expand to full viewport width and the main content SHALL be hidden behind it
7. WHEN the new-inspection protocol layout is displayed below the md breakpoint, THE protocol navigation SHALL collapse to a compact horizontal strip showing only section numbers

### Requirement 5: Improve Accessibility for Light and Dark Themes

**User Story:** As a user with visual accessibility needs, I want the application to meet WCAG 2.1 AA standards in both light and dark modes, so that I can use the application comfortably regardless of theme.

#### Acceptance Criteria

1. THE Application SHALL detect the user's system color scheme preference using prefers-color-scheme media query and apply the matching theme on first load when no user preference is stored
2. WHEN text is displayed on any background, THE color combination SHALL meet WCAG 2.1 AA minimum contrast ratio of 4.5:1 for normal text and 3:1 for large text in both light and dark themes
3. WHEN an interactive element receives keyboard focus, THE element SHALL display a visible focus indicator with at least 3:1 contrast ratio against adjacent colors
4. THE Application SHALL ensure all form inputs have associated visible labels or aria-label attributes
5. WHEN a dialog or modal is opened, THE Application SHALL trap keyboard focus within the dialog and return focus to the triggering element on close
6. THE Application SHALL provide appropriate ARIA landmark roles (main, navigation, complementary) on major layout sections

### Requirement 6: Organize CSS Architecture

**User Story:** As a developer, I want a well-organized CSS architecture with clear module boundaries, so that I can find and maintain styles efficiently.

#### Acceptance Criteria

1. THE Shared_Style_Modules SHALL include a new `_layouts.css` module containing master-detail layout, list-panel, detail-panel, and grid layout patterns extracted from component files
2. THE Shared_Style_Modules SHALL include a new `_cards.css` module containing card, card-main, card-title, card-badges-row, card-info-row, and entity card patterns extracted from component files
3. THE globals.css entry point SHALL import all shared style modules in dependency order (variables, reset, layouts, cards, forms, buttons, components, utilities)
4. WHEN a Component_CSS file is refactored, THE Component_CSS SHALL only contain styles unique to that component and SHALL import globals.css for all shared patterns
5. THE Component_CSS files SHALL not redefine styles that exist identically in Shared_Style_Modules (no duplicated card, layout, badge, loading, or dialog styles)

### Requirement 7: Standardize Component Visual Alignment

**User Story:** As a user, I want all application screens to have a consistent look and feel matching the crop-sprayers and clients screens, so that the application feels polished and modern.

#### Acceptance Criteria

1. WHEN the inspections component displays its list and detail panels, THE inspections component SHALL use the same visual pattern (panel background, border, padding, card structure, badge styling) as the Reference_Components
2. WHEN the settings component displays its content, THE settings component SHALL use consistent heading sizes, form styling, and spacing that align with the Reference_Components patterns
3. WHEN the notifications component displays notification cards, THE notifications component SHALL use card styling consistent with the Reference_Components card pattern
4. WHEN the new-inspection component displays forms and protocol sections, THE new-inspection component SHALL use form grid patterns and section styling consistent with the Reference_Components
5. THE Application SHALL use consistent button styling across all components without component-specific button overrides or !important declarations
