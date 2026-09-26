using System.Text.Json;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Data.Configurations;

public sealed class EventPlanDraftConfiguration : IEntityTypeConfiguration<EventPlanDraft>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<EventPlanDraft> builder)
    {
        builder.ToTable("EventPlanDrafts", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_EventPlanDrafts_Version_Positive", "\"Version\" > 0");
            tableBuilder.HasCheckConstraint(
                "CK_EventPlanDrafts_PlanCompletenessScore_Range",
                "\"PlanCompletenessScore\" BETWEEN 0 AND 100");
        });

        builder.HasKey(draft => draft.Id);
        builder.HasIndex(draft => new { draft.EventId, draft.Version }).IsUnique();
        builder.HasIndex(draft => new { draft.EventId, draft.Status });

        builder.Property(draft => draft.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(draft => draft.Rationale).HasMaxLength(8000).IsRequired();
        builder.Property(draft => draft.ValidationSummary).HasMaxLength(8000).IsRequired();
        builder.Property(draft => draft.PlannerRemarks).HasMaxLength(4000);
        builder.Property(draft => draft.CreatedAt).IsRequired();
        builder.Property(draft => draft.UpdatedAt).IsRequired();
        builder.Property(draft => draft.GeneratedAt).IsRequired();
        builder.Property<DateTime>("LastAuditAt");

        builder.Property(draft => draft.ServiceCategories)
            .HasColumnType("jsonb")
            .HasConversion(JsonConverter<List<string>>());
        builder.Property(draft => draft.BudgetAllocation)
            .HasColumnType("jsonb")
            .HasConversion(JsonConverter<Dictionary<string, decimal>>());
        builder.Property(draft => draft.TargetVendorTypes)
            .HasColumnType("jsonb")
            .HasConversion(JsonConverter<List<string>>());
        builder.Property(draft => draft.ProposedTimeline)
            .HasColumnType("jsonb")
            .HasConversion(JsonConverter<Dictionary<string, string>>());
        builder.Property(draft => draft.EventSnapshot)
            .HasColumnType("jsonb")
            .HasConversion(JsonConverter<EventSnapshot>());

        builder.HasOne(draft => draft.Event)
            .WithMany(evt => evt.EventPlanDrafts)
            .HasForeignKey(draft => draft.EventId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(draft => draft.CreatedBy)
            .WithMany(user => user.CreatedEventPlanDrafts)
            .HasForeignKey(draft => draft.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(draft => draft.ApprovedBy)
            .WithMany(user => user.ApprovedEventPlanDrafts)
            .HasForeignKey(draft => draft.ApprovedById)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(draft => draft.RejectedBy)
            .WithMany(user => user.RejectedEventPlanDrafts)
            .HasForeignKey(draft => draft.RejectedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsMany(draft => draft.IdentifiedRisks, risk =>
        {
            risk.ToJson();
            risk.Property(item => item.Risk).HasMaxLength(500).IsRequired();
            risk.Property(item => item.Recommendation).HasMaxLength(1000).IsRequired();
            risk.Property(item => item.Severity).HasConversion<string>().HasMaxLength(16);
        });
        builder.OwnsMany(draft => draft.MissingRequirements, requirement =>
        {
            requirement.ToJson();
            requirement.Property(item => item.Requirement).HasMaxLength(500).IsRequired();
            requirement.Property(item => item.Reason).HasMaxLength(1000).IsRequired();
            requirement.Property(item => item.Category).HasConversion<string>().HasMaxLength(32);
        });
    }

    private static ValueConverter<T, string> JsonConverter<T>() where T : class =>
        new(
            value => JsonSerializer.Serialize(value, JsonOptions),
            value => DeserializeJson<T>(value));

    private static T DeserializeJson<T>(string value) where T : class =>
        JsonSerializer.Deserialize<T>(value, JsonOptions)
        ?? throw new JsonException($"Could not deserialize JSON as {typeof(T).Name}.");
}
