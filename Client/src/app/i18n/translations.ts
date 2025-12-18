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
      subtitle: 'System Zarządzania Stacją Kontroli Opryskiwaczy',
      username: 'Użytkownik',
      password: 'Hasło',
      usernamePlaceholder: 'admin',
      passwordPlaceholder: 'hasło',
      submit: 'Zaloguj',
      submitting: 'Logowanie...',
      demoHint: 'Dane demonstracyjne: admin / admin',
      languageLabel: 'Wybierz język',
      errors: {
        invalidCredentials: 'Nieprawidłowy login lub hasło'
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
        description: 'Opis',
        dateFrom: 'Data od',
        dateTo: 'Data do'
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
        theme: 'Motyw',
        notifications: 'Powiadomienia'
      },
      admin: {
        users: 'Użytkownicy',
        backup: 'Kopia zapasowa',
        logs: 'Logi'
      }
    },
    types: {
      title: 'Typy pojazdów',
      description: 'Zarządzaj typami pojazdów wykorzystywanymi w badaniach.',
      list: {
        car: 'Samochód osobowy',
        truck: 'Ciężarówka',
        motorcycle: 'Motocykl',
        trailer: 'Przyczepa'
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
      subtitle: '\u0421\u0438\u0441\u0442\u0435\u043c\u0430 \u0443\u043f\u0440\u0430\u0432\u043b\u0456\u043d\u043d\u044f \u0441\u0442\u0430\u043d\u0446\u0456\u0454\u044e \u043a\u043e\u043d\u0442\u0440\u043e\u043b\u044e \u043e\u0431\u043f\u0440\u0438\u0441\u043a\u0443\u0432\u0430\u0447\u0456\u0432',
      username: 'Користувач',
      password: 'Пароль',
      usernamePlaceholder: 'admin',
      passwordPlaceholder: 'пароль',
      submit: 'Увійти',
      submitting: 'Вхід...',
      demoHint: 'Демо дані: admin / admin',
      languageLabel: 'Оберіть мову',
      errors: {
        invalidCredentials: 'Невірний логін або пароль'
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
      title: 'Типи транспортних засобів',
      description: 'Керуйте типами транспортних засобів для техоглядів.',
      list: {
        car: 'Легковий автомобіль',
        truck: 'Вантажівка',
        motorcycle: 'Мотоцикл',
        trailer: 'Причіп'
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
