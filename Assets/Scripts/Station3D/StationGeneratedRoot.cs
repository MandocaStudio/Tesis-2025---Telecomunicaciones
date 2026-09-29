using UnityEngine;

/// <summary>
/// Marca el GameObject raíz del blockout que construye StationGenerator. Al regenerar, el
/// generador solo borra objetos con esta marca: todo lo que se modele a mano FUERA de ella
/// (detalle con ProBuilder, atrezo) sobrevive. Lo que se meta DENTRO se pierde.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("")] // solo la pone el generador
public class StationGeneratedRoot : MonoBehaviour
{
    [Tooltip("Layout del que salió este blockout. El constructor lo reutiliza al regenerar.")]
    public StationLayout layout;
}
