using UnityEngine;
using Michsky.MUIP;

/// <summary>
/// 挂到按钮上，在 ButtonManager 的 On Click 中绑定 ShowAnnouncement。
/// 仅在按钮按下时调用 NotificationManager.Open()。
/// </summary>
public class AnnouncementDisplay : MonoBehaviour
{
    [SerializeField] private NotificationManager notification;

    /// <summary>供 ButtonManager → On Click () 绑定。</summary>
    public void ShowAnnouncement()
    {
        if (notification == null)
        {
            Debug.LogWarning("[AnnouncementDisplay] 请在 Inspector 中指定 Notification。", this);
            return;
        }

        notification.Open();
    }
}
