using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Database;

public class FirebaseInit : MonoBehaviour
{
    public static FirebaseAuth Auth => FirebaseAuth.DefaultInstance;
    public static FirebaseDatabase DB => FirebaseDatabase.DefaultInstance;
    public static bool IsReady { get; private set; } = false;

    private async void Awake()
    {
        await InitializeFirebase();
    }

    private async Task InitializeFirebase()
    {
        var deps = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (deps != DependencyStatus.Available)
        {
            Debug.LogError($"[FirebaseInit] Dependencies error: {deps}");
            return;
        }

        Debug.Log("[FirebaseInit] Firebase dependencies OK");

        // Нужен Request to DB URL в Editor? (см тестирование)
        // FirebaseDatabase.DefaultInstance.SetEditorDatabaseUrl("https://<YOUR_PROJECT>.firebaseio.com/");

        // Анонимный вход
        if (Auth.CurrentUser == null)
        {
            try
            {
                var result = await Auth.SignInAnonymouslyAsync();
                Debug.Log($"[FirebaseInit] Signed in anonymously as {result.User.UserId}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[FirebaseInit] Auth error: " + ex);
            }
        }
        else
        {
            Debug.Log($"[FirebaseInit] Already signed: {Auth.CurrentUser.UserId}");
        }

        IsReady = true;
    }
}
