//using UnityEngine;
//using TMPro;
//using System.Linq;
//using UnityEngine.SceneManagement;

//public class GameManager : MonoBehaviour
//{
//    public static GameManager Instance { get; private set; }

//    [Header("UI References")]
//    public GameObject resultsPanel;
//    public Transform resultsContainer;
//    public GameObject resultEntryPrefab;

//    public FirebaseLeaderboard firebaseLeaderboard; // присвоить в инспекторе


//    private bool gameEnded = false;

//    private void Awake()
//    {
//        // Настраиваем синглтон
//        if (Instance == null)
//        {
//            Instance = this;
//            DontDestroyOnLoad(gameObject);
//        }
//        else
//        {
//            //Destroy(gameObject);
//            return;
//        }

//        // Скрываем панель результатов при старте
//        if (resultsPanel != null)
//            resultsPanel.SetActive(false);

//        // Подписываемся на событие загрузки сцены
//        SceneManager.sceneLoaded += OnSceneLoaded;
//    }

//    private void OnDestroy()
//    {
//        SceneManager.sceneLoaded -= OnSceneLoaded;
//    }

//    // Когда загружается новая сцена — пробуем найти UI-элементы заново
//    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
//    {
//        FindUIReferences();
//    }

//    private void FindUIReferences()
//    {
//        // Если уже всё назначено — ничего не делаем
//        if (resultsPanel != null && resultsContainer != null && resultEntryPrefab != null)
//            return;

//        // Пробуем найти панель результатов по тегу или имени
//        if (resultsPanel == null)
//            resultsPanel = GameObject.Find("ResultsPanel");

//        if (resultsContainer == null && resultsPanel != null)
//            resultsContainer = resultsPanel.transform.Find("ResultsContainer");

//        // Если префаб не назначен — пробуем найти в ресурсах
//        if (resultEntryPrefab == null)
//            resultEntryPrefab = Resources.Load<GameObject>("ResultEntry");

//        if (resultsPanel != null)
//            resultsPanel.SetActive(false);

//        Debug.Log("[GameManager] UI references refreshed after scene load.");
//    }

//    void Update()
//    {
//        // Проверяем, когда все движущиеся препятствия исчезли
//        if (!gameEnded && FindObjectsByType<ObstacleMover>(FindObjectsSortMode.None).Length == 0)
//            EndGame();
//    }
//    void EndGame()
//    {
//        gameEnded = true;

//        // Убедимся, что ссылки на UI есть
//        FindUIReferences();

//        if (resultsPanel == null || resultsContainer == null || resultEntryPrefab == null)
//        {
//            Debug.LogError("[GameManager] UI references missing! Can't display results.");
//            return;
//        }

//        // Активируем панель результатов
//        resultsPanel.SetActive(true);

//        // Получаем всех игроков и сортируем по HP (от большего к меньшему)
//        var players = FindObjectsOfType<PlayerHealth>()
//            .OrderByDescending(p => p.hp)
//            .ToList();

//        // Очищаем старые записи
//        foreach (Transform child in resultsContainer)
//            Destroy(child.gameObject);

//        int rank = 1;
//        foreach (var p in players)
//        {
//            GameObject entry = Instantiate(resultEntryPrefab, resultsContainer);
//            TMP_Text txt = entry.GetComponent<TMP_Text>();

//            if (txt != null)
//            {
//                txt.text = $"{rank}. {p.gameObject.name} — {p.hp} HP";


//                if (rank == 1)
//                {
//                    txt.color = new Color(1f, 0.85f, 0.2f); // золотистый
//                    txt.fontStyle = TMPro.FontStyles.Bold;
//                }
//            }
//            else
//            {
//                Debug.LogWarning("[GameManager] resultEntryPrefab has no TMP_Text component!");
//            }

//            rank++;
//        }

//        Debug.Log("[GameManager] Results panel displayed successfully.");
//    }


//    public void ApplySlowToOthers(PlayerController collector, float duration, float slowFactor)
//    {
//        var players = FindObjectsOfType<PlayerController>();
//        foreach (var pc in players)
//        {
//            if (pc == collector) continue;
//            pc.ApplySpeedMultiplier(slowFactor, duration);
//        }
//    }
//}
using UnityEngine;
using TMPro;
using System.Linq;
using UnityEngine.SceneManagement;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject resultsPanel;
    public Transform resultsContainer;
    public GameObject resultEntryPrefab;

    private bool gameEnded = false;

    // Firebase
    private DatabaseReference dbReference;
    private bool firebaseReady = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else { return; }

        if (resultsPanel != null) resultsPanel.SetActive(false);
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Инициализация Firebase
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                dbReference = FirebaseDatabase.DefaultInstance.RootReference;
                firebaseReady = true;
                Debug.Log("[GameManager] Firebase ready.");
            }
            else
            {
                Debug.LogError("[GameManager] Firebase error: " + task.Result);
            }
        });
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m) { FindUIReferences(); }

    void Update()
    {
        if (!gameEnded && FindObjectsByType<ObstacleMover>(FindObjectsSortMode.None).Length == 0)
            EndGame();
    }

    void EndGame()
    {
        gameEnded = true;
        // UI checks omitted (как у тебя)
        resultsPanel.SetActive(true);

        var players = FindObjectsOfType<PlayerHealth>().OrderByDescending(p => p.hp).ToList();
        // очищаем и отображаем как у тебя (Instantiate resultEntryPrefab)...

        // Отправляем каждого игрока в leaderboard (firebase)
        foreach (var p in players)
        {
            string localName = NameEntryUI.GetSavedName(); // имя текущего пользователя (если есть)
            // если у тебя несколько локальных игроков, можно сделать диалог ввода имени для каждого
            int score = Mathf.Clamp(p.hp, 0, p.maxHP); // защита от странных значений
            SubmitToLeaderboard(localName, score);
        }
    }

    private void SubmitToLeaderboard(string playerName, int score)
    {
        if (!firebaseReady)
        {
            Debug.LogWarning("[GameManager] Firebase not ready — skip submit.");
            return;
        }

        // Формируем запись: name, score, timestamp (серверный)
        Dictionary<string, object> entry = new Dictionary<string, object>();
        entry["name"] = playerName;
        entry["score"] = score;
        entry["time"] = ServerValue.Timestamp;

        // push
        dbReference.Child("leaderboard").Push().SetValueAsync(entry).ContinueWithOnMainThread(t =>
        {
            if (t.IsCompleted) Debug.Log($"[GameManager] Submitted {playerName}:{score}");
            else Debug.LogError("[GameManager] Submit failed: " + t.Exception);
        });
    }

    public void ApplySlowToOthers(PlayerController collector, float duration, float slowFactor)
    {
        var players = FindObjectsOfType<PlayerController>();
        foreach (var pc in players)
        {
            if (pc == collector) continue;
            pc.ApplySpeedMultiplier(slowFactor, duration);
        }
    }
}
