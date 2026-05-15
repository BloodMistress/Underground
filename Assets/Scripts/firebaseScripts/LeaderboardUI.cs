using UnityEngine;
using TMPro;
using Firebase.Database;
using Firebase.Extensions;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.UI;

public class LeaderboardUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panelRoot;         // Сама панель (LeaderboardPanel)
    public Transform topContainer;       // Контейнер для Top-10
    public Transform aroundContainer;    // Контейнер для “вокруг игрока”
    public GameObject entryPrefab;       // Префаб строки (TMP_Text)
    public Button closeButton;           // Кнопка “Закрыть”

    [Header("Settings")]
    public int topLimit = 10;            // сколько топ игроков показывать
    public int scanLimit = 200;          // максимум записей для поиска текущего

    private DatabaseReference dbRef;

    private void Awake()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        // Подключаем кнопку "Закрыть"
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);

        // Если не указана панель, используем сам объект
        if (panelRoot == null)
            panelRoot = gameObject;

        // По умолчанию панель скрыта
        panelRoot.SetActive(false);
    }

    public void Show()
    {
        Debug.Log("LeaderboardUI.Show");
        panelRoot.SetActive(true);
        LoadLeaderboard();
    }

    public void Hide()
    {
        panelRoot.SetActive(false);
    }

    public void LoadLeaderboard()
    {
        Debug.Log("LoadLeaderboard called. topContainer=" + topContainer + " entryPrefab=" + entryPrefab);
        if (topContainer == null) { Debug.LogError("topContainer is null"); return; }
        if (entryPrefab == null) { Debug.LogError("entryPrefab is null"); return; }
        // Загрузка TOP-10
        dbRef.Child("leaderboard").OrderByChild("score").LimitToLast(topLimit)
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted) { Debug.LogError("Leaderboard load error"); return; }

                Clear(topContainer);
                var list = task.Result.Children.Select(c => c).ToList();
                list.Reverse(); // сортировка по убыванию

                int rank = 1;
                foreach (var s in list)
                {
                    CreateEntry(topContainer, rank,
                        s.Child("name").Value?.ToString(),
                        s.Child("score").Value?.ToString() ?? "0");
                    rank++;
                }
            });

        // Загрузка “вокруг текущего игрока”
        string myName = NameEntryUI.GetSavedName();
        dbRef.Child("leaderboard").OrderByChild("score").LimitToLast(scanLimit)
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted) return;
                var arr = task.Result.Children.Select(c => new {
                    name = c.Child("name").Value?.ToString(),
                    score = c.Child("score").Value != null ? long.Parse(c.Child("score").Value.ToString()) : 0L
                }).OrderByDescending(x => x.score).ToList();

                Clear(aroundContainer);
                int idx = arr.FindIndex(x => x.name == myName);

                if (idx == -1)
                {
                    // не найден — просто показать первые 3
                    for (int i = 0; i < Mathf.Min(3, arr.Count); i++)
                        CreateEntry(aroundContainer, i + 1, arr[i].name, arr[i].score.ToString());
                    return;
                }

                int start = Mathf.Max(0, idx - 3);
                int end = Mathf.Min(arr.Count - 1, idx + 3);

                for (int i = start; i <= end; i++)
                    CreateEntry(aroundContainer, i + 1, arr[i].name, arr[i].score.ToString(), i == idx);
            });
    }

    void CreateEntry(Transform parent, int rank, string name, string score, bool highlight = false)
    {
        var go = Instantiate(entryPrefab, parent);
        var txt = go.GetComponent<TMP_Text>();

        if (txt != null)
        {
            txt.text = $"{rank}. {name} — {score}";
            if (highlight)
                txt.color = new Color(0.2f, 0.8f, 1f);
        }
    }

    void Clear(Transform t)
    {
        if (t == null) return;
        foreach (Transform c in t)
            Destroy(c.gameObject);
    }
}
