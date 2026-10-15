using UnityEngine;

// Genera una malla 2D (en el plano XZ) que representa el cono de visión del enemigo,
// recortada contra los obstáculos mediante Raycasts, igual que en las imágenes de referencia.
// Colócalo en un GameObject HIJO del enemigo, con MeshFilter y MeshRenderer.
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class EnemyVisionMesh : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private EnemyFieldOfView fov; // el script de detección del padre
    [SerializeField] private EnemyAI enemyAI;

    [Header("Config de la malla")]
    [SerializeField] private int rayosPorGrado = 2; // resolución del cono (más = más suave, más coste)
    [SerializeField] private LayerMask capaObstaculos;
    [SerializeField] private float radioVision = 8f;
    [SerializeField] private float anguloVision = 90f;
    [SerializeField] private float alturaMalla = 0.05f; // altura sobre el suelo para evitar z-fighting

    [Header("Colores por estado (alpha incluido)")]
    [SerializeField] private Color colorPatrulla = new Color(1f, 1f, 1f, 0.25f);
    [SerializeField] private Color colorAlerta = new Color(1f, 0.6f, 0f, 0.35f);
    [SerializeField] private Color colorPersecucion = new Color(1f, 0f, 0f, 0.4f);
    [SerializeField] private Color colorBusqueda = new Color(1f, 1f, 0f, 0.3f);

    private Mesh malla;
    private MeshRenderer meshRenderer;

    private void Awake()
    {
        malla = new Mesh { name = "ConoVision" };
        GetComponent<MeshFilter>().mesh = malla;
        meshRenderer = GetComponent<MeshRenderer>();
    }

    private void LateUpdate()
    {
        GenerarMalla();
        ActualizarColor();
    }

    private void GenerarMalla()
    {
        int numRayos = Mathf.Max(1, Mathf.RoundToInt(anguloVision * rayosPorGrado));
        float anguloInicial = -anguloVision / 2f;
        float incremento = anguloVision / numRayos;

        // +1 por el vértice central (origen del cono)
        Vector3[] vertices = new Vector3[numRayos + 2];
        int[] triangulos = new int[numRayos * 3];

        vertices[0] = Vector3.zero; // origen, en coordenadas locales de este objeto

        for (int i = 0; i <= numRayos; i++)
        {
            float anguloActual = anguloInicial + incremento * i;
            Vector3 direccion = Quaternion.Euler(0, anguloActual, 0) * transform.forward;

            float distanciaFinal = radioVision;

            // Lanzamos el rayo en coordenadas de mundo pero guardamos el resultado en locales
            if (Physics.Raycast(transform.position, direccion, out RaycastHit hit, radioVision, capaObstaculos))
            {
                distanciaFinal = hit.distance;
            }

            Vector3 puntoLocal = transform.InverseTransformDirection(direccion) * distanciaFinal;
            vertices[i + 1] = new Vector3(puntoLocal.x, alturaMalla, puntoLocal.z);
        }

        for (int i = 0; i < numRayos; i++)
        {
            triangulos[i * 3] = 0;
            triangulos[i * 3 + 1] = i + 1;
            triangulos[i * 3 + 2] = i + 2;
        }

        malla.Clear();
        malla.vertices = vertices;
        malla.triangles = triangulos;
        malla.RecalculateNormals();
    }

    private void ActualizarColor()
    {
        if (enemyAI == null || meshRenderer == null) return;

        Color colorObjetivo = colorPatrulla;
        switch (enemyAI.EstadoActual)
        {
            case EnemyAI.Estado.Patrulla: colorObjetivo = colorPatrulla; break;
            case EnemyAI.Estado.Alerta: colorObjetivo = colorAlerta; break;
            case EnemyAI.Estado.Persecucion: colorObjetivo = colorPersecucion; break;
            case EnemyAI.Estado.Busqueda: colorObjetivo = colorBusqueda; break;
        }

        meshRenderer.material.color = colorObjetivo;
    }
}
