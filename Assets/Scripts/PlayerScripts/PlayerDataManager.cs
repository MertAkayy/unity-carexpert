using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PlayerScripts
{
    public class PlayerDataManager : MonoBehaviour
    {
        public static PlayerDataManager Instance;
        public PlayerData playerData;
        public MarketItemDatabase itemDatabase;
        public System.Action<IUsableTool> OnToolChanged;
        [SerializeField] private Transform holdArea;
        private List<MarketItem> AllItems => itemDatabase.items;
        [Header("Ray Settings")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private float rayDistance = 100f;
        [SerializeField] private LayerMask hitLayers;
        private GameObject _selectedObject;
        private string _saveFilePath;
        [Header("Pickup Parameters")]
        [SerializeField] private float pickupForce = 150.0f;
    
        private GameObject _activeObject;
        private GameObject _heldObject;
        private Rigidbody _heldObjectRigidbody;
        private int _originalLayer = -1;
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (transform.parent != null)
            {
                DontDestroyOnLoad(gameObject);
            }
            _saveFilePath = Path.Combine(Application.persistentDataPath, "playerdata.json");
            LoadData();
        }

        private void OnApplicationQuit()
        {
            SaveData();
        }
        private void Start()
        {
            LoadAllItems();
            
             SelectTool(playerData.inventory[6]);
        }

        private void LoadAllItems()
        {
            playerData.inventory.Clear();
            foreach (var item in AllItems)
            {
                if (item.isTool)
                {
                    Debug.Log(item.name);
                    playerData.inventory.Add(item);
                }
               
            }
        }
        private void Update()
        {
            Raycaster();
        }
        // ReSharper disable Unity.PerformanceAnalysis
        private void Raycaster()
        {
            Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);

            if (Physics.Raycast(ray, out var hit, rayDistance, hitLayers))
            {
                GameObject hitObj = hit.collider.gameObject;
                if (_activeObject == null)
                {
                    _activeObject = hitObj;
                    _originalLayer = _activeObject.layer; // Önceki katmanı kaydet
                    _activeObject.layer = LayerMask.NameToLayer("Highlight");
               
                }
           
                else if (_activeObject != hitObj)
                {
                    // Eski objeyi eski katmanına döndür
                    _activeObject.layer = _originalLayer;

                    // Yeni objeyi aktif yap
                    _activeObject = hitObj;
                    _originalLayer = _activeObject.layer; // Yeni objenin eski katmanı
                    _activeObject.layer = LayerMask.NameToLayer("Highlight");
                }
                // Aynı objeyse hiçbir şey yapma
            }
            else
            {
                if (_activeObject != null)
                {
                    _activeObject.layer = _originalLayer; // Geri kendi katmanına al
                    _activeObject = null;
                    _originalLayer = -1;
                }
            }
        }
//Grap Functions
        void MoveObject()
        {
            if (Vector3.Distance(_heldObject.transform.position, holdArea.position) > 0.1f)
            {
                Vector3 moveDirection = (holdArea.position - _heldObject.transform.position);
                _heldObjectRigidbody.AddForce(moveDirection * pickupForce);
            }
        }
        void PickUpObject(GameObject objectToPickUp)
        {
            if (objectToPickUp.GetComponent<Rigidbody>())
            {
                _heldObjectRigidbody = objectToPickUp.GetComponent<Rigidbody>();
                _heldObjectRigidbody.useGravity = false;
                _heldObjectRigidbody.linearDamping = 10f;
                _heldObjectRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
                _heldObjectRigidbody.transform.SetParent(holdArea);
                _heldObjectRigidbody.transform.localPosition =new Vector3(-0.4f,-1.1f,1.2f);
                _heldObject = objectToPickUp;
            }
        }
        void DropObject()
        {
            _heldObjectRigidbody.useGravity = true;
            _heldObjectRigidbody.linearDamping = 1;
            _heldObjectRigidbody.constraints = RigidbodyConstraints.None;
            _heldObject.transform.SetParent(null);
            _heldObject = null;
        }
        public void CanGrabObejct()
        {
            if (!_heldObject)
            {
                if(_activeObject!=null && _activeObject.TryGetComponent<IGrabbable>(out _))
                    if ( playerData.selectedItem.toolObject==Tool.Handle)
                    {
                        PickUpObject(_activeObject.transform.gameObject);
                    }
            }
            else
            {
                DropObject();
            }

            if (_heldObject != null)
            {
                MoveObject();
            }

        }
        public bool HasActiveInteractable()
        {
            return _activeObject != null && _activeObject.GetComponent<IInteractable>() != null;
        }

        //interactive functions
        public void CanInteract()
        {
            if (_activeObject )
            {
                IInteractable interactable = _activeObject.GetComponent<IInteractable>();
                if (interactable != null)
                    interactable.Interact();
            }
        }

        public void CanReadDocument()
        {
            if (_activeObject )
            {
                
                IReadable readable = _activeObject.GetComponent<IReadable>();
                if (readable != null)
                    readable.Read();
            }
        }
        public void SelectTool(MarketItem item)
        {
            playerData.selectedItem = item;
            if (_selectedObject!=null)
            {
                Destroy(_selectedObject);
                _selectedObject = null;
            }
           
            IUsableTool newTool = null;
            if(item.itemObject != null )
            {
                _selectedObject = Instantiate(item.itemObject, holdArea);
                newTool = _selectedObject.GetComponentInChildren<IUsableTool>();
            }
            GameLogger.Log($"[PlayerDataManager] Tool changed to {item.name} (handler: {(newTool != null ? newTool.GetType().Name : "none")})");
            OnToolChanged?.Invoke(newTool);
        }
        public void SaveData()
        {
            string json = JsonUtility.ToJson(playerData, true);
            File.WriteAllText(_saveFilePath, json);
            Debug.Log("Player data saved to: " + _saveFilePath);
        }
        private void LoadData()
        {
            if (File.Exists(_saveFilePath))
            {
                string json = File.ReadAllText(_saveFilePath);
                playerData = JsonUtility.FromJson<PlayerData>(json);
                Debug.Log("Player data loaded."+playerData.level);
            }
            else
            {
                Debug.Log("No save file found. Creating new player data.");
                playerData = new PlayerData("selamo"); // Varsayılan isim
            }
        }

        public void AddNoteToVehicle(string s)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Adds money to the player's balance.
        /// </summary>
        /// <param name="amount">Amount to add (positive value)</param>
        public void AddMoney(float amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning("[PlayerDataManager] AddMoney called with negative value. Use DeductMoney instead.");
                return;
            }
            playerData.money += amount;
            Debug.Log($"[PlayerDataManager] Added ${amount:F2}. New balance: ${playerData.money:F2}");
        }

        /// <summary>
        /// Deducts money from the player's balance.
        /// </summary>
        /// <param name="amount">Amount to deduct (positive value)</param>
        /// <returns>True if the deduction was successful</returns>
        public bool DeductMoney(float amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning("[PlayerDataManager] DeductMoney called with negative value.");
                return false;
            }
            if (playerData.money >= amount)
            {
                playerData.money -= amount;
                Debug.Log($"[PlayerDataManager] Deducted ${amount:F2}. New balance: ${playerData.money:F2}");
                return true;
            }
            Debug.LogWarning($"[PlayerDataManager] Insufficient funds. Have: ${playerData.money:F2}, Need: ${amount:F2}");
            return false;
        }
    }
}
