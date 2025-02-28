using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SimpleJSON;

public class StoryManager : MonoBehaviour
{
    [Header("References")]
    public TextAsset storyJsonFile;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI titleText;
    public Transform choicesContainer;
    public GameObject choiceButtonPrefab;
    public Image backgroundImage;
    public CanvasGroup fadeOverlay;
    public Button nextButton;      // Button used to advance unfoldSteps
    public Button resetSaveButton; // New button to reset save and restart game

    [Header("Audio")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioClip buttonClickSFX;

    [Header("Typing Settings")]
    public float typingSpeed = 0.03f;
    private Coroutine typingCoroutine;

    private Story storyData;
    private Dictionary<string, NodeData> nodeDict = new Dictionary<string, NodeData>();
    private NodeData currentNode;
    private bool skipTyping = false;
    private int currentStepIndex = 0; // Tracks which unfoldStep is being shown

    void Start()
    {
        // Check for EventSystem.
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            Debug.LogError("No EventSystem found in the scene. Please add one to capture UI clicks.");
        }

        // Set up Next button click event.
        if (nextButton != null)
            nextButton.onClick.AddListener(AdvanceStep);
        else
            Debug.LogWarning("Next button is not assigned in the Inspector.");

        // Set up Reset Save button click event.
        if (resetSaveButton != null)
            resetSaveButton.onClick.AddListener(ResetGame);
        else
            Debug.LogWarning("Reset Save button is not assigned in the Inspector.");

        LoadStoryFromJson();

        if (PlayerPrefs.HasKey("CurrentNode"))
        {
            LoadGame();
        }
        else
        {
            if (nodeDict.ContainsKey(storyData.startNode))
            {
                currentNode = nodeDict[storyData.startNode];
                DisplayNode(currentNode);
            }
            else
            {
                Debug.LogError($"Start node '{storyData.startNode}' not found in nodeDict!");
            }
        }
    }

    void LoadStoryFromJson()
    {
        if (storyJsonFile == null)
        {
            Debug.LogError("Story JSON file not assigned in StoryManager!");
            return;
        }

        try
        {
            var json = JSON.Parse(storyJsonFile.text);
            if (json == null)
            {
                Debug.LogError("Failed to parse JSON.");
                return;
            }
            Debug.Log("Story JSON loaded successfully.");

            // Parse top-level story properties.
            storyData = new Story
            {
                title = json["title"],
                description = json["description"],
                startNode = json["startNode"],
                variables = new Variables()
            };

            // Parse Variables.
            var vars = json["variables"];
            storyData.variables.playerName = vars["playerName"];
            storyData.variables.playerMotivation = vars["playerMotivation"];

            // Parse trustLevels as a dictionary.
            storyData.variables.trustLevels = new Dictionary<string, int>();
            foreach (var key in vars["trustLevels"].Keys)
            {
                storyData.variables.trustLevels[key] = vars["trustLevels"][key].AsInt;
            }
            // Parse suspicionLevels as a dictionary.
            storyData.variables.suspicionLevels = new Dictionary<string, int>();
            foreach (var key in vars["suspicionLevels"].Keys)
            {
                storyData.variables.suspicionLevels[key] = vars["suspicionLevels"][key].AsInt;
            }
            // Convert cluesFound JSONArray to a string array.
            var cluesArray = vars["cluesFound"].AsArray;
            if (cluesArray != null)
            {
                List<string> cluesList = new List<string>();
                foreach (JSONNode clue in cluesArray)
                {
                    cluesList.Add(clue.Value);
                }
                storyData.variables.cluesFound = cluesList.ToArray();
            }
            else
            {
                storyData.variables.cluesFound = new string[0];
            }
            storyData.variables.actProgress = vars["actProgress"];
            // Parse extraFlags using JsonUtility.
            storyData.variables.extraFlags = JsonUtility.FromJson<ExtraFlags>(vars["extraFlags"].ToString());

            // Parse nodes.
            var nodesObj = json["nodes"];
            if (nodesObj == null || nodesObj.Count == 0)
            {
                Debug.LogError("No 'nodes' found in the JSON!");
                return;
            }

            foreach (var nodeKey in nodesObj.Keys)
            {
                NodeData nodeData = JsonUtility.FromJson<NodeData>(nodesObj[nodeKey].ToString());
                if (nodeData != null)
                {
                    // Set the node id (using key if missing).
                    nodeData.id = nodeKey;
                    nodeDict[nodeKey] = nodeData;
                }
            }

            Debug.Log($"Story loaded. Total nodes: {nodeDict.Count}");
        }
        catch (Exception e)
        {
            Debug.LogError("JSON Parsing Error: " + e.Message);
        }
    }

    void DisplayNode(NodeData node)
    {
        currentStepIndex = 0; // Reset step counter

        // 1) Attempt to load a background image named the same as the node's ID from Assets/Resources/Backgrounds
        string nodeBackgroundPath = "Backgrounds/" + node.id; // e.g. "Backgrounds/NodeName"
        Sprite nodeSprite = Resources.Load<Sprite>(nodeBackgroundPath);

        if (nodeSprite != null)
        {
            backgroundImage.sprite = nodeSprite;
        }
        else
        {
            // If not found, do nothing. The existing background remains.
            Debug.Log($"No background sprite found at '{nodeBackgroundPath}'. Keeping current background.");
        }

        // 2) Update UI text
        if (titleText != null)
            titleText.text = node.title;

        // 3) Clear any previous choices and hide the container
        ClearChoices();
        HideChoices();

        // 4) If there are unfoldSteps, show them one by one; otherwise show node.text
        if (node.unfoldSteps != null && node.unfoldSteps.Length > 0)
        {
            ShowCurrentStep();
        }
        else
        {
            dialogueText.text = node.text;
            CreateChoices(node);
        }
    }

    // Displays the current unfoldStep using a typing effect.
    void ShowCurrentStep()
    {
        if (currentStepIndex >= currentNode.unfoldSteps.Length)
        {
            // Finished unfolding: hide Next button and display choices.
            if (nextButton != null)
                nextButton.gameObject.SetActive(false);
            CreateChoices(currentNode);
            return;
        }

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeTextRoutine(currentNode.unfoldSteps[currentStepIndex].text));

        // Show the Next button during unfolding.
        if (nextButton != null)
            nextButton.gameObject.SetActive(true);
    }

    // Coroutine to type text character-by-character.
    IEnumerator TypeTextRoutine(string fullText)
    {
        dialogueText.text = "";
        skipTyping = false;
        foreach (char c in fullText)
        {
            if (skipTyping)
            {
                dialogueText.text = fullText;
                break;
            }
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
        typingCoroutine = null;
        skipTyping = false;
    }

    // Called by the Next button.
    public void AdvanceStep()
    {
        if (typingCoroutine != null)
        {
            // If still typing, skip to full text.
            skipTyping = true;
            return;
        }
        currentStepIndex++;
        ShowCurrentStep();
    }

    // Hides the choices container.
    void HideChoices()
    {
        if (choicesContainer != null)
            choicesContainer.gameObject.SetActive(false);
    }

    // Creates and displays choice buttons.
    void CreateChoices(NodeData node)
    {
        if (choicesContainer != null)
            choicesContainer.gameObject.SetActive(true);

        if (node.options == null || node.options.Length == 0)
        {
            Debug.Log("No options available for node: " + node.id);
            return;
        }

        foreach (var opt in node.options)
        {
            GameObject choiceObj = Instantiate(choiceButtonPrefab, choicesContainer);
            if (choiceObj == null)
            {
                Debug.LogError("Choice button prefab instantiation failed.");
                continue;
            }

            Button btn = choiceObj.GetComponent<Button>();
            if (btn == null)
            {
                Debug.LogError("Instantiated choice button does not have a Button component.");
                continue;
            }

            // Remove previous listeners.
            btn.onClick.RemoveAllListeners();

            // Add listener with logging.
            btn.onClick.AddListener(() =>
            {
                Debug.Log("Choice button clicked: " + opt.text);
                OnChoiceSelected(opt);
            });

            // Set the button text.
            TextMeshProUGUI txt = choiceObj.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.text = opt.text;
                Debug.Log("Choice button created with text: " + txt.text);
            }
            else
            {
                Debug.LogWarning("No TextMeshProUGUI found on the choice button prefab.");
            }
        }
    }

    // Called when a choice button is clicked.
    void OnChoiceSelected(OptionData option)
    {
        Debug.Log("OnChoiceSelected called for option: " + option.text);

        // Disable Next button to avoid interference.
        if (nextButton != null)
            nextButton.gameObject.SetActive(false);

        if (buttonClickSFX != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(buttonClickSFX);
        }

        ApplyEffects(option);

        if (!string.IsNullOrEmpty(option.nextNode))
        {
            if (nodeDict.ContainsKey(option.nextNode))
            {
                Debug.Log("Advancing to node: " + option.nextNode);
                currentNode = nodeDict[option.nextNode];
                SaveGame();
                DisplayNode(currentNode);
            }
            else
            {
                Debug.LogError("Next node not found in dictionary: " + option.nextNode);
            }
        }
        else
        {
            Debug.Log("No nextNode specified for this option.");
        }
    }

    // Clears all choice buttons.
    void ClearChoices()
    {
        if (choicesContainer == null) return;
        foreach (Transform child in choicesContainer)
        {
            Destroy(child.gameObject);
        }
    }

    void ApplyEffects(OptionData option)
    {
        // Process top-level setVariable.
        if (option.setVariable != null && !string.IsNullOrEmpty(option.setVariable.playerMotivation))
        {
            storyData.variables.playerMotivation = option.setVariable.playerMotivation;
        }

        // Process top-level adjustTrust.
        if (option.adjustTrust != null)
        {
            foreach (var kv in option.adjustTrust)
            {
                if (storyData.variables.trustLevels.ContainsKey(kv.Key))
                    storyData.variables.trustLevels[kv.Key] += kv.Value;
                else
                    storyData.variables.trustLevels[kv.Key] = kv.Value;
            }
        }

        // Process top-level adjustSuspicion.
        if (option.adjustSuspicion != null)
        {
            foreach (var kv in option.adjustSuspicion)
            {
                if (storyData.variables.suspicionLevels.ContainsKey(kv.Key))
                    storyData.variables.suspicionLevels[kv.Key] += kv.Value;
                else
                    storyData.variables.suspicionLevels[kv.Key] = kv.Value;
            }
        }

        // Process top-level addClue.
        if (option.addClue != null && !string.IsNullOrEmpty(option.addClue.clueName))
        {
            List<string> clues = new List<string>(storyData.variables.cluesFound);
            if (!clues.Contains(option.addClue.clueName))
                clues.Add(option.addClue.clueName);
            storyData.variables.cluesFound = clues.ToArray();
        }

        // Process nested effects.
        if (option.effect != null)
        {
            if (option.effect.setVariable != null)
            {
                foreach (var kv in option.effect.setVariable)
                {
                    if (kv.Key == "playerMotivation")
                        storyData.variables.playerMotivation = kv.Value;
                    else if (kv.Key.StartsWith("extraFlags."))
                    {
                        string flagName = kv.Key.Substring("extraFlags.".Length);
                        if (flagName == "foundSecretPassage")
                            storyData.variables.extraFlags.foundSecretPassage = kv.Value.ToLower() == "true";
                        else if (flagName == "heardBasementNoises")
                            storyData.variables.extraFlags.heardBasementNoises = kv.Value.ToLower() == "true";
                        else if (flagName == "investigatedLockedRoom")
                            storyData.variables.extraFlags.investigatedLockedRoom = kv.Value.ToLower() == "true";
                    }
                }
            }

            if (option.effect.adjustTrust != null)
            {
                foreach (var kv in option.effect.adjustTrust)
                {
                    if (storyData.variables.trustLevels.ContainsKey(kv.Key))
                        storyData.variables.trustLevels[kv.Key] += kv.Value;
                    else
                        storyData.variables.trustLevels[kv.Key] = kv.Value;
                }
            }

            if (option.effect.adjustSuspicion != null)
            {
                foreach (var kv in option.effect.adjustSuspicion)
                {
                    if (storyData.variables.suspicionLevels.ContainsKey(kv.Key))
                        storyData.variables.suspicionLevels[kv.Key] += kv.Value;
                    else
                        storyData.variables.suspicionLevels[kv.Key] = kv.Value;
                }
            }

            if (!string.IsNullOrEmpty(option.effect.addClue))
            {
                List<string> clues = new List<string>(storyData.variables.cluesFound);
                if (!clues.Contains(option.effect.addClue))
                    clues.Add(option.effect.addClue);
                storyData.variables.cluesFound = clues.ToArray();
            }
        }
    }

    void SetBackground(string bgName)
    {
        Sprite bgSprite = Resources.Load<Sprite>("Backgrounds/" + bgName);
        if (bgSprite != null)
        {
            backgroundImage.sprite = bgSprite;
        }
        else
        {
            Debug.LogWarning("Background sprite not found for: " + bgName);
        }
    }

    #region Saving & Loading

    void SaveGame()
    {
        PlayerPrefs.SetString("CurrentNode", currentNode.id);
        string varJson = JsonUtility.ToJson(storyData.variables);
        PlayerPrefs.SetString("StoryVars", varJson);
        PlayerPrefs.Save();
        Debug.Log("Game saved at node: " + currentNode.id);
    }

    void LoadGame()
    {
        string savedNode = PlayerPrefs.GetString("CurrentNode");
        string varJson = PlayerPrefs.GetString("StoryVars");
        storyData.variables = JsonUtility.FromJson<Variables>(varJson);

        if (nodeDict.ContainsKey(savedNode))
        {
            currentNode = nodeDict[savedNode];
            DisplayNode(currentNode);
            Debug.Log("Game loaded at node: " + currentNode.id);
        }
        else
        {
            Debug.LogError($"Saved node '{savedNode}' not found in nodeDict!");
        }
    }

    #endregion

    // Resets the saved game and restarts from the beginning.
    public void ResetGame()
    {
        Debug.Log("Reset Save button clicked. Clearing saved game data and restarting.");
        PlayerPrefs.DeleteKey("CurrentNode");
        PlayerPrefs.DeleteKey("StoryVars");
        PlayerPrefs.Save();
        // Restart the game from the starting node.
        if (nodeDict.ContainsKey(storyData.startNode))
        {
            currentNode = nodeDict[storyData.startNode];
            DisplayNode(currentNode);
        }
        else
        {
            Debug.LogError("Start node not found in nodeDict when resetting game.");
        }
    }
}
