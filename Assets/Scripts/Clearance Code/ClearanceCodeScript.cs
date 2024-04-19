using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text.RegularExpressions;

public class ClearanceCodeScript : MonoBehaviour {

	public KMBombModule modself;
	public KMBossModule bossHandler;
	public KMBombInfo bombInfo;
	public KMAudio mAudio;
	public KMSelectable[] btnSelectables;
	public TextMesh[] digitsMesh;
	public MeshRenderer[] buttonOutlineRenders, ancilleryBtnRenders, miscRenderers;
	public TextMesh inputText;
	public CircleAligner aligner;
	public MeshRenderer lockRenderer;
	public Texture unlockIcon;

	static int modIDCnt;
	int moduleID;
	int curStageIdx, lastNonignoredSolveCount, reachableStageIdx;
	List<ClearCodeStage> allStages;
	string curInput = "";

	const int digitsToInput = 4, authorPPAScore = 2;

	string[] ignoreListIDs = DefaultIgnoreList.ignoreListIDs;

	bool activated = false, inputting = false, moduleSolved = false, interactable, disableStrike, TPRequireDelayStrike;
	IEnumerator animHandler;
	static readonly Color transWhite = new Color(1, 1, 1, 0);

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
		mAudio.PlaySoundAtTransform("KPDFlick", btnSelectables[idx].transform);
		btnSelectables[idx].AddInteractionPunch(0.1f);
		if (!inputting || moduleSolved) return;
		if (allStages.Any())
        {
			var curStage = allStages[curStageIdx];
			curInput += curStage.inputDigitsLayout[idx].ToString();
			inputText.text = curInput.PadRight(4, '-');
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
			curInput += idx.ToString();
			inputText.text = curInput.PadRight(4, '-');
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
		animHandler = HandleMercyStage(allStages[curStageIdx], curStageIdx % 2 == 1);
		StartCoroutine(animHandler);
	}

	void ActivateModule()
    {
		lockRenderer.enabled = false;
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
		animHandler = HandleStartupAnim();
		StartCoroutine(animHandler);
    }
	IEnumerator HandleDelayCheck(ClearCodeStage curStage = null)
    {
		yield return new WaitForSeconds(0.4f);
		if (curStage == null)
		{
			var solveTextPicked = new[] { "DONE", "8888", curInput }.PickRandom();
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
				var solveTextPicked = new[] { "DONE", "8888", curInput }.PickRandom();
				QuickLog("All accessible stages have been completed. Open sesame!");
				inputText.text = solveTextPicked;
				mAudio.PlaySoundAtTransform("KPDSolve", transform);
				yield return BrieflyFlashInputTextColor(Color.green, 20);
				inputText.text = "";
				SolveModule();
			}
			else
			{
				mAudio.PlaySoundAtTransform("KPDAccepted", transform);
				var correctAdvTextPicked = new[] { "YEAH", "5URE" }.PickRandom();
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
			var wrongTextPicked = new[] { "NOPE", "NONO", "FA1L" }.PickRandom();
			inputText.text = wrongTextPicked;
			mAudio.PlaySoundAtTransform("KPDError 2", transform);
			yield return BrieflyFlashInputTextColor(Color.red);
			CauseStrikeMercy();
			inputText.text = "----";
			TPRequireDelayStrike = false;
		}
		curInput = "";
		interactable = true;
		activated = true;
	}
	IEnumerator HandleMercyStage(ClearCodeStage curStage, bool altMercyColor = false)
	{
		var stepCur = 0;
		while (true)
        {
			var lastColorsOutlines = buttonOutlineRenders.Select(a => a.material.color).ToArray();
			var lastColorsCenters = ancilleryBtnRenders.Select(a => a.material.color).ToArray();
			var lastColorsTexts = digitsMesh.Select(a => a.color).ToArray();
			if (stepCur >= digitsToInput)
            {
				for (float t = 0; t < 1f; t += Time.deltaTime)
				{
					for (var x = 0; x < buttonOutlineRenders.Length; x++)
						buttonOutlineRenders[x].material.color = Color.Lerp(lastColorsOutlines[x], Color.white, t);
					for (var x = 0; x < ancilleryBtnRenders.Length; x++)
						ancilleryBtnRenders[x].material.color = Color.Lerp(lastColorsCenters[x], transWhite, t);
					for (var x = 0; x < digitsMesh.Length; x++)
						digitsMesh[x].color = Color.Lerp(lastColorsTexts[x], Color.white, t);
					yield return null;
				}
				foreach (MeshRenderer x in buttonOutlineRenders)
					x.material.color = Color.white;
				foreach (MeshRenderer x in ancilleryBtnRenders)
					x.material.color = transWhite;
				foreach (var x in digitsMesh)
					x.color = Color.white;
			}
			else
            {
				var litInitCurStage = curStage.idxDigitsLit[stepCur];
				var postInitCurStage = curStage.inputDigitsLayout.ToList().IndexOf(curStage.preInputDigitsLayout[litInitCurStage]);
				for (float t = 0; t < 1f; t += Time.deltaTime)
				{
					for (var x = 0; x < buttonOutlineRenders.Length; x++)
					{
						buttonOutlineRenders[x].material.color = Color.Lerp(lastColorsOutlines[x], postInitCurStage == x ? transWhite : litInitCurStage == x ? (altMercyColor ? Color.magenta : Color.cyan) : Color.white, t);
					}
					for (var x = 0; x < ancilleryBtnRenders.Length; x++)
					{
						ancilleryBtnRenders[x].enabled = true;
						ancilleryBtnRenders[x].material.color = Color.Lerp(lastColorsCenters[x], postInitCurStage == x ? (altMercyColor ? Color.magenta : Color.cyan) : transWhite, t);
					}
					for (var x = 0; x < digitsMesh.Length; x++)
						digitsMesh[x].color = Color.Lerp(lastColorsTexts[x], postInitCurStage == x ? Color.black : litInitCurStage == x ? (altMercyColor ? Color.magenta : Color.cyan) : Color.white, t);
					yield return null;
				}
				foreach (MeshRenderer x in buttonOutlineRenders)
                    x.material.color = Color.white;
				for (int x = 0; x < buttonOutlineRenders.Length; x++)
				{
					buttonOutlineRenders[x].material.color = postInitCurStage == x ? transWhite : litInitCurStage == x ? (altMercyColor ? Color.magenta : Color.cyan) : Color.white;
				}
				for (var x = 0; x < ancilleryBtnRenders.Length; x++)
				{
					ancilleryBtnRenders[x].enabled = postInitCurStage == x;
					ancilleryBtnRenders[x].material.color = postInitCurStage == x ? (altMercyColor ? Color.magenta : Color.cyan) : transWhite;
				}
				for (var x = 0; x < digitsMesh.Length; x++)
					digitsMesh[x].color = postInitCurStage == x ? Color.black : litInitCurStage == x ? (altMercyColor ? Color.magenta : Color.cyan) : Color.white;
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
		var reversed = Random.value < 0.5f;
		var lastColorsAllAncilleryMats = ancilleryBtnRenders.Select(a => a.material.color).ToArray();
		var lastColorsAllOutlineMats = buttonOutlineRenders.Select(a => a.material.color).ToArray();
		var lastColorsTexts = digitsMesh.Select(a => a.color).ToArray();
		lockRenderer.enabled = true;
		for (int n = 0; n < buttonOutlineRenders.Length; n++)
		{
			mAudio.PlaySoundAtTransform("BinMemSolve", transform);
			var outRender = buttonOutlineRenders[hasLastStage ? allStages.Last().inputDigitsLayout.ToList().IndexOf(n) : n];
			var ancilRender = ancilleryBtnRenders[hasLastStage ? allStages.Last().inputDigitsLayout.ToList().IndexOf(n) : n];
			var txtMesh = digitsMesh[hasLastStage ? allStages.Last().inputDigitsLayout.ToList().IndexOf(n) : n];
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
		if (allStages.Any())
			yield return HandleDisplayStage(allStages.First());
		else
        {
			inputting = true;
			yield return HandleDisplayInputFake();
		}
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
		for (var x = 0; x < 5; x++)
		{
			var newString = "";
			for (var y = 0; y < 10; y++)
				newString += Random.Range(0, 10).ToString();
			randomDigitsLayout[x] = newString;
			randomDigitsInput[x] = Random.Range(0, 10000).ToString("0000");
		}
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
				ancilleryBtnRenders[x].material.color = Color.Lerp(lastColorsAllAncilleryMats[x], transWhite, t);
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
			ancilleryBtnRenders[x].material.color = transWhite;
		}
		for (var x = 0; x < digitsMesh.Length; x++)
		{
			digitsMesh[x].color = Color.white;
			digitsMesh[x].text = x.ToString();
		}
		inputText.text = "----";
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
					ancilleryBtnRenders[x].material.color = Color.Lerp(lastColorsAllAncilleryMats[x], specifiedStage.idxDigitsLit.Contains(x) ? Color.white : transWhite, t);
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
				buttonOutlineRenders[x].material.color = specifiedStage.idxDigitsLit.Contains(x) ? transWhite : Color.white;
				buttonOutlineRenders[x].enabled = !specifiedStage.idxDigitsLit.Contains(x);
			}
			for (var x = 0; x < ancilleryBtnRenders.Length; x++)
			{
				ancilleryBtnRenders[x].enabled = specifiedStage.idxDigitsLit.Contains(x);
				ancilleryBtnRenders[x].material.color = specifiedStage.idxDigitsLit.Contains(x) ? Color.white : transWhite;
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
					ancilleryBtnRenders[x].material.color = Color.Lerp(lastColorsAllAncilleryMats[x], transWhite, t);
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
				ancilleryBtnRenders[x].material.color = transWhite;
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
				mAudio.PlaySoundAtTransform("Error 2", transform);
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
	readonly static string TwitchHelpMessage = "Input the four digit code with \"!{0} ####\" or \"!{0} submit ####\", where # is the digit of your code.";
	IEnumerator ProcessTwitchCommand(string cmd)
    {
		var regexInputCode = Regex.Match(cmd, @"^((submit|press)\s)?[0-9]+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
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
			var curStageDigitLayout = allStages.Any() ? allStages[curStageIdx].inputDigitsLayout.Join("") : "0123456789";
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
				yield return null;
			}
			if (!inputting)
				yield return string.Format("awardpoints {0}", authorPPAScore);
			else if (moduleSolved)
				yield return "solve";
        }
		yield break;
    }
}
