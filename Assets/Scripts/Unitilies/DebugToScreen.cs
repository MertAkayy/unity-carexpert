using UnityEngine;

public class DebugToScreen : MonoBehaviour
{
    public static void ShowMessage(string newMessage, float duration = 5f)
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowInfo(newMessage);
            // Use coroutine on UIManager to auto-hide
            UIManager.Instance.StartCoroutine(HideAfterDelay(duration));
        }
    }

    private static System.Collections.IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        UIManager.Instance?.HideInfo();
    }
}
