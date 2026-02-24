using UnityEngine;

public class PunchDetector : MonoBehaviour
{
    [Header("Configuration")]
    public BoxerCombat owner; 
    public bool isLeftGlove; 

    private void OnTriggerEnter(Collider other)
    {
        // Debug pour voir si Unity détecte au moins un contact
        // Debug.Log(gameObject.name + " a touché : " + other.name + " avec le tag : " + other.tag);

        if (other.CompareTag("Glove") && !other.transform.IsChildOf(owner.transform)) {
            Debug.Log("BLOCAGE détecté par " + gameObject.name);
            owner.HandleImpact(isLeftGlove, true, null);
            return;
        }
        // 2. Détection du COUP RÉUSSI (Corps)
        if (other.CompareTag("Opponent") || other.CompareTag("Player"))
        {
            // Debug.Log("Coup potentiel détecté sur " + other.name + ". Vérification de l'adversaire...");
            if (!other.transform.IsChildOf(owner.transform)) {
                // On cherche BoxerCombat sur l'adversaire
                BoxerCombat adversaire = other.GetComponentInParent<BoxerCombat>();
                
                // Debug.Log("Coup réussi sur " + other.transform.root.name + ". BoxerCombat : " + (adversaire != null ? "Trouvé" : "Introuvable"));
                
                if (adversaire != null)
                {
                    owner.HandleImpact(isLeftGlove, false, adversaire);
                }
                else 
                {
                    Debug.LogWarning("BoxerCombat introuvable sur l'objet touché !");
                }
            }
        }
    }
}