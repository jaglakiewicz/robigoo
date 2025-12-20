# SVG Icons Resource Guide

## Overview
All SVG icons used in the Robigoo application are centralized in the `svg-icons.ts` file located in `src/app/shared/`.

## Structure
The file exports two objects:
- **SVG_ICONS**: Contains complete SVG elements ready to use (includes viewBox and size attributes)
- **SVG_PATHS**: Contains just the path data for reference

## Usage

### In TypeScript Components

1. Import the SVG_ICONS object:
```typescript
import { SVG_ICONS } from './shared/svg-icons';
```

2. Add it to your component class to make it available in the template:
```typescript
export class MyComponent {
  SVG_ICONS = SVG_ICONS;
  // ... rest of component
}
```

3. Use in your template with sanitized HTML binding:
```typescript
getSafeHtml(html: string): SafeHtml {
  return this.sanitizer.bypassSecurityTrustHtml(html);
}
```

### In Templates

Display the SVG using innerHTML binding with the getSafeHtml method:

```html
<!-- Single icon -->
<span [innerHTML]="getSafeHtml(SVG_ICONS.deleteIcon)"></span>

<!-- Icon with text in button -->
<button>
  <span [innerHTML]="getSafeHtml(SVG_ICONS.deleteIcon)"></span>
  Delete
</button>

<!-- Using with ngIf -->
<span *ngIf="condition" [innerHTML]="getSafeHtml(SVG_ICONS.chevronDown)"></span>
```

## Available Icons

### Menu Icons (18x18)
- `new` - New inspection icon
- `inspections` - Inspections icon
- `clients` - Clients icon
- `marks` - Marks/Inspection marks icon
- `notifications` - Notifications icon
- `types` - Types of vehicles icon
- `stats` - Statistics icon
- `settings` - Settings icon

### UI/Form Icons (16x16 or 24x24)
- `chevronDown` - Chevron down arrow
- `chevronUp` - Chevron up arrow (toggle icon)
- `pinIcon` - Pin/Unpin tab icon
- `closeIcon` - Close/Remove icon
- `deleteIcon` - Delete icon (trash can)
- `userDefaultAvatar` - Default user avatar icon

## Integration Points

The following components use SVG_ICONS:
- **app.component.ts** - Menu and tab icons
- **settings.component.ts** - Settings form icons (avatar, delete, toggle)

## Adding New Icons

1. Add the new SVG to the `SVG_ICONS` object in `svg-icons.ts`
2. Optionally add just the path data to `SVG_PATHS` for reference
3. Import and use in your component following the usage guidelines above

## Benefits

✅ Centralized icon management
✅ Easy to update icon designs globally
✅ Reduces code duplication
✅ Consistent icon styling across the app
✅ Better maintainability
✅ Easy to add new icons in one place
