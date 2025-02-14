using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    // -------------- UI Panels --------------
    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject insertCoinPanel;
    public GameObject toyDisplayPanel;
    public GameObject adPromptPanel;
    public GameObject collectionPanel;
    public GameObject settingsPanel;

    // -------------- UI Elements --------------
    public TMP_Text coinsLeftText;
    public Button insertCoinButton;

    // -------------- Game Data --------------
    [Header("Game Data")]
    public int maxCoins = 5;
    private GameData gameData; // Our game data containing coin count and toy collection

    // -------------- Cameras --------------
    [Header("Cameras")]
    public Camera mainCamera;         // The camera showing the vending machine area
    public Camera showcaseCamera;     // The camera showing the toy showcase area

    // -------------- Toy Showcase References --------------
    [Header("Toy Showcase Setup")]
    public Transform rotatorTransform;  // The parent object (with ToyRotator) for the toy
    public GameObject placeholderChild; // The placeholder child that gets swapped out
    public GameObject[] toyPrefabs;     // Array of toy prefabs to choose from
    private GameObject currentDispensedToy; // The currently spawned toy

    // -------------- Collection Panel References --------------
    [Header("Collection Panel Setup")]
    public Transform collectionContentParent; // Assign Content object of Scroll View
    public GameObject collectionGridItemPrefab; // Assign CollectionGridItem prefab

    // -------------- Toy Detail References --------------
    [Header("Toy Detail Setup")]
    public Camera toyDetailCamera;       // A camera looking at the "ToyDetailArea"
    public GameObject toyDetailPanel;    // The UI panel showing toy metadata
    public Transform toyDetailRotatorTransform; // Where to spawn the toy in detail area
    private GameObject currentToyDetailModel; // Tracks the currently displayed toy in detail
    public GameObject placeholderChild2; // The placeholder child that gets swapped out

    // Text fields for displaying metadata in the detail panel
    public TMP_Text detailNameText;
    public TMP_Text detailRarityText;
    public TMP_Text detailTypeText;
    public TMP_Text detailDescriptionText;

    // -------------- Initialization --------------
    void Start()
    {
        LoadData();

        // Ensure a new game starts with coins if coinCount is 0
        if (gameData.coinCount <= 0)
        {
            gameData.coinCount = maxCoins;
        }
        if (gameData.toyCollection == null)
        {
            gameData.toyCollection = new SerializableDictionary<int, int>();
        }

        // Set camera defaults
        if (mainCamera != null) mainCamera.enabled = true;
        if (showcaseCamera != null) showcaseCamera.enabled = false;
        if (toyDetailCamera != null) toyDetailCamera.enabled = false; // Keep detail camera off at start

        // Hide detail panel at start
        if (toyDetailPanel != null)
            toyDetailPanel.SetActive(false);

        ShowMainMenu();
        UpdateCoinText();
    }

    // -------------- UI Navigation --------------
    public void ShowMainMenu()
    {
        if (insertCoinPanel != null) insertCoinPanel.SetActive(false);
        if (toyDisplayPanel != null) toyDisplayPanel.SetActive(false);
        if (adPromptPanel != null) adPromptPanel.SetActive(false);
        if (collectionPanel != null) collectionPanel.SetActive(false);
        if (toyDetailPanel != null) toyDetailPanel.SetActive(false);

        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    public void OnPlayButtonPressed()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);
        GoToInsertCoinState();
    }

    void GoToInsertCoinState()
    {
        if (insertCoinPanel != null)
            insertCoinPanel.SetActive(true);
        UpdateCoinText();
    }
    public void OnBackFromInsertCoinButtonPressed()
    {
        // Hide Insert Coin panel
        if (insertCoinPanel != null)
            insertCoinPanel.SetActive(false);

        // Show the Main Menu
        ShowMainMenu();
    }

    public void OnBackFromToyDetailButtonPressed()
    {
        // Hide Toy Detail Panel  and show Collection Panel and destroy the current toy detail model
        if (toyDetailPanel != null)
            toyDetailPanel.SetActive(false);
        if (collectionPanel != null)
            collectionPanel.SetActive(true);
        if (currentToyDetailModel != null)
        {
            Destroy(currentToyDetailModel);
            currentToyDetailModel = null;
        }

        // Disable the detail camera, re-enable the main camera (or collection camera)
        if (toyDetailCamera != null) toyDetailCamera.enabled = false;
        if (mainCamera != null) mainCamera.enabled = true;

    }


    void UpdateCoinText()
    {
        if (coinsLeftText != null)
            coinsLeftText.text = "Coins Left: " + gameData.coinCount;
        if (insertCoinButton != null)
            insertCoinButton.interactable = (gameData.coinCount > 0);
    }

    public void OnSettingsButtonPressed()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            mainMenuPanel.SetActive(false);
            collectionPanel.SetActive(false);
            toyDetailPanel.SetActive(false);
            insertCoinPanel.SetActive(false);
            adPromptPanel.SetActive(false);


        }
        else
        {
            Debug.LogError("Settings Panel not assigned in the Inspector!");
        }


    }

    // Called by the close button on the settings panel
    public void OnCloseSettingsButtonPressed()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            ShowMainMenu();
        }
    }

    // -------------- Coin & Toy Dispense Logic --------------
    public void OnInsertCoinButtonPressed()
    {
        if (gameData.coinCount <= 0) return;

        // Decrement the coin count and update UI
        gameData.coinCount--;
        UpdateCoinText();

        // Dispense a toy
        DispenseToy();
    }

    void DispenseToy()
    {
        // Hide the Insert Coin panel
        if (insertCoinPanel != null)
            insertCoinPanel.SetActive(false);

        // Switch cameras: disable main, enable showcase
        if (mainCamera != null) mainCamera.enabled = false;
        if (showcaseCamera != null) showcaseCamera.enabled = true;

        // Show the Toy Display panel
        if (toyDisplayPanel != null)
            toyDisplayPanel.SetActive(true);

        // Pick a random toy from the array
        if (toyPrefabs == null || toyPrefabs.Length == 0)
        {
            Debug.LogError("No toy prefabs assigned!");
            return;
        }
        int randomIndex = Random.Range(0, toyPrefabs.Length);
        GameObject chosenToyPrefab = toyPrefabs[randomIndex];

        // Destroy the placeholder if it exists
        if (placeholderChild != null)
        {
            Destroy(placeholderChild);
            placeholderChild = null;
        }

        // Instantiate the chosen toy as a child of the rotatorTransform
        if (rotatorTransform != null)
        {
            currentDispensedToy = Instantiate(chosenToyPrefab, rotatorTransform);
            currentDispensedToy.transform.localPosition = Vector3.zero;
            currentDispensedToy.transform.localRotation = Quaternion.identity;
        }
        else
        {
            Debug.LogError("rotatorTransform is not assigned!");
        }

        // Record the toy in our collection (using a dictionary)
        AddToyToCollection(randomIndex);
    }

    // Called by a button on the Toy Display panel to return to the Insert Coin screen
    public void OnCloseToyDisplayButton()
    {
        if (toyDisplayPanel != null)
            toyDisplayPanel.SetActive(false);

        // Destroy the currently dispensed toy
        if (currentDispensedToy != null)
        {
            Destroy(currentDispensedToy);
            currentDispensedToy = null;
        }

        // Switch cameras back
        if (showcaseCamera != null) showcaseCamera.enabled = false;
        if (mainCamera != null) mainCamera.enabled = true;

        // Show the Insert Coin panel again
        if (insertCoinPanel != null)
            insertCoinPanel.SetActive(true);

        // If coins are now zero, show the ad prompt
        if (gameData.coinCount <= 0)
        {
            ShowAdPrompt();
        }

        SaveData();
    }

    void ShowAdPrompt()
    {
        if (adPromptPanel != null)
            adPromptPanel.SetActive(true);
    }

    public void OnYesWatchAdButton()
    {
        // (Integrate real ad logic here; for now we simply award coins.)
        gameData.coinCount = maxCoins;
        if (adPromptPanel != null)
            adPromptPanel.SetActive(false);
        UpdateCoinText();

        SaveData();
    }

    public void OnNoThanksButton()
    {
        if (adPromptPanel != null)
            adPromptPanel.SetActive(false);
    }


    // -------------- Collection Panel Logic --------------
    public void OnCollectionButtonPressed()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);
        if (collectionPanel != null)
        {
            collectionPanel.SetActive(true);
            PopulateCollectionPanel();
        }
    }

    public void OnCloseCollectionButton()
    {
        if (collectionPanel != null)
            collectionPanel.SetActive(false);
        ShowMainMenu();
    }

    void PopulateCollectionPanel()
    {
        if (collectionContentParent == null || collectionGridItemPrefab == null)
        {
            Debug.LogError("CollectionContentParent or CollectionGridItemPrefab is not assigned!");
            return;
        }

        // Clear previous entries
        foreach (Transform child in collectionContentParent)
        {
            Destroy(child.gameObject);
        }

        // Loop through all possible toys, even if the player hasn't collected them yet
        for (int toyId = 0; toyId < toyPrefabs.Length; toyId++)
        {
            GameObject toyPrefab = toyPrefabs[toyId];
            ToyMetadata meta = toyPrefab.GetComponent<ToyMetadata>();

            if (meta == null)
            {
                Debug.LogError("Toy prefab " + toyPrefab.name + " does not have a ToyMetadata component.");
                continue;
            }

            int count = gameData.toyCollection.dictionary.ContainsKey(toyId) ? gameData.toyCollection.dictionary[toyId] : 0;

            // Instantiate the grid item in the collection panel
            GameObject item = Instantiate(collectionGridItemPrefab, collectionContentParent);
            CollectionGridItem gridItem = item.GetComponent<CollectionGridItem>();

            if (gridItem != null)
            {
                gridItem.Setup(meta.toyImage, meta.placeholderImage, count, toyId);
            }
            else
            {
                Debug.LogError("The collection grid item prefab does not have a CollectionGridItem script attached.");
            }
        }
    }

    // -------------- Toy Detail Logic --------------
    public void ShowToyDetail(int toyID)
    {
        Debug.Log($"[ShowToyDetail] Called with toyID: {toyID}");

        // 1. Disable other cameras, enable toyDetailCamera
        if (mainCamera != null)
        {
            mainCamera.enabled = false;
            Debug.Log("[ShowToyDetail] Disabled mainCamera.");
        }
        if (showcaseCamera != null)
        {
            showcaseCamera.enabled = false;
            Debug.Log("[ShowToyDetail] Disabled showcaseCamera.");
        }
        if (toyDetailCamera != null)
        {
            toyDetailCamera.enabled = true;
            Debug.Log("[ShowToyDetail] Enabled toyDetailCamera.");
        }

        // 2. Hide the collection panel so we only see detail stuff
        if (collectionPanel != null)
        {
            collectionPanel.SetActive(false);
            Debug.Log("[ShowToyDetail] Collection panel hidden.");
        }

        // 3. Show the detail panel
        if (toyDetailPanel != null)
        {
            toyDetailPanel.SetActive(true);
            Debug.Log("[ShowToyDetail] ToyDetailPanel set active.");
        }

        // 4. Destroy the old placeholder (the cube) if it exists
        if (placeholderChild2 != null)
        {
            Debug.Log($"[ShowToyDetail] Destroying placeholderChild2: {placeholderChild2.name}");
            Destroy(placeholderChild2);
            placeholderChild2 = null;
        }

        // 5. Destroy any previously spawned toy model
        if (currentToyDetailModel != null)
        {
            Debug.Log($"[ShowToyDetail] Destroying old model: {currentToyDetailModel.name}");
            Destroy(currentToyDetailModel);
            currentToyDetailModel = null;
        }

        // 6. Validate toyID
        if (toyID < 0 || toyID >= toyPrefabs.Length)
        {
            Debug.LogError($"[ShowToyDetail] Invalid toy ID {toyID}. Prefabs array length: {toyPrefabs.Length}");
            return;
        }

        // 7. Instantiate the new toy as a child of the rotator 
        if (toyDetailRotatorTransform != null)
        {
            Debug.Log($"[ShowToyDetail] Instantiating toy prefab: {toyPrefabs[toyID].name} at spawn point: {toyDetailRotatorTransform.name}");
            currentToyDetailModel = Instantiate(toyPrefabs[toyID], toyDetailRotatorTransform.position, Quaternion.identity);

            // Attach the newly spawned toy to the toy detail rotator point
            currentToyDetailModel.transform.SetParent(toyDetailRotatorTransform, true);

            // Make sure it sits at local (0,0,0) and rotation is reset
            currentToyDetailModel.transform.localPosition = Vector3.zero;
            currentToyDetailModel.transform.localRotation = Quaternion.identity;
            currentToyDetailModel.transform.localScale = Vector3.one * 10.5f; // Scale up for detail view

            Debug.Log($"[ShowToyDetail] Spawned toy model: {currentToyDetailModel.name}");

            // 8. Get metadata from the new toy (if available) and display
            ToyMetadata meta = currentToyDetailModel.GetComponent<ToyMetadata>();
            if (meta != null)
            {
                Debug.Log($"[ShowToyDetail] Found ToyMetadata on {currentToyDetailModel.name}, updating UI fields.");

                if (detailNameText != null) detailNameText.text = meta.toyName;
                if (detailRarityText != null) detailRarityText.text = meta.rarity;
                if (detailTypeText != null) detailTypeText.text = meta.type;
                if (detailDescriptionText != null) detailDescriptionText.text = meta.description;
            }
            else
            {
                Debug.LogWarning($"[ShowToyDetail] No ToyMetadata found on {currentToyDetailModel.name}");
            }
        }
        else
        {
            Debug.LogError("[ShowToyDetail] toyDetailSpawnPoint is not assigned!");
        }

        Debug.Log("[ShowToyDetail] Finished ShowToyDetail execution.");
    }



    // Called by a Close button on the ToyDetailPanel
    public void HideToyDetail()
    {
        // Hide the detail panel
        if (toyDetailPanel != null)
            toyDetailPanel.SetActive(false);

        // Destroy the detail model
        if (currentToyDetailModel != null)
        {
            Destroy(currentToyDetailModel);
            currentToyDetailModel = null;
        }

        // Disable the detail camera, re-enable the main camera (or collection camera)
        if (toyDetailCamera != null) toyDetailCamera.enabled = false;
        if (mainCamera != null) mainCamera.enabled = true;
    }

    // -------------- Toy Collection with Dictionary --------------
    void AddToyToCollection(int toyIndex)
    {
        if (gameData.toyCollection == null)
        {
            gameData.toyCollection = new SerializableDictionary<int, int>();
        }
        if (gameData.toyCollection.dictionary.ContainsKey(toyIndex))
        {
            gameData.toyCollection.dictionary[toyIndex]++;
        }
        else
        {
            gameData.toyCollection.dictionary[toyIndex] = 1;
        }
    }

    // -------------- Singleton Pattern --------------
    public static GameManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject); // Prevent duplicates
        }
    }


    // -------------- Save / Load Logic --------------
    void SaveData()
    {
        SaveSystem.SaveGame(gameData);
    }

    void LoadData()
    {
        gameData = SaveSystem.LoadGame();
        if (gameData == null)
        {
            gameData = new GameData();
        }
        if (gameData.toyCollection == null)
        {
            gameData.toyCollection = new SerializableDictionary<int, int>();
        }
    }

    // Save when the application quits or pauses (for mobile)
    void OnApplicationQuit()
    {
        SaveData();
    }

    void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            SaveData();
        }
    }

    // -------------- Debugging --------------
    public GameData GetGameData()
    {
        return gameData;
    }
}
