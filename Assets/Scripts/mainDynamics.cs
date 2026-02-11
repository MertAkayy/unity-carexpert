using PlayerScripts;
using UnityEngine;

public class mainDynamics : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var data = PlayerDataManager.Instance.playerData;
        Debug.Log(data.name);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
