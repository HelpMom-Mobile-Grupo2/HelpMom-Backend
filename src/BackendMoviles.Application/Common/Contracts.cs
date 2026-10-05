using BackendMoviles.Domain.Identity;
using BackendMoviles.Domain.Pregnancy;
using BackendMoviles.Domain.Telemetry;
using BackendMoviles.Domain.Triage;

namespace BackendMoviles.Application.Common;

public interface IPasswordService
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}

public interface IAccessTokenService
{
    string CreateToken(User user, FamilyRole? role);
}

public sealed record UserDto(Guid Id, string Email, string FullName, bool IsActive);
public sealed record AuthResponse(UserDto User, Guid? FamilyGroupId, string? Role, string AccessToken);
public sealed record FamilyMemberDto(Guid UserId, FamilyRole Role, DateTimeOffset JoinedAt);
public sealed record FamilyGroupDto(Guid Id, string Name, IReadOnlyCollection<FamilyMemberDto> Members);
public sealed record SymptomDto(Guid Id, string Description, SymptomSeverity Severity, DateTimeOffset OccurredAt);
public sealed record PregnancyDto(
    Guid Id,
    Guid MotherUserId,
    DateOnly LastMenstrualPeriod,
    DateOnly DueDate,
    int GestationalWeeks,
    int GestationalDays,
    IReadOnlyCollection<SymptomDto> Symptoms);
public sealed record TelemetryDto(
    Guid Id,
    Guid MotherUserId,
    int FetalHeartRateBpm,
    decimal TemperatureCelsius,
    DateTimeOffset MeasuredAt,
    HealthLight HealthLight,
    string HealthSummary);
public sealed record TriageMessageDto(Guid Id, MessageAuthor Author, string Content, DateTimeOffset SentAt);
public sealed record TriageConversationDto(
    Guid Id,
    Guid MotherUserId,
    DateTimeOffset CreatedAt,
    IReadOnlyCollection<TriageMessageDto> Messages,
    TriageUrgency? Urgency,
    string? Guidance);

public static class DtoMapper
{
    public static UserDto ToDto(this User user) => new(user.Id, user.Email.Value, user.FullName, user.IsActive);

    public static FamilyGroupDto ToDto(this FamilyGroup group) => new(
        group.Id,
        group.Name,
        group.Members.Select(member => new FamilyMemberDto(member.UserId, member.Role, member.JoinedAt)).ToArray());

    public static PregnancyDto ToDto(this PregnancyRecord pregnancy, DateOnly? asOfDate = null)
    {
        var age = pregnancy.GetGestationalAge(asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow));
        return new PregnancyDto(
            pregnancy.Id,
            pregnancy.MotherUserId,
            pregnancy.LastMenstrualPeriod,
            pregnancy.DueDate,
            age.Weeks,
            age.Days,
            pregnancy.Symptoms.Select(symptom => new SymptomDto(
                symptom.Id,
                symptom.Description,
                symptom.Severity,
                symptom.OccurredAt)).ToArray());
    }

    public static TelemetryDto ToDto(this HealthTelemetry telemetry) => new(
        telemetry.Id,
        telemetry.MotherUserId,
        telemetry.FetalHeartRate.BeatsPerMinute,
        telemetry.Temperature.Celsius,
        telemetry.MeasuredAt,
        telemetry.Semaphore.Light,
        telemetry.Semaphore.Summary);

    public static TriageConversationDto ToDto(this TriageConversation conversation) => new(
        conversation.Id,
        conversation.MotherUserId,
        conversation.CreatedAt,
        conversation.Messages
            .OrderBy(message => message.SentAt)
            .Select(message => new TriageMessageDto(message.Id, message.Author, message.Content, message.SentAt))
            .ToArray(),
        conversation.LatestAssessment?.Urgency,
        conversation.LatestAssessment?.Guidance);
}