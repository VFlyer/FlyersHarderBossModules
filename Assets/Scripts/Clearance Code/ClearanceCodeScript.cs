using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text.RegularExpressions;
using KeepCoding;

public class ClearanceCodeScript : MonoBehaviour {

	public KMBombModule modself;
	public KMBossModule bossHandler;
	public KMBombInfo bombInfo;
	public KMSelectable mainSelectable;
	public KMAudio mAudio;
	public KMSelectable[] btnSelectables;
	public TextMesh[] digitsMesh;
	public MeshRenderer[] buttonOutlineRenders, ancilleryBtnRenders, miscRenderers;
	public TextMesh inputText, settingsTxt;
	public CircleAligner aligner;
	public MeshRenderer lockRenderer;
	public Texture unlockIcon;

	MeshRenderer[] usedButtonOutlineRenders, usedAncilleryBtnRenders;
	TextMesh[] usedDigitsMesh;

	static List<string> overrideStrings = new List<string>();
	static long lastModIDLoad = 0;

	static int modIDCnt;
	int moduleID;
	int curStageIdx, lastNonignoredSolveCount, reachableStageIdx;
	List<ClearCodeStage> allStages;
	string curInput = "";

	int digitsToInput = 4, baseAuthorPPAScore = 2;
	const string base16Digits = "0123456789ABCDEF";

	string[] ignoreListIDs = DefaultIgnoreList.ignoreListIDs;

	bool activated = false, inputting = false, moduleSolved = false, interactable, requireLastStage, shuffleDigitsAlways, disableStrike, TPRequireDelayStrike;
	int digitsToDisplay, stagesPerCodeOperSwap, stagesPerDistOperSwap, stagesPerDistDirSwap, stagesPerSeqDirSwap, colorCycleLimit;
	IEnumerator animHandler;
	static readonly Color transWhite = new Color(1, 1, 1, 0);
	static readonly Color[] cyclingColors = new[] { Color.cyan, Color.magenta, new Color(.5f, 1, 1), new Color(1, .5f, 1), new Color(.1f, .1f, 1), new Color(.5f, .5f, 1), new Color(0, .5f, 1), new Color(.5f, 0, 1) };

	static readonly Dictionary<int, string[]> possibleTextsWrong = new Dictionary<int, string[]> {
		{ 2, new[] { "NO" } },
		{ 3, new[] { "NON" } },
		{ 4, new[] { "N0PE", "NONO", "FA1L" } },
		{ 5, new[] { "NOPE1", "NOT1T" } },
		{ 6, new[] { "UUR0N9" } },
		{ 7, new[] { "NONONON" } },
		{ 8, new[] { "NONONONO" } },
		{ 9, new[] { "1NCORRECT" } },
		{ 10, new[] { "NOTCORRECT" } },
		{ 11, new[] { "NONONONONON" } },
		{ 12, new[] { "N0PEN0PEN0PE", "NONONONONONO" } },
		{ 13, new[] { "NONONONONONON" } },
		{ 14, new[] { "NONONONONONONO" } },
		{ 15, new[] { "NONONONONONONON" } },
		{ 16, new[] { "ABS0LUTELYUUR0N9", "N0PEN0PEN0PEN0PE" } },
	}, possibleTextsCorrect = new Dictionary<int, string[]> {
		{ 2, new[] { "YO" } },
		{ 3, new[] { "YES", "YEP" } },
		{ 4, new[] { "YEAH", "5URE" } },
		{ 5, new[] { "R19HT" } },
		{ 6, new[] { "ACCEPT" } },
		{ 7, new[] { "CORRECT", "PROCEED" } },
		{ 8, new[] { "88888888" } },
		{ 9, new[] { "888888888" } },
		{ 10, new[] { "8888888888" } },
		{ 11, new[] { "88888888888" } },
		{ 12, new[] { "888888888888" } },
		{ 13, new[] { "8888888888888" } },
		{ 14, new[] { "88888888888888" } },
		{ 15, new[] { "888888888888888" } },
		{ 16, new[] { "8888888888888888" } },
	}, possibleTextsLast = new Dictionary<int, string[]> {
		{ 2, new[] { "88" } },
		{ 3, new[] { "888" } },
		{ 4, new[] { "DONE", "8888" } },
		{ 5, new[] { "88888" } },
		{ 6, new[] { "0PENED", "F1n1sh", "888888" } },
		{ 7, new[] { "F1n1sh1", "AllD0nE", "8888888" } },
		{ 8, new[] { "88888888" } },
		{ 9, new[] { "888888888" } },
		{ 10, new[] { "8888888888" } },
		{ 11, new[] { "88888888888" } },
		{ 12, new[] { "888888888888" } },
		{ 13, new[] { "8888888888888" } },
		{ 14, new[] { "88888888888888" } },
		{ 15, new[] { "888888888888888" } },
		{ 16, new[] { "8888888888888888" } },
	};
	static readonly Dictionary<int, string> numberToWord = new Dictionary<int, string>
	{
		{ 2, "two" },
		{ 3, "three" },
		{ 4, "four" },
		{ 5, "five" },
		{ 6, "six" },
		{ 7, "seven" },
		{ 8, "eight" },
		{ 9, "nine" },
		{ 10, "ten" },
		{ 11, "eleven" },
		{ 12, "twelve" },
		{ 13, "thirteen" },
		{ 14, "fourteen" },
		{ 15, "fifteen" },
		{ 16, "sixteen" },
	};

	FlyersBossierSettings bossSettings;

	void QuickLog(string toLog = "", params object[] args)
    {
		Debug.LogFormat("[{0} #{1}] {2}", modself.ModuleDisplayName, moduleID, string.Format(toLog, args));
    }
	void QuickLogDebug(string toLog = "", params object[] args)
    {
		Debug.LogFormat("<{0} #{1}> {2}", modself.ModuleDisplayName, moduleID, string.Format(toLog, args));
    }
	void Awake()
    {
		try
		{
			ModConfig<FlyersBossierSettings> settingsFile = new ModConfig<FlyersBossierSettings>("FlyersBossierSettings");
			bossSettings = settingsFile.Settings;
			settingsFile.Settings = bossSettings;
			if (bossSettings.CCEasyMode)
            {
				colorCycleLimit = Mathf.Clamp(bossSettings.CCColorsToCycle, 2, 8);
				digitsToDisplay = 10;
                digitsToInput = 4;
				requireLastStage = false;
				stagesPerCodeOperSwap = 0;
				stagesPerSeqDirSwap = 0;
				stagesPerDistDirSwap = 0;
				stagesPerDistOperSwap = 0;
				shuffleDigitsAlways = false;
			}
            else
			{
				colorCycleLimit = Mathf.Clamp(bossSettings.CCColorsToCycle, 2, 8);
				digitsToDisplay = Mathf.Clamp(bossSettings.CCDigitsDisplayed, 3, 16);
				digitsToInput = Mathf.Clamp(bossSettings.CCDigitsRequired, 2, digitsToDisplay);
				requireLastStage = bossSettings.CCRequireLastStage;
				stagesPerCodeOperSwap = bossSettings.CCStagesPerCodeOperSwap;
				stagesPerSeqDirSwap = bossSettings.CCStagesPerSeqDirSwap;
				stagesPerDistDirSwap = bossSettings.CCStagesPerDisDirSwap;
				stagesPerDistOperSwap = bossSettings.CCStagesPerDistOperSwap;
				shuffleDigitsAlways = bossSettings.CCShuffleDigitsAlways;
			}
		}
		catch
		{
			Debug.LogWarning("Clearance Code settings do not work as intended! Using default settings!");
			digitsToDisplay = 10;
			digitsToInput = 4;
			requireLastStage = true;
			stagesPerCodeOperSwap = 0;
			stagesPerSeqDirSwap = 2;
			stagesPerDistDirSwap = 1;
			stagesPerDistOperSwap = 0;
			shuffleDigitsAlways = false;
		}
	}
	bool TryOverrideMission()
	{
		var successful = false;
		var missionID = Game.Mission.ID ?? "freeplay";
		switch (missionID)
		{
			case "freeplay":
			case "custom":
				QuickLogDebug("Mission detected as freeplay/custom bomb. Not allowed to override settings.");
				return false;
		}
		var description = Game.Mission.Description ?? "";
		var regexCCOverrideAll = Regex.Matches(description, @"\[CCOverride\]\s\d+,\d+,(true|false),\-?\d+,\-?\d+,\-?\d+,\-?\d+,(true|false)", RegexOptions.CultureInvariant);
		if (!overrideStrings.Any())
			foreach (Match match in regexCCOverrideAll)
				overrideStrings.Add(match.Value);

		var curIdxOverride = lastModIDLoad - moduleID;
		var curOverrideString = overrideStrings.ElementAtOrDefault((int)curIdxOverride);
		if (!string.IsNullOrEmpty(curOverrideString))
		{
			
			try
			{
				successful = true;
				var lastPartOnlySplit = curOverrideString.Split().Last().Split(',');
				digitsToDisplay = Mathf.Clamp(int.Parse(lastPartOnlySplit[0]), 3, 16);
				digitsToInput = Mathf.Clamp(int.Parse(lastPartOnlySplit[1]), 2, digitsToDisplay);
				requireLastStage = bool.Parse(lastPartOnlySplit[2]);
				stagesPerCodeOperSwap = int.Parse(lastPartOnlySplit[3]);
				stagesPerSeqDirSwap = int.Parse(lastPartOnlySplit[4]);
				stagesPerDistDirSwap = int.Parse(lastPartOnlySplit[5]);
				stagesPerDistOperSwap = int.Parse(lastPartOnlySplit[6]);
				shuffleDigitsAlways = bool.Parse(lastPartOnlySplit[7]);
			}
			catch
			{
				successful = false;
				QuickLogDebug("EXCEPTION THROWN, OVERRIDE COUNTED AS FAILURE.");
			}
		}
		return successful;
	}
	// Use this for initialization
	void Start () {
		moduleID = ++modIDCnt;
		if (lastModIDLoad < moduleID)
		{
			//QuickLogDebug("Last ID loaded is later.");
			lastModIDLoad = moduleID;
			overrideStrings.Clear();
		}
		var obtainedIds = bossHandler.GetIgnoredModuleIDs(modself);
		if (obtainedIds == null || !obtainedIds.Any())
			QuickLogDebug("Using default ignore list! This will cause issues when multiple bosses are present!");
		else
			ignoreListIDs = obtainedIds;
		TryOverrideMission();
		modself.OnActivate += delegate {
			ActivateModule();
		};
		inputText.text = "";
		settingsTxt.text = "";
		foreach (var render in buttonOutlineRenders)
			render.enabled = false;
		foreach (var render in ancilleryBtnRenders)
		{
			render.enabled = false;
			render.material.color = Color.clear;
		}
		foreach (var render in miscRenderers)
			render.enabled = false;
		foreach (var txt in digitsMesh)
		{
			txt.text = "";
			txt.color = Color.clear;
		}
		for (var x = 0; x < btnSelectables.Length; x++)
        {
			var y = x;
			btnSelectables[x].OnInteract += delegate { if (activated && interactable) HandleBtnPress(y); return false; };
        }
	}
	void HandleBtnPress(int idx)
    {
		if (idx >= digitsToDisplay) return;
		mAudio.PlaySoundAtTransform("KPDFlick", btnSelectables[idx].transform);
		btnSelectables[idx].AddInteractionPunch(0.1f);
		if (!inputting || moduleSolved) return;
		if (allStages.Any())
        {
			var curStage = allStages[curStageIdx];
			curInput += base16Digits[curStage.inputDigitsLayout[idx]];
			inputText.text = curInput.PadRight(digitsToInput, '-');
			if (curInput.Length < digitsToInput) return;
			interactable = false;
			activated = false;
			if (animHandler != null)
				StopCoroutine(animHandler);
			animHandler = HandleDelayCheck(curStage);
			StartCoroutine(animHandler);
		}
		else
        {
			curInput += base16Digits[idx].ToString();
			inputText.text = curInput.PadRight(digitsToInput, '-');
			if (curInput.Length < digitsToInput) return;
			interactable = false;
			activated = false;
			if (animHandler != null)
				StopCoroutine(animHandler);
			animHandler = HandleDelayCheck();
			StartCoroutine(animHandler);
		}
    }
	void SolveModule()
    {
		moduleSolved = true;
		if (bombInfo.GetTime() < 60f)
			modself.HandlePass();
		if (animHandler != null)
			StopCoroutine(animHandler);
		animHandler = HandleSolveAnim();
		StartCoroutine(animHandler);
	}
	void CauseStrikeMercy()
    {
		modself.HandleStrike();
		if (animHandler != null)
			StopCoroutine(animHandler);
		animHandler = HandleMercyStage(allStages[curStageIdx], cyclingColors[curStageIdx % colorCycleLimit]);
		StartCoroutine(animHandler);
	}

	void ActivateModule()
    {
		// Handle circle animation mitigation.
		lockRenderer.enabled = false;
		aligner.AffectedObjects = aligner.AffectedObjects.Take(digitsToDisplay).ToArray();
		usedButtonOutlineRenders = buttonOutlineRenders.Take(digitsToDisplay).ToArray();
		usedAncilleryBtnRenders = ancilleryBtnRenders.Take(digitsToDisplay).ToArray();
		usedDigitsMesh = digitsMesh.Take(digitsToDisplay).ToArray();
		for (var x = digitsToDisplay; x < buttonOutlineRenders.Length; x++)
			buttonOutlineRenders[x].enabled = false;
		for (var x = digitsToDisplay; x < ancilleryBtnRenders.Length; x++)
			ancilleryBtnRenders[x].enabled = false;
		mainSelectable.Children = mainSelectable.Children.Take(digitsToDisplay).ToArray();
		mainSelectable.UpdateChildrenProperly();
		var sizeAdjusts = new[] { 0.003f, 0.0025f, 0.002f, 0.002f, 0.0015f, 0.0015f, 0.0015f, 0.001f, 0.001f, 0.001f, 0.001f, 0.001f, };
		inputText.characterSize = sizeAdjusts[Mathf.Clamp(digitsToInput - 4, 0, sizeAdjusts.Length - 1)];
		reachableStageIdx = bombInfo.GetSolvableModuleIDs().Count(a => !ignoreListIDs.Contains(a));
		allStages = new List<ClearCodeStage>();
		QuickLog("Watch out! This module has been updated to have a lot more settings and calculations!");
		QuickLog("{0} digits will be displayed, {1} of which are part of the initial code.", digitsToDisplay, digitsToInput >= digitsToDisplay ? "ALL" : digitsToInput.ToString());
		QuickLog("Initial digits of the code must be grabbed {0} from top{1}.", stagesPerSeqDirSwap >= 0 ? "clockwise" : "counterclockwise",
			stagesPerSeqDirSwap == 0 ? "" : stagesPerSeqDirSwap == 1 ? ", then alternate this direction every stage." : string.Format(", then alternate this direction every {0} stages", Mathf.Abs(stagesPerSeqDirSwap)));
		QuickLog("Distances between the digits must be obtained going {0}{1}.", stagesPerDistDirSwap >= 0 ? "clockwise" : "counterclockwise",
			stagesPerDistDirSwap == 0 ? "" : stagesPerDistDirSwap == 1 ? ", then alternating every stage" : string.Format(", then alternating every {0} stages", Mathf.Abs(stagesPerDistDirSwap)));
		QuickLog("For stage 1{1}, {0} the distances to each digit of the code.{2}", stagesPerDistOperSwap >= 0 ? "add" : "subtract",
			stagesPerCodeOperSwap == 0 ? " and all stages afterwards" : "",
			stagesPerDistOperSwap == 0 ? "" : stagesPerDistOperSwap == 1 ? " Switch between adding/subtracting every stage." : string.Format(", Switch between adding/subtracting every {0} stages", Mathf.Abs(stagesPerDistOperSwap)));
		if (requireLastStage)
		{
			QuickLog("For each stage after the first, you will need the last stage's code.");
			QuickLog("For stage 2{1}, {0} each digit of the code for this stage.{2}", stagesPerCodeOperSwap >= 0 ? "subtract each digit of the final code from" : "add each digit from the final code to",
				stagesPerCodeOperSwap == 0 ? " and all stages afterwards" : "",
				stagesPerCodeOperSwap == 0 ? "" : stagesPerCodeOperSwap == 1 ? " Switch between adding/subtracting every stage after stage 2." : string.Format(" Switch between adding/subtracting every {0} stages after stage 2.", Mathf.Abs(stagesPerCodeOperSwap)));
		}
		if (shuffleDigitsAlways)
			QuickLog("Digits will be shuffled every stage regardless if the module is ready to input or not.");
		else
			QuickLog("Digits will only be shuffled when the module is ready to input.");
		QuickLog("Non-ignored modules detected: {0}", reachableStageIdx);


		var lastDisplayedDigits = Enumerable.Range(0, digitsToDisplay).ToArray().Shuffle();
		QuickLog("Initial digits in clockwise order, from top: {0}", lastDisplayedDigits.Select(a => base16Digits[a]).Join(","));
		var lastFinalCode = "";
		var ccwIntDigits = stagesPerSeqDirSwap < 0;
		var ccwDistCalc = stagesPerDistDirSwap < 0;
		var subDistCalc = stagesPerDistOperSwap < 0;
		var addCodeCalc = stagesPerCodeOperSwap < 0;

		for (var x = 0; x < reachableStageIdx; x++)
		{
			if (x > 0 && stagesPerSeqDirSwap != 0 && x % stagesPerSeqDirSwap == 0) // 2nd condition prevents division by 0.
				ccwIntDigits ^= true; // If X is divisible by the no. of stages before swapping the direction of obtaining initial digits...
			if (x > 0 && stagesPerDistDirSwap != 0 && x % stagesPerDistDirSwap == 0) // 2nd condition prevents division by 0.
				ccwDistCalc ^= true; // If X is divisible by the no. of stages before swapping the direction of the distances between digits...
			if (x > 0 && stagesPerDistOperSwap != 0 && x % stagesPerDistOperSwap == 0) // 2nd condition prevents division by 0.
				subDistCalc ^= true; // If X is divisible by the no. of stages before swapping the operation of the distances between digits...
			if (x > 1 && stagesPerCodeOperSwap != 0 && (x - 1) % stagesPerCodeOperSwap == 0) // 2nd condition prevents division by 0.
				addCodeCalc ^= true; // If X is divisible by the no. of stages before swapping final code operations...
			QuickLog("--------------- Stage {0} ---------------", x + 1);
			var newStage = new ClearCodeStage();
			if (shuffleDigitsAlways)
			{
				lastDisplayedDigits = lastDisplayedDigits.ToArray().Shuffle();
				QuickLog("The digits will be shuffled to the following in clockwise order, from top: {0}", lastDisplayedDigits.Select(a => base16Digits[a]).Join(","));
			}
			var pickedDigitIdxesCurStage = Enumerable.Range(0, digitsToDisplay).ToArray().Shuffle().Take(digitsToInput).ToList();
			pickedDigitIdxesCurStage.Sort();
			if (ccwIntDigits)
			{
				pickedDigitIdxesCurStage = pickedDigitIdxesCurStage.Select(a => (digitsToDisplay - a) % digitsToDisplay).ToList();
				QuickLog("Picked digits in counter-clockwise order: {0}", pickedDigitIdxesCurStage.Select(a => base16Digits[lastDisplayedDigits[a]]).Join(","));
			}
			else
				QuickLog("Picked digits in clockwise order: {0}", pickedDigitIdxesCurStage.Select(a => base16Digits[lastDisplayedDigits[a]]).Join(","));
			newStage.idxDigitsLit = pickedDigitIdxesCurStage.ToArray();
			newStage.preInputDigitsLayout = lastDisplayedDigits.ToArray();

			var newDisplayedDigits = Enumerable.Range(0, digitsToDisplay).ToList();
			newDisplayedDigits.Shuffle();
			newStage.inputDigitsLayout = newDisplayedDigits.ToArray();
			// Stage calculation procedures
			QuickLog("When inputting the digits will be shuffled to the following in clockwise order, from top: {0}", newDisplayedDigits.Select(a => base16Digits[a]).Join(","));
			var distancesFromTargetCW = pickedDigitIdxesCurStage.Select(a => PMod(newDisplayedDigits.IndexOf(lastDisplayedDigits[a]) - a, digitsToDisplay)).ToArray();
			if (ccwDistCalc)
				QuickLog("Distances counter-clockwise from previous position: {0}", distancesFromTargetCW.Select(a => PMod(digitsToDisplay - a, digitsToDisplay)).Join(","));
			else
				QuickLog("Distances clockwise from previous position: {0}", distancesFromTargetCW.Join(","));
			var inputStr = "";
			for (var y = 0; y < digitsToInput; y++) // Add the distances for each digit that were lit up.
			{
				var idxPickedDigit = pickedDigitIdxesCurStage[y];
				inputStr += base16Digits[PMod(lastDisplayedDigits[idxPickedDigit] + (subDistCalc ? -1 : 1) * (ccwDistCalc ? digitsToDisplay - distancesFromTargetCW[y] : distancesFromTargetCW[y]), digitsToDisplay)];
			}
			if (x > 0 && requireLastStage)
            {
				QuickLog("The last stage's code to input was {0}, {2} each digit of this code from {1}.", lastFinalCode, inputStr, addCodeCalc ? "adding" : "subtracting");
				var numsEachLast = lastFinalCode.Select(a => base16Digits.IndexOf(a)).ToArray();
				var newInputStr = "";
				for (var n = 0; n < inputStr.Length; n++)
					newInputStr += base16Digits[PMod(base16Digits.IndexOf(inputStr[n]) + (addCodeCalc ? 1 : -1) * numsEachLast[n], digitsToDisplay)];
				inputStr = newInputStr;
            }
			lastFinalCode = inputStr;
			newStage.expectedInput = inputStr;
			QuickLog("Expected code for stage {0}: {1}", x + 1, inputStr);
			allStages.Add(newStage);
			lastDisplayedDigits = newDisplayedDigits.ToArray();
		}

		TwitchHelpMessage = string.Format("Input the {1} digit code with \"!{0} {2}\" or \"!{0} submit {2}\", where # is the digit of your code.", "{0}", numberToWord[digitsToInput], Enumerable.Repeat('#', digitsToInput).Join(""));
		QuickLog("--------------- User Interactions ---------------");
		activated = true;
		animHandler = HandleStartupAnim();
		StartCoroutine(animHandler);
	}

	IEnumerator HandleDelayCheck(ClearCodeStage curStage = null)
    {
		yield return new WaitForSeconds(0.4f);
		if (curStage == null)
		{
			var solveTextPicked = possibleTextsLast[digitsToInput].Concat(new[] { curInput }).PickRandom();
			QuickLog("All accessible stages have been completed. Open sesame!");
			inputText.text = solveTextPicked;
			mAudio.PlaySoundAtTransform("KPDSolve", transform);
			yield return BrieflyFlashInputTextColor(Color.green, 20);
			inputText.text = "";
			yield return HandleSolveAnim(false);
			SolveModule();
			yield break;
		}
		if (curInput == curStage.expectedInput || disableStrike)
		{
			if (curInput == curStage.expectedInput)
				QuickLog("Accepted intended code for stage {0}.", curStageIdx + 1);
			else
				QuickLog("Incorrect code for stage {0}. Skipping current stage anyway.", curStageIdx + 1);
			curStageIdx++;
			if (curStageIdx >= reachableStageIdx)
			{
				var solveTextPicked = possibleTextsLast[digitsToInput].Concat(new[] { curInput }).PickRandom();
				QuickLog("All accessible stages have been completed. Open sesame!");
				inputText.text = solveTextPicked;
				mAudio.PlaySoundAtTransform("KPDSolve", transform);
				yield return BrieflyFlashInputTextColor(Color.green, 20);
				inputText.text = "";
				SolveModule();
				yield break;
			}
			else
			{
				mAudio.PlaySoundAtTransform("KPDAccepted", transform);
				var correctAdvTextPicked = possibleTextsCorrect[digitsToInput].PickRandom();
				inputText.text = correctAdvTextPicked;
				yield return BrieflyFlashInputTextColor(Color.green, 6);
				inputting = false;
				mAudio.PlaySoundAtTransform("Blip", transform);
				if (animHandler != null)
					StopCoroutine(animHandler);
				animHandler = HandleDisplayStage(allStages[curStageIdx], false);
				StartCoroutine(animHandler);
			}
		}
		else
		{
			TPRequireDelayStrike = true;
			QuickLog("Denied code for stage {0}: {1}", curStageIdx + 1, curInput);
			var wrongTextPicked = possibleTextsWrong[digitsToInput].PickRandom();
			inputText.text = wrongTextPicked;
			mAudio.PlaySoundAtTransform("KPDError 2", transform);
			yield return BrieflyFlashInputTextColor(Color.red);
			CauseStrikeMercy();
			inputText.text = Enumerable.Repeat("-", digitsToInput).Join("");
			TPRequireDelayStrike = false;
		}
		curInput = "";
		interactable = true;
		activated = true;
	}
	IEnumerator DelayDisplayTextLastInput()
    {
		yield return null;
		inputText.text = allStages[curStageIdx - 1].expectedInput;
	}

	IEnumerator HandleMercyStage(ClearCodeStage curStage, Color markerColor)
	{
		if (requireLastStage && curStageIdx > 0)
			StartCoroutine(DelayDisplayTextLastInput());
		var stepCur = 0;
		while (true)
        {
			var lastColorsOutlines = usedButtonOutlineRenders.Select(a => a.material.color).ToArray();
			var lastColorsCenters = usedAncilleryBtnRenders.Select(a => a.material.color).ToArray();
			var lastColorsTexts = usedDigitsMesh.Select(a => a.color).ToArray();
			if (stepCur >= digitsToInput)
            {
				for (float t = 0; t < 1f; t += Time.deltaTime)
				{
					for (var x = 0; x < usedButtonOutlineRenders.Length; x++)
						usedButtonOutlineRenders[x].material.color = Color.Lerp(lastColorsOutlines[x], Color.white, t);
					for (var x = 0; x < usedAncilleryBtnRenders.Length; x++)
						usedAncilleryBtnRenders[x].material.color = Color.Lerp(lastColorsCenters[x], transWhite, t);
					for (var x = 0; x < usedDigitsMesh.Length; x++)
						usedDigitsMesh[x].color = Color.Lerp(lastColorsTexts[x], Color.white, t);
					yield return null;
				}
				foreach (MeshRenderer x in usedButtonOutlineRenders)
					x.material.color = Color.white;
				foreach (MeshRenderer x in usedAncilleryBtnRenders)
					x.material.color = transWhite;
				foreach (var x in usedDigitsMesh)
					x.color = Color.white;
			}
			else
            {
				var litInitCurStage = curStage.idxDigitsLit[stepCur];
				var postInitCurStage = curStage.inputDigitsLayout.ToList().IndexOf(curStage.preInputDigitsLayout[litInitCurStage]);
				for (float t = 0; t < 1f; t += Time.deltaTime)
				{
					for (var x = 0; x < usedButtonOutlineRenders.Length; x++)
					{
						usedButtonOutlineRenders[x].material.color = Color.Lerp(lastColorsOutlines[x], postInitCurStage == x ? transWhite : litInitCurStage == x ? markerColor : Color.white, t);
					}
					for (var x = 0; x < usedAncilleryBtnRenders.Length; x++)
					{
						usedAncilleryBtnRenders[x].enabled = true;
						usedAncilleryBtnRenders[x].material.color = Color.Lerp(lastColorsCenters[x], postInitCurStage == x ? markerColor : transWhite, t);
					}
					for (var x = 0; x < usedDigitsMesh.Length; x++)
						usedDigitsMesh[x].color = Color.Lerp(lastColorsTexts[x], postInitCurStage == x ? Color.black : litInitCurStage == x ? markerColor : Color.white, t);
					yield return null;
				}
				foreach (MeshRenderer x in usedButtonOutlineRenders)
                    x.material.color = Color.white;
				for (int x = 0; x < usedButtonOutlineRenders.Length; x++)
				{
					usedButtonOutlineRenders[x].material.color = postInitCurStage == x ? transWhite : litInitCurStage == x ? markerColor : Color.white;
				}
				for (var x = 0; x < usedAncilleryBtnRenders.Length; x++)
				{
					usedAncilleryBtnRenders[x].enabled = postInitCurStage == x;
					usedAncilleryBtnRenders[x].material.color = postInitCurStage == x ? markerColor : transWhite;
				}
				for (var x = 0; x < usedDigitsMesh.Length; x++)
					usedDigitsMesh[x].color = postInitCurStage == x ? Color.black : litInitCurStage == x ? markerColor : Color.white;
			}
			stepCur = (stepCur + 1) % (digitsToInput + 1);
			yield return new WaitForSeconds(1f);
		}
	}
	IEnumerator BrieflyFlashInputTextColor(Color flashingColor, int iterationCount = 10)
    {
		foreach (var render in miscRenderers)
			render.material.color = flashingColor;
		for (var x = 0; x < iterationCount; x++)
		{
			foreach (var render in miscRenderers)
				render.enabled = true;
			inputText.color = flashingColor;
			yield return new WaitForSeconds(0.05f);
			foreach (var render in miscRenderers)
				render.enabled = false;
			inputText.color = Color.white;
			yield return new WaitForSeconds(0.05f);
		}
	}
	IEnumerator HandleSolveAnim(bool hasLastStage = true)
	{
		//var reversed = Random.value < 0.5f;
		var lastColorsAllAncilleryMats = usedAncilleryBtnRenders.Select(a => a.material.color).ToArray();
		var lastColorsAllOutlineMats = usedButtonOutlineRenders.Select(a => a.material.color).ToArray();
		var lastColorsTexts = usedDigitsMesh.Select(a => a.color).ToArray();
		lockRenderer.enabled = true;
		for (int n = 0; n < usedButtonOutlineRenders.Length; n++)
		{
			mAudio.PlaySoundAtTransform("BinMemSolve", transform);
			var outRender = usedButtonOutlineRenders[hasLastStage ? allStages[reachableStageIdx - 1].inputDigitsLayout.ToList().IndexOf(n) : n];
			var ancilRender = usedAncilleryBtnRenders[hasLastStage ? allStages[reachableStageIdx - 1].inputDigitsLayout.ToList().IndexOf(n) : n];
			var txtMesh = usedDigitsMesh[hasLastStage ? allStages[reachableStageIdx - 1].inputDigitsLayout.ToList().IndexOf(n) : n];
			for (float t = 0; t < 1f; t += Time.deltaTime * 10)
			{
				outRender.material.color = Color.Lerp(lastColorsAllOutlineMats[n], Color.white, t);
				ancilRender.material.color = Color.Lerp(lastColorsAllAncilleryMats[n], transWhite, t);
				txtMesh.color = Color.Lerp(lastColorsTexts[n], Color.white, t);
				yield return null;
			}
			ancilRender.enabled = false;
			for (float t = 0; t < 1f; t += Time.deltaTime * 10)
			{
				outRender.material.color = Color.Lerp(Color.white, transWhite, t);
				txtMesh.color = Color.Lerp(Color.white, transWhite, t);
				yield return null;
			}
			outRender.material.color = transWhite;
			txtMesh.color = transWhite;
		}
		mAudio.PlaySoundAtTransform("SSecKeyturn", transform);
		lockRenderer.material.mainTexture = unlockIcon;
		modself.HandlePass();
	}
	IEnumerator HandleStartupAnim()
    {
		foreach (var render in usedButtonOutlineRenders)
			render.enabled = true;
		var reversed = Random.value < 0.5f;
		for (float t = 0; t < 1f; t += Time.deltaTime)
        {
			aligner.radius = t * 6;
			aligner.percentOffset = reversed ? (1f - t) : t;
			aligner.UpdatePositions();
			foreach (var render in usedButtonOutlineRenders)
				render.material.color = new Color(1f, 1f, 1f, t);
			inputText.text = "".PadRight((int)(digitsToInput * t), '0');
			yield return null;
		}
		aligner.radius = 6f;
		aligner.percentOffset = 0;
		aligner.UpdatePositions();
		foreach (var render in usedButtonOutlineRenders)
			render.material.color = Color.white;
		interactable = true;
		if (allStages.Any())
		{
			mAudio.PlaySoundAtTransform("Blip", transform);
			StartCoroutine(HandleDisplaySettingsTxt());
			yield return HandleDisplayStage(allStages.First());
		}
		else
		{
			inputting = true;
			mAudio.PlaySoundAtTransform("KPDScan", transform);
			yield return HandleDisplayInputFake();
		}
	}
	IEnumerator ShakeButtonsGently()
    {
		var reversed = Random.value < 0.5f;
		for (float t = 0; t < 1f; t += Time.deltaTime * 2f)
		{
			aligner.percentOffset = 0.05f * ((reversed ? Easing.InOutSine(t + 0.25f, 0f, 1f, 0.5f) : Easing.InOutSine(1.25f - t, 0f, 1f, 0.5f)) - 0.5f);
			aligner.UpdatePositions();
			yield return null;
		}
		aligner.percentOffset = 0;
		aligner.UpdatePositions();
	}
	IEnumerator HandleDisplayInputFake(float speed = 2f)
    {
		StartCoroutine(ShakeButtonsGently());
		var lastColorsAllAncilleryMats = ancilleryBtnRenders.Select(a => a.material.color).ToArray();
		var lastColorsAllOutlineMats = buttonOutlineRenders.Select(a => a.material.color).ToArray();
		var lastColorsTexts = digitsMesh.Select(a => a.color).ToArray();
		var randomDigitsLayout = new string[5];
		var randomDigitsInput = new string[5];
		var padded0s = Enumerable.Repeat('0', digitsToInput).Join("");
		long maxValueRNG = long.Parse("1" + padded0s);
		for (var x = 0; x < 5; x++)
		{
			var newString = "";
			for (var y = 0; y < 10; y++)
				newString += Random.Range(0, 10).ToString();
			randomDigitsLayout[x] = newString;
			randomDigitsInput[x] = Random.Range(0, maxValueRNG).ToString(padded0s);
		}
		for (float t = 0; t < 1f; t += Time.deltaTime * speed)
		{
			for (var x = 0; x < usedButtonOutlineRenders.Length; x++)
			{
				usedButtonOutlineRenders[x].enabled = true;
				usedButtonOutlineRenders[x].material.color = Color.Lerp(lastColorsAllOutlineMats[x], Color.white, t);
			}
			for (var x = 0; x < usedAncilleryBtnRenders.Length; x++)
			{
				usedAncilleryBtnRenders[x].enabled = true;
				usedAncilleryBtnRenders[x].material.color = Color.Lerp(lastColorsAllAncilleryMats[x], transWhite, t);
			}
			for (var x = 0; x < usedDigitsMesh.Length; x++)
			{
				usedDigitsMesh[x].color = Color.Lerp(lastColorsTexts[x], Color.white, t);
				usedDigitsMesh[x].text = randomDigitsLayout[(int)Mathf.Lerp(0, 5, t)][x].ToString();
			}
			inputText.text = randomDigitsInput[(int)Mathf.Lerp(0, 5, t)];
			yield return null;
		}
		for (var x = 0; x < usedButtonOutlineRenders.Length; x++)
			usedButtonOutlineRenders[x].material.color = Color.white;
		for (var x = 0; x < usedAncilleryBtnRenders.Length; x++)
		{
			usedAncilleryBtnRenders[x].enabled = false;
			usedAncilleryBtnRenders[x].material.color = transWhite;
		}
		for (var x = 0; x < usedDigitsMesh.Length; x++)
		{
			usedDigitsMesh[x].color = Color.white;
			usedDigitsMesh[x].text = x.ToString();
		}
		inputText.text = Enumerable.Repeat("-", digitsToInput).Join("");
	}

	IEnumerator HandleDisplaySettingsTxt()
    {
		var cyclingTxt = "0123456789abcdefhjlnoprtuy-";
		var settingsTextEncoded = string.Format("{0}{1}{2}{3}{7}{4}{8}{5}{9}{6}{10}",
			cyclingTxt[digitsToDisplay - 1],
			cyclingTxt[digitsToInput - 1],
			requireLastStage ? "t" : "f",
			Mathf.Clamp(stagesPerCodeOperSwap, -allStages.Count, allStages.Count).ToString(),
			Mathf.Clamp(stagesPerSeqDirSwap, -allStages.Count, allStages.Count).ToString(),
			Mathf.Clamp(stagesPerDistDirSwap, -allStages.Count, allStages.Count).ToString(),
			Mathf.Clamp(stagesPerDistOperSwap, -allStages.Count, allStages.Count).ToString(),
			cyclingTxt[(digitsToDisplay + 2 * digitsToInput) % 15 + 10],
			cyclingTxt[(Mathf.Clamp(Mathf.Abs(stagesPerCodeOperSwap), 0, allStages.Count) + 2 * Mathf.Clamp(Mathf.Abs(stagesPerSeqDirSwap), 0, allStages.Count)) % 15 + 10],
			cyclingTxt[(Mathf.Clamp(Mathf.Abs(stagesPerDistDirSwap), 0, allStages.Count) + 2 * Mathf.Clamp(Mathf.Abs(stagesPerDistOperSwap), 0, allStages.Count)) % 15 + 10],
			shuffleDigitsAlways ? "t" : "f"
			);
		var curString = "";
		
		while (curString != settingsTextEncoded && lastNonignoredSolveCount == 0)
        {
			var curStringPosIdx = 0;
			while (cyclingTxt[curStringPosIdx] != settingsTextEncoded[curString.Length])
            {
				curStringPosIdx++;
				settingsTxt.text = curString + cyclingTxt[curStringPosIdx];
				yield return new WaitForSeconds(0.02f);
			}
			curString += cyclingTxt[curStringPosIdx];
			settingsTxt.text = curString;
			yield return new WaitForSeconds(0.02f);
        }
		settingsTxt.text = settingsTextEncoded;
		while (lastNonignoredSolveCount == 0)
			yield return null;
		while (settingsTxt.text.Any())
        {
			var lastText = settingsTxt.text;
			settingsTxt.text = lastText.Substring(0, lastText.Length - 1);
			yield return new WaitForSeconds(0.02f);
		}
		while (!moduleSolved)
			yield return null;
		settingsTxt.color = Color.green;
		curString = "";
		while (curString != settingsTextEncoded)
		{
			var curStringPosIdx = 0;
			while (cyclingTxt[curStringPosIdx] != settingsTextEncoded[curString.Length])
			{
				curStringPosIdx++;
				settingsTxt.text = curString + cyclingTxt[curStringPosIdx];
				yield return new WaitForSeconds(0.02f);
			}
			curString += cyclingTxt[curStringPosIdx];
			settingsTxt.text = curString;
			yield return new WaitForSeconds(0.02f);
		}
		settingsTxt.text = settingsTextEncoded;
		yield break;
    }

	IEnumerator HandleDisplayStage(ClearCodeStage specifiedStage, bool inputting = false, float speed = 2f)
    {
		StartCoroutine(ShakeButtonsGently());
		var lastColorsAllAncilleryMats = usedAncilleryBtnRenders.Select(a => a.material.color).ToArray();
		var lastColorsAllOutlineMats = usedButtonOutlineRenders.Select(a => a.material.color).ToArray();
		var lastColorsTexts = usedDigitsMesh.Select(a => a.color).ToArray();
		var randomDigitsLayout = new string[5];
		var randomDigitsInput = new string[5];
		var possibleRandomDigits = base16Digits.Take(digitsToDisplay);
		var padded0s = Enumerable.Repeat('0', digitsToInput).Join("");
		long maxValueRNG = long.Parse("1" + padded0s);
		for (var x = 0; x < 5; x++)
		{
			var newString = "";
			for (var y = 0; y < digitsToDisplay; y++)
				newString += possibleRandomDigits.PickRandom();
			randomDigitsLayout[x] = newString;
			randomDigitsInput[x] = Random.Range(0, maxValueRNG).ToString(padded0s);
		}
		if (!inputting)
		{
			for (float t = 0; t < 1f; t += Time.deltaTime * speed)
			{
				for (var x = 0; x < usedButtonOutlineRenders.Length; x++)
				{
					usedButtonOutlineRenders[x].enabled = true;
					usedButtonOutlineRenders[x].material.color = Color.Lerp(lastColorsAllOutlineMats[x], specifiedStage.idxDigitsLit.Contains(x) ? new Color(1f, 1f, 1f, 0f) : Color.white, t);
				}
				for (var x = 0; x < usedAncilleryBtnRenders.Length; x++)
				{
					usedAncilleryBtnRenders[x].enabled = true;
					usedAncilleryBtnRenders[x].material.color = Color.Lerp(lastColorsAllAncilleryMats[x], specifiedStage.idxDigitsLit.Contains(x) ? Color.white : transWhite, t);
				}
				for (var x = 0; x < usedDigitsMesh.Length; x++)
				{
					usedDigitsMesh[x].color = Color.Lerp(lastColorsTexts[x], specifiedStage.idxDigitsLit.Contains(x) ? Color.black : Color.white, t);
					usedDigitsMesh[x].text = randomDigitsLayout[(int)Mathf.Lerp(0, 5, t)][x].ToString();
				}
				inputText.text = randomDigitsInput[(int)Mathf.Lerp(0, 5, t)];
				inputText.color = requireLastStage ? Color.Lerp(Color.white, cyclingColors[curStageIdx % colorCycleLimit], t) : Color.white;
				yield return null;
			}
			for (var x = 0; x < usedButtonOutlineRenders.Length; x++)
			{
				usedButtonOutlineRenders[x].material.color = specifiedStage.idxDigitsLit.Contains(x) ? transWhite : Color.white;
				usedButtonOutlineRenders[x].enabled = !specifiedStage.idxDigitsLit.Contains(x);
			}
			for (var x = 0; x < usedAncilleryBtnRenders.Length; x++)
			{
				usedAncilleryBtnRenders[x].enabled = specifiedStage.idxDigitsLit.Contains(x);
				usedAncilleryBtnRenders[x].material.color = specifiedStage.idxDigitsLit.Contains(x) ? Color.white : transWhite;
			}
			for (var x = 0; x < usedDigitsMesh.Length; x++)
			{
				usedDigitsMesh[x].color = specifiedStage.idxDigitsLit.Contains(x) ? Color.black : Color.white;
				usedDigitsMesh[x].text = base16Digits[specifiedStage.preInputDigitsLayout[x]].ToString();
			}
			inputText.text = ((curStageIdx + 1) % maxValueRNG).ToString(padded0s);
			inputText.color = requireLastStage ? cyclingColors[curStageIdx % colorCycleLimit] : Color.white;
		}
		else
        {
			for (float t = 0; t < 1f; t += Time.deltaTime * speed)
			{
				for (var x = 0; x < usedButtonOutlineRenders.Length; x++)
				{
					usedButtonOutlineRenders[x].enabled = true;
					usedButtonOutlineRenders[x].material.color = Color.Lerp(lastColorsAllOutlineMats[x], Color.white, t);
				}
				for (var x = 0; x < usedAncilleryBtnRenders.Length; x++)
				{
					usedAncilleryBtnRenders[x].enabled = true;
					usedAncilleryBtnRenders[x].material.color = Color.Lerp(lastColorsAllAncilleryMats[x], transWhite, t);
				}
				for (var x = 0; x < usedDigitsMesh.Length; x++)
				{
					usedDigitsMesh[x].color = Color.Lerp(lastColorsTexts[x], Color.white, t);
					usedDigitsMesh[x].text = randomDigitsLayout[(int)Mathf.Lerp(0, 5, t)][x].ToString();
				}
				inputText.text = randomDigitsInput[(int)Mathf.Lerp(0, 5, t)];
				inputText.color = requireLastStage ? Color.Lerp(cyclingColors[curStageIdx % colorCycleLimit], Color.white, t) : Color.white;
				yield return null;
			}
			for (var x = 0; x < usedButtonOutlineRenders.Length; x++)
				usedButtonOutlineRenders[x].material.color = Color.white;
			for (var x = 0; x < usedAncilleryBtnRenders.Length; x++)
			{
				usedAncilleryBtnRenders[x].enabled = false;
				usedAncilleryBtnRenders[x].material.color = transWhite;
			}
			for (var x = 0; x < usedDigitsMesh.Length; x++)
			{
				usedDigitsMesh[x].color = Color.white;
				usedDigitsMesh[x].text = base16Digits[specifiedStage.inputDigitsLayout[x]].ToString();
			}
			inputText.text = Enumerable.Repeat("-", digitsToInput).Join("");
			inputText.color = Color.white;
		}
		yield break;
    }

	int PMod(int dividend, int divisor)
    {
		return ((dividend % divisor) + divisor) % divisor;
    }


	// Update is called once per frame
	void Update () {
		var curSolveCountNonIgnored = bombInfo.GetSolvedModuleIDs().Count(a => !ignoreListIDs.Contains(a));
		if (activated && lastNonignoredSolveCount < curSolveCountNonIgnored)
        {
			var solvesToConsider = curSolveCountNonIgnored - lastNonignoredSolveCount;
			var requireStrike = false;
			var countStagesRemoved = 0;
			for (var x = 0; x < solvesToConsider; x++)
            {
				if (!inputting)
                {
					QuickLog("Solve detected, revealing layout for inputting stage {0}", curStageIdx + 1);
					inputting = true;
					mAudio.PlaySoundAtTransform("KPDScan", transform);
					if (animHandler != null)
						StopCoroutine(animHandler);
					animHandler = HandleDisplayStage(allStages[curStageIdx], true);
					StartCoroutine(animHandler);
                }
				else
                {
					requireStrike = true;
					countStagesRemoved++;
					reachableStageIdx--;
				}
            }
			lastNonignoredSolveCount = curSolveCountNonIgnored;
			if (requireStrike && !disableStrike)
            {
				QuickLog("Strike! A stage is still waiting for input. The number of required stages to disarm the module has decreased by {0}.", countStagesRemoved);
				mAudio.PlaySoundAtTransform("KPDError 2", transform);
				StartCoroutine(BrieflyFlashInputTextColor(Color.red));
				CauseStrikeMercy();
			}
		}
	}
	IEnumerator AutosolveHandler()
	{
		while (!moduleSolved)
		{
			while (!inputting)
				yield return true;
			var curStage = allStages[curStageIdx];
			if (curInput.Any() && !curStage.expectedInput.StartsWith(curInput))
				curInput = "";
			for (var x = 0; x < digitsToInput; x++)
            {
				var curDigit = curStage.expectedInput[x];
				btnSelectables[curStage.inputDigitsLayout.Join("").IndexOf(curDigit)].OnInteract();
				yield return new WaitForSeconds(0.05f);
            }
			while (!interactable)
				yield return true;
		}
	}
	void TwitchHandleForcedSolve()
	{
		disableStrike = true;
		QuickLog("Requesting autosolve via TP. Disabling strike handling on skipped stages.");
		StartCoroutine(AutosolveHandler());
	}
	private string TwitchHelpMessage = "Input the four digit code with \"!{0} ####\" or \"!{0} submit ####\", where # is the digit of your code.";
	IEnumerator ProcessTwitchCommand(string cmd)
    {
		var regexInputCode = Regex.Match(cmd, string.Format(@"^((submit|press)\s)?[{0}]+$", base16Digits.Take(digitsToDisplay).Join("")), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		if (regexInputCode.Success)
        {
			if (TPRequireDelayStrike)
            {
				yield return "sendtochaterror The module is sending a strike. Please wait until the module has finished.";
				yield break;
            }
			if (moduleSolved || !interactable || !inputting)
			{
				yield return "sendtochaterror The module is not accepting inputs right now. Wait a bit for the module to accept it.";
				yield break;
			}
			var digits = regexInputCode.Value.Split().Last();
			if (digits.Length != digitsToInput)
            {
				yield return "sendtochaterror The module wants the code to not have that many digits. Check your command for typos.";
				yield break;
			}
			var curStageDigitLayout = allStages.Any() ? allStages[curStageIdx].inputDigitsLayout.Join("") : digits;
			yield return null;
			for (var x = 0; x < digits.Length; x++)
            {
				btnSelectables[curStageDigitLayout.IndexOf(digits[x])].OnInteract();
				yield return new WaitForSeconds(0.1f);
            }
			while (!interactable)
			{
				if (TPRequireDelayStrike)
				{
					yield return "strike";
					yield break;
				}
				else if (moduleSolved)
				{
					yield return "solve";
					yield break;
				}
				yield return null;
			}
			if (!inputting)
				yield return string.Format("awardpoints {0}", baseAuthorPPAScore);
			
        }
		yield break;
    }
}
