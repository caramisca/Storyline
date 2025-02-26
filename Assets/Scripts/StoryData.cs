using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Story
{
    public string title;
    public string description;
    public string startNode;
    public Variables variables;
    public string[] endings;
    public CharacterData[] characters;
}

[Serializable]
public class Variables
{
    public string playerName;
    public string playerMotivation;
    public Dictionary<string, int> trustLevels;
    public Dictionary<string, int> suspicionLevels;
    public string[] cluesFound;
    public string actProgress;
    public ExtraFlags extraFlags;
}

[Serializable]
public class ExtraFlags
{
    public bool foundSecretPassage;
    public bool heardBasementNoises;
    public bool investigatedLockedRoom;
}

[Serializable]
public class NodeData
{
    public string id;
    public string act;
    public string scene;
    // New field for background image name (if provided).
    public string background;
    public string animation;
    public string title;
    public UnfoldStep[] unfoldSteps;  // Used for sequential text display.
    public string text;             // Fallback text if unfoldSteps are not provided.
    public string inputPrompt;      // Optional prompt (e.g., for name entry).
    public OptionData[] options;
}

[Serializable]
public class UnfoldStep
{
    public string text;
}

[Serializable]
public class OptionData
{
    public string text;
    public string nextNode;
    public SetVariable setVariable;
    public Dictionary<string, int> adjustTrust;
    public Dictionary<string, int> adjustSuspicion;
    public AddClue addClue;
    // Wrapper for nested effects.
    public OptionEffect effect;
}

[Serializable]
public class SetVariable
{
    public string playerMotivation;
}

[Serializable]
public class AddClue
{
    public string clueName;
}

[Serializable]
public class OptionEffect
{
    public Dictionary<string, string> setVariable;
    public Dictionary<string, int> adjustTrust;
    public Dictionary<string, int> adjustSuspicion;
    public string addClue;
}

[Serializable]
public class CharacterData
{
    public string name;
    public string role;
    public string background;
    public string motives;
}
