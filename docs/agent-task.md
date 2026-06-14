# Задача агентам

## Общий контекст

Проект выполняется в подходе Design First. TypeSpec-спецификация в `contracts/api/main.tsp` является единым источником правды для frontend и backend. Сгенерированный `contracts/openapi.yaml` коммитится как читаемый артефакт для ревью и интеграции.

Перед изменением frontend или backend нужно свериться с контрактом и доменной моделью в `docs/domain.md`. Если реализация требует изменить поведение API, сначала меняется TypeSpec-контракт, затем регенерируется OpenAPI и обновляются тесты.

## Contract agent

Поддерживай `contracts/api/main.tsp`, `contracts/openapi.yaml` и `tests/contract.test.mjs` синхронными. Команда `make test` должна генерировать OpenAPI, запускать contract tests и проверять coverage threshold 80% statements.

При добавлении endpoints фиксируй:

- модели request/response;
- HTTP statuses;
- `operationId`;
- теги;
- доменные ошибки через `ProblemDetails.code`;
- examples для будущих implementation agents.

## Backend agent

Будущий backend стек: .NET 8 + C#.

Backend должен реализовывать API из `contracts/openapi.yaml` без добавления неописанных полей или статусов. Все timestamps хранятся и сравниваются в UTC. Авторизации в MVP нет: `/api/owner/*` использует единственный преднастроенный профиль владельца.

Особое внимание:

- полуоткрытые интервалы `[startsAt, endsAt)`;
- запрет любых пересечений scheduled бронирований;
- генерация слотов из недельного расписания владельца в `Owner.timeZone`;
- строгая граница окна записи `now < startsAt < now + 14 days`;
- идемпотентная отмена будущих бронирований владельцем.

## Frontend agent

Будущий frontend стек: Vite + Vue + TypeScript.

Frontend должен строить типы API от `contracts/openapi.yaml` или вручную соответствовать ему без расхождений. Гость видит публичные endpoints, владелец использует `/api/owner/*`. В MVP нет login/logout и личных кабинетов.

Особое внимание:

- показывать время с учетом `Owner.timeZone`;
- не создавать слот локально, если backend не вернул его в public slots API;
- корректно обрабатывать `409 SLOT_UNAVAILABLE` и `422 SLOT_OUTSIDE_BOOKING_WINDOW`;
- owner upcoming list показывает только будущие scheduled звонки;
- owner schedule редактируется как недельный шаблон с одним интервалом на день.
