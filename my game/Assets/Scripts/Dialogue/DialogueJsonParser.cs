using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// 解析 Dialogue JSON。不使用 JsonUtility（对 lines 内 int 字段不可靠）。
/// </summary>
public static class DialogueJsonParser
{
    public static bool TryParse(string json, out DialogueData data)
    {
        data = null;
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogError("[DialogueReader] JSON 为空。");
            return false;
        }

        try
        {
            int index = 0;
            data = ReadDialogueData(json, ref index);
            SkipWhitespace(json, ref index);

            if (data?.lines == null || data.lines.Length == 0)
            {
                Debug.LogError("[DialogueReader] JSON 解析失败或 lines 为空。");
                data = null;
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError("[DialogueReader] JSON 解析失败: " + ex.Message);
            data = null;
            return false;
        }
    }

    static DialogueData ReadDialogueData(string json, ref int index)
    {
        var data = new DialogueData();
        ReadObjectStart(json, ref index);

        while (ReadObjectFieldKey(json, ref index, out string key))
        {
            switch (key)
            {
                case "id":
                    data.id = ReadString(json, ref index);
                    break;
                case "title":
                    data.title = ReadString(json, ref index);
                    break;
                case "settings":
                    data.settings = ReadSettings(json, ref index);
                    break;
                case "lines":
                    data.lines = ReadLines(json, ref index);
                    break;
                default:
                    SkipValue(json, ref index);
                    break;
            }

            if (ReadObjectFieldEnd(json, ref index))
                break;
        }

        return data;
    }

    static DialogueSettings ReadSettings(string json, ref int index)
    {
        var settings = new DialogueSettings();
        ReadObjectStart(json, ref index);

        while (ReadObjectFieldKey(json, ref index, out string key))
        {
            switch (key)
            {
                case "autoPlay":
                    settings.autoPlay = ReadBool(json, ref index);
                    break;
                case "background":
                    settings.background = ReadStringOrNull(json, ref index);
                    break;
                case "bgm":
                    settings.bgm = ReadStringOrNull(json, ref index);
                    break;
                default:
                    SkipValue(json, ref index);
                    break;
            }

            if (ReadObjectFieldEnd(json, ref index))
                break;
        }

        return settings;
    }

    static DialogueLine[] ReadLines(string json, ref int index)
    {
        SkipWhitespace(json, ref index);
        Expect(json, ref index, '[');

        var lines = new List<DialogueLine>();
        SkipWhitespace(json, ref index);
        if (TryConsume(json, ref index, ']'))
            return lines.ToArray();

        while (true)
        {
            lines.Add(ReadLine(json, ref index));
            SkipWhitespace(json, ref index);
            if (TryConsume(json, ref index, ','))
                continue;
            Expect(json, ref index, ']');
            break;
        }

        return lines.ToArray();
    }

    static DialogueLine ReadLine(string json, ref int index)
    {
        var line = new DialogueLine();
        ReadObjectStart(json, ref index);

        while (ReadObjectFieldKey(json, ref index, out string key))
        {
            switch (key)
            {
                case "id":
                    line.id = ReadString(json, ref index);
                    break;
                case "type":
                    line.type = ReadString(json, ref index);
                    break;
                case "speakerName":
                    line.speakerName = ReadString(json, ref index);
                    break;
                case "emo":
                    line.emo = ReadInt(json, ref index);
                    break;
                case "text":
                    line.text = ReadString(json, ref index);
                    break;
                default:
                    SkipValue(json, ref index);
                    break;
            }

            if (ReadObjectFieldEnd(json, ref index))
                break;
        }

        return line;
    }

    static void ReadObjectStart(string json, ref int index)
    {
        SkipWhitespace(json, ref index);
        Expect(json, ref index, '{');
        SkipWhitespace(json, ref index);
    }

    static bool ReadObjectFieldKey(string json, ref int index, out string key)
    {
        if (TryConsume(json, ref index, '}'))
        {
            key = null;
            return false;
        }

        key = ReadString(json, ref index);
        SkipWhitespace(json, ref index);
        Expect(json, ref index, ':');
        return true;
    }

    static bool ReadObjectFieldEnd(string json, ref int index)
    {
        SkipWhitespace(json, ref index);
        if (TryConsume(json, ref index, ','))
            return false;

        Expect(json, ref index, '}');
        return true;
    }

    static string ReadString(string json, ref int index)
    {
        SkipWhitespace(json, ref index);
        Expect(json, ref index, '"');

        var chars = new List<char>();
        while (index < json.Length)
        {
            char c = json[index++];
            if (c == '"')
                return new string(chars.ToArray());

            if (c == '\\')
            {
                if (index >= json.Length)
                    throw new FormatException("字符串转义不完整。");

                char escape = json[index++];
                switch (escape)
                {
                    case '"': chars.Add('"'); break;
                    case '\\': chars.Add('\\'); break;
                    case '/': chars.Add('/'); break;
                    case 'b': chars.Add('\b'); break;
                    case 'f': chars.Add('\f'); break;
                    case 'n': chars.Add('\n'); break;
                    case 'r': chars.Add('\r'); break;
                    case 't': chars.Add('\t'); break;
                    case 'u':
                        if (index + 4 > json.Length)
                            throw new FormatException("Unicode 转义不完整。");
                        string hex = json.Substring(index, 4);
                        index += 4;
                        chars.Add((char)Convert.ToInt32(hex, 16));
                        break;
                    default:
                        throw new FormatException("未知转义字符: \\" + escape);
                }
                continue;
            }

            chars.Add(c);
        }

        throw new FormatException("字符串未闭合。");
    }

    static string ReadStringOrNull(string json, ref int index)
    {
        SkipWhitespace(json, ref index);
        if (TryReadNull(json, ref index))
            return null;

        return ReadString(json, ref index);
    }

    static int ReadInt(string json, ref int index)
    {
        SkipWhitespace(json, ref index);
        int start = index;

        if (index < json.Length && json[index] == '-')
            index++;

        if (index >= json.Length || !char.IsDigit(json[index]))
            throw new FormatException("期望数字。");

        while (index < json.Length && char.IsDigit(json[index]))
            index++;

        string number = json.Substring(start, index - start);
        if (!int.TryParse(number, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            throw new FormatException("整数超出范围: " + number);

        return value;
    }

    static bool ReadBool(string json, ref int index)
    {
        SkipWhitespace(json, ref index);
        if (TryConsumeLiteral(json, ref index, "true"))
            return true;
        if (TryConsumeLiteral(json, ref index, "false"))
            return false;

        throw new FormatException("期望 true/false。");
    }

    static void SkipValue(string json, ref int index)
    {
        SkipWhitespace(json, ref index);
        if (index >= json.Length)
            throw new FormatException("JSON 意外结束。");

        char c = json[index];
        switch (c)
        {
            case '"':
                ReadString(json, ref index);
                break;
            case '{':
                SkipObject(json, ref index);
                break;
            case '[':
                SkipArray(json, ref index);
                break;
            default:
                if (TryReadNull(json, ref index))
                    return;
                if (TryConsumeLiteral(json, ref index, "true") || TryConsumeLiteral(json, ref index, "false"))
                    return;
                ReadNumber(json, ref index);
                break;
        }
    }

    static void SkipObject(string json, ref int index)
    {
        ReadObjectStart(json, ref index);

        while (ReadObjectFieldKey(json, ref index, out _))
        {
            SkipValue(json, ref index);
            if (ReadObjectFieldEnd(json, ref index))
                break;
        }
    }

    static void SkipArray(string json, ref int index)
    {
        Expect(json, ref index, '[');
        SkipWhitespace(json, ref index);
        if (TryConsume(json, ref index, ']'))
            return;

        while (true)
        {
            SkipValue(json, ref index);
            SkipWhitespace(json, ref index);
            if (TryConsume(json, ref index, ','))
                continue;
            Expect(json, ref index, ']');
            break;
        }
    }

    static void ReadNumber(string json, ref int index)
    {
        int start = index;
        if (json[index] == '-')
            index++;

        while (index < json.Length && (char.IsDigit(json[index]) || json[index] == '.' || json[index] == 'e' || json[index] == 'E' || json[index] == '+' || json[index] == '-'))
            index++;

        if (start == index)
            throw new FormatException("期望数字。");
    }

    static bool TryReadNull(string json, ref int index)
    {
        return TryConsumeLiteral(json, ref index, "null");
    }

    static bool TryConsumeLiteral(string json, ref int index, string literal)
    {
        SkipWhitespace(json, ref index);
        if (index + literal.Length > json.Length)
            return false;
        if (!string.Equals(json.Substring(index, literal.Length), literal, StringComparison.Ordinal))
            return false;

        index += literal.Length;
        return true;
    }

    static bool TryConsume(string json, ref int index, char expected)
    {
        SkipWhitespace(json, ref index);
        if (index >= json.Length || json[index] != expected)
            return false;

        index++;
        return true;
    }

    static void Expect(string json, ref int index, char expected)
    {
        SkipWhitespace(json, ref index);
        if (index >= json.Length || json[index] != expected)
            throw new FormatException("期望字符 '" + expected + "'。");

        index++;
    }

    static void SkipWhitespace(string json, ref int index)
    {
        while (index < json.Length && char.IsWhiteSpace(json[index]))
            index++;
    }
}
