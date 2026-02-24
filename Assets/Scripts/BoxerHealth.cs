using UnityEngine;
using System.Collections;

public class BoxerHealth : MonoBehaviour
{
    [Header("Réglages Santé")]
    public int maxHits = 10; 
    private int _currentHits;
    private bool _isKO = false;
    private Vector3 _lastHitDirection; // Stocke la direction du dernier impact

    [Header("Références")]
    public Transform pivot; 

    private CapsuleCollider _mainCollider;

    void Awake() {
        _currentHits = 0;
        _mainCollider = GetComponent<CapsuleCollider>(); // On récupère la capsule
    }

    // On ajoute le paramètre hitDirection ici
    public void TakeDamage(Vector3 hitDirection)
    {
        if (_isKO) return;

        _lastHitDirection = hitDirection;
        _currentHits++;

        if (_currentHits >= maxHits)
        {
            TriggerKO();
        }
    }

    private void TriggerKO() {
        if (_isKO) return; // Sécurité pour ne pas déclencher deux fois
        _isKO = true;

        // 1. On récupère TOUS les scripts de logique
        BoxerMovement mvmt = GetComponent<BoxerMovement>();
        BoxerCombat combat = GetComponent<BoxerCombat>();
        BoxerVisuals visuals = GetComponent<BoxerVisuals>();

        // 2. On les stoppe et on les désactive FERMEMENT
        if (mvmt != null) { mvmt.StopAllCoroutines(); mvmt.enabled = false; }
        if (combat != null) { combat.StopAllCoroutines(); combat.enabled = false; }
        if (visuals != null) { visuals.StopAllCoroutines(); visuals.enabled = false; }

        if (_mainCollider != null) {
            // Option A : Désactivation totale (Attention à la chute à travers le sol !)
            _mainCollider.enabled = false; 
            
            // Option B (Recommandée) : Changer le Layer pour "Ignore Raycast" ou un layer mort
            // gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        }
        
        // On passe la direction à la routine de chute
        StartCoroutine(FallRoutine(_lastHitDirection));
    }

    IEnumerator FallRoutine(Vector3 worldHitDir)
    {
        // 1. Convertir la direction mondiale du coup en direction LOCALE
        // Indispensable si l'agent est tourné (ex: face à l'autre joueur)
        Vector3 localHitDir = transform.InverseTransformDirection(worldHitDir);
        localHitDir.y = 0; // On reste sur le plan horizontal pour l'axe de chute
        localHitDir.Normalize();

        // 2. Calculer l'axe de basculement (perpendiculaire à l'impact)
        // Si le coup vient de devant, l'axe sera "droite/gauche" (X local)
        Vector3 fallAxis = Vector3.Cross(Vector3.up, localHitDir);

        Quaternion startRot = pivot.localRotation;
        
        // 3. Calculer la rotation cible (85 degrés sur l'axe de chute)
        // On ajoute un petit décalage aléatoire pour éviter les chutes trop robotiques
        float randomTwist = Random.Range(-45f, 45f);
        Quaternion endRot = Quaternion.AngleAxis(85f, fallAxis) * Quaternion.Euler(0, randomTwist, 0);
        
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * 2.5f; 
            pivot.localRotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }
        
        pivot.localRotation = endRot;
    }

    // Méthode utile pour ML-Agents ou UI
    public bool IsKO() => _isKO;
}