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
    private Coroutine _endEpisodeCoroutine;
    private float _previousDistance = -1f;

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
        
        _isEndingEpisode = false;
        StopAllCoroutines();

        // RESET INDIVIDUEL
        ResetBoxer(gameObject, _health, _mvmt, _combat, _rb, mySpawn);

        // Initialize previous distance for dense shaping
        if (opponent != null)
        {
            _previousDistance = Vector3.Distance(transform.position, opponent.transform.position);
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
        Vector3 dirToOpponent = opponent.transform.position - transform.position;
        sensor.AddObservation(dirToOpponent.normalized); 
        sensor.AddObservation(dirToOpponent.magnitude); 
        
        sensor.AddObservation(Vector3.Dot(transform.forward, dirToOpponent.normalized)); 
        sensor.AddObservation(Vector3.Dot(opponent.transform.forward, -dirToOpponent.normalized)); 

        sensor.AddObservation(_mvmt.leftGuardState / 2f); 
        sensor.AddObservation(_mvmt.rightGuardState / 2f); 
        sensor.AddObservation(_combat.isPunchingLeft ? 1f : 0f); 
        sensor.AddObservation(_combat.isPunchingRight ? 1f : 0f); 
        //sensor.AddObservation(_mvmt.isDodging ? 1f : 0f); 

        sensor.AddObservation(_oppMvmt.leftGuardState / 2f); 
        sensor.AddObservation(_oppMvmt.rightGuardState / 2f); 
        sensor.AddObservation(opponent.GetComponent<BoxerCombat>().isPunchingLeft ? 1f : 0f); 
        sensor.AddObservation(opponent.GetComponent<BoxerCombat>().isPunchingRight ? 1f : 0f); 
        //sensor.AddObservation(_oppMvmt.isDodging ? 1f : 0f); 
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (_isEndingEpisode) return;

        var discrete = actions.DiscreteActions;

        if (discrete[0] == 1) _combat.PunchLeft();
        if (discrete[0] == 2) _combat.PunchRight();

        //if (discrete[1] == 1) _mvmt.TriggerDodge(1f);
        //if (discrete[1] == 2) _mvmt.TriggerDodge(-1f);

        _mvmt.leftGuardState = discrete[1];
        _mvmt.rightGuardState = discrete[2];

        _mvmt.moveInput.y = ConvertToDirect(discrete[3]);
        _mvmt.moveInput.x = ConvertToDirect(discrete[4]);
        //_mvmt.rotateInput = ConvertToDirect(discrete[5]);

        CalculateRewards();
    }

    private void CalculateRewards()
    {
        if (opponent == null || _isEndingEpisode) return;

        // Dense reward to encourage closing the distance:
        // 1) small reward proportional to how much the agent reduced distance since last step
        // 2) small shaping reward for being close
        float dist = Vector3.Distance(transform.position, opponent.transform.position);

        if (_previousDistance < 0f) _previousDistance = dist;

        float delta = _previousDistance - dist; // positive when getting closer
        // Reward closing: scale to keep per-step reward small but meaningful
        AddReward(Mathf.Clamp(delta * 0.05f, -0.001f, 0.01f));

        // Bonus for proximity (closer -> larger bonus), normalized by a max distance
        float maxConsiderDist = 6f;
        float closeness = Mathf.Clamp01((maxConsiderDist - dist) / maxConsiderDist);
        AddReward(closeness * 0.0005f);

        _previousDistance = dist;
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

        // 1. Punition du perdant
        AddReward(-1.0f);

        // 2. Récompense du vainqueur
        BoxerAgent winner = opponent.GetComponent<BoxerAgent>();
        if (winner != null && !winner._isEndingEpisode)
        {
            //Debug.Log($"KO ! +1 pour {opponent.name} / -1 pour {gameObject.name}");
            winner.AddReward(1.0f);
            
            // FORCER LE VAINQUEUR À FINIR TOUT DE SUITE AUSSI
            winner.DirectEndEpisode(true);
        }

        DirectEndEpisode(true);
    }

    private void DirectEndEpisode(bool isKO)
    {
        if (_isEndingEpisode) return;
        _isEndingEpisode = true;
        
        if (_endEpisodeCoroutine != null) StopCoroutine(_endEpisodeCoroutine);
        _endEpisodeCoroutine = StartCoroutine(EndEpisodeAfterDelay());
        //EndEpisode();
    }

    IEnumerator EndEpisodeAfterDelay()
    {
        yield return new WaitForSecondsRealtime(3f);
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