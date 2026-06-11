/// <summary>批量 Set / Remove GameEventManager 标签。</summary>
public static class FlagEventActions
{
    public static void Apply(string[] flagsToAdd, string[] flagsToRemove)
    {
        if (flagsToAdd != null)
        {
            foreach (string flag in flagsToAdd)
            {
                if (!string.IsNullOrWhiteSpace(flag))
                    GameEventManager.Set(flag.Trim());
            }
        }

        if (flagsToRemove != null)
        {
            foreach (string flag in flagsToRemove)
            {
                if (!string.IsNullOrWhiteSpace(flag))
                    GameEventManager.Remove(flag.Trim());
            }
        }
    }
}
