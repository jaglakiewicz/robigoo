# 🎨 CSS Quick Reference - Copy & Paste

## Szybkie Snippets do Użytku

### Responsive Container
```css
.container {
  padding: var(--space-lg);
}

@media (max-width: 767px) {
  .container {
    padding: var(--space-md);
  }
}
```

### Responsive Grid 2-Column
```css
.grid-2 {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: var(--space-lg);
}

@media (max-width: 1023px) {
  .grid-2 {
    grid-template-columns: 1fr;
    gap: var(--space-md);
  }
}
```

### Responsive Grid 3-Column
```css
.grid-3 {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: var(--space-lg);
}

@media (max-width: 1023px) {
  .grid-3 {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (max-width: 767px) {
  .grid-3 {
    grid-template-columns: 1fr;
  }
}
```

### Responsive Flex
```css
.flex-responsive {
  display: flex;
  gap: var(--space-lg);
}

@media (max-width: 767px) {
  .flex-responsive {
    flex-direction: column;
    gap: var(--space-md);
  }
}
```

### Card Component
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
    border-radius: var(--radius-md);
  }
}
```

### Input / Form Field
```css
input, textarea, select {
  padding: var(--space-sm) var(--space-md);
  border: 1px solid var(--panel-border);
  border-radius: var(--radius-md);
  background: var(--bg);
  color: var(--text);
  font-size: 16px;
  font-family: var(--font-family);
  min-height: 44px;
  transition: all var(--transition-fast);
}

input:focus, textarea:focus, select:focus {
  outline: none;
  border-color: var(--accent);
  box-shadow: 0 0 0 3px rgba(59, 142, 165, 0.1);
}
```

### Button - Primary
```css
.btn-primary {
  padding: var(--space-sm) var(--space-lg);
  background: var(--accent);
  color: white;
  border: none;
  border-radius: var(--radius-md);
  font-size: var(--font-size-base);
  font-weight: var(--font-weight-medium);
  cursor: pointer;
  min-height: 44px;
  transition: all var(--transition-fast);
  display: inline-flex;
  align-items: center;
  gap: var(--space-sm);
}

.btn-primary:hover {
  background: var(--accent-light);
  transform: translateY(-1px);
  box-shadow: var(--shadow-md);
}

@media (max-width: 767px) {
  .btn-primary {
    width: 100%;
    justify-content: center;
    min-height: 48px;
  }
}
```

### Button - Secondary
```css
.btn-secondary {
  padding: var(--space-sm) var(--space-lg);
  background: var(--bg-alt);
  color: var(--text);
  border: 1px solid var(--panel-border);
  border-radius: var(--radius-md);
  font-weight: var(--font-weight-medium);
  cursor: pointer;
  min-height: 44px;
  transition: all var(--transition-fast);
}

.btn-secondary:hover {
  background: var(--panel-border);
}
```

### Button - Danger
```css
.btn-danger {
  padding: var(--space-sm) var(--space-lg);
  background: var(--error);
  color: white;
  border: none;
  border-radius: var(--radius-md);
  font-weight: var(--font-weight-medium);
  cursor: pointer;
  min-height: 44px;
  transition: all var(--transition-fast);
}

.btn-danger:hover {
  background: #e53935;
  transform: translateY(-1px);
  box-shadow: var(--shadow-md);
}
```

### Table - Responsive
```css
.data-table {
  width: 100%;
  border-collapse: collapse;
  background: var(--panel);
  border: 1px solid var(--panel-border);
  border-radius: var(--radius-md);
  overflow: hidden;
  box-shadow: var(--shadow-sm);
}

.data-table thead {
  background: var(--bg-alt);
  border-bottom: 2px solid var(--panel-border);
}

.data-table th {
  padding: var(--space-md);
  text-align: left;
  font-weight: var(--font-weight-semibold);
  color: var(--text);
  font-size: var(--font-size-sm);
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.data-table td {
  padding: var(--space-md);
  border-bottom: 1px solid var(--panel-border);
  color: var(--text);
}

.data-table tbody tr:hover {
  background: var(--bg-alt);
}

/* Mobile - Stack table */
@media (max-width: 767px) {
  .data-table, .data-table thead, .data-table tbody, 
  .data-table th, .data-table td, .data-table tr {
    display: block;
    width: 100%;
  }

  .data-table thead {
    display: none;
  }

  .data-table tr {
    margin-bottom: var(--space-lg);
    border: 1px solid var(--panel-border);
    border-radius: var(--radius-md);
    padding: var(--space-md);
    background: var(--panel);
  }

  .data-table td {
    border: none;
    display: flex;
    justify-content: space-between;
    padding: var(--space-sm) 0;
    border-bottom: 1px solid var(--panel-border);
  }

  .data-table td:before {
    content: attr(data-label);
    font-weight: var(--font-weight-semibold);
    text-transform: uppercase;
    font-size: var(--font-size-xs);
    color: var(--text-light);
    margin-right: var(--space-md);
    flex-shrink: 0;
  }

  .data-table td:last-child {
    border-bottom: none;
  }
}
```

### List Item
```css
.list-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--space-md);
  background: var(--bg);
  border: 1px solid var(--panel-border);
  border-radius: var(--radius-md);
  gap: var(--space-lg);
  transition: all var(--transition-fast);
}

.list-item:hover {
  background: var(--bg-alt);
  box-shadow: var(--shadow-sm);
}

.list-item-content {
  flex: 1;
}

.list-item-actions {
  display: flex;
  gap: var(--space-sm);
  flex-shrink: 0;
}

@media (max-width: 767px) {
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

### Modal / Dialog
```css
.modal {
  display: none;
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: var(--space-lg);
  z-index: 9000;
}

.modal.open {
  display: flex;
}

.modal-content {
  background: var(--panel);
  border-radius: var(--radius-lg);
  padding: var(--space-2xl);
  max-width: 500px;
  width: 100%;
  box-shadow: var(--shadow-xl);
  max-height: 90vh;
  overflow-y: auto;
}

.modal-header {
  margin-bottom: var(--space-lg);
  border-bottom: 1px solid var(--panel-border);
  padding-bottom: var(--space-lg);
}

.modal-title {
  font-size: var(--font-size-xl);
  font-weight: var(--font-weight-semibold);
  color: var(--text);
}

.modal-body {
  margin-bottom: var(--space-lg);
}

.modal-footer {
  display: flex;
  gap: var(--space-md);
  justify-content: flex-end;
  border-top: 1px solid var(--panel-border);
  padding-top: var(--space-lg);
}

@media (max-width: 767px) {
  .modal-content {
    padding: var(--space-xl) var(--space-lg);
  }

  .modal-footer {
    flex-direction: column;
  }

  .modal-footer button {
    width: 100%;
  }
}
```

### Alert / Notification
```css
.alert {
  padding: var(--space-md);
  border-radius: var(--radius-md);
  border-left: 4px solid;
  margin-bottom: var(--space-md);
  font-size: var(--font-size-sm);
}

.alert-info {
  background: rgba(33, 150, 243, 0.1);
  border-color: var(--info);
  color: var(--info);
}

.alert-success {
  background: rgba(76, 175, 80, 0.1);
  border-color: var(--success);
  color: var(--success);
}

.alert-warning {
  background: rgba(255, 152, 0, 0.1);
  border-color: var(--warning);
  color: var(--warning);
}

.alert-error {
  background: rgba(244, 67, 54, 0.1);
  border-color: var(--error);
  color: var(--error);
}

.alert-close {
  background: none;
  border: none;
  color: inherit;
  cursor: pointer;
  font-size: var(--font-size-lg);
  opacity: 0.7;
  transition: opacity var(--transition-fast);
  padding: 0;
}

.alert-close:hover {
  opacity: 1;
}
```

### Badge / Tag
```css
.badge {
  display: inline-flex;
  align-items: center;
  gap: var(--space-xs);
  padding: var(--space-xs) var(--space-sm);
  border-radius: 999px;
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-medium);
  background: var(--bg-alt);
  color: var(--text-light);
  white-space: nowrap;
}

.badge-primary {
  background: rgba(59, 142, 165, 0.15);
  color: var(--accent);
}

.badge-success {
  background: rgba(76, 175, 80, 0.15);
  color: var(--success);
}

.badge-error {
  background: rgba(244, 67, 54, 0.15);
  color: var(--error);
}
```

### Skeleton Loader
```css
@keyframes skeleton-loading {
  0% {
    background-position: -1000px 0;
  }
  100% {
    background-position: 1000px 0;
  }
}

.skeleton {
  background: linear-gradient(
    90deg,
    var(--bg-alt) 25%,
    var(--panel-border) 50%,
    var(--bg-alt) 75%
  );
  background-size: 1000px 100%;
  animation: skeleton-loading 2s infinite;
  border-radius: var(--radius-md);
}

.skeleton-text {
  height: var(--space-md);
  margin-bottom: var(--space-md);
}

.skeleton-avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;
}

.skeleton-line {
  height: var(--space-sm);
  margin-bottom: var(--space-xs);
}
```

### Breadcrumb
```css
.breadcrumb {
  display: flex;
  gap: var(--space-sm);
  flex-wrap: wrap;
  padding: var(--space-md);
  font-size: var(--font-size-sm);
}

.breadcrumb-item {
  display: flex;
  align-items: center;
  gap: var(--space-sm);
}

.breadcrumb-item a {
  color: var(--accent);
  text-decoration: none;
  transition: color var(--transition-fast);
}

.breadcrumb-item a:hover {
  color: var(--accent-light);
  text-decoration: underline;
}

.breadcrumb-separator {
  color: var(--text-light);
}

.breadcrumb-item.active {
  color: var(--text);
  font-weight: var(--font-weight-medium);
}
```

### Tabs
```css
.tabs {
  display: flex;
  border-bottom: 2px solid var(--panel-border);
  overflow-x: auto;
  gap: 0;
}

.tab {
  padding: var(--space-md) var(--space-lg);
  background: none;
  border: none;
  cursor: pointer;
  color: var(--text-light);
  font-size: var(--font-size-base);
  font-weight: var(--font-weight-medium);
  position: relative;
  white-space: nowrap;
  transition: color var(--transition-fast);
  border-bottom: 3px solid transparent;
  margin-bottom: -2px;
}

.tab:hover {
  color: var(--text);
}

.tab.active {
  color: var(--accent);
  border-bottom-color: var(--accent);
}

@media (max-width: 767px) {
  .tabs {
    gap: var(--space-xs);
  }

  .tab {
    padding: var(--space-md) var(--space-md);
    font-size: var(--font-size-sm);
  }
}
```

---

## Design Tokens - Quick Copy

### Colors
```css
--primary: #0b4f6c
--accent: #3b8ea5
--success: #4caf50
--warning: #ff9800
--error: #f44336
--info: #2196f3

--bg: #f5f5f5
--panel: #ffffff
--text: #1a1a1a
--text-light: #555555
```

### Spacing
```css
--space-xs:   4px
--space-sm:   8px
--space-md:   12px
--space-lg:   16px
--space-xl:   24px
--space-2xl:  32px
```

### Borders
```css
--radius-sm:  4px
--radius-md:  6px
--radius-lg:  8px
```

### Fonts
```css
--font-size-xs:    11px (mobile) / 12px (tablet+)
--font-size-sm:    12px (mobile) / 13px (tablet+)
--font-size-base:  13px (mobile) / 14px (tablet+)
--font-size-lg:    15px (mobile) / 16px (tablet+)
--font-size-xl:    17px (mobile) / 18px (tablet+)
--font-size-2xl:   22px (mobile) / 24px (tablet+)
```

### Shadows
```css
--shadow-sm:  0 1px 2px rgba(0, 0, 0, 0.05)
--shadow-md:  0 4px 6px rgba(0, 0, 0, 0.1)
--shadow-lg:  0 10px 15px rgba(0, 0, 0, 0.1)
--shadow-xl:  0 20px 25px rgba(0, 0, 0, 0.15)
```

### Transitions
```css
--transition-fast:  0.15s ease
--transition-base:  0.25s ease
--transition-slow:  0.35s ease
```

---

**Pro Tips**:
1. **Always use `var()`** - Never hardcode values
2. **Mobile-first** - Start with mobile styles, add complexity with `@media`
3. **Touch targets** - Never go below 44px height for clickable elements
4. **Font size 16px** - Always use in inputs on mobile to prevent auto-zoom
5. **Test all breakpoints** - 375px (mobile), 768px (tablet), 1024px (desktop+)

Last updated: 2025-12-16
