// Assets/Editor/WordsLearnedPanelBuilder.cs
//
// Builds the WORDS LEARNED board: the Filipino words a scene taught, each with
// what it means, shown after the summary's CONTINUE.
//
// Art pieces (Assets/Sprites/), cut by Tools/cut_result_ui.py:
//   ui_words_board      the board, with its five row plates, its title and the
//                       line "REVIEW THE FILIPINO WORDS..." painted in
//   btn_result_continue the plank that turns the page or moves on
//   btn_result_close    the cross in the corner
//
// Built as a child of the summary's own canvas rather than a canvas of its
// own. That is not a saving of one GameObject: it means RestyleSceneResultPanels
// recreates this panel every time it rebuilds the summary, without having to
// know it exists. A separate canvas would have to be found, destroyed and
// re-wired by hand in that tool, and would be silently orphaned the first time
// somebody forgot.
//
// Only the words and their meanings are placed. Everything else is in the art.

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class WordsLearnedPanelBuilder
{
    private const string BoardSprite = "ui_words_board";
    private const string ContinueSprite = "btn_result_continue";
    private const string CloseSprite = "btn_result_close";

    // ui_words_board is 794x941, the same shape as the summary board so the
    // two read as one screen in two beats.
    private const float PanelWidth = 740f;
    private const float PanelHeight = 878f;

    // The five plates are painted into the board. Measured off it: centres a
    // little over an eighth of the board apart, starting below the printed
    // instruction line.
    private static readonly float[] RowFracs = { 0.2795f, 0.4049f, 0.5260f, 0.6472f, 0.7704f };
    private const float RowHeightFrac = 0.1084f;

    // The word sits left on its plate and its meaning right, which is how the
    // mockup reads: a column of Filipino with a column of English beside it.
    private const float WordLeft = 0.085f;
    private const float WordWidth = 0.42f;
    private const float MeaningRight = 0.915f;
    private const float MeaningWidth = 0.40f;

    private const float ContinueFrac = 0.885f;
    private const float ContinueWidthFrac = 0.29f;
    private const float CloseFrac = 0.082f;
    private const float CloseX = 0.438f;
    private const float CloseSize = 112f;

    private static readonly Color Cream = new Color(0.99f, 0.96f, 0.89f);

    /// <summary>A fraction of the way down the board, as a Y from its centre.</summary>
    private static float FromTop(float frac)
    {
        return (0.5f - frac) * PanelHeight;
    }

    /// <summary>
    /// Adds the board under an existing canvas and returns the component that
    /// drives it. Starts switched off; the summary opens it.
    /// </summary>
    public static WordsLearnedPanel Build(Transform canvas, SceneObjectiveController controller)
    {
        SpriteImportFixer.Verify(new[] { BoardSprite, ContinueSprite, CloseSprite },
                                 "Words Learned Panel");

        GameObject content = new GameObject("WordsLearned_Content", typeof(RectTransform));
        content.transform.SetParent(canvas, false);
        Stretch(content.GetComponent<RectTransform>());

        // Blocks the summary behind it, which is still in the hierarchy.
        GameObject dim = new GameObject("Dim", typeof(RectTransform));
        dim.transform.SetParent(content.transform, false);
        Image dimImage = dim.AddComponent<Image>();
        dimImage.color = new Color(0f, 0f, 0f, 0.55f);
        dimImage.raycastTarget = true;
        Stretch(dim.GetComponent<RectTransform>());

        GameObject board = new GameObject("Board", typeof(RectTransform));
        board.transform.SetParent(content.transform, false);
        Image boardImage = board.AddComponent<Image>();
        boardImage.sprite = LoadSprite(BoardSprite);
        boardImage.raycastTarget = false;
        RectTransform boardRect = board.GetComponent<RectTransform>();
        boardRect.anchoredPosition = Vector2.zero;
        boardRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

        List<WordsLearnedPanel.Row> rows = new List<WordsLearnedPanel.Row>();
        for (int i = 0; i < RowFracs.Length; i++)
        {
            rows.Add(CreateRow(board.transform, i, RowFracs[i]));
        }

        Button cont = CreateButton(board.transform, "ContinueButton", ContinueSprite,
                                   new Vector2(0f, FromTop(ContinueFrac)), PanelWidth * ContinueWidthFrac);
        Button close = CreateButton(board.transform, "CloseButton", CloseSprite,
                                    new Vector2(PanelWidth * CloseX, FromTop(CloseFrac)),
                                    CloseSize);

        // On the CANVAS, not on content. content is the thing switched off, and
        // a component living there would run its own Awake the moment it was
        // switched back on - see the guard in WordsLearnedPanel.Awake, and the
        // identical one SceneResultPanel has carried since it was written.
        WordsLearnedPanel panel = canvas.gameObject.AddComponent<WordsLearnedPanel>();
        panel.objectiveController = controller;
        panel.panelRoot = content;
        panel.rows = rows.ToArray();
        panel.continueButton = cont;
        panel.closeButton = close;

        // Off in the saved scene, so it never flashes up on the first frame.
        content.SetActive(false);
        return panel;
    }

    private static WordsLearnedPanel.Row CreateRow(Transform parent, int index, float frac)
    {
        GameObject root = new GameObject("Row_" + (index + 1), typeof(RectTransform));
        root.transform.SetParent(parent, false);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(0f, FromTop(frac));
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight * RowHeightFrac);

        float height = PanelHeight * RowHeightFrac;

        Text word = CreateText(root.transform, "Word", "",
            new Vector2((WordLeft + WordWidth * 0.5f - 0.5f) * PanelWidth, 0f),
            new Vector2(PanelWidth * WordWidth, height), 40, TextAnchor.MiddleLeft);

        Text meaning = CreateText(root.transform, "Meaning", "",
            new Vector2((MeaningRight - MeaningWidth * 0.5f - 0.5f) * PanelWidth, 0f),
            new Vector2(PanelWidth * MeaningWidth, height), 40, TextAnchor.MiddleRight);

        // A long word and a long meaning on one plate would otherwise collide.
        foreach (Text text in new[] { word, meaning })
        {
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 20;
            text.resizeTextMaxSize = 40;
        }

        return new WordsLearnedPanel.Row { root = root, word = word, meaning = meaning };
    }

    // ---------- helpers (the shapes SceneResultPanelBuilder uses) ----------

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Text CreateText(Transform parent, string name, string content,
        Vector2 position, Vector2 size, int fontSize, TextAnchor align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Text text = go.AddComponent<Text>();
        text.text = content;
        text.font = UIFont.Get();
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = align;
        text.color = Cream;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;

        // Cream on mid-brown woodgrain is weak on its own - the grain runs
        // straight through the letters. The same outline the summary uses.
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0.16f, 0.09f, 0.04f, 0.95f);
        outline.effectDistance = new Vector2(2.2f, -2.2f);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return text;
    }

    private static Button CreateButton(Transform parent, string name, string spriteFile,
        Vector2 position, float width)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image image = go.AddComponent<Image>();
        image.sprite = LoadSprite(spriteFile);
        image.preserveAspect = true;

        // Square unless the art says otherwise, which is what the close cross
        // is; the planks carry their own ratio.
        float height = width;
        if (image.sprite != null)
            height = width * image.sprite.rect.height / image.sprite.rect.width;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(width, height);

        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.97f, 0.9f);
        colors.pressedColor = new Color(0.8f, 0.75f, 0.7f);
        // Touch screens never clear a selection, so Unity's dimmed default
        // leaves the last thing pressed looking stuck.
        colors.selectedColor = Color.white;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        return button;
    }

    private static Sprite LoadSprite(string spriteFile)
    {
        if (string.IsNullOrEmpty(spriteFile)) return null;
        string path = "Assets/Sprites/" + spriteFile + ".png";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            Debug.LogWarning("[SALINLAHI] Could not load sprite at '" + path + "'.");
        return sprite;
    }
}
