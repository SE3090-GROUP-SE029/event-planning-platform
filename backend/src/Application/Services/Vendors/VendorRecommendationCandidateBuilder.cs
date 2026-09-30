using Domain.Entities;
using Domain.Enums;

namespace Application.Services.Vendors;

/// <summary>Builds AI candidate payloads from APPROVED vendors using plan constraints.</summary>
public static class VendorRecommendationCandidateBuilder
{
    private const int MaxCandidates = 30;

    public sealed record Candidate(
        Vendor Vendor,
        VendorOffering? Offering,
        decimal? EffectivePrice,
        bool BudgetFit,
        bool AvailabilityMatch,
        decimal? AverageRating,
        int ReviewCount);

    public static IReadOnlyList<BusinessCategory> ResolveCategories(
        IEnumerable<string> serviceCategories,
        IEnumerable<string> targetVendorTypes)
    {
        var resolved = new HashSet<BusinessCategory>();
        foreach (var raw in serviceCategories.Concat(targetVendorTypes))
        {
            if (TryMapCategory(raw, out var category))
                resolved.Add(category);
        }

        return resolved.Count == 0
            ? Enum.GetValues<BusinessCategory>()
            : resolved.ToList();
    }

    public static bool TryMapCategory(string? raw, out BusinessCategory category)
    {
        category = default;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var normalized = raw.Trim().ToUpperInvariant()
            .Replace('-', ' ')
            .Replace('_', ' ');

        if (Enum.TryParse(normalized.Replace(" ", ""), true, out category))
            return true;

        if (normalized.Contains("CATER", StringComparison.Ordinal))
        {
            category = BusinessCategory.CATERING;
            return true;
        }

        if (normalized.Contains("PHOTO", StringComparison.Ordinal))
        {
            category = BusinessCategory.PHOTOGRAPHY;
            return true;
        }

        if (normalized.Contains("VENUE", StringComparison.Ordinal) ||
            normalized.Contains("HALL", StringComparison.Ordinal))
        {
            category = BusinessCategory.VENUE;
            return true;
        }

        if (normalized.Contains("MUSIC", StringComparison.Ordinal) ||
            normalized.Contains("DJ", StringComparison.Ordinal) ||
            normalized.Contains("ENTERTAIN", StringComparison.Ordinal) ||
            normalized.Contains("AUDIO", StringComparison.Ordinal))
        {
            category = BusinessCategory.MUSIC;
            return true;
        }

        if (normalized.Contains("FLOR", StringComparison.Ordinal) ||
            normalized.Contains("DECOR", StringComparison.Ordinal) ||
            normalized.Contains("FLOWER", StringComparison.Ordinal))
        {
            category = BusinessCategory.FLORIST;
            return true;
        }

        if (normalized.Contains("TRANSPORT", StringComparison.Ordinal) ||
            normalized.Contains("LIMO", StringComparison.Ordinal))
        {
            category = BusinessCategory.TRANSPORTATION;
            return true;
        }

        return false;
    }

    public static decimal? ResolveBudgetCap(
        BusinessCategory category,
        IReadOnlyDictionary<string, decimal> budgetAllocation,
        decimal? totalBudget)
    {
        foreach (var pair in budgetAllocation)
        {
            if (TryMapCategory(pair.Key, out var mapped) && mapped == category && pair.Value > 0)
                return pair.Value;
        }

        var categoryName = category.ToString();
        foreach (var pair in budgetAllocation)
        {
            if (pair.Key.Contains(categoryName, StringComparison.OrdinalIgnoreCase) && pair.Value > 0)
                return pair.Value;
        }

        return totalBudget is > 0 ? totalBudget : null;
    }

    public static decimal? ComputeEffectivePrice(
        VendorOffering offering,
        int guestCount)
    {
        if (offering.Price is null)
            return null;

        return offering.PricingType switch
        {
            PricingType.PER_PERSON => offering.Price.Value * Math.Max(guestCount, 1),
            _ => offering.Price.Value
        };
    }

    public static bool IsBlockedOnDate(
        IEnumerable<VendorAvailability> windows,
        DateTime eventDateUtc)
    {
        var dayStart = DateTime.SpecifyKind(eventDateUtc.Date, DateTimeKind.Utc);
        var dayEnd = dayStart.AddDays(1);

        return windows.Any(w =>
            !w.IsAvailable &&
            w.StartDateTime < dayEnd &&
            dayStart < w.EndDateTime);
    }

    public static bool HasAvailabilityMatch(
        IEnumerable<VendorAvailability> windows,
        DateTime eventDateUtc)
    {
        var dayStart = DateTime.SpecifyKind(eventDateUtc.Date, DateTimeKind.Utc);
        var dayEnd = dayStart.AddDays(1);

        return windows.Any(w =>
            w.IsAvailable &&
            w.StartDateTime <= dayStart &&
            dayEnd <= w.EndDateTime);
    }

    public static VendorOffering? SelectBestOffering(
        IReadOnlyList<VendorOffering> offerings,
        decimal? budgetCap,
        int guestCount,
        out decimal? effectivePrice,
        out bool budgetFit)
    {
        effectivePrice = null;
        budgetFit = true;

        if (offerings.Count == 0)
            return null;

        VendorOffering? best = null;
        decimal? bestEffective = null;
        var bestFits = false;

        foreach (var offering in offerings)
        {
            var effective = ComputeEffectivePrice(offering, guestCount);
            var fits = budgetCap is null ||
                       effective is null ||
                       effective <= budgetCap.Value;

            if (best is null)
            {
                best = offering;
                bestEffective = effective;
                bestFits = fits;
                continue;
            }

            // Prefer in-budget offerings; among those, prefer lower effective price.
            if (fits && !bestFits)
            {
                best = offering;
                bestEffective = effective;
                bestFits = true;
                continue;
            }

            if (fits == bestFits)
            {
                if (effective is not null &&
                    (bestEffective is null || effective < bestEffective))
                {
                    best = offering;
                    bestEffective = effective;
                    bestFits = fits;
                }
            }
        }

        // Hard filter: if budget exists and no offering fits, exclude vendor entirely.
        if (budgetCap is not null && !bestFits && bestEffective is not null)
        {
            effectivePrice = bestEffective;
            budgetFit = false;
            return null;
        }

        effectivePrice = bestEffective;
        budgetFit = bestFits || bestEffective is null;
        return best;
    }

    public static IReadOnlyList<Candidate> Build(
        IReadOnlyList<Vendor> approvedVendors,
        IReadOnlyDictionary<Guid, IReadOnlyList<VendorOffering>> offeringsByVendor,
        IReadOnlyDictionary<Guid, IReadOnlyList<VendorAvailability>> availabilityByVendor,
        IReadOnlyDictionary<Guid, (decimal AverageRating, int ReviewCount)> ratings,
        IReadOnlyCollection<BusinessCategory> categories,
        IReadOnlyDictionary<string, decimal> budgetAllocation,
        decimal? totalBudget,
        int guestCount,
        DateTime eventDateUtc)
    {
        var results = new List<Candidate>();

        foreach (var vendor in approvedVendors)
        {
            if (vendor.Status != VendorStatus.APPROVED)
                continue;

            if (categories.Count > 0 && !categories.Contains(vendor.Category))
                continue;

            var windows = availabilityByVendor.TryGetValue(vendor.Id, out var avail)
                ? avail
                : Array.Empty<VendorAvailability>();

            if (IsBlockedOnDate(windows, eventDateUtc))
                continue;

            var offerings = offeringsByVendor.TryGetValue(vendor.Id, out var list)
                ? list
                : Array.Empty<VendorOffering>();

            var budgetCap = ResolveBudgetCap(vendor.Category, budgetAllocation, totalBudget);
            var offering = SelectBestOffering(
                offerings.ToList(),
                budgetCap,
                guestCount,
                out var effectivePrice,
                out var budgetFit);

            // Vendors with priced offerings must fit budget; vendors without offerings still allowed.
            if (offerings.Count > 0 && offering is null)
                continue;

            ratings.TryGetValue(vendor.Id, out var ratingStats);

            results.Add(new Candidate(
                vendor,
                offering,
                effectivePrice,
                budgetFit,
                HasAvailabilityMatch(windows, eventDateUtc),
                ratingStats.ReviewCount > 0 ? ratingStats.AverageRating : null,
                ratingStats.ReviewCount));
        }

        return results
            .OrderByDescending(c => c.AvailabilityMatch)
            .ThenByDescending(c => c.AverageRating ?? 0)
            .ThenBy(c => c.EffectivePrice ?? decimal.MaxValue)
            .Take(MaxCandidates)
            .ToList();
    }
}
