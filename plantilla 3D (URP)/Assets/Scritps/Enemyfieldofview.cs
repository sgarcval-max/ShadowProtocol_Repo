using UnityEngine;

// Se encarga de detectar al jugador (vista) y calcular un nivel de sospecha progresivo.
// EnemyAI.cs leerá "NivelSospecha" (0-100) en vez de un simple booleano.
public class EnemyFieldOfView : MonoBehaviour
{
    [Header("Parámetros de visión")]
    [SerializeField] private float radioVision = 8f;
    [Range(0, 360)]
    [SerializeField] private float anguloVision = 90f;
    [SerializeField] private LayerMask capaJugador;
    [SerializeField] private LayerMask capaObstaculos;

    [Header("Sigilo del jugador")]
    [SerializeField] private float factorReduccionAgachado = 0.5f; // 0.5 = la mitad de radio si el jugador está agachado

    [Header("Sospecha progresiva")]
    [SerializeField] private float velocidadAumentoSospecha = 40f; // puntos por segundo viendo al jugador
    [SerializeField] private float velocidadDisminucionSospecha = 20f; // puntos por segundo sin verlo
    [SerializeField] private float umbralSospechaTotal = 100f;

    [Header("Referencia")]
    [SerializeField] private Transform ojoEnemigo;

    // JugadorVisible = línea de visión directa AHORA MISMO (instantáneo)
    public bool JugadorVisible { get; private set; }

    // NivelSospecha = 0 (tranquilo) a 100 (te ha detectado del todo). Progresivo.
    public float NivelSospecha { get; private set; }
    public bool SospechaCompleta => NivelSospecha >= umbralSospechaTotal;

    public Vector3 UltimaPosicionJugador { get; private set; }

    // Referencia directa al transform del jugador, por si otro script (ej. captura) la necesita
    public Transform JugadorTransform => jugador;

    private Transform jugador;
    private PlayerHidingState estadoEsconditeJugador;
    private PlayerController controladorJugador;

    private void Start()
    {
        if (ojoEnemigo == null) ojoEnemigo = transform;

        GameObject jugadorObj = GameObject.FindGameObjectWithTag("Player");
        if (jugadorObj != null)
        {
            jugador = jugadorObj.transform;
            estadoEsconditeJugador = jugadorObj.GetComponent<PlayerHidingState>();
            controladorJugador = jugadorObj.GetComponent<PlayerController>();
        }
    }

    private void Update()
    {
        ComprobarVision();
        ActualizarNivelSospecha();
    }

    private void ComprobarVision()
    {
        JugadorVisible = false;

        if (jugador == null) return;

        // Si el jugador está en una zona de escondite, es invisible aunque esté dentro del cono y sin obstáculos
        if (estadoEsconditeJugador != null && estadoEsconditeJugador.EstaEscondido) return;

        // Si el jugador está agachado, el enemigo necesita estar más cerca para detectarlo
        float radioEfectivo = radioVision;
        if (controladorJugador != null && controladorJugador.EstaAgachado)
        {
            radioEfectivo *= factorReduccionAgachado;
        }

        Vector3 direccionAlJugador = (jugador.position - ojoEnemigo.position);
        float distancia = direccionAlJugador.magnitude;

        if (distancia > radioEfectivo) return;

        float anguloEntre = Vector3.Angle(ojoEnemigo.forward, direccionAlJugador);
        if (anguloEntre > anguloVision / 2f) return;

        if (Physics.Raycast(ojoEnemigo.position, direccionAlJugador.normalized, out RaycastHit hit, radioEfectivo, capaObstaculos | capaJugador))
        {
            if (((1 << hit.collider.gameObject.layer) & capaJugador) != 0)
            {
                JugadorVisible = true;
                UltimaPosicionJugador = jugador.position;
            }
        }
    }

    private void ActualizarNivelSospecha()
    {
        if (JugadorVisible)
        {
            NivelSospecha += velocidadAumentoSospecha * Time.deltaTime;
        }
        else
        {
            NivelSospecha -= velocidadDisminucionSospecha * Time.deltaTime;
        }

        NivelSospecha = Mathf.Clamp(NivelSospecha, 0f, umbralSospechaTotal);
    }

    // Llamado desde fuera (ej. un script de "Ruido") cuando el enemigo oye algo sin verlo directamente.
    // Sube la sospecha y marca esa posición como la última conocida, para que el enemigo vaya a investigar.
    public void RegistrarRuido(Vector3 posicionRuido, float cantidadSospecha)
    {
        NivelSospecha = Mathf.Clamp(NivelSospecha + cantidadSospecha, 0f, umbralSospechaTotal);
        UltimaPosicionJugador = posicionRuido;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origen = ojoEnemigo != null ? ojoEnemigo.position : transform.position;
        Vector3 forward = ojoEnemigo != null ? ojoEnemigo.forward : transform.forward;

        // Color del gizmo según nivel de sospecha: amarillo (tranquilo) -> rojo (sospecha total)
        Gizmos.color = Color.Lerp(Color.yellow, Color.red, NivelSospecha / umbralSospechaTotal);
        Gizmos.DrawWireSphere(origen, radioVision);

        Vector3 limiteIzq = Quaternion.Euler(0, -anguloVision / 2f, 0) * forward * radioVision;
        Vector3 limiteDer = Quaternion.Euler(0, anguloVision / 2f, 0) * forward * radioVision;

        Gizmos.DrawLine(origen, origen + limiteIzq);
        Gizmos.DrawLine(origen, origen + limiteDer);
    }
}