using UnityEngine;
using UnityEngine.InputSystem;

public class BoxerInputHandler : MonoBehaviour
{
    private BoxerMovement _mvmt;
    private BoxerCombat _combat;

    void Awake() {
        _mvmt = GetComponent<BoxerMovement>();
        _combat = GetComponent<BoxerCombat>();
    }

    void Update() {
        // --- VISÉE (Gardée en continu) ---
        float moveX = 0;
        float moveY = 0;
        if (Keyboard.current.leftArrowKey.isPressed) moveX = -1;
        if (Keyboard.current.rightArrowKey.isPressed) moveX = 1;
        if (Keyboard.current.upArrowKey.isPressed) moveY = 1;
        if (Keyboard.current.downArrowKey.isPressed) moveY = -1;
        
        _mvmt.aimInput = new Vector2(moveX, moveY);

        // --- ESQUIVE (Impulsion) ---
        // On détecte la pression unique sur les flèches Gauche/Droite
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            _mvmt.TriggerDodge(1f); // Penche à gauche
        }
        else if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            _mvmt.TriggerDodge(-1f); // Penche à droite
        }

        // --- COMBAT ---
        if (Keyboard.current.aKey.wasPressedThisFrame) _combat.PunchLeft();
        if (Keyboard.current.dKey.wasPressedThisFrame) _combat.PunchRight();
    }
}