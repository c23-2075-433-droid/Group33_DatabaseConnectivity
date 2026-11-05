// Assets/Editor/NicknameDialogBuilder.cs
//
// Builds the "Enter Nickname" modal dialog shown when Adventure is pressed:
// a scrim that fades the menu behind it, the hanging board, and the OKAY /
// CANCEL plates that hang under it on their own ropes.
//
// Art pieces (Assets/Sprites/), all with their labels painted in:
//   ui_dialog_panel - the board: two ropes up to the top of the screen,
//                     "ENTER NICKNAME:" and the white name field painted on
//   btn_okay        - "OKAY", with the two short ropes that hold it
//   btn_cancel      - "CANCEL", likewise
//
// There is no separate input-slot sprite any more: the white field is part of
// the board, so the InputField below is an invisible rect laid over it.
//
// Positions and sizes are page coordinates from the Canva export, scaled by
// 0.96 onto the 1920x1080 canvas, exactly as the menu behind it is laid out.
// Tools/import_nickname_dialog_art.py prints every number in this file - run
// it after a re-export. To adjust by hand, move the RectTransforms in the
// Scene view; no code change needed.
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

    // The board, hanging from the top of the screen. Its ropes are painted in,
    // so the sprite starts at the top edge of the Canva page and the rect sits
    // high enough that they still reach it.
    private static readonly Vector2 BoardPosition = new Vector2(46, 202);
    private static readonly Vector2 BoardSize = new Vector2(1027, 677);

    // The white name field, painted onto the board - these are offsets from
    // the board's own centre, so nudging the board takes the field with it.
    private static readonly Vector2 FieldPosition = new Vector2(8, -136);
    private static readonly Vector2 FieldSize = new Vector2(792, 95);

    // "Kylo is taken. Try Kylo01." Goes on the maroon plank below the field,
    // which is clear from row 585 to row 645 of the board art - below that is
    // the board's dark rim.
    private static readonly Vector2 StatusPosition = new Vector2(8, -252);
    private static readonly Vector2 StatusSize = new Vector2(792, 56);

    // The two plates hang below the board on their own ropes, and the export
    // does not draw them quite level or quite the same size - these are each
    // plate's own box, so they land where the artist put them.
    private static readonly Vector2 OkayPosition = new Vector2(-241, -232);
    private static readonly Vector2 OkaySize = new Vector2(492, 190);
    private static readonly Vector2 CancelPosition = new Vector2(314, -227);
    private static readonly Vector2 CancelSize = new Vector2(487, 180);

    /// <summary>
    /// The full-screen catcher behind the dialog. It draws nothing: the menu
    /// is pushed back by going out of focus instead (MenuBlurBackdrop), not by
    /// a wash of colour over it. The Image is still here because something has
    /// to swallow clicks aimed at the menu underneath, and alpha plays no part
    /// in UI raycasting. Give it an alpha if a tint is ever wanted on top of
    /// the blur.
    /// </summary>
    private static readonly Color Scrim = new Color(1f, 1f, 1f, 0f);

    /// <summary>
    /// Result of building the dialog, so callers can wire MainMenuController.
    /// </summary>
    public class Dialog
    {
        public GameObject root;
        public InputField input;
        public Button okay;
        public Button cancel;
        public Text status;
    }

    /// <summary>
    /// Creates the dialog under the given canvas, replacing any previous copy.
    /// Starts inactive - MainMenuController shows it when Adventure is pressed.
    /// </summary>
    public static Dialog Build(Transform canvas)
    {
        foreach (GameObject stale in FindAllInScene(RootName)) Object.DestroyImmediate(stale);

        // Full-screen root: carries the scrim, and catches clicks so the menu
        // behind can't be used while the dialog is open.
        GameObject root = new GameObject(RootName, typeof(RectTransform));
        root.transform.SetParent(canvas, false);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        root.AddComponent<Image>().color = Scrim;

        // The board. Its ropes are painted in and run off the top of the
        // sprite, so they meet the top of the screen at the reference
        // resolution.
        GameObject panel = CreateImage(root.transform, "Panel", "ui_dialog_panel",
            BoardPosition, BoardSize);
        panel.GetComponent<Image>().raycastTarget = false;
        Transform panelT = panel.transform;

        // The name field is painted on the board, so this is an invisible rect
        // laid exactly over the white pill.
        GameObject slot = CreateTransparentRect(panelT, "NicknameInput", FieldPosition, FieldSize);
        InputField input = BuildInputField(slot);

        // The plates hang off the board, so they are its siblings rather than
        // its children: a plate is its own sign, not part of the board.
        GameObject okayGO = CreateImage(root.transform, "OkayButton", "btn_okay",
            OkayPosition, OkaySize);
        GameObject cancelGO = CreateImage(root.transform, "CancelButton", "btn_cancel",
            CancelPosition, CancelSize);

        // "Kylo is taken. Try Kylo01." Goes on the board's lower plank, under
        // the name field - the plates are off the board now, so the space
        // below the field is clear wood with nothing painted on it.
        GameObject statusGO = CreateStatusText(panelT, StatusPosition, StatusSize);

        root.SetActive(false); // MainMenuController turns this on when Adventure is pressed

        return new Dialog
        {
            root = root,
            input = input,
            okay = MakeButton(okayGO),
            cancel = MakeButton(cancelGO),
            status = statusGO.GetComponent<Text>(),
        };
    }

    /// <summary>
    /// The line that explains a rejected nickname. Starts switched off, so an
    /// untouched dialog shows nothing; NicknameAvailability turns it on when
    /// it has something to say.
    /// </summary>
    private static GameObject CreateStatusText(Transform parent, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject("StatusText", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Text text = go.AddComponent<Text>();
        text.font = UIFont.Get();
        text.fontSize = 30;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.supportRichText = false;
        text.raycastTarget = false;
        // Warm cream, like the other text on the menu's wooden surfaces, but
        // pushed towards amber so it reads as a notice rather than a label.
        text.color = new Color(1f, 0.90f, 0.72f);
        text.text = "";

        go.SetActive(false);
        return go;
    }

    private static InputField BuildInputField(GameObject slot)
    {
        // The rect already matches the white pill, so the text only needs
        // enough inset to keep it off the pill's rounded ends.
        Vector2 offsetMin = new Vector2(36f, 6f);
        Vector2 offsetMax = new Vector2(-36f, -6f);

        // Dark text, because the pill is white (unlike the wooden surfaces
        // elsewhere in the menu, which take cream text).
        Text placeholder = CreateFieldText(slot.transform, "Placeholder", "Your name?",
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
        text.font = UIFont.Get();
        text.fontSize = 44;
        text.fontStyle = style;
        // Centred: the pill is far wider than any 16-character name, and a
        // name tucked against the left end looks dropped rather than typed.
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.supportRichText = false;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        return text;
    }

    /// <summary>
    /// An Image that draws nothing but still takes clicks - what an InputField
    /// needs as its targetGraphic when the field it sits in is painted on the
    /// art behind it. Alpha plays no part in UI raycasting, so a fully
    /// transparent graphic is still pressable.
    /// </summary>
    private static GameObject CreateTransparentRect(Transform parent, string name,
        Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
        return go;
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
