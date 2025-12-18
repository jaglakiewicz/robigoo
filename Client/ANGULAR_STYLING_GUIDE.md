# 🏗️ Angular Components - Styling Best Practices

## Stosowanie Design System w Angular Komponentach

### 1. Responsive Directive (Rekomendacja)

Dodaj do `shared/directives/responsive.directive.ts`:

```typescript
import { Directive, ElementRef, OnInit } from '@angular/core';

@Directive({
  selector: '[appResponsive]'
})
export class ResponsiveDirective implements OnInit {
  constructor(private el: ElementRef) {}

  ngOnInit() {
    // Automatically adds responsive classes
    this.el.nativeElement.classList.add('responsive-container');
  }
}
```

### 2. Component Template Best Practices

```html
<!-- ✅ GOOD - Classes for layout -->
<div class="flex flex-col gap-md">
  <h2 class="text-lg text-bold">Title</h2>
  <div class="grid grid-2">
    <!-- Content -->
  </div>
</div>

<!-- ❌ BAD - Inline styles -->
<div style="display: flex; gap: 12px;">
  <h2 style="font-size: 16px; font-weight: 600;">Title</h2>
</div>
```

### 3. Component CSS Template

```css
/* my-component.component.css */
:host {
  display: block;
}

.container {
  padding: var(--space-lg);
}

/* Mobile */
@media (max-width: 767px) {
  .container {
    padding: var(--space-md);
  }
}

/* Responsive grid */
.grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: var(--space-lg);
}

@media (max-width: 1023px) {
  .grid {
    grid-template-columns: repeat(2, 1fr);
    gap: var(--space-md);
  }
}

@media (max-width: 767px) {
  .grid {
    grid-template-columns: 1fr;
    gap: var(--space-md);
  }
}
```

### 4. Common Component Patterns

#### Card Component
```html
<div class="card">
  <div class="card-header">
    <h3 class="card-title">{{ title }}</h3>
    <button class="btn-ghost">⋯</button>
  </div>
  <div class="card-body">
    <!-- Content -->
  </div>
</div>
```

```css
.card {
  background: var(--panel);
  border: 1px solid var(--panel-border);
  border-radius: var(--radius-lg);
  padding: var(--space-lg);
  box-shadow: var(--shadow-sm);
  transition: all var(--transition-fast);
}

.card:hover {
  box-shadow: var(--shadow-md);
}

@media (max-width: 767px) {
  .card {
    padding: var(--space-md);
  }
}
```

#### Form Group
```html
<div class="form-group">
  <label for="email">Email</label>
  <input 
    id="email" 
    type="email" 
    placeholder="example@domain.com"
    [(ngModel)]="email"
  >
  <span *ngIf="emailError" class="error-message">
    {{ emailError }}
  </span>
</div>
```

```css
.form-group {
  display: flex;
  flex-direction: column;
  gap: var(--space-sm);
  margin-bottom: var(--space-lg);
}

.form-group label {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--text);
}

.form-group input {
  padding: var(--space-sm) var(--space-md);
  border: 1px solid var(--panel-border);
  border-radius: var(--radius-md);
  background: var(--bg);
  color: var(--text);
  font-size: 16px;
  min-height: 44px;
  transition: all var(--transition-fast);
}

.form-group input:focus {
  outline: none;
  border-color: var(--accent);
  box-shadow: 0 0 0 3px rgba(59, 142, 165, 0.1);
}

.error-message {
  color: var(--error);
  font-size: var(--font-size-sm);
}
```

#### Responsive List
```html
<div class="list-container">
  <div class="list-header">
    <h3 class="text-lg">Items</h3>
    <button class="btn-primary">Add Item</button>
  </div>
  
  <div class="list-items">
    <div class="list-item" *ngFor="let item of items">
      <div class="list-item-content">
        <h4>{{ item.name }}</h4>
        <p>{{ item.description }}</p>
      </div>
      <div class="list-item-actions">
        <button class="btn-secondary">Edit</button>
        <button class="btn-danger">Delete</button>
      </div>
    </div>
  </div>
</div>
```

```css
.list-container {
  background: var(--panel);
  border-radius: var(--radius-lg);
  padding: var(--space-lg);
}

.list-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--space-lg);
  gap: var(--space-md);
  flex-wrap: wrap;
}

.list-items {
  display: flex;
  flex-direction: column;
  gap: var(--space-md);
}

.list-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--space-md);
  background: var(--bg);
  border-radius: var(--radius-md);
  border: 1px solid var(--panel-border);
  gap: var(--space-lg);
  transition: all var(--transition-fast);
}

.list-item:hover {
  box-shadow: var(--shadow-sm);
}

.list-item-content {
  flex: 1;
}

.list-item-content h4 {
  margin: 0 0 var(--space-xs) 0;
  color: var(--text);
}

.list-item-content p {
  margin: 0;
  color: var(--text-light);
  font-size: var(--font-size-sm);
}

.list-item-actions {
  display: flex;
  gap: var(--space-sm);
  flex-shrink: 0;
}

/* Mobile responsive */
@media (max-width: 767px) {
  .list-container {
    padding: var(--space-md);
  }

  .list-header {
    flex-direction: column;
    align-items: stretch;
  }

  .list-header button {
    width: 100%;
  }

  .list-item {
    flex-direction: column;
    align-items: stretch;
  }

  .list-item-actions {
    width: 100%;
  }

  .list-item-actions button {
    flex: 1;
  }
}
```

### 5. Using ngClass for Responsive

```html
<div [ngClass]="{
  'list-container': true,
  'list-compact': isCompactMode,
  'list-expanded': !isCompactMode
}">
  <!-- Content -->
</div>
```

### 6. ViewEncapsulation & Style Scope

```typescript
import { Component, ViewEncapsulation } from '@angular/core';

@Component({
  selector: 'app-my-component',
  templateUrl: './my-component.component.html',
  styleUrls: ['./my-component.component.css'],
  encapsulation: ViewEncapsulation.ShadowDom // Optional
})
export class MyComponent {
  // Component logic
}
```

### 7. Responsive Breakpoint Service

Utwórz plik `shared/services/breakpoint.service.ts`:

```typescript
import { Injectable, signal } from '@angular/core';
import { fromEvent } from 'rxjs';
import { debounceTime } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class BreakpointService {
  screenSize = signal<'mobile' | 'tablet' | 'desktop'>('desktop');

  constructor() {
    this.updateScreenSize();
    fromEvent(window, 'resize')
      .pipe(debounceTime(200))
      .subscribe(() => this.updateScreenSize());
  }

  private updateScreenSize() {
    const width = window.innerWidth;
    if (width < 768) {
      this.screenSize.set('mobile');
    } else if (width < 1024) {
      this.screenSize.set('tablet');
    } else {
      this.screenSize.set('desktop');
    }
  }

  isMobile() {
    return this.screenSize() === 'mobile';
  }

  isTablet() {
    return this.screenSize() === 'tablet';
  }

  isDesktop() {
    return this.screenSize() === 'desktop';
  }
}
```

Usage w komponencie:

```typescript
export class MyComponent {
  constructor(public breakpoint: BreakpointService) {}
}
```

```html
<div *ngIf="breakpoint.isDesktop()" class="desktop-only">
  Desktop layout
</div>

<div *ngIf="breakpoint.isMobile()" class="mobile-only">
  Mobile layout
</div>
```

### 8. Accessibility + Responsive

```html
<button 
  [attr.aria-label]="'Delete item: ' + item.name"
  class="btn-danger"
  (click)="deleteItem(item)">
  Delete
</button>
```

```css
/* Focus-visible for keyboard navigation */
button:focus-visible {
  outline: 2px solid var(--accent);
  outline-offset: 2px;
}

/* Reduce motion for users who prefer it */
@media (prefers-reduced-motion: reduce) {
  * {
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.01ms !important;
  }
}
```

### 9. Dark Mode Support

```typescript
export class MyComponent implements OnInit {
  @HostBinding('class') get themeClass() {
    return this.isDarkMode ? 'dark-theme' : 'light-theme';
  }

  isDarkMode = false;

  constructor() {
    this.isDarkMode = this.detectSystemPreference();
  }

  private detectSystemPreference() {
    return window.matchMedia('(prefers-color-scheme: dark)').matches;
  }
}
```

---

## 📋 Checklist dla Nowego Komponentu

- [ ] Komponuje używa `var()` dla wszystkich kolorów i spacing
- [ ] CSS ma mobile-first approach z `@media (max-width: 767px)`
- [ ] Buttons i inputs mają min-height 44px
- [ ] Responsive grids stają się 1-column na mobile
- [ ] Font sizes są z design system scale
- [ ] Border-radius to zawsze `var(--radius-md/lg/sm)`
- [ ] Testowałem na iPhone (390px), iPad (768px), Desktop (1024px+)
- [ ] Accessibility - ARIA labels, semantic HTML
- [ ] Keyboard navigation - `:focus-visible` styles
- [ ] Performance - no inline styles, use CSS classes

---

## 🔗 Linkowanie stylów

### ❌ Don't

```html
<!-- External styles in template -->
<div style="color: red; font-size: 16px;">Text</div>

<!-- Hardcoded values -->
<div style="margin: 12px; padding: 8px;">Content</div>
```

### ✅ Do

```html
<!-- Use CSS classes -->
<div class="error-text text-base">Text</div>
<div class="p-md">Content</div>
```

```css
.error-text {
  color: var(--error);
}

.text-base {
  font-size: var(--font-size-base);
}

.p-md {
  padding: var(--space-md);
}
```

---

**Last updated**: 2025-12-16
**Part of**: Robigoo Styling System
**Status**: ✅ Production Ready
