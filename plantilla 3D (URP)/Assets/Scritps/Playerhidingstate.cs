using UnityEngine;

// Añade este script al Player. Cuenta cuántas zonas de escondite está tocando a la vez
// (por si se solapan dos zonas), para saber si está "oculto" de verdad.
public class PlayerHidingState : MonoBehaviour
{
    [SerializeField] private string tagZonaEscondite = "Escondite";

    private int zonasEsconditeActivas = 0;

    public bool EstaEscondido => zonasEsconditeActivas > 0;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(tagZonaEscondite))
        {
            zonasEsconditeActivas++;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(tagZonaEscondite))
        {
            zonasEsconditeActivas = Mathf.Max(0, zonasEsconditeActivas - 1);
        }
    }
}
