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
        // Sécurité de base : on ignore si l'owner est absent ou si on se touche soi-même
        if (owner == null || other.transform.IsChildOf(owner.transform)) return;

        // --- SÉCURITÉ DE PUNCH ---
        // On ne détecte absolument rien si le gant n'est pas dans une phase d'attaque active
        bool isActuallyPunching = isLeftGlove ? owner.isPunchingLeft : owner.isPunchingRight;
        if (!isActuallyPunching) return;
        
        // 1. BLOCAGE (Collision entre deux gants)
        if (other.CompareTag("Glove")) {
            owner.HandleImpact(isLeftGlove, true, null);
            return;
        }

        // 2. COUP RÉUSSI (Uniquement sur la "Hurtbox")
        // On ignore les tags "Opponent" ou "Player" pour ne pas frapper le gros collider de distance
        if (other.CompareTag("Hurtbox"))
        {
            // Comme le script de santé est sur le parent (le root de l'agent), on utilise GetComponentInParent
            BoxerHealth targetHealth = other.GetComponentInParent<BoxerHealth>();
            
            // On vérifie que la cible existe et n'est pas déjà KO
            if (targetHealth == null || targetHealth.IsKO) return;

            // --- VÉRIFICATION ESQUIVE ---
            // On vérifie sur le parent si l'adversaire est en train d'esquiver
            BoxerMovement targetMvmt = other.GetComponentInParent<BoxerMovement>();
            if (targetMvmt != null && targetMvmt.isDodging) return; 

            // --- RÉCOMPENSE ---
            // On donne une récompense à l'IA pour avoir touché la zone précise de la Hurtbox
            BoxerAgent myAgent = owner.GetComponent<BoxerAgent>();
            if (myAgent != null) myAgent.AddReward(0.2f); 

            // --- DÉGÂTS ---
            Vector3 hitDir = (other.transform.position - transform.position).normalized;
            targetHealth.TakeDamage(hitDir);

            // --- IMPACT VISUEL ET RETOUR DU BRAS ---
            BoxerCombat targetCombat = other.GetComponentInParent<BoxerCombat>();
            owner.HandleImpact(isLeftGlove, false, targetCombat);
        }
    }
}