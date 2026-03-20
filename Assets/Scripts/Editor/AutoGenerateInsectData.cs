using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class AutoGenerateInsectData
{
    static AutoGenerateInsectData()
    {
        EditorApplication.delayCall += GenerateData;
    }

    private static void GenerateData()
    {
        string folderPath = "Assets/Resources/Encyclopedia";
        
        // Cố gắng tạo thư mục nếu chưa tồn tại
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
            
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "Encyclopedia");
        }

        string[] samples = { "DeMen", "DeChoat", "XenToc", "ConKien", "DeTrui" };
        string[] names = { "Dế Mèn", "Dế Choắt", "Xén Tóc", "Kiến Quân Đội", "Dế Trũi" };
        bool generatedAny = false;
        
        for (int i = 0; i < samples.Length; i++)
        {
            string path = $"{folderPath}/{samples[i]}.asset";
            if (AssetDatabase.LoadAssetAtPath<InsectData>(path) == null)
            {
                InsectData data = ScriptableObject.CreateInstance<InsectData>();
                data.insectID = samples[i];
                data.insectName = names[i];
                data.description = "Mô tả sinh học về " + names[i] + ". \nHãy cập nhật thêm tại file thiết kế (Data).";
                data.funFact = "Sự thật thú vị: " + names[i] + " thường sống ở đâu?";
                data.dangerLevel = InsectDangerLevel.HienLanh;
                
                AssetDatabase.CreateAsset(data, path);
                generatedAny = true;
            }
        }
        
        if (generatedAny) 
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=green>✅ Đã tự động nạp ScriptableObject Dữ liệu Sổ tay Bách Khoa Toàn Thư!</color>");
        }
    }
}
