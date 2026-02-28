using UnityEngine;

public class PunchDetector : MonoBehaviour
{
    public BoxerCombat owner;
    public bool isLeftGlove;
    public bool isHeadCollider = true;

    private void Awake()
    {
        if (owner == null) owner = GetComponentInParent<BoxerCombat>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (owner == null || other.transform.IsChildOf(owner.transform)) return;

        // 1. BLOCAGE GANT CONTRE GANT
        if (other.CompareTag("Glove"))
        {
            owner.HandleImpact(isLeftGlove, true, null);
            return;
        }

        // 2. COUP SUR L'ADVERSAIRE
        if (other.CompareTag("Opponent") || other.CompareTag("Player"))
        {
            BoxerHealth targetHealth = other.GetComponentInParent<BoxerHealth>();
            BoxerMovement targetMvmt = other.GetComponentInParent<BoxerMovement>();
            BoxerAgent myAgent = owner.GetComponent<BoxerAgent>();

            if (targetHealth == null || targetHealth.IsKO) return;

            // ── ESQUIVE : coup dans le vide ──────────────────────
            if (targetMvmt != null && targetMvmt.isDodging)
            {
                // Légère pénalité : on a frappé dans le vide
                if (myAgent != null) myAgent.AddReward(-0.05f);
                return;
            }

            // ── VÉRIFICATION GARDE ───────────────────────────────
            if (targetMvmt != null && IsBlockedByGuard(targetMvmt.currentGuard))
            {
                // Coup BLOQUÉ par la garde
                // → Pénalité pour l'attaquant (coup inutile)
                if (myAgent != null) myAgent.AddReward(-0.05f);

                // → Récompense pour le défenseur (garde efficace !)
                BoxerAgent defenderAgent = targetHealth.GetComponent<BoxerAgent>();
                if (defenderAgent != null) defenderAgent.AddReward(0.15f);

                owner.HandleImpact(isLeftGlove, true, null);
                return;
            }

            // ── COUP RÉUSSI SUR ZONE NON GARDÉE ─────────────────
            float reward = 0.1f; // récompense de base

            if (targetMvmt != null)
            {
                GuardType oppGuard = targetMvmt.currentGuard;

                // Bonus si on exploite intelligemment la garde adverse
                // Ex: l'adversaire est en garde haute → on frappe le corps
                if (oppGuard == GuardType.High && !isHeadCollider)
                {
                    reward = 0.2f; // +100% bonus : exploit garde haute → corps
                }
                // Ex: l'adversaire est en garde basse → on frappe la tête
                else if (oppGuard == GuardType.Low && isHeadCollider)
                {
                    reward = 0.2f; // +100% bonus : exploit garde basse → tête
                }
                // Aucune garde → coup normal
                else if (oppGuard == GuardType.None)
                {
                    reward = 0.1f;
                }
            }

            if (myAgent != null) myAgent.AddReward(reward);

            // Dégâts et feedback
            Vector3 hitDir = (other.transform.position - transform.position).normalized;
            targetHealth.TakeDamage(hitDir);

            BoxerCombat targetCombat = other.GetComponentInParent<BoxerCombat>();
            owner.HandleImpact(isLeftGlove, false, targetCombat);
        }
    }

    private bool IsBlockedByGuard(GuardType guard)
    {
        if (guard == GuardType.High && isHeadCollider) return true;
        if (guard == GuardType.Low && !isHeadCollider) return true;
        return false;
    }
}