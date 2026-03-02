using UnityEngine;

public class RandomAgent : MonoBehaviour
{
    public BoxerCombat _combat;
    public BoxerMovement _mvmt;

    [Header("Réglages Bot")]
    public float decisionFrequency = 0.2f; // Le bot change de cible toutes les 0.2s
    private float _timer = 0f;

    void Update()
    {
        _timer += Time.deltaTime;

        if (_timer >= decisionFrequency)
        {
            _timer = 0f;

            // 1. DÉCISION DE VISÉE (Mouvement continu des gants)
            // On lui donne 40% de chance de rester immobile (0), 30% d'aller à gauche, 30% à droite
            //_mvmt.aimInputLeft = new Vector2(GetWeightedDirection(), GetWeightedDirection());
            //_mvmt.aimInputRight = new Vector2(GetWeightedDirection(), GetWeightedDirection());

            // 2. DÉCISION D'ACTION (Punch ou Esquive)
            float dice = Random.value;
            if (dice < 0.15f) _combat.PunchLeft();
            else if (dice < 0.30f) _combat.PunchRight();
            else if (dice < 0.40f) _mvmt.TriggerDodge(Random.value > 0.5f ? 1f : -1f);
        }
    }

    // Fonction pour que le bot ne soit pas TOUJOURS en train de bouger
    float GetWeightedDirection()
    {
        float r = Random.value;
        if (r < 0.4f) return 0f;      // 40% de chance de s'arrêter
        if (r < 0.7f) return 1f;      // 30% vers le haut/droite
        return -1f;                   // 30% vers le bas/gauche
    }
}