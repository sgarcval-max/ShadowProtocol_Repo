using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyFieldOfView))]
public class EnemyAI : MonoBehaviour
{
    public enum Estado { Patrulla, Alerta, Persecucion, Busqueda }

    [Header("Estado actual (solo lectura)")]
    [SerializeField] private Estado estadoActual = Estado.Patrulla;

    // Propiedad pública de solo lectura para que otros scripts (feedback visual, UI, etc.) sepan el estado actual
    public Estado EstadoActual => estadoActual;

    [Header("Patrulla")]
    [SerializeField] private Transform[] puntosPatrulla;
    [SerializeField] private float velocidadPatrulla = 2f;
    [SerializeField] private float esperaEnPunto = 1.5f;

    [Header("Persecución")]
    [SerializeField] private float velocidadPersecucion = 4.5f;
    [SerializeField] private float distanciaCaptura = 1.2f; // si se acerca más que esto al jugador en persecución, te atrapa

    [Header("Búsqueda")]
    [SerializeField] private float duracionBusqueda = 4f; // tiempo mirando alrededor tras perder al jugador
    [SerializeField] private float velocidadGiroBusqueda = 90f; // grados por segundo al mirar alrededor

    [Header("Aviso entre compañeros")]
    [SerializeField] private float radioAvisoCompaneros = 15f; // a qué distancia se oye la alarma de otro enemigo
    [SerializeField] private LayerMask capaEnemigos; // qué capa usan los objetos "Enemigo" para encontrarse entre ellos
    private bool yaAviseEstaPersecucion = false; // evita spamear el aviso cada frame mientras persigue

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sonidoPersecucion;

    [Header("Animator")]
    // CAMBIA estos nombres por los de la pestaña Parameters del Animator Controller de este enemigo
    [SerializeField] private string parametroVelocidad = "Speed";
    // Si tu Animator NO tiene este parámetro, deja el campo vacío en el Inspector
    [SerializeField] private string parametroMotionSpeed = "MotionSpeed";

    private NavMeshAgent agent;
    private EnemyFieldOfView fov;
    private Animator animator;

    private int indicePuntoActual = 0;
    private float temporizadorEspera = 0f;
    private float temporizadorEstado = 0f; // temporizador genérico reutilizado en Alerta y Búsqueda
    private Vector3 posicionDondeSeVioAlJugador;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        fov = GetComponent<EnemyFieldOfView>();
        animator = GetComponentInChildren<Animator>();

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void Start()
    {
        CambiarEstado(Estado.Patrulla);
    }

    private void Update()
    {
        // Transición global: en cuanto hay ALGO de sospecha (por vista o por ruido) y estamos tranquilos (Patrulla/Busqueda), empezamos a reaccionar en Alerta.
        bool estaReaccionando = estadoActual == Estado.Alerta || estadoActual == Estado.Persecucion;
        if (fov.NivelSospecha > 0f && !estaReaccionando)
        {
            CambiarEstado(Estado.Alerta);
        }

        switch (estadoActual)
        {
            case Estado.Patrulla:
                ActualizarPatrulla();
                break;
            case Estado.Alerta:
                ActualizarAlerta();
                break;
            case Estado.Persecucion:
                ActualizarPersecucion();
                break;
            case Estado.Busqueda:
                ActualizarBusqueda();
                break;
        }

        ActualizarAnimator();
    }

    // Le pasa al Animator la velocidad REAL a la que se está moviendo el agente (0 si está parado)
    private void ActualizarAnimator()
    {
        if (animator == null) return;

        float velocidadReal = agent.velocity.magnitude;
        animator.SetFloat(parametroVelocidad, velocidadReal, 0.1f, Time.deltaTime); // el 0.1f suaviza la transición

        // Multiplicador de velocidad de la animación: a 1 el ciclo avanza normal
        if (!string.IsNullOrEmpty(parametroMotionSpeed))
        {
            animator.SetFloat(parametroMotionSpeed, 1f);
        }
    }

    private void CambiarEstado(Estado nuevoEstado)
    {
        estadoActual = nuevoEstado;
        temporizadorEstado = 0f;

        switch (nuevoEstado)
        {
            case Estado.Patrulla:
                agent.speed = velocidadPatrulla;
                agent.isStopped = false;
                if (puntosPatrulla.Length > 0)
                    agent.SetDestination(puntosPatrulla[indicePuntoActual].position);
                break;

            case Estado.Alerta:
                // Se detiene en seco para "reaccionar" antes de perseguir
                agent.isStopped = true;
                posicionDondeSeVioAlJugador = fov.UltimaPosicionJugador;
                break;

            case Estado.Persecucion:
                agent.isStopped = false;
                agent.speed = velocidadPersecucion;
                if (!yaAviseEstaPersecucion)
                {
                    AvisarACompanerosCercanos();
                    yaAviseEstaPersecucion = true;

                    // Reproduce el SFX solo una vez al entrar en persecución
                    if (audioSource != null && sonidoPersecucion != null)
                    {
                        audioSource.PlayOneShot(sonidoPersecucion);
                    }
                }
                break;

            case Estado.Busqueda:
                agent.isStopped = false;
                agent.speed = velocidadPatrulla;
                agent.SetDestination(posicionDondeSeVioAlJugador);
                yaAviseEstaPersecucion = false; // si vuelve a detectar, podrá avisar y sonar de nuevo
                break;
        }
    }

    private void ActualizarPatrulla()
    {
        if (puntosPatrulla.Length == 0) return;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            temporizadorEspera += Time.deltaTime;
            if (temporizadorEspera >= esperaEnPunto)
            {
                temporizadorEspera = 0f;
                indicePuntoActual = (indicePuntoActual + 1) % puntosPatrulla.Length;
                agent.SetDestination(puntosPatrulla[indicePuntoActual].position);
            }
        }
    }

    private void ActualizarAlerta()
    {
        // Mientras dura la alerta, el enemigo se gira hacia donde sospecha que está el jugador
        if (fov.JugadorVisible)
        {
            posicionDondeSeVioAlJugador = fov.UltimaPosicionJugador;
        }

        Vector3 direccion = (posicionDondeSeVioAlJugador - transform.position);
        direccion.y = 0f;
        if (direccion.sqrMagnitude > 0.01f)
        {
            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccion);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rotacionObjetivo, 360f * Time.deltaTime);
        }

        // La sospecha ha llegado al máximo: confirmado, a perseguir de verdad
        if (fov.SospechaCompleta)
        {
            CambiarEstado(Estado.Persecucion);
            return;
        }

        // Falsa alarma: la sospecha ha vuelto a 0 antes de confirmar nada, volvemos a patrullar tranquilos
        if (fov.NivelSospecha <= 0f)
        {
            CambiarEstado(Estado.Patrulla);
        }
    }

    private void ActualizarPersecucion()
    {
        if (fov.JugadorVisible)
        {
            posicionDondeSeVioAlJugador = fov.UltimaPosicionJugador;
            agent.SetDestination(fov.UltimaPosicionJugador);

            // Comprobamos si ya está lo bastante cerca como para "atraparlo"
            if (fov.JugadorTransform != null)
            {
                float distanciaReal = Vector3.Distance(transform.position, fov.JugadorTransform.position);
                if (distanciaReal <= distanciaCaptura && GameManager.Instancia != null)
                {
                    GameManager.Instancia.Perder();
                }
            }
        }
        else
        {
            // Ha perdido de vista al jugador: pasamos a buscar en la última posición conocida
            CambiarEstado(Estado.Busqueda);
        }
    }

    private void ActualizarBusqueda()
    {
        // Si en mitad de la búsqueda vuelve a ver al jugador, la transición global ya lo manda a Alerta/Persecución.

        // Al llegar a la última posición conocida, empieza a girar sobre sí mismo "mirando alrededor"
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            transform.Rotate(0f, velocidadGiroBusqueda * Time.deltaTime, 0f);

            temporizadorEstado += Time.deltaTime;
            if (temporizadorEstado >= duracionBusqueda)
            {
                CambiarEstado(Estado.Patrulla);
            }
        }
    }

    // Busca otros EnemyAI cercanos y les pasa la posición del jugador, como si gritara "¡por aquí!"
    private void AvisarACompanerosCercanos()
    {
        Collider[] companerosCercanos = Physics.OverlapSphere(transform.position, radioAvisoCompaneros, capaEnemigos);

        foreach (Collider col in companerosCercanos)
        {
            EnemyAI otraIA = col.GetComponent<EnemyAI>();
            if (otraIA != null && otraIA != this)
            {
                otraIA.RecibirAvisoDeCompanero(posicionDondeSeVioAlJugador);
            }
        }
    }

    // Llamado por OTRO enemigo cuando él sí ha detectado al jugador del todo.
    // Si este enemigo está tranquilo patrullando, va a investigar la zona (Búsqueda).
    // Si ya está reaccionando por su cuenta (Alerta/Persecución), no le interrumpimos.
    public void RecibirAvisoDeCompanero(Vector3 posicionJugador)
    {
        if (estadoActual == Estado.Patrulla)
        {
            posicionDondeSeVioAlJugador = posicionJugador;
            CambiarEstado(Estado.Busqueda);
        }
    }
}
