using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine.InputSystem;
using System.Collections;

public class BoxerAgent : Agent
{
    [Header("Environnement")]
    public GameObject opponent;
    public Transform mySpawn;
    public Transform opponentSpawn;

    private BoxerMovement _mvmt;
    private BoxerCombat _combat;
    private BoxerHealth _health;
    private Rigidbody _rb;

    private BoxerMovement _oppMvmt;
    private BoxerHealth _oppHealth;

    private bool _isEndingEpisode = false;
    private bool _victoryReached = false; // Pour savoir si l'épisode s'est fini par un KO

    public override void Initialize()
    {
        _mvmt = GetComponent<BoxerMovement>();
        _combat = GetComponent<BoxerCombat>();
        _health = GetComponent<BoxerHealth>();
        _rb = GetComponent<Rigidbody>();

        if (opponent != null)
        {
            _oppMvmt = opponent.GetComponent<BoxerMovement>();
            _oppHealth = opponent.GetComponent<BoxerHealth>();
        }
    }

    public override void OnEpisodeBegin()
    {
        // --- STATISTIQUE DE TIMEOUT ---
        // Si l'épisode recommence sans que _isEndingEpisode n'ait été mis à true, 
        // cela signifie que nous avons atteint le MaxStep (Timeout)
        if (!_isEndingEpisode && _mvmt != null)
        {
            Academy.Instance.StatsRecorder.Add("Combat/KO_Rate", 0f);
        }

        _isEndingEpisode = false;
        _victoryReached = false;
        StopAllCoroutines();

        ResetBoxer(gameObject, _health, _mvmt, _combat, _rb, mySpawn);

        if (opponent != null)
        {
            BoxerCombat oppCombat = opponent.GetComponent<BoxerCombat>();
            Rigidbody oppRb = opponent.GetComponent<Rigidbody>();
            ResetBoxer(opponent, _oppHealth, _oppMvmt, oppCombat, oppRb, opponentSpawn);
        }
    }

    private void ResetBoxer(GameObject obj, BoxerHealth h, BoxerMovement m, BoxerCombat c, Rigidbody r, Transform s)
    {
        h.ResetHealth();
        m.ResetMovementState(); 
        c.ResetCombatState();
        
        m.enabled = true;
        c.enabled = true;

        if (s != null) {
            // Variation aléatoire du spawn pour éviter le sur-apprentissage
            Vector3 randomOffset = new Vector3(Random.Range(-0.5f, 0.5f), 0, Random.Range(-0.5f, 0.5f));
            obj.transform.localPosition = s.localPosition + randomOffset;

            float randomYaw = Random.Range(-15f, 15f);
            obj.transform.localRotation = s.localRotation * Quaternion.Euler(0, randomYaw, 0);
        }

        if (r != null) {
            r.linearVelocity = Vector3.zero;
            r.angularVelocity = Vector3.zero;
            r.isKinematic = false; 
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // --- TOTAL OBSERVATIONS : 22 ---
        Vector3 dirToOpponent = (opponent.transform.position - transform.position);
        sensor.AddObservation(dirToOpponent.normalized); // 3
        sensor.AddObservation(dirToOpponent.magnitude); // 1
        
        sensor.AddObservation(Vector3.Dot(transform.forward, dirToOpponent.normalized)); // 1
        sensor.AddObservation(Vector3.Dot(opponent.transform.forward, -dirToOpponent.normalized)); // 1

        sensor.AddObservation(_mvmt.leftGuardState / 2f); // 1
        sensor.AddObservation(_mvmt.rightGuardState / 2f); // 1
        sensor.AddObservation(_combat.isPunchingLeft ? 1f : 0f); // 1
        sensor.AddObservation(_combat.isPunchingRight ? 1f : 0f); // 1
        sensor.AddObservation(_mvmt.isDodging ? 1f : 0f); // 1

        sensor.AddObservation(_oppMvmt.leftGuardState / 2f); // 1
        sensor.AddObservation(_oppMvmt.rightGuardState / 2f); // 1
        sensor.AddObservation(opponent.GetComponent<BoxerCombat>().isPunchingLeft ? 1f : 0f); // 1
        sensor.AddObservation(opponent.GetComponent<BoxerCombat>().isPunchingRight ? 1f : 0f); // 1
        sensor.AddObservation(_oppMvmt.isDodging ? 1f : 0f); // 1
        
        sensor.AddObservation(_mvmt.leftGlove.localPosition); // 3
        sensor.AddObservation(_mvmt.rightGlove.localPosition); // 3
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (_isEndingEpisode) return;

        var discrete = actions.DiscreteActions;

        if (discrete[0] == 1) _combat.PunchLeft();
        if (discrete[0] == 2) _combat.PunchRight();

        if (discrete[1] == 1) _mvmt.TriggerDodge(1f);
        if (discrete[1] == 2) _mvmt.TriggerDodge(-1f);

        _mvmt.leftGuardState = discrete[2];
        _mvmt.rightGuardState = discrete[3];

        _mvmt.moveInput.y = ConvertToDirect(discrete[4]);
        _mvmt.moveInput.x = ConvertToDirect(discrete[5]);
        _mvmt.rotateInput = ConvertToDirect(discrete[6]);

        CalculateRewards();
    }

    private void CalculateRewards()
    {
        if (opponent == null) return;

        // 1. RÉCOMPENSE DE DISTANCE (Entre 2.1 et 3.2m car radius de 1+1 = 2)
        float dist = Vector3.Distance(transform.position, opponent.transform.position);
        if (dist >= 2.1f && dist <= 3.2f) 
        {
            AddReward(0.0002f); 
        }
        else if (dist > 6f) 
        {
            AddReward(-0.0005f);
        }

        // 2. RÉCOMPENSE D'ORIENTATION (Face à face)
        Vector3 dirToOpp = (opponent.transform.position - transform.position).normalized;
        float lookAtDot = Vector3.Dot(transform.forward, dirToOpp);
        if (lookAtDot > 0.95f) 
        {
            AddReward(0.0002f); 
        }

        // 3. VICTOIRE (KO de l'adversaire)
        if (_oppHealth != null && _oppHealth.IsKO && !_isEndingEpisode) 
        {
            AddReward(1.0f); 
            StartCoroutine(WaitAndEndEpisode(true)); // Terminé par KO
        }
    }

    private float ConvertToDirect(int val)
    {
        if (val == 1) return 1f;
        if (val == 2) return -1f;
        return 0f;
    }

    public void AgentKO()
    {
        if (_isEndingEpisode) return;
        AddReward(-1.0f); 
        StartCoroutine(WaitAndEndEpisode(true)); // Terminé par KO
    }

    private IEnumerator WaitAndEndEpisode(bool isKO)
    {
        _isEndingEpisode = true;
        _victoryReached = isKO;

        // Envoi de la statistique à TensorBoard (1 = KO, 0 = Timeout)
        Academy.Instance.StatsRecorder.Add("Combat/KO_Rate", isKO ? 1f : 0f);

        yield return new WaitForSeconds(1.5f);
        EndEpisode();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discrete = actionsOut.DiscreteActions;
        for (int i = 0; i < discrete.Length; i++) discrete[i] = 0;

        if (Keyboard.current.fKey.isPressed) discrete[0] = 1;
        if (Keyboard.current.gKey.isPressed) discrete[0] = 2;

        if (Keyboard.current.iKey.isPressed) discrete[2] = 1;
        else if (Keyboard.current.kKey.isPressed) discrete[2] = 2;

        if (Keyboard.current.oKey.isPressed) discrete[3] = 1;
        else if (Keyboard.current.lKey.isPressed) discrete[3] = 2;

        if (Keyboard.current.wKey.isPressed) discrete[4] = 1;
        else if (Keyboard.current.sKey.isPressed) discrete[4] = 2;

        if (Keyboard.current.dKey.isPressed) discrete[5] = 1;
        else if (Keyboard.current.aKey.isPressed) discrete[5] = 2;

        if (Keyboard.current.eKey.isPressed) discrete[6] = 1;
        else if (Keyboard.current.qKey.isPressed) discrete[6] = 2;
    }
}