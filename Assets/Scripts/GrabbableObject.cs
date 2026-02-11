using UnityEngine;

public class GrabbableObject : MonoBehaviour,IGrabbable
{

    public void Grab()
    {
      GameLogger.Log("GrabbableObject");
    }
}
