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
        // --- BRAS GAUCHE (ZQSD / WASD) ---
        float leftX = 0, leftY = 0;
        if (Keyboard.current.aKey.isPressed) leftX = -0.5f;
        if (Keyboard.current.dKey.isPressed) leftX = 0.5f;
        if (Keyboard.current.wKey.isPressed) leftY = 1;
        if (Keyboard.current.sKey.isPressed) leftY = -0.25f;
        _mvmt.aimInputLeft = new Vector2(leftX, leftY);

        // --- BRAS DROIT (Flèches) ---
        float rightX = 0, rightY = 0;
        if (Keyboard.current.leftArrowKey.isPressed) rightX = -0.5f;
        if (Keyboard.current.rightArrowKey.isPressed) rightX = 0.5f;
        if (Keyboard.current.upArrowKey.isPressed) rightY = 1;
        if (Keyboard.current.downArrowKey.isPressed) rightY = -0.25f;
        _mvmt.aimInputRight = new Vector2(rightX, rightY);

        // --- ESQUIVE (Basée sur l'input du bras gauche par exemple) ---
        if (Keyboard.current.leftShiftKey.wasPressedThisFrame) _mvmt.TriggerDodge(1f); 
        if (Keyboard.current.rightShiftKey.wasPressedThisFrame) _mvmt.TriggerDodge(-1f);

        // --- COMBAT ---
        if (Keyboard.current.qKey.wasPressedThisFrame) _combat.PunchLeft();
        if (Keyboard.current.eKey.wasPressedThisFrame) _combat.PunchRight(); // Changé 'D' en 'F' pour libérer les touches
    }
}