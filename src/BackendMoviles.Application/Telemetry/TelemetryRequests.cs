using BackendMoviles.Application.Common;
using BackendMoviles.Domain.Common;
using BackendMoviles.Domain.Telemetry;
using FluentValidation;
using MediatR;

namespace BackendMoviles.Application.Telemetry;

public sealed record AddTelemetryCommand(
    Guid MotherUserId,
    int FetalHeartRateBpm,
    decimal TemperatureCelsius,
    DateTimeOffset? MeasuredAt = null) : IRequest<TelemetryDto>;

public sealed class AddTelemetryValidator : AbstractValidator<AddTelemetryCommand>
{
    public AddTelemetryValidator()
    {
        RuleFor(request => request.MotherUserId).NotEmpty();
        RuleFor(request => request.FetalHeartRateBpm).InclusiveBetween(30, 250);
        RuleFor(request => request.TemperatureCelsius).InclusiveBetween(30m, 45m);
    }
}

public sealed class AddTelemetryHandler(IHealthTelemetryRepository telemetry, IUnitOfWork unitOfWork)
    : IRequestHandler<AddTelemetryCommand, TelemetryDto>
{
    public async Task<TelemetryDto> Handle(AddTelemetryCommand request, CancellationToken cancellationToken)
    {
        var measurement = HealthTelemetry.Create(
            request.MotherUserId,
            FetalHeartRate.Create(request.FetalHeartRateBpm),
            BodyTemperature.Create(request.TemperatureCelsius),
            request.MeasuredAt ?? DateTimeOffset.UtcNow);
        await telemetry.AddAsync(measurement, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return measurement.ToDto();
    }
}

public sealed record GetHealthStatusQuery(Guid MotherUserId) : IRequest<TelemetryDto?>;

public sealed class GetHealthStatusHandler(IHealthTelemetryRepository telemetry)
    : IRequestHandler<GetHealthStatusQuery, TelemetryDto?>
{
    public async Task<TelemetryDto?> Handle(GetHealthStatusQuery request, CancellationToken cancellationToken)
    {
        var latest = await telemetry.GetRecentByMotherAsync(request.MotherUserId, 1, cancellationToken);
        return latest.FirstOrDefault()?.ToDto();
    }
}

public sealed record GetTelemetryHistoryQuery(Guid MotherUserId, int Count = 50)
    : IRequest<IReadOnlyCollection<TelemetryDto>>;

public sealed class GetTelemetryHistoryValidator : AbstractValidator<GetTelemetryHistoryQuery>
{
    public GetTelemetryHistoryValidator()
    {
        RuleFor(request => request.MotherUserId).NotEmpty();
        RuleFor(request => request.Count).InclusiveBetween(1, 500);
    }
}

public sealed class GetTelemetryHistoryHandler(IHealthTelemetryRepository telemetry)
    : IRequestHandler<GetTelemetryHistoryQuery, IReadOnlyCollection<TelemetryDto>>
{
    public async Task<IReadOnlyCollection<TelemetryDto>> Handle(
        GetTelemetryHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var measurements = await telemetry.GetRecentByMotherAsync(request.MotherUserId, request.Count, cancellationToken);
        return measurements.Select(measurement => measurement.ToDto()).ToArray();
    }
}