using System;

[Serializable]
public class GameSaveData
{
    public const int CurrentVersion = 3;

    public int version = CurrentVersion;
    public int sceneBuildIndex;
    public string[] flags = Array.Empty<string>();
    public string[] skillLoadout = Array.Empty<string>();
}
