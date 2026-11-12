// Assets/Editor/ProfileUIBuilder.cs
//
// Builds everything that hangs off the nickname holder on the main menu:
//
//   the holder itself, with the playing child's name on it
//   the two signs that swing down from it - CHANGE PROFILE, LEADERBOARDS
//   the WHO ARE YOU? board, where profiles are chosen, renamed and made
//   the LEADERBOARDS board
//
// Art (Assets/Sprites/), cut by Tools/import_profile_art.py:
//   ui_name_holder   the plaque. Empty: the name is live text laid over it
//   ui_profile_gem   the clasp on its bottom edge
//   ui_profile_menu  the ropes and both signs, in one piece
//   ui_profile_board, ui_leaderboard_board
//   btn_rename, btn_delete, btn_okay_plate, btn_cancel_plate
//
// Both boards are drawn complete - title, slots and plates all painted in -
// so nothing here draws a panel or a button of its own. The rows become
// invisible rects with a Text laid over them, and each plate gets its own copy
// of the same art laid exactly on top of the painted one, which is what lets a
// press tint the plate instead of the whole board.
//
// Every number below is a page coordinate measured off the Canva export,
// scaled by 0.96 onto the 1920x1080 canvas - the same convention as
// BuildMainMenuScene and NicknameDialogBuilder. To adjust by hand, move the
// RectTransforms in the Scene view; no code change needed.

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ProfileUIBuilder
{
    private const string SpriteFolder = "Assets/Sprites/";
    private const float K = 0.96f;          // page pixels -> canvas units

    public const string HolderName = "NicknameHolder";
    public const string DropdownName = "ProfileSigns";
    public const string CatcherName = "ProfileSignsCatcher";
    public const string PickerName = "ProfileBoard";
    public const string LeaderboardName = "LeaderboardBoard";

    /// <summary>Sprites this screen adds to the menu, so BuildMainMenuScene can
    /// pair the ones with soft twins for the out-of-focus backdrop.</summary>
    public static readonly string[] Sprites = { "ui_name_holder" };

    // ---- the holder, top right of the menu ----
    private static readonly Vector2 HolderPosition = new Vector2(707.0f, 432.0f);
    private static readonly Vector2 HolderSize = new Vector2(420.5f, 137.3f);
    // The name sits in the plaque's gold inner, short of the painted pencil.
    private static readonly Vector2 NamePosition = new Vector2(-20.6f, 2.4f);
    private static readonly Vector2 NameSize = new Vector2(310.1f, 88.3f);

    // ---- what hangs under it ----
    private static readonly Vector2 SignsOffset = new Vector2(-0.5f, -195.4f);
    private static readonly Vector2 SignsSize = new Vector2(267.8f, 257.3f);
    private static readonly Vector2 GemOffset = new Vector2(0.96f, -62.5f);
    private static readonly Vector2 GemSize = new Vector2(66.1f, 58.0f);
    // The two signs, as offsets from the middle of the piece they are drawn on.
    private static readonly Vector2 ChangeProfilePosition = new Vector2(-1.0f, 47.5f);
    private static readonly Vector2 LeaderboardsPosition = new Vector2(0f, -77.8f);
    private static readonly Vector2 SignSize = new Vector2(259.2f, 94.5f);

    // ---- the boards ----
    private static readonly Vector2 BoardSize = new Vector2(617.3f, 784.3f);
    private static readonly Vector2 SlotSize = new Vector2(472.3f, 77.8f);
    private const float SlotX = 4.6f;
    // Four rows on the profile board: three profiles, then "(CREATE NEW USER)".
    private static readonly float[] ProfileSlotY = { 189.1f, 97.0f, 4.8f, -87.4f };
    // Six on the leaderboard.
    private static readonly float[] LeaderboardSlotY =
        { 189.1f, 97.0f, 4.8f, -87.4f, -177.6f, -267.8f };

    private static readonly Vector2 PlateSize = new Vector2(218.9f, 84.5f);
    private static readonly Vector2 RenamePosition = new Vector2(-109.9f, -188.6f);
    private static readonly Vector2 DeletePosition = new Vector2(118.6f, -188.6f);
    private static readonly Vector2 OkayPosition = new Vector2(-108.0f, -272.6f);
    private static readonly Vector2 CancelPosition = new Vector2(120.0f, -272.2f);

    // The leaderboard board has no plates painted on it, so its way out is a
    // loose OKAY plate hung under the board rather than inside it.
    private static readonly Vector2 LeaderboardOkayPosition = new Vector2(0f, -450f);

    /// <summary>
    /// A catcher that draws nothing. Alpha plays no part in UI raycasting, so
    /// a fully transparent graphic still swallows the press - which is all
    /// this is for. The menu behind a board is pushed back by going out of
    /// focus (MenuBlurBackdrop), not by a wash of colour over it.
    /// </summary>
    private static readonly Color Invisible = new Color(1f, 1f, 1f, 0f);

    // Cream on wood, as everywhere else on this menu. The display face brings
    // its own colour and stroke (VocabularyFont.Chalk), so this is only for
    // the text that stays in the UI font.
    private static readonly Color Cream = new Color(1f, 0.92f, 0.78f);

    public class Built
    {
        public PlayerProfileMenu menu;
        public ProfilePickerPanel picker;
        public LeaderboardPanel leaderboard;
        public GameObject holder;
    }

    /// <summary>
    /// Creates the holder, the signs and both boards under the given canvas,
    /// replacing any previous copies. Everything but the holder starts
    /// inactive.
    /// </summary>
    public static Built Build(Transform canvas, MainMenuController controller,
                              MenuBlurBackdrop blur, LeaderboardClient leaderboardClient)
    {
        foreach (string stale in new[] { HolderName, DropdownName, CatcherName,
                                         PickerName, LeaderboardName })
        {
            foreach (GameObject found in FindAllInScene(stale)) Object.DestroyImmediate(found);
        }

        GameObject holder = CreateImage(canvas, HolderName, "ui_name_holder",
                                        HolderPosition, HolderSize);
        Button holderButton = MakeButton(holder);
        // 59 is the size at which the name matches the "YOUR NAME" painted
        // into the mockup: those capitals are 43px on the 2000px page, 41px at
        // the size this is built, and the display face's capitals are 0.695 of
        // its point size. It is a ceiling, not a setting - best fit takes a
        // sixteen-character name back down on its own.
        Text nameLabel = CreateLabel(holder.transform, "Name", NamePosition, NameSize,
                                     59, TextAnchor.MiddleCenter, Color.white, true);

        // Above the holder so a second tap on it closes the signs, and above
        // the menu so nothing else can be pressed while they are down. Turned
        // on and off with them - left on, it would eat the whole menu.
        GameObject catcher = CreateTransparentRect(canvas, CatcherName, Vector2.zero, Vector2.zero);
        RectTransform catcherRect = catcher.GetComponent<RectTransform>();
        catcherRect.anchorMin = Vector2.zero;
        catcherRect.anchorMax = Vector2.one;
        catcherRect.offsetMin = Vector2.zero;
        catcherRect.offsetMax = Vector2.zero;
        Button catcherButton = catcher.AddComponent<Button>();
        catcher.SetActive(false);

        // The signs and their clasp, as one group so they appear together.
        GameObject dropdown = new GameObject(DropdownName, typeof(RectTransform));
        dropdown.transform.SetParent(canvas, false);
        dropdown.GetComponent<RectTransform>().anchoredPosition = HolderPosition;
        dropdown.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

        GameObject gem = CreateImage(dropdown.transform, "Clasp", "ui_profile_gem",
                                     GemOffset, GemSize);
        gem.GetComponent<Image>().raycastTarget = false;

        GameObject signs = CreateImage(dropdown.transform, "Signs", "ui_profile_menu",
                                       SignsOffset, SignsSize);
        signs.GetComponent<Image>().raycastTarget = false;

        // The words are painted on the signs, so these are invisible rects
        // laid over them - the same trick the nickname field uses.
        Button changeProfile = MakeButton(CreateTransparentRect(signs.transform,
            "ChangeProfileButton", ChangeProfilePosition, SignSize));
        Button leaderboards = MakeButton(CreateTransparentRect(signs.transform,
            "LeaderboardsButton", LeaderboardsPosition, SignSize));

        dropdown.SetActive(false);

        ProfilePickerPanel picker = BuildPicker(canvas, controller, blur);
        LeaderboardPanel board = BuildLeaderboard(canvas, blur, leaderboardClient);

        // All three live on the canvas, beside MainMenuController, and not on
        // the objects they drive. A board starts switched off, and a component
        // on a switched-off object never gets its Start() - so its buttons
        // would be wired to nothing the first time it was opened.
        PlayerProfileMenu menu = canvas.gameObject.AddComponent<PlayerProfileMenu>();
        menu.holderButton = holderButton;
        menu.nicknameLabel = nameLabel;
        menu.dropdown = dropdown;
        menu.changeProfileButton = changeProfile;
        menu.leaderboardsButton = leaderboards;
        menu.outsideCatcher = catcherButton;
        menu.picker = picker;
        menu.leaderboard = board;
        menu.leaderboardClient = leaderboardClient;

        return new Built { menu = menu, picker = picker, leaderboard = board, holder = holder };
    }

    // ---------- WHO ARE YOU? ----------

    private static ProfilePickerPanel BuildPicker(Transform canvas,
        MainMenuController controller, MenuBlurBackdrop blur)
    {
        GameObject root = FullScreen(canvas, PickerName);
        Button scrim = root.AddComponent<Button>();

        GameObject board = CreateImage(root.transform, "Board", "ui_profile_board",
                                       Vector2.zero, BoardSize);
        board.GetComponent<Image>().raycastTarget = false;

        // Three profile rows, then the painted "(CREATE NEW USER)" row.
        Text[] labels = new Text[ProfileSlotY.Length - 1];
        Button[] buttons = new Button[ProfileSlotY.Length - 1];
        for (int i = 0; i < labels.Length; i++)
        {
            GameObject slot = CreateTransparentRect(board.transform, "Slot" + (i + 1),
                new Vector2(SlotX, ProfileSlotY[i]), SlotSize);
            buttons[i] = MakeButton(slot);
            labels[i] = CreateLabel(slot.transform, "Name", Vector2.zero, SlotSize,
                                    40, TextAnchor.MiddleCenter, Cream, true);
        }

        GameObject createSlot = CreateTransparentRect(board.transform, "CreateSlot",
            new Vector2(SlotX, ProfileSlotY[ProfileSlotY.Length - 1]), SlotSize);
        Button create = MakeButton(createSlot);

        // The plates are painted into the board too. Laying the loose art on
        // top, at the size it is drawn there, means a press darkens the plate
        // rather than the whole board - and the copy is the same picture, so
        // nothing about the board looks different.
        Button rename = Plate(board.transform, "RenameButton", "btn_rename", RenamePosition);
        Button remove = Plate(board.transform, "DeleteButton", "btn_delete", DeletePosition);
        Button okay = Plate(board.transform, "OkayButton", "btn_okay_plate", OkayPosition);
        Button cancel = Plate(board.transform, "CancelButton", "btn_cancel_plate", CancelPosition);

        root.SetActive(false);

        ProfilePickerPanel panel = canvas.gameObject.AddComponent<ProfilePickerPanel>();
        panel.root = root;
        panel.scrim = scrim;
        panel.slotLabels = labels;
        panel.slotButtons = buttons;
        panel.createButton = create;
        panel.renameButton = rename;
        panel.deleteButton = remove;
        panel.okayButton = okay;
        panel.cancelButton = cancel;
        panel.menu = controller;
        panel.menuBlur = blur;
        return panel;
    }

    // ---------- LEADERBOARDS ----------

    private static LeaderboardPanel BuildLeaderboard(Transform canvas, MenuBlurBackdrop blur,
                                                     LeaderboardClient client)
    {
        GameObject root = FullScreen(canvas, LeaderboardName);
        Button scrim = root.AddComponent<Button>();

        GameObject board = CreateImage(root.transform, "Board", "ui_leaderboard_board",
                                       Vector2.zero, BoardSize);
        board.GetComponent<Image>().raycastTarget = false;

        Text[] labels = new Text[LeaderboardSlotY.Length];
        for (int i = 0; i < labels.Length; i++)
        {
            // Left-aligned with the rank first, because that is the column a
            // child reads down. The inset keeps it off the slot's rounded end.
            GameObject slot = new GameObject("Row" + (i + 1), typeof(RectTransform));
            slot.transform.SetParent(board.transform, false);
            RectTransform rect = slot.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(SlotX, LeaderboardSlotY[i]);
            rect.sizeDelta = SlotSize;
            // The one piece of text here left in the UI font. It is a column
            // of names and scores rather than a line addressed to the child,
            // and the display face is a heavy one to read six rows of.
            labels[i] = CreateLabel(slot.transform, "Text", new Vector2(18f, 0f),
                                    new Vector2(SlotSize.x - 56f, SlotSize.y),
                                    36, TextAnchor.MiddleLeft, Cream, false);
        }

        // The board has no plate painted on it, so the way out is a loose one
        // hung underneath - a tap outside closes it too, but a child should be
        // able to see the door.
        Button okay = Plate(root.transform, "OkayButton", "btn_okay_plate",
                            LeaderboardOkayPosition);

        root.SetActive(false);

        LeaderboardPanel panel = canvas.gameObject.AddComponent<LeaderboardPanel>();
        panel.root = root;
        panel.scrim = scrim;
        panel.okayButton = okay;
        panel.rowLabels = labels;
        panel.client = client;
        panel.menuBlur = blur;
        return panel;
    }

    // ---------- helpers ----------

    /// <summary>A board's full-screen root: catches every press aimed at the
    /// menu behind it, and draws nothing while doing so.</summary>
    private static GameObject FullScreen(Transform canvas, string name)
    {
        GameObject root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(canvas, false);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        root.AddComponent<Image>().color = Invisible;
        return root;
    }

    private static Button Plate(Transform parent, string name, string sprite, Vector2 position)
    {
        return MakeButton(CreateImage(parent, name, sprite, position, PlateSize));
    }

    private static GameObject CreateImage(Transform parent, string name, string spriteFile,
        Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.sprite = LoadSprite(spriteFile);
        img.preserveAspect = true;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
        return go;
    }

    private static GameObject CreateTransparentRect(Transform parent, string name,
        Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = Invisible;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
        return go;
    }

    /// <summary>
    /// A line of live text over painted art. Best-fit is on because the words
    /// are the one thing here that is not drawn at a known size - a child may
    /// type sixteen characters into a plaque drawn for eight.
    ///
    /// <paramref name="display"/> picks the face, and it is the same split the
    /// level summary makes: a name addressed to the child is set in the
    /// display face the lettering on these boards is drawn in
    /// (VocabularyFont), while a column of names and scores stays in the UI
    /// font, which is calmer to read down. The summary's own records strip is
    /// the precedent - see SceneResultPanelBuilder.
    /// </summary>
    private static Text CreateLabel(Transform parent, string name, Vector2 position,
        Vector2 size, int fontSize, TextAnchor alignment, Color color, bool display)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Text text = go.AddComponent<Text>();
        text.font = UIFont.Get();
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = alignment;
        text.color = color;
        text.supportRichText = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 18;
        text.resizeTextMaxSize = fontSize;
        text.raycastTarget = false;
        text.text = "";

        if (display)
        {
            // The face, the weight, the colour and the stroke in one call, so
            // a name on this plaque letters the same way as a name on the
            // summary board. It overrides the font, style and colour set
            // above; what the caller passes still decides size and alignment,
            // and the panels repaint the colour at runtime to pick a row out.
            VocabularyFont.Apply(text);
        }

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return text;
    }

    /// <summary>A button whose whole look is the art behind it. Darkens slightly
    /// when pressed, like every other plate on this menu.</summary>
    private static Button MakeButton(GameObject go)
    {
        Button button = go.AddComponent<Button>();
        button.targetGraphic = go.GetComponent<Image>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.97f, 0.9f);
        colors.pressedColor = new Color(0.8f, 0.75f, 0.7f);
        colors.disabledColor = new Color(1f, 1f, 1f, 0.45f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        return button;
    }

    private static Sprite LoadSprite(string spriteFile)
    {
        string path = SpriteFolder + spriteFile + ".png";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            Debug.LogWarning("[SALINLAHI] Could not load sprite at '" + path +
                              "'. Run Tools/import_profile_art.py, then check it imported as Sprite (2D and UI).");
        return sprite;
    }

    /// <summary>Finds GameObjects by name in the open scene, including inactive
    /// ones (unlike GameObject.Find).</summary>
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
