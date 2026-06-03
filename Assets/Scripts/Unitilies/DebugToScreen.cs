using UnityEngine;

public class DebugToScreen : MonoBehaviour
{
    private static Coroutine _activeHideCoroutine;

    public static void ShowMessage(string newMessage, float duration = 5f)
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowInfo(newMessage);

            // Cancel previous hide coroutine so it doesn't hide this message early
            if (_activeHideCoroutine != null)
            {
                UIManager.Instance.StopCoroutine(_activeHideCoroutine);
            }
            _activeHideCoroutine = UIManager.Instance.StartCoroutine(HideAfterDelay(duration));
        }
    }

    private static System.Collections.IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        UIManager.Instance?.HideInfo();
        _activeHideCoroutine = null;
    }
}
