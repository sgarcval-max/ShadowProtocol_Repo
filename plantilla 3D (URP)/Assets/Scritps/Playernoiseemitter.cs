using UnityEngine;

// Añádelo al Player. Cuando el jugador corre (Sprint), emite pulsos de ruido
// que los EnemyFieldOfView cercanos pueden "oír" sin necesidad de tener línea de visión.
public class PlayerNoiseEmitter : MonoBehaviour
{
    [Header("Ruido al correr")]
    [SerializeField] private float radioRuidoCorriendo = 6f;
    [SerializeField] private float sospechaPorRuido = 15f; // cuánto sube la sospecha de un enemigo que lo oye
    [SerializeField] private float intervaloEntrePulsos = 0.5f; // cada cuánto emite un pulso de ruido

    [Header("Filtro de qué puede oír el ruido")]
    [SerializeField] private LayerMask capaEnemigos;

    private float temporizadorPulso = 0f;

    // PlayerController debe indicar si está corriendo. Lo exponemos como propiedad pública sencilla.
    public bool EstaCorriendo { get; set; }

    private void Update()
    {
        if (!EstaCorriendo) return;

        temporizadorPulso += Time.deltaTime;
        if (temporizadorPulso >= intervaloEntrePulsos)
        {
            temporizadorPulso = 0f;
            EmitirPulsoDeRuido();
        }
    }

    private void EmitirPulsoDeRuido()
    {
        Collider[] enemigosCercanos = Physics.OverlapSphere(transform.position, radioRuidoCorriendo, capaEnemigos);

        foreach (Collider col in enemigosCercanos)
        {
            EnemyFieldOfView fov = col.GetComponent<EnemyFieldOfView>();
            if (fov != null)
            {
                fov.RegistrarRuido(transform.position, sospechaPorRuido);
            }
        }
    }

    // Ayuda visual en el Editor para ver el radio de ruido mientras corres
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, radioRuidoCorriendo);
    }
}
