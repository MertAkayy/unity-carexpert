using PlayerScripts;
using UnityEngine;

public class CarSeat : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform seatPoint;
    [SerializeField] private Transform exitPoint;

    public void Interact()
    {
        if (PlayerDataManager.Instance == null) return;

        MarketItem selected = PlayerDataManager.Instance.playerData.selectedItem;
        GameLogger.Log($"[CarSeat] Interact called. Selected tool: {(selected != null ? selected.toolObject.ToString() : "NULL")}");

        if (selected == null || selected.toolObject != Tool.Handle) return;

        Player player = FindObjectOfType<Player>();
        if (player == null) return;

        PlayerCharacter character = player.GetComponentInChildren<PlayerCharacter>();
        if (character == null) return;

        if (!character.IsSeated)
        {
            character.Sit(seatPoint, exitPoint);
            GameLogger.Log("[CarSeat] Player seated.");
        }
    }
}
