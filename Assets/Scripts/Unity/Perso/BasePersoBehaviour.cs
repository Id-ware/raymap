﻿using OpenSpace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public abstract class BasePersoBehaviour : MonoBehaviour {
    public bool IsLoaded { get; protected set; } = false;
    public Controller controller { get; set; }
    public SectorComponent sector;

    // States
    protected bool hasStates = false;
    [HideInInspector]
    public string[] stateNames = { "Placeholder" };
    public int currentState { get; protected set; } = 0; // Follows "stateIndex", sometimes with a small delay.
    public int stateIndex { get; set; } = 0; // Set this variable
    public bool autoNextState = false;

    // Physical object lists
    [HideInInspector]
    public string[] poListNames = { "Null" };
    protected int currentPOList = 0;
    public int poListIndex { get; set; } = 0;

    // Animation common properties
    public GameObject[] channelObjects { get; protected set; }
    protected int[] currentActivePO = null;
    protected bool[] channelParents = null;
    protected bool forceAnimUpdate = false;
    protected bool hasBones = false; // We can optimize a tiny bit if this object doesn't have bones
    protected float updateCounter = 0f;
    protected Dictionary<short, List<int>> channelIDDictionary = new Dictionary<short, List<int>>();

    public uint currentFrame = 0;
    public bool playAnimation = true;
    public float animationSpeed = 15f;

    // Other
    public bool IsAlways { get; set; }
    public bool AlwaysPlayAnimation { get; set; } = false;

    private bool isEnabled = true;
    public bool IsEnabled {
        get { return isEnabled; }
        set {
            isEnabled = value;
            controller.UpdatePersoActive(this);
        }
    }

    // AI
    public BrainComponent brain;

    // Abstract properties
    public abstract LegacyPointer Offset { get; }
    public abstract string NameFamily { get; }
    public abstract string NameModel { get; }
    public abstract string NameInstance { get; }
    public abstract StateTransition[] GetStateTransitions(int stateIndex);

    public class StateTransition {
        public string StateToGoName { get; set; }
        public int StateToGoIndex { get; set; }
        public string TargetStateName { get; set; }
        public int TargetStateIndex { get; set; }
        public int LinkingType { get; set; }
    }
}
