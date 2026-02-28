using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System.Collections;

public class BoxerAgent : Agent
{
    [Header("Environnement")]
    public GameObject opponent;
    public Transform mySpawn;
    public Transform opponentSpawn;

    [Header("Réglages IA")]
    public float optimalFightRange = 2.0f;
    public float rangePenaltyWeight = 0.002f;

    private BoxerMovement _mvmt;
    private BoxerCombat _combat;
    private BoxerHealth _health;
    private Rigidbody _rb;

    private BoxerMovement _oppMvmt;
    private BoxerCombat _oppCombat;
    private BoxerHealth _oppHealth;
    private Rigidbody _oppRb;

    private bool _isEndingEpisode = false;

    // ─────────────────────────────────────────────────────────────
    //  INITIALISATION
    // ─────────────────────────────────────────────────────────────
    public override void Initialize()
    {
        _mvmt = GetComponent<BoxerMovement>();
        _combat = GetComponent<BoxerCombat>();
        _health = GetComponent<BoxerHealth>();
        _rb = GetComponent<Rigidbody>();

        if (opponent != null)
        {
            _oppMvmt = opponent.GetComponent<BoxerMovement>();
            _oppCombat = opponent.GetComponent<BoxerCombat>();
            _oppHealth = opponent.GetComponent<BoxerHealth>();
            _oppRb = opponent.GetComponent<Rigidbody>();
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  DÉBUT D'ÉPISODE
    // ─────────────────────────────────────────────────────────────
    public override void OnEpisodeBegin()
    {
        _isEndingEpisode = false;
        StopAllCoroutines();

        ResetBoxer(gameObject, _health, _mvmt, _combat, _rb, mySpawn);

        if (opponent != null)
            ResetBoxer(opponent, _oppHealth, _oppMvmt, _oppCombat, _oppRb, opponentSpawn);
    }

    private void ResetBoxer(GameObject obj, BoxerHealth h, BoxerMovement m,
                             BoxerCombat c, Rigidbody r, Transform s)
    {
        h.ResetHealth();
        m.ResetMovementState();
        c.ResetCombatState();
        m.enabled = true;
        c.enabled = true;

        if (s != null)
        {
            obj.transform.localPosition = s.localPosition;
            obj.transform.localRotation = s.localRotation;
        }

        if (r != null)
        {
            if (!r.isKinematic)
            {
                r.linearVelocity = Vector3.zero;
                r.angularVelocity = Vector3.zero;
            }
            r.isKinematic = true;
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  OBSERVATIONS  (Space Size = 32)
    // ─────────────────────────────────────────────────────────────
    public override void CollectObservations(VectorSensor sensor)
    {
        // ── SOI-MÊME (9 valeurs) ─────────────────────────────────
        sensor.AddObservation(_mvmt.leftGlove.localPosition);           // 3
        sensor.AddObservation(_mvmt.rightGlove.localPosition);          // 3
        sensor.AddObservation(_combat.isPunchingLeft ? 1f : 0f);       // 1
        sensor.AddObservation(_combat.isPunchingRight ? 1f : 0f);       // 1
        sensor.AddObservation(_mvmt.isDodging ? 1f : 0f);       // 1

        // ── ADVERSAIRE (9 valeurs) ───────────────────────────────
        sensor.AddObservation(_oppMvmt.leftGlove.localPosition);        // 3
        sensor.AddObservation(_oppMvmt.rightGlove.localPosition);       // 3
        sensor.AddObservation(_oppCombat.isPunchingLeft ? 1f : 0f);    // 1
        sensor.AddObservation(_oppCombat.isPunchingRight ? 1f : 0f);    // 1
        sensor.AddObservation(_oppMvmt.isDodging ? 1f : 0f);    // 1

        // ── POSITIONNEMENT (12 valeurs) ──────────────────────────
        Vector3 toOpp = opponent.transform.position - transform.position;
        float distance = toOpp.magnitude;

        sensor.AddObservation(transform.InverseTransformDirection(toOpp.normalized)); // 3
        sensor.AddObservation(Mathf.Clamp(distance / 10f, 0f, 1f));                  // 1
        sensor.AddObservation(Vector3.Dot(transform.forward, toOpp.normalized));      // 1
        sensor.AddObservation(Vector3.Dot(transform.right, toOpp.normalized));      // 1
        sensor.AddObservation(transform.localPosition.x / 5f);                       // 1
        sensor.AddObservation(transform.localPosition.z / 5f);                       // 1
        sensor.AddObservation(Vector3.Dot(opponent.transform.forward,
                                          -toOpp.normalized));                        // 1
        sensor.AddObservation(_mvmt.moveInput.x);                                     // 1
        sensor.AddObservation(_mvmt.moveInput.y);                                     // 1
        sensor.AddObservation(_mvmt.rotateInput);                                     // 1

        // ── GARDE (2 valeurs) ────────────────────────────────────
        sensor.AddObservation(EncodeGuard(_mvmt.currentGuard));         // 1
        sensor.AddObservation(EncodeGuard(_oppMvmt.currentGuard));      // 1

        // TOTAL = 32
    }

    private float EncodeGuard(GuardType guard)
    {
        switch (guard)
        {
            case GuardType.High: return 0.5f;
            case GuardType.Low: return 1.0f;
            default: return 0.0f;
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  ACTIONS (10 branches de taille 3)
    // ─────────────────────────────────────────────────────────────
    public override void OnActionReceived(ActionBuffers actions)
    {
        if (_isEndingEpisode) return;

        // 1. Visée des gants
        _mvmt.aimInputLeft.x = ConvertDiscrete(actions.DiscreteActions[2]);
        _mvmt.aimInputLeft.y = ConvertDiscrete(actions.DiscreteActions[3]);
        _mvmt.aimInputRight.x = ConvertDiscrete(actions.DiscreteActions[4]);
        _mvmt.aimInputRight.y = ConvertDiscrete(actions.DiscreteActions[5]);

        // 2. Punch
        int punch = actions.DiscreteActions[0];
        if (punch == 1) _combat.PunchLeft();
        if (punch == 2) _combat.PunchRight();

        // 3. Esquive
        int dodge = actions.DiscreteActions[1];
        if (dodge == 1) _mvmt.TriggerDodge(1f);
        if (dodge == 2) _mvmt.TriggerDodge(-1f);

        // 4. Déplacement
        _mvmt.moveInput.x = ConvertDiscrete(actions.DiscreteActions[6]);
        _mvmt.moveInput.y = ConvertDiscrete(actions.DiscreteActions[7]);

        // 5. Rotation
        _mvmt.rotateInput = ConvertDiscrete(actions.DiscreteActions[8]);

        // 6. Garde
        int guard = actions.DiscreteActions[9];
        if (guard == 1) _mvmt.guardInput = GuardType.High;
        else if (guard == 2) _mvmt.guardInput = GuardType.Low;
        else _mvmt.guardInput = GuardType.None;

        // ── RÉCOMPENSES ──────────────────────────────────────────

        // 7. Pénalité distance
        float dist = Vector3.Distance(transform.position, opponent.transform.position);
        float distExcess = Mathf.Max(0f, dist - optimalFightRange);
        AddReward(-distExcess * rangePenaltyWeight);

        // 8. Récompense orientation — faire face à l'adversaire
        Vector3 toOpp = (opponent.transform.position - transform.position).normalized;
        float facingDot = Vector3.Dot(transform.forward, toOpp);
        AddReward(Mathf.Max(0f, facingDot) * 0.001f);

        // 9. Récompense garde intelligente
        if ((_oppCombat.isPunchingLeft || _oppCombat.isPunchingRight)
            && _mvmt.currentGuard != GuardType.None)
        {
            AddReward(0.01f);
        }

        // 10. VICTOIRE
        if (_oppHealth != null && _oppHealth.IsKO && !_isEndingEpisode)
        {
            SetReward(1.0f);
            StartCoroutine(WaitAndEndEpisode());
        }
    }

    // ──────────────────────────────────────────────��──────────────
    //  UTILITAIRES
    // ─────────────────────────────────────────────────────────────
    private float ConvertDiscrete(int v)
    {
        if (v == 1) return 1f;
        if (v == 2) return -1f;
        return 0f;
    }

    public void AgentKO()
    {
        if (_isEndingEpisode) return;
        SetReward(-1.0f);
        StartCoroutine(WaitAndEndEpisode());
    }

    private IEnumerator WaitAndEndEpisode()
    {
        _isEndingEpisode = true;
        yield return new WaitForSeconds(1.5f);
        EndEpisode();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // Laissé vide : BoxerInputHandler gère les inputs humains
    }
}