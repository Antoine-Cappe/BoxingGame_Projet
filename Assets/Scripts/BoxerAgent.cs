using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class BoxerAgent : Agent
{
    [Header("Environnement")]
    public GameObject opponent; // Ton Bot (RandomAgent)
    public Transform mySpawn;
    public Transform opponentSpawn;

    private BoxerMovement _mvmt;
    private BoxerCombat _combat;
    private BoxerHealth _health;
    private Rigidbody _rb;

    // Références du Bot pour les observations et le reset
    private BoxerMovement _oppMvmt;
    private BoxerCombat _oppCombat;
    private BoxerHealth _oppHealth;
    private Rigidbody _oppRb;

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

    public override void OnEpisodeBegin()
    {
        // 1. Reset de l'IA
        ResetBoxer(gameObject, _health, _mvmt, _combat, _rb, mySpawn);

        // 2. Reset du BOT
        if (opponent != null)
        {
            ResetBoxer(opponent, _oppHealth, _oppMvmt, _oppCombat, _oppRb, opponentSpawn);
        }
    }

    private void ResetBoxer(GameObject obj, BoxerHealth h, BoxerMovement m, BoxerCombat c, Rigidbody r, Transform s)
    {
        h.ResetHealth();
        m.enabled = true;
        c.enabled = true;
        m.StopAllCoroutines();
        c.ResetCombatState();
        
        if (s != null) {
            obj.transform.localPosition = s.localPosition;
            obj.transform.localRotation = s.localRotation;
        }

        if (r != null) {
            r.isKinematic = false;
            r.linearVelocity = Vector3.zero;
            r.angularVelocity = Vector3.zero;
            r.isKinematic = true;
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // --- SOI-MÊME ---
        sensor.AddObservation(_mvmt.leftGlove.localPosition);
        sensor.AddObservation(_mvmt.rightGlove.localPosition);
        sensor.AddObservation(_combat.isPunchingLeft ? 1f : 0f);
        sensor.AddObservation(_combat.isPunchingRight ? 1f : 0f);
        sensor.AddObservation(_mvmt.isDodging ? 1f : 0f); 

        // --- ADVERSAIRE ---
        sensor.AddObservation(_oppMvmt.leftGlove.localPosition);
        sensor.AddObservation(_oppMvmt.rightGlove.localPosition);
        sensor.AddObservation(_oppCombat.isPunchingLeft ? 1f : 0f);
        sensor.AddObservation(_oppCombat.isPunchingRight ? 1f : 0f);
        sensor.AddObservation(_oppMvmt.isDodging ? 1f : 0f);

    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Actions continues : Visée
        _mvmt.aimInputLeft = new Vector2(actions.ContinuousActions[0], actions.ContinuousActions[1]);
        _mvmt.aimInputRight = new Vector2(actions.ContinuousActions[2], actions.ContinuousActions[3]);

        // Actions discrètes : Punch et Esquive
        int punch = actions.DiscreteActions[0];
        if (punch == 1) _combat.PunchLeft();
        if (punch == 2) _combat.PunchRight();

        int dodge = actions.DiscreteActions[1];
        if (dodge == 1) _mvmt.TriggerDodge(1f);
        if (dodge == 2) _mvmt.TriggerDodge(-1f);

        // Pénalité de temps très légère
        AddReward(-0.0001f);

        // VICTOIRE : Si le bot est KO
        if (_oppHealth != null && _oppHealth.IsKO()) 
        {
            AddReward(1.0f);
            EndEpisode();
        }
    }

    // Appelé par BoxerHealth si l'IA est KO
    public void AgentKO()
    {
        AddReward(-1.0f);
        EndEpisode();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // (Ton code Heuristic pour tester au clavier)
    }
}