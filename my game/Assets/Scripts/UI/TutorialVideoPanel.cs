using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// 新手指引视频面板：按顺序播放多个 VideoClip，左键点击切换下一段；播完后关闭。
/// </summary>
[DisallowMultipleComponent]
public class TutorialVideoPanel : MonoBehaviour
{
    [Serializable]
    public class VideoStep
    {
        public VideoClip clip;
        [TextArea(2, 6)]
        public string description;
    }

    [Header("内容")]
    [SerializeField] VideoStep[] steps;

    [Header("UI")]
    [Tooltip("留空则关闭本物体")]
    [SerializeField] GameObject panelRoot;
    [SerializeField] RawImage videoDisplay;
    [SerializeField] TMP_Text descriptionText;

    [Header("视频")]
    [SerializeField] VideoPlayer videoPlayer;
    [SerializeField] RenderTexture renderTexture;
    [SerializeField] Vector2Int renderTextureSize = new Vector2Int(1280, 720);

    VideoStep[] activeSteps;
    int currentIndex = -1;
    bool isOpen;

    public event Action PanelOpened;
    public event Action PanelClosed;

    public bool IsOpen => isOpen;

    public bool HasConfiguredSteps => steps != null && steps.Length > 0;

    GameObject PanelRootObject => panelRoot != null ? panelRoot : gameObject;

    void Awake()
    {
        EnsureVideoPlayer();
        EnsureHidden();
    }

    void OnDestroy()
    {
        if (renderTexture != null && renderTexture.IsCreated())
            renderTexture.Release();
    }

    void Update()
    {
        if (!isOpen)
            return;

        if (Input.GetMouseButtonDown(0))
            AdvanceOrClose();
    }

    public void OpenPanel()
    {
        OpenPanel(steps);
    }

    public void OpenPanel(VideoStep[] overrideSteps)
    {
        activeSteps = overrideSteps != null && overrideSteps.Length > 0 ? overrideSteps : steps;

        if (activeSteps == null || activeSteps.Length == 0)
        {
            Debug.LogWarning($"{name}: 没有配置视频步骤。", this);
            return;
        }

        isOpen = true;
        PanelRootObject.SetActive(true);

        ShowStep(0);
        PanelOpened?.Invoke();
    }

    public void EnsureHidden()
    {
        isOpen = false;
        currentIndex = -1;
        StopVideo();
        PanelRootObject.SetActive(false);
    }

    public void ClosePanel()
    {
        if (!isOpen)
            return;

        isOpen = false;
        currentIndex = -1;
        StopVideo();
        PanelRootObject.SetActive(false);
        PanelClosed?.Invoke();
    }

    void AdvanceOrClose()
    {
        if (currentIndex + 1 < activeSteps.Length)
            ShowStep(currentIndex + 1);
        else
            ClosePanel();
    }

    void ShowStep(int index)
    {
        currentIndex = index;
        VideoStep step = activeSteps[index];

        if (descriptionText != null)
            descriptionText.text = step != null ? step.description ?? string.Empty : string.Empty;

        EnsureVideoPlayer();

        videoPlayer.Stop();

        VideoClip clip = step != null ? step.clip : null;
        videoPlayer.clip = clip;

        if (clip != null)
            videoPlayer.Play();
    }

    void StopVideo()
    {
        if (videoPlayer == null)
            return;

        if (videoPlayer.isPlaying)
            videoPlayer.Stop();

        videoPlayer.clip = null;
    }

    void EnsureVideoPlayer()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        if (videoPlayer == null)
            videoPlayer = gameObject.AddComponent<VideoPlayer>();

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = true;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(
                renderTextureSize.x,
                renderTextureSize.y,
                0,
                RenderTextureFormat.ARGB32)
            {
                name = "TutorialVideoRT"
            };
        }

        if (!renderTexture.IsCreated())
            renderTexture.Create();

        videoPlayer.targetTexture = renderTexture;

        if (videoDisplay != null)
            videoDisplay.texture = renderTexture;
    }
}
