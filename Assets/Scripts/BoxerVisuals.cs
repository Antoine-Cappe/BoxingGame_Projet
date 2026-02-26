using UnityEngine;
using System.Collections;

public class BoxerVisuals : MonoBehaviour
{
    [Header("Réglages du Tremblement")]
    public Transform pivot;
    public float wobbleIntensity = 30f;
    public float recoverSpeed = 5f;

    private Coroutine _wobbleCoroutine;
    private BoxerMovement _mvmt;

    void Awake() {
        _mvmt = GetComponent<BoxerMovement>();
    }

    // Renommé pour correspondre à l'appel dans BoxerHealth
    public void TriggerWobble(Vector3 direction) {
        
        // Si on est en train d'esquiver, on ignore l'effet visuel du choc
        if (_mvmt != null && _mvmt.isDodging) {
            return;
        }

        Vector3 localDir = transform.InverseTransformDirection(direction);
        // On calcule l'axe de rotation perpendiculaire à l'impact
        Vector3 axis = Vector3.Cross(Vector3.up, localDir.normalized);

        // Si un tremblement est déjà en cours, on l'arrête pour recommencer le nouveau
        if (_wobbleCoroutine != null) StopCoroutine(_wobbleCoroutine);
        _wobbleCoroutine = StartCoroutine(WobbleRoutine(axis));
    }

    IEnumerator WobbleRoutine(Vector3 axis) {
        Quaternion target = Quaternion.AngleAxis(wobbleIntensity, axis);
        float t = 0;

        // 1. PHASE D'IMPACT : On penche brusquement le pivot
        while (t < 1) {
            t += Time.deltaTime * 20f; // Vitesse d'impact très rapide
            pivot.localRotation = Quaternion.Lerp(pivot.localRotation, target, t);
            yield return null;
        }

        // 2. PHASE DE RÉCUPÉRATION : On revient à la position droite
        while (Quaternion.Angle(pivot.localRotation, Quaternion.identity) > 0.1f)
        {
            // SÉCURITÉ : Si le boxeur est mis KO pendant le tremblement, on arrête tout !
            // (Le script sera désactivé par BoxerHealth)
            if (!this.enabled) yield break; 

            pivot.localRotation = Quaternion.Lerp(pivot.localRotation, Quaternion.identity, Time.deltaTime * recoverSpeed);
            yield return null;
        }
        
        // On s'assure d'être parfaitement droit à la fin
        if (this.enabled) pivot.localRotation = Quaternion.identity;
        _wobbleCoroutine = null;
    }
}