using UnityEngine;

/// <summary>
/// Tracks when H has stepped away (<see cref="ComputerDesktopUI.NotifyRemoteAccessEstablished"/>) vs returned to the feed.
/// Closes an active maze breach and applies suspicion when H catches the player mid-level.
/// </summary>
public static class HackingRemoteAccessController
{
    const string RemoteAccessMarker = "Remote access established";

    public static bool PrepareForHLine(bool establishesRemoteAccess)
    {
        bool mazeWasOpen = HackingMazeMinigame.ForceCloseBecauseHReturned();
        var desktop = ResolveDesktopUi();

        if (establishesRemoteAccess)
        {
            desktop?.NotifyRemoteAccessEstablished();
            return false;
        }

        desktop?.RevokeRemoteAccess();
        if (mazeWasOpen)
            ResolveOllama()?.ApplySuspicionIncrementForCaughtHacking();
        return mazeWasOpen;
    }

    public static bool ResponseEstablishesRemoteAccess(string hMessage) =>
        !string.IsNullOrWhiteSpace(hMessage)
        && hMessage.IndexOf(RemoteAccessMarker, System.StringComparison.OrdinalIgnoreCase) >= 0;

    public static string EnsureCaughtHackingComment(string hReply)
    {
        if (string.IsNullOrWhiteSpace(hReply))
            return hReply;

        var trimmed = hReply.Trim();
        if (trimmed.IndexOf("catch you", System.StringComparison.OrdinalIgnoreCase) >= 0
            || trimmed.IndexOf("caught you", System.StringComparison.OrdinalIgnoreCase) >= 0
            || trimmed.IndexOf("mid-breach", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return trimmed;

        return trimmed
               + "\n\nI saw you on the terminal. You think I wouldn't catch you mid-breach, bru? Don't touch my systems again until I'm gone.";
    }

    static ComputerDesktopUI ResolveDesktopUi() =>
        Object.FindFirstObjectByType<ComputerDesktopUI>(FindObjectsInactive.Include);

    static OllamaConnector ResolveOllama() =>
        Object.FindFirstObjectByType<OllamaConnector>(FindObjectsInactive.Include);
}
