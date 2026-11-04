using BackendMoviles.Domain.Common;
using BackendMoviles.Domain.Companion;
using BackendMoviles.Domain.Identity;
using MediatR;

namespace BackendMoviles.Application.Companion;

public static class EmergencyReasons
{
    public const string RedTelemetry = "la medición de salud salió en rojo";
    public const string TriageEmergency = "el triage detectó una posible emergencia";
}

// Published after a red measurement or an emergency triage has been saved.
public sealed record EmergencyDetected(Guid MotherUserId, string Reason) : INotification;

public sealed class EmergencyAlertPolicy(
    IFamilyGroupRepository families,
    IQuickAlertRepository alerts,
    IUnitOfWork unitOfWork)
    : INotificationHandler<EmergencyDetected>
{
    public static readonly TimeSpan DuplicateWindow = TimeSpan.FromMinutes(10);

    public static string MessageFor(string reason) => $"Alerta automática: {reason}";

    public async Task Handle(EmergencyDetected notification, CancellationToken cancellationToken)
    {
        var family = await families.GetByMemberAsync(notification.MotherUserId, FamilyRole.Mother, cancellationToken);
        if (family?.GetPartner() is null)
        {
            return;
        }

        // Several red readings in a row should notify the partner once, not once per reading.
        var now = DateTimeOffset.UtcNow;
        var message = MessageFor(notification.Reason);
        if (await alerts.HasPendingSinceAsync(family.Id, QuickAlertType.Sos, message, now - DuplicateWindow, cancellationToken))
        {
            return;
        }

        var alert = QuickAlert.Create(family.Id, notification.MotherUserId, QuickAlertType.Sos, message, now);
        await alerts.AddAsync(alert, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
