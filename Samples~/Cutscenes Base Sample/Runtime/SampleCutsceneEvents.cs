
using UnityEngine;
using IndieGabo.HandyTools.CutscenesModule.Events;
using IndieGabo.HandyTools.HandyBusModule;

/// <summary>
/// Example event: signals the start of a cutscene.
/// </summary>
/// <summary>
/// Example event: signals the start of a cutscene.
/// </summary>
[CutsceneBusEvent("CutsceneStarted", Description = "Fired when a cutscene begins.")]
public sealed class CutsceneStartedEvent : IEvent
{
    /// <summary>
    /// The name or ID of the cutscene that started.
    /// </summary>
    public string CutsceneName { get; }

    /// <summary>
    /// Constructs the event with the cutscene name.
    /// </summary>
    /// <param name="cutsceneName">The cutscene identifier.</param>
    public CutsceneStartedEvent(string cutsceneName)
    {
        CutsceneName = cutsceneName;
    }

    /// <summary>
    /// Parameterless constructor for registry/discovery.
    /// </summary>
    public CutsceneStartedEvent() { }
}

/// <summary>
/// Example event: signals the end of a cutscene.
/// </summary>
/// <summary>
/// Example event: signals the end of a cutscene.
/// </summary>
[CutsceneBusEvent("CutsceneEnded", Description = "Fired when a cutscene ends.")]
public sealed class CutsceneEndedEvent : IEvent
{
    /// <summary>
    /// The name or ID of the cutscene that ended.
    /// </summary>
    public string CutsceneName { get; }

    /// <summary>
    /// Constructs the event with the cutscene name.
    /// </summary>
    /// <param name="cutsceneName">The cutscene identifier.</param>
    public CutsceneEndedEvent(string cutsceneName)
    {
        CutsceneName = cutsceneName;
    }

    /// <summary>
    /// Parameterless constructor for registry/discovery.
    /// </summary>
    public CutsceneEndedEvent() { }
}

/// <summary>
/// Example event: signals a dialogue line was delivered.
/// </summary>
/// <summary>
/// Example event: signals a dialogue line was delivered.
/// </summary>
[CutsceneBusEvent("DialogueLineSpoken", Description = "Fired when a dialogue line is spoken.")]
public sealed class DialogueLineSpokenEvent : IEvent
{
    /// <summary>
    /// The character who spoke the line.
    /// </summary>
    public string CharacterName { get; }

    /// <summary>
    /// The text of the dialogue line.
    /// </summary>
    public string LineText { get; }

    /// <summary>
    /// Constructs the event with character and line text.
    /// </summary>
    /// <param name="characterName">The speaker's name.</param>
    /// <param name="lineText">The dialogue text.</param>
    public DialogueLineSpokenEvent(string characterName, string lineText)
    {
        CharacterName = characterName;
        LineText = lineText;
    }

    /// <summary>
    /// Parameterless constructor for registry/discovery.
    /// </summary>
    public DialogueLineSpokenEvent() { }
}

/// <summary>
/// Example event: signals a camera shot change in a cutscene.
/// </summary>
/// <summary>
/// Example event: signals a camera shot change in a cutscene.
/// </summary>
[CutsceneBusEvent("CameraShotChanged", Description = "Fired when the camera shot changes.")]
public sealed class CameraShotChangedEvent : IEvent
{
    /// <summary>
    /// The name or ID of the new camera shot.
    /// </summary>
    public string ShotName { get; }

    /// <summary>
    /// Constructs the event with the shot name.
    /// </summary>
    /// <param name="shotName">The camera shot identifier.</param>
    public CameraShotChangedEvent(string shotName)
    {
        ShotName = shotName;
    }

    /// <summary>
    /// Parameterless constructor for registry/discovery.
    /// </summary>
    public CameraShotChangedEvent() { }
}
