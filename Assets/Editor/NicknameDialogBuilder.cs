// Assets/Editor/NicknameDialogBuilder.cs
//
// Builds the "Enter Nickname" modal dialog shown when Play is pressed:
// a dimmed backdrop, the bamboo panel, the name input slot, and OKAY /
// CANCEL buttons.
//
// Art pieces (Assets/Sprites/), all with their labels painted in:
//   ui_dialog_panel - frame, with "Enter Nickname:" already on it
//   ui_input_slot   - wooden surround with a white writable interior
//   btn_okay        - "OKAY"
//   btn_cancel      - "CANCEL"
//
// Element sizes and positions below were measured off the group's composed
// mockup by diffing it against the empty frame, then expressed as fractions
// of the panel so they scale with it. To adjust, move the RectTransforms in
// the Scene view - no code change needed.
//
// Used by AddNicknameDialogToMainMenu.cs (patches the built menu) and
// BuildMainMenuScene.cs (fresh rebuild).

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class NicknameDialogBuilder
{
    public const string RootName = "NicknamePanel";

    // Panel art is 1517x924 (~1.64:1); everything else is sized relative to it.
    private const float PanelWidth = 1150f;
    private const float PanelAspect = 1517f / 924f;
    private const float Scale = PanelWidth / 1517f;

    // The panel's interior isn't centred on the sprite - the bamboo and leaves
    // sit further out on the left - so the contents sit slightly left of centre.
    private const float ContentCentreX = -15f;

    /// <summary>
    /// Result of building the dialog, so callers can wire MainMenuController.
    /// </summary>
    public class Dialog
    {
        public GameObject root;
        public InputField input;
        public Button okay;
        public Button cancel;
    }

    /// <summary>
    /// Creates the dialog under the given canvas, replacing any previous copy.
    /// Starts inactive - MainMenuController shows it when Play is pressed.
    /// </summary>
    public static Dialog Build(Transform canvas)
    {
        foreach (GameObject stale in FindAllInScene(RootName)) Object.DestroyImmediate(stale);

        float panelHeight = PanelWidth / PanelAspect;

        // Full-screen root: also catches clicks so the menu behind can't be
        // used while the dialog is open.
        GameObject root = new GameObject(RootName, typeof(RectTransform));
        root.transform.SetParent(canvas, false);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        Image dim = root.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.55f);

        // Panel
        GameObject panel = CreateImage(root.transform, "Panel", "ui_dialog_panel",
            Vector2.zero, new Vector2(PanelWidth, panelHeight));
        panel.GetComponent<Image>().raycastTarget = false;
        Transform panelT = panel.transform;

        // Input slot - measured at 1133x261 offset (-23.5, -17.0) in the mockup.
        GameObject slot = CreateImage(panelT, "NicknameInput", "ui_input_slot",
            new Vector2(-23.5f * Scale, -17f * Scale), new Vector2(1133f * Scale, 261f * Scale));
        InputField input = BuildInputField(slot);

        // Buttons - measured at 441x102, y offset -206.5, sitting either side
        // of the content centre.
        Vector2 buttonSize = new Vector2(441f * Scale, 102f * Scale);
        float buttonY = -206.5f * Scale;
        float buttonSpread = 243.5f * Scale;

        GameObject okayGO = CreateImage(panelT, "OkayButton", "btn_okay",
            new Vector2(ContentCentreX - buttonSpread, buttonY), buttonSize);
        GameObject cancelGO = CreateImage(panelT, "CancelButton", "btn_cancel",
            new Vector2(ContentCentreX + buttonSpread, buttonY), buttonSize);

        root.SetActive(false); // MainMenuController turns this on when Play is pressed

        return new Dialog
        {
            root = root,
            input = input,
            okay = MakeButton(okayGO),
            cancel = MakeButton(cancelGO),
        };
    }

    private static InputField BuildInputField(GameObject slot)
    {
        // The writable white area occupies the middle of the slot art; inset
        // the text to match so it never runs onto the wooden surround or the
        // leaves on the left (measured as fractions of the sprite).
        RectTransform slotRect = slot.GetComponent<RectTransform>();
        float w = slotRect.sizeDelta.x, h = slotRect.sizeDelta.y;
        Vector2 offsetMin = new Vector2(w * 0.152f + 14f, h * 0.187f + 6f);
        Vector2 offsetMax = new Vector2(-(w * 0.072f + 14f), -(h * 0.25f + 6f));

        // Dark text, because this slot's interior is white (unlike the wooden
        // surfaces elsewhere in the menu, which take cream text).
        Text placeholder = CreateFieldText(slot.transform, "Placeholder", "Pangalan mo?",
            new Color(0.45f, 0.38f, 0.30f, 0.8f), FontStyle.Italic, offsetMin, offsetMax);
        Text text = CreateFieldText(slot.transform, "Text", "",
            new Color(0.20f, 0.13f, 0.07f), FontStyle.Bold, offsetMin, offsetMax);

        InputField input = slot.AddComponent<InputField>();
        input.targetGraphic = slot.GetComponent<Image>();
        input.textComponent = text;
        input.placeholder = placeholder;
        input.characterLimit = 16; // keeps names readable in the recent-scores list
        input.lineType = InputField.LineType.SingleLine;
        return input;
    }

    private static Button MakeButton(GameObject go)
    {
        Button button = go.AddComponent<Button>();
        button.targetGraphic = go.GetComponent<Image>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.97f, 0.9f);
        colors.pressedColor = new Color(0.8f, 0.75f, 0.7f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        return button;
    }

    private static Text CreateFieldText(Transform parent, string name, string content,
        Color color, FontStyle style, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Text text = go.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 44;
        text.fontStyle = style;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = color;
        text.supportRichText = false;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        return text;
    }

    private static GameObject CreateImage(Transform parent, string name, string spriteFile,
        Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.sprite = LoadSprite(spriteFile);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
        return go;
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
