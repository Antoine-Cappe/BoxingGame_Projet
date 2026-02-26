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

        // 1. On change la visée UNE SEULE FOIS à la moitié du cycle (0.5s)
        if (_actionTimer >= 0.5f && !_inputTriggered) 
        {
            TakeRandomInput();
            _inputTriggered = true; // On verrouille
        }

        // 2. On déclenche l'action et on reset à 1.0s
        if (_actionTimer >= 1f) 
        {
            _actionTimer = 0f;
            _inputTriggered = false; // On déverrouille pour le prochain cycle

            if (Random.value < 0.5f)
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
        int punchAction = Random.Range(1, 3); // 0 = rien, 1 = gauche, 2 = droite
        if (punchAction == 1) _combat.PunchLeft();
        if (punchAction == 2) _combat.PunchRight();
    }

    void TakeRandomInput()
    {
        _mvmt.aimInputLeft = new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(-0.25f, 1f));
        _mvmt.aimInputRight = new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(-0.25f, 1f));
    }

    void TakeRandomDodge()
    {
        int dodgeAction = Random.Range(1, 3); // 0 = rien, 1 = gauche, 2 = droite
        if (dodgeAction == 1) _mvmt.TriggerDodge(1f);
        if (dodgeAction == 2) _mvmt.TriggerDodge(-1f); 
    }
}