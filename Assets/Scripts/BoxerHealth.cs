using UnityEngine;
using System.Collections;

public class BoxerHealth : MonoBehaviour
{
    [Header("Réglages Santé")]
    public int maxHits = 5; 
    private int _currentHits;
    private bool _isKO = false;

    [Header("Références")]
    private BoxerController _controller;
    public Transform pivot; 

    void Awake()
    {
        _currentHits = maxHits;
        _controller = GetComponent<BoxerController>();
    }

    public void TakeDamage()
    {
        if (_isKO) return;

        _currentHits--;
        if (_currentHits <= 0)
        {
            TriggerKO();
        }
    }

    private void TriggerKO()
    {
        if (_isKO) return;
        _isKO = true;

        // Désactivation du controller et arrêt des routines de punch/wobble
        if (_controller != null)
        {
            _controller.StopAllCoroutines(); 
            _controller.enabled = false;
        }

        // On lance la chute finale
        StartCoroutine(FallRoutine());
    }

    IEnumerator FallRoutine()
    {
        // On s'assure que rien d'autre ne touche au pivot
        Quaternion startRot = pivot.localRotation;
        
        // On peut varier la chute : un peu sur le côté et vers l'arrière
        Quaternion endRot = Quaternion.Euler(85, 15, 0); 
        
        float t = 0;
        while (t < 1f)
        {
            // On utilise deltaTime pour une chute fluide
            t += Time.deltaTime * 2.5f; 
            
            // Utilisation de Slerp pour une rotation de chute naturelle
            pivot.localRotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }
        
        // On force la position finale pour être sûr qu'il ne bouge plus
        pivot.localRotation = endRot;
    }
}