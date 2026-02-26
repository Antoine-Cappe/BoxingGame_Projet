using UnityEngine;

public class PunchDetector : MonoBehaviour
{
    [Header("Configuration")]
    public BoxerCombat owner; 
    public bool isLeftGlove; 

    private void OnTriggerEnter(Collider other)
    {
        // 1. Détection du BLOCAGE (Gant contre Gant)
        if (other.CompareTag("Glove") && !other.transform.IsChildOf(owner.transform)) {
            owner.HandleImpact(isLeftGlove, true, null);
            return;
        }

        // 2. Détection du COUP RÉUSSI (Corps de l'adversaire)
        if (other.CompareTag("Opponent") || other.CompareTag("Player"))
        {
            if (!other.transform.IsChildOf(owner.transform)) 
            {
                // --- RÉCOMPENSE POUR CELUI QUI FRAPPE ---
                BoxerAgent myAgent = owner.GetComponent<BoxerAgent>();
                if (myAgent != null) {
                    myAgent.AddReward(0.1f); 
                }

                BoxerCombat adversaireCombat = other.GetComponentInParent<BoxerCombat>();
                if (adversaireCombat != null)
                {
                    owner.HandleImpact(isLeftGlove, false, adversaireCombat);
                }
            }
        }
    }
}