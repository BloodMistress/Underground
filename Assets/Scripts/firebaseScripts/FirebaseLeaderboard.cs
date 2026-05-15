using System;
using System.Collections.Generic;
using System.Linq;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using UnityEngine;

public class FirebaseLeaderboard : MonoBehaviour
{
    private DatabaseReference scoresRef;

    private void Awake()
    {
        // Если Firebase ещё не инициализирован — будем ждать
        if (!FirebaseInit.IsReady)
        {
            Debug.Log("[FirebaseLeaderboard] Waiting FirebaseInit...");
            // простой таймер: запустить Setup позднее
            Invoke(nameof(Setup), 0.5f);
        }
        else
        {
            Setup();
        }
    }

    private void Setup()
    {
        scoresRef = FirebaseInit.DB.GetReference("scores");
        Debug.Log("[FirebaseLeaderboard] Ready, scoresRef set");
    }

    [Serializable]
    public class ScoreEntry
    {
        public string uid;
        public string name;
        public long score;
        public object ts;
    }

    public async System.Threading.Tasks.Task<bool> SubmitScore(string playerName, long score)
    {
        if (scoresRef == null)
        {
            Debug.LogError("[FirebaseLeaderboard] DB not ready");
            return false;
        }

        try
        {
            string uid = FirebaseAuth.DefaultInstance.CurrentUser?.UserId ?? "anonymous";
            var newRef = scoresRef.Push();
            var entry = new Dictionary<string, object>
            {
                { "uid", uid },
                { "name", playerName },
                { "score", score },
                { "ts", ServerValue.Timestamp }
            };

            await newRef.SetValueAsync(entry);
            Debug.Log($"[FirebaseLeaderboard] Submitted {playerName} {score}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError("[FirebaseLeaderboard] SubmitScore error: " + ex);
            return false;
        }
    }

    // Получить Top N
    public void GetTopScores(int limit, Action<List<ScoreEntry>> callback)
    {
        if (scoresRef == null) { callback?.Invoke(new List<ScoreEntry>()); return; }
        var q = scoresRef.OrderByChild("score").LimitToLast(limit);
        q.GetValueAsync().ContinueWith(t =>
        {
            var list = new List<ScoreEntry>();
            if (t.IsFaulted || t.IsCanceled) { callback?.Invoke(list); return; }

            var snap = t.Result;
            foreach (var child in snap.Children)
            {
                try
                {
                    var dict = child.Value as Dictionary<string, object>;
                    if (dict == null) continue;
                    var e = new ScoreEntry
                    {
                        uid = dict.ContainsKey("uid") ? dict["uid"].ToString() : "",
                        name = dict.ContainsKey("name") ? dict["name"].ToString() : "Player",
                        score = dict.ContainsKey("score") ? Convert.ToInt64(dict["score"]) : 0,
                        ts = dict.ContainsKey("ts") ? dict["ts"] : 0
                    };
                    list.Add(e);
                }
                catch { }
            }

            list = list.OrderByDescending(x => x.score).ToList();
            callback?.Invoke(list);
        });
    }
}
