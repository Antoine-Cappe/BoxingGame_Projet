using UnityEngine;

public class BoxerHealth : MonoBehaviour
{
    [Header("Réglages Santé")]
    public int maxHits = 10;
    private int _currentHits;
    private bool _isKO = false;
    private Vector3 _lastHitDirection;

    [Header("Physique du KO")]
    public GameObject ragdollPrefab; // Ton prefab de capsule physique
    public float knockbackForce = 15f; // Force de recul
    public float liftForce = 5f;      // Force vers le haut pour l'effet "volant"
    
    private GameObject _spawnedRagdoll;
    private CapsuleCollider _mainCollider;
    
    [Header("Références")]
    public GameObject visualsRoot; // L'objet parent de tout ton visuel (Mesh, gants, etc.)

    void Awake() {
        _mainCollider = GetComponent<CapsuleCollider>();
        _currentHits = 0;
    }

    public void TakeDamage(Vector3 hitDirection)
    {
        if (_isKO) return;

        _lastHitDirection = hitDirection;
        _currentHits++;

        // Récompense négative pour l'IA (instinct de survie)
        BoxerAgent agent = GetComponent<BoxerAgent>();
        if (agent != null) {
            //agent.AddReward(-0.1f); 
        }

        if (_currentHits >= maxHits) {
            TriggerKO();
        } else {
            // Si pas KO, on déclenche juste le tremblement visuel
            BoxerVisuals visuals = GetComponent<BoxerVisuals>();
            if (visuals != null && visuals.enabled) {
                visuals.TriggerWobble(hitDirection);
            }
        }
    }

    private void TriggerKO() {
        if (_isKO) return;
        _isKO = true;

        // 1. Désactivation de la logique et du visuel
        if (_mainCollider != null) _mainCollider.enabled = false;
        if (visualsRoot != null) visualsRoot.SetActive(false);
        
        // On stoppe les mouvements et le combat
        BoxerMovement mvmt = GetComponent<BoxerMovement>();
        BoxerCombat combat = GetComponent<BoxerCombat>();
        if (mvmt != null) mvmt.enabled = false;
        if (combat != null) combat.enabled = false;

        // 2. Apparition du corps physique (Ragdoll)
        if (ragdollPrefab != null) {
            _spawnedRagdoll = Instantiate(ragdollPrefab, transform.position, transform.rotation);
            Rigidbody rb = _spawnedRagdoll.GetComponent<Rigidbody>();
            
            if (rb != null) {
                // --- OPTIMISATION DE L'IMPACT ---
                
                // On applique la force un peu plus haut que le centre (ex: à 0.8 unité de haut)
                // Cela crée un "levier" qui fait basculer la capsule instantanément.
                Vector3 impactPoint = _spawnedRagdoll.transform.position + Vector3.up * 0.8f;
                
                // Force de recul (direction du coup + un peu de hauteur)
                Vector3 force = _lastHitDirection.normalized * knockbackForce + Vector3.up * liftForce;
                
                // On utilise AddForceAtPosition pour créer le basculement naturel
                rb.AddForceAtPosition(force, impactPoint, ForceMode.Impulse);

                // Au lieu d'un torque aléatoire, on force une rotation "en arrière" 
                // par rapport à la direction du coup (Backflip)
                Vector3 torqueAxis = Vector3.Cross(Vector3.up, _lastHitDirection).normalized;
                rb.AddTorque(torqueAxis * (knockbackForce * 0.5f), ForceMode.Impulse);
            }
        }

        // 3. Alerte l'Agent pour finir l'épisode (avec le délai qu'on va coder dans BoxerAgent)
        BoxerAgent myAgent = GetComponent<BoxerAgent>();
        if (myAgent != null) {
            myAgent.AgentKO();
        }
    }

    public void ResetHealth()
    {
        // Nettoyage du round précédent
        _isKO = false;
        _currentHits = 0;

        if (_spawnedRagdoll != null) {
            Destroy(_spawnedRagdoll);
        }

        // Réactivation du boxeur
        if (_mainCollider != null) _mainCollider.enabled = true;
        if (visualsRoot != null) visualsRoot.SetActive(true);
        
        BoxerMovement mvmt = GetComponent<BoxerMovement>();
        BoxerCombat combat = GetComponent<BoxerCombat>();
        if (mvmt != null) mvmt.enabled = true;
        if (combat != null) combat.enabled = true;
    }

    public bool IsKO => _isKO;
}