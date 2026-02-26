using UnityEngine;

public class PunchDetector : MonoBehaviour
{
    public BoxerCombat owner; 
    public bool isLeftGlove; 

    private void Awake() {
        if (owner == null) owner = GetComponentInParent<BoxerCombat>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (owner == null || other.transform.IsChildOf(owner.transform)) return;

        // 1. BLOCAGE
        if (other.CompareTag("Glove")) {
            owner.HandleImpact(isLeftGlove, true, null);
            return;
        }

        // 2. COUP RÉUSSI
        if (other.CompareTag("Opponent") || other.CompareTag("Player"))
        {
            BoxerHealth targetHealth = other.GetComponentInParent<BoxerHealth>();
            // SÉCURITÉ : On ne frappe pas un mort ou un objet sans santé
            if (targetHealth == null || targetHealth.IsKO) return;

            // --- VÉRIFICATION ESQUIVE ---
            BoxerMovement targetMvmt = other.GetComponentInParent<BoxerMovement>();
            if (targetMvmt != null && targetMvmt.isDodging) return; // L'esquive protège !

            // --- RÉCOMPENSE ---
            BoxerAgent myAgent = owner.GetComponent<BoxerAgent>();
            if (myAgent != null) myAgent.AddReward(0.1f); 

            // --- DÉGÂTS IMMÉDIATS ---
            Vector3 hitDir = (other.transform.position - transform.position).normalized;
            targetHealth.TakeDamage(hitDir);

            // --- IMPACT VISUEL ---
            BoxerCombat targetCombat = other.GetComponentInParent<BoxerCombat>();
            owner.HandleImpact(isLeftGlove, false, targetCombat);
        }
    }
}