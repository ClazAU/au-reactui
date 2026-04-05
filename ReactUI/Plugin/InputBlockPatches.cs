using HarmonyLib;

namespace ReactUI.Plugin;

/// <summary>
/// Harmony patches to block Among Us PassiveButton clicks when the mouse
/// is over a ReactUI element. Patches ReceiveClickDown/Up to skip processing.
/// </summary>
[HarmonyPatch(typeof(PassiveButton), nameof(PassiveButton.ReceiveClickDown))]
static class PassiveButtonClickDownPatch
{
    public static bool Prefix() => !Input.InputSystem.BlockGameInput;
}

[HarmonyPatch(typeof(PassiveButton), nameof(PassiveButton.ReceiveClickUp))]
static class PassiveButtonClickUpPatch
{
    public static bool Prefix() => !Input.InputSystem.BlockGameInput;
}
