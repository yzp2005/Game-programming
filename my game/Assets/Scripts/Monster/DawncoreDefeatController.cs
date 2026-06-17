using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 监听 Dawncore（CoreHealth）被摧毁：显示 Lose UI，点击任意处后按当前 flag 回溯并带加载界面重载场景。
/// </summary>
[DisallowMultipleComponent]
public class DawncoreDefeatController : MonoBehaviour
{
    [Serializable]
    public class RollbackEntry
    {
        [Tooltip("当前存在此 flag 时使用本规则（按列表顺序，先匹配先生效）")]
        public string whenHasFlag;

        [Header("关闭 Lose UI 后写入（如 fight_level1 → phase3_level1）")]
        public string[] flagsToAdd;
        public string[] flagsToRemove;

        [Tooltip("留空则使用 Default Spawn Id")]
        public string spawnIdOverride;
    }

    [Header("引用")]
    [SerializeField] CoreHealth coreHealth;
    [SerializeField] GameObject loseUiRoot;

    [Header("重载")]
    [Tooltip("与 PlayerSpawnPoint.SpawnId 一致")]
    [SerializeField] string defaultSpawnId = "FromVillage";
    [SerializeField] RollbackEntry[] rollbackRules;

    [Header("输入")]
    [SerializeField] float dismissDelaySeconds = 0.35f;
    [SerializeField] bool pauseTimeWhileLoseUi = true;

    bool defeatShown;
    bool awaitingDismiss;
    float loseUiShownAt;
    RollbackEntry pendingRollback;

    void Awake()
    {
        if (coreHealth == null)
            coreHealth = FindObjectOfType<CoreHealth>();

        HideLoseUiImmediate();
    }

    void OnEnable()
    {
        if (coreHealth != null)
            coreHealth.OnDestroyed += HandleCoreDestroyed;
    }

    void OnDisable()
    {
        if (coreHealth != null)
            coreHealth.OnDestroyed -= HandleCoreDestroyed;
    }

    void Update()
    {
        if (!awaitingDismiss)
            return;

        if (Time.unscaledTime - loseUiShownAt < dismissDelaySeconds)
            return;

        if (!TryGetDismissInput())
            return;

        DismissAndReload();
    }

    void HandleCoreDestroyed()
    {
        if (defeatShown)
            return;

        defeatShown = true;
        pendingRollback = ResolveRollbackRule();

        StopActiveWaves();
        ShowLoseUi();

        if (pauseTimeWhileLoseUi)
            Time.timeScale = 0f;

        PlayerInputLock.SetLocked(true, dialogueCursor: true);
        awaitingDismiss = true;
        loseUiShownAt = Time.unscaledTime;
    }

    RollbackEntry ResolveRollbackRule()
    {
        if (rollbackRules == null)
            return null;

        for (int i = 0; i < rollbackRules.Length; i++)
        {
            RollbackEntry entry = rollbackRules[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.whenHasFlag))
                continue;

            if (GameEventManager.Has(entry.whenHasFlag.Trim()))
                return entry;
        }

        return null;
    }

    void ShowLoseUi()
    {
        if (loseUiRoot == null)
        {
            Debug.LogWarning($"{name}: Lose UI 未绑定。", this);
            return;
        }

        Transform rootTransform = loseUiRoot.transform;
        if (rootTransform.localScale.sqrMagnitude < 0.001f)
            rootTransform.localScale = Vector3.one;

        loseUiRoot.SetActive(true);
    }

    void HideLoseUiImmediate()
    {
        if (loseUiRoot == null)
            return;

        loseUiRoot.SetActive(false);
    }

    void DismissAndReload()
    {
        awaitingDismiss = false;
        HideLoseUiImmediate();

        if (pauseTimeWhileLoseUi)
            Time.timeScale = 1f;

        ApplyRollbackAndReload(pendingRollback);
    }

    void ApplyRollbackAndReload(RollbackEntry entry)
    {
        string[] flagsToAdd = entry?.flagsToAdd;
        string[] flagsToRemove = entry?.flagsToRemove;
        string spawnId = entry != null && !string.IsNullOrWhiteSpace(entry.spawnIdOverride)
            ? entry.spawnIdOverride.Trim()
            : defaultSpawnId;

        if (entry == null)
            Debug.LogWarning($"{name}: 未匹配到 defeat rollback 规则，仅重载场景。", this);
        else
            FlagEventActions.Apply(flagsToAdd, flagsToRemove);

        int buildIndex = SceneManager.GetActiveScene().buildIndex;
        SceneTransition.NextSpawnPointId = spawnId;
        PlayerInputLock.ForceUnlockGameplay();

        if (FindObjectOfType<SceneLoadRunner>() != null)
        {
            SceneLoadRunner.LoadScene(buildIndex, spawnId);
            return;
        }

        if (SceneFadeLoader.Instance != null)
        {
            SceneFadeLoader.LoadScene(buildIndex);
            SceneManager.sceneLoaded += OnReloadSceneLoaded;
            return;
        }

        SceneManager.sceneLoaded -= OnReloadSceneLoaded;
        SceneManager.sceneLoaded += OnReloadSceneLoaded;
        SceneManager.LoadScene(buildIndex);
    }

    static void OnReloadSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnReloadSceneLoaded;
        PlayerSpawnOnLoad.ApplyIfNeeded();
        PlayerInputLock.ForceUnlockGameplay();
        Time.timeScale = 1f;
    }

    static bool TryGetDismissInput()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            return true;

        if (Input.touchCount > 0)
        {
            TouchPhase phase = Input.GetTouch(0).phase;
            if (phase == TouchPhase.Began || phase == TouchPhase.Ended)
                return true;
        }

        return Input.anyKeyDown;
    }

    static void StopActiveWaves()
    {
        foreach (WaveManager waveManager in FindObjectsOfType<WaveManager>())
        {
            if (waveManager != null && waveManager.IsRunning)
                waveManager.StopWaves();
        }
    }
}
