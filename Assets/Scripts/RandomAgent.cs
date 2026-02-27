using UnityEngine;

public class RandomAgent : MonoBehaviour
{
    public BoxerCombat _combat;
    public BoxerMovement _mvmt;
    private float _actionTimer = 0f;

    private bool _inputTriggered = false;

    void Update()
    {
        _actionTimer += Time.deltaTime;

        // 1. MISE À JOUR DE LA VISÉE (Toutes les 0.5s)
        if (_actionTimer >= 0.5f && !_inputTriggered) 
        {
            TakeRandomInput();
            _inputTriggered = true; // On verrouille jusqu'à la fin du cycle d'une seconde
        }

        // 2. DÉCISION D'ACTION (Toutes les 1s)
        if (_actionTimer >= 1f) 
        {
            _actionTimer = 0f;
            _inputTriggered = false; // On déverrouille pour le prochain cycle

            if (Random.value < 0.7f)
            {
                TakeRandomAction(); 
            }
            else {
                TakeRandomDodge();
            }
        }
    }

    void TakeRandomAction()
    {
        // 0 = rien, 1 = gauche, 2 = droite (Range int est exclusif pour le max)
        int punchAction = Random.Range(0, 3); 
        if (punchAction == 1) _combat.PunchLeft();
        if (punchAction == 2) _combat.PunchRight();
    }

    void TakeRandomInput()
    {
        // On utilise maintenant des valeurs discrètes pour correspondre au nouveau système
        // Chaque axe peut être : -1 (Gauche/Bas), 0 (Fixe), ou 1 (Droite/Haut)
        _mvmt.aimInputLeft = new Vector2(GetRandomDiscrete(), GetRandomDiscrete());
        _mvmt.aimInputRight = new Vector2(GetRandomDiscrete(), GetRandomDiscrete());
    }

    // Fonction utilitaire pour générer -1, 0 ou 1
    float GetRandomDiscrete()
    {
        int r = Random.Range(0, 3); // Retourne 0, 1 ou 2
        if (r == 1) return 1f;
        if (r == 2) return -1f;
        return 0f; // r == 0
    }

    void TakeRandomDodge()
    {
        int dodgeAction = Random.Range(0, 3); 
        if (dodgeAction == 1) _mvmt.TriggerDodge(1f);
        if (dodgeAction == 2) _mvmt.TriggerDodge(-1f); 
    }
}