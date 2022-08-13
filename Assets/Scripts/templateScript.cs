using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class templateScript : MonoBehaviour {

    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMSelectable button;

    //Logging
    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake () {
        moduleId = moduleIdCounter++;
        
	//for SIMILAR selectables (buttons)
	/*
        foreach (KMSelectable object in keypad) {
            object.OnInteract += () => keypadPress(object);
        }
        */

	//for INDIVIDUAL selectables (buttons)
        button.OnInteract += buttonPress;
    }

    // Use this for initialization
    void Start () {

    }

    // Update is called once per frame
    void Update () {

    }

    /*
    bool keypadPress(KMSelectable object) {
        return false;
    }
    */

    
    bool buttonPress() {
        GetComponent<KMBombModule>().HandlePass();
	return false;
    }
    
}
