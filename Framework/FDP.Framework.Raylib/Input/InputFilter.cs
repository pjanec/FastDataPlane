using ImGuiNET;

namespace FDP.Framework.Raylib.Input;

/// <summary>
/// Utility to detect if ImGui is capturing input (prevents click-through).
/// </summary>
public static class InputFilter
{
    /// <summary>
    /// Returns true if the mouse is hovering over an ImGui window.
    /// Use this to block game-world clicks.
    /// </summary>
    public static bool IsMouseCaptured => ImGui.GetIO().WantCaptureMouse;

    /// <summary>
    /// Returns true if the keyboard is focused on an ImGui input field.
    /// Use this to block game-world hotkeys.
    /// </summary>
    public static bool IsKeyboardCaptured => ImGui.GetIO().WantCaptureKeyboard;
}
