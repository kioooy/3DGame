using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    public Slider hpBar;
    public float maxHP = 100;
    float currentHP;

    void Start()
    {
        currentHP = maxHP;
        hpBar.maxValue = maxHP;
        hpBar.value = currentHP;
    }

    public void TakeDamage(float dmg)
    {
        currentHP -= dmg;
        hpBar.value = currentHP;
    }
}