### Hexlet tests and linter status:
[![Actions Status](https://github.com/a-syreyschikov/ai-for-developers-project-386/actions/workflows/hexlet-check.yml/badge.svg)](https://github.com/a-syreyschikov/ai-for-developers-project-386/actions)

# Calendar

`calendar` — учебный сервис записи на звонок по мотивам Cal.com.

Проект выполняется в подходе Design First: сначала фиксируются доменная модель и API-контракт, затем отдельно реализуются frontend и backend.

## Деплой

Публичная ссылка: <a href="https://calendar-app-ov28.onrender.com" target="_blank" rel="noopener noreferrer">https://calendar-app-ov28.onrender.com</a>

## Render MCP

`opencode.json` ожидает Render MCP token в переменной окружения `RENDER_MCP_TOKEN`. Реальный `.env` не коммитится; шаблон доступен в `.env.example`.

```bash
export RENDER_MCP_TOKEN=your_render_mcp_token
opencode
```

## Стек

- API-контракт: TypeSpec + OpenAPI 3.0.
- Contract tests: `node:test` + `c8`.
- E2E tests: Playwright + Docker Compose.
- Backend: .NET 8 + ASP.NET Core Minimal API + C#, запуск и тесты через Docker.
- Frontend: Vite + Vue + TypeScript.
- Релизы: Conventional Commits + release-please.

## Структура

- `contracts/api/main.tsp` — источник правды API.
- `contracts/openapi.yaml` — сгенерированный OpenAPI YAML, коммитится для ревью и интеграции.
- `docs/domain.md` — доменные сущности и правила.
- `docs/e2e-scenarios.md` — пользовательские сценарии для интеграционных проверок.
- `docs/agent-task.md` — инструкции будущим агентам.
- `docs/adr/` — архитектурные решения.
- `apps/backend/` — backend на .NET 8 + ASP.NET Core Minimal API.
- `apps/frontend/` — место под frontend на Vite + Vue + TypeScript.
- `tests/contract.test.mjs` — тесты API-контракта.

## Команды

```bash
make install
make contract
make test
make test-coverage
make test-e2e
make backend-build
make backend-run
make backend-test
make compose-build
make compose-up
make compose-down
make docker-build
make docker-run
```

`make` по умолчанию запускает `make test`.

## Интеграционные тесты

Playwright проверяет основной путь бронирования в реальном браузере против frontend и backend, поднятых через Docker Compose.

```bash
make test-e2e
```

Сценарии описаны в `docs/e2e-scenarios.md`. Первый обязательный сценарий проходит путь Guest от выбора EventType и Slot до создания Booking и проверяет, что Owner видит Booking в `/admin/upcoming`.

## Запуск приложения

Production-сценарий собирает один Docker-образ: backend запускает API, отдает собранный frontend и слушает порт из переменной окружения `PORT`.

```bash
make docker-build
PORT=8090 make docker-run
```

Приложение будет доступно на `http://localhost:8090`.

Локальный dev-сценарий: backend запускается через Docker, frontend запускается через Vite и проксирует `/api` на `http://localhost:8080`.

```bash
make backend-build
make backend-run
```

В другом терминале:

```bash
cd apps/frontend
npm ci
npm run dev
```

Compose-сценарий запускает backend и frontend одной командой:

```bash
make compose-up
```

Frontend будет доступен на `http://localhost:5173`, backend - на `http://localhost:8080`. Если порты заняты, их можно переопределить: `FRONTEND_PORT=5174 BACKEND_PORT=8081 make compose-up`.

Данные backend хранятся в памяти и сбрасываются при перезапуске сервиса.

## Правила разработки

- TypeSpec является единым источником правды для frontend и backend.
- Любое изменение API сначала вносится в `contracts/api/main.tsp`.
- После изменения контракта нужно выполнить `make test` и закоммитить обновленный `contracts/openapi.yaml`.
- Backend реализуется от контракта и запускается через Docker, без локальной установки `dotnet`.
- Frontend вызывает `/api`; в dev и compose запросы проксируются на backend.
- `make test` проверяет contract-test tooling с coverage threshold 80% statements.
- `make backend-test` запускает backend integration tests через Docker с coverage threshold 80% lines.
- `make test-e2e` запускает Playwright против Docker Compose приложения.
- Тесты frontend/backend частей добавляются вместе с реализацией соответствующего поведения.

## Релизы и коммиты

Новые коммиты пишутся в формате Conventional Commits, например `feat: добавить e2e-проверки и автоматизацию релизов`. Правила описаны в `CONTRIBUTING.md`.

После merge в `main` workflow `release-please` создает или обновляет release PR с changelog и предложенной версией. Релиз ведется одной версией для всего репозитория через root `package.json`.
