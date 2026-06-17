using System;
using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 自动存档：标签、技能组或场景变化时写入 JSON（persistentDataPath/save/save.json）。
/// 启动时若存在存档则恢复标签，并按配置加载对应场景。
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-450)]
public class GameSaveSystem : MonoBehaviour
{
    public static GameSaveSystem Instance { get; private set; }

    const string SaveFolderName = "save";
    const string SaveFileName = "save.json";

    [Serializable]
    public class SceneSpawnEntry
    {
        public int buildIndex;
        [Tooltip("该场景唯一出生点的 Spawn Id，与 PlayerSpawnPoint 一致")]
        public string spawnId;
    }

    [Header("读档")]
    [Tooltip("仅非主菜单场景生效；主菜单请点 Continue")]
    [SerializeField] bool autoLoadOnStart = true;

    [Header("主菜单")]
    [Tooltip("这些场景不自动读档，进入时也不写入存档")]
    [SerializeField] string[] menuSceneNames = { "Main Menu" };

    [Header("场景 → 出生点（每场景一个）")]
    [SerializeField] SceneSpawnEntry[] sceneSpawns =
    {
        new SceneSpawnEntry { buildIndex = 0, spawnId = "init" },
        new SceneSpawnEntry { buildIndex = 1, spawnId = "FromVillage" }
    };

    [Header("写入")]
    [SerializeField] float saveDebounceSeconds = 0.15f;

    bool suppressAutoSave;
    Coroutine saveRoutine;

    public static string SaveDirectoryPath => Path.Combine(Application.persistentDataPath, SaveFolderName);

    public static string SaveFilePath => Path.Combine(SaveDirectoryPath, SaveFileName);

    public static bool HasSaveFile => File.Exists(SaveFilePath);

    internal void EnsureGameEventManager()
    {
        if (GameEventManager.Instance != null)
            return;

        if (GetComponent<GameEventManager>() == null)
            gameObject.AddComponent<GameEventManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (transform.parent != null)
            transform.SetParent(null);

        DontDestroyOnLoad(gameObject);

        EnsureGameEventManager();

        GameEventManager.FlagAdded += OnFlagsChanged;
        GameEventManager.FlagRemoved += OnFlagsChanged;
        SkillLoadout.Changed += OnSkillLoadoutChanged;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        if (autoLoadOnStart && !IsMenuScene(SceneManager.GetActiveScene().name))
            TryLoadSave();
    }

    void OnDestroy()
    {
        if (Instance != this)
            return;

        Instance = null;
        GameEventManager.FlagAdded -= OnFlagsChanged;
        GameEventManager.FlagRemoved -= OnFlagsChanged;
        SkillLoadout.Changed -= OnSkillLoadoutChanged;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSkillLoadoutChanged() => RequestSave();

    void OnFlagsChanged(string _) => RequestSave();

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsMenuScene(scene.name))
            return;

        RequestSave();
    }

    public void RequestSave()
    {
        if (suppressAutoSave)
            return;

        if (saveDebounceSeconds <= 0f)
        {
            SaveNow();
            return;
        }

        if (saveRoutine != null)
            StopCoroutine(saveRoutine);

        saveRoutine = StartCoroutine(SaveDebounced());
    }

    IEnumerator SaveDebounced()
    {
        yield return new WaitForSecondsRealtime(saveDebounceSeconds);
        saveRoutine = null;
        SaveNow();
    }

    public void SaveNow()
    {
        if (suppressAutoSave)
            return;

        GameSaveData data = CaptureCurrentState();
        WriteToDisk(data);
    }

    public bool TryLoadSave()
    {
        if (!HasSaveFile)
            return false;

        GameSaveData data = ReadFromDisk();
        if (data == null)
            return false;

        ApplySaveData(data);
        return true;
    }

    public static void DeleteSaveFile()
    {
        if (!HasSaveFile)
            return;

        File.Delete(SaveFilePath);
    }

    /// <summary>删存档、清空标签，黑屏过渡后进入指定场景（新游戏）。</summary>
    public static void BeginNewGame(int sceneBuildIndex)
    {
        SceneFadeLoader.LoadScene(sceneBuildIndex, PrepareNewGameData);
    }

    static void PrepareNewGameData()
    {
        if (Instance != null)
            Instance.suppressAutoSave = true;

        DeleteSaveFile();
        GameEventManager.RestoreFlags(Array.Empty<string>());
        SkillLoadout.ResetForNewGame();

        if (Instance != null)
            Instance.suppressAutoSave = false;
    }

    /// <summary>继续游戏：读存档场景与标签，经黑幕过渡进入。</summary>
    public static void ContinueGame()
    {
        if (!HasSaveFile)
            return;

        GameSaveData data = ReadFromDisk();
        if (data == null)
            return;

        SceneFadeLoader.LoadScene(data.sceneBuildIndex, () =>
        {
            if (Instance != null)
                Instance.suppressAutoSave = true;

            GameEventManager.RestoreFlags(data.flags);
            SkillLoadout.Restore(data.skillLoadout);
            QuestProgressDisplay.EnsureInstance();

            if (Instance != null)
                Instance.suppressAutoSave = false;
        });
    }

    bool IsMenuScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName) || menuSceneNames == null)
            return false;

        for (int i = 0; i < menuSceneNames.Length; i++)
        {
            string menuName = menuSceneNames[i];
            if (!string.IsNullOrWhiteSpace(menuName) && sceneName == menuName.Trim())
                return true;
        }

        return false;
    }

    GameSaveData CaptureCurrentState()
    {
        return new GameSaveData
        {
            version = GameSaveData.CurrentVersion,
            sceneBuildIndex = SceneManager.GetActiveScene().buildIndex,
            flags = GameEventManager.GetAllFlagsSorted() as string[]
                   ?? Array.Empty<string>(),
            skillLoadout = SkillLoadout.SelectedSkillIds
        };
    }

    static void ApplyPersistentState(GameSaveData data)
    {
        if (data == null)
            return;

        GameEventManager.RestoreFlags(data.flags);
        SkillLoadout.Restore(data.skillLoadout);
    }

    void ApplySaveData(GameSaveData data)
    {
        suppressAutoSave = true;

        ApplyPersistentState(data);

        int targetScene = data.sceneBuildIndex;
        int currentScene = SceneManager.GetActiveScene().buildIndex;

        if (targetScene != currentScene)
        {
            string spawnId = ResolveSpawnId(targetScene);
            if (string.IsNullOrWhiteSpace(spawnId))
            {
                Debug.LogWarning(
                    $"[GameSaveSystem] 未配置 buildIndex={targetScene} 的 spawnId，读档后玩家位置可能不正确。",
                    this);
            }
            else
            {
                SceneLoadRunner.LoadScene(targetScene, spawnId);
            }
        }

        suppressAutoSave = false;
    }

    string ResolveSpawnId(int buildIndex)
    {
        if (sceneSpawns == null)
            return string.Empty;

        for (int i = 0; i < sceneSpawns.Length; i++)
        {
            SceneSpawnEntry entry = sceneSpawns[i];
            if (entry != null && entry.buildIndex == buildIndex)
                return entry.spawnId != null ? entry.spawnId.Trim() : string.Empty;
        }

        return string.Empty;
    }

    static void WriteToDisk(GameSaveData data)
    {
        string path = SaveFilePath;
        string directory = SaveDirectoryPath;

        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        string json = JsonUtility.ToJson(data, prettyPrint: true);
        string tempPath = path + ".tmp";

        File.WriteAllText(tempPath, json);

        if (File.Exists(path))
            File.Delete(path);

        File.Move(tempPath, path);
    }

    static GameSaveData ReadFromDisk()
    {
        try
        {
            string json = File.ReadAllText(SaveFilePath);
            if (string.IsNullOrWhiteSpace(json))
                return null;

            GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
            if (data == null)
                return null;

            if (data.flags == null)
                data.flags = Array.Empty<string>();

            if (data.skillLoadout == null)
                data.skillLoadout = Array.Empty<string>();

            return data;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[GameSaveSystem] 读档失败: {ex.Message}");
            return null;
        }
    }
}
