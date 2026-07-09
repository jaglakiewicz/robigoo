# Settings Component Refactoring Plan

## Current Issues
1. Custom table styles instead of reusable components
2. Inconsistent button colors (especially in session management)
3. Mixed styling approaches
4. Not using available shared components

## Available Reusable Components
- `app-entity-toolbar` ✅ Already used
- `app-entity-list-panel` - For users list
- `app-entity-detail-panel` - For detail views
- `generic-list` - For tables (sessions, activity logs)
- `custom-select` - For dropdowns
- `date-picker` - For date inputs
- `filter-panel` - For filtering

## Refactoring Tasks

### 1. User Management Section
**Current**: Custom table with inline styles
**Refactor to**: Use `app-entity-list-panel` or `generic-list`
- Consistent table styling
- Built-in actions support
- Responsive design

### 2. Session Management Section
**Current**: Custom `data-table` class with custom button colors
**Issues**: 
- `btn-warning` class not following design system
- Custom table styles
**Refactor to**:
- Use `generic-list` component
- Use standard button classes: `btn-danger` for terminate actions
- Remove custom `data-table` styles

### 3. Activity Logs Section  
**Current**: Custom table
**Refactor to**: Use `generic-list` with proper column configuration

### 4. Button Color Standardization
**Current button classes to fix**:
- `btn-warning` → `btn-secondary` or `btn-danger` (depending on action)
- Terminate session → `btn-danger`
- Terminate all → `btn-danger`
- Refresh → `btn-secondary`

### 5. Form Inputs
**Current**: Mix of native inputs and custom styling
**Keep**: Native inputs work well, just ensure consistent styling

## Implementation Priority

### Phase 1: Button Colors (Quick Win)
- Fix session management button colors
- Standardize all button usage
- Remove custom button color classes

### Phase 2: Tables to Components
- Replace users table with reusable component
- Replace sessions table with `generic-list`
- Replace activity logs table with `generic-list`

### Phase 3: CSS Cleanup
- Remove unused custom table styles
- Consolidate form styling
- Use CSS variables consistently

## Design System Colors

### Button Classes (from globals.css)
- `btn-primary` - Main actions (blue)
- `btn-secondary` - Secondary actions (gray)
- `btn-danger` - Destructive actions (red)
- `btn-success` - Positive actions (green)

### Session Management Buttons Should Be:
- "Odśwież" → `btn-secondary`
- "Wyrzuć" (single) → `btn-danger`  
- "Wyrzuć wszystkie" → `btn-danger`

## Next Steps
1. Start with button color fixes (minimal risk)
2. Test each section after refactoring
3. Ensure all functionality preserved
4. Remove unused CSS after component migration
