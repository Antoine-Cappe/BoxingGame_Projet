using UnityEngine;
using System.Collections;

public class BoxerVisuals : MonoBehaviour
{
    public Transform pivot;
    public float wobbleIntensity = 30f;
    public float recoverSpeed = 5f;

    private Coroutine _wobbleCoroutine;
    private BoxerMovement _mvmt;
    private BoxerHealth _health;

    void Awake() {
        _mvmt = GetComponent<BoxerMovement>();
        _health = GetComponent<BoxerHealth>();
    }

    public void GetHit(Vector3 direction) {
        // Debug pour vérifier que la fonction est bien reçue
        // Debug.Log(gameObject.name + " a reçu un message d'impact !");

        if (_mvmt != null && _mvmt.isDodging) {
            Debug.Log("Coup ignoré : " + gameObject.name + " est en train d'esquiver.");
            return;
        }

        if (_health != null) _health.TakeDamage(direction);

        Vector3 localDir = transform.InverseTransformDirection(direction);
        // On s'assure que l'axe n'est pas (0,0,0)
        Vector3 axis = Vector3.Cross(Vector3.up, localDir.normalized);

        if (_wobbleCoroutine != null) StopCoroutine(_wobbleCoroutine);
        _wobbleCoroutine = StartCoroutine(WobbleRoutine(axis));
    }

    // Propriété pour savoir si le Wobble est en cours
    public bool IsWobbling => _wobbleCoroutine != null;

    IEnumerator WobbleRoutine(Vector3 axis) {
        Quaternion target = Quaternion.AngleAxis(wobbleIntensity, axis);
        float t = 0;
        while (t < 1) {
            t += Time.deltaTime * 20f;
            pivot.localRotation = Quaternion.Lerp(pivot.localRotation, target, t);
            yield return null;
        }
        while (Quaternion.Angle(pivot.localRotation, Quaternion.identity) > 0.1f)
    {
        // AJOUT : Si le script est désactivé (KO), on arrête tout de suite !
        if (!this.enabled) yield break; 

        pivot.localRotation = Quaternion.Lerp(pivot.localRotation, Quaternion.identity, Time.deltaTime * recoverSpeed);
        yield return null;
    }
    
    if (this.enabled) pivot.localRotation = Quaternion.identity;
    _wobbleCoroutine = null;
    }
}