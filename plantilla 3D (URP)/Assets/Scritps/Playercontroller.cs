using UnityEngine;

// Requiere un CharacterController en el mismo GameObject.
// Movimiento simple con WASD, sin salto, pensado como base
// para el prototipo de sigilo.
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float velocidad = 4f;
    [SerializeField] private float velocidadCorriendo = 7f;
    [SerializeField] private float velocidadAgachado = 2f;
    [SerializeField] private float velocidadRotacion = 10f;
    [SerializeField] private float gravedad = -9.81f;

    [Header("Agacharse")]
    [SerializeField] private Transform modeloVisual; // el objeto HIJO que contiene la malla (no el que tiene el CharacterController)
    [SerializeField] private float escalaAlturaAgachado = 0.5f; // 0.5 = la mitad de alto visualmente
    [SerializeField] private float alturaOriginalModelo = 2f; // la altura (en unidades locales) del modelo de pie, para recolocar los pies en el suelo al escalar
    [SerializeField] private float velocidadTransicionAltura = 8f;

    // La IA (EnemyFieldOfView) lee esto para reducir su radio de detección
    public bool EstaAgachado { get; private set; }

    private CharacterController controller;
    private PlayerNoiseEmitter emisorRuido; // opcional: si no está el componente, simplemente no emite ruido
    private Vector3 velocidadVertical;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        emisorRuido = GetComponent<PlayerNoiseEmitter>();
    }

    private void Update()
    {
        // Input horizontal (A/D) y vertical (W/S)
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // Mantener pulsada la tecla C para agacharse; al soltarla, se levanta.
        // Usamos "C" (letra normal) en vez de Ctrl/Shift porque las teclas modificadoras
        // son las que más sufren el bug de "se queda pillada" si la ventana pierde el foco.
        EstaAgachado = Input.GetKey(KeyCode.C);

        bool corriendo = !EstaAgachado && Input.GetKey(KeyCode.LeftShift) && (Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f);
        if (emisorRuido != null) emisorRuido.EstaCorriendo = corriendo;

        float velocidadActual = EstaAgachado ? velocidadAgachado : (corriendo ? velocidadCorriendo : velocidad);

        // Escalamos SOLO el modelo visual (no el CharacterController, que se queda siempre igual para evitar bugs de colisión).
        // El collider de colisión no cambia de tamaño; solo cambia cómo se ve el personaje y cuánto detectan los enemigos.
        if (modeloVisual != null)
        {
            float escalaObjetivoY = EstaAgachado ? escalaAlturaAgachado : 1f;
            Vector3 escalaActual = modeloVisual.localScale;
            escalaActual.y = Mathf.Lerp(escalaActual.y, escalaObjetivoY, velocidadTransicionAltura * Time.deltaTime);
            modeloVisual.localScale = escalaActual;

            // Unity escala la malla desde su centro, así que al encoger en Y el modelo
            // "flota" (los pies se despegan del suelo). Lo compensamos bajando el modelo
            // la mitad de lo que se ha encogido, para que los pies sigan tocando el suelo.
            Vector3 posicionActual = modeloVisual.localPosition;
            posicionActual.y = (escalaActual.y - 1f) * (alturaOriginalModelo / 2f);
            modeloVisual.localPosition = posicionActual;
        }

        Vector3 direccion = new Vector3(h, 0f, v).normalized;

        if (direccion.magnitude >= 0.1f)
        {
            // Rotar al jugador hacia la dirección de movimiento
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

        // Gravedad simple para que el CharacterController se mantenga pegado al suelo
        if (controller.isGrounded && velocidadVertical.y < 0)
        {
            velocidadVertical.y = -2f;
        }
        velocidadVertical.y += gravedad * Time.deltaTime;
        controller.Move(velocidadVertical * Time.deltaTime);
    }

    // Red de seguridad: si la ventana pierde el foco, evitamos que algún estado se quede "pillado"
    private void OnApplicationFocus(bool tieneFoco)
    {
        if (!tieneFoco && emisorRuido != null)
        {
            emisorRuido.EstaCorriendo = false;
        }
    }
}
