using UnityEngine;

/// <summary>
/// 保证 GameEventManager + GameSaveSystem 始终存在（DontDestroyOnLoad）。
/// 主菜单 SaveSystem 上可只挂 GameSaveSystem，会自动补齐 GameEventManager。
/// 若场景里没有 SaveSystem，会在首帧自动创建。
/// </summary>
static class GameSystemsBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureAfterSceneLoad()
    {
        if (GameSaveSystem.Instance != null)
        {
            GameSaveSystem.Instance.EnsureGameEventManager();
            QuestProgressDisplay.EnsureInstance();
            return;
        }

        var go = new GameObject("SaveSystem");
        go.AddComponent<GameEventManager>();
        go.AddComponent<GameSaveSystem>();
        QuestProgressDisplay.EnsureInstance();
    }
}
