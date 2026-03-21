using UnityEngine;

public enum InsectDangerLevel { HienLanh, NguyHiem, CucDoc }

[CreateAssetMenu(fileName = "New Insect Data", menuName = "Bestiary/Insect Data")]
public class InsectData : ScriptableObject
{
    [Header("Thông tin cơ bản")]
    public string insectID; // Phải duy nhất, VD: "DeMen", "XenToc"
    public string insectName;
    
    [TextArea(3, 5)]
    public string description;
    
    [Header("Thông tin chi tiết")]
    [TextArea(3, 5)]
    public string funFact;
    public InsectDangerLevel dangerLevel;
    
    [Header("Hình ảnh")]
    public Sprite unlockedSprite;
    public Sprite lockedSprite; // Trắng đen hoặc cái bóng
}
