//using UnityEngine;

//public class MenuUI : MonoBehaviour
//{
//    public string gameSceneName = "game"; 
//    public void Start1()
//    {
//        GameSettings.Instance.SetPlayers(1);
//        GameSettings.Instance.LoadScene(gameSceneName);
//    }

//    public void Start2()
//    {
//        GameSettings.Instance.SetPlayers(2);
//        GameSettings.Instance.LoadScene(gameSceneName);
//    }

//    public void Start3()
//    {
//        GameSettings.Instance.SetPlayers(3);
//        GameSettings.Instance.LoadScene(gameSceneName);
//    }

//    public void Start4()
//    {
//        GameSettings.Instance.SetPlayers(4);
//        GameSettings.Instance.LoadScene(gameSceneName);
//    }
//}
using UnityEngine;

public class MenuUI : MonoBehaviour
{
    public GameObject nameEntryPanel; 

    public void Start1() => SelectPlayers(1);
    public void Start2() => SelectPlayers(2);
    public void Start3() => SelectPlayers(3);
    public void Start4() => SelectPlayers(4);

    void SelectPlayers(int count)
    {
        Debug.Log("SelectPlayers called with " + count);
        GameSettings.Instance.SetPlayers(count);
        if (nameEntryPanel == null)
        {
            Debug.LogError("nameEntryPanel is null in MenuUI!");
            return;
        }
        //GameSettings.Instance.SetPlayers(count);
        nameEntryPanel.GetComponent<NameEntryUI>().Show();
    }
}
