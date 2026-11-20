using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Settles whether the child may have the nickname they typed, and when they
/// may not, fills the box with a free one instead.
///
/// Sits between MainMenuController and SupabaseClient: the menu asks
/// Check(name, startGame) and only gets its callback once the name is settled.
/// Leave MainMenuController.nicknameAvailability empty and the menu behaves
/// exactly as it did before, which is what older scenes do.
///
/// It asks two questions, in this order, because they fail in different ways:
///
///   1. Is the name already on this device? PlayerProfileStore knows, no
///      network involved, and this is the case the old check could never
///      catch - two children taking turns on one tablet are the likeliest
///      pair to want the same name, and the first child's name existed
///      nowhere outside the save file until they finished a scene.
///
///   2. Can this device claim the name in the database? Not "has anyone saved
///      a score under it", which is a question about the past: a lookup's
///      answer is stale the moment it is sent, so two tablets could both be
///      told a name was free and both be right. The claim asks Postgres to
///      decide, and the unique index means one of them gets it. See
///      supabase/migrations/20261120_nickname_claims.sql.
///
/// Two rules worth knowing before changing this:
///
///   A failed check still lets the child through. If Supabase is unconfigured,
///   offline or slow, the worst case is two children sharing a name - the
///   alternative is a child who cannot start the game at all, on a menu with
///   no way to explain why. A claim that comes back 409 is not a failure: it
///   is the check working, and the name really is spoken for.
///
///   A claim belongs to the device, not to the profile. The name is settled
///   before the profile that will carry it exists, so there is no profile id
///   to file it under; and renaming away from a name and back again must not
///   be refused by the row this device put there itself.
/// </summary>
public class NicknameAvailability : MonoBehaviour
{
    [Tooltip("Claims the name and reads existing ones. Leave empty and only the " +
              "names already on this device are checked.")]
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

    // A name this component has already settled. Pressing OKAY on it again -
    // which is exactly what a child does after we fill in "Kylo01" - goes
    // straight through instead of filing a second claim for a name this
    // device already holds.
    private string clearedName;

    // The name the dialog was opened with. Accepted unchanged, because a
    // rename to the name you already have is not a collision, and the profile
    // being renamed is in the list of names this device is using.
    private string editingName;

    /// <summary>
    /// Settles <paramref name="nickname"/>, then calls <paramref name="onAccepted"/>
    /// if the game should start. Not called at all when the name was taken -
    /// the box now holds a suggestion and the child presses OKAY again.
    /// </summary>
    public void Check(string nickname, Action onAccepted)
    {
        // Cleaned rather than trimmed, so what is checked is the same string
        // the save file and the leaderboard will end up holding. Checking
        // "Kylo  Jr" and then storing "Kylo Jr" would claim one name and use
        // another.
        string wanted = NicknameRules.Clean(nickname);

        // Swallow anything that arrives while a check is in flight. Disabling
        // the OKAY button is not enough on its own: the input field also
        // confirms on Enter, which does not go through the button at all, and
        // accepting here would start the game on a name we have not settled.
        if (checking) return;

        // Blank is allowed: NicknameManager falls back to "Bata".
        if (wanted.Length == 0)
        {
            onAccepted?.Invoke();
            return;
        }

        if (Same(wanted, clearedName) || Same(wanted, editingName))
        {
            SetStatus("");
            onAccepted?.Invoke();
            return;
        }

        // ---- 1. this device ----
        List<string> localNames = PlayerProfileStore.NicknamesInUse();
        if (NicknameSuggester.IsTaken(wanted, localNames))
        {
            Offer(wanted, localNames);
            return;
        }

        if (supabaseClient == null)
        {
            clearedName = wanted;
            SetStatus("");
            onAccepted?.Invoke();
            return;
        }

        // ---- 2. the database ----
        checking = true;
        SetStatus("Checking...");
        if (okayButton != null) okayButton.interactable = false;

        // Only names built on the same stem can collide with it or with any
        // suggestion made from it, so there is no reason to read the rest.
        string stem = NicknameSuggester.StemOf(wanted);
        if (string.IsNullOrEmpty(stem)) stem = wanted;

        supabaseClient.ClaimNickname(wanted, PlayerProfileStore.DeviceId, (result, error) =>
        {
            if (result == NicknameClaimResult.Claimed)
            {
                Finish();
                clearedName = wanted;
                SetStatus("");
                onAccepted?.Invoke();
                return;
            }

            if (result == NicknameClaimResult.Taken)
            {
                // Somebody holds it. Which somebody decides what happens next,
                // so the claims are read back rather than guessed at.
                supabaseClient.FetchClaimsLike(stem, lookupLimit, (claims, claimError) =>
                {
                    Finish();

                    if (claims == null)
                    {
                        Allow(wanted, "the claim was refused but could not be read back",
                              claimError, onAccepted);
                        return;
                    }

                    if (OwnedHere(wanted, claims))
                    {
                        clearedName = wanted;
                        SetStatus("");
                        onAccepted?.Invoke();
                        return;
                    }

                    Offer(wanted, Union(localNames, NamesOf(claims)));
                });
                return;
            }

            // The claim itself did not work - most often a project where the
            // claims migration has not been run yet. Fall back to the lookup
            // this check used before the claims table existed, so a half-set-up
            // project is no worse off than it was: it still catches a name
            // somebody has finished a level under.
            supabaseClient.FetchNamesLike(stem, lookupLimit, (names, lookupError) =>
            {
                Finish();

                if (names == null)
                {
                    Allow(wanted, "neither the claim nor the lookup worked", error, onAccepted);
                    return;
                }

                if (!NicknameSuggester.IsTaken(wanted, names))
                {
                    // Not claimed, because claiming is what did not work. The
                    // name is as free as the old check could tell.
                    clearedName = wanted;
                    SetStatus("");
                    onAccepted?.Invoke();
                    return;
                }

                Offer(wanted, Union(localNames, names));
            });
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
        //
        // The suggestion is deliberately NOT remembered as cleared. It is a
        // name nobody has claimed, which is not the same as a name this device
        // holds, and pressing OKAY on it has to go and claim it like any other.
        if (input != null)
        {
            input.text = suggestions[0];
            input.caretPosition = suggestions[0].Length;
            input.Select();
            input.ActivateInputField();
        }
    }

    /// <summary>
    /// Lets a name through because the check could not be completed, and says
    /// so in the log. The child sees nothing: there is nothing they could do
    /// about it, and the name they asked for is the name they get.
    /// </summary>
    private void Allow(string wanted, string why, string detail, Action onAccepted)
    {
        Debug.LogWarning("NicknameAvailability: letting \"" + wanted + "\" through - " +
                          why + ". " + detail);
        SetStatus("");
        clearedName = wanted;
        onAccepted?.Invoke();
    }

    private void Finish()
    {
        checking = false;
        if (okayButton != null) okayButton.interactable = true;
    }

    /// <summary>True when the claim on this exact name was filed by this device.</summary>
    private static bool OwnedHere(string wanted, List<NicknameClaim> claims)
    {
        string deviceId = PlayerProfileStore.DeviceId;
        foreach (NicknameClaim claim in claims)
        {
            if (claim == null || claim.nickname == null) continue;
            if (!Same(claim.nickname, wanted)) continue;
            return string.Equals(claim.device_id, deviceId, StringComparison.OrdinalIgnoreCase);
        }
        // The name is not in the rows that came back, so whoever refused the
        // claim is not visible from here. Treat it as someone else's.
        return false;
    }

    private static List<string> NamesOf(List<NicknameClaim> claims)
    {
        List<string> names = new List<string>();
        foreach (NicknameClaim claim in claims)
        {
            if (claim != null && !string.IsNullOrEmpty(claim.nickname)) names.Add(claim.nickname);
        }
        return names;
    }

    private static List<string> Union(List<string> first, List<string> second)
    {
        List<string> all = new List<string>(first);
        if (second != null) all.AddRange(second);
        return all;
    }

    private static bool Same(string a, string b) =>
        !string.IsNullOrEmpty(a) && string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private void SetStatus(string message)
    {
        if (statusText == null) return;
        statusText.text = message;
        statusText.gameObject.SetActive(!string.IsNullOrEmpty(message));
    }

    /// <summary>
    /// Readies the component for a freshly opened dialog: clears the last
    /// message, and takes the name the dialog is starting from.
    ///
    /// That name is passed in because this component cannot work it out. The
    /// board renames whichever profile the child chose, which is not
    /// necessarily the playing one, and a first-run ask starts from nothing at
    /// all - only the caller knows which it is.
    /// </summary>
    public void ResetStatus(string nameBeingEdited = "")
    {
        clearedName = null;
        editingName = NicknameRules.Clean(nameBeingEdited);
        SetStatus("");
    }
}
