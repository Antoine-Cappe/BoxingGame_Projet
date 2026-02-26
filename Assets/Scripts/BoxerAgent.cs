using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System.Collections;

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

    private bool _isEndingEpisode = false;

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
        _isEndingEpisode = false;
        StopAllCoroutines();

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
        
        // Utilise la nouvelle méthode de nettoyage ici !
        m.ResetMovementState(); 
        
        c.ResetCombatState();
        
        m.enabled = true;
        c.enabled = true;

        if (s != null) {
            obj.transform.localPosition = s.localPosition;
            obj.transform.localRotation = s.localRotation;
        }

        if (r != null) {
            if (!r.isKinematic) {
                r.linearVelocity = Vector3.zero;
                r.angularVelocity = Vector3.zero;
            }
            r.isKinematic = true;
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Space Size = 18
        // --- SOI-MÊME ---
        sensor.AddObservation(_mvmt.leftGlove.localPosition); // 3
        sensor.AddObservation(_mvmt.rightGlove.localPosition); // 3
        sensor.AddObservation(_combat.isPunchingLeft ? 1f : 0f); // 1
        sensor.AddObservation(_combat.isPunchingRight ? 1f : 0f); // 1
        sensor.AddObservation(_mvmt.isDodging ? 1f : 0f); // 1

        // --- ADVERSAIRE ---
        sensor.AddObservation(_oppMvmt.leftGlove.localPosition); // 3
        sensor.AddObservation(_oppMvmt.rightGlove.localPosition); // 3
        sensor.AddObservation(_oppCombat.isPunchingLeft ? 1f : 0f); // 1
        sensor.AddObservation(_oppCombat.isPunchingRight ? 1f : 0f); // 1
        sensor.AddObservation(_oppMvmt.isDodging ? 1f : 0f); // 1
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (_isEndingEpisode) return;

        // Actions continues : Visée
        _mvmt.aimInputLeft = new Vector2(actions.ContinuousActions[0], actions.ContinuousActions[1]);
        _mvmt.aimInputRight = new Vector2(actions.ContinuousActions[2], actions.ContinuousActions[3]);

        // Actions discrètes
        int punch = actions.DiscreteActions[0];
        if (punch == 1) _combat.PunchLeft();
        if (punch == 2) _combat.PunchRight();

        int dodge = actions.DiscreteActions[1];
        if (dodge == 1) _mvmt.TriggerDodge(1f);
        if (dodge == 2) _mvmt.TriggerDodge(-1f);

        // Pénalité de temps
        AddReward(-0.0001f);

        // VICTOIRE : Si le bot tombe KO
        if (_oppHealth != null && _oppHealth.IsKO && !_isEndingEpisode) 
        {
            SetReward(1.0f);
            StartCoroutine(WaitAndEndEpisode());
        }
    }

    // Appelé par BoxerHealth si l'AGENT (IA) est mis KO
    public void AgentKO()
    {
        if (_isEndingEpisode) return;
        SetReward(-1.0f);
        StartCoroutine(WaitAndEndEpisode());
    }

    private IEnumerator WaitAndEndEpisode()
    {
        _isEndingEpisode = true;
        // On attend que la capsule physique finisse son vol
        yield return new WaitForSeconds(1.5f);
        EndEpisode();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // (Remplir ici avec BoxerInputHandler si tu veux tester l'agent au clavier)
    }
}