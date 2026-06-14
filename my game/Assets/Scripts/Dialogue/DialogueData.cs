[System.Serializable]
public class DialogueLine
{
    public string id;
    public string type;
    public string speakerName;
    /// <summary>表情编号，从 1 开始；0 或未写表示不显示头像。</summary>
    public int emo;
    public string text;
}

[System.Serializable]
public class DialogueSettings
{
    public bool autoPlay;
    public string background;
    public string bgm;
}

[System.Serializable]
public class DialogueData
{
    public string id;
    public string title;
    public DialogueSettings settings;
    public DialogueLine[] lines;
}
