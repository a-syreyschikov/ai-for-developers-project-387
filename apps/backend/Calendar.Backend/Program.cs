using System.Globalization;
using System.Net.Mail;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
builder.WebHost.UseUrls($"http://0.0.0.0:{(string.IsNullOrWhiteSpace(port) ? "8080" : port)}");

builder.Services.ConfigureHttpJsonOptions(options =>
{
  options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});
builder.Services.AddCors(options =>
{
  options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<CalendarStore>();

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
  errorApp.Run(async context =>
  {
    var exception = context.Features.Get<IExceptionHandlerPathFeature>()?.Error;
    var statusCode = exception is BadHttpRequestException or JsonException
      ? StatusCodes.Status400BadRequest
      : StatusCodes.Status500InternalServerError;

    var problem = statusCode == StatusCodes.Status400BadRequest
      ? Problems.ValidationFailed("Запрос не соответствует ожидаемой JSON-структуре.")
      : Problems.UnexpectedError();

    context.Response.StatusCode = statusCode;
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsJsonAsync(problem);
  });
});

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors();

app.MapGet("/api/public/owner", (CalendarStore store) => store.GetOwner());
app.MapGet("/api/owner", (CalendarStore store) => store.GetOwner());

app.MapGet("/api/public/event-types", (CalendarStore store) => new EventTypeList(store.ListEventTypes()));
app.MapGet("/api/owner/event-types", (CalendarStore store) => new EventTypeList(store.ListEventTypes()));

app.MapGet("/api/public/event-types/{eventTypeId}", (string eventTypeId, CalendarStore store) =>
  ToHttpResult(store.GetEventType(eventTypeId)));

app.MapGet("/api/public/event-types/{eventTypeId}/slots", (string eventTypeId, CalendarStore store) =>
  ToHttpResult(store.ListSlots(eventTypeId)));

app.MapPost("/api/owner/event-types", (CreateEventTypeRequest? request, CalendarStore store) =>
  ToHttpResult(store.CreateEventType(request), StatusCodes.Status201Created));

app.MapGet("/api/owner/schedule", (CalendarStore store) => store.GetSchedule());

app.MapPut("/api/owner/schedule", (UpdateScheduleRequest? request, CalendarStore store) =>
  ToHttpResult(store.UpdateSchedule(request)));

app.MapPost("/api/public/bookings", (CreateBookingRequest? request, CalendarStore store) =>
  ToHttpResult(store.CreateBooking(request), StatusCodes.Status201Created));

app.MapGet("/api/owner/bookings/upcoming", (CalendarStore store) => new BookingList(store.ListUpcomingBookings()));

app.MapPost("/api/owner/bookings/{bookingId}/cancel", (string bookingId, CalendarStore store) =>
  ToHttpResult(store.CancelBooking(bookingId)));

app.MapFallbackToFile("index.html");

app.Run();

static IResult ToHttpResult<T>(ApiResult<T> result, int successStatusCode = StatusCodes.Status200OK) =>
  result.Problem is null
    ? Results.Json(result.Value, statusCode: successStatusCode)
    : Results.Json(result.Problem, statusCode: result.Problem.Status);

public sealed class CalendarStore(IClock clock)
{
  private static readonly Guid OwnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
  private static readonly Guid SeedEventTypeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
  private static readonly string[] Weekdays = ["monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday"];
  private static readonly Regex LocalTimePattern = new("^([01][0-9]|2[0-3]):(00|15|30|45)$", RegexOptions.Compiled);
  private static readonly StringComparer RussianTitleComparer = StringComparer.Create(new CultureInfo("ru-RU"), ignoreCase: false);

  private readonly object gate = new();
  private readonly List<EventType> eventTypes =
  [
    new EventType(
      SeedEventTypeId,
      "Вводная встреча",
      "Короткая встреча для знакомства и обсуждения задачи.",
      30)
  ];
  private readonly List<Booking> bookings = [];
  private Schedule schedule = CreateDefaultSchedule();

  public Owner GetOwner() => new(OwnerId, "Алексей Петров", "Europe/Moscow");

  public IReadOnlyList<EventType> ListEventTypes()
  {
    lock (gate)
    {
      return eventTypes
        .OrderBy(eventType => eventType.Title, RussianTitleComparer)
        .ToList();
    }
  }

  public ApiResult<EventType> GetEventType(string eventTypeId)
  {
    if (!Guid.TryParse(eventTypeId, out var id))
    {
      return ApiResult<EventType>.Failure(Problems.EventTypeNotFound());
    }

    lock (gate)
    {
      var eventType = eventTypes.FirstOrDefault(item => item.Id == id);
      return eventType is null
        ? ApiResult<EventType>.Failure(Problems.EventTypeNotFound())
        : ApiResult<EventType>.Success(eventType);
    }
  }

  public ApiResult<EventType> CreateEventType(CreateEventTypeRequest? request)
  {
    if (request is null)
    {
      return ApiResult<EventType>.Failure(Problems.ValidationFailed("Передайте данные типа события."));
    }

    var title = request.Title?.Trim() ?? "";
    if (request.Description is null)
    {
      return ApiResult<EventType>.Failure(Problems.ValidationFailed("Описание типа события обязательно."));
    }

    var description = request.Description.Trim();

    if (title.Length is < 1 or > 120)
    {
      return ApiResult<EventType>.Failure(Problems.ValidationFailed("Название типа события должно содержать от 1 до 120 символов."));
    }

    if (description.Length > 1000)
    {
      return ApiResult<EventType>.Failure(Problems.ValidationFailed("Описание типа события должно содержать не больше 1000 символов."));
    }

    if (request.DurationMinutes is null or < 15 or > 240 || request.DurationMinutes % 15 != 0)
    {
      return ApiResult<EventType>.Failure(Problems.ValidationFailed("Длительность типа события должна быть от 15 до 240 минут и кратна 15."));
    }

    lock (gate)
    {
      var normalizedTitle = NormalizeTitle(title);
      if (eventTypes.Any(eventType => NormalizeTitle(eventType.Title) == normalizedTitle))
      {
        return ApiResult<EventType>.Failure(Problems.DuplicateEventTypeTitle());
      }

      var eventType = new EventType(Guid.NewGuid(), title, description, request.DurationMinutes.Value);
      eventTypes.Add(eventType);
      return ApiResult<EventType>.Success(eventType);
    }
  }

  public Schedule GetSchedule()
  {
    lock (gate)
    {
      return CloneSchedule(schedule);
    }
  }

  public ApiResult<Schedule> UpdateSchedule(UpdateScheduleRequest? request)
  {
    if (request?.Days is null)
    {
      return ApiResult<Schedule>.Failure(Problems.ValidationFailed("Передайте расписание владельца."));
    }

    var validationProblem = ValidateSchedule(request.Days);
    if (validationProblem is not null)
    {
      return ApiResult<Schedule>.Failure(validationProblem);
    }

    var updated = new Schedule(request.Days.Select(day => new ScheduleDay(
      day.Weekday!,
      day.Enabled!.Value,
      day.Enabled.Value ? day.StartsAtLocalTime : null,
      day.Enabled.Value ? day.EndsAtLocalTime : null)).ToList());

    lock (gate)
    {
      schedule = CloneSchedule(updated);
      return ApiResult<Schedule>.Success(CloneSchedule(schedule));
    }
  }

  public ApiResult<SlotList> ListSlots(string eventTypeId)
  {
    if (!Guid.TryParse(eventTypeId, out var id))
    {
      return ApiResult<SlotList>.Failure(Problems.EventTypeNotFound());
    }

    lock (gate)
    {
      var eventType = eventTypes.FirstOrDefault(item => item.Id == id);
      if (eventType is null)
      {
        return ApiResult<SlotList>.Failure(Problems.EventTypeNotFound());
      }

      return ApiResult<SlotList>.Success(new SlotList(ListSlotsForEventTypeLocked(eventType, clock.UtcNow())));
    }
  }

  public ApiResult<Booking> CreateBooking(CreateBookingRequest? request)
  {
    if (request is null)
    {
      return ApiResult<Booking>.Failure(Problems.ValidationFailed("Передайте данные бронирования."));
    }

    if (!Guid.TryParse(request.EventTypeId, out var eventTypeId))
    {
      return ApiResult<Booking>.Failure(Problems.ValidationFailed("Передайте корректный идентификатор типа события."));
    }

    if (!TryParseUtcMinute(request.StartsAt, out var startsAt))
    {
      return ApiResult<Booking>.Failure(Problems.ValidationFailed("Время начала должно быть UTC timestamp с суффиксом Z и нулевыми секундами."));
    }

    var guestName = request.GuestName?.Trim() ?? "";
    var guestEmail = request.GuestEmail?.Trim().ToLowerInvariant() ?? "";

    if (guestName.Length is < 1 or > 120)
    {
      return ApiResult<Booking>.Failure(Problems.ValidationFailed("Имя гостя должно содержать от 1 до 120 символов."));
    }

    if (!IsValidEmail(guestEmail))
    {
      return ApiResult<Booking>.Failure(Problems.ValidationFailed("Email гостя должен быть корректным и содержать не больше 254 символов."));
    }

    lock (gate)
    {
      var eventType = eventTypes.FirstOrDefault(item => item.Id == eventTypeId);
      if (eventType is null)
      {
        return ApiResult<Booking>.Failure(Problems.EventTypeNotFound());
      }

      var now = clock.UtcNow();
      var endsAt = startsAt.AddMinutes(eventType.DurationMinutes);
      if (!IsInsideBookingWindow(startsAt, now))
      {
        return ApiResult<Booking>.Failure(Problems.SlotOutsideBookingWindow());
      }

      if (bookings.Any(booking => booking.Status == BookingStatus.Scheduled && IntervalsOverlap(startsAt, endsAt, booking.StartsAt, booking.EndsAt)))
      {
        return ApiResult<Booking>.Failure(Problems.SlotUnavailable());
      }

      var selectedSlot = ListSlotsForEventTypeLocked(eventType, now).FirstOrDefault(slot => slot.StartsAt == startsAt);
      if (selectedSlot is null)
      {
        return ApiResult<Booking>.Failure(Problems.SlotOutsideBookingWindow("Выбранное время не является доступным слотом для этого типа события."));
      }

      var booking = new Booking
      {
        Id = Guid.NewGuid(),
        EventTypeId = eventType.Id,
        EventTypeTitle = eventType.Title,
        DurationMinutes = eventType.DurationMinutes,
        StartsAt = selectedSlot.StartsAt,
        EndsAt = selectedSlot.EndsAt,
        GuestName = guestName,
        GuestEmail = guestEmail,
        Status = BookingStatus.Scheduled,
        CreatedAt = TruncateToMinute(now)
      };
      bookings.Add(booking);
      return ApiResult<Booking>.Success(booking);
    }
  }

  public IReadOnlyList<Booking> ListUpcomingBookings()
  {
    var now = clock.UtcNow();
    lock (gate)
    {
      return bookings
        .Where(booking => booking.Status == BookingStatus.Scheduled && booking.StartsAt > now)
        .OrderBy(booking => booking.StartsAt)
        .ToList();
    }
  }

  public ApiResult<Booking> CancelBooking(string bookingId)
  {
    if (!Guid.TryParse(bookingId, out var id))
    {
      return ApiResult<Booking>.Failure(Problems.BookingNotFound());
    }

    lock (gate)
    {
      var booking = bookings.FirstOrDefault(item => item.Id == id);
      if (booking is null)
      {
        return ApiResult<Booking>.Failure(Problems.BookingNotFound());
      }

      if (booking.Status == BookingStatus.Cancelled)
      {
        return ApiResult<Booking>.Success(booking);
      }

      var now = clock.UtcNow();
      if (booking.StartsAt <= now)
      {
        return ApiResult<Booking>.Failure(Problems.BookingNotCancellable());
      }

      booking.Status = BookingStatus.Cancelled;
      booking.CancelledAt = TruncateToMinute(now);
      return ApiResult<Booking>.Success(booking);
    }
  }

  private static Schedule CreateDefaultSchedule() => new(Weekdays.Select(weekday =>
  {
    var enabled = weekday is not ("saturday" or "sunday");
    return new ScheduleDay(weekday, enabled, enabled ? "09:00" : null, enabled ? "18:00" : null);
  }).ToList());

  private static Schedule CloneSchedule(Schedule source) => new(source.Days.Select(day => day with { }).ToList());

  private IReadOnlyList<Slot> ListSlotsForEventTypeLocked(EventType eventType, DateTime now)
  {
    var owner = GetOwner();
    var timeZone = TimeZoneInfo.FindSystemTimeZoneById(owner.TimeZone);
    var windowEnd = now.AddDays(14);
    var localStartDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(now, timeZone));
    var localEndDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(windowEnd, timeZone));
    var result = new List<Slot>();

    for (var date = localStartDate; date <= localEndDate; date = date.AddDays(1))
    {
      var day = schedule.Days.FirstOrDefault(item => item.Weekday == WeekdayFor(date));
      if (day is not { Enabled: true } || day.StartsAtLocalTime is null || day.EndsAtLocalTime is null)
      {
        continue;
      }

      var intervalStartsAt = LocalDateTimeToUtc(date, day.StartsAtLocalTime, timeZone);
      var intervalEndsAt = LocalDateTimeToUtc(date, day.EndsAtLocalTime, timeZone);
      for (var startsAt = intervalStartsAt; startsAt.AddMinutes(eventType.DurationMinutes) <= intervalEndsAt; startsAt = startsAt.AddMinutes(eventType.DurationMinutes))
      {
        var endsAt = startsAt.AddMinutes(eventType.DurationMinutes);
        if (!IsInsideBookingWindow(startsAt, now))
        {
          continue;
        }

        var blocked = bookings.Any(booking => booking.Status == BookingStatus.Scheduled && IntervalsOverlap(startsAt, endsAt, booking.StartsAt, booking.EndsAt));
        if (!blocked)
        {
          result.Add(new Slot(eventType.Id, startsAt, endsAt, eventType.DurationMinutes));
        }
      }
    }

    return result.OrderBy(slot => slot.StartsAt).ToList();
  }

  private static ProblemDetailsResponse? ValidateSchedule(IReadOnlyList<ScheduleDayRequest> days)
  {
    if (days.Count != 7)
    {
      return Problems.InvalidSchedule("Расписание должно содержать ровно семь дней.");
    }

    for (var index = 0; index < days.Count; index += 1)
    {
      var day = days[index];
      if (day.Weekday != Weekdays[index])
      {
        return Problems.InvalidSchedule("Дни расписания должны идти с понедельника по воскресенье.");
      }

      if (day.Enabled is null)
      {
        return Problems.InvalidSchedule("Для каждого дня нужно указать признак доступности.");
      }

      if (!day.Enabled.Value)
      {
        if (day.StartsAtLocalTime is not null || day.EndsAtLocalTime is not null)
        {
          return Problems.InvalidSchedule("У выключенного дня начало и конец должны быть пустыми.");
        }

        continue;
      }

      if (!IsValidLocalTime(day.StartsAtLocalTime) || !IsValidLocalTime(day.EndsAtLocalTime))
      {
        return Problems.InvalidSchedule("Для включенного дня нужно указать начало и конец в формате HH:mm с шагом 15 минут.");
      }

      var startsAt = ParseLocalTime(day.StartsAtLocalTime!);
      var endsAt = ParseLocalTime(day.EndsAtLocalTime!);
      if (endsAt <= startsAt)
      {
        return Problems.InvalidSchedule("Время окончания должно быть позже времени начала.");
      }
    }

    return null;
  }

  private static DateTime LocalDateTimeToUtc(DateOnly date, string localTime, TimeZoneInfo timeZone)
  {
    var localDateTime = date.ToDateTime(TimeOnly.ParseExact(localTime, "HH:mm", CultureInfo.InvariantCulture), DateTimeKind.Unspecified);
    return TimeZoneInfo.ConvertTimeToUtc(localDateTime, timeZone);
  }

  private static string WeekdayFor(DateOnly date) => date.DayOfWeek switch
  {
    DayOfWeek.Monday => "monday",
    DayOfWeek.Tuesday => "tuesday",
    DayOfWeek.Wednesday => "wednesday",
    DayOfWeek.Thursday => "thursday",
    DayOfWeek.Friday => "friday",
    DayOfWeek.Saturday => "saturday",
    DayOfWeek.Sunday => "sunday",
    _ => throw new InvalidOperationException("Неизвестный день недели.")
  };

  private static bool IsInsideBookingWindow(DateTime startsAt, DateTime now) => startsAt > now && startsAt < now.AddDays(14);

  private static bool IntervalsOverlap(DateTime leftStartsAt, DateTime leftEndsAt, DateTime rightStartsAt, DateTime rightEndsAt) =>
    leftStartsAt < rightEndsAt && rightStartsAt < leftEndsAt;

  private static bool TryParseUtcMinute(string? value, out DateTime utc)
  {
    utc = default;
    if (value is null || !value.EndsWith('Z'))
    {
      return false;
    }

    if (!DateTimeOffset.TryParseExact(
      value,
      "yyyy-MM-dd'T'HH:mm:ss'Z'",
      CultureInfo.InvariantCulture,
      DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
      out var parsed))
    {
      return false;
    }

    if (parsed.Second != 0 || parsed.Millisecond != 0)
    {
      return false;
    }

    utc = DateTime.SpecifyKind(parsed.UtcDateTime, DateTimeKind.Utc);
    return true;
  }

  private static bool IsValidLocalTime(string? value) => value is not null && LocalTimePattern.IsMatch(value);

  private static TimeSpan ParseLocalTime(string value) => TimeSpan.ParseExact(value, "hh\\:mm", CultureInfo.InvariantCulture);

  private static bool IsValidEmail(string value)
  {
    if (value.Length is < 1 or > 254)
    {
      return false;
    }

    try
    {
      var address = new MailAddress(value);
      return address.Address == value;
    }
    catch (FormatException)
    {
      return false;
    }
  }

  private static string NormalizeTitle(string title) => title.Trim().ToUpperInvariant();

  private static DateTime TruncateToMinute(DateTime value) => new(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0, DateTimeKind.Utc);
}

public interface IClock
{
  DateTime UtcNow();
}

public sealed class SystemClock : IClock
{
  public DateTime UtcNow() => DateTime.UtcNow;
}

public sealed record ApiResult<T>(T? Value, ProblemDetailsResponse? Problem)
{
  public static ApiResult<T> Success(T value) => new(value, null);

  public static ApiResult<T> Failure(ProblemDetailsResponse problem) => new(default, problem);
}

public sealed record Owner(Guid Id, string DisplayName, string TimeZone);

public sealed record EventType(Guid Id, string Title, string Description, int DurationMinutes);

public sealed record Schedule(IReadOnlyList<ScheduleDay> Days);

public sealed record ScheduleDay(string Weekday, bool Enabled, string? StartsAtLocalTime, string? EndsAtLocalTime);

public sealed record Slot(Guid EventTypeId, DateTime StartsAt, DateTime EndsAt, int DurationMinutes);

public sealed class Booking
{
  public Guid Id { get; init; }
  public Guid EventTypeId { get; init; }
  public string EventTypeTitle { get; init; } = "";
  public int DurationMinutes { get; init; }
  public DateTime StartsAt { get; init; }
  public DateTime EndsAt { get; init; }
  public string GuestName { get; init; } = "";
  public string GuestEmail { get; init; } = "";
  public string Status { get; set; } = BookingStatus.Scheduled;
  public DateTime CreatedAt { get; init; }

  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public DateTime? CancelledAt { get; set; }
}

public static class BookingStatus
{
  public const string Scheduled = "scheduled";
  public const string Cancelled = "cancelled";
}

public sealed record CreateEventTypeRequest(string? Title, string? Description, int? DurationMinutes);

public sealed record UpdateScheduleRequest(IReadOnlyList<ScheduleDayRequest>? Days);

public sealed record ScheduleDayRequest(string? Weekday, bool? Enabled, string? StartsAtLocalTime, string? EndsAtLocalTime);

public sealed record CreateBookingRequest(string? EventTypeId, string? StartsAt, string? GuestName, string? GuestEmail);

public sealed record EventTypeList(IReadOnlyList<EventType> Items);

public sealed record SlotList(IReadOnlyList<Slot> Items);

public sealed record BookingList(IReadOnlyList<Booking> Items);

public sealed record ProblemDetailsResponse(string Type, string Title, int Status, string? Detail, string Code);

public static class Problems
{
  public static ProblemDetailsResponse ValidationFailed(string detail) => new(
    "https://calendar.local/problems/validation-failed",
    "Ошибка валидации",
    StatusCodes.Status400BadRequest,
    detail,
    "VALIDATION_FAILED");

  public static ProblemDetailsResponse EventTypeNotFound() => new(
    "https://calendar.local/problems/event-type-not-found",
    "Тип события не найден",
    StatusCodes.Status404NotFound,
    "Запрошенный тип события не существует.",
    "EVENT_TYPE_NOT_FOUND");

  public static ProblemDetailsResponse BookingNotFound() => new(
    "https://calendar.local/problems/booking-not-found",
    "Бронирование не найдено",
    StatusCodes.Status404NotFound,
    "Запрошенное бронирование не существует.",
    "BOOKING_NOT_FOUND");

  public static ProblemDetailsResponse DuplicateEventTypeTitle() => new(
    "https://calendar.local/problems/duplicate-event-type-title",
    "Тип события уже существует",
    StatusCodes.Status409Conflict,
    "Тип события с таким названием уже есть.",
    "DUPLICATE_EVENT_TYPE_TITLE");

  public static ProblemDetailsResponse InvalidSchedule(string detail) => new(
    "https://calendar.local/problems/invalid-schedule",
    "Некорректное расписание",
    StatusCodes.Status422UnprocessableEntity,
    detail,
    "INVALID_SCHEDULE");

  public static ProblemDetailsResponse SlotUnavailable() => new(
    "https://calendar.local/problems/slot-unavailable",
    "Слот уже занят",
    StatusCodes.Status409Conflict,
    "Выбранное время уже пересекается с существующим бронированием.",
    "SLOT_UNAVAILABLE");

  public static ProblemDetailsResponse SlotOutsideBookingWindow(string? detail = null) => new(
    "https://calendar.local/problems/slot-outside-booking-window",
    "Слот недоступен для записи",
    StatusCodes.Status422UnprocessableEntity,
    detail ?? "Выбранное время находится вне окна записи.",
    "SLOT_OUTSIDE_BOOKING_WINDOW");

  public static ProblemDetailsResponse BookingNotCancellable() => new(
    "https://calendar.local/problems/booking-not-cancellable",
    "Бронирование нельзя отменить",
    StatusCodes.Status422UnprocessableEntity,
    "Можно отменить только будущее scheduled бронирование.",
    "BOOKING_NOT_CANCELLABLE");

  public static ProblemDetailsResponse UnexpectedError() => new(
    "https://calendar.local/problems/unexpected-error",
    "Внутренняя ошибка сервера",
    StatusCodes.Status500InternalServerError,
    "Повторите запрос позже.",
    "INTERNAL_ERROR");
}

public partial class Program;
