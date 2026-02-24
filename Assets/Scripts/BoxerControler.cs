using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class BoxerController : MonoBehaviour
{
    [Header("Gants & Pivot")]
    public Transform leftGlove;
    public Transform rightGlove;
    public Transform pivot;

    [Header("Réglages de Combat")]
    public float punchSpeed = 15f;
    public float punchDistance = 1.8f;
    public float returnMultiplier = 2.0f; 

    [Header("Réglages Visée (Garde)")]
    public Vector2 aimInput; 
    public Vector2 aimRange = new Vector2(0.6f, 0.4f);
    public float aimSmoothSpeed = 10f;

    [Header("Réglages Esquive")]
    public float dodgeAngle = 35f;      // L'angle d'inclinaison de l'esquive
    public float dodgeSmoothSpeed = 12f; // Vitesse de l'esquive
    private float _currentDodgeLean = 0f; // Valeur cible de rotation Z
    private bool _isDodging = false;

    [Header("Réglages Impact (Wobble)")]
    public float wobbleIntensity = 30f;
    public float recoverSpeed = 5f;

    private Vector3 _leftHomePos, _rightHomePos;
    private Quaternion _leftHomeRot, _rightHomeRot;
    private bool _isPunchingLeft = false, _isPunchingRight = false;

    private Coroutine _leftPunchCoroutine, _rightPunchCoroutine, _wobbleCoroutine;

    void Start()
    {
        _leftHomePos = leftGlove.localPosition;
        _rightHomePos = rightGlove.localPosition;
        _leftHomeRot = leftGlove.localRotation;
        _rightHomeRot = rightGlove.localRotation;
    }

    void Update()
    {
        HandleDodge(); // Priorité à l'esquive
        HandleGuardMovement();
        HandleInputs();
    }

    // --- GESTION DES ESQUIVES ---
    private void HandleDodge()
    {
        float targetZ = 0f;
        _isDodging = false;

        // On vérifie les touches d'esquive (Flèches ou Q/D selon ton mapping)
        if (Keyboard.current.leftArrowKey.isPressed)
        {
            targetZ = dodgeAngle; // Penche à gauche
            _isDodging = true;
        }
        else if (Keyboard.current.rightArrowKey.isPressed)
        {
            targetZ = -dodgeAngle; // Penche à droite
            _isDodging = true;
        }

        // Applique l'inclinaison au pivot de manière fluide
        // On ne fait ça que si on n'est pas en train de subir un "Wobble" (coup reçu)
        if (_wobbleCoroutine == null)
        {
            _currentDodgeLean = Mathf.Lerp(_currentDodgeLean, targetZ, Time.deltaTime * dodgeSmoothSpeed);
            pivot.localRotation = Quaternion.Euler(0, 0, _currentDodgeLean);
        }
    }

    private void HandleGuardMovement()
    {
        Vector3 aimOffset = new Vector3(aimInput.x * aimRange.x, aimInput.y * aimRange.y, 0);

        if (!_isPunchingLeft)
            leftGlove.localPosition = Vector3.Lerp(leftGlove.localPosition, _leftHomePos + aimOffset, Time.deltaTime * aimSmoothSpeed);
        
        if (!_isPunchingRight)
            rightGlove.localPosition = Vector3.Lerp(rightGlove.localPosition, _rightHomePos + aimOffset, Time.deltaTime * aimSmoothSpeed);
    }

    private void HandleInputs()
    {
        // On empêche de frapper si on est en train d'esquiver (optionnel)
        if (_isDodging) return;

        if (Keyboard.current.qKey.wasPressedThisFrame) PunchLeft();
        if (Keyboard.current.dKey.wasPressedThisFrame) PunchRight();
    }

    // --- ACTIONS DE COMBAT ---
    public void PunchLeft()
    {
        if (!_isPunchingLeft)
        {
            _isPunchingLeft = true;
            _leftPunchCoroutine = StartCoroutine(PunchRoutine(leftGlove, _leftHomePos, _leftHomeRot, true));
        }
    }

    public void PunchRight()
    {
        if (!_isPunchingRight)
        {
            _isPunchingRight = true;
            _rightPunchCoroutine = StartCoroutine(PunchRoutine(rightGlove, _rightHomePos, _rightHomeRot, false));
        }
    }

    public void HandleImpact(bool isLeft, bool wasBlocked, BoxerController opponent)
    {
        StartCoroutine(ImpactSequenceRoutine(isLeft, wasBlocked, opponent));
    }

    IEnumerator ImpactSequenceRoutine(bool isLeft, bool wasBlocked, BoxerController opponent)
    {
        StopPunch(isLeft);
        Transform glove = isLeft ? leftGlove : rightGlove;
        Vector3 homePos = isLeft ? _leftHomePos : _rightHomePos;
        Quaternion homeRot = isLeft ? _leftHomeRot : _rightHomeRot;

        if (wasBlocked)
        {
            yield return StartCoroutine(ReturnRoutine(glove, homePos, homeRot, isLeft));
        }
        else
        {
            yield return new WaitForSeconds(0.05f);
            if (opponent != null) opponent.GetHit((opponent.transform.position - transform.position).normalized);
            yield return StartCoroutine(ReturnRoutine(glove, homePos, homeRot, isLeft));
        }
    }

    private void StopPunch(bool isLeft)
    {
        if (isLeft && _leftPunchCoroutine != null) StopCoroutine(_leftPunchCoroutine);
        if (!isLeft && _rightPunchCoroutine != null) StopCoroutine(_rightPunchCoroutine);
    }

    IEnumerator PunchRoutine(Transform glove, Vector3 homePos, Quaternion homeRot, bool isLeft)
    {
        Vector3 startPos = glove.localPosition;
        Vector3 targetPos = startPos + Vector3.forward * punchDistance;
        Quaternion targetRot = homeRot * Quaternion.Euler(0, 0, isLeft ? 90 : -90);

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * punchSpeed;
            glove.localPosition = Vector3.Lerp(startPos, targetPos, t);
            glove.localRotation = Quaternion.Slerp(homeRot, targetRot, t);
            yield return null;
        }
        yield return StartCoroutine(ReturnRoutine(glove, homePos, homeRot, isLeft));
    }

    IEnumerator ReturnRoutine(Transform glove, Vector3 homePos, Quaternion homeRot, bool isLeft)
    {
        Vector3 currentPos = glove.localPosition;
        Quaternion currentRot = glove.localRotation;
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * (punchSpeed * returnMultiplier);
            Vector3 finalTarget = homePos + new Vector3(aimInput.x * aimRange.x, aimInput.y * aimRange.y, 0);
            glove.localPosition = Vector3.Lerp(currentPos, finalTarget, t);
            glove.localRotation = Quaternion.Slerp(currentRot, homeRot, t);
            yield return null;
        }
        if (isLeft) _isPunchingLeft = false; else _isPunchingRight = false;
    }

    // --- ENCAISSER OU ESQUIVER ---
    public void GetHit(Vector3 worldPunchDirection)
    {
        // 1. Si on esquive, on ignore tout
        if (_isDodging)
        {
            Debug.Log("ESQUIVÉ !");
            return; 
        }

        // 2. APPEL AU SYSTÈME DE SANTÉ
        BoxerHealth health = GetComponent<BoxerHealth>();
        if (health != null)
        {
            health.TakeDamage();
        }

        // 3. Le reste de ton code (Wobble/basculement de tête)
        Vector3 localDir = transform.InverseTransformDirection(worldPunchDirection);
        localDir.y = 0; 
        Vector3 tiltAxis = Vector3.Cross(Vector3.up, localDir);

        if (_wobbleCoroutine != null) StopCoroutine(_wobbleCoroutine);
        _wobbleCoroutine = StartCoroutine(WobbleRoutine(tiltAxis));
    }
    
    IEnumerator WobbleRoutine(Vector3 axis)
    {
        Quaternion targetRot = Quaternion.AngleAxis(wobbleIntensity, axis);
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 20f; 
            pivot.localRotation = Quaternion.Lerp(pivot.localRotation, targetRot, t);
            yield return null;
        }
        while (Quaternion.Angle(pivot.localRotation, Quaternion.identity) > 0.1f)
        {
            pivot.localRotation = Quaternion.Lerp(pivot.localRotation, Quaternion.identity, Time.deltaTime * recoverSpeed);
            yield return null;
        }
        pivot.localRotation = Quaternion.identity;
        _wobbleCoroutine = null;
    }
}