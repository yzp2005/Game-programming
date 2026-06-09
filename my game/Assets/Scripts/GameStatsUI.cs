using TMPro;
using UnityEngine;

/// <summary>
/// 在 TMP 上显示关卡计时与击杀数。挂到 Canvas 上，拖入两个 Text 即可。
/// </summary>
[DisallowMultipleComponent]
public class GameStatsUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] TMP_Text timerText;
    [SerializeField] TMP_Text killCountText;

    [Header("计时")]
    [SerializeField] bool autoStartOnPlay = true;
    [SerializeField] bool countOnlyMonsterKills = true;

    float elapsedTime;
    int killCount;
    bool isRunning;

    public float ElapsedTime => elapsedTime;
    public int KillCount => killCount;

    void OnEnable()
    {
        MonsterHealth.OnAnyDeath += HandleDeath;
    }

    void OnDisable()
    {
        MonsterHealth.OnAnyDeath -= HandleDeath;
    }

    void Start()
    {
        if (autoStartOnPlay)
            ResetStats();
        else
            RefreshUI();
    }

    void Update()
    {
        if (!isRunning)
            return;

        elapsedTime += Time.deltaTime;
        RefreshTimerText();
    }

    public void ResetStats()
    {
        elapsedTime = 0f;
        killCount = 0;
        isRunning = true;
        RefreshUI();
    }

    public void StartTimer()
    {
        isRunning = true;
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    void HandleDeath(MonsterHealth health)
    {
        if (health == null)
            return;

        if (countOnlyMonsterKills && health.GetComponent<MonsterChaseAI>() == null)
            return;

        killCount++;
        RefreshKillText();
    }

    void RefreshUI()
    {
        RefreshTimerText();
        RefreshKillText();
    }

    void RefreshTimerText()
    {
        if (timerText == null)
            return;

        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    void RefreshKillText()
    {
        if (killCountText == null)
            return;

        killCountText.text = killCount.ToString();
    }
}
