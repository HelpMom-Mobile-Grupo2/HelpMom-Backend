using System;
using BackendMoviles.Domain.Identity;
using BackendMoviles.Domain.Pregnancy;
using BackendMoviles.Domain.Telemetry;
using BackendMoviles.Domain.Triage;
using Xunit;

namespace BackendMoviles.Tests;

public class DomainTests
{
    [Fact]
    public void EmailAddress_Create_ReturnsNormalizedValue()
    {
        var email = EmailAddress.Create("  ExAMPle@Domain.Com  ");
        Assert.Equal("example@domain.com", email.Value);
    }

    [Fact]
    public void User_Create_SetsIsActiveTrue()
    {
        var email = EmailAddress.Create("user@test.com");
        var user = User.Create(email, "Nombre Apellido", "hashedpwd");
        Assert.True(user.IsActive);
    }

    [Fact]
    public void PregnancyRecord_GetGestationalAge_ComputesWeeks()
    {
        var lmp = new DateOnly(2024, 1, 1);
        var asOf = new DateOnly(2024, 3, 11); // 70 days -> 10 weeks
        var record = PregnancyRecord.Create(Guid.NewGuid(), lmp, lmp.AddDays(280));
        var age = record.GetGestationalAge(asOf);
        Assert.Equal(10, age.Weeks);
    }

    [Fact]
    public void HealthTelemetry_Semaphore_IsGreenForNormalValues()
    {
        var heart = FetalHeartRate.Create(140);
        var temp = BodyTemperature.Create(36.6m);
        var telemetry = HealthTelemetry.Create(Guid.NewGuid(), heart, temp, DateTimeOffset.UtcNow);
        Assert.Equal(HealthLight.Green, telemetry.Semaphore.Light);
    }

    [Fact]
    public void TriageAssessment_Create_TrimsGuidance()
    {
        var assessedAt = DateTimeOffset.UtcNow;
        var assessment = TriageAssessment.Create(TriageUrgency.SelfCare, "  mantenerse en reposo  ", assessedAt);
        Assert.Equal("mantenerse en reposo", assessment.Guidance);
    }
}
