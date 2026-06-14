using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Calendar.Backend.Tests;

public sealed class CalendarApiTests
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
  private static readonly DateTime FixedNow = Utc(2026, 6, 15, 5, 0);
  private static readonly Guid SeedEventTypeId = Guid.Parse("22222222-2222-2222-2222-222222222222");

  [Fact]
  public async Task OwnerEndpointsReturnSamePreconfiguredOwner()
  {
    await using var factory = CreateFactory();
    using var client = factory.CreateClient();

    var publicOwnerResponse = await client.GetAsync("/api/public/owner");
    var ownerResponse = await client.GetAsync("/api/owner");

    var publicOwner = await ReadJson<OwnerDto>(publicOwnerResponse);
    var owner = await ReadJson<OwnerDto>(ownerResponse);

    Assert.Equal(HttpStatusCode.OK, publicOwnerResponse.StatusCode);
    Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);
    Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), publicOwner.Id);
    Assert.Equal("Алексей Петров", publicOwner.DisplayName);
    Assert.Equal("Europe/Moscow", publicOwner.TimeZone);
    Assert.Equal(publicOwner, owner);
  }

  [Fact]
  public async Task EventTypeEndpointsListSortAndReturnNotFoundErrors()
  {
    await using var factory = CreateFactory();
    using var client = factory.CreateClient();

    var created = await CreateEventType(client, "Анализ задачи", "", 45);
    var publicList = await ReadJson<EventTypeListDto>(await client.GetAsync("/api/public/event-types"));
    var ownerList = await ReadJson<EventTypeListDto>(await client.GetAsync("/api/owner/event-types"));
    var seedResponse = await client.GetAsync($"/api/public/event-types/{SeedEventTypeId}");
    var invalidResponse = await client.GetAsync("/api/public/event-types/not-a-uuid");
    var unknownResponse = await client.GetAsync("/api/public/event-types/99999999-9999-9999-9999-999999999999");

    var seed = await ReadJson<EventTypeDto>(seedResponse);
    await AssertProblem(invalidResponse, HttpStatusCode.NotFound, "EVENT_TYPE_NOT_FOUND");
    await AssertProblem(unknownResponse, HttpStatusCode.NotFound, "EVENT_TYPE_NOT_FOUND");

    Assert.Equal(HttpStatusCode.OK, seedResponse.StatusCode);
    Assert.Equal(SeedEventTypeId, seed.Id);
    Assert.Equal("Вводная встреча", seed.Title);
    Assert.Equal(publicList.Items.Select(item => item.Id), ownerList.Items.Select(item => item.Id));
    Assert.Equal(created.Id, publicList.Items[0].Id);
    Assert.Equal(SeedEventTypeId, publicList.Items[1].Id);
  }

  [Fact]
  public async Task CreateEventTypeValidatesContractRules()
  {
    await using var factory = CreateFactory();
    using var client = factory.CreateClient();

    var createdResponse = await client.PostAsJsonAsync("/api/owner/event-types", new
    {
      title = "  Консультация  ",
      description = "",
      durationMinutes = 60
    }, JsonOptions);
    var missingDescriptionResponse = await client.PostAsJsonAsync("/api/owner/event-types", new
    {
      title = "Без описания",
      durationMinutes = 30
    }, JsonOptions);
    var nullDescriptionResponse = await client.PostAsJsonAsync("/api/owner/event-types", new
    {
      title = "Пустое описание",
      description = (string?)null,
      durationMinutes = 30
    }, JsonOptions);
    var invalidDurationResponse = await client.PostAsJsonAsync("/api/owner/event-types", new
    {
      title = "Некорректная длительность",
      description = "Описание",
      durationMinutes = 10
    }, JsonOptions);
    var duplicateResponse = await client.PostAsJsonAsync("/api/owner/event-types", new
    {
      title = "  вводная встреча  ",
      description = "Дубликат",
      durationMinutes = 30
    }, JsonOptions);

    var created = await ReadJson<EventTypeDto>(createdResponse);

    Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
    Assert.Equal("Консультация", created.Title);
    Assert.Equal("", created.Description);
    Assert.Equal(60, created.DurationMinutes);
    await AssertProblem(missingDescriptionResponse, HttpStatusCode.BadRequest, "VALIDATION_FAILED");
    await AssertProblem(nullDescriptionResponse, HttpStatusCode.BadRequest, "VALIDATION_FAILED");
    await AssertProblem(invalidDurationResponse, HttpStatusCode.BadRequest, "VALIDATION_FAILED");
    await AssertProblem(duplicateResponse, HttpStatusCode.Conflict, "DUPLICATE_EVENT_TYPE_TITLE");
  }

  [Fact]
  public async Task ScheduleEndpointsReturnDefaultAndValidateReplacementRules()
  {
    await using var factory = CreateFactory();
    using var client = factory.CreateClient();

    var defaultScheduleResponse = await client.GetAsync("/api/owner/schedule");
    var defaultSchedule = await ReadJson<ScheduleDto>(defaultScheduleResponse);

    Assert.Equal(HttpStatusCode.OK, defaultScheduleResponse.StatusCode);
    Assert.Equal(Weekdays, defaultSchedule.Days.Select(day => day.Weekday));
    Assert.All(defaultSchedule.Days.Take(5), day =>
    {
      Assert.True(day.Enabled);
      Assert.Equal("09:00", day.StartsAtLocalTime);
      Assert.Equal("18:00", day.EndsAtLocalTime);
    });
    Assert.All(defaultSchedule.Days.Skip(5), day =>
    {
      Assert.False(day.Enabled);
      Assert.Null(day.StartsAtLocalTime);
      Assert.Null(day.EndsAtLocalTime);
    });

    var replacementDays = MondayOnlySchedule("10:00", "11:00");
    var updateResponse = await client.PutAsJsonAsync("/api/owner/schedule", new { days = replacementDays }, JsonOptions);
    var updatedSchedule = await ReadJson<ScheduleDto>(updateResponse);

    Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
    Assert.True(updatedSchedule.Days[0].Enabled);
    Assert.Equal("10:00", updatedSchedule.Days[0].StartsAtLocalTime);
    Assert.Equal("11:00", updatedSchedule.Days[0].EndsAtLocalTime);
    Assert.All(updatedSchedule.Days.Skip(1), day => Assert.False(day.Enabled));

    await AssertProblem(await client.PutAsJsonAsync("/api/owner/schedule", new { }, JsonOptions), HttpStatusCode.BadRequest, "VALIDATION_FAILED");
    await AssertInvalidSchedule(client, replacementDays.Take(6).ToArray());
    await AssertInvalidSchedule(client, replacementDays.Select((day, index) => index == 0 ? day with { Weekday = "tuesday" } : day).ToArray());
    await AssertInvalidSchedule(client, replacementDays.Select((day, index) => index == 1 ? day with { StartsAtLocalTime = "09:00" } : day).ToArray());
    await AssertInvalidSchedule(client, replacementDays.Select((day, index) => index == 0 ? day with { StartsAtLocalTime = "09:10" } : day).ToArray());
    await AssertInvalidSchedule(client, replacementDays.Select((day, index) => index == 0 ? day with { StartsAtLocalTime = "11:00", EndsAtLocalTime = "10:00" } : day).ToArray());
  }

  [Fact]
  public async Task SlotsUseOwnerTimeZoneScheduleWindowSortingAndBookedFiltering()
  {
    await using var factory = CreateFactory();
    using var client = factory.CreateClient();

    await ReplaceSchedule(client, MondayOnlySchedule("09:00", "10:00"));

    var slotsResponse = await client.GetAsync($"/api/public/event-types/{SeedEventTypeId}/slots");
    var slots = await ReadJson<SlotListDto>(slotsResponse);
    var missingResponse = await client.GetAsync("/api/public/event-types/99999999-9999-9999-9999-999999999999/slots");

    Assert.Equal(HttpStatusCode.OK, slotsResponse.StatusCode);
    await AssertProblem(missingResponse, HttpStatusCode.NotFound, "EVENT_TYPE_NOT_FOUND");
    Assert.Equal([
      Utc(2026, 6, 15, 6, 0),
      Utc(2026, 6, 15, 6, 30),
      Utc(2026, 6, 22, 6, 0),
      Utc(2026, 6, 22, 6, 30)
    ], slots.Items.Select(slot => slot.StartsAt));
    Assert.All(slots.Items, slot =>
    {
      Assert.Equal(SeedEventTypeId, slot.EventTypeId);
      Assert.Equal(30, slot.DurationMinutes);
      Assert.Equal(slot.StartsAt.AddMinutes(30), slot.EndsAt);
      Assert.True(slot.StartsAt > FixedNow);
      Assert.True(slot.StartsAt < FixedNow.AddDays(14));
    });

    await CreateBooking(client, SeedEventTypeId, "2026-06-15T06:00:00Z");

    var filteredSlots = await ReadJson<SlotListDto>(await client.GetAsync($"/api/public/event-types/{SeedEventTypeId}/slots"));

    Assert.Equal([
      Utc(2026, 6, 15, 6, 30),
      Utc(2026, 6, 22, 6, 0),
      Utc(2026, 6, 22, 6, 30)
    ], filteredSlots.Items.Select(slot => slot.StartsAt));
  }

  [Fact]
  public async Task CreateBookingReturnsContractResponseAndValidationErrors()
  {
    await using var factory = CreateFactory();
    using var client = factory.CreateClient();

    var created = await CreateBooking(client, SeedEventTypeId, "2026-06-15T06:00:00Z", "  Иван Иванов  ", "IVAN@EXAMPLE.COM");

    Assert.Equal(SeedEventTypeId, created.EventTypeId);
    Assert.Equal("Вводная встреча", created.EventTypeTitle);
    Assert.Equal(30, created.DurationMinutes);
    Assert.Equal(Utc(2026, 6, 15, 6, 0), created.StartsAt);
    Assert.Equal(Utc(2026, 6, 15, 6, 30), created.EndsAt);
    Assert.Equal("Иван Иванов", created.GuestName);
    Assert.Equal("ivan@example.com", created.GuestEmail);
    Assert.Equal("scheduled", created.Status);
    Assert.Equal(FixedNow, created.CreatedAt);
    Assert.Null(created.CancelledAt);

    await AssertProblem(await PostBooking(client, SeedEventTypeId, "2026-06-15T06:00:01Z"), HttpStatusCode.BadRequest, "VALIDATION_FAILED");
    await AssertProblem(await PostBooking(client, SeedEventTypeId, "2026-06-15T06:00:00.000Z"), HttpStatusCode.BadRequest, "VALIDATION_FAILED");
    await AssertProblem(await PostBooking(client, SeedEventTypeId, "2026-06-15T06:00:00"), HttpStatusCode.BadRequest, "VALIDATION_FAILED");
    await AssertProblem(await PostBooking(client, Guid.Parse("99999999-9999-9999-9999-999999999999"), "2026-06-15T06:30:00Z"), HttpStatusCode.NotFound, "EVENT_TYPE_NOT_FOUND");
    await AssertProblem(await PostBooking(client, SeedEventTypeId, "2026-06-15T05:00:00Z"), HttpStatusCode.UnprocessableEntity, "SLOT_OUTSIDE_BOOKING_WINDOW");
    await AssertProblem(await PostBooking(client, SeedEventTypeId, "2026-06-29T05:00:00Z"), HttpStatusCode.UnprocessableEntity, "SLOT_OUTSIDE_BOOKING_WINDOW");
    await AssertProblem(await PostBooking(client, SeedEventTypeId, "2026-06-15T06:00:00Z"), HttpStatusCode.Conflict, "SLOT_UNAVAILABLE");
  }

  [Fact]
  public async Task BookingOverlapUsesHalfOpenIntervalsAcrossEventTypes()
  {
    await using var factory = CreateFactory();
    using var client = factory.CreateClient();

    await ReplaceSchedule(client, MondayOnlySchedule("09:00", "10:30"));
    var longEventType = await CreateEventType(client, "Длинная встреча", "", 60);

    await CreateBooking(client, longEventType.Id, "2026-06-15T06:00:00Z");

    await AssertProblem(await PostBooking(client, SeedEventTypeId, "2026-06-15T06:30:00Z"), HttpStatusCode.Conflict, "SLOT_UNAVAILABLE");
    var adjacent = await CreateBooking(client, SeedEventTypeId, "2026-06-15T07:00:00Z");

    Assert.Equal(Utc(2026, 6, 15, 7, 0), adjacent.StartsAt);
    Assert.Equal(Utc(2026, 6, 15, 7, 30), adjacent.EndsAt);
  }

  [Fact]
  public async Task CancelBookingIsIdempotentAndFreesSlot()
  {
    await using var factory = CreateFactory();
    using var client = factory.CreateClient();

    var booking = await CreateBooking(client, SeedEventTypeId, "2026-06-15T06:00:00Z");

    var cancelResponse = await client.PostAsync($"/api/owner/bookings/{booking.Id}/cancel", null);
    var cancelled = await ReadJson<BookingDto>(cancelResponse);
    var repeatedCancelResponse = await client.PostAsync($"/api/owner/bookings/{booking.Id}/cancel", null);
    var repeated = await ReadJson<BookingDto>(repeatedCancelResponse);
    var invalidResponse = await client.PostAsync("/api/owner/bookings/not-a-uuid/cancel", null);
    var unknownResponse = await client.PostAsync("/api/owner/bookings/99999999-9999-9999-9999-999999999999/cancel", null);
    var replacement = await CreateBooking(client, SeedEventTypeId, "2026-06-15T06:00:00Z", "Другой гость", "other@example.com");

    Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);
    Assert.Equal("cancelled", cancelled.Status);
    Assert.Equal(FixedNow, cancelled.CancelledAt);
    Assert.Equal(HttpStatusCode.OK, repeatedCancelResponse.StatusCode);
    Assert.Equal(cancelled.CancelledAt, repeated.CancelledAt);
    await AssertProblem(invalidResponse, HttpStatusCode.NotFound, "BOOKING_NOT_FOUND");
    await AssertProblem(unknownResponse, HttpStatusCode.NotFound, "BOOKING_NOT_FOUND");
    Assert.NotEqual(booking.Id, replacement.Id);
    Assert.Equal("scheduled", replacement.Status);
  }

  [Fact]
  public async Task CancelBookingRejectsStartedBookings()
  {
    var clock = new FixedClock(FixedNow);
    await using var factory = CreateFactory(clock);
    using var client = factory.CreateClient();
    var booking = await CreateBooking(client, SeedEventTypeId, "2026-06-15T06:00:00Z");

    clock.Current = Utc(2026, 6, 15, 6, 0);

    var response = await client.PostAsync($"/api/owner/bookings/{booking.Id}/cancel", null);

    await AssertProblem(response, HttpStatusCode.UnprocessableEntity, "BOOKING_NOT_CANCELLABLE");
  }

  [Fact]
  public async Task UpcomingBookingsReturnOnlyScheduledFutureBookingsSortedAscending()
  {
    var clock = new FixedClock(FixedNow);
    await using var factory = CreateFactory(clock);
    using var client = factory.CreateClient();

    var past = await CreateBooking(client, SeedEventTypeId, "2026-06-15T06:00:00Z", "Past", "past@example.com");
    var cancelled = await CreateBooking(client, SeedEventTypeId, "2026-06-15T06:30:00Z", "Cancelled", "cancelled@example.com");
    var future = await CreateBooking(client, SeedEventTypeId, "2026-06-15T07:00:00Z", "Future", "future@example.com");
    await client.PostAsync($"/api/owner/bookings/{cancelled.Id}/cancel", null);

    clock.Current = Utc(2026, 6, 15, 6, 15);

    var response = await client.GetAsync("/api/owner/bookings/upcoming");
    var upcoming = await ReadJson<BookingListDto>(response);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.DoesNotContain(upcoming.Items, booking => booking.Id == past.Id);
    Assert.DoesNotContain(upcoming.Items, booking => booking.Id == cancelled.Id);
    var item = Assert.Single(upcoming.Items);
    Assert.Equal(future.Id, item.Id);
    Assert.Equal(Utc(2026, 6, 15, 7, 0), item.StartsAt);
  }

  private static WebApplicationFactory<Program> CreateFactory() => CreateFactory(new FixedClock(FixedNow));

  private static WebApplicationFactory<Program> CreateFactory(FixedClock clock) =>
    new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
    {
      builder.ConfigureServices(services =>
      {
        services.RemoveAll<IClock>();
        services.AddSingleton<IClock>(clock);
      });
    });

  private static async Task<EventTypeDto> CreateEventType(HttpClient client, string title, string description, int durationMinutes)
  {
    var response = await client.PostAsJsonAsync("/api/owner/event-types", new
    {
      title,
      description,
      durationMinutes
    }, JsonOptions);

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    return await ReadJson<EventTypeDto>(response);
  }

  private static async Task<BookingDto> CreateBooking(HttpClient client, Guid eventTypeId, string startsAt, string guestName = "Гость", string guestEmail = "guest@example.com")
  {
    var response = await PostBooking(client, eventTypeId, startsAt, guestName, guestEmail);
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    return await ReadJson<BookingDto>(response);
  }

  private static Task<HttpResponseMessage> PostBooking(HttpClient client, Guid eventTypeId, string startsAt, string guestName = "Гость", string guestEmail = "guest@example.com") =>
    client.PostAsJsonAsync("/api/public/bookings", new
    {
      eventTypeId,
      startsAt,
      guestName,
      guestEmail
    }, JsonOptions);

  private static async Task ReplaceSchedule(HttpClient client, IReadOnlyList<ScheduleDayPayload> days)
  {
    var response = await client.PutAsJsonAsync("/api/owner/schedule", new { days }, JsonOptions);
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
  }

  private static async Task AssertInvalidSchedule(HttpClient client, IReadOnlyList<ScheduleDayPayload> days)
  {
    var response = await client.PutAsJsonAsync("/api/owner/schedule", new { days }, JsonOptions);
    await AssertProblem(response, HttpStatusCode.UnprocessableEntity, "INVALID_SCHEDULE");
  }

  private static async Task<ProblemDetailsDto> AssertProblem(HttpResponseMessage response, HttpStatusCode statusCode, string code)
  {
    var problem = await ReadJson<ProblemDetailsDto>(response);
    Assert.Equal(statusCode, response.StatusCode);
    Assert.Equal((int)statusCode, problem.Status);
    Assert.Equal(code, problem.Code);
    Assert.False(string.IsNullOrWhiteSpace(problem.Type));
    Assert.False(string.IsNullOrWhiteSpace(problem.Title));
    return problem;
  }

  private static async Task<T> ReadJson<T>(HttpResponseMessage response)
  {
    var content = await response.Content.ReadAsStringAsync();
    Assert.True(response.Content.Headers.ContentType?.MediaType == "application/json", content);
    return JsonSerializer.Deserialize<T>(content, JsonOptions) ?? throw new InvalidOperationException(content);
  }

  private static IReadOnlyList<ScheduleDayPayload> MondayOnlySchedule(string startsAt, string endsAt) =>
    Weekdays.Select(weekday => weekday == "monday"
      ? new ScheduleDayPayload(weekday, true, startsAt, endsAt)
      : new ScheduleDayPayload(weekday, false, null, null)).ToArray();

  private static DateTime Utc(int year, int month, int day, int hour, int minute) =>
    DateTime.SpecifyKind(new DateTime(year, month, day, hour, minute, 0), DateTimeKind.Utc);

  private static readonly string[] Weekdays = ["monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday"];

  private sealed class FixedClock(DateTime current) : IClock
  {
    public DateTime Current { get; set; } = current;

    public DateTime UtcNow() => Current;
  }

  private sealed record OwnerDto(Guid Id, string DisplayName, string TimeZone);

  private sealed record EventTypeDto(Guid Id, string Title, string Description, int DurationMinutes);

  private sealed record EventTypeListDto(IReadOnlyList<EventTypeDto> Items);

  private sealed record ScheduleDto(IReadOnlyList<ScheduleDayDto> Days);

  private sealed record ScheduleDayDto(string Weekday, bool Enabled, string? StartsAtLocalTime, string? EndsAtLocalTime);

  private sealed record ScheduleDayPayload(string Weekday, bool Enabled, string? StartsAtLocalTime, string? EndsAtLocalTime);

  private sealed record SlotDto(Guid EventTypeId, DateTime StartsAt, DateTime EndsAt, int DurationMinutes);

  private sealed record SlotListDto(IReadOnlyList<SlotDto> Items);

  private sealed record BookingDto(
    Guid Id,
    Guid EventTypeId,
    string EventTypeTitle,
    int DurationMinutes,
    DateTime StartsAt,
    DateTime EndsAt,
    string GuestName,
    string GuestEmail,
    string Status,
    DateTime CreatedAt,
    DateTime? CancelledAt);

  private sealed record BookingListDto(IReadOnlyList<BookingDto> Items);

  private sealed record ProblemDetailsDto(string Type, string Title, int Status, string? Detail, string Code);
}
