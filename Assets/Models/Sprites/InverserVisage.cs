using UnityEngine;

[ExecuteAlways]
public class InverserVisage : MonoBehaviour
{
    void Start()
    {
        AppliquerTransparence();
    }

    // S'exécute dès que tu modifies un réglage dans l'Inspector
    void OnValidate()
    {
        AppliquerTransparence();
    }

    void AppliquerTransparence()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        // On vérifie qu'il y a bien un Sprite Renderer et une image
        if (sr != null && sr.sprite != null)
        {
            Texture2D tex = sr.sprite.texture;

            try
            {
                // On crée une nouvelle texture pour ne pas modifier l'originale
                Texture2D newTex = new Texture2D(tex.width, tex.height);

                for (int y = 0; y < tex.height; y++)
                {
                    for (int x = 0; x < tex.width; x++)
                    {
                        Color c = tex.GetPixel(x, y);

                        // Si le pixel est blanc (proche de 1,1,1), on le rend invisible
                        if (c.r > 0.9f && c.g > 0.9f && c.b > 0.9f)
                            newTex.SetPixel(x, y, new Color(0, 0, 0, 0));
                        else
                            newTex.SetPixel(x, y, c); // On garde le noir (ou les autres couleurs)
                    }
                }

                newTex.Apply();

                // On crée le nouveau sprite avec la transparence
                sr.sprite = Sprite.Create(newTex, new Rect(0, 0, newTex.width, newTex.height), new Vector2(0.5f, 0.5f));
            }
            catch
            {
                // Ce message s'affiche si tu as oublié de cocher Read/Write
                Debug.LogWarning("Vérifie que 'Read/Write' est coché dans l'Inspector de ton image !");
            }
        }
    }
}