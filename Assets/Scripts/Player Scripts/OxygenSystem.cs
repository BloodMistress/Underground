using UnityEngine;
using UnityEngine.UI;

public class OxygenSystem : MonoBehaviour
{
    public float maxOxygen = 100f;
    public float oxygen;
    public float drainRate = 5f;

    public Slider oxygenSlider;

    public bool inWater = false;
    public bool inAirZone = false;

    void Start()
    {
        oxygen = maxOxygen;
    }

    void Update()
    {
        if (inAirZone)
        {
            oxygen += 20f * Time.deltaTime;
        }
        else
        {
            float rate = inWater ? drainRate * 2 : drainRate;
            oxygen -= rate * Time.deltaTime;
        }

        oxygen = Mathf.Clamp(oxygen, 0, maxOxygen);
        oxygenSlider.value = oxygen;

        if (oxygen <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Вы погибли");
        Time.timeScale = 0f;
    }
}