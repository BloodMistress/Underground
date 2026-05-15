using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHP = 100;
    public int hp;

    public System.Action<PlayerHealth> OnHpChanged;

    void Awake() => hp = maxHP;

    public void Damage(int amount)
    {
        hp = Mathf.Max(0, hp - amount);
        OnHpChanged?.Invoke(this);
    }
}
