using UnityEngine;

public class DebugToggle : MonoBehaviour
{
    public static bool DebugEnabled { get; set; } = false;

    public static void SetDebugMode(bool enabled)
    {
        DebugEnabled = enabled;
        Debug.Log($"[DEBUG] Debug inputs {(DebugEnabled ? "ACTIVADOS (Dev Mode)" : "DESACTIVADOS (Normal Mode)")}");
    }
}
