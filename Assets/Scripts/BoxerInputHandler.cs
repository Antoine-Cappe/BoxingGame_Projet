using UnityEngine;
using UnityEngine.InputSystem;

public class BoxerInputHandler : MonoBehaviour
{
    private BoxerMovement _mvmt;
    private BoxerCombat _combat;

    void Awake()
    {
        _mvmt = GetComponent<BoxerMovement>();
        _combat = GetComponent<BoxerCombat>();
    }

    void Update()
    {
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

        // ── GARDE (G = haute / B = basse / relâché = aucune) ─────
        if (Keyboard.current.gKey.isPressed)
            _mvmt.guardInput = GuardType.High;
        else if (Keyboard.current.bKey.isPressed)
            _mvmt.guardInput = GuardType.Low;
        else
            _mvmt.guardInput = GuardType.None;

        // ── GANT GAUCHE (Flèches ← ↑ ↓ →) ───────────────────────
        float leftX = 0f, leftY = 0f;
        if (Keyboard.current.leftArrowKey.isPressed) leftX = -0.5f;
        if (Keyboard.current.rightArrowKey.isPressed) leftX = 0.5f;
        if (Keyboard.current.upArrowKey.isPressed) leftY = 1f;
        if (Keyboard.current.downArrowKey.isPressed) leftY = -0.25f;
        _mvmt.aimInputLeft = new Vector2(leftX, leftY);

        // ── GANT DROIT (Pavé numérique 4 / 6 / 8 / 5) ───────────
        float rightX = 0f, rightY = 0f;
        if (Keyboard.current.numpad4Key.isPressed) rightX = -0.5f;
        if (Keyboard.current.numpad6Key.isPressed) rightX = 0.5f;
        if (Keyboard.current.numpad8Key.isPressed) rightY = 1f;
        if (Keyboard.current.numpad5Key.isPressed) rightY = -0.25f;
        _mvmt.aimInputRight = new Vector2(rightX, rightY);

        // ── ESQUIVE (Shift Gauche / Shift Droit) ─────────────────
        if (Keyboard.current.leftShiftKey.wasPressedThisFrame)
            _mvmt.TriggerDodge(1f);
        if (Keyboard.current.rightShiftKey.wasPressedThisFrame)
            _mvmt.TriggerDodge(-1f);

        // ── COMBAT (Pavé num 7 = Gauche / 9 = Droite) ────────────
        if (Keyboard.current.numpad7Key.wasPressedThisFrame) _combat.PunchLeft();
        if (Keyboard.current.numpad9Key.wasPressedThisFrame) _combat.PunchRight();
    }
}