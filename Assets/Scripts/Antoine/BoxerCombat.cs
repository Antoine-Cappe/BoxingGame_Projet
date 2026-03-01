using UnityEngine;
using System.Collections;

public class BoxerCombat : MonoBehaviour
{
    [Header("Réglages de Vitesse")]
    public float punchSpeed = 15f;      
    public float returnMultiplier = 1.2f; 
    public float cooldown = 0.5f; 
    public float punchDistance = 1.8f;

    // isPunching = Est-ce que le gant est projeté ? (Bloque le mouvement de garde)
    public bool isPunchingLeft { get; private set; }
    public bool isPunchingRight { get; private set; }

    // _canPunch = Est-ce que le cooldown est fini ? (Autorise une nouvelle attaque)
    private bool _canPunchLeft = true;
    private bool _canPunchRight = true;

    private BoxerMovement _mvmt;
    private Coroutine _leftPunchCoroutine, _rightPunchCoroutine;

    void Awake() => _mvmt = GetComponent<BoxerMovement>();

    public void PunchLeft() {
        // On vérifie si on PEUT frapper, pas si on est en mouvement
        if (_canPunchLeft && !isPunchingRight && !_mvmt.isDodging) {
            isPunchingLeft = true;
            _canPunchLeft = false; // Début du cooldown
            _leftPunchCoroutine = StartCoroutine(PunchRoutine(_mvmt.leftGlove, _mvmt.GetLeftHomePos(), true));
        }
    }

    public void PunchRight() {
        if (_canPunchRight && !isPunchingLeft && !_mvmt.isDodging) {
            isPunchingRight = true;
            _canPunchRight = false; // Début du cooldown
            _rightPunchCoroutine = StartCoroutine(PunchRoutine(_mvmt.rightGlove, _mvmt.GetRightHomePos(), false));
        }
    }

    public void HandleImpact(bool isLeft, bool wasBlocked, BoxerCombat opponent) {
        if (isLeft) {
            if (_leftPunchCoroutine != null) StopCoroutine(_leftPunchCoroutine);
        } else {
            if (_rightPunchCoroutine != null) StopCoroutine(_rightPunchCoroutine);
        }

        StartCoroutine(ImpactSequence(isLeft, wasBlocked, opponent));
    }

    IEnumerator PunchRoutine(Transform glove, Vector3 home, bool isLeft) {
        Vector3 target = glove.localPosition + Vector3.forward * punchDistance;

        while (Vector3.Distance(glove.localPosition, target) > 0.01f) {
            glove.localPosition = Vector3.MoveTowards(glove.localPosition, target, punchSpeed * Time.deltaTime);
            yield return null;
        }

        yield return ReturnRoutine(glove, home, isLeft);
    }

    IEnumerator ReturnRoutine(Transform glove, Vector3 home, bool isLeft) {
        float speed = punchSpeed * returnMultiplier;

        // Retour vers la garde actuelle (dynamique)
        while (true) {
            Vector3 targetGuard = isLeft ? _mvmt.GetCurrentLeftGuard() : _mvmt.GetCurrentRightGuard();
            float dist = Vector3.Distance(glove.localPosition, targetGuard);
            
            if (dist < 0.01f) break;

            glove.localPosition = Vector3.MoveTowards(glove.localPosition, targetGuard, speed * Time.deltaTime);
            yield return null;
        }

        // --- LIBÉRATION DU MOUVEMENT ---
        // Le gant est revenu : on autorise BoxerMovement à bouger la garde
        if (isLeft) isPunchingLeft = false; else isPunchingRight = false;

        // --- ATTENTE DU COOLDOWN ---
        // Le mouvement est libre, mais on ne peut pas encore cliquer/ordonner un nouveau punch
        yield return new WaitForSeconds(cooldown);
        
        if (isLeft) _canPunchLeft = true; else _canPunchRight = true;
    }

    IEnumerator ImpactSequence(bool isLeft, bool wasBlocked, BoxerCombat opponent) {
        if (!wasBlocked && opponent != null) {
            BoxerHealth opponentHealth = opponent.GetComponent<BoxerHealth>();

            if (opponentHealth != null && !opponentHealth.IsKO) {
                yield return new WaitForSeconds(0.05f);

                BoxerVisuals opponentVisuals = opponent.GetComponent<BoxerVisuals>();
                if (opponentVisuals != null && opponentVisuals.enabled) {
                    PunchManager pm = GetComponent<PunchManager>();
                    if (pm != null) pm.JouerSonImpact();
                    Vector3 punchDir = (opponent.transform.position - transform.position).normalized;
                    opponentVisuals.TriggerWobble(punchDir);
                }
            }
        }

        Transform glove = isLeft ? _mvmt.leftGlove : _mvmt.rightGlove;
        Vector3 home = isLeft ? _mvmt.GetLeftHomePos() : _mvmt.GetRightHomePos();
        yield return ReturnRoutine(glove, home, isLeft);
    }

    public void ResetCombatState()
    {
        StopAllCoroutines();
        isPunchingLeft = false;
        isPunchingRight = false;
        _canPunchLeft = true;
        _canPunchRight = true;
    }
}