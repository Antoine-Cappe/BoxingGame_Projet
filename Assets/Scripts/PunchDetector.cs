using UnityEngine;

public class PunchDetector : MonoBehaviour
{
    public BoxerController owner;
    public bool isLeftGlove; 

    private void OnTriggerEnter(Collider other)
    {
        // 1. Détection du BLOCAGE (Gant contre Gant)
        if (other.CompareTag("Glove") && other.transform.root != transform.root)
        {
            owner.HandleImpact(isLeftGlove, wasBlocked: true, opponent: null);
            return;
        }

        // 2. Détection du COUP RÉUSSI (Corps)
        if (other.CompareTag("Opponent") || other.CompareTag("Player"))
        {
            if (other.gameObject != owner.gameObject)
            {
                BoxerController adversaire = other.GetComponent<BoxerController>();
                if (adversaire != null)
                {
                    // On envoie l'adversaire au contrôleur pour appliquer les dégâts
                    owner.HandleImpact(isLeftGlove, wasBlocked: false, opponent: adversaire);
                }
            }
        }
    }
}