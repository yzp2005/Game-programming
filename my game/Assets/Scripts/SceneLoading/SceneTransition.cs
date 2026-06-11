/// <summary>跨场景传送时，告诉下一关用哪个出生点（LoadScene 前写入，新场景 Start 后读取并清空）。</summary>
public static class SceneTransition
{
    public static string NextSpawnPointId { get; set; }
    public static string[] FlagsToAddOnLoadComplete { get; set; }
    public static string[] FlagsToRemoveOnLoadComplete { get; set; }

    public static void QueueLoadCompleteFlags(string[] flagsToAdd, string[] flagsToRemove)
    {
        FlagsToAddOnLoadComplete = flagsToAdd;
        FlagsToRemoveOnLoadComplete = flagsToRemove;
    }

    public static void ApplyLoadCompleteFlags()
    {
        FlagEventActions.Apply(FlagsToAddOnLoadComplete, FlagsToRemoveOnLoadComplete);
        FlagsToAddOnLoadComplete = null;
        FlagsToRemoveOnLoadComplete = null;
    }
}
