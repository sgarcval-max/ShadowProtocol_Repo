using UnityEngine;

// Requiere un CharacterController en el mismo GameObject.
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float velocidad = 4f;
    [SerializeField] private float velocidadCorriendo = 7f;
    [SerializeField] private float velocidadAgachado = 2f;
    [SerializeField] private float velocidadRotacion = 10f;
    [SerializeField] private float gravedad = -9.81f;

    [Header("Animator")]
    // CAMBIA estos nombres por los de la pestaña Parameters de tu Animator Controller
    [SerializeField] private string parametroVelocidad = "Speed";
    [SerializeField] private string parametroAgachado = "IsCrouching";
    // Si tu Animator NO tiene este parámetro, deja el campo vacío en el Inspector
    [SerializeField] private string parametroMotionSpeed = "MotionSpeed";

    [Header("Agacharse")]
    [SerializeField] private Transform modeloVisual; // el objeto HIJO que contiene la malla
    [SerializeField] private float escalaAlturaAgachado = 0.5f;
    [SerializeField] private float alturaOriginalModelo = 2f;
    [SerializeField] private float velocidadTransicionAltura = 8f;

    [Header("Audio de Pasos")]
    [SerializeField] private AudioSource audioSourcePasos;
    [SerializeField] private AudioClip[] sonidosPasos; // Arrastra aquí tus 5 o 6 clips de audio
    [SerializeField] private float intervaloPasosCaminar = 0.5f;   // Segundos entre pasos al caminar
    [SerializeField] private float intervaloPasosCorrer = 0.32f;  // Segundos entre pasos al correr
    [SerializeField] private float intervaloPasosAgachado = 0.65f;// Segundos entre pasos agachado
    [Range(0f, 1f)]
    [SerializeField] private float volumenPasos = 0.7f;
    [SerializeField] private float variacionPitch = 0.1f; // Variación ligera de tono para mayor naturalidad

    [Tooltip("Marca esto si vas a usar Animation Events en los FBX/Clips de animación en lugar del temporizador por código.")]
    [SerializeField] private bool usarEventosDeAnimacion = false;

    // La IA (EnemyFieldOfView) lee esto para reducir su radio de detección
    public bool EstaAgachado { get; private set; }

    private CharacterController controller;
    private PlayerNoiseEmitter emisorRuido;
    private Animator animator;
    private Vector3 velocidadVertical;
    private float temporizadorPasos = 0f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        emisorRuido = GetComponent<PlayerNoiseEmitter>();
        animator = GetComponentInChildren<Animator>();

        if (audioSourcePasos == null)
        {
            audioSourcePasos = GetComponent<AudioSource>();
        }
    }

    private void Update()
    {
        // Input horizontal (A/D) y vertical (W/S)
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        bool seMueve = Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f;

        EstaAgachado = Input.GetKey(KeyCode.C);

        bool corriendo = !EstaAgachado && Input.GetKey(KeyCode.LeftShift) && seMueve;
        if (emisorRuido != null) emisorRuido.EstaCorriendo = corriendo;

        float velocidadActual = EstaAgachado ? velocidadAgachado : (corriendo ? velocidadCorriendo : velocidad);

        // Animaciones
        if (animator != null)
        {
            float velocidadAnim = seMueve ? velocidadActual : 0f;
            animator.SetFloat(parametroVelocidad, velocidadAnim, 0.1f, Time.deltaTime);
            animator.SetBool(parametroAgachado, EstaAgachado);

            if (!string.IsNullOrEmpty(parametroMotionSpeed))
            {
                animator.SetFloat(parametroMotionSpeed, 1f);
            }
        }

        // Escalar modelo visual al agacharse
        if (modeloVisual != null)
        {
            float escalaObjetivoY = EstaAgachado ? escalaAlturaAgachado : 1f;
            Vector3 escalaActual = modeloVisual.localScale;
            escalaActual.y = Mathf.Lerp(escalaActual.y, escalaObjetivoY, velocidadTransicionAltura * Time.deltaTime);
            modeloVisual.localScale = escalaActual;

            Vector3 posicionActual = modeloVisual.localPosition;
            posicionActual.y = (escalaActual.y - 1f) * (alturaOriginalModelo / 2f);
            modeloVisual.localPosition = posicionActual;
        }

        Vector3 direccion = new Vector3(h, 0f, v).normalized;

        if (direccion.magnitude >= 0.1f)
        {
            float anguloObjetivo = Mathf.Atan2(direccion.x, direccion.z) * Mathf.Rad2Deg;
            float anguloSuavizado = Mathf.LerpAngle(
                transform.eulerAngles.y,
                anguloObjetivo,
                velocidadRotacion * Time.deltaTime
            );
            transform.rotation = Quaternion.Euler(0f, anguloSuavizado, 0f);

            Vector3 movimiento = direccion * velocidadActual * Time.deltaTime;
            controller.Move(movimiento);
        }

        // Gravedad
        if (controller.isGrounded && velocidadVertical.y < 0)
        {
            velocidadVertical.y = -2f;
        }
        velocidadVertical.y += gravedad * Time.deltaTime;
        controller.Move(velocidadVertical * Time.deltaTime);

        // Control automático de sonido de pasos por temporizador
        if (!usarEventosDeAnimacion && controller.isGrounded && seMueve)
        {
            float intervaloActual = EstaAgachado ? intervaloPasosAgachado : (corriendo ? intervaloPasosCorrer : intervaloPasosCaminar);
            temporizadorPasos += Time.deltaTime;

            if (temporizadorPasos >= intervaloActual)
            {
                temporizadorPasos = 0f;
                ReproducirPasoAleatorio();
            }
        }
        else if (!seMueve)
        {
            temporizadorPasos = 0f;
        }
    }

    private void OnApplicationFocus(bool tieneFoco)
    {
        if (!tieneFoco && emisorRuido != null)
        {
            emisorRuido.EstaCorriendo = false;
        }
    }

    // Selecciona y reproduce uno de los sonidos de pasos al azar
    public void ReproducirPasoAleatorio()
    {
        if (audioSourcePasos == null || sonidosPasos == null || sonidosPasos.Length == 0) return;

        // Escoge un clip al azar del array
        AudioClip clip = sonidosPasos[Random.Range(0, sonidosPasos.Length)];

        // Aplica una pequeña variación de tono para dar variedad de audio
        audioSourcePasos.pitch = Random.Range(1f - variacionPitch, 1f + variacionPitch);
        audioSourcePasos.PlayOneShot(clip, volumenPasos);
    }

    // Método listo para ser llamado desde Animation Events en el Animator / FBX
    public void OnFootstep()
    {
        if (controller.isGrounded)
        {
            ReproducirPasoAleatorio();
        }
    }
}
