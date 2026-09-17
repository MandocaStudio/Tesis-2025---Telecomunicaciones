using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    // Aqui cargas escenas por índice 
    public void LoadSceneByIndex(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }

    // Método para salir de la aplicación obviamente
    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false; // Para salir en el editor
        #else
            Application.Quit(); // Para salir en la build final, 
        #endif
    }
}