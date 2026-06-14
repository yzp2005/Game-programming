using System;
using UnityEngine;

/// <summary>
/// 说话人 → 表情编号 → Sprite。emo 从 1 开始，对应 emoSprites[emo - 1]。
/// </summary>
[CreateAssetMenu(fileName = "DialoguePortraitDatabase", menuName = "Dialogue/Portrait Database")]
public class DialoguePortraitDatabase : ScriptableObject
{
    [Serializable]
    public class SpeakerEntry
    {
        public string speakerName;
        public Sprite[] emoSprites;
    }

    [SerializeField] SpeakerEntry[] speakers;

    public bool TryGetSprite(string speakerName, int emo, out Sprite sprite)
    {
        sprite = null;

        if (emo <= 0 || string.IsNullOrWhiteSpace(speakerName) || speakers == null)
            return false;

        for (int i = 0; i < speakers.Length; i++)
        {
            SpeakerEntry entry = speakers[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.speakerName))
                continue;

            if (!string.Equals(entry.speakerName.Trim(), speakerName.Trim(), StringComparison.OrdinalIgnoreCase))
                continue;

            if (entry.emoSprites == null || emo > entry.emoSprites.Length)
                return false;

            sprite = entry.emoSprites[emo - 1];
            return sprite != null;
        }

        return false;
    }
}
