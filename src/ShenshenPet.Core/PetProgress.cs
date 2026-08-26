using System.Globalization;

namespace ShenshenPet.Core;

public static class PetProgress
{
    public const int DailyRiceReward = 3;
    public const int FeedCost = 1;
    public const int ExperiencePerFeed = 1;
    public const int ExperiencePerLevel = 5;
    public const int MaximumRice = 999;
    public const int MaximumBondLevel = 99;

    public static int GetBondLevel(PetSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var experience = Math.Max(0, settings.BondExperience);
        return Math.Min(MaximumBondLevel, 1 + (experience / ExperiencePerLevel));
    }

    public static bool TryClaimDailyRice(PetSettings settings, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (DateOnly.TryParseExact(
                settings.LastDailyRiceClaim,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var claimedOn)
            && claimedOn == today)
        {
            return false;
        }

        settings.Rice = Math.Min(MaximumRice, Math.Max(0, settings.Rice) + DailyRiceReward);
        settings.LastDailyRiceClaim = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return true;
    }

    public static bool TryFeed(PetSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (settings.Rice < FeedCost)
        {
            settings.Rice = Math.Max(0, settings.Rice);
            return false;
        }

        settings.Rice -= FeedCost;
        var maximumExperience = (MaximumBondLevel - 1) * ExperiencePerLevel;
        settings.BondExperience = Math.Min(
            maximumExperience,
            Math.Max(0, settings.BondExperience) + ExperiencePerFeed);
        return true;
    }
}
