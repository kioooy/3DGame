using UnityEngine;
using System.Collections.Generic;

public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SaveGame()
    {
        // 1. Lưu Vị trí Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerPrefs.SetFloat("PlayerX", player.transform.position.x);
            PlayerPrefs.SetFloat("PlayerY", player.transform.position.y);
            PlayerPrefs.SetFloat("PlayerZ", player.transform.position.z);
            PlayerPrefs.SetFloat("PlayerRotY", player.transform.eulerAngles.y);
        }

        // 2. Lưu Inventory
        if (InventoryManager.Instance != null)
        {
            InventorySaveData invData = new InventorySaveData();
            invData.slots = new List<SlotSaveData>();
            
            var allSlots = InventoryManager.Instance.GetAllSlots();
            for (int i = 0; i < allSlots.Length; i++)
            {
                if (!allSlots[i].IsEmpty && allSlots[i].item != null)
                {
                    SlotSaveData sData = new SlotSaveData();
                    sData.slotIndex = i;
                    sData.itemName = allSlots[i].item.name; // Lưu theo tên file ScriptableObject
                    sData.quantity = allSlots[i].quantity;
                    invData.slots.Add(sData);
                }
            }

            string json = JsonUtility.ToJson(invData);
            PlayerPrefs.SetString("InventoryData", json);
        }

        // Đánh dấu cờ đã save
        PlayerPrefs.SetInt("HasSavedGame", 1);
        PlayerPrefs.Save();
        
        // 3. Lưu Save Sổ tay côn trùng (để đồng bộ nếu muốn)
        if (EncyclopediaManager.Instance != null)
        {
            EncyclopediaManager.Instance.SaveData();
        }

        Debug.Log("<color=green>[SaveLoadManager]</color> Đã lưu Game thành công!");
    }

    public bool LoadGame()
    {
        if (PlayerPrefs.GetInt("HasSavedGame", 0) != 1)
        {
            Debug.LogWarning("[SaveLoadManager] Không có dữ liệu Save!");
            return false;
        }

        // 1. Load Vị trí Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // Tắt CharacterController để có thể dịch chuyển vị trí thực tế
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            Vector3 pos = new Vector3(
                PlayerPrefs.GetFloat("PlayerX"),
                PlayerPrefs.GetFloat("PlayerY"),
                PlayerPrefs.GetFloat("PlayerZ")
            );
            player.transform.position = pos;
            player.transform.rotation = Quaternion.Euler(0, PlayerPrefs.GetFloat("PlayerRotY"), 0);

            if (cc != null) cc.enabled = true;
        }

        // 2. Load Inventory
        if (InventoryManager.Instance != null)
        {
            string json = PlayerPrefs.GetString("InventoryData", "");
            if (!string.IsNullOrEmpty(json))
            {
                InventorySaveData invData = JsonUtility.FromJson<InventorySaveData>(json);
                if (invData != null && invData.slots != null)
                {
                    InventoryManager.Instance.ClearInventory();
                    
                    ItemData[] allItems = Resources.LoadAll<ItemData>("");
                    var allSlots = InventoryManager.Instance.GetAllSlots();

                    foreach (var sData in invData.slots)
                    {
                        ItemData foundItem = null;
                        foreach (var item in allItems)
                        {
                            if (item.name == sData.itemName) { foundItem = item; break; }
                        }

                        if (foundItem != null && sData.slotIndex < allSlots.Length)
                        {
                            allSlots[sData.slotIndex].AddItem(foundItem, sData.quantity);
                        }
                    }
                }
            }
        }
        
        // 3. Load Sổ tay Bách Khoa
        if (EncyclopediaManager.Instance != null)
        {
            EncyclopediaManager.Instance.LoadData();
        }

        Debug.Log("<color=green>[SaveLoadManager]</color> Đã nạp Game thành công!");
        return true;
    }
}

[System.Serializable]
public class InventorySaveData
{
    public List<SlotSaveData> slots;
}

[System.Serializable]
public class SlotSaveData
{
    public int slotIndex;
    public string itemName;
    public int quantity;
}

