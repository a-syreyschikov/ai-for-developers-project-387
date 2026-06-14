# ADR 0002: Weekly Schedule Availability

The MVP models owner availability as one weekly `Schedule` in the owner's time zone instead of storing concrete availability windows. This matches the owner UI, where availability is edited as recurring weekday rules, while the backend still returns concrete free UTC slots to guests for the strict 14-day booking window.

## Status

Accepted.

## Consequences

- `Schedule` is the public owner-facing availability model in the API.
- Public slot generation expands the weekly schedule into concrete UTC slots using `Owner.timeZone`.
- Existing bookings are not changed when the owner updates the schedule; the new schedule only affects future slot generation.
- One interval per weekday is supported in the MVP; exceptions and multiple intervals per day are out of scope.
