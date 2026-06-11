using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>监听 finish_level{n}，延迟后显示用时；关闭时 Remove finish_level{n} 并 Set phase1_level{n+1}。</summary>
[DisallowMultipleComponent]
public class LevelVictoryPanel : MonoBehaviour
{
    const string FinishPrefix = "finish_level";
    const string PhasePrefix = "phase1_level";

    [SerializeField] float showDelay = 3f;
    [SerializeField] GameObject panelRoot;
    [SerializeField] TMP_Text timeText;

    readonly HashSet<string> triggeredFlags = new HashSet<string>();
    Coroutine showRoutine;
    bool panelVisible;
    bool lockedInput;
    string pendingFinishFlag;

    void Awake()
    {
        GameEventManager.FlagAdded += OnFlagAdded;

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    void OnDestroy()
    {
        GameEventManager.FlagAdded -= OnFlagAdded;

        if (showRoutine != null)
            StopCoroutine(showRoutine);
    }

    void Update()
    {
        if (panelVisible && Input.GetMouseButtonDown(0))
            HidePanel();
    }

    void OnFlagAdded(string flag) => TryShow(flag);

    void TryShow(string flag)
    {
        if (!TryParseFinishLevelNumber(flag, out _) || triggeredFlags.Contains(flag))
            return;

        triggeredFlags.Add(flag);
        pendingFinishFlag = flag;

        if (showRoutine != null)
            StopCoroutine(showRoutine);

        showRoutine = StartCoroutine(ShowAfterDelay());
    }

    static bool TryParseFinishLevelNumber(string flag, out int levelNumber)
    {
        levelNumber = 0;

        if (string.IsNullOrEmpty(flag) || !flag.StartsWith(FinishPrefix))
            return false;

        string suffix = flag.Substring(FinishPrefix.Length);
        return suffix.Length > 0 && int.TryParse(suffix, out levelNumber);
    }

    IEnumerator ShowAfterDelay()
    {
        string time = "00:00";
        if (GameStatsUI.Instance != null)
        {
            GameStatsUI.Instance.StopTimer();
            time = GameStatsUI.Instance.GetFormattedTime();
        }

        if (showDelay > 0f)
            yield return new WaitForSeconds(showDelay);

        showRoutine = null;

        if (panelRoot == null)
            yield break;

        if (timeText != null)
            timeText.text = time;

        panelRoot.SetActive(true);
        panelVisible = true;
        PlayerInputLock.SetLocked(true);
        lockedInput = true;
    }

    void HidePanel()
    {
        if (!panelVisible)
            return;

        if (panelRoot != null)
            panelRoot.SetActive(false);

        panelVisible = false;

        if (lockedInput)
        {
            PlayerInputLock.SetLocked(false);
            lockedInput = false;
        }

        ApplyPhaseTransition();
    }

    void ApplyPhaseTransition()
    {
        if (!TryParseFinishLevelNumber(pendingFinishFlag, out int levelNumber))
            return;

        GameEventManager.Remove(pendingFinishFlag);
        GameEventManager.Set($"{PhasePrefix}{levelNumber + 1}");
        pendingFinishFlag = null;
    }
}
