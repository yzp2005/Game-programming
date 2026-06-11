using TMPro;
using UnityEngine;

/// <summary>
/// 关卡全局统计：时间、击杀、已放置人数、剩余巧克力。
/// </summary>
[DisallowMultipleComponent]
public class GameStatsUI : MonoBehaviour
{
    public static GameStatsUI Instance { get; private set; }

    [Header("UI")]
    [SerializeField] TMP_Text timerText;
    [SerializeField] TMP_Text killCountText;
    [SerializeField] TMP_Text populationText;
    [SerializeField] TMP_Text chocolateText;

    [Header("初始值")]
    [SerializeField] int startingChocolate = 10;
    [SerializeField] bool autoStartOnPlay = true;
    [SerializeField] bool countOnlyMonsterKills = true;

    float elapsedTime;
    int killCount;
    int placedPopulation;
    int chocolateRemaining;
    bool isRunning;

    public float ElapsedTime => elapsedTime;
    public bool IsTimerRunning => isRunning;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"{name}: 场景里存在多个 GameStatsUI，将使用 {Instance.name}。", this);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void OnEnable() => MonsterHealth.OnAnyDeath += HandleDeath;

    void OnDisable() => MonsterHealth.OnAnyDeath -= HandleDeath;

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
        placedPopulation = 0;
        chocolateRemaining = startingChocolate;
        isRunning = true;
        RefreshUI();
    }

    public void StopTimer()
    {
        isRunning = false;
        RefreshTimerText();
    }

    public string GetFormattedTime()
    {
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    public bool CanAfford(int cost) => chocolateRemaining >= cost;

    public bool TryPlaceNpc(int chocolateCost)
    {
        chocolateCost = Mathf.Max(0, chocolateCost);
        if (!CanAfford(chocolateCost))
            return false;

        chocolateRemaining -= chocolateCost;
        placedPopulation++;
        RefreshUI();
        return true;
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
        RefreshPopulationText();
        RefreshChocolateText();
    }

    void RefreshTimerText()
    {
        if (timerText == null)
            return;

        timerText.text = GetFormattedTime();
    }

    void RefreshKillText()
    {
        if (killCountText != null)
            killCountText.text = killCount.ToString();
    }

    void RefreshPopulationText()
    {
        if (populationText != null)
            populationText.text = placedPopulation.ToString();
    }

    void RefreshChocolateText()
    {
        if (chocolateText != null)
            chocolateText.text = chocolateRemaining.ToString();
    }
}
