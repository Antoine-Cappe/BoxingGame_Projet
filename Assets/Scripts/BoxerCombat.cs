using UnityEngine;
using System.Collections;

public class BoxerCombat : MonoBehaviour
{
    [Header("Réglages de Vitesse")]
    public float punchSpeed = 15f;      
    public float returnMultiplier = 1.2f; 
    public float cooldown = 0.5f; 
    public float punchDistance = 1.8f;

    public bool isPunchingLeft { get; private set; }
    public bool isPunchingRight { get; private set; }

    private BoxerMovement _mvmt;
    private Coroutine _leftPunchCoroutine, _rightPunchCoroutine;

    void Awake() => _mvmt = GetComponent<BoxerMovement>();

    public void PunchLeft() {
        if (!isPunchingLeft && !isPunchingRight && !_mvmt.isDodging) {
            isPunchingLeft = true;
            _leftPunchCoroutine = StartCoroutine(PunchRoutine(_mvmt.leftGlove, _mvmt.GetLeftHomePos(), true));
        }
    }

    public void PunchRight() {
        if (!isPunchingLeft && !isPunchingRight && !_mvmt.isDodging) {
            isPunchingRight = true;
            _rightPunchCoroutine = StartCoroutine(PunchRoutine(_mvmt.rightGlove, _mvmt.GetRightHomePos(), false));
        }
    }

    public void HandleImpact(bool isLeft, bool wasBlocked, BoxerCombat opponent) {
        // Arrêt immédiat de l'aller
        if (isLeft) {
            if (_leftPunchCoroutine != null) StopCoroutine(_leftPunchCoroutine);
        } else {
            if (_rightPunchCoroutine != null) StopCoroutine(_rightPunchCoroutine);
        }

        // Retour forcé suite à l'impact
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

        while (Vector3.Distance(glove.localPosition, CalculateTargetGuard(home, isLeft)) > 0.01f) {
            Vector3 targetGuard = CalculateTargetGuard(home, isLeft);
            glove.localPosition = Vector3.MoveTowards(glove.localPosition, targetGuard, speed * Time.deltaTime);
            yield return null;
        }

        yield return new WaitForSeconds(cooldown);
        if (isLeft) isPunchingLeft = false; else isPunchingRight = false;
    }

    private Vector3 CalculateTargetGuard(Vector3 home, bool isLeft) 
    {
        Vector2 specificInput = isLeft ? _mvmt.aimInputLeft : _mvmt.aimInputRight;
        return home + new Vector3(specificInput.x * _mvmt.aimRange.x, specificInput.y * _mvmt.aimRange.y, 0);
    }

    IEnumerator ImpactSequence(bool isLeft, bool wasBlocked, BoxerCombat opponent) {
        if (!wasBlocked && opponent != null) {
            // On vérifie si l'adversaire est encore vivant avant de faire trembler son visuel
            BoxerHealth opponentHealth = opponent.GetComponent<BoxerHealth>();

            if (opponentHealth != null && !opponentHealth.IsKO) {
                yield return new WaitForSeconds(0.05f); // Petit délai de pénétration

                BoxerVisuals opponentVisuals = opponent.GetComponent<BoxerVisuals>();
                if (opponentVisuals != null && opponentVisuals.enabled) {
                    Vector3 punchDir = (opponent.transform.position - transform.position).normalized;
                    opponentVisuals.TriggerWobble(punchDir); // On appelle la nouvelle méthode
                }
            }
        }

        // Le bras revient toujours à la base
        Transform glove = isLeft ? _mvmt.leftGlove : _mvmt.rightGlove;
        Vector3 home = isLeft ? _mvmt.GetLeftHomePos() : _mvmt.GetRightHomePos();
        yield return ReturnRoutine(glove, home, isLeft);
    }

    public void ResetCombatState()
    {
        StopAllCoroutines();
        isPunchingLeft = false;
        isPunchingRight = false;
    }
}