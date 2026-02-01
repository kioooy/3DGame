
    using UnityEngine;
using UnityEngine.UI;

public class SkillDeMen : MonoBehaviour
{
    public Image skillIcon; // Kéo cái hình (Image) của nút vào đây
    public float cooldownTime = 5.0f; // Thời gian hồi chiêu
    private bool isCooldown = false;

    void Start()
    {
        skillIcon.fillAmount = 1; // Mới vào là đầy
    }

    void Update()
    {
        if (isCooldown)
        {
            // Giảm dần vòng quay hồi chiêu
            skillIcon.fillAmount += 1 / cooldownTime * Time.deltaTime;

            if (skillIcon.fillAmount >= 1)
            {
                skillIcon.fillAmount = 1;
                isCooldown = false;
                GetComponent<Button>().interactable = true; // Cho phép bấm lại
            }
        }
    }

    // Gán hàm này vào sự kiện OnClick của nút
    public void UseSkill()
    {
        if (!isCooldown)
        {
            isCooldown = true;
            skillIcon.fillAmount = 0; // Về 0 để bắt đầu hồi
            GetComponent<Button>().interactable = false; // Khóa nút không cho bấm

            // Code gọi hành động nhân vật ở đây (Ví dụ: Player.Attack())
            Debug.Log("Dế Mèn tung cú đá!");
        }
    }
}

