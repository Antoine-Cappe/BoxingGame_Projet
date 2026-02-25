using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class BoxerCombat : MonoBehaviour
{
    [Header("Réglages de Vitesse")]
    public float punchSpeed = 15f;      // Vitesse constante (m/s)
    public float returnMultiplier = 1.2f; // Le retour est 20% plus rapide que l'aller
    public float cooldown = 0.5f; // Délai avant de libérer l'action après le retour

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
        // 1. ARRÊT IMMÉDIAT de la coroutine d'aller (PunchRoutine)
        if (isLeft) {
            if (_leftPunchCoroutine != null) StopCoroutine(_leftPunchCoroutine);
        } else {
            if (_rightPunchCoroutine != null) StopCoroutine(_rightPunchCoroutine);
        }

        // 2. Lancement de la séquence d'impact
        // On passe l'adversaire (qui est un BoxerCombat)
        StartCoroutine(ImpactSequence(isLeft, wasBlocked, opponent));
    }

    IEnumerator PunchRoutine(Transform glove, Vector3 home, bool isLeft) {
        Vector3 target = glove.localPosition + Vector3.forward * punchDistance; // Utilise ta variable punchDistance

        // ALLER : Tant qu'on n'est pas arrivé à la cible
        while (Vector3.Distance(glove.localPosition, target) > 0.01f) {
            glove.localPosition = Vector3.MoveTowards(glove.localPosition, target, punchSpeed * Time.deltaTime);
            yield return null;
        }

        // RETOUR automatique si on n'a rien touché
        yield return ReturnRoutine(glove, home, isLeft);
    }

    IEnumerator ReturnRoutine(Transform glove, Vector3 home, bool isLeft) {
        float speed = punchSpeed * returnMultiplier;

        // RETOUR : Tant qu'on n'est pas revenu à la garde
        while (Vector3.Distance(glove.localPosition, CalculateTargetGuard(home, isLeft)) > 0.01f) {
            Vector3 targetGuard = CalculateTargetGuard(home, isLeft);
            glove.localPosition = Vector3.MoveTowards(glove.localPosition, targetGuard, speed * Time.deltaTime);
            yield return null;
        }

        // Fin du coup : On libère l'action après un cooldown
        yield return new WaitForSeconds(cooldown);
        if (isLeft) isPunchingLeft = false; else isPunchingRight = false;
    }

    // Calcul dynamique de la position de garde (inclut la visée)
    private Vector3 CalculateTargetGuard(Vector3 home, bool isLeft) 
    {
        // On récupère l'input spécifique au bras
        Vector2 specificInput = isLeft ? _mvmt.aimInputLeft : _mvmt.aimInputRight;
        
        return home + new Vector3(specificInput.x * _mvmt.aimRange.x, specificInput.y * _mvmt.aimRange.y, 0);
    }

    IEnumerator ImpactSequence(bool isLeft, bool wasBlocked, BoxerCombat opponent) {
        if (!wasBlocked && opponent != null) {
            // Petit délai de pénétration pour le punch
            yield return new WaitForSeconds(0.05f);

            // On cherche le script Visuals de l'ADVERSAIRE
            BoxerVisuals opponentVisuals = opponent.GetComponent<BoxerVisuals>();
            if (opponentVisuals != null) {
                Vector3 punchDir = (opponent.transform.position - transform.position).normalized;
                opponentVisuals.GetHit(punchDir); // Déclenche le Wobble chez l'autre
            }
        }

        // 3. RETOUR IMMÉDIAT
        Transform glove = isLeft ? _mvmt.leftGlove : _mvmt.rightGlove;
        Vector3 home = isLeft ? _mvmt.GetLeftHomePos() : _mvmt.GetRightHomePos();
        yield return ReturnRoutine(glove, home, isLeft);
    }

    // Ajoute cette variable si elle manquait
    public float punchDistance = 1.8f;

    public void ResetCombatState()
    {
        // On arrête les coups en cours
        StopAllCoroutines();
        
        // On force les booléens à faux
        isPunchingLeft = false;
        isPunchingRight = false;
        
        // Si tu as des paramètres d'animator à reset, fais-le ici aussi
        // ex: _anim.Play("Idle");
    }
}   