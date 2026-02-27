using UnityEngine;
using System.Collections;

public class BoxerMovement : MonoBehaviour
{
    [Header("Références")]
    public Transform pivot;
    public Transform leftGlove, rightGlove;

    [Header("Réglages Visée")]
    public Vector2 aimInput; 
    public Vector2 aimRange = new Vector2(0.6f, 0.4f);
    public float stepSpeed = 0.02f;

    [Header("Réglages Esquive (Impulsion)")]
    public float dodgeAngle = 35f;
    public float dodgeSpeed = 15f;      // Vitesse de l'inclinaison
    public float dodgeDuration = 0.3f;   // Temps passé en position penchée
    public float dodgeCooldown = 0.8f;   // Temps avant la prochaine esquive possible

    // --- NOUVEAUX INPUTS INDÉPENDANTS ---
    [HideInInspector] public Vector2 aimInputLeft;
    [HideInInspector] public Vector2 aimInputRight;
    
    public bool isDodging { get; private set; }
    private bool _canDodge = true;
    private Vector3 _leftHomePos, _rightHomePos;
    private BoxerCombat _combat;
    private BoxerVisuals _visuals;

    private Vector3 _currentLeftGuard;
    private Vector3 _currentRightGuard;

    void Awake()
    {
        _leftHomePos = leftGlove.localPosition;
        _rightHomePos = rightGlove.localPosition;
        _currentLeftGuard = _leftHomePos;
        _currentRightGuard = _rightHomePos;
        _combat = GetComponent<BoxerCombat>();
        _visuals = GetComponent<BoxerVisuals>();
    }

    void Update()
    {
        // On ne gère plus l'esquive ici, seulement la garde
        HandleGuardMovement();
    }

    // Cette fonction sera appelée par l'InputHandler ou l'IA
    public void TriggerDodge(float direction)
    {
        if (_canDodge && !isDodging && !_combat.isPunchingLeft && !_combat.isPunchingRight)
        {
            StartCoroutine(DodgeRoutine(direction));
        }
    }

    IEnumerator DodgeRoutine(float direction)
    {
        _canDodge = false;
        isDodging = true;

        float targetZ = direction * dodgeAngle; // direction sera -1 (droite) ou 1 (gauche)
        Quaternion targetRotation = Quaternion.Euler(0, 0, targetZ);
        Quaternion startRotation = Quaternion.identity;

        // 1. ALLER (Inclinaison)
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * dodgeSpeed;
            pivot.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        // 2. MAINTIEN (Le temps où on est invincible)
        yield return new WaitForSeconds(dodgeDuration);

        // AJOUT : Vérification avant le retour à la normale
        if (!this.enabled) yield break;

        // 3. RETOUR (Redressement)
        t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * dodgeSpeed;
            pivot.localRotation = Quaternion.Slerp(targetRotation, startRotation, t);
            yield return null;
        }

        pivot.localRotation = startRotation;
        isDodging = false;

        // 4. COOLDOWN
        yield return new WaitForSeconds(dodgeCooldown);
        _canDodge = true;
    }

    private void HandleGuardMovement()
{
    // --- CALCUL DE LA GARDE GAUCHE (Toujours mis à jour) ---
    if (aimInputLeft.x > 0.2f) _currentLeftGuard.x += stepSpeed;
    else if (aimInputLeft.x < -0.2f) _currentLeftGuard.x -= stepSpeed;

    if (aimInputLeft.y > 0.2f) _currentLeftGuard.y += stepSpeed;
    else if (aimInputLeft.y < -0.2f) _currentLeftGuard.y -= stepSpeed;

    _currentLeftGuard.x = Mathf.Clamp(_currentLeftGuard.x, _leftHomePos.x - aimRange.x, _leftHomePos.x + aimRange.x);
    _currentLeftGuard.y = Mathf.Clamp(_currentLeftGuard.y, _leftHomePos.y - aimRange.y, _leftHomePos.y + aimRange.y);

    // On n'applique physiquement la position que si on ne punch pas
    if (!_combat.isPunchingLeft) {
        leftGlove.localPosition = _currentLeftGuard;
    }

    // --- CALCUL DE LA GARDE DROITE (Toujours mis à jour) ---
    if (aimInputRight.x > 0.2f) _currentRightGuard.x += stepSpeed;
    else if (aimInputRight.x < -0.2f) _currentRightGuard.x -= stepSpeed;

    if (aimInputRight.y > 0.2f) _currentRightGuard.y += stepSpeed;
    else if (aimInputRight.y < -0.2f) _currentRightGuard.y -= stepSpeed;

    _currentRightGuard.x = Mathf.Clamp(_currentRightGuard.x, _rightHomePos.x - aimRange.x, _rightHomePos.x + aimRange.x);
    _currentRightGuard.y = Mathf.Clamp(_currentRightGuard.y, _rightHomePos.y - aimRange.y, _rightHomePos.y + aimRange.y);

    if (!_combat.isPunchingRight) {
        rightGlove.localPosition = _currentRightGuard;
    }
}

    public Vector3 GetLeftHomePos() => _leftHomePos;
    public Vector3 GetRightHomePos() => _rightHomePos;

    public Vector3 GetCurrentLeftGuard() => _currentLeftGuard;
    public Vector3 GetCurrentRightGuard() => _currentRightGuard;

    public void ResetMovementState()
    {
        // 1. On arrête la coroutine d'esquive en cours
        StopAllCoroutines();
        
        // 2. On remet les verrous à zéro
        isDodging = false;
        _canDodge = true;

        // 3. On remet le pivot bien droit instantanément
        if (pivot != null) {
            pivot.localRotation = Quaternion.identity;
        }
    }
}