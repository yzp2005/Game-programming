using System;

[Serializable]
public class GameSaveData
{
    public const int CurrentVersion = 4;

    public int version = CurrentVersion;
    public int sceneBuildIndex;
    public string[] flags = Array.Empty<string>();
}
