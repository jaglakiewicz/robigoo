/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Injectable } from '@angular/core';

interface TextTree {
  [key: string]: string | TextTree;
}

const TEXTS: TextTree = {
  theme: {
    toggle: 'Zmień motyw',
    mode: {
      light: 'Jasny',
      dark: 'Ciemny'
    }
  },
  tabs: {
    empty: 'Brak otwartych kart. Użyj menu, aby rozpocząć.'
  },
  userMenu: {
    loggedInAs: 'Zalogowany jako',
    permissionLabel: 'Nr uprawnień',
    edit: 'Edytuj użytkownika',
    logout: 'Wyloguj'
  },
  menu: {
    newInspection: 'Nowe badanie',
    newInspectionHint: 'Utwórz nowy protokół badania',
    inspections: 'Wszystkie badania',
    inspectionsHint: 'Przeglądaj i filtruj wyniki badań',
    types: 'Ewidencja opryskiwaczy',
    typesHint: 'Dodawaj i edytuj opryskiwacze',
    stats: 'Statystyki',
    statsHint: 'Podgląd wyników i trendów',
    settings: 'Ustawienia',
    settingsHint: 'Konfiguracja aplikacji',
    clients: 'Klienci',
    clientsHint: 'Kartoteka klientów i flot',
    marks: 'Ewidencja znaków',
    marksHint: 'Śledź znaki kontrolne',
    notifications: 'Powiadomienia',
    notificationsHint: 'Ostatnie alerty systemowe'
  },
  login: {
    username: 'Użytkownik',
    password: 'Hasło',
    usernamePlaceholder: 'login',
    passwordPlaceholder: 'hasło',
    passwordShow: 'Pokaż hasło',
    passwordHide: 'Ukryj hasło',
    submit: 'Zaloguj',
    submitting: 'Logowanie...',
    demoHint: '© Copyright by Wojciech Salamon',
    errors: {
      invalidCredentials: 'Nieprawidłowy login lub hasło',
      sessionActive: 'Dla tego użytkownika istnieje już aktywna sesja.'
    },
    session: {
      terminated: 'Twoja sesja została zakończona, ponieważ zalogowano się na to konto na innym urządzeniu lub ponownie na tym samym.'
    },
    sessionConflict: {
      message: 'Dla tego użytkownika istnieje już aktywna sesja na innym urządzeniu.',
      confirm: 'Czy chcesz zakończyć tamtą sesję i zalogować się tutaj?',
      buttons: {
        takeOver: 'Przejmij sesję',
        cancel: 'Anuluj'
      }
    }
  },
  status: {
    connected: 'Połączono',
    disconnected: 'Brak połączenia',
    database: 'Baza',
    logout: 'Wyloguj'
  },
  newInspection: {
    title: 'Nowe badanie',
    sections: {
      vehicle: 'Dane pojazdu',
      suspension: 'Zawieszenie',
      alignment: 'Geometria',
      lights: 'Oświetlenie',
      brakes: 'Hamulce'
    },
    headings: {
      vehicle: 'Informacje o pojeździe',
      suspension: 'Kontrola zawieszenia',
      alignment: 'Kontrola geometrii kół',
      lights: 'Kontrola oświetlenia',
      brakes: 'Kontrola hamulców'
    },
    fields: {
      vehiclePlate: 'Numer rejestracyjny',
      inspectorName: 'Diagnosta',
      inspectionDate: 'Data badania',
      notes: 'Uwagi'
    },
    placeholders: {
      vehiclePlate: 'np. ABC-12345',
      inspectorName: 'np. Jan Kowalski',
      notes: 'Dodatkowe informacje...',
      description: 'Opis'
    },
    labels: {
      pass: 'Zaliczone'
    },
    actions: {
      addItem: 'Dodaj pozycję',
      removeItem: 'Usuń pozycję',
      back: 'Wstecz',
      next: 'Dalej',
      save: 'Zapisz',
      cancel: 'Anuluj'
    },
    messages: {
      saved: 'Zapisano',
      error: 'Błąd zapisu'
    },
    defaults: {
      suspension: {
        springs: 'Sprężyny',
        shocks: 'Amortyzatory',
        controlArms: 'Wahacze'
      },
      alignment: {
        camber: 'Pochylenie kół',
        caster: 'Wyprzedzenie sworznia',
        toe: 'Zbieżność'
      },
      lights: {
        headlights: 'Reflektory',
        tailLights: 'Światła tylne',
        turnSignals: 'Kierunkowskazy'
      },
      brakes: {
        front: 'Hamulce przednie',
        rear: 'Hamulce tylne',
        fluid: 'Płyn hamulcowy'
      }
    }
  },
  inspections: {
    loading: 'Ładowanie badań...',
    filterTitle: 'Filtruj badania',
    placeholders: {
      plate: 'Rejestracja pojazdu',
      inspector: 'Imię diagnosty',
      dateFrom: 'Data od',
      dateTo: 'Data do'
    },
    actions: {
      clear: 'Wyczyść filtry'
    },
    results: 'Wyniki',
    empty: 'Brak wyników.',
    columns: {
      plate: 'Tablica',
      inspector: 'Diagnosta',
      date: 'Data',
      items: 'Pozycje',
      passRate: 'Zaliczone'
    }
  },
  statistics: {
    title: 'Statystyki',
    cards: {
      last30: 'Badania (30 dni)',
      passRate: 'Wskaźnik zaliczeń'
    }
  },
  settings: {
    title: 'Ustawienia',
    tabs: {
      application: 'Ustawienia programu',
      user: 'Ustawienia użytkownika',
      admin: 'Ustawienia administratora'
    },
    items: {
      database: 'Ścieżka bazy',
      api: 'Adres API',
      version: 'Wersja'
    },
    user: {
      theme: 'Motyw'
    },
    admin: {
      users: 'Użytkownicy',
      backup: 'Kopia zapasowa',
      logs: 'Logi'
    }
  },
  types: {
    title: 'Opryskiwacze',
    list: {
      car: 'Samochód osobowy',
      truck: 'Ciężarówka',
      motorcycle: 'Motocykl',
      trailer: 'Przyczepa'
    },
    machines: {
      addButton: 'Nowy opryskiwacz',
      listTitle: 'Lista opryskiwaczy',
      searchPlaceholder: 'Szukaj po numerze, nazwie, producencie...',
      filters: {
        type: 'Typ',
        kind: 'Rodzaj',
        typeField: 'Polowy',
        typeGarden: 'Sadowniczy',
        kindMounted: 'Zawieszany',
        kindTrailed: 'Przyczepiany',
        kindSelfPropelled: 'Samobieżny',
        kindOther: 'Inny',
        manufacturer: 'Producent',
        yearFrom: 'Rok od',
        yearTo: 'Rok do'
      },
      steps: {
        basics: 'Dane podstawowe',
        pump: 'Pompa',
        tank: 'Zbiornik',
        control: 'Urządzenia',
        boom: 'Belka',
        sections: 'Sekcje',
        fieldNozzles: 'Rozpylacze polowe',
        gardenNozzles: 'Rozpylacze sadownicze',
        nozzles: 'Rozpylacze',
        fan: 'Wentylator'
      },
      fields: {
        serialNumber: 'Nr seryjny / ewidencyjny',
        sprayerName: 'Nazwa opryskiwacza',
        type: 'Typ',
        kind: 'Rodzaj',
        manufacturer: 'Producent',
        productionYear: 'Rok produkcji',
        purchaseDate: 'Data zakupu',
        pumpType: 'Typ pompy',
        pumpPiston: 'Tłokowa',
        pumpDiaphragm: 'Membranowa',
        pumpOther: 'Inna',
        pumpOtherType: 'Inny typ pompy',
        pumpFlowRate: 'Natężenie przepływu [dm³/min]',
        tankCapacity: 'Pojemność zbiornika [l]',
        hasFlushing: 'Przepłukiwanie',
        hasDiluter: 'Rozwadniacz',
        hasWashingDevice: 'Urządzenie myjące',
        hasManometer: 'Manometr',
        hasComputer: 'Komputer',
        boomWidth: 'Szerokość belki [m]',
        boomWet: 'Belka mokra',
        boomDry: 'Belka sucha',
        boomDampeningMechanism: 'Mechanizm tłumienia belki',
        sectionCount: 'Liczba sekcji',
        nozzlesFieldFeatures: 'Rozpylacze polowe – cechy i oznaczenia',
        nozzlesGardenFeatures: 'Rozpylacze sadownicze – cechy i oznaczenia',
        fanType: 'Typ wentylatora'
      },
      actions: {
        save: 'Zapisz',
        cancel: 'Anuluj',
        discardChanges: 'Odrzuć zmiany',
        keepEditing: 'Pozostań w edycji',
        edit: 'Edytuj',
        delete: 'Usuń',
        deleteConfirm: 'Czy na pewno chcesz usunąć tę maszynę? Tej operacji nie można cofnąć.',
        yes: 'Tak',
        no: 'Nie'
      },
      messages: {
        saved: 'Opryskiwacz został zapisany.',
        deleted: 'Opryskiwacz został usunięty.',
        error: 'Wystąpił błąd podczas zapisu opryskiwacza.',
        unsavedTitle: 'Niezapisane zmiany',
        unsavedText: 'Masz niezapisane zmiany dla bieżącego opryskiwacza. Co zamierzasz zrobić?'
      }
    }
  },
  clients: {
    title: 'Klienci',
    description: 'Zarządzaj kartoteką klientów i ich flotą.',
    columns: {
      name: 'Nazwa',
      document: 'Nr dokumentu',
      vehicle: 'Pojazd',
      status: 'Status'
    },
    status: {
      active: 'Aktywny',
      blocked: 'Zablokowany'
    }
  },
  marks: {
    title: 'Ewidencja znaków kontrolnych',
    description: 'Monitoruj wydane nalepki i terminy ważności.',
    columns: {
      number: 'Numer znaku',
      vehicle: 'Pojazd',
      issued: 'Wydano',
      expires: 'Ważne do'
    }
  },
  notifications: {
    title: 'Powiadomienia',
    empty: 'Brak powiadomień.',
    items: {
      maintenance: {
        title: 'Zaplanowano przerwę serwisową',
        body: 'System będzie niedostępny dziś o 22:00.'
      },
      newAssignments: {
        title: '3 nowe badania przydzielone',
        body: 'Sprawdź panel zleceń, aby potwierdzić.'
      },
      backup: {
        title: 'Kopia bazy zakończona',
        body: 'Ostatnia kopia bezpieczeństwa została wykonana pomyślnie.'
      }
    },
    times: {
      minutes5: '5 min temu',
      hour1: '1 h temu',
      todayMorning: 'Dziś 07:45'
    }
  }
};

@Injectable({ providedIn: 'root' })
export class TextService {
  /**
   * Pobiera tekst po kluczu, np. 'login.username'
   */
  get(key: string): string {
    if (!key) {
      return '';
    }
    const value = key.split('.').reduce<unknown>((acc, segment) => {
      if (acc && typeof acc === 'object' && segment in acc) {
        return (acc as Record<string, unknown>)[segment];
      }
      return undefined;
    }, TEXTS);
    return typeof value === 'string' ? value : key;
  }
}
