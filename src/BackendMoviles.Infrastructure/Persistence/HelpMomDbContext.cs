using BackendMoviles.Domain.Common;
using BackendMoviles.Domain.Companion;
using BackendMoviles.Domain.Identity;
using BackendMoviles.Domain.Pregnancy;
using BackendMoviles.Domain.Telemetry;
using BackendMoviles.Domain.Triage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BackendMoviles.Infrastructure.Persistence;

public class HelpMomDbContext : DbContext, IUnitOfWork
{
    public HelpMomDbContext(DbContextOptions<HelpMomDbContext> options)
        : base(options)
    {
    }

    // Lets provider-specific subclasses (PostgreSQL) keep their own migrations, which EF matches by context type.
    protected HelpMomDbContext(DbContextOptions options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<FamilyGroup> FamilyGroups => Set<FamilyGroup>();
    public DbSet<FamilyInvitation> FamilyInvitations => Set<FamilyInvitation>();
    public DbSet<PregnancyRecord> PregnancyRecords => Set<PregnancyRecord>();
    public DbSet<HealthTelemetry> HealthTelemetry => Set<HealthTelemetry>();
    public DbSet<TriageConversation> TriageConversations => Set<TriageConversation>();
    public DbSet<QuickAlert> QuickAlerts => Set<QuickAlert>();
    public DbSet<DailyTip> DailyTips => Set<DailyTip>();
    public DbSet<TipCompletion> TipCompletions => Set<TipCompletion>();

    public void Add(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        base.Add(entity);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        if (Database.IsSqlite())
        {
            // SQLite cannot order or compare DateTimeOffset columns, so they are stored as sortable numbers.
            configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<Entity>();

        modelBuilder.Entity<User>(builder =>
        {
            builder.HasKey(user => user.Id);
            builder.Property(user => user.Email)
                .HasConversion(email => email.Value, value => EmailAddress.Create(value))
                .HasMaxLength(254)
                .IsRequired();
            builder.HasIndex(user => user.Email).IsUnique();
            builder.Property(user => user.FullName).HasMaxLength(160).IsRequired();
            builder.Property(user => user.PasswordHash).IsRequired();
        });

        modelBuilder.Entity<FamilyGroup>(builder =>
        {
            builder.HasKey(group => group.Id);
            builder.Property(group => group.Name).HasMaxLength(160).IsRequired();
            builder.HasMany(group => group.Members)
                .WithOne()
                .HasForeignKey("FamilyGroupId")
                .OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(group => group.Members).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<FamilyMember>(builder =>
        {
            builder.HasKey(member => member.Id);
            builder.Property<Guid>("FamilyGroupId").IsRequired();
            builder.HasIndex("FamilyGroupId", nameof(FamilyMember.UserId)).IsUnique();
        });

        modelBuilder.Entity<FamilyInvitation>(builder =>
        {
            builder.HasKey(invitation => invitation.Id);
            builder.Property(invitation => invitation.Code).HasMaxLength(7).IsRequired();
            builder.HasIndex(invitation => invitation.Code);
            builder.HasOne<FamilyGroup>()
                .WithMany()
                .HasForeignKey(invitation => invitation.FamilyGroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PregnancyRecord>(builder =>
        {
            builder.HasKey(pregnancy => pregnancy.Id);
            builder.HasIndex(pregnancy => pregnancy.MotherUserId);
            builder.HasMany(pregnancy => pregnancy.Symptoms)
                .WithOne()
                .HasForeignKey("PregnancyRecordId")
                .OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(pregnancy => pregnancy.Symptoms).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<SymptomEntry>(builder =>
        {
            builder.HasKey(symptom => symptom.Id);
            builder.Property<Guid>("PregnancyRecordId").IsRequired();
            builder.Property(symptom => symptom.Description).HasMaxLength(1000).IsRequired();
        });

        modelBuilder.Entity<HealthTelemetry>(builder =>
        {
            builder.HasKey(measurement => measurement.Id);
            builder.HasIndex(measurement => new { measurement.MotherUserId, measurement.MeasuredAt });
            builder.Property(measurement => measurement.FetalHeartRate)
                .HasConversion(value => value.BeatsPerMinute, value => FetalHeartRate.Create(value));
            builder.Property(measurement => measurement.Temperature)
                .HasConversion(value => value.Celsius, value => BodyTemperature.Create(value));
            builder.Ignore(measurement => measurement.Semaphore);
        });

        modelBuilder.Entity<TriageConversation>(builder =>
        {
            builder.HasKey(conversation => conversation.Id);
            builder.HasIndex(conversation => conversation.MotherUserId);
            builder.HasMany(conversation => conversation.Messages)
                .WithOne()
                .HasForeignKey("TriageConversationId")
                .OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(conversation => conversation.Messages).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.OwnsOne(conversation => conversation.LatestAssessment)
                .Property(assessment => assessment.Urgency);
            builder.OwnsOne(conversation => conversation.LatestAssessment)
                .Property(assessment => assessment.Guidance).HasMaxLength(2000);
            builder.OwnsOne(conversation => conversation.LatestAssessment)
                .Property(assessment => assessment.AssessedAt);
        });

        modelBuilder.Entity<TriageMessage>(builder =>
        {
            builder.HasKey(message => message.Id);
            builder.Property<Guid>("TriageConversationId").IsRequired();
            builder.Property(message => message.Content).HasMaxLength(4000).IsRequired();
        });

        modelBuilder.Entity<QuickAlert>(builder =>
        {
            builder.HasKey(alert => alert.Id);
            builder.Property(alert => alert.Message).HasMaxLength(QuickAlert.MaxMessageLength).IsRequired();
            builder.HasIndex(alert => new { alert.FamilyGroupId, alert.CreatedAt });
            builder.HasOne<FamilyGroup>()
                .WithMany()
                .HasForeignKey(alert => alert.FamilyGroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DailyTip>(builder =>
        {
            builder.HasKey(tip => tip.Id);
            builder.Property(tip => tip.Title).HasMaxLength(160).IsRequired();
            builder.Property(tip => tip.Body).HasMaxLength(1000).IsRequired();
            builder.HasData(DailyTipSeed.Tips);
        });

        modelBuilder.Entity<TipCompletion>(builder =>
        {
            builder.HasKey(completion => completion.Id);
            builder.HasIndex(completion => new { completion.TipId, completion.PartnerUserId, completion.Date }).IsUnique();
            builder.HasOne<DailyTip>()
                .WithMany()
                .HasForeignKey(completion => completion.TipId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}