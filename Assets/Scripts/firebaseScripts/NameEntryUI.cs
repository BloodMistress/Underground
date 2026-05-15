using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class NameEntryUI : MonoBehaviour
{
    public GameObject panelRoot;      // сама панель ввода имени
    public TMP_InputField nameInput;  // поле ввода имени
    public Button playButton;         // кнопка "»грать"
    public Button cancelButton;       // можно скрыть если не нужна

    private const string PREF_KEY = "PlayerName";
    public string gameSceneName = "game"; // сцена игры

    private void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);

        // если пол€ не назначены вручную Ч найти автоматически
        if (nameInput == null) nameInput = GetComponentInChildren<TMP_InputField>();
        if (playButton != null) playButton.onClick.AddListener(OnPlay);
        if (cancelButton != null) cancelButton.onClick.AddListener(OnCancel);

        // подставить предыдущее им€
        string saved = PlayerPrefs.GetString(PREF_KEY, "");
        if (nameInput != null) nameInput.text = saved;
    }

    public void Show()
    {
        Debug.Log("NameEntryUI.Show");
        if (panelRoot != null) panelRoot.SetActive(true);
        if (nameInput != null) nameInput.ActivateInputField();
    }

    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void OnPlay()
    {
        Debug.Log("NameEntryUI.OnPlay input=" + nameInput.text);
        string name = nameInput != null ? nameInput.text.Trim() : "";
        if (string.IsNullOrEmpty(name))
            name = "Player";

        name = SanitizeName(name);

        PlayerPrefs.SetString(PREF_KEY, name);
        PlayerPrefs.Save();

        Hide();

        // переход в игру
        GameSettings.Instance.LoadScene(gameSceneName);
    }

    private void OnCancel()
    {
        // вернутьс€ к меню 
        Hide();
    }

    public static string GetSavedName()
    {
        return PlayerPrefs.GetString(PREF_KEY, "Player");
    }

    private string SanitizeName(string s)
    {
        if (s.Length > 20) s = s.Substring(0, 20);

        // запрещЄнные Firebase символы
        s = s.Replace(".", "")
             .Replace("#", "")
             .Replace("$", "")
             .Replace("/", "")
             .Replace("[", "")
             .Replace("]", "");

        return s;
    }
}
