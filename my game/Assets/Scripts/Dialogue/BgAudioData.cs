using System;

/// <summary>bg_audio.json一条完整配音 + 每句起止时间（秒）。</summary>
[Serializable]
public class BgAudioSegment
{
    public float start;
    public float end;
    public string text;
}

[Serializable]
public class BgAudioData
{
    public string title;
    public string language;
    public BgAudioSegment[] segments;
}
