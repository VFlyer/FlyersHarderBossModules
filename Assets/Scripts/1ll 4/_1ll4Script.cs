using KModkit;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FlyersBossierEnums;

public class _1ll4Script : MonoBehaviour {

	public KMSelectable[] segmentSelectables, clrSelectables;
	public KMSelectable submitBtn;
	public KMBombInfo bombInfo;
	public KMBossModule bossHandler;
	public KMBombModule modSelf;
	public KMColorblindMode colorblindMode;
	public KMAudio mAudio;
	public MeshRenderer[] segmentRenderers;
	public TextMesh[] cbTextMesh;
	public TextMesh stageTxt, cbLedTxt;

	Dictionary<char, string> chrSegmentDisplay = new Dictionary<char, string> {
		{ '0', "**--**--**--**" },
		{ '1', "----**------*-" },
		{ '2', "*----****----*" },
		{ '3', "*----***----**" },
		{ '4', "-*---***----*-" },
		{ '5', "*-*----*----**" },
		{ '6', "**----***---**" },
		{ '7', "*---*----*----" },
		{ '8', "**---****---**" },
		{ '9', "**---***----**" },
		{ 'A', "**---****---*-" },
		{ 'B', "*--*-*-*--*-**" },
		{ 'C', "**------*----*" },
		{ 'D', "*--*-*----*-**" },
		{ 'E', "**----***----*" },
		{ 'F', "**----***-----" },
		{ 'G', "**-----**---**" },
		{ 'H', "-*---****---*-" },
		{ 'I', "*--*------*--*" },
		{ 'J', "-----*--*---**" },
		{ 'K', "-*--*-*-*--*--" },
		{ 'L', "-*------*----*" },
		{ 'M', "-**-**--*---*-" },
		{ 'N', "-**--*--*--**-" },
		{ 'O', "**---*--*---**" },
		{ 'P', "**---****-----" },
		{ 'Q', "**---*--*--***" },
		{ 'R', "**---****--*--" },
		{ 'S', "**----**----**" },
		{ 'T', "*--*------*---" },
		{ 'U', "-*---*--*---**" },
		{ 'V', "-*--*---**----" },
		{ 'W', "-*---*--**-**-" },
		{ 'X', "--*-*----*-*--" },
		{ 'Y', "--*-*-----*---" },
		{ 'Z', "*---*----*---*" },
		{ '/', "----*----*----" },
		{ '-', "------**------" },
		{ '+', "---*--**--*---" },
		{ '*', "--***----***--" },
		{ '|', "---*------*---" },
		{ '\\', "--*--------*--" },
		{ '_', "-------------*" },
		{ '$', "**-*--**--*-**" },
		{ '\u00AC', "------**----*-" }, // Not
		{ '\u00AF', "*-------------" }, // Overline
		{ '\u00B1', "---*--**--*--*" }, // Plus Minus
		{ '\u0393', "**------*-----" }, // Capital Gamma
		{ '\u03A0', "**---*--*---*-" }, // Capital Pi
		{ '\u03A3', "*-*------*---*" }, // Capital Sigma
		{ '\u03A8', "--***-----*---" }, // Capital Psi
		{ '\u225A', "--*-*-**-----*" }, // ≚
		{ '\u2261', "*-----**-----*" }, // Congruent
		{ '\u2262', "*---*-**-*---*" }, // Not congruent to
	};
	readonly static Color[] colorOptions = new[] { Color.black, Color.grey, Color.white };
	List<char> chrDigits;
	List<bool> invChrDigit;
	const string serialNoAllowedChrs = "0123456789ABCDEFGHIJKLMNPQRSTUVWXZ"; // O and Y will never appear for ambiguity reasons.
	IEnumerable<string> ignoreListIDs = DefaultIgnoreList.ignoreListIDs;
	[SerializeField]
	bool debugCharacters = true;
	bool interactable = false, readyToSubmit = false, colorblindDetected, modSolved = false;

	_1ll4Modifier selectedDifficulty;

	int idxCycle = -1, curStageIdx, maxStageIdx, idxColorSelected = 0;

	static int modIDCnt;
	int moduleID;

	long seedUponSubmission;

	FlyersBossierSettings globalSettings;

	void Awake()
    {
		try
		{
			var obtainedSettings = new ModConfig<FlyersBossierSettings>("FlyersBossierSettings");
			globalSettings = obtainedSettings.Settings;
			obtainedSettings.Settings = globalSettings;
			selectedDifficulty = globalSettings._1ll4Difficulty;
		}
		catch
		{
			selectedDifficulty = _1ll4Modifier.Normal;
		}
		finally
		{
			try
			{
				colorblindDetected = colorblindMode.ColorblindModeActive;
			}
			catch
			{
				colorblindDetected = false;
			}
		}
	}
	void QuickLog(string toLog, params object[] args)
    {
		Debug.LogFormat("[{0} #{1}] {2}", modSelf.ModuleDisplayName, moduleID, string.Format(toLog, args));
    }
	// Use this for initialization
	void Start () {
		moduleID = ++modIDCnt;
		chrDigits = new List<char>();
		invChrDigit = new List<bool>();
		var ignoreListRepo = bossHandler.GetIgnoredModuleIDs(modSelf);
		if (ignoreListRepo != null)
			ignoreListIDs = ignoreListRepo;
		else
			Debug.LogWarningFormat("[{0}] Using default ignore list! This will cause some issues with other bosses present.", modSelf.ModuleDisplayName);
		if (debugCharacters)
		{
			chrDigits.AddRange(chrSegmentDisplay.Keys);
			invChrDigit.AddRange(Enumerable.Repeat(false, chrSegmentDisplay.Keys.Count));
		}
		else
        {
			var allComboSegments = new Dictionary<string, int[]>();

        }
		for (var x = 0; x < clrSelectables.Length; x++)
		{
			var y = x;
			clrSelectables[x].OnInteract += delegate { HandleColorSelectable(y); return false; };
		}
		modSelf.OnActivate += ActivateModule;
	}
	void ActivateModule()
    {
		interactable = true;
		if (debugCharacters) return;
		var unignoredModCnt = bombInfo.GetSolvableModuleIDs().Count(a => !ignoreListIDs.Contains(a));
		maxStageIdx = unignoredModCnt;
		var stageCntPastInitial = unignoredModCnt - 1;
		var serialNoExcludeForbidden = bombInfo.GetSerialNumber().Where(a => serialNoAllowedChrs.Contains(a));
	}

	IEnumerator CycleInitialDigits()
    {
		yield break;
    }

	void HandleColorSelectable(int idx)
    {
		if (!interactable) return;
		
		if (debugCharacters)
		{
			switch (idx)
			{
				case 0: idxCycle++; break;
				case 1: idxColorSelected++; break;
				case 2: invChrDigit[idxCycle] ^= true; break;
				case 3: idxColorSelected--; break;
				case 4: idxCycle--; break;
			}
			idxCycle = ((idxCycle % chrDigits.Count) + chrDigits.Count) % chrDigits.Count;
			idxColorSelected = ((idxColorSelected % chrDigits.Count) + chrDigits.Count) % chrDigits.Count;
			UpdateSegmentsDebug();
		}
		else
			UpdateSegments();
    }

	void UpdateSegments()
    {
		//var allChrs = chrSegmentDisplay.Keys.ToList();
		var usedVal = chrSegmentDisplay[chrDigits.ElementAt(idxCycle)];
		for (var x = 0; x < segmentRenderers.Length; x++)
			segmentRenderers[x].material.color = usedVal[x] == '*' ^ invChrDigit[idxCycle] ? Color.white : Color.black;
    }
	void UpdateSegmentsDebug()
    {
		//var allChrs = chrSegmentDisplay.Keys.ToList();
		var usedVal = chrSegmentDisplay[chrDigits.ElementAt(idxCycle)];
		var usedVal2 = chrSegmentDisplay[chrDigits.ElementAt(idxColorSelected)];
		for (var x = 0; x < segmentRenderers.Length; x++)
			segmentRenderers[x].material.color = colorOptions[(usedVal[x] == '*' ^ invChrDigit[idxCycle] ? 1 : 0) + (usedVal2[x] == '*' ^ invChrDigit[idxColorSelected] ? 1 : 0)];
    }

	// Update is called once per frame
	void Update () {
		
	}
}
