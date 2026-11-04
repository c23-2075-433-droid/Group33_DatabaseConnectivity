using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Gives every button in the game a click. One object, installed once, that
/// watches where the player presses - no button anywhere knows this exists.
///
/// The obvious way to do this is onClick.AddListener on each button, and it is
/// the wrong way here, for three reasons that are all about this project
/// specifically:
///
///   1. The buttons are built by code, in a dozen places. GameplayHUDLayout
///      makes the HUD's, VoiceUIBuilder and the Build*Scene editor scripts put
///      the rest into GENERATED scenes, and LevelSelectController instantiates
///      one per level node. A listener added by hand is a listener somebody has
///      to remember to add to the next button, forever.
///   2. SceneResultPanel and WordsLearnedPanel both call
///      onClick.RemoveAllListeners() before wiring their buttons, because what
///      Continue continues to changes per run. A click sound added as a
///      listener would be thrown away by that call - silently, and only on the
///      panels the child sees most.
///   3. Pressing is when the sound is wanted. onClick fires on release, and a
///      six-year-old holding a finger down on LARO should hear something while
///      it is down, not after they let go.
///
/// So: on a press, ask the EventSystem what is under the pointer, the same
/// question the input module is about to ask, and play the click if the answer
/// is a button that would accept it. A press on a locked level node, on a
/// faded-out screen, or on the dark behind a panel hits no interactable button
/// and stays quiet, which is right - those presses do nothing.
///
/// The one button deliberately left silent is the microphone. See IsMicrophone.
/// </summary>
public class UIClickSound : MonoBehaviour
{
    public static UIClickSound Instance { get; private set; }

    // Reused rather than allocated per press: this runs on a child's tablet.
    private readonly List<RaycastResult> hits = new List<RaycastResult>();

    // The scene's voice UI, once something has asked for it. Not cached when
    // null: in gameplay scenes the HUD is built at run time, so the first few
    // presses can happen before there is one to find.
    private VoiceInteractionUI voiceUI;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // The old scene's voice UI is destroyed; holding the reference would
        // compare this scene's presses against a dead button.
        voiceUI = null;
    }

    void Update()
    {
        Vector2 at;
        if (PointerWentDown(out at)) PressAt(at);
    }

    /// <summary>
    /// Plays the click if an interactable button is under this screen point,
    /// and says whether it did.
    ///
    /// Split out from Update so a press can be put through the real raycast
    /// without faking mouse input - which is how this was checked in the
    /// editor, and how a test would check it.
    /// </summary>
    public bool PressAt(Vector2 at)
    {
        EventSystem events = EventSystem.current;
        if (events == null) return false;

        // A fresh PointerEventData per press. It is one small allocation on a
        // press, and it cannot go stale the way a kept one does when the
        // EventSystem is replaced on a scene load.
        PointerEventData pointer = new PointerEventData(events) { position = at };

        hits.Clear();
        events.RaycastAll(pointer, hits);
        if (hits.Count == 0) return false;

        // Only the top hit. Something drawn over a button - the fader, a panel
        // backdrop - swallows the press, and must swallow the sound with it.
        // GetComponentInParent because the press usually lands on the button's
        // label or icon rather than on the button itself.
        Button button = hits[0].gameObject.GetComponentInParent<Button>();
        if (button == null || !button.IsInteractable()) return false;
        if (IsMicrophone(button)) return false;

        SoundEffects.Click();
        return true;
    }

    /// <summary>
    /// Where the player just started pressing, this frame. Touches are read
    /// first and the mouse only when there are none: on Android a touch is
    /// also reported as mouse button 0, and asking both would play two clicks
    /// for one finger. Every touch is checked rather than GetTouch(0) so a
    /// second finger arriving on a button still counts.
    /// </summary>
    private static bool PointerWentDown(out Vector2 position)
    {
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began)
                {
                    position = touch.position;
                    return true;
                }
            }

            position = default;
            return false;
        }

        if (Input.GetMouseButtonDown(0))
        {
            position = Input.mousePosition;
            return true;
        }

        position = default;
        return false;
    }

    /// <summary>
    /// The microphone button, which gets no click.
    ///
    /// Pressing it calls VoiceCommand.RequestPermissionAndListen() - the
    /// recognizer opens in the same frame, and the microphone hears the
    /// device's own speaker. A click there would be the first thing recorded of
    /// the child's answer, every time, at the one moment in the game where
    /// what the microphone picks up is being judged. The mic already answers a
    /// press by pulsing, so it is not left with no feedback.
    ///
    /// Found by asking the scene's VoiceInteractionUI which button is its mic,
    /// rather than by name: the mic is built into generated scenes by
    /// VoiceUIBuilder, and anything matched on a name there is one rebuild away
    /// from being wrong.
    /// </summary>
    private bool IsMicrophone(Button button)
    {
        // FindAnyObjectByType, not FindFirstObjectByType: there is one voice UI
        // per scene, nothing here cares which is "first", and Unity 6 has
        // deprecated the ordered one.
        if (voiceUI == null) voiceUI = FindAnyObjectByType<VoiceInteractionUI>();
        return voiceUI != null && voiceUI.micButton == button;
    }
}

/// <summary>
/// Puts the click in the game once, the same way SoundEffectsBootstrap puts
/// the effects player in.
/// </summary>
public static class UIClickSoundBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (UIClickSound.Instance != null) return;

        GameObject go = new GameObject("UIClickSound");
        go.AddComponent<UIClickSound>();
    }
}
