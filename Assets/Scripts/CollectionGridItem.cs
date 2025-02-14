using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CollectionGridItem : MonoBehaviour
{
    public Image toyImage; // Assign in Inspector
    public TMP_Text countText; // Assign in Inspector
    private int toyID;

    // This function sets up the grid item with data.
    public void Setup(Sprite collectedSprite, Sprite placeholderSprite, int count, int id)
    {
        toyID = id;

        if (count > 0)
        {
            // If the toy has been collected, show its real image
            toyImage.sprite = collectedSprite;
            countText.text = "x" + count;
        }
        else
        {
            // If the toy hasn't been collected, show the placeholder image
            toyImage.sprite = placeholderSprite;
            countText.text = "?"; // Optional: Change to "Locked" or hide count
        }
    }

    // This will be used later to navigate to the toy detail screen.
    public void OnItemClick()
    {
        Debug.Log("Clicked on Toy ID: " + toyID);

        // Retrieve the prefab that will be used for this toyID
        GameObject toyPrefab = GameManager.Instance.toyPrefabs[toyID];
        if (toyPrefab != null)
        {
            Debug.Log($"The prefab for Toy ID {toyID} is: {toyPrefab.name}");
        }
        else
        {
            Debug.LogError($"No prefab found for Toy ID {toyID}!");
        }

        // Then continue to show the detail screen
        GameManager.Instance.ShowToyDetail(toyID);
    }

}
