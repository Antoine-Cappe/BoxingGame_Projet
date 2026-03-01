using UnityEngine;

public class PunchManager : MonoBehaviour
{
    public AudioSource maSource;
    public AudioClip monFichierAudio;
    public float dureeUnCoup = 0.5f;

    // Cette fonction peut maintenant être appelée par n'importe quel script
    public void JouerSonImpact()


    {
        if (monFichierAudio == null || maSource == null) return;

        float momentAuHasard = Random.Range(0f, monFichierAudio.length - dureeUnCoup);

        maSource.clip = monFichierAudio;
        maSource.time = momentAuHasard;
        maSource.Play();

        Invoke("CouperLeSon", dureeUnCoup);
        Debug.Log("Impact sonore déclenché !");
    }

    void CouperLeSon()
    {
        maSource.Stop();
    }
}