using UnityEngine;
using UnityEngine.SceneManagement;

// Único en la escena. Otros scripts (Objetivo, EnemyCaptura) le avisan cuando el jugador gana o pierde.
public class GameManager : MonoBehaviour
{
    public static GameManager Instancia { get; private set; }

    public enum EstadoPartida { Jugando, Ganada, Perdida }
    public EstadoPartida EstadoActual { get; private set; } = EstadoPartida.Jugando;

    private void Awake()
    {
        // Patrón Singleton simple: solo puede haber un GameManager
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        Instancia = this;
    }

    public void Ganar()
    {
        if (EstadoActual != EstadoPartida.Jugando) return; // evita llamadas dobles

        EstadoActual = EstadoPartida.Ganada;
        Debug.Log("¡NIVEL COMPLETADO! El jugador ha llegado al objetivo sin ser detectado.");
        Time.timeScale = 0f; // pausa el juego; en el siguiente paso añadimos UI real
    }

    public void Perder()
    {
        if (EstadoActual != EstadoPartida.Jugando) return;

        EstadoActual = EstadoPartida.Perdida;
        Debug.Log("TE HAN ATRAPADO. Pulsa R para reiniciar.");
        Time.timeScale = 0f;
    }

    private void Update()
    {
        if (EstadoActual != EstadoPartida.Jugando && Input.GetKeyDown(KeyCode.R))
        {
            ReiniciarNivel();
        }
    }

    public void ReiniciarNivel()
    {
        Time.timeScale = 1f; // importante: restaurar el tiempo antes de recargar la escena
        Scene escenaActual = SceneManager.GetActiveScene();
        SceneManager.LoadScene(escenaActual.name);
    }
}
