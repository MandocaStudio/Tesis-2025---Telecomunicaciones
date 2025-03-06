using UnityEngine;
using UnityEngine.SceneManagement;

public class TeoriaManager : MonoBehaviour
{
    public GameObject[] diapositivas; 
    private int indiceActual = 0;

    private void Start()
    {
        
        MostrarDiapositiva(indiceActual);
    }

  
    private void MostrarDiapositiva(int indice)
    {
        
        for (int i = 0; i < diapositivas.Length; i++)
        {
            diapositivas[i].SetActive(false);
        }

       
        diapositivas[indice].SetActive(true);
    }

    public void SiguienteDiapositiva()
    {
        if (indiceActual < diapositivas.Length - 1) 
        {
            indiceActual++;
            MostrarDiapositiva(indiceActual);
        }
    }

    public void AnteriorDiapositiva()
    {
        if (indiceActual > 0) 
        {
            indiceActual--;
            MostrarDiapositiva(indiceActual);
        }
    }

    public void VolverAlMenu()
    {
        SceneManager.LoadScene(0); 
    }
}