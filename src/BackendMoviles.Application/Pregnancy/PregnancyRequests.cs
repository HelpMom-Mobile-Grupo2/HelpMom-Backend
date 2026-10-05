using BackendMoviles.Application.Common;
using BackendMoviles.Domain.Common;
using BackendMoviles.Domain.Pregnancy;
using FluentValidation;
using MediatR;

namespace BackendMoviles.Application.Pregnancy;

public sealed record CreatePregnancyRecordCommand(
    Guid MotherUserId,
    DateOnly LastMenstrualPeriod,
    DateOnly DueDate) : IRequest<PregnancyDto>;

public sealed class CreatePregnancyRecordValidator : AbstractValidator<CreatePregnancyRecordCommand>
{
    public CreatePregnancyRecordValidator()
    {
        RuleFor(request => request.MotherUserId).NotEmpty();
        RuleFor(request => request.DueDate).GreaterThan(request => request.LastMenstrualPeriod)
            .Must((request, dueDate) => dueDate.DayNumber - request.LastMenstrualPeriod.DayNumber <= 308)
            .WithMessage("La fecha probable de parto debe estar dentro de las 44 semanas posteriores a la FUM.");
    }
}

public sealed class CreatePregnancyRecordHandler(IPregnancyRepository pregnancies, IUnitOfWork unitOfWork)
    : IRequestHandler<CreatePregnancyRecordCommand, PregnancyDto>
{
    public async Task<PregnancyDto> Handle(CreatePregnancyRecordCommand request, CancellationToken cancellationToken)
    {
        var pregnancy = PregnancyRecord.Create(request.MotherUserId, request.LastMenstrualPeriod, request.DueDate);
        await pregnancies.AddAsync(pregnancy, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return pregnancy.ToDto();
    }
}

public sealed record GetPregnancyRecordQuery(Guid Id) : IRequest<PregnancyDto?>;

public sealed class GetPregnancyRecordHandler(IPregnancyRepository pregnancies)
    : IRequestHandler<GetPregnancyRecordQuery, PregnancyDto?>
{
    public async Task<PregnancyDto?> Handle(GetPregnancyRecordQuery request, CancellationToken cancellationToken)
    {
        var pregnancy = await pregnancies.GetByIdAsync(request.Id, cancellationToken);
        return pregnancy?.ToDto();
    }
}

public sealed record GetPregnancyRecordsQuery(Guid MotherUserId) : IRequest<IReadOnlyCollection<PregnancyDto>>;

public sealed class GetPregnancyRecordsHandler(IPregnancyRepository pregnancies)
    : IRequestHandler<GetPregnancyRecordsQuery, IReadOnlyCollection<PregnancyDto>>
{
    public async Task<IReadOnlyCollection<PregnancyDto>> Handle(
        GetPregnancyRecordsQuery request,
        CancellationToken cancellationToken)
    {
        var records = await pregnancies.GetByMotherAsync(request.MotherUserId, cancellationToken);
        return records.Select(record => record.ToDto()).ToArray();
    }
}

public sealed record UpdatePregnancyDatesCommand(
    Guid Id,
    DateOnly LastMenstrualPeriod,
    DateOnly DueDate) : IRequest<PregnancyDto>;

public sealed class UpdatePregnancyDatesValidator : AbstractValidator<UpdatePregnancyDatesCommand>
{
    public UpdatePregnancyDatesValidator()
    {
        RuleFor(request => request.Id).NotEmpty();
        RuleFor(request => request.DueDate).GreaterThan(request => request.LastMenstrualPeriod)
            .Must((request, dueDate) => dueDate.DayNumber - request.LastMenstrualPeriod.DayNumber <= 308)
            .WithMessage("La fecha probable de parto debe estar dentro de las 44 semanas posteriores a la FUM.");
    }
}

public sealed class UpdatePregnancyDatesHandler(IPregnancyRepository pregnancies, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdatePregnancyDatesCommand, PregnancyDto>
{
    public async Task<PregnancyDto> Handle(UpdatePregnancyDatesCommand request, CancellationToken cancellationToken)
    {
        var pregnancy = await pregnancies.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("No se encontró el expediente de embarazo.");
        pregnancy.UpdateDates(request.LastMenstrualPeriod, request.DueDate);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return pregnancy.ToDto();
    }
}

public sealed record DeletePregnancyRecordCommand(Guid Id) : IRequest<bool>;

public sealed class DeletePregnancyRecordHandler(IPregnancyRepository pregnancies, IUnitOfWork unitOfWork)
    : IRequestHandler<DeletePregnancyRecordCommand, bool>
{
    public async Task<bool> Handle(DeletePregnancyRecordCommand request, CancellationToken cancellationToken)
    {
        var pregnancy = await pregnancies.GetByIdAsync(request.Id, cancellationToken);
        if (pregnancy is null)
        {
            return false;
        }

        await pregnancies.DeleteAsync(pregnancy, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed record RecordSymptomCommand(
    Guid PregnancyId,
    string Description,
    SymptomSeverity Severity,
    DateTimeOffset? OccurredAt = null) : IRequest<SymptomDto>;

public sealed class RecordSymptomValidator : AbstractValidator<RecordSymptomCommand>
{
    public RecordSymptomValidator()
    {
        RuleFor(request => request.PregnancyId).NotEmpty();
        RuleFor(request => request.Description).NotEmpty().MaximumLength(1000);
        RuleFor(request => request.Severity).IsInEnum();
    }
}

public sealed class RecordSymptomHandler(IPregnancyRepository pregnancies, IUnitOfWork unitOfWork)
    : IRequestHandler<RecordSymptomCommand, SymptomDto>
{
    public async Task<SymptomDto> Handle(RecordSymptomCommand request, CancellationToken cancellationToken)
    {
        var pregnancy = await pregnancies.GetByIdAsync(request.PregnancyId, cancellationToken)
            ?? throw new KeyNotFoundException("No se encontró el expediente de embarazo.");
        var symptom = pregnancy.RecordSymptom(
            request.Description,
            request.Severity,
            request.OccurredAt ?? DateTimeOffset.UtcNow);
        unitOfWork.Add(symptom);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new SymptomDto(symptom.Id, symptom.Description, symptom.Severity, symptom.OccurredAt);
    }
}