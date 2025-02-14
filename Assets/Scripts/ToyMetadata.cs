using UnityEngine;

public class ToyMetadata : MonoBehaviour
{
    public string toyName;
    public string rarity;
    public string type;
    public string description; // Used later for details
    public Sprite toyImage; // Main image when collected
    public Sprite placeholderImage; // Image if not yet collected
}
