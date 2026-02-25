using UnityEngine;

public class PunchDetector : MonoBehaviour
{
    [Header("Configuration")]
    public BoxerCombat owner; 
    public bool isLeftGlove; 

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Glove") && !other.transform.IsChildOf(owner.transform)) {
            owner.HandleImpact(isLeftGlove, true, null);
            return;
        }
        // 2. Détection du COUP RÉUSSI (Corps)
        if (other.CompareTag("Opponent") || other.CompareTag("Player"))
        {
            if (!other.transform.IsChildOf(owner.transform)) {
                // On cherche BoxerCombat sur l'adversaire
                BoxerCombat adversaire = other.GetComponentInParent<BoxerCombat>();

                // Dans PunchDetector.cs, là où le coup est réussi :
                BoxerAgent myAgent = owner.GetComponent<BoxerAgent>();
                if (myAgent != null) {
                    myAgent.AddReward(0.1f); // Récompense pour avoir touché
                }
                
                if (adversaire != null)
                {
                    owner.HandleImpact(isLeftGlove, false, adversaire);
                }
            }
        }
    }
}