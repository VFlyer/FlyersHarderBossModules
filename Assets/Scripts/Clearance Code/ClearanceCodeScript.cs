using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ClearanceCodeScript : MonoBehaviour {

	public KMBombModule modself;
	public KMBossModule bossHandler;
	public KMBombInfo bombInfo;
	public KMAudio mAudio;
	public KMSelectable[] btnSelectables;
	public TextMesh[] digitsMesh;
	public MeshRenderer[] buttonOutlineRenders, ancilleryBtnRenders;
	public TextMesh inputText;
	public CircleAligner aligner;

	static int modIDCnt;
	int moduleID;
	int curStageIdx, lastNonignoredSolveCount, reachableStageIdx;
	List<ClearCodeStage> allStages;
	string curInput = "";

	const int digitsToInput = 4, authorPPAScore = 2;

	string[] ignoreListIDs = DefaultIgnoreList.ignoreListIDs;

	bool activated = false, inputting = false, moduleSolved = false, interactable;
	IEnumerator animHandler;

	void QuickLog(string toLog = "", params object[] args)
    {
		Debug.LogFormat("[{0} #{1}] {2}", modself.ModuleDisplayName, moduleID, string.Format(toLog, args));
    }
	void QuickLogDebug(string toLog = "", params object[] args)
    {
		Debug.LogFormat("<{0} #{1}> {2}", modself.ModuleDisplayName, moduleID, string.Format(toLog, args));
    }
	// Use this for initialization
	void Start () {
		moduleID = ++modIDCnt;
		var obtainedIds = bossHandler.GetIgnoredModuleIDs(modself);
		if (obtainedIds == null || !obtainedIds.Any())
			QuickLogDebug("Using default ignore list! This will cause issues when multiple bosses are present!");
		else
			ignoreListIDs = obtainedIds;
		modself.OnActivate += ActivateModule;
		inputText.text = "";
		foreach (var render in buttonOutlineRenders)
			render.enabled = false;
		foreach (var render in ancilleryBtnRenders)
		{
			render.enabled = false;
			render.material.color = Color.clear;
		}
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
		mAudio.PlaySoundAtTransform("Flick", btnSelectables[idx].transform);
		btnSelectables[idx].AddInteractionPunch(0.1f);
		if (!inputting || moduleSolved) return;
		if (allStages.Any())
        {
			var curStage = allStages[curStageIdx];
			curInput += curStage.inputDigitsLayout[idx].ToString();
			inputText.text = curInput.PadRight(4, '-');
			if (curInput.Length < digitsToInput) return;
			if (curInput == curStage.expectedInput)
            {
				QuickLog("Accepted intended code for stage {0}", curStageIdx + 1);
				curStageIdx++;
				if (curStageIdx >= reachableStageIdx)
					SolveModule();
				else
                {
					mAudio.PlaySoundAtTransform("Blip", transform);
					if (animHandler != null)
						StopCoroutine(animHandler);
					animHandler = HandleDisplayStage(allStages[curStageIdx], false);
					StartCoroutine(animHandler);
                }
			}
			else
            {
				QuickLog("Denied code for stage {0}: {1}", curStageIdx + 1, curInput);
				CauseStrikeMercy();
			}
			curInput = "";
		}

    }
	void SolveModule()
    {
		modself.HandlePass();
		moduleSolved = true;
	}
	void CauseStrikeMercy()
    {
		mAudio.PlaySoundAtTransform("Error 2", transform);
		modself.HandleStrike();
    }

	void ActivateModule()
    {
		reachableStageIdx = bombInfo.GetSolvableModuleIDs().Count(a => !ignoreListIDs.Contains(a));
		allStages = new List<ClearCodeStage>();
		QuickLog("Non-ignored modules detected: {0}", reachableStageIdx);
		var lastDisplayedDigits = Enumerable.Range(0, 10).ToArray().Shuffle();
		QuickLog("Initial digits in clockwise order, from top: {0}", lastDisplayedDigits.Join(","));
		for (var x = 0; x < reachableStageIdx; x++)
        {
			QuickLog("--------------- Stage {0} ---------------", x + 1);
			var newStage = new ClearCodeStage();
            var pickedDigitIdxesCurStage = new List<int>();
			while (pickedDigitIdxesCurStage.Count < digitsToInput)
            {
				var pickedDigitIdx = Random.Range(0, 10);
				if (!pickedDigitIdxesCurStage.Contains(pickedDigitIdx))
					pickedDigitIdxesCurStage.Add(pickedDigitIdx);
            }
			pickedDigitIdxesCurStage.Sort();

			QuickLog("Picked digits in clockwise order: {0}", pickedDigitIdxesCurStage.Select(a => lastDisplayedDigits[a]).Join(","));
			newStage.idxDigitsLit = pickedDigitIdxesCurStage.ToArray();
			newStage.preInputDigitsLayout = lastDisplayedDigits.ToArray();

			var newDisplayedDigits = Enumerable.Range(0, 10).ToList();
			newDisplayedDigits.Shuffle();
			newStage.inputDigitsLayout = newDisplayedDigits.ToArray();
			// Stage calculation procedures
			QuickLog("When inputting the digits will be shuffled to the following in clockwise order, from top: {0}", newDisplayedDigits.Join(","));
			var distancesFromTargetCW = pickedDigitIdxesCurStage.Select(a => PMod(newDisplayedDigits.IndexOf(lastDisplayedDigits[a]) - a, 10)).ToArray();
			QuickLog("Distances clockwise from previous position: {0}", distancesFromTargetCW.Join(","));
			var inputStr = "";
			if (x % 2 == 1)
			{
				for (var y = 0; y < digitsToInput; y++)
				{
					var idxPickedDigit = pickedDigitIdxesCurStage[y];
					inputStr += PMod(lastDisplayedDigits[idxPickedDigit] - (10 - distancesFromTargetCW[y]), 10).ToString();
				}
				newStage.expectedInput = inputStr;
			}
			else
            {
                for (var y = 0; y < digitsToInput; y++)
                {
					var idxPickedDigit = pickedDigitIdxesCurStage[y];
					inputStr += PMod(lastDisplayedDigits[idxPickedDigit] + distancesFromTargetCW[y], 10).ToString();
                }
				newStage.expectedInput = inputStr;
			}
			QuickLog("Expected code for stage {0}: {1}", x + 1, inputStr);
			allStages.Add(newStage);
			lastDisplayedDigits = newDisplayedDigits.ToArray();
		}
		QuickLog("--------------- User Interactions ---------------");
		activated = true;
		if (allStages.Any())
        {
			animHandler = HandleStartupAnim();
			StartCoroutine(animHandler);
        }
    }
	IEnumerator HandleStartupAnim()
    {
		foreach (var render in buttonOutlineRenders)
			render.enabled = true;
		var reversed = Random.value < 0.5f;
		for (float t = 0; t < 1f; t += Time.deltaTime)
        {
			aligner.radius = t * 6;
			aligner.percentOffset = reversed ? (1f - t) : t;
			aligner.UpdatePositions();
			foreach (var render in buttonOutlineRenders)
				render.material.color = new Color(1f, 1f, 1f, t);
			inputText.text = "".PadRight((int)(4 * t), '0');
			yield return null;
		}
		aligner.radius = 6f;
		aligner.percentOffset = 0;
		aligner.UpdatePositions();
		foreach (var render in buttonOutlineRenders)
			render.material.color = Color.white;
		interactable = true;
		yield return HandleDisplayStage(allStages.First());
	}
	IEnumerator ShakeButtonsGently()
    {
		var reversed = Random.value < 0.5f;
		for (float t = 0; t < 1f; t += Time.deltaTime * 2f)
		{
			aligner.percentOffset = 0.05f * (Easing.InOutSine(t + 0.25f, 0f, 1f, 0.5f) - 0.5f);
			aligner.UpdatePositions();
			yield return null;
		}
	}
	IEnumerator HandleDisplayStage(ClearCodeStage specifiedStage, bool inputting = false, float speed = 2f)
    {
		StartCoroutine(ShakeButtonsGently());
		var lastColorsAllAncilleryMats = ancilleryBtnRenders.Select(a => a.material.color).ToArray();
		var lastColorsAllOutlineMats = buttonOutlineRenders.Select(a => a.material.color).ToArray();
		var lastColorsTexts = digitsMesh.Select(a => a.color).ToArray();
		var randomDigitsLayout = new string[5];
		var randomDigitsInput = new string[5];
		for (var x = 0; x < 5; x++)
		{
			var newString = "";
			for (var y = 0; y < 10; y++)
				newString += Random.Range(0, 10).ToString();
			randomDigitsLayout[x] = newString;
			randomDigitsInput[x] = Random.Range(0, 10000).ToString("0000");
		}
		if (!inputting)
		{
			for (float t = 0; t < 1f; t += Time.deltaTime * speed)
			{
				for (var x = 0; x < buttonOutlineRenders.Length; x++)
				{
					buttonOutlineRenders[x].enabled = true;
					buttonOutlineRenders[x].material.color = Color.Lerp(lastColorsAllOutlineMats[x], specifiedStage.idxDigitsLit.Contains(x) ? new Color(1f, 1f, 1f, 0f) : Color.white, t);
				}
				for (var x = 0; x < ancilleryBtnRenders.Length; x++)
				{
					ancilleryBtnRenders[x].enabled = true;
					ancilleryBtnRenders[x].material.color = Color.Lerp(lastColorsAllAncilleryMats[x], specifiedStage.idxDigitsLit.Contains(x) ? Color.white : new Color(1f, 1f, 1f, 0f), t);
				}
				for (var x = 0; x < digitsMesh.Length; x++)
				{
					digitsMesh[x].color = Color.Lerp(lastColorsTexts[x], specifiedStage.idxDigitsLit.Contains(x) ? Color.black : Color.white, t);
					digitsMesh[x].text = randomDigitsLayout[(int)Mathf.Lerp(0, 5, t)][x].ToString();
				}
				inputText.text = randomDigitsInput[(int)Mathf.Lerp(0, 5, t)];
				yield return null;
			}
			for (var x = 0; x < buttonOutlineRenders.Length; x++)
			{
				buttonOutlineRenders[x].material.color = specifiedStage.idxDigitsLit.Contains(x) ? new Color(1f, 1f, 1f, 0f) : Color.white;
				buttonOutlineRenders[x].enabled = !specifiedStage.idxDigitsLit.Contains(x);
			}
			for (var x = 0; x < ancilleryBtnRenders.Length; x++)
			{
				ancilleryBtnRenders[x].enabled = specifiedStage.idxDigitsLit.Contains(x);
				ancilleryBtnRenders[x].material.color = specifiedStage.idxDigitsLit.Contains(x) ? Color.white : new Color(1f, 1f, 1f, 0f);
			}
			for (var x = 0; x < digitsMesh.Length; x++)
			{
				digitsMesh[x].color = specifiedStage.idxDigitsLit.Contains(x) ? Color.black : Color.white;
				digitsMesh[x].text = specifiedStage.preInputDigitsLayout[x].ToString();
			}
			inputText.text = ((curStageIdx + 1) % 10000).ToString("0000");
		}
		else
        {
			for (float t = 0; t < 1f; t += Time.deltaTime * speed)
			{
				for (var x = 0; x < buttonOutlineRenders.Length; x++)
				{
					buttonOutlineRenders[x].enabled = true;
					buttonOutlineRenders[x].material.color = Color.Lerp(lastColorsAllOutlineMats[x], Color.white, t);
				}
				for (var x = 0; x < ancilleryBtnRenders.Length; x++)
				{
					ancilleryBtnRenders[x].enabled = true;
					ancilleryBtnRenders[x].material.color = Color.Lerp(lastColorsAllAncilleryMats[x], new Color(1f, 1f, 1f, 0f), t);
				}
				for (var x = 0; x < digitsMesh.Length; x++)
				{
					digitsMesh[x].color = Color.Lerp(lastColorsTexts[x], Color.white, t);
					digitsMesh[x].text = randomDigitsLayout[(int)Mathf.Lerp(0, 5, t)][x].ToString();
				}
				inputText.text = randomDigitsInput[(int)Mathf.Lerp(0, 5, t)];
				yield return null;
			}
			for (var x = 0; x < buttonOutlineRenders.Length; x++)
				buttonOutlineRenders[x].material.color = Color.white;
			for (var x = 0; x < ancilleryBtnRenders.Length; x++)
			{
				ancilleryBtnRenders[x].enabled = false;
				ancilleryBtnRenders[x].material.color = new Color(1f, 1f, 1f, 0f);
			}
			for (var x = 0; x < digitsMesh.Length; x++)
			{
				digitsMesh[x].color = Color.white;
				digitsMesh[x].text = specifiedStage.inputDigitsLayout[x].ToString();
			}
			inputText.text = "----";
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
					QuickLog("Solve detected, revealing layout for inputting for stage {0}", curStageIdx + 1);
					inputting = true;
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
			if (requireStrike)
            {
				QuickLog("Strike! A stage is still waiting for input. The number of required stages to disarm the module has decreased by {0}.", countStagesRemoved);
				CauseStrikeMercy();
			}
		}
	}
}
