using UnityEngine;
using System.Collections;

public class BoxerMovement : MonoBehaviour
{
    [Header("Références")]
    public Transform pivot;
    public Transform leftGlove, rightGlove;

    [Header("Réglages Garde (Verticale uniquement)")]
    public float guardHighY = 0.35f;  // Décalage vers le haut
    public float guardLowY = -0.35f;  // Décalage vers le bas
    public float guardLerpSpeed = 20f; // Vitesse de transition entre les positions

    // États : 0 = Milieu, 1 = Haut, 2 = Bas
    [HideInInspector] public int leftGuardState = 0;
    [HideInInspector] public int rightGuardState = 0;

    [Header("Réglages Esquive")]
    public float dodgeAngle = 35f;
    public float dodgeSpeed = 15f;
    public float dodgeDuration = 0.3f;
    public float dodgeCooldown = 0.8f;

    public bool isDodging { get; private set; }
    private bool _canDodge = true;
    private Vector3 _leftHomePos, _rightHomePos;
    private BoxerCombat _combat;
    private Rigidbody _rb;

    // Positions de garde actuelles (calculées)
    private Vector3 _currentLeftGuard;
    private Vector3 _currentRightGuard;

    [Header("Réglages Déplacement")]
    public float moveSpeed = 4f;
    public float rotationSpeed = 150f;
    [HideInInspector] public Vector2 moveInput;
    [HideInInspector] public float rotateInput;

    void Awake()
    {
        _leftHomePos = leftGlove.localPosition;
        _rightHomePos = rightGlove.localPosition;
        _currentLeftGuard = _leftHomePos;
        _currentRightGuard = _rightHomePos;

        _combat = GetComponent<BoxerCombat>();
        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = false;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    void FixedUpdate()
    {
        HandleGuardMovement();
        HandleMovement();
    }

    private void HandleGuardMovement()
    {
        // --- CALCUL CIBLE GAUCHE ---
        _currentLeftGuard = CalculateGuardPos(_leftHomePos, leftGuardState, _currentLeftGuard);
        if (!_combat.isPunchingLeft) leftGlove.localPosition = _currentLeftGuard;

        // --- CALCUL CIBLE DROITE ---
        _currentRightGuard = CalculateGuardPos(_rightHomePos, rightGuardState, _currentRightGuard);
        if (!_combat.isPunchingRight) rightGlove.localPosition = _currentRightGuard;
    }

    private Vector3 CalculateGuardPos(Vector3 home, int state, Vector3 current)
    {
        float targetY = home.y;
        if (state == 1) targetY += guardHighY; // Garde Haute
        else if (state == 2) targetY += guardLowY; // Garde Basse

        Vector3 targetPos = new Vector3(home.x, targetY, home.z);
        
        // On utilise Lerp pour que le mouvement soit fluide visuellement
        return Vector3.Lerp(current, targetPos, Time.fixedDeltaTime * guardLerpSpeed);
    }

    // --- INTERFACE POUR LES AUTRES SCRIPTS ---
    public Vector3 GetLeftHomePos() => _leftHomePos;
    public Vector3 GetRightHomePos() => _rightHomePos;
    public Vector3 GetCurrentLeftGuard() => _currentLeftGuard;
    public Vector3 GetCurrentRightGuard() => _currentRightGuard;

    public void TriggerDodge(float direction)
    {
        if (_canDodge && !isDodging && !_combat.isPunchingLeft && !_combat.isPunchingRight)
            StartCoroutine(DodgeRoutine(direction));
    }

    IEnumerator DodgeRoutine(float direction)
    {
        _canDodge = false;
        isDodging = true;
        float targetZ = direction * dodgeAngle;
        Quaternion targetRotation = Quaternion.Euler(0, 0, targetZ);
        Quaternion startRotation = Quaternion.identity;

        float t = 0;
        while (t < 1f) {
            t += Time.deltaTime * dodgeSpeed;
            pivot.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }
        yield return new WaitForSeconds(dodgeDuration);
        t = 0;
        while (t < 1f) {
            t += Time.deltaTime * dodgeSpeed;
            pivot.localRotation = Quaternion.Slerp(targetRotation, startRotation, t);
            yield return null;
        }
        pivot.localRotation = startRotation;
        isDodging = false;
        yield return new WaitForSeconds(dodgeCooldown);
        _canDodge = true;
    }

    private void HandleMovement()
    {
        if (Mathf.Abs(rotateInput) > 0.1f) {
            float rotation = rotateInput * rotationSpeed * Time.fixedDeltaTime;
            _rb.MoveRotation(_rb.rotation * Quaternion.Euler(0f, rotation, 0f));
        }
        if (moveInput.magnitude > 0.1f) {
            Vector3 moveDir = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;
            _rb.MovePosition(_rb.position + moveDir * moveSpeed * Time.fixedDeltaTime);
        }
    }

    public void ResetMovementState()
    {
        StopAllCoroutines();
        isDodging = false;
        _canDodge = true;
        moveInput = Vector2.zero;
        rotateInput = 0f;
        leftGuardState = 0;
        rightGuardState = 0;
        pivot.localRotation = Quaternion.identity;
    }
}