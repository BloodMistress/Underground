using UnityEngine;

public class GameSettings : MonoBehaviour
{
    public const string MasterVolumeKey = "settings.masterVolume";
    public const string MouseSensitivityKey = "settings.mouseSensitivity";

    public const float DefaultMasterVolume = 0.8f;
    public const float DefaultMouseSensitivity = 1.0f;

    private void Awake()
    {
        ApplySavedSettings();
    }

    public static float GetMasterVolume()
    {
        return Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, DefaultMasterVolume));
    }

    public static void SetMasterVolume(float value)
    {
        float normalizedValue = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MasterVolumeKey, normalizedValue);
        AudioListener.volume = normalizedValue;
    }

    public static float GetMouseSensitivity()
    {
        return Mathf.Clamp(PlayerPrefs.GetFloat(MouseSensitivityKey, DefaultMouseSensitivity), 0.25f, 4.0f);
    }

    public static void SetMouseSensitivity(float value)
    {
        PlayerPrefs.SetFloat(MouseSensitivityKey, Mathf.Clamp(value, 0.25f, 4.0f));
    }

    public static void ApplySavedSettings()
    {
        AudioListener.volume = GetMasterVolume();
    }
}