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
        // ── DÉPLACEMENT DU CORPS (WASD) ──────────────────────────
        float moveX = 0f, moveY = 0f;
        if (Keyboard.current.aKey.isPressed) moveX = -1f;
        if (Keyboard.current.dKey.isPressed) moveX = 1f;
        if (Keyboard.current.wKey.isPressed) moveY = 1f;
        if (Keyboard.current.sKey.isPressed) moveY = -1f;
        _mvmt.moveInput = new Vector2(moveX, moveY);

        // ── ROTATION DU CORPS (Q gauche / E droite) ───────────────
        float rotate = 0f;
        if (Keyboard.current.qKey.isPressed) rotate = -1f;
        if (Keyboard.current.eKey.isPressed) rotate = 1f;
        _mvmt.rotateInput = rotate;

        // ── SYSTÈME DE GARDE (3 POSITIONS) ────────────────────────
        // On remplace les Vector2 par des états : 0=Milieu, 1=Haut, 2=Bas

        // --- GARDE GAUCHE (I = Haut, K = Bas, sinon Milieu) ---
        int leftState = 0; 
        if (Keyboard.current.iKey.isPressed) leftState = 1;
        else if (Keyboard.current.kKey.isPressed) leftState = 2;
        _mvmt.leftGuardState = leftState;

        // --- GARDE DROITE (O = Haut, L = Bas, sinon Milieu) ---
        int rightState = 0;
        if (Keyboard.current.oKey.isPressed) rightState = 1;
        else if (Keyboard.current.lKey.isPressed) rightState = 2;
        _mvmt.rightGuardState = rightState;

        // ── ESQUIVE ──────────────────────────────────────────────
        if (Keyboard.current.leftShiftKey.wasPressedThisFrame) _mvmt.TriggerDodge(1f); 
        if (Keyboard.current.rightShiftKey.wasPressedThisFrame) _mvmt.TriggerDodge(-1f);

        // ── COMBAT ───────────────────────────────────────────────
        if (Keyboard.current.fKey.wasPressedThisFrame) _combat.PunchLeft();
        if (Keyboard.current.gKey.wasPressedThisFrame) _combat.PunchRight();
    }
}