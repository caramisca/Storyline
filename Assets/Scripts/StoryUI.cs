//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;
//using System.Collections.Generic;

//public class StoryUI : MonoBehaviour
//{
//    public static StoryUI Instance;

//    [Header("UI References")]
//    public TMP_Text nodeTitleText;
//    public TMP_Text nodeBodyText;
//    public Transform choicesContainer;
//    public GameObject choiceButtonPrefab;
//    public TMP_InputField nameInputField; // for prompt input

//    public Button nextButton; // Reference to the Next button

//    private StoryNode currentNode;
//    private int unfoldIndex; // track which unfoldStep is being shown
//    private UnfoldStep[] unfoldStepsCache;

//    // -----------------------------------------------------------
//    // ADDED FOR DEBUG UI
//    // -----------------------------------------------------------
//    [Header("Debug UI")]
//    public GameObject debugPanel; // Assign a Panel in the inspector
//    public TextMeshProUGUI debugInfoText; // A text field to show variable details
//    private bool debugVisible;

//    void Awake()
//    {
//        // Singleton so we can reference StoryUI.Instance
//        if (Instance == null)
//        {
//            Instance = this;
//        }
//        else
//        {
//            Destroy(gameObject);
//        }
//    }

//    /// <summary>
//    /// Add the Next-button listener at Start. 
//    /// This ensures the button calls OnNextStep() when clicked.
//    /// </summary>
//    void Start()
//    {
//        if (nextButton != null)
//        {
//            nextButton.onClick.AddListener(OnNextStep);
//        }
//    }

//    void Update()
//    {
//        // If the Next button is active, and the name input field is not active, allow Enter key to trigger the next step.
//        if (nextButton != null && nextButton.gameObject.activeSelf && !nameInputField.gameObject.activeSelf)
//        {
//            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
//            {
//                OnNextStep();
//            }
//        }

//        // --------------------------------------------------------
//        // Toggle debug panel with the BackQuote (`) key
//        // --------------------------------------------------------
//        if (Input.GetKeyDown(KeyCode.BackQuote))
//        {
//            debugVisible = !debugVisible;
//            if (debugPanel != null)
//            {
//                debugPanel.SetActive(debugVisible);
//            }
//        }

//        // If debug panel is visible, update info text
//        if (debugVisible && debugInfoText != null && StoryManager.Instance != null)
//        {
//            debugInfoText.text = BuildDebugInfo();
//        }
//    }

//    /// <summary>
//    /// Displays a node in the UI. 
//    /// If the node has unfoldSteps, they appear one at a time with the Next button.
//    /// Otherwise, we show the node text + choices.
//    /// </summary>
//    public void DisplayNode(StoryNode node)
//    {
//        ClearUI();
//        currentNode = node;
//        unfoldIndex = 0;
//        unfoldStepsCache = node.unfoldSteps;

//        // If we have unfoldSteps, show them one at a time and display the Next button
//        if (unfoldStepsCache != null && unfoldStepsCache.Length > 0)
//        {
//            nodeTitleText.text = node.title;
//            nodeBodyText.text = unfoldStepsCache[0].text;
//            if (nextButton != null) nextButton.gameObject.SetActive(true);
//        }
//        else
//        {
//            // If no unfoldSteps, just display node.text or the title
//            nodeTitleText.text = node.title;
//            nodeBodyText.text = string.IsNullOrEmpty(node.text) ? "" : node.text;
//            if (nextButton != null) nextButton.gameObject.SetActive(false);

//            // Show options immediately if available
//            if (node.options != null && node.options.Length > 0)
//            {
//                CreateOptionButtons(node.options);
//            }
//        }
//    }

//    /// <summary>
//    /// Called when user presses the "Next" button during an unfold sequence
//    /// or when Enter is pressed (if the Next button is active).
//    /// </summary>
//    public void OnNextStep()
//    {
//        if (currentNode == null) return;
//        if (unfoldStepsCache == null || unfoldStepsCache.Length == 0) return;

//        unfoldIndex++;
//        if (unfoldIndex < unfoldStepsCache.Length)
//        {
//            nodeBodyText.text = unfoldStepsCache[unfoldIndex].text;
//        }
//        else
//        {
//            // All unfold steps exhausted, append node.text if available
//            if (!string.IsNullOrEmpty(currentNode.text))
//            {
//                nodeBodyText.text += "\n\n" + currentNode.text;
//            }

//            // Show available options if any
//            if (currentNode.options != null && currentNode.options.Length > 0)
//            {
//                CreateOptionButtons(currentNode.options);
//            }
//            // Disable the Next button when there are no further unfold steps
//            if (nextButton != null) nextButton.gameObject.SetActive(false);
//        }
//    }

//    /// <summary>
//    /// Creates a button for each available StoryOption.
//    /// </summary>
//    void CreateOptionButtons(StoryOption[] options)
//    {
//        foreach (var opt in options)
//        {
//            GameObject btnObj = Instantiate(choiceButtonPrefab, choicesContainer);
//            TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();
//            if (btnText != null)
//            {
//                btnText.text = opt.text;
//            }

//            Button b = btnObj.GetComponent<Button>();
//            if (b != null)
//            {
//                b.onClick.AddListener(() => OnOptionClicked(opt));
//            }
//        }
//    }

//    void OnOptionClicked(StoryOption option)
//    {
//        ClearUI();

//        // Check if the option requires an input prompt
//        if (!string.IsNullOrEmpty(option.inputPrompt))
//        {
//            nodeBodyText.text = option.inputPrompt;
//            nameInputField.gameObject.SetActive(true);
//            // Optionally store a reference to the option for after input
//        }
//        else
//        {
//            // Immediately handle the option if no input prompt is required
//            StoryManager.Instance.HandleOptionSelection(option);
//        }
//    }

//    /// <summary>
//    /// Called when the "ConfirmName" button is pressed for the input field.
//    /// </summary>
//    public void OnConfirmName()
//    {
//        string enteredName = nameInputField.text;
//        StoryManager.Instance.SetPlayerName(enteredName);

//        nameInputField.text = "";
//        nameInputField.gameObject.SetActive(false);
//        // Possibly proceed or show next node, depending on your design
//    }

//    /// <summary>
//    /// Clears UI text and removes any existing choice buttons.
//    /// </summary>
//    void ClearUI()
//    {
//        nodeBodyText.text = "";
//        // Destroy any existing option buttons
//        foreach (Transform child in choicesContainer)
//        {
//            Destroy(child.gameObject);
//        }
//        nameInputField.gameObject.SetActive(false);

//        // Reset the Next button state
//        if (nextButton != null)
//            nextButton.gameObject.SetActive(false);
//    }

//    // -----------------------------------------------------------
//    // ADDED FOR DEBUG UI
//    // Build a string of current variables to show in the debug panel
//    // -----------------------------------------------------------
//    private string BuildDebugInfo()
//    {
//        if (StoryManager.Instance == null) return "No StoryManager!";

//        var v = StoryManager.Instance.CurrentVariables;
//        if (v == null) return "No variables loaded!";

//        System.Text.StringBuilder sb = new System.Text.StringBuilder();
//        sb.AppendLine("<b>DEBUG INFO</b>");
//        sb.AppendLine("Current Node: " + (StoryManager.Instance.CurrentNode != null ? StoryManager.Instance.CurrentNode.id : "[none]"));
//        sb.AppendLine("Player Name: " + v.playerName);
//        sb.AppendLine("Motivation: " + v.playerMotivation);
//        sb.AppendLine("Act Progress: " + v.actProgress);

//        sb.AppendLine("\n<b>Trust Levels</b>");
//        foreach (var kvp in v.trustLevels)
//        {
//            sb.AppendLine(kvp.Key + ": " + kvp.Value);
//        }

//        sb.AppendLine("\n<b>Suspicion Levels</b>");
//        foreach (var kvp in v.suspicionLevels)
//        {
//            sb.AppendLine(kvp.Key + ": " + kvp.Value);
//        }

//        sb.AppendLine("\n<b>Clues Found</b>");
//        if (v.cluesFound != null && v.cluesFound.Count > 0)
//        {
//            foreach (var c in v.cluesFound)
//            {
//                sb.AppendLine("- " + c);
//            }
//        }
//        else
//        {
//            sb.AppendLine("None");
//        }

//        sb.AppendLine("\n<b>Extra Flags</b>");
//        sb.AppendLine("foundSecretPassage: " + v.extraFlags.foundSecretPassage);
//        sb.AppendLine("heardBasementNoises: " + v.extraFlags.heardBasementNoises);
//        sb.AppendLine("investigatedLockedRoom: " + v.extraFlags.investigatedLockedRoom);

//        return sb.ToString();
//    }
//}
