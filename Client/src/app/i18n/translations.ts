/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

export type LanguageCode = 'pl' | 'uk';

export interface TranslationTree {
  [key: string]: string | TranslationTree;
}

export interface LanguageOption {
  code: LanguageCode;
  label: string;
  nativeName: string;
  flag: string;
}

export const LANGUAGES: LanguageOption[] = [
  { code: 'pl', label: 'PL', nativeName: 'Polski', flag: 'pl' },
  { code: 'uk', label: 'UA', nativeName: 'Українська', flag: 'uk' }
];

export const DEFAULT_LANGUAGE: LanguageCode = 'pl';

export const translations: Record<LanguageCode, TranslationTree> = {
  pl: {
    language: {
      label: 'Język',
      options: {
        pl: 'Polski',
        uk: 'Ukraiński'
      }
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
    userMenu: {
      loggedInAs: 'Zalogowany jako:',
      permissionLabel: 'Nr uprawnień',
      edit: 'Edytuj użytkownika',
      logout: 'Wyloguj'
    },
    menu: {
      newInspection: 'Nowe badanie',
      newInspectionHint: 'Utwórz nowy protokół badania',
      inspections: 'Wszystkie badania',
      inspectionsHint: 'Przeglądaj i filtruj wyniki badań',
      types: 'Typy pojazdów',
      typesHint: 'Zarządzaj kategoriami pojazdów',
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
      languageLabel: 'Wybierz język',
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
        language: 'Język',
          kindSelfPropelled: 'Samobieżny',
          kindOther: 'Inny',
          manufacturer: 'Producent',
          yearFrom: 'Rok od',
          yearTo: 'Rok do'
      },
      admin: {
        users: 'Użytkownicy',
        backup: 'Kopia zapasowa',
        logs: 'Logi'
      }
    },
    types: {
      title: 'Typy pojazdów / Maszyny opryskujące',
      list: {
        car: 'Samochód osobowy',
        truck: 'Ciężarówka',
        motorcycle: 'Motocykl',
        trailer: 'Przyczepa'
      },
      machines: {
        addButton: 'Nowy',
        listTitle: 'Lista maszyn opryskujących',
        searchPlaceholder: 'Szukaj po numerze, nazwie, producencie...',
        filters: {
          type: 'Typ',
          kind: 'Rodzaj',
          typeField: 'Polowy',
          typeGarden: 'Sadowniczy',
          kindMounted: 'Zawieszany',
          kindTrailed: 'Przyczepiany',
          kindSelfPropelled: 'Samobieżny',
          kindOther: 'Inny'
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
          yes: 'TAK',
          no: 'NIE'
        },
        messages: {
          saved: 'Maszyna została zapisana.',
          deleted: 'Maszyna została usunięta.',
          error: 'Wystąpił błąd podczas zapisu maszyny.',
          unsavedTitle: 'Niezapisane zmiany',
          unsavedText: 'Masz niezapisane zmiany dla bieżącej maszyny. Co chcesz zrobić?'
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
  },
  uk: {
    language: {
      label: 'Мова',
      options: {
        pl: 'Польська',
        uk: 'Українська'
      }
    },
    theme: {
      toggle: 'Змінити тему',
      mode: {
        light: 'Світлий',
        dark: 'Темний'
      }
    },
    tabs: {
      empty: 'Немає відкритих вкладок. Використайте меню, щоб почати.'
    },
    userMenu: {
      loggedInAs: 'Увійшов як:',
      permissionLabel: 'Номер дозволу',
      edit: 'Редагувати користувача',
      logout: 'Вийти'
    },
    menu: {
      newInspection: 'Новий огляд',
      newInspectionHint: 'Створити новий протокол огляду',
      inspections: 'Усі огляди',
      inspectionsHint: 'Перегляд та фільтрація результатів',
      types: 'Типи ТЗ',
      typesHint: 'Керування категоріями транспорту',
      stats: 'Статистика',
      statsHint: 'Перегляд статистики оглядів',
      settings: 'Налаштування',
      settingsHint: 'Параметри застосунку',
      clients: 'Клієнти',
      clientsHint: 'Картотека клієнтів і автопарку',
      marks: 'Реєстр знаків',
      marksHint: 'Відстеження виданих знаків',
      notifications: 'Сповіщення',
      notificationsHint: 'Останні події системи'
    },
    login: {
      username: 'Користувач',
      password: 'Пароль',
       usernamePlaceholder: 'login',
      passwordPlaceholder: 'пароль',
      passwordShow: 'Показати пароль',
      passwordHide: 'Приховати пароль',
      submit: 'Увійти',
      submitting: 'Вхід...',
      demoHint: '© Copyright by Wojciech Salamon',
      languageLabel: 'Оберіть мову',
      errors: {
        invalidCredentials: 'Невірний логін або пароль',
        sessionActive: 'Для цього користувача вже є активна сесія.'
      },
      session: {
        terminated: 'Вашу сесію завершено, оскільки на цей акаунт увійшли з іншого пристрою або повторно на цьому ж.'
      },
      sessionConflict: {
        message: 'Для цього користувача вже є активна сесія на іншому пристрої.',
        confirm: 'Завершити ту сесію та увійти тут?',
        buttons: {
          takeOver: 'Перехопити сесію',
          cancel: 'Скасувати'
        }
      }
    },
    status: {
      connected: 'Підключено',
      disconnected: 'Немає з\'єднання',
      database: 'База',
      logout: 'Вийти'
    },
    newInspection: {
      title: 'Новий огляд',
      sections: {
        vehicle: 'Дані про ТЗ',
        suspension: 'Підвіска',
        alignment: 'Геометрія',
        lights: 'Освітлення',
        brakes: 'Гальма'
      },
      headings: {
        vehicle: 'Відомості про транспорт',
        suspension: 'Перевірка підвіски',
        alignment: 'Перевірка геометрії коліс',
        lights: 'Перевірка освітлення',
        brakes: 'Перевірка гальмівної системи'
      },
      fields: {
        vehiclePlate: 'Реєстраційний номер',
        inspectorName: 'Ім’я діагноста',
        inspectionDate: 'Дата огляду',
        notes: 'Примітки'
      },
      placeholders: {
        vehiclePlate: 'напр. ABC-12345',
        inspectorName: 'наприклад Іван Петренко',
        notes: 'Додаткова інформація...',
        description: 'Опис',
        dateFrom: 'Дата від',
        dateTo: 'Дата до'
      },
      labels: {
        pass: 'Пройдено'
      },
      actions: {
        addItem: 'Додати позицію',
        back: 'Назад',
        next: 'Далі',
        save: 'Зберегти',
        cancel: 'Скасувати'
      },
      messages: {
        saved: 'Збережено',
        error: 'Помилка збереження'
      },
      defaults: {
        suspension: {
          springs: 'Пружини',
          shocks: 'Амортизатори',
          controlArms: 'Важелі підвіски'
        },
        alignment: {
          camber: 'Розвал',
          caster: 'Кастер',
          toe: 'Сходження'
        },
        lights: {
          headlights: 'Фари',
          tailLights: 'Задні ліхтарі',
          turnSignals: 'Поворотники'
        },
        brakes: {
          front: 'Передні гальма',
          rear: 'Задні гальма',
          fluid: 'Гальмівна рідина'
        }
      }
    },
    inspections: {
      loading: 'Завантаження оглядів...',
      filterTitle: 'Фільтрувати огляди',
      placeholders: {
        plate: 'Номерний знак',
        inspector: 'Ім’я діагноста',
        dateFrom: 'Дата від',
        dateTo: 'Дата до'
      },
      actions: {
        clear: 'Очистити фільтри'
      },
      results: 'Результати',
      empty: 'Нічого не знайдено.',
      columns: {
        plate: 'Номер',
        inspector: 'Інспектор',
        date: 'Дата',
        items: 'Позиції',
        passRate: 'Показник'
      }
    },
    statistics: {
      title: 'Статистика',
      cards: {
        last30: 'Огляди (30 днів)',
        passRate: 'Показник успішності'
      }
    },
    settings: {
      title: 'Налаштування',
      tabs: {
        application: 'Налаштування програми',
        user: 'Налаштування користувача',
        admin: 'Налаштування адміністратора'
      },
      items: {
        database: 'Шлях до бази',
        api: 'Адреса API',
        version: 'Версія'
      },
      user: {
        language: 'Мова',
        theme: 'Тема',
        notifications: 'Сповіщення'
      },
      admin: {
        users: 'Користувачі',
        backup: 'Резервна копія',
        logs: 'Журнали'
      }
    },
    types: {
      title: 'Типи ТЗ / Обприскувачі',
      description: 'Керуйте машинами для обприскування та їх параметрами.',
      list: {
        car: 'Легковий автомобіль',
        truck: 'Вантажівка',
        motorcycle: 'Мотоцикл',
        trailer: 'Причіп'
      },
      machines: {
        addButton: 'Новий',
        listTitle: 'Список машин для обприскування',
        searchPlaceholder: 'Пошук за номером, назвою, виробником...',
        filters: {
          type: 'Тип',
          kind: 'Різновид',
          typeField: 'Польовий',
          typeGarden: 'Садовий',
          kindMounted: 'Навісний',
          kindTrailed: 'Причіпний',
          kindSelfPropelled: 'Самохідний',
          kindOther: 'Інший'
        },
        steps: {
          basics: 'Основні дані',
          pump: 'Насос',
          tank: 'Бак',
          control: 'Вимірювально-керуючі пристрої',
          boom: 'Штанга обприскувача',
          sections: 'Секції',
          fieldNozzles: 'Польові розпилювачі',
          gardenNozzles: 'Садові розпилювачі',
          fan: 'Вентилятор'
        },
        fields: {
          serialNumber: 'Серійний / інвентарний номер',
          sprayerName: 'Назва обприскувача',
          type: 'Тип (польовий / садовий)',
          kind: 'Різновид (навісний / причіпний / самохідний / інший)',
          manufacturer: 'Виробник',
          productionYear: 'Рік випуску',
          purchaseDate: 'Дата купівлі',
          pumpType: 'Тип насоса',
          pumpPiston: 'Поршневий',
          pumpDiaphragm: 'Мембранний',
          pumpOther: 'Інший',
          pumpOtherType: 'Інший тип насоса',
          pumpFlowRate: 'Продуктивність [дм³/хв]',
          tankCapacity: 'Обʼєм бака [л]',
          hasFlushing: 'Система промивання',
          hasDiluter: 'Розчинювач',
          hasWashingDevice: 'Мийний пристрій',
          hasManometer: 'Манометр',
          hasComputer: 'Компʼютер',
          boomWidth: 'Ширина штанги [м]',
          boomWet: 'Мокра штанга',
          boomDry: 'Суха штанга',
          boomDampeningMechanism: 'Механізм демпфування штанги',
          sectionCount: 'Кількість секцій',
          nozzlesFieldFeatures: 'Польові розпилювачі – характеристики та позначення',
          nozzlesGardenFeatures: 'Садові розпилювачі – характеристики та позначення',
          fanType: 'Тип вентилятора'
        },
        actions: {
          back: 'Назад',
          next: 'Далі',
          save: 'Зберегти',
          cancel: 'Скасувати',
          discardChanges: 'Відхилити зміни',
          keepEditing: 'Залишитись в режимі редагування',
          edit: 'Редагувати',
          delete: 'Видалити',
          deleteConfirm: 'Видалити цю машину? Дію неможливо скасувати.',
          yes: 'ТАК',
          no: 'НІ'
        },
        messages: {
          saved: 'Машину збережено.',
          deleted: 'Машину видалено.',
          error: 'Помилка під час збереження машини.',
          unsavedTitle: 'Незбережені зміни',
          unsavedText: 'Є незбережені зміни для поточної машини. Що зробити?'
        }
      }
    },
    clients: {
      title: 'Клієнти',
      description: 'Керуйте картотекою клієнтів та їх флотом.',
      columns: {
        name: 'Ім’я',
        document: '№ документа',
        vehicle: 'Транспорт',
        status: 'Статус'
      },
      status: {
        active: 'Активний',
        blocked: 'Заблоковано'
      }
    },
    marks: {
      title: 'Реєстр контрольних знаків',
      description: 'Відстежуйте видані наклейки та терміни дії.',
      columns: {
        number: 'Номер знака',
        vehicle: 'Транспорт',
        issued: 'Видано',
        expires: 'Дійсне до'
      }
    },
    notifications: {
      title: 'Сповіщення',
      empty: 'Немає сповіщень.',
      items: {
        maintenance: {
          title: 'Заплановано сервісне вікно',
          body: 'Система буде недоступна сьогодні о 22:00.'
        },
        newAssignments: {
          title: '3 нові огляди призначено',
          body: 'Перевірте панель завдань, щоб підтвердити.'
        },
        backup: {
          title: 'Резервне копіювання завершено',
          body: 'Остання копія бази створена успішно.'
        }
      },
      times: {
        minutes5: '5 хв тому',
        hour1: '1 год тому',
        todayMorning: 'Сьогодні 07:45'
      }
    }
  }
};
