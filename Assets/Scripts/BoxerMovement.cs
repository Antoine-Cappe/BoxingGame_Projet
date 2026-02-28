using UnityEngine;
using System.Collections;

public enum GuardType { None, High, Low }

public class BoxerMovement : MonoBehaviour
{
    [Header("Références")]
    public Transform pivot;
    public Transform leftGlove, rightGlove;

    [Header("Réglages Visée")]
    public Vector2 aimInput;
    public Vector2 aimRange = new Vector2(0.6f, 0.4f);
    public float stepSpeed = 0.02f;

    [Header("Réglages Déplacement")]
    public float moveSpeed = 2.5f;
    public float rotateSpeed = 120f;
    public float ringRadius = 4.5f;

    [Header("Réglages Esquive (Impulsion)")]
    public float dodgeAngle = 35f;
    public float dodgeSpeed = 15f;
    public float dodgeDuration = 0.3f;
    public float dodgeCooldown = 0.8f;

    [Header("Réglages Garde")]
    // Décalage Z vers l'avant quand on se met en garde (rapproche les poings)
    public float guardZOffset = 0.25f;
    // Décalage X vers le centre (resserre les poings)
    public float guardXOffset = 0.15f;
    // Décalage Y pour la garde haute (monte les poings vers le visage)
    public float guardHighYOffset = 0.35f;
    // Vitesse de transition vers/depuis la garde
    public float guardTransitionSpeed = 12f;

    // --- INPUTS INDÉPENDANTS GANTS ---
    [HideInInspector] public Vector2 aimInputLeft;
    [HideInInspector] public Vector2 aimInputRight;

    // --- INPUTS DÉPLACEMENT & ROTATION ---
    [HideInInspector] public Vector2 moveInput;
    [HideInInspector] public float rotateInput;

    // --- GARDE ---
    [HideInInspector] public GuardType guardInput = GuardType.None;
    public GuardType currentGuard { get; private set; } = GuardType.None;

    // --- BACKING FIELD ESQUIVE ---
    private bool _isDodging = false;
    public bool isDodging => _isDodging;

    private bool _canDodge = true;
    private Vector3 _leftHomePos, _rightHomePos;
    private BoxerCombat _combat;
    private BoxerVisuals _visuals;

    private Vector3 _currentLeftGuard;
    private Vector3 _currentRightGuard;

    // Positions de garde calculées dynamiquement depuis _homePos
    private Vector3 _guardHighLeft, _guardHighRight;
    private Vector3 _guardLowLeft, _guardLowRight;

    private Vector3 _ringCenter;

    void Awake()
    {
        _leftHomePos = leftGlove.localPosition;
        _rightHomePos = rightGlove.localPosition;
        _currentLeftGuard = _leftHomePos;
        _currentRightGuard = _rightHomePos;
        _combat = GetComponent<BoxerCombat>();
        _visuals = GetComponent<BoxerVisuals>();
        _ringCenter = Vector3.zero;

        // ── Calcul des positions de garde RELATIVES à la position home ──
        //
        // GARDE BASSE : même hauteur Y que home, poings rapprochés
        //   → Z + guardZOffset  (avancer les poings)
        //   → X réduit vers 0   (resserrer)
        _guardLowLeft = new Vector3(
            _leftHomePos.x + guardXOffset,   // gauche  : X augmente vers 0
            _leftHomePos.y,                    // même hauteur
            _leftHomePos.z + guardZOffset);   // avancer

        _guardLowRight = new Vector3(
            _rightHomePos.x - guardXOffset,   // droite  : X diminue vers 0
            _rightHomePos.y,                   // même hauteur
            _rightHomePos.z + guardZOffset);   // avancer

        // GARDE HAUTE : poings montés + rapprochés (protection visage)
        _guardHighLeft = new Vector3(
            _leftHomePos.x + guardXOffset,
            _leftHomePos.y + guardHighYOffset, // monter vers le visage
            _leftHomePos.z + guardZOffset);

        _guardHighRight = new Vector3(
            _rightHomePos.x - guardXOffset,
            _rightHomePos.y + guardHighYOffset,
            _rightHomePos.z + guardZOffset);
    }

    void Update()
    {
        HandleBodyMovement();
        HandleGuardMovement();
    }

    // ─────────────────────────────────────────────────────────────
    //  DÉPLACEMENT & ROTATION
    // ─────────────────────────────────────────────────────────────
    private void HandleBodyMovement()
    {
        if (_isDodging) return;

        if (Mathf.Abs(rotateInput) > 0.1f)
        {
            float rotation = rotateInput * rotateSpeed * Time.deltaTime;
            transform.Rotate(0f, rotation, 0f, Space.Self);
        }

        if (moveInput.sqrMagnitude > 0.01f)
        {
            Vector3 direction = transform.forward * moveInput.y
                              + transform.right * moveInput.x;
            direction.y = 0f;

            Vector3 nextPos = transform.position + direction * moveSpeed * Time.deltaTime;
            Vector3 toCenter = nextPos - _ringCenter;
            if (toCenter.magnitude > ringRadius)
                nextPos = _ringCenter + toCenter.normalized * ringRadius;

            transform.position = nextPos;
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  GARDE + MOUVEMENT DES GANTS
    // ─────────────────────────────────────────────────────────────
    private void HandleGuardMovement()
    {
        currentGuard = guardInput;

        if (currentGuard != GuardType.None)
        {
            // Sélectionne la cible selon le type de garde
            Vector3 targetLeft = currentGuard == GuardType.High
                                  ? _guardHighLeft : _guardLowLeft;
            Vector3 targetRight = currentGuard == GuardType.High
                                  ? _guardHighRight : _guardLowRight;

            // Interpolation fluide vers la position de garde
            if (!_combat.isPunchingLeft)
            {
                leftGlove.localPosition = Vector3.Lerp(
                    leftGlove.localPosition,
                    targetLeft,
                    guardTransitionSpeed * Time.deltaTime);
            }
            if (!_combat.isPunchingRight)
            {
                rightGlove.localPosition = Vector3.Lerp(
                    rightGlove.localPosition,
                    targetRight,
                    guardTransitionSpeed * Time.deltaTime);
            }

            // Synchro pour que le retour de punch vise la bonne position
            _currentLeftGuard = leftGlove.localPosition;
            _currentRightGuard = rightGlove.localPosition;
        }
        else
        {
            // ── HORS GARDE : mouvement libre des gants ──
            if (aimInputLeft.x > 0.2f) _currentLeftGuard.x += stepSpeed;
            else if (aimInputLeft.x < -0.2f) _currentLeftGuard.x -= stepSpeed;
            if (aimInputLeft.y > 0.2f) _currentLeftGuard.y += stepSpeed;
            else if (aimInputLeft.y < -0.2f) _currentLeftGuard.y -= stepSpeed;

            _currentLeftGuard.x = Mathf.Clamp(_currentLeftGuard.x,
                _leftHomePos.x - aimRange.x, _leftHomePos.x + aimRange.x);
            _currentLeftGuard.y = Mathf.Clamp(_currentLeftGuard.y,
                _leftHomePos.y - aimRange.y, _leftHomePos.y + aimRange.y);

            if (!_combat.isPunchingLeft)
                leftGlove.localPosition = _currentLeftGuard;

            if (aimInputRight.x > 0.2f) _currentRightGuard.x += stepSpeed;
            else if (aimInputRight.x < -0.2f) _currentRightGuard.x -= stepSpeed;
            if (aimInputRight.y > 0.2f) _currentRightGuard.y += stepSpeed;
            else if (aimInputRight.y < -0.2f) _currentRightGuard.y -= stepSpeed;

            _currentRightGuard.x = Mathf.Clamp(_currentRightGuard.x,
                _rightHomePos.x - aimRange.x, _rightHomePos.x + aimRange.x);
            _currentRightGuard.y = Mathf.Clamp(_currentRightGuard.y,
                _rightHomePos.y - aimRange.y, _rightHomePos.y + aimRange.y);

            if (!_combat.isPunchingRight)
                rightGlove.localPosition = _currentRightGuard;
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  ESQUIVE
    // ─────────────────────────────────────────────────────────────
    public void TriggerDodge(float direction)
    {
        if (_canDodge && !_isDodging && !_combat.isPunchingLeft && !_combat.isPunchingRight)
            StartCoroutine(DodgeRoutine(direction));
    }

    IEnumerator DodgeRoutine(float direction)
    {
        _canDodge = false;
        _isDodging = true;

        float targetZ = direction * dodgeAngle;
        Quaternion targetRotation = Quaternion.Euler(0, 0, targetZ);
        Quaternion startRotation = Quaternion.identity;

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * dodgeSpeed;
            pivot.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        yield return new WaitForSeconds(dodgeDuration);
        if (!this.enabled) yield break;

        t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * dodgeSpeed;
            pivot.localRotation = Quaternion.Slerp(targetRotation, startRotation, t);
            yield return null;
        }

        pivot.localRotation = startRotation;
        _isDodging = false;

        yield return new WaitForSeconds(dodgeCooldown);
        _canDodge = true;
    }

    // ─────────────────────────────────────────────────────────────
    //  ACCESSEURS
    // ─────────────────────────────────────────────────────────────
    public Vector3 GetLeftHomePos() => _leftHomePos;
    public Vector3 GetRightHomePos() => _rightHomePos;
    public Vector3 GetCurrentLeftGuard() => _currentLeftGuard;
    public Vector3 GetCurrentRightGuard() => _currentRightGuard;

    // ─────────────────────────────────────────────────────────────
    //  RESET
    // ─────────────────────────────────────────────────────────────
    public void ResetMovementState()
    {
        StopAllCoroutines();
        _isDodging = false;
        _canDodge = true;
        moveInput = Vector2.zero;
        rotateInput = 0f;
        guardInput = GuardType.None;

        _currentLeftGuard = _leftHomePos;
        _currentRightGuard = _rightHomePos;

        if (pivot != null)
            pivot.localRotation = Quaternion.identity;
    }
}