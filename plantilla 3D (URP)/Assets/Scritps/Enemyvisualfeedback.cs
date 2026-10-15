using UnityEngine;

// Añade este script al mismo objeto "Enemigo".
// Se encarga solo de la parte visual: no toca la lógica de la IA.
[RequireComponent(typeof(EnemyAI))]
public class EnemyVisualFeedback : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Renderer rendererEnemigo; // el MeshRenderer de la cápsula del enemigo
    [SerializeField] private TextMesh iconoEstado;      // texto 3D flotando sobre la cabeza
    [SerializeField] private EnemyFieldOfView fov;      // para leer el NivelSospecha
    [SerializeField] private Transform barraSospechaRelleno; // un cubo/quad hijo que escalaremos en X (0 a 1 en localScale.x)

    [Header("Colores por estado")]
    [SerializeField] private Color colorPatrulla = Color.white;
    [SerializeField] private Color colorAlerta = new Color(1f, 0.6f, 0f); // naranja
    [SerializeField] private Color colorPersecucion = Color.red;
    [SerializeField] private Color colorBusqueda = Color.yellow;

    private EnemyAI enemyAI;
    private EnemyAI.Estado estadoAnterior;

    private void Awake()
    {
        enemyAI = GetComponent<EnemyAI>();

        if (rendererEnemigo == null)
            rendererEnemigo = GetComponentInChildren<Renderer>();

        if (fov == null)
            fov = GetComponent<EnemyFieldOfView>();
    }

    private void Start()
    {
        estadoAnterior = enemyAI.EstadoActual;
        ActualizarVisual(enemyAI.EstadoActual);
    }

    private void Update()
    {
        // Solo actualizamos el visual cuando el estado realmente cambia, no cada frame
        if (enemyAI.EstadoActual != estadoAnterior)
        {
            estadoAnterior = enemyAI.EstadoActual;
            ActualizarVisual(estadoAnterior);
        }

        // El icono siempre mira hacia la cámara principal, si existe
        if (iconoEstado != null && Camera.main != null)
        {
            iconoEstado.transform.LookAt(Camera.main.transform);
            iconoEstado.transform.Rotate(0f, 180f, 0f); // corrige que el texto no salga espejado
        }

        ActualizarBarraSospecha();
    }

    private void ActualizarBarraSospecha()
    {
        if (barraSospechaRelleno == null || fov == null) return;

        // Suponemos que el "100%" de sospecha equivale a localScale.x = 1 (ajústalo si tu umbral no es 100)
        float proporcion = Mathf.Clamp01(fov.NivelSospecha / 100f);

        Vector3 escala = barraSospechaRelleno.localScale;
        escala.x = proporcion;
        barraSospechaRelleno.localScale = escala;

        // La barra solo se muestra si hay algo de sospecha; en Patrulla tranquila, se oculta
        barraSospechaRelleno.gameObject.SetActive(proporcion > 0.01f);
    }

    private void ActualizarVisual(EnemyAI.Estado estado)
    {
        Color colorNuevo = colorPatrulla;
        string textoIcono = "";

        switch (estado)
        {
            case EnemyAI.Estado.Patrulla:
                colorNuevo = colorPatrulla;
                textoIcono = "";
                break;
            case EnemyAI.Estado.Alerta:
                colorNuevo = colorAlerta;
                textoIcono = "!?";
                break;
            case EnemyAI.Estado.Persecucion:
                colorNuevo = colorPersecucion;
                textoIcono = "!";
                break;
            case EnemyAI.Estado.Busqueda:
                colorNuevo = colorBusqueda;
                textoIcono = "?";
                break;
        }

        if (rendererEnemigo != null)
        {
            rendererEnemigo.material.color = colorNuevo;
        }

        if (iconoEstado != null)
        {
            iconoEstado.text = textoIcono;
            iconoEstado.color = colorNuevo;
        }
    }
}