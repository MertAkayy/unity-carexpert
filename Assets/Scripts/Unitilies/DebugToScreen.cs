using UnityEngine;

public class DebugToScreen : MonoBehaviour
{
    public static string message = "Merhaba, bu bir debug mesajıdır!";
    private static float messageEndTime = 0f;
    void OnGUI()
    {
        if (!string.IsNullOrEmpty(message) && Time.time < messageEndTime)
        {
            GUI.Label(new Rect(10, 10, 500, 20), message);
        }
    }
    public static void ShowMessage(string newMessage, float duration = 10f)
    {
        message = newMessage;
        messageEndTime = Time.time + duration;
    }
}