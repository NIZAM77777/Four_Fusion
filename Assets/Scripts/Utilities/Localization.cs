using System;
using System.Collections.Generic;
using UnityEngine;

public static class Localization
{
    [Serializable] public sealed class Entry { public string key; public string value; }
    [Serializable] public sealed class LanguageTable { public string code; public string name; public bool rtl; public string font; public Entry[] entries; }
    [Serializable] private sealed class Catalog { public LanguageTable[] tables; }
    private static readonly Dictionary<string, Dictionary<string, string>> values = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
    private static readonly Dictionary<string, LanguageTable> languages = new Dictionary<string, LanguageTable>(StringComparer.Ordinal);
    public static string[] Codes { get; private set; } = Array.Empty<string>();
    public static string[] Names { get; private set; } = Array.Empty<string>();
    public static string Language { get; private set; } = "en";
    public static bool IsRTL => languages.TryGetValue(Language, out var table) && table.rtl;
    public static event Action Changed;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { values.Clear(); languages.Clear(); Changed = null; Language = "en"; }
    public static void Initialize()
    {
        if (values.Count != 0) return;
        var asset = Resources.Load<TextAsset>("Fusion/Localization");
        if (asset == null) { Debug.LogError("Localization catalog missing."); return; }
        var catalog = JsonUtility.FromJson<Catalog>(asset.text);
        Codes = new string[catalog.tables.Length]; Names = new string[catalog.tables.Length];
        for (int i = 0; i < catalog.tables.Length; i++)
        {
            var table = catalog.tables[i];
            Codes[i] = table.code; Names[i] = table.name; languages.Add(table.code, table);
            var dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var entry in table.entries) dictionary.Add(entry.key, entry.value);
            values.Add(table.code, dictionary);
        }
        string device = Application.systemLanguage switch
        {
            SystemLanguage.Spanish => "es",
            SystemLanguage.French => "fr",
            SystemLanguage.Portuguese => "pt",
            SystemLanguage.Arabic => "ar",
            SystemLanguage.Hindi => "hi",
            _ => "en"
        };
        Language = PlayerPrefs.GetString("Language", device);
        if (!values.ContainsKey(Language)) Language = "en";
    }
    public static string FontName(string code) { Initialize(); return languages.TryGetValue(code, out var table) ? table.font : "NotoSans"; }
    public static void SetLanguage(string code)
    {
        Initialize();
        if (!values.ContainsKey(code)) return;
        bool changed = code != Language;
        Language = code; PlayerPrefs.SetString("Language", code); PlayerPrefs.Save();
        if (changed) Changed?.Invoke();
    }
    public static string Get(string key)
    {
        Initialize();
        if (values.TryGetValue(Language, out var current) && current.TryGetValue(key, out var text) && !string.IsNullOrEmpty(text)) return text;
        if (values.TryGetValue("en", out var fallback) && fallback.TryGetValue(key, out text)) return text;
        Debug.LogError("Missing localization key: " + key); return key;
    }
}
