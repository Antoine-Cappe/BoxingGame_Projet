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
    public float aimSmoothSpeed = 10f;

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

    void Awake()
    {
        _leftHomePos = leftGlove.localPosition;
        _rightHomePos = rightGlove.localPosition;
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
        // Calcul du décalage pour le gant GAUCHE
        Vector3 offsetLeft = new Vector3(aimInputLeft.x * aimRange.x, aimInputLeft.y * aimRange.y, 0);
        if (!_combat.isPunchingLeft)
            leftGlove.localPosition = Vector3.Lerp(leftGlove.localPosition, _leftHomePos + offsetLeft, Time.deltaTime * aimSmoothSpeed);

        // Calcul du décalage pour le gant DROIT
        Vector3 offsetRight = new Vector3(aimInputRight.x * aimRange.x, aimInputRight.y * aimRange.y, 0);
        if (!_combat.isPunchingRight)
            rightGlove.localPosition = Vector3.Lerp(rightGlove.localPosition, _rightHomePos + offsetRight, Time.deltaTime * aimSmoothSpeed);
    }

    public Vector3 GetLeftHomePos() => _leftHomePos;
    public Vector3 GetRightHomePos() => _rightHomePos;

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