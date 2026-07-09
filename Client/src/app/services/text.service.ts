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
  general: {
    save: 'Zapisz',
    cancel: 'Anuluj',
    close: 'Zamknij',
    delete: 'Usuń',
    edit: 'Edytuj',
    add: 'Dodaj',
    preview: 'Podgląd',
    search: 'Szukaj',
    loading: 'Ładowanie...',
    noData: 'Brak danych',
    yes: 'Tak',
    no: 'Nie',
    confirm: 'Potwierdź'
  },
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
  globalSearch: {
    placeholder: 'Nazwa stacji',
    ariaLabel: 'Nazwa stacji'
  },
  userMenu: {
    loggedInAs: 'Zalogowany jako',
    permissionLabel: 'Nr uprawnień',
    edit: 'Edytuj użytkownika',
    logout: 'Wyloguj'
  },
  menu: {
    home: 'Strona główna',
    newInspection: 'Nowe badanie',
    newInspectionHint: 'Utwórz nowy protokół badania',
    inspections: 'Wszystkie badania',
    inspectionsHint: 'Przeglądaj i filtruj wyniki badań',
    types: 'Ewidencja opryskiwaczy',
    typesHint: 'Dodawaj i edytuj opryskiwacze',
    settings: 'Ustawienia',
    settingsHint: 'Konfiguracja aplikacji',
    clients: 'Klienci',
    clientsHint: 'Kartoteka klientów i flot',
    marks: 'Ewidencja znaków',
    marksHint: 'Śledź znaki kontrolne',
    registry: 'Rejestr sprzętu',
    registryHint: 'Rejestr przebadanego sprzętu'
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
    demoHint: '',
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
  inspectionProtocol: {
    steps: {
      client: 'Klient',
      sprayer: 'Opryskiwacz',
      metadata: 'Dane badania',
      general: 'Ogólne',
      pump: 'Pompa',
      agitation: 'Mieszanie',
      tank: 'Zbiornik',
      measuring: 'Manometr',
      piping: 'Przewody',
      filtration: 'Filtracja',
      boom: 'Belka',
      nozzles: 'Rozpylacze',
      distribution: 'Rozkład',
      summary: 'Podsumowanie',
      generalData: 'Dane ogólne',
      protocol: 'Protokół badania'
    },
    pdfPreview: {
      title: 'Podgląd protokołu PDF',
      generate: 'Generuj podgląd',
      generating: 'Generowanie...',
      close: 'Zamknij',
      download: 'Pobierz PDF',
      print: 'Drukuj'
    }
  },
  newInspection: {
    title: 'Nowe badanie',
    confirmCancel: 'Czy na pewno chcesz anulować? Niezapisane zmiany zostaną utracone.',
    sections: {
      vehicle: 'Dane pojazdu',
      suspension: 'Zawieszenie',
      alignment: 'Geometria',
      lights: 'Oświetlenie',
      brakes: 'Hamulce'
    },
    headings: {
      vehicle: 'Informacje o pojeździe',
      client: 'Wybierz klienta',
      sprayer: 'Wybierz opryskiwacz',
      metadata: 'Dane badania',
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
    title: 'Protokoły badań',
    loading: 'Ładowanie badań...',
    filterTitle: 'Filtruj badania',
    searchPlaceholder: 'Szukaj po numerze protokołu, kliencie, opryskiwaczu...',
    emptyDetailMessage: 'Wybierz protokół z listy, aby zobaczyć szczegóły',
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
    empty: 'Brak protokołów.',
    columns: {
      plate: 'Tablica',
      inspector: 'Diagnosta',
      date: 'Data badania',
      items: 'Pozycje',
      passRate: 'Zaliczone',
      client: 'Klient',
      sprayer: 'Opryskiwacz',
      validUntil: 'Ważne do'
    },
    filters: {
      year: 'Rok',
      allYears: 'Wszystkie lata',
      result: 'Wynik',
      allResults: 'Wszystkie wyniki',
      resultPositive: 'Pozytywny',
      resultNegative: 'Negatywny',
      inspector: 'Diagnosta',
      city: 'Miasto',
      cityPlaceholder: 'Wpisz nazwę miasta...',
      voivodeship: 'Województwo',
      voivodeshipPlaceholder: 'Wpisz województwo...',
      distance: 'Odległość od klienta',
      noDistanceLimit: 'Bez ograniczeń',
      dateRange: 'Zakres dat',
      sprayerType: 'Typ opryskiwacza',
      allSprayerTypes: 'Wszystkie typy'
    },
    steps: {
      general: 'Dane ogólne',
      sprayer: 'Opryskiwacz',
      results: 'Wyniki badania',
      final: 'Wynik końcowy'
    },
    result: {
      positive: 'Pozytywny',
      negative: 'Negatywny',
      pending: 'W trakcie'
    },
    detail: {
      protocolNumber: 'Numer protokołu',
      inspectionDate: 'Data badania',
      inspectorName: 'Imię i nazwisko diagnosty',
      inspectorNumber: 'Numer uprawnień',
      inspectionPlace: 'Miejsce badania',
      clientInfo: 'Dane klienta',
      clientName: 'Nazwa klienta',
      clientAddress: 'Adres',
      clientCity: 'Miasto',
      clientVoivodeship: 'Województwo',
      sprayerType: 'Typ opryskiwacza',
      sprayerManufacturer: 'Producent',
      sprayerModel: 'Model',
      sprayerSerial: 'Numer fabryczny',
      productionYear: 'Rok produkcji',
      tankCapacity: 'Pojemność zbiornika',
      boomWidth: 'Szerokość belki',
      nozzleCount: 'Liczba rozpylaczy',
      finalResult: 'Wynik końcowy',
      validUntil: 'Ważne do',
      remarks: 'Uwagi',
      inspectorSignature: 'Podpis diagnosty',
      ownerSignature: 'Podpis właściciela',
      controlStickerNumber: 'Numer naklejki kontrolnej',
      clientTaxId: 'NIP',
      sectionCount: 'Liczba sekcji'
    },
    messages: {
      loadError: 'Nie udało się załadować listy protokołów',
      loadDetailError: 'Nie udało się załadować szczegółów protokołu',
      deleted: 'Protokół został usunięty',
      deleteError: 'Nie udało się usunąć protokołu'
    },
    deleteDialog: {
      title: 'Usuń protokół',
      message: 'Czy na pewno chcesz usunąć ten protokół? Tej operacji nie można cofnąć.'
    }
  },
  inspection: {
    notes: 'Uwagi',
    sections: {
      general: 'Stan ogólny i wyposażenie',
      pump: 'Pompa i napęd',
      agitation: 'Mieszanie',
      tank: 'Zbiornik',
      measuring: 'Urządzenia pomiarowe',
      piping: 'Przewody i węże',
      filtration: 'Filtracja',
      boom: 'Belka polowa / Opryskiwacz sadowniczy',
      nozzles: 'Rozpylacze',
      distribution: 'Rozkład poprzeczny'
    },
    section1: {
      generalCondition: 'Ogólny stan techniczny',
      markingsReadable: 'Czytelność oznaczeń',
      equipmentComplete: 'Kompletność wyposażenia'
    },
    section2: {
      pumpOperation: 'Praca pompy',
      pumpSealing: 'Szczelność pompy',
      pressurePulsation: 'Pulsacja ciśnienia'
    },
    section3: {
      agitatorOperation: 'Praca mieszadła'
    },
    section4: {
      tankCondition: 'Stan zbiornika',
      tankSealing: 'Szczelność zbiornika',
      levelIndicator: 'Wskaźnik poziomu',
      flushingSystem: 'System płukania'
    },
    section5: {
      manometer: 'Manometr',
      manometerDialSize: 'Średnica tarczy manometru'
    },
    section6: {
      pipesCondition: 'Stan przewodów',
      connectionsSealing: 'Szczelność połączeń'
    },
    section7: {
      suctionFilter: 'Filtr ssawny',
      pressureFilter: 'Filtr ciśnieniowy',
      nozzleFilters: 'Filtry rozpylaczy'
    },
    section8: {
      fieldBoomCondition: 'Stan belki polowej',
      boomStability: 'Stabilność belki',
      boomHeight: 'Wysokość belki',
      boomSymmetry: 'Symetria belki',
      orchardSprayerCondition: 'Stan opryskiwacza sadowniczego',
      airStreamDirection: 'Kierunek strumienia powietrza'
    },
    section9: {
      nozzleUniformity: 'Jednolitość rozpylaczy',
      nozzleFlowRate: 'Wydajność rozpylaczy',
      nozzleCondition: 'Stan rozpylaczy'
    },
    section10: {
      transverseDistribution: 'Rozkład poprzeczny',
      coefficientOfVariation: 'Współczynnik zmienności'
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
    cropSprayers: {
      addButton: 'Nowy opryskiwacz',
      listTitle: 'Lista opryskiwaczy',
      searchPlaceholder: 'Szukaj po numerze, nazwie, producencie...',
      selectOrAdd: 'Wybierz opryskiwacz z listy lub dodaj nowy',
      empty: 'Brak opryskiwaczy',
      loading: 'Ładowanie opryskiwaczy...',
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
        yearTo: 'Rok do',
        allTypes: 'Wszystkie typy',
        allKinds: 'Wszystkie rodzaje',
        clear: 'Wyczyść',
        apply: 'Zastosuj'
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
        owner: 'Właściciel',
        noOwner: '— Brak właściciela —',
        noOwnerShort: 'Brak właściciela',
        selectOwner: 'Wpisz nazwę lub adres właściciela...',
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
        fanType: 'Typ wentylatora',
        createdAt: 'Data utworzenia'
      },
      actions: {
        save: 'Zapisz',
        cancel: 'Anuluj',
        discardChanges: 'Odrzuć zmiany',
        keepEditing: 'Pozostań w edycji',
        edit: 'Edytuj',
        delete: 'Usuń',
        deleteConfirm: 'Czy na pewno chcesz usunąć ten opryskiwacz? Tej operacji nie można cofnąć.',
        yes: 'Tak',
        no: 'Nie'
      },
      messages: {
        saved: 'Opryskiwacz został zapisany.',
        deleted: 'Opryskiwacz został usunięty.',
        error: 'Wystąpił błąd podczas zapisu opryskiwacza.',
        unsavedTitle: 'Niezapisane zmiany',
        unsavedText: 'Masz niezapisane zmiany dla bieżącego opryskiwacza. Co zamierzasz zrobić?',
        deleteTitle: 'Potwierdzenie usunięcia'
      }
    }
  },
  clients: {
    title: 'Klienci',
    description: 'Zarządzaj kartoteką klientów i ich flotą.',
    listTitle: 'Lista klientów',
    addButton: 'Dodaj klienta',
    searchPlaceholder: 'Szukaj klienta...',
    selectOrAdd: 'Wybierz klienta z listy lub dodaj nowego',
    empty: 'Brak klientów',
    loading: 'Ładowanie...',
    addressSection: 'Adres',
    sprayersPlaceholder: 'Lista opryskiwaczy klienta zostanie dodana w przyszłej wersji.',
    noSprayersAssigned: 'Ten klient nie ma przypisanych opryskiwaczy.',
    goToSprayer: 'Przejdź do opryskiwacza',
    steps: {
      basics: 'Dane podstawowe',
      address: 'Adres',
      sprayers: 'Opryskiwacze'
    },
    fields: {
      clientType: 'Typ klienta',
      displayName: 'Nazwa wyświetlana',
      firstName: 'Imię',
      lastName: 'Nazwisko',
      pesel: 'PESEL',
      companyName: 'Nazwa firmy',
      nip: 'NIP',
      regon: 'REGON',
      voivodeship: 'Województwo',
      city: 'Miasto',
      street: 'Ulica',
      buildingNumber: 'Nr budynku',
      apartmentNumber: 'Nr lokalu',
      zipCode: 'Kod pocztowy',
      post: 'Poczta',
      createdAt: 'Data utworzenia',
      newClient: 'Nowy klient',
      mapPreview: 'Podgląd lokalizacji',
      mapPlaceholder: 'Wprowadź pełny adres, aby wyświetlić mapę'
    },
    filters: {
      allTypes: 'Wszystkie typy',
      typePerson: 'Osoba fizyczna',
      typeCompany: 'Firma',
      city: 'Miasto',
      clear: 'Wyczyść',
      apply: 'Zastosuj'
    },
    actions: {
      edit: 'Edytuj',
      delete: 'Usuń',
      save: 'Zapisz',
      cancel: 'Anuluj',
      discard: 'Odrzuć'
    },
    messages: {
      saved: 'Klient został zapisany.',
      deleted: 'Klient został usunięty.',
      error: 'Wystąpił błąd podczas operacji.',
      unsavedTitle: 'Niezapisane zmiany',
      unsavedText: 'Masz niezapisane zmiany. Co zamierzasz zrobić?',
      deleteTitle: 'Potwierdzenie usunięcia',
      deleteText: 'Czy na pewno chcesz usunąć tego klienta?',
      mapNotFound: 'Podany adres nie został odnaleziony w Google Maps. Uzupełnij dane adresowe (miasto, ulica, numer budynku), aby wyświetlić lokalizację.',
      requiredAddressFields: 'Wypełnij wymagane pola adresowe'
    },
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
    description: 'Ewidencja znaków kontrolnych potwierdzających sprawność techniczną sprzętu.',
    dateFrom: 'Data od',
    dateTo: 'Data do',
    generate: 'Generuj',
    print: 'Drukuj',
    noData: 'Brak danych dla wybranego zakresu dat.',
    columns: {
      lp: 'Lp.',
      stickerNumber: 'Nr znaku kontrolnego',
      issueDate: 'Data wydania',
      owner: 'Właściciel opryskiwacza',
      protocolNumber: 'Nr protokołu kontroli'
    }
  },
  registry: {
    title: 'Rejestr przebadanego sprzętu',
    description: 'Generuj rejestr przebadanego sprzętu za wybrany okres.',
    dateFrom: 'Data od',
    dateTo: 'Data do',
    generate: 'Generuj',
    print: 'Drukuj',
    noData: 'Brak danych dla wybranego zakresu dat.',
    columns: {
      lp: 'Lp.',
      protocolNumber: 'Nr protokołu',
      inspectionDate: 'Data badania',
      owner: 'Posiadacz sprzętu',
      ownerAddress: 'Adres posiadacza',
      sprayerType: 'Typ opryskiwacza',
      sprayerKind: 'Rodzaj',
      manufacturer: 'Producent',
      serialNumber: 'Nr fabryczny',
      productionYear: 'Rok produkcji',
      result: 'Wynik badania',
      stickerNumber: 'Nr naklejki',
      validUntil: 'Termin ważności',
      inspector: 'Diagnosta'
    },
    resultPositive: 'Pozytywny',
    resultNegative: 'Negatywny',
    resultPending: '—',
    typeField: 'Polowy',
    typeGarden: 'Sadowniczy',
    kindMounted: 'Zawieszany',
    kindTrailed: 'Przyczepiany',
    kindSelfPropelled: 'Samobieżny',
    kindOther: 'Inny'
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
