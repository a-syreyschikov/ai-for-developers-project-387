# Contributing

## Conventional Commits

Новые коммиты пишутся по спецификации Conventional Commits. Это нужно для `release-please`: инструмент читает историю коммитов и поддерживает release PR с changelog и новой версией.

Формат:

```text
type(scope): описание
```

Scope необязателен. `type` и `scope` пишутся на английском, описание можно писать на русском.

Примеры:

```text
feat: добавить e2e-проверки и автоматизацию релизов
fix(booking): обработать занятый слот
test(e2e): покрыть основной сценарий бронирования
ci: добавить запуск Playwright в GitHub Actions
docs: описать пользовательские сценарии
```

Основные типы:

- `feat` - новая пользовательская или проектная возможность.
- `fix` - исправление ошибки.
- `test` - тесты без изменения production-поведения.
- `ci` - CI/CD и GitHub Actions.
- `docs` - документация.
- `chore` - обслуживание без изменения поведения.
- `refactor` - изменение структуры без изменения поведения.

Breaking changes отмечаются через `!` или footer `BREAKING CHANGE:`.

## Релизы

Релизы ведутся одной версией для всего репозитория. После merge в `main` workflow `release-please` создает или обновляет release PR. В release PR должны появиться обновления `CHANGELOG.md`, root `package.json` и root `package-lock.json`.

Старую историю коммитов не переписываем. Conventional Commits применяются к новым коммитам.
