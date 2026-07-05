public static class MissionResultState
{
    public static bool HasPendingResult { get; private set; }
    public static FireGraphOutcome Outcome { get; private set; } = FireGraphOutcome.InProgress;
    public static MissionDefeatReason DefeatReason { get; private set; } = MissionDefeatReason.None;
    public static string MissionDisplayName { get; private set; } = "Mision";
    public static string RetrySceneName { get; private set; } = string.Empty;
    public static string ContinueSceneName { get; private set; } = string.Empty;
    public static string RetryButtonLabel { get; private set; } = "Reintentar Mision";

    public static void SetResult(
        FireGraphOutcome outcome,
        MissionDefeatReason defeatReason,
        string missionDisplayName,
        string retrySceneName,
        string continueSceneName = "",
        string retryButtonLabel = "Reintentar Mision")
    {
        HasPendingResult = outcome == FireGraphOutcome.Victory || outcome == FireGraphOutcome.Defeat;
        Outcome = outcome;
        DefeatReason = defeatReason;
        MissionDisplayName = string.IsNullOrWhiteSpace(missionDisplayName) ? "Mision" : missionDisplayName;
        RetrySceneName = retrySceneName ?? string.Empty;
        ContinueSceneName = continueSceneName ?? string.Empty;
        RetryButtonLabel = string.IsNullOrWhiteSpace(retryButtonLabel)
            ? "Reintentar Mision"
            : retryButtonLabel;
    }

    public static void Clear()
    {
        HasPendingResult = false;
        Outcome = FireGraphOutcome.InProgress;
        DefeatReason = MissionDefeatReason.None;
        MissionDisplayName = "Mision";
        RetrySceneName = string.Empty;
        ContinueSceneName = string.Empty;
        RetryButtonLabel = "Reintentar Mision";
    }
}
