[System.Serializable]
public class DialogueLine
{
    public string id;
    public string type;
    public string speakerName;
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
