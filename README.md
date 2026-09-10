# ECommerce Platform (.NET 10)

Масштабована платформа електронної комерції, побудована за архітектурою **модульного моноліту (Vertical Slice Architecture)** на базі **.NET 10 (C# 14)** із шлюзом **ApiGateway (Ocelot)**, готова до переходу на мікросервіси.

---

## 1. Структура репозиторію

```text
.
├── ECommerce.sln               # Єдине рішення .NET 10
├── docs/                       # Архітектурна документація
│   ├── monolith-inventory.md   # Інвентаризація моноліту (Завдання 4.1)
│   ├── adr/                    # Архітектурні рішення (Architecture Decision Records)
│   │   ├── ADR-0001.md         # Поділ моноліту на сервіси (з інвентаризацією 4.1)
│   │   └── ADR-0002.md         # Спосіб маршрутизації зовнішніх запитів (Ocelot)
│   └── diagrams/               # Архітектурні діаграми
│       ├── architecture.drawio # Готовий файл для https://app.diagrams.net (3 вкладки)
│       ├── architecture.md     # Опис та джерельний код Mermaid-діаграм
│       └── view_diagrams.html  # Інтерактивний HTML-переглядач діаграм для браузера
├── src/
│   ├── ApiGateway/             # Шлюз Ocelot API Gateway (порт 5000)
│   │   ├── ocelot.json         # Декларативна маршрутизація
│   │   ├── appsettings.json    # Конфігурація хоста моноліту
│   │   ├── Program.cs          # Pipeline Ocelot, Correlation-Id, CORS, Health check
│   │   └── ApiGateway.csproj
│   └── Monolith/               # Основний модульний моноліт Web API (порт 5100)
│       ├── Domain/             # Доменні сутності та перелічення (Enums)
│       ├── Database/           # AppDbContext (EF Core + PostgreSQL)
│       ├── Common/             # Обробка винятків (IExceptionHandler), фільтри валідації
│       ├── Features/           # Вертикальні слайси (Auth, Catalog, Products, Orders)
│       ├── Extensions/         # ServiceCollection та EndpointRouteBuilder розширення
│       ├── Migrations/         # Міграції Entity Framework Core
│       ├── appsettings.json    # Конфігурація PostgreSQL та JWT
│       ├── Program.cs          # Точка входу Monolith API
│       └── ECommerce.Monolith.csproj
├── .gitignore
└── README.md
```

---

## 2. Архітектурна документація (docs/)

Усі проєктні рішення зафіксовані у відповідних файлах документації:
- **Інвентаризація моноліту (Завдання 4.1)**: детальний опис та таблиця винесені в окремий документ [docs/monolith-inventory.md](file:///docs/monolith-inventory.md), а також зафіксовані в [ADR-0001](file:///docs/adr/ADR-0001.md).
- **ADR-0001 ([docs/adr/ADR-0001.md](file:///docs/adr/ADR-0001.md))**: Обґрунтування поділу моноліту на 3 незалежні сервіси (`Auth`, `Catalog & Products`, `Orders`) за принципом бізнес-можливостей та володіння даними.
- **ADR-0002 ([docs/adr/ADR-0002.md](file:///docs/adr/ADR-0002.md))**: Вибір шлюзу **Ocelot** як єдиної точки входу на порті `5000` для інкапсуляції внутрішніх адрес і реалізації патерну Strangler Fig.
- **Діаграми ([docs/diagrams/](file:///docs/diagrams/))**:
  - **[architecture.drawio](file:///docs/diagrams/architecture.drawio)**: файл для відкриття на [app.diagrams.net (Draw.io)](https://app.diagrams.net/) — містить 3 сторінки з усіма діаграмами.
  - **[architecture.md](file:///docs/diagrams/architecture.md)**: текстовий опис та вихідний код Mermaid-діаграм.
  - **[view_diagrams.html](file:///docs/diagrams/view_diagrams.html)**: автономний HTML-файл для швидкого перегляду діаграм у браузері.
  - *Контейнерна діаграма композиції (6.1)*: C4 Container Level — клієнт, шлюз, моноліт та цільові сервіси з портами, протоколами на стрілках і назвами баз даних.
  - *Діаграми послідовностей (6.2)*: порівняння сценарію оформлення замовлення у поточному моноліті (ЛР0) та в цільовій системі мікросервісів з міжсервісним мережевим викликом.

### Таблиця інвентаризації моноліту (Завдання 4.1)

| Контролер / Модуль | Сутності | Таблиці БД | Хто ще їх читає / пише (міжмодульні залежності) |
| :--- | :--- | :--- | :--- |
| **AuthController** (`/api/auth/*`) | `User`, `UserRole` | `Users` | **Orders**: читає `UserId` для прив'язки замовлення. **ApiGateway**: читає claims для авторизації. |
| **CategoriesEndpoints** (`/api/categories/*`) | `Category` | `Categories` | **Products**: читає `CategoryId` при валідації та створенні товару (`FK_Products_Categories`). |
| **ProductsEndpoints** (`/api/products/*`) | `Product` | `Products` | **Orders**: читає ціну `Price` та **зменшує (пише)** `StockQuantity` при оформленні замовлення. |
| **OrdersEndpoints** (`/api/orders/*`) | `Order`, `OrderItem`, `OrderStatus` | `Orders`, `OrderItems` | Читає покупець та адміністратор. Списує залишки в `Products`. Зовнішні ключі на `Users` та `Products`. |

---

## 3. Стек технологій

- **Платформа:** .NET 10 SDK (`net10.0`, C# 14)
- **API Gateway:** Ocelot 25.0.0
- **Web API:** ASP.NET Core Minimal APIs
- **ORM / СУБД:** Entity Framework Core 10, PostgreSQL (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- **Валідація:** FluentValidation з кастомним `ValidationFilter<T>`
- **Аутентифікація:** In-House JWT Bearer, PBKDF2 Password Hashing (`PasswordHasher<User>`)
- **Документація:** Swagger UI (`Swashbuckle.AspNetCore`) з підтримкою JWT Bearer

---

## 4. Порти та запуск

| Застосунок | Порт | URL перевірки |
| :--- | :--- | :--- |
| **ApiGateway** | `5000` | `http://localhost:5000/health` |
| **Monolith** | `5100` | `http://localhost:5100/swagger` |

### 4.1. Запуск моноліту (порт 5100)
```bash
dotnet run --project src/Monolith --launch-profile http
```

### 4.2. Запуск шлюзу (порт 5000)
```bash
dotnet run --project src/ApiGateway --launch-profile http
```

---

## 5. Приклади запитів через шлюз (порт 5000)

Усі клієнтські запити виконуються виключно через шлюз на порт **5000**:

1. **Перевірка стану шлюзу (GET):**
```bash
curl -X GET http://localhost:5000/health
```

2. **Автентифікація / отримання JWT токена (POST):**
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "admin@shop.com", "password": "AdminPassword123!"}'
```

3. **Отримання списку категорій каталогу (GET):**
```bash
curl -X GET http://localhost:5000/api/categories
```

4. **Оформлення замовлення із наскрізним ідентифікатором (POST):**
```bash
curl -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <ВАШ_ТОКЕН>" \
  -H "X-Correlation-Id: custom-trace-12345" \
  -d '{"items": [{"productId": "<PRODUCT_GUID>", "quantity": 1}]}'
```

---

## 6. Порядок перевірки роботи на захисті (Розділ 5 методички)

1. **Запуск обох застосунків:** моноліт на порту `5100`, шлюз на порту `5000`.
   - `GET http://localhost:5000/health` повертає `200 OK` (`{"service":"ApiGateway","status":"Healthy","port":5000}`).
2. **Порівняння прямих запитів та запитів через шлюз:**
   - `GET http://localhost:5100/api/categories` та `GET http://localhost:5000/api/categories` повертають ідентичний JSON.
3. **Логування у консолі шлюзу:**
   - У логах шлюзу фіксується: correlation id, HTTP-метод, шлях, адреса отримувача (downstream), статус-код та час виконання:
     `[trace-id] GET /api/categories -> http://localhost:5100 -> 200 (12 ms)`
4. **Перевірка 3 різних HTTP-методів:**
   - `GET /api/categories` (перегляд каталогу)
   - `POST /api/auth/login` (автентифікація)
   - `POST /api/orders` (оформлення замовлення)
5. **Пріоритет явних маршрутів над перехоплювачем:**
   - Явні маршрути `/api/auth/{everything}` та `/api/categories/{everything}` розміщені вище перехоплювача `/api/{everything}` в `ocelot.json`.
   - Зміна порту в маршруті `/api/categories/{everything}` на неіснуючий призведе до `502 Bad Gateway`, що підтверджує: Ocelot обирає перший збіг зверху вниз, а не загальний перехоплювач.
6. **Зупинка моноліту:**
   - Якщо процес `Monolith` зупинено, шлюз на порті `5000` не падає, а штатно повертає клієнту `502 Bad Gateway` або `ConnectionToDownstreamServiceError`.
7. **Документація:**
   - У теці `docs/adr/` створено `ADR-0001.md` та `ADR-0002.md`.
   - У теці `docs/diagrams/` створено `architecture.md` з діаграмою композиції та 2 діаграмами послідовностей.
