// Assets/Editor/NicknameFieldBuilder.cs
//
// Shared helper that builds the main menu's nickname entry box (label +
// InputField) and wires it to MainMenuController.nicknameInput.
//
// MainMenuController already handled nicknames - it pre-fills the last used
// name on Start and calls NicknameManager.SetNickname() when Play is pressed -
// there just was never an on-screen field to type into, so every play session
// saved under the default name "Bata".
//
// Used by both AddNicknameFieldToMainMenu.cs (patches the already-built menu)
// and BuildMainMenuScene.cs (fresh full rebuild).

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class NicknameFieldBuilder
{
    private const string FieldName = "NicknameInput";
    private const string LabelName = "NicknameLabel";

    /// <summary>
    /// Creates the nickname label + input field under the given canvas and
    /// returns the InputField. Any existing copies are removed first, so this
    /// is safe to re-run.
    /// </summary>
    public static InputField Build(Transform canvas)
    {
        foreach (GameObject stale in FindAllInScene(FieldName)) Object.DestroyImmediate(stale);
        foreach (GameObject stale in FindAllInScene(LabelName)) Object.DestroyImmediate(stale);

        // Sits just above the Play button. If it overlaps anything on your
        // screen, move the RectTransform in the Scene view - no code change.
        CreateLabel(canvas, LabelName, "Ano ang pangalan mo?", new Vector2(81, 210), new Vector2(560, 60));

        GameObject fieldGO = new GameObject(FieldName, typeof(RectTransform));
        fieldGO.transform.SetParent(canvas, false);
        RectTransform fieldRect = fieldGO.GetComponent<RectTransform>();
        fieldRect.anchoredPosition = new Vector2(81, 140);
        fieldRect.sizeDelta = new Vector2(560, 84);

        Image background = fieldGO.AddComponent<Image>();
        background.color = new Color(1f, 0.98f, 0.92f, 0.97f); // warm cream, readable on the menu art

        // Child text objects the InputField drives.
        Text placeholder = CreateFieldText(fieldGO.transform, "Placeholder", "Type your name...",
            new Color(0.45f, 0.38f, 0.30f), FontStyle.Italic);
        Text text = CreateFieldText(fieldGO.transform, "Text", "",
            new Color(0.20f, 0.13f, 0.07f), FontStyle.Bold);

        InputField input = fieldGO.AddComponent<InputField>();
        input.targetGraphic = background;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.characterLimit = 16; // keeps names readable in the recent-scores list
        input.lineType = InputField.LineType.SingleLine;

        return input;
    }

    private static Text CreateFieldText(Transform parent, string name, string content, Color color, FontStyle style)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Text text = go.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 34;
        text.fontStyle = style;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = color;
        text.supportRichText = false;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(20, 8);
        rect.offsetMax = new Vector2(-20, -8);
        return text;
    }

    private static void CreateLabel(Transform parent, string name, string content, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Text text = go.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 36;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
    }

    /// <summary>Finds GameObjects by name in the open scene, including inactive ones (unlike GameObject.Find).</summary>
    private static List<GameObject> FindAllInScene(string name)
    {
        List<GameObject> matches = new List<GameObject>();
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Collect(root.transform, name, matches);
        }
        return matches;
    }

    private static void Collect(Transform t, string name, List<GameObject> matches)
    {
        if (t.name == name) matches.Add(t.gameObject);
        for (int i = 0; i < t.childCount; i++) Collect(t.GetChild(i), name, matches);
    }
}
