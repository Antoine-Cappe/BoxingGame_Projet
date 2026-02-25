using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class BoxerAgent : Agent
{
    [Header("Références")]
    public BoxerAgent opponent; 
    public Transform spawnPoint; // Un objet vide pour marquer la position de départ

    private BoxerMovement _mvmt;
    private BoxerCombat _combat;
    private BoxerHealth _health;
    private BoxerVisuals _visuals;
    private Rigidbody _rb;

    public override void Initialize()
    {
        _mvmt = GetComponent<BoxerMovement>();
        _combat = GetComponent<BoxerCombat>();
        _health = GetComponent<BoxerHealth>();
        _visuals = GetComponent<BoxerVisuals>();
        _rb = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        // 1. Reset de la santé, du collider et du Tag
        _health.ResetHealth();

        // 2. Réactivation des scripts
        _mvmt.enabled = true;
        _combat.enabled = true;
        _visuals.enabled = true;

        // 3. Arrêt des mouvements et reset interne du combat
        // On appelle la fonction de BoxerCombat pour contourner l'erreur de "set accessor"
        StopAllCoroutines();
        _combat.ResetCombatState(); 
        _mvmt.StopAllCoroutines();
        
        // Reset des inputs pour éviter que l'IA ne commence le round en "visant" déjà
        _mvmt.aimInputLeft = Vector2.zero;
        _mvmt.aimInputRight = Vector2.zero;

        // 4. Téléportation au spawn (Position et Rotation)
        if (spawnPoint != null)
        {
            transform.localPosition = spawnPoint.localPosition;
            transform.localRotation = spawnPoint.localRotation;
        }
        
        // Remettre le pivot (corps) bien droit
        if (_mvmt.pivot != null)
            _mvmt.pivot.localRotation = Quaternion.identity;

        // 5. Reset physique complet
        if (_rb != null)
        {
            _rb.isKinematic = false; // On désactive temporairement pour vider les forces
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true;
            _rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // --- OBSERVATIONS DE SOI-MÊME ---
        // Position des gants (6 floats)
        sensor.AddObservation(_mvmt.leftGlove.localPosition);
        sensor.AddObservation(_mvmt.rightGlove.localPosition);
        
        // État des poings (2 floats)
        sensor.AddObservation(_combat.isPunchingLeft ? 1f : 0f);
        sensor.AddObservation(_combat.isPunchingRight ? 1f : 0f);

        // Rotation du pivot (1 float)
        sensor.AddObservation(transform.localRotation.eulerAngles.z / 180f - 1f); // Normalisé entre -1 et 1

        // --- OBSERVATIONS DE L'ADVERSAIRE ---
        // Rotation relative de l'adversaire (1 float)
        Quaternion relativeRot = Quaternion.Inverse(transform.rotation) * opponent.transform.rotation;
        sensor.AddObservation(relativeRot.eulerAngles.z / 180f - 1f); // Normalisé entre -1 et 1

        // Position des gants adverses (6 floats)
        sensor.AddObservation(opponent._mvmt.leftGlove.localPosition);
        sensor.AddObservation(opponent._mvmt.rightGlove.localPosition);

        // L'adversaire est-il en train de frapper ? (2 floats)
        sensor.AddObservation(opponent._combat.isPunchingLeft ? 1f : 0f);
        sensor.AddObservation(opponent._combat.isPunchingRight ? 1f : 0f);

        // Distance (1 float)
        // sensor.AddObservation(Vector3.Distance(transform.position, opponent.transform.position));
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // --- ACTIONS CONTINUES : Visée des gants (4 actions) ---
        // On mappe les valeurs de l'IA (-1 à 1) sur les inputs de visée
        _mvmt.aimInputLeft = new Vector2(actions.ContinuousActions[0], actions.ContinuousActions[1]);
        _mvmt.aimInputRight = new Vector2(actions.ContinuousActions[2], actions.ContinuousActions[3]);

        // --- ACTIONS DISCRÈTES : Coups et Esquives ---
        // Branche 0 : Frappe (0: rien, 1: Gauche, 2: Droite)
        int punchAction = actions.DiscreteActions[0];
        if (punchAction == 1) _combat.PunchLeft();
        if (punchAction == 2) _combat.PunchRight();

        // Branche 1 : Esquive (0: rien, 1: Gauche, 2: Droite)
        int dodgeAction = actions.DiscreteActions[1];
        if (dodgeAction == 1) _mvmt.TriggerDodge(1f);
        if (dodgeAction == 2) _mvmt.TriggerDodge(-1f);

        // Petite pénalité de temps pour encourager l'agressivité
        AddReward(-0.0005f);
    }

    // Pour tester manuellement avec le clavier
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        var discreteActions = actionsOut.DiscreteActions;

        // Visée Bras Gauche (ZQSD)
        continuousActions[0] = Input.GetAxisRaw("Horizontal");
        continuousActions[1] = Input.GetAxisRaw("Vertical");

        // Frappe (Espace ou clics)
        discreteActions[0] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    }

    // Appelée par BoxerHealth lors du KO
    public void AgentKO()
{
    // On donne les récompenses
    SetReward(-1.0f); // Je suis KO, je perds
    opponent.SetReward(1.0f); // Mon adversaire gagne

    // IMPORTANT : On demande la fin de l'épisode pour les DEUX
    // Cela va déclencher OnEpisodeBegin() sur les deux agents.
    EndEpisode();
    opponent.EndEpisode();
}
}