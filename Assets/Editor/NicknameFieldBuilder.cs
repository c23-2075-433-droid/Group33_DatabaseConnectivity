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
    // The plank art is 1874x455 (~4.12:1); the field below keeps that ratio so
    // the wood isn't stretched. Leaves occupy the left ~11.8% of the sprite, so
    // the text is inset past them - otherwise typing would run over the leaves.
    private const float PlateAspect = 1874f / 455f;
    private const float FieldWidth = 620f;
    private const float LeafInset = 0.118f;

    public static InputField Build(Transform canvas)
    {
        foreach (GameObject stale in FindAllInScene(FieldName)) Object.DestroyImmediate(stale);
        // Older versions added a separate white label above the box; the
        // wooden plank plus its placeholder says the same thing with less
        // clutter (and nothing to collide with the menu art), so remove it.
        foreach (GameObject stale in FindAllInScene(LabelName)) Object.DestroyImmediate(stale);

        float fieldHeight = FieldWidth / PlateAspect;

        GameObject fieldGO = new GameObject(FieldName, typeof(RectTransform));
        fieldGO.transform.SetParent(canvas, false);
        RectTransform fieldRect = fieldGO.GetComponent<RectTransform>();
        // Sits just above the Play button. If it clashes with the menu art,
        // move the RectTransform in the Scene view - no code change needed.
        fieldRect.anchoredPosition = new Vector2(81, 150);
        fieldRect.sizeDelta = new Vector2(FieldWidth, fieldHeight);

        Image background = fieldGO.AddComponent<Image>();
        background.sprite = LoadSprite("ui_name_plate");
        background.color = Color.white; // white = show the sprite's own colours untinted

        // Cream text, because the plank is mid-brown - dark text would not read
        // against it (the menu title uses the same cream-on-wood pairing).
        float leftPad = FieldWidth * LeafInset + 16f;
        Text placeholder = CreateFieldText(fieldGO.transform, "Placeholder", "Pangalan mo?",
            new Color(0.99f, 0.96f, 0.89f, 0.55f), FontStyle.Italic, leftPad);
        Text text = CreateFieldText(fieldGO.transform, "Text", "",
            new Color(0.99f, 0.96f, 0.89f, 1f), FontStyle.Bold, leftPad);

        InputField input = fieldGO.AddComponent<InputField>();
        input.targetGraphic = background;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.characterLimit = 16; // keeps names readable in the recent-scores list
        input.lineType = InputField.LineType.SingleLine;

        return input;
    }

    private static Text CreateFieldText(Transform parent, string name, string content,
        Color color, FontStyle style, float leftPadding)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Text text = go.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 38;
        text.fontStyle = style;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = color;
        text.supportRichText = false;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(leftPadding, 26); // clear the leaves and the plank's outline
        rect.offsetMax = new Vector2(-30, -26);
        return text;
    }

    private static Sprite LoadSprite(string spriteFile)
    {
        string path = "Assets/Sprites/" + spriteFile + ".png";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            Debug.LogWarning("[SALINLAHI] Could not load sprite at '" + path + "'. Make sure it's imported as Sprite (2D and UI).");
        return sprite;
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
