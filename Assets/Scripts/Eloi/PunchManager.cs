using UnityEngine;

public class PunchManager : MonoBehaviour
{
    public AudioSource maSource;
    public AudioClip monFichierAudio1;
    public AudioClip monFichierAudio2;
    public float dureeUnCoup = 0.5f;

    public void JouerSonImpact(int i)
    {
        // 1. Vérifications de sécurité habituelles
        if ((i == 1 && monFichierAudio1 == null) || (i == 2 && monFichierAudio2 == null) || maSource == null) return;

        // 2. CORRECTION CRUCIALE : On annule TOUTE demande de coupure précédente.
        // Cela empêche un "Stop" programmé il y a 0.4s de couper le nouveau son.
        CancelInvoke("CouperLeSon");

        // Paramètres pour le hasard (inutilisés pour le son 2 dans votre code actuel, mais présents)
        float[] timeCode = new float[] { 0.3f, 1.1f, 1.8f, 2.5f, 3.2f, 3.7f };
        float momentAuHasard = timeCode[Random.Range(0, timeCode.Length)];

        // Gestion du Son 1
        if (i == 1)
        {
            maSource.clip = monFichierAudio1;
            maSource.time = momentAuHasard; // Le hasard n'est utilisé que pour le son 1
            maSource.Play();
            Invoke("CouperLeSon", dureeUnCoup); // Programme la coupure
        }

        // Gestion du Son 2
        if (i == 2)
        {
            maSource.clip = monFichierAudio2;
            // Pour le son 2, vous n'utilisez pas momentAuHasard ni Invoke. 
            // Il jouera tout son long, sauf si JouerSonImpact est rappelé.
            maSource.Play();  
        }
    }

    void CouperLeSon()
    {
        // Si la source existe et joue encore, on l'arrête.
        if (maSource != null && maSource.isPlaying)
        {
            maSource.Stop();
        }
    }
}