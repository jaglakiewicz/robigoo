# Architektura Generyczna - Refaktoring

## Przegląd

Repozytorium zostało refaktoryzowane w celu eliminacji duplikacji kodu i zwiększenia wielokrotnego użytku komponentów. Wszystkie listy danych teraz korzystają z generycznych komponentów i serwisów.

## Struktura Katalogów

```
src/app/
├── shared/                          # Wspólne komponenty i serwisy
│   ├── services/
│   │   └── generic-crud.service.ts  # Serwis bazowy dla CRUD operacji
│   ├── models/
│   │   └── filter.model.ts          # Definicje interfejsów filtrów i kolumn
│   └── components/
│       └── generic-list/            # Generyczny komponent listy
│           ├── generic-list.component.ts
│           ├── generic-list.component.html
│           └── generic-list.component.css
├── inspections/                     # Komponenty domenowe
├── clients/
└── ...
```

## Komponenty Generyczne

### GenericCrudService<T>

Serwis bazowy dla wszystkich operacji CRUD:

```typescript
// Obsługuje:
getAll(endpoint)           // Pobierz wszystkie
getById(endpoint, id)      // Pobierz jeden
create(endpoint, data)     // Utwórz
update(endpoint, id, data) // Zaktualizuj
delete(endpoint, id)       // Usuń
search(endpoint, params)   // Szukaj z parametrami
getPaginated(...)          // Stronicowanie
```

**Jak używać w serwisie domenowym:**

```typescript
@Injectable({ providedIn: 'root' })
export class MyService extends GenericCrudService<MyModel> {
  readonly endpoint = 'my-endpoint';

  create(dto: any) {
    return super.create(this.endpoint, dto);
  }
}
```

### GenericListComponent<T>

Generyczny komponent do wyświetlania list z filtrowaniem:

```typescript
@Input() endpoint: string              // Endpoint API
@Input() columns: ColumnConfig[]       // Definicja kolumn
@Input() filters: FilterDefinition[]   // Definicja filtrów
@Input() title: string                 // Tytuł
@Input() emptyMessage: string          // Wiadomość, gdy brak danych
@Input() filterConfig?: {...}          // Niestandardowa logika filtrowania

@Output() itemSelected = new EventEmitter<T>()  // Emitter na kliknięcie wiersza
@Output() dataLoaded = new EventEmitter<T[]>()  // Emitter na załadowanie danych
```

**ColumnConfig:**

```typescript
interface ColumnConfig {
  key: string;                           // Pole w obiekcie
  label: string;                         // Etykieta kolumny
  type?: 'text' | 'date' | 'number' | 'custom' | 'boolean';
  format?: string;                       // Format (np. '2' dla liczby miejsc dziesiętnych)
  sortable?: boolean;                    // Czy sortowalna
  width?: string;                        // Szerokość
  render?: (value, row) => string;       // Funkcja renderowania
}
```

**FilterDefinition:**

```typescript
interface FilterDefinition {
  key: string;                                           // Pole w obiekcie
  type: 'text' | 'date' | 'number' | 'select';
  placeholder?: string;
  label?: string;
  options?: Array<{ label: string; value: any }>;  // Dla select
}
```

## Przykład: Refaktoryzacja Komponentu

### Przed (hardcoded):

```typescript
export class InspectionsComponent implements OnInit {
  inspections: Inspection[] = [];
  filtered: Inspection[] = [];
  loading = false;
  searchPlate = '';
  searchInspector = '';

  ngOnInit() { this.load(); }

  load() {
    this.svc.getAll().subscribe({
      next: (data) => { 
        this.inspections = data; 
        this.filter(); 
        this.loading = false; 
      },
      error: () => { this.loading = false; }
    });
  }

  filter() {
    this.filtered = this.inspections.filter(i =>
      (this.searchPlate ? i.vehiclePlate.toLowerCase().includes(...) : true) &&
      (this.searchInspector ? i.inspectorName.toLowerCase().includes(...) : true) &&
      // ... więcej warunków
    );
  }
}
```

HTML był również hardcoded z manualnym renderowaniem tabeli.

### Po (generyczne):

```typescript
export class InspectionsComponent implements OnInit {
  endpoint = 'inspections';
  title = 'inspections.filterTitle';
  
  columns: ColumnConfig[] = [
    { key: 'vehiclePlate', label: 'inspections.columns.plate' },
    { key: 'inspectorName', label: 'inspections.columns.inspector' },
    { key: 'inspectionDate', label: 'inspections.columns.date', type: 'date' },
    { 
      key: 'items', 
      label: 'inspections.columns.passRate',
      render: (value) => {
        const passed = value?.filter((it: any) => it.passed).length || 0;
        return `${passed} / ${value?.length || 0}`;
      }
    }
  ];

  filters: FilterDefinition[] = [
    { key: 'vehiclePlate', type: 'text', placeholder: 'inspections.placeholders.plate' },
    { key: 'inspectorName', type: 'text', placeholder: 'inspections.placeholders.inspector' },
  ];

  // Gotowe - cała logika w GenericListComponent!
}
```

HTML:

```html
<app-generic-list
  [endpoint]="endpoint"
  [columns]="columns"
  [filters]="filters"
  [title]="title"
></app-generic-list>
```

## FilterUtil

Narzędzie do filtrowania danych:

```typescript
// Filtrowanie wielopól
FilterUtil.applyFilters(items, filterValues, customFilterConfig);

// Szukanie w kilku polach
FilterUtil.searchInFields(items, searchTerm, ['field1', 'field2']);
```

## Korzyści

✅ **Redukcja kodu** - Przeciętnie 60-80% mniej linii w komponentach  
✅ **Konsystencja** - Wszystkie listy wyglądają i działają podobnie  
✅ **Łatwość rozszerzania** - Nowe listy to teraz tylko konfiguracja  
✅ **Utrzymanie** - Zmiany w jeden miejscu dotyczą wszystkich list  
✅ **Testowanie** - Generyczne komponenty można testować raz  

## Migracja Istniejących Komponentów

Jeśli masz inne komponenty listy (np. clients, settings), postupuj tak:

1. **Utwórz serwis domenowy** rozszerzający `GenericCrudService`:
   ```typescript
   export class ClientService extends GenericCrudService<Client> {
     readonly endpoint = 'clients';
   }
   ```

2. **Zdefiniuj kolumny i filtry** w komponencie:
   ```typescript
   columns: ColumnConfig[] = [
     { key: 'name', label: 'clients.columns.name' },
     // ...
   ];
   ```

3. **Zamień template** na `<app-generic-list>`:
   ```html
   <app-generic-list
     [endpoint]="endpoint"
     [columns]="columns"
     [filters]="filters"
   ></app-generic-list>
   ```

4. **Usuń logikę z komponenty** - cała obsługiwana przez GenericListComponent

## Następne Kroki

- [ ] Refaktoryzuj Client list
- [ ] Dodaj stronicowanie do GenericListComponent
- [ ] Dodaj sortowanie kolumn
- [ ] Utwórz GenericFormComponent dla edycji
- [ ] Dodaj generyczne komponenty dla modali
- [ ] Rozpowszechnij wzorce na backend (generic repository pattern)

## Kontakt

Jeśli masz pytania o architekturę, sprawdź inline documentation w plikach komponentów.
