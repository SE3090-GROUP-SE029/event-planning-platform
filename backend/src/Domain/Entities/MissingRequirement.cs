using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>A value object describing information missing from an event.</summary>
public sealed class MissingRequirement : IEquatable<MissingRequirement>
{
    private MissingRequirement() { }

    public MissingRequirement(
        string requirement,
        string reason,
        RequirementCategory category,
        DateTime? identifiedAt = null)
    {
        Requirement = ValidateText(requirement, 500, nameof(requirement));
        Reason = ValidateText(reason, 1000, nameof(reason));
        if (!Enum.IsDefined(category))
            throw new ArgumentOutOfRangeException(nameof(category));

        Category = category;
        IdentifiedAt = identifiedAt ?? DateTime.UtcNow;
    }

    [Required, MaxLength(500)]
    public string Requirement { get; init; } = string.Empty;

    [Required, MaxLength(1000)]
    public string Reason { get; init; } = string.Empty;

    public RequirementCategory Category { get; init; }

    public DateTime IdentifiedAt { get; init; }

    public bool Equals(MissingRequirement? other) =>
        other is not null &&
        Requirement == other.Requirement &&
        Reason == other.Reason &&
        Category == other.Category &&
        IdentifiedAt == other.IdentifiedAt;

    public override bool Equals(object? obj) => Equals(obj as MissingRequirement);

    public override int GetHashCode() => HashCode.Combine(Requirement, Reason, Category, IdentifiedAt);

    public static bool operator ==(MissingRequirement? left, MissingRequirement? right) =>
        EqualityComparer<MissingRequirement>.Default.Equals(left, right);

    public static bool operator !=(MissingRequirement? left, MissingRequirement? right) => !(left == right);

    public override string ToString() => $"{Category}: {Requirement} ({Reason})";

    private static string ValidateText(string value, int maxLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maxLength)
            throw new ArgumentException($"Must contain between 1 and {maxLength} characters.", parameterName);
        return value.Trim();
    }
}
