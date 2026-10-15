using UnityEngine;

// Pon este script en un GameObject con un Collider marcado como "Is Trigger",
// colocado en el punto al que el jugador debe llegar para ganar.
public class ZonaObjetivo : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (GameManager.Instancia != null)
            {
                GameManager.Instancia.Ganar();
            }
        }
    }
}
