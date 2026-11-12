// Negative tests for SALINLAHI's input-validation rules (Activity 9, Part VI).
//
// These compile the GAME'S OWN Assets/Scripts/NicknameRules.cs - not a copy of
// it - and assert on what it does with input the game should never produce:
// names read from a hand-edited save file, control characters, lengths past
// the input field's limit, and player ids that are not UUIDs.
//
// It runs without Unity, which is the point. A validation rule that can only
// be exercised by opening the editor and playing the game is a rule that stops
// being checked. Run it with Tools/test_security_rules.sh.

using System;

internal static class SecurityRuleTests
{
    private static int failed;
    private static int passed;

    private static void Check(string name, bool condition, string detail)
    {
        if (condition)
        {
            passed++;
            Console.WriteLine("  PASS  " + name);
        }
        else
        {
            failed++;
            Console.WriteLine("  FAIL  " + name + "  -> " + detail);
        }
    }

    private static string Show(string s)
    {
        if (s == null) return "(null)";
        string shown = s.Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t")
                         .Replace("\u0000", "\\0").Replace("\u001b", "\\e");
        return "\"" + (shown.Length > 40 ? shown.Substring(0, 40) + "..." : shown) +
               "\" (" + s.Length + " chars)";
    }

    private static int Main()
    {
        Console.WriteLine("SALINLAHI security rules - negative tests");
        Console.WriteLine();

        // ---- nickname: length -------------------------------------------
        Console.WriteLine("Nickname length (cap is " + NicknameRules.MaxLength + ")");

        string longName = new string('A', 500);
        string cleanedLong = NicknameRules.Clean(longName);
        Check("a 500-character nickname is capped, not stored",
              cleanedLong.Length == NicknameRules.MaxLength,
              "got " + Show(cleanedLong));

        Check("a nickname inside the cap is left alone",
              NicknameRules.Clean("Kyla") == "Kyla",
              "got " + Show(NicknameRules.Clean("Kyla")));

        // ---- nickname: control characters --------------------------------
        Console.WriteLine();
        Console.WriteLine("Control characters");

        string withNewline = "Kyla\nSamantha";
        Check("a newline becomes a space, so two words stay two words",
              NicknameRules.Clean(withNewline) == "Kyla Samantha",
              "got " + Show(NicknameRules.Clean(withNewline)));

        string withNul = "Ky\u0000la";
        Check("a NUL byte does not survive",
              NicknameRules.Clean(withNul).IndexOf('\u0000') < 0,
              "got " + Show(NicknameRules.Clean(withNul)));

        string withEscape = "Kyla\u001b[31mRED";
        string cleanedEscape = NicknameRules.Clean(withEscape);
        Check("an ANSI escape sequence loses its escape character",
              cleanedEscape.IndexOf('\u001b') < 0,
              "got " + Show(cleanedEscape));

        string onlyControl = "\n\r\t\u0000";
        Check("a nickname of nothing but control characters comes back empty",
              NicknameRules.Clean(onlyControl) == "",
              "got " + Show(NicknameRules.Clean(onlyControl)));

        string padded = "   Kyla   ";
        Check("surrounding whitespace is trimmed",
              NicknameRules.Clean(padded) == "Kyla",
              "got " + Show(NicknameRules.Clean(padded)));

        string runs = "Kyla     Sam";
        Check("a run of spaces collapses to one",
              NicknameRules.Clean(runs) == "Kyla Sam",
              "got " + Show(NicknameRules.Clean(runs)));

        Check("null is handled rather than thrown on",
              NicknameRules.Clean(null) == "",
              "got " + Show(NicknameRules.Clean(null)));

        // A long name whose cut lands on a space must not keep the space.
        string cutOnSpace = "Kylaaaaaaaaaaaaa Samantha";
        string cutResult = NicknameRules.Clean(cutOnSpace);
        Check("a name cut at the limit does not end in a space",
              cutResult.Length <= NicknameRules.MaxLength &&
              cutResult == cutResult.Trim(),
              "got " + Show(cutResult));

        // ---- player id ---------------------------------------------------
        Console.WriteLine();
        Console.WriteLine("Player id (must be the UUID the game made)");

        Check("the game's own id shape is accepted",
              NicknameRules.IsValidPlayerId("3f2504e0-4f89-41d3-9a0c-0305e82c3301"),
              "a real UUID was rejected");

        Check("upper case hex is still a UUID",
              NicknameRules.IsValidPlayerId("3F2504E0-4F89-41D3-9A0C-0305E82C3301"),
              "an upper-case UUID was rejected");

        Check("a hand-typed id is refused",
              !NicknameRules.IsValidPlayerId("not-a-uuid"),
              "\"not-a-uuid\" was accepted");

        // This is the one that mattered: the rank request used to be built by
        // string concatenation, so an id carrying a quote wrote its own JSON.
        Check("an id carrying JSON punctuation is refused",
              !NicknameRules.IsValidPlayerId("x\",\"p_player_id\":\"y"),
              "an id with a quote in it was accepted");

        Check("an id with SQL in it is refused",
              !NicknameRules.IsValidPlayerId("' or 1=1 --"),
              "an id with SQL in it was accepted");

        Check("a UUID without its hyphens is refused",
              !NicknameRules.IsValidPlayerId("3f2504e04f8941d39a0c0305e82c3301"),
              "the N format was accepted where D is required");

        Check("a braced UUID is refused",
              !NicknameRules.IsValidPlayerId("{3f2504e0-4f89-41d3-9a0c-0305e82c3301}"),
              "the B format was accepted where D is required");

        Check("an empty id is refused",
              !NicknameRules.IsValidPlayerId(""),
              "the empty string was accepted");

        Check("a null id is refused rather than thrown on",
              !NicknameRules.IsValidPlayerId(null),
              "null was accepted");

        Console.WriteLine();
        Console.WriteLine(passed + " passed, " + failed + " failed.");
        return failed == 0 ? 0 : 1;
    }
}
