using UnityEngine;
using UnityEngine.SceneManagement;
public class MenuManagerGame : MonoBehaviour
{
   public void OpenScene()
    {
        SceneManager.LoadScene("Unity Standard Demo Scene");
    }
}
