using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Checks the nickname against the names already in the database when OKAY is
/// pressed, and when it is taken, fills the box with a free one instead.
///
/// Sits between MainMenuController and SupabaseClient: the menu asks
/// Check(name, startGame) and only gets its callback once the name is settled.
/// Leave MainMenuController.nicknameAvailability empty and the menu behaves
/// exactly as it did before, which is what older scenes do.
///
/// Two rules worth knowing before changing this:
///
///   A failed check lets the child through. If Supabase is unconfigured,
///   offline or slow, the worst case is two children sharing a name - the
///   alternative is a child who cannot start the game at all, on a menu with
///   no way to explain why.
///
///   "Taken" means some saved score row carries that name. A name only
///   becomes taken once that player finishes a level, so two children picking
///   "Kylo" minutes apart will both be allowed. Closing that gap needs a
///   players table of its own, which is a schema change rather than a fix
///   here.
/// </summary>
public class NicknameAvailability : MonoBehaviour
{
    [Tooltip("Reads the existing names. Leave empty and every name is accepted.")]
    public SupabaseClient supabaseClient;

    [Tooltip("The nickname box. Rewritten with the first free suggestion when the typed name is taken.")]
    public InputField input;

    [Tooltip("Optional line under the buttons, for '\"Kylo\" is taken. Try Kylo01.'")]
    public Text statusText;

    [Tooltip("Disabled while the check is in flight, so OKAY cannot be pressed twice.")]
    public Button okayButton;

    [Tooltip("How many alternatives to name in the message.")]
    [Range(1, 5)]
    public int suggestionsShown = 3;

    [Tooltip("Names fetched per check. Only names starting with the typed one are asked for, so this is generous.")]
    public int lookupLimit = 200;

    private bool checking;

    // A name this component has already cleared. Pressing OKAY on it again -
    // which is exactly what a child does after we fill in "Kylo01" - goes
    // straight through instead of spending another round trip confirming what
    // we worked out from the list we just fetched.
    private string clearedName;

    /// <summary>
    /// Settles <paramref name="nickname"/>, then calls <paramref name="onAccepted"/>
    /// if the game should start. Not called at all when the name was taken -
    /// the box now holds a suggestion and the child presses OKAY again.
    /// </summary>
    public void Check(string nickname, Action onAccepted)
    {
        string wanted = nickname == null ? "" : nickname.Trim();

        // Swallow anything that arrives while a check is in flight. Disabling
        // the OKAY button is not enough on its own: the input field also
        // confirms on Enter, which does not go through the button at all, and
        // accepting here would start the game on a name we have not settled.
        if (checking) return;

        // Blank is allowed: NicknameManager falls back to "Bata".
        if (wanted.Length == 0 || supabaseClient == null)
        {
            onAccepted?.Invoke();
            return;
        }

        if (string.Equals(wanted, clearedName, StringComparison.OrdinalIgnoreCase))
        {
            onAccepted?.Invoke();
            return;
        }

        checking = true;
        SetStatus("Checking...");
        if (okayButton != null) okayButton.interactable = false;

        // Only names built on the same stem can collide with it or with any
        // suggestion made from it, so there is no reason to read the rest.
        string stem = NicknameSuggester.StemOf(wanted);
        if (string.IsNullOrEmpty(stem)) stem = wanted;

        supabaseClient.FetchNamesLike(stem, lookupLimit, (names, error) =>
        {
            checking = false;
            if (okayButton != null) okayButton.interactable = true;

            if (error != null)
            {
                Debug.LogWarning("NicknameAvailability: could not check \"" + wanted +
                                  "\", letting it through. " + error);
                SetStatus("");
                clearedName = wanted;
                onAccepted?.Invoke();
                return;
            }

            if (!NicknameSuggester.IsTaken(wanted, names))
            {
                SetStatus("");
                clearedName = wanted;
                onAccepted?.Invoke();
                return;
            }

            Offer(wanted, names);
        });
    }

    /// <summary>Puts the first free name in the box and explains why.</summary>
    private void Offer(string wanted, List<string> names)
    {
        List<string> suggestions = NicknameSuggester.Suggest(wanted, names, suggestionsShown);
        SetStatus(NicknameSuggester.TakenMessage(wanted, suggestions));

        if (suggestions.Count == 0) return;

        // Filling the box matters more than the message does: typing is hard
        // at this age, so the child should be able to accept by pressing the
        // same button again rather than editing text.
        if (input != null)
        {
            input.text = suggestions[0];
            input.caretPosition = suggestions[0].Length;
            input.Select();
            input.ActivateInputField();
        }
        clearedName = suggestions[0];
    }

    private void SetStatus(string message)
    {
        if (statusText == null) return;
        statusText.text = message;
        statusText.gameObject.SetActive(!string.IsNullOrEmpty(message));
    }

    /// <summary>Clears the message, so a reopened dialog does not show the last one.</summary>
    public void ResetStatus()
    {
        clearedName = null;
        SetStatus("");
    }
}
