using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > SALINLAHI > Rebuild Stress References: re-measures Teacher Rachel's
/// word recordings and rewrites Assets/Scripts/StressReferenceData.cs,
/// RawStressReferenceData.cs and PronunciationReferenceData.cs. Run it
/// after re-recording any word that WordStress marks. (Takes a few minutes:
/// every word is judged under a dozen simulated phones and speakers.)
/// </summary>
public static class StressReferenceTool
{
    private const string VoFolder = "Assets/Audio/VO";
    private const string DataFile = "Assets/Scripts/StressReferenceData.cs";
    private const string RawDataFile = "Assets/Scripts/RawStressReferenceData.cs";
    private const string PronunciationDataFile = "Assets/Scripts/PronunciationReferenceData.cs";
    private const string TemplateDataFile = "Assets/Scripts/PronunciationTemplateData.cs";

    [MenuItem("Tools/SALINLAHI/Rebuild Stress References")]
    public static void Rebuild()
    {
        StressReferenceBuilder.Result result = StressReferenceBuilder.Build(VoFolder);
        File.WriteAllText(DataFile, StressReferenceBuilder.Generate(result));
        StressReferenceBuilder.Result raw = StressReferenceBuilder.BuildRaw(VoFolder);
        File.WriteAllText(RawDataFile, StressReferenceBuilder.GenerateRaw(raw));
        PronunciationReferenceBuilder.Result pron = PronunciationReferenceBuilder.Build(VoFolder);
        File.WriteAllText(PronunciationDataFile, PronunciationReferenceBuilder.Generate(pron));
        AssetDatabase.ImportAsset(DataFile);
        AssetDatabase.ImportAsset(RawDataFile);
        PronunciationTemplateBuilder.Result templates = PronunciationTemplateBuilder.Build(VoFolder);
        File.WriteAllText(TemplateDataFile, PronunciationTemplateBuilder.Generate(templates));
        AssetDatabase.ImportAsset(PronunciationDataFile);
        AssetDatabase.ImportAsset(TemplateDataFile);

        List<string> trusted = new List<string>();
        foreach (KeyValuePair<string, PronunciationReferenceBuilder.WordResult> row in pron.rows)
            if (row.Value.entry.trusted) trusted.Add(row.Key);
        Debug.Log("Pronunciation references rebuilt. Trusted (Rachel passes and every wrong stress fails on every " +
                  "simulated phone and speaker): " + (trusted.Count > 0 ? string.Join(", ", trusted.ToArray()) : "(none)"));

        List<string> enforced = new List<string>(), notEnforced = new List<string>();
        foreach (KeyValuePair<string, StressReferenceBuilder.WordResult> row in result.rows)
            (row.Value.enforced ? enforced : notEnforced).Add(row.Key);

        List<string> rawEnforced = new List<string>();
        foreach (KeyValuePair<string, StressReferenceBuilder.WordResult> row in raw.rows)
            if (row.Value.enforced) rawEnforced.Add(row.Key);

        string summary = "Stress references rebuilt. From recorded audio, enforced for: " +
                         (rawEnforced.Count > 0 ? string.Join(", ", rawEnforced.ToArray()) : "(none)") +
                         ". From loudness readings (fallback), enforced for: " +
                         (enforced.Count > 0 ? string.Join(", ", enforced.ToArray()) : "(none)");
        if (notEnforced.Count > 0)
            summary += ". Measured and logged only (recording does not separate the two stresses clearly): " +
                       string.Join(", ", notEnforced.ToArray());
        if (result.missing.Count > 0)
            summary += ". No recording: " + string.Join(", ", result.missing.ToArray());
        Debug.Log(summary);
    }
}
