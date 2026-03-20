using UnityEngine;
using UnityEditor;

public class FixThrowableItems
{
    [MenuItem("Tools/Fix Throwable Items")]
    public static void FixAll()
    {
        string[] guids = AssetDatabase.FindAssets("t:ItemData");
        int count = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item != null)
            {
                bool modified = false;

                if (!item.isThrowable)
                {
                    item.isThrowable = true;
                    modified = true;
                }

                if (item.worldModelPrefab == null || item.projectilePrefab == null)
                {
                    // Thử tìm prefab có tên tương ứng
                    string prefabName = $"Pickable_{item.itemName.Replace(" ", "")}";
                    string[] prefabGuids = AssetDatabase.FindAssets($"{prefabName} t:Prefab");
                    if (prefabGuids.Length > 0)
                    {
                        string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[0]);
                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                        if (prefab != null)
                        {
                            if (item.worldModelPrefab == null) item.worldModelPrefab = prefab;
                            if (item.projectilePrefab == null) item.projectilePrefab = prefab;
                            if (item.handModelPrefab == null) item.handModelPrefab = prefab;
                            modified = true;
                        }
                    }
                }

                if (modified)
                {
                    EditorUtility.SetDirty(item);
                    count++;
                }
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"Đã sửa chữa và cấp phép ném cho {count} vật phẩm (Bao gồm gắn lại 3D model).");
    }
}
