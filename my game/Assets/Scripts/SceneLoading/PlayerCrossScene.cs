using UnityEngine;

/// <summary>挂在玩家根物体（如 Riko）上，切场景时不被销毁。</summary>
[DefaultExecutionOrder(-500)]
[DisallowMultipleComponent]
public class PlayerCrossScene : MonoBehaviour
{
    static GameObject persistedPlayer;

    public static bool TryGetPersistedPlayer(out GameObject player)
    {
        player = persistedPlayer;
        return player != null;
    }

    void Awake()
    {
        if (!HasPlayerComponents())
            return;

        if (persistedPlayer != null && persistedPlayer != gameObject)
        {
            Destroy(gameObject);
            return;
        }

        persistedPlayer = gameObject;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (persistedPlayer == gameObject)
            persistedPlayer = null;
    }

    bool HasPlayerComponents()
    {
        return GetComponent<CharactorController>() != null
            || GetComponent<CharacterController>() != null;
    }
}
