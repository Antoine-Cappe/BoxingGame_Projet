using UnityEngine;

public class PunchManager : MonoBehaviour
{
    public AudioSource maSource;
    public AudioClip monFichierAudio;
    public float dureeUnCoup = 0.5f;

   
    public void JouerSonImpact()


    {
        if (monFichierAudio == null || maSource == null) return;

        float[] timeCode = new float[] { 0.3f, 1.1f, 1.8f, 2.5f, 3.2f, 3.7f };
        float momentAuHasard = timeCode[Random.Range(0, timeCode.Length)];

        maSource.clip = monFichierAudio;
        maSource.time = momentAuHasard;
        maSource.Play();

        Invoke("CouperLeSon", dureeUnCoup);
    }

    void CouperLeSon()
    {
        maSource.Stop();
    }
}