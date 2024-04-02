using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using Random = UnityEngine.Random;
using System.Text.RegularExpressions;

public class InOrderScript : MonoBehaviour {

	public Color[] possibleColors;
	public Mesh[] possibleShapes;
	public string[] colorblindPossibleColors, possibleDigitDisplays, possiblePhraseDisplays;
	public int[] possibleValueColors, possibleValueShapes, possibleValueDigits, possiblePhraseValues;
	public Vector3[] modifierShapeSizes;
	public int[] idxColorToInvertTxtColor;

	public string[] debugShapeNames, debugColorNames;

	public MeshRenderer shapeRenderer;
	public MeshRenderer[] screens, arrowInterior;
	public MeshFilter shapeFilter;
	public TextMesh stageNumText, digitText, phraseText;
	public KMAudio mAudio;
	public KMBossModuleExtensions bossHandlerExtended;
	public KMBombInfo bombInfo;
	public KMSelectable[] arrowsSelectable;
	public KMColorblindMode colorblindMode;
	public KMBombModule modSelf;

	static int modIDCnt;
	int moduleID;
	int curStageIdx, curInputIdx;
	List<StageInOrder> allStages;
	bool bossModeActive = true, hasStarted, canInput, modSolved, showStageRecovery, stageCoroutineRunning, requestAutosolve, colorblindActive;
	float delayCur, delayMax = 5f;
	IEnumerable<string> ignoreIds;
	float[] timesArrows = new float[4];
	bool[] pauseDecreaseArrows = new bool[4];
	enum Direction
    {
		Up,
		Right,
		Down,
		Left
    }

	static Dictionary<int, Direction> numDirReference = new Dictionary<int, Direction>
	{
		{ 1, Direction.Down },
		{ 2, Direction.Up },
		{ 3, Direction.Right },
		{ 4, Direction.Left },
		{ 5, Direction.Up },
		{ 6, Direction.Right },
		{ 7, Direction.Down },
		{ 8, Direction.Left },
	};
	static string[] debugDirections = { "Up", "Right", "Down", "Left" };
	public class StageInOrder
	{
		public int idxColor, idxShape, idxDigit, idxPhrase, calculatedValue;
	}
	void QuickLog(string value, params object[] args)
	{
		Debug.LogFormat("[In Order #{0}]: {1}", moduleID, string.Format(value, args));
	}
	void QuickLogDebug(string value, params object[] args)
	{
		Debug.LogFormat("<In Order #{0}>: {1}", moduleID, string.Format(value, args));
	}

	// Use this for initialization
	void Start () {
		moduleID = ++modIDCnt;
		
		var obtainedIgnoreIDs = bossHandlerExtended.GetIgnoredModuleIDs(modSelf);
		if (obtainedIgnoreIDs == null || !obtainedIgnoreIDs.Any())
        {
			QuickLogDebug("Using default ignore IDs! This is due to the result of Boss Module Manager being disabled or not present.");
			ignoreIds = DefaultIgnoreList.ignoreListIDs;
		}
		else
			ignoreIds = obtainedIgnoreIDs;
		modSelf.OnActivate += StartModule;
		stageNumText.text = "";
		phraseText.text = "";
		digitText.text = "";
		shapeRenderer.enabled = false;
        for (var x = 0; x < screens.Length; x++)
        {
			screens[x].material.color = Color.black;
        }
		for (var x = 0; x < 4; x++)
		{
			arrowInterior[x].material.color = Color.black;
		}
		for (var x = 0; x < arrowsSelectable.Length; x++)
        {
			var y = x;
			arrowsSelectable[x].OnInteract += delegate {
				arrowsSelectable[y].AddInteractionPunch(0.1f);
				mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, arrowsSelectable[y].transform);
				HandleInput(GetDirection(y));
				return false;
			};
        }

	}
	Direction GetDirection(int idx)
    {
		switch(idx)
        {
			case 0:
			default:
				return Direction.Up;
			case 1:
				return Direction.Right;
			case 2:
				return Direction.Down;
			case 3:
				return Direction.Left;
        }
    }
	void HandleInput(Direction directionInputted)
    {
		timesArrows[(int)directionInputted] = 0.1f;
		if (modSolved || !hasStarted) return;
		if (bossModeActive)
		{
			if (!canInput)
			{
				QuickLog("The module is not ready to input yet! Strike incurred.");
				modSelf.HandleStrike();
				return;
			}
			else if (allStages.Any() && numDirReference[allStages[curInputIdx].calculatedValue] != directionInputted)
            {
				QuickLog("{0} was incorrectly inputted for stage {1}!", directionInputted.ToString(), curInputIdx + 1);
				modSelf.HandleStrike();
				showStageRecovery = true;
				//DisplayStage(curInputIdx);
			}
			else
            {
				showStageRecovery = false;
				
				curInputIdx++;
				if (curInputIdx >= allStages.Count)
				{
					modSolved = true;
					QuickLog("Directions inputted successfully. Module solved.");
					modSelf.HandlePass();
				}
			}
		}
		else
        {
			if (numDirReference[allStages[curInputIdx].calculatedValue] != directionInputted)
			{
				QuickLog("{0} was incorrectly inputted for stage {1}!", directionInputted.ToString(), curInputIdx + 1);
				modSelf.HandleStrike();
				if (curInputIdx + 1 >= allStages.Count)
				{
					QuickLog("A new stage has been added as a result of this.");
					AddStage();
				}
			}
			else
            {
				curInputIdx++;
				if (curInputIdx >= allStages.Count)
                {
					modSolved = true;
					QuickLog("Directions inputted successfully. Module solved.");
					modSelf.HandlePass();
                }
            }
		}
    }

	void AddStage()
    {
		QuickLog("-------------------------- Stage {0} --------------------------", allStages.Count + 1);
		var newStage = new StageInOrder();
		newStage.idxColor = Random.Range(0, possibleColors.Length);
		newStage.idxShape = Random.Range(0, possibleShapes.Length);
		newStage.idxDigit = Random.Range(0, possibleDigitDisplays.Length);
		newStage.idxPhrase = Random.Range(0, possiblePhraseDisplays.Length);
		QuickLog("Shape displayed: {0}", debugShapeNames[newStage.idxShape]);
		QuickLog("Colour of the shape: {0}", debugColorNames[newStage.idxColor]);
		QuickLog("Number inside of shape: {0}", possibleDigitDisplays[newStage.idxDigit]);
		QuickLog("Phrase shown: \"{0}\"", possiblePhraseDisplays[newStage.idxPhrase]);
		var curSum = possibleValueColors[newStage.idxColor] + possibleValueDigits[newStage.idxDigit] + possibleValueShapes[newStage.idxShape];
		QuickLog("Adding up the values associated with the shape, colour and number gives this sum: {0}", curSum);
		while (curSum > numDirReference.Keys.Max())
		{
			curSum -= possiblePhraseValues[newStage.idxPhrase];
		}
		QuickLog("After adjusting this value, the result should be: {0}", curSum);
		QuickLog("...Corresponding to the direction to press for this stage: {0}", numDirReference[curSum]);
		newStage.calculatedValue = curSum;
		allStages.Add(newStage);
		QuickLog("--------------------------------------------------------------");
	}
	void StartModule()
    {
		allStages = new List<StageInOrder>();
		var stagesToGenerate = ignoreIds == null || !ignoreIds.Any() ? 1 : bombInfo.GetSolvableModuleIDs().Count(a => !ignoreIds.Contains(a));
		if (!bossModeActive)
			QuickLog("Boss handling disabled. Solve this stage without striking to disarm the module.");
		else
			QuickLog("Non-ignored modules detected: {0}", stagesToGenerate);
		for (var x = 0; x < stagesToGenerate; x++)
        {
			QuickLog("-------------------------- Stage {0} --------------------------", x + 1);
			var newStage = new StageInOrder();
			newStage.idxColor = Random.Range(0, possibleColors.Length);
			newStage.idxShape = Random.Range(0, possibleShapes.Length);
			newStage.idxDigit = Random.Range(0, possibleDigitDisplays.Length);
			newStage.idxPhrase = Random.Range(0, possiblePhraseDisplays.Length);
			QuickLog("Shape displayed: {0}", debugShapeNames[newStage.idxShape]);
			QuickLog("Colour of the shape: {0}", debugColorNames[newStage.idxColor]);
			QuickLog("Number inside of shape: {0}", possibleDigitDisplays[newStage.idxDigit]);
			QuickLog("Phrase shown: \"{0}\"", possiblePhraseDisplays[newStage.idxPhrase]);
			var curSum = possibleValueColors[newStage.idxColor] + possibleValueDigits[newStage.idxDigit] + possibleValueShapes[newStage.idxShape];
			QuickLog("Adding up the values associated with the shape, colour and number gives this sum: {0}", curSum);
			if (curSum > numDirReference.Keys.Max())
			{
				QuickLog("This sum is greater than 8. The value of the phrase displayed is {0}, and will be repeatedly subtracted until the sum is within 1-8.", possiblePhraseValues[newStage.idxPhrase]);
				while (curSum > numDirReference.Keys.Max())
				{
					curSum -= possiblePhraseValues[newStage.idxPhrase];
				}
				QuickLog("After adjusting this sum, the result should be: {0}", curSum);
			}
			else
				QuickLog("This sum is within 1 - 8 inclusive. The value at this point should remain at {0}", curSum);
			QuickLog("...Corresponding to the direction to press for this stage: {0}", numDirReference[curSum]);
			newStage.calculatedValue = curSum;
			allStages.Add(newStage);
			QuickLog("--------------------------------------------------------------");
		}
		if (bossModeActive)
        {
			QuickLog("-------------------------- Summary --------------------------");
			QuickLog("Expected directions to press for all stages: {0}", allStages.Select(a => numDirReference[a.calculatedValue]).Join(", "));
			QuickLog("--------------------------------------------------------------");
		}
		QuickLog("-------------------------- User Interactions --------------------------");
		hasStarted = true;
		for (var x = 0; x < screens.Length; x++)
		{
			screens[x].material.color = Color.gray;
		}
		StartCoroutine(bossModeActive ? HandleBossModeStages() : HandleNonBossModeStages());
		//DisplayStage(curStageIdx);
    }
	IEnumerator SolveAnim()
    {
		var textToType = "Module\nSolved";
		phraseText.lineSpacing = 0f;
		phraseText.color = Color.black;
		for (var x = 0; x < textToType.Length; x++)
		{
			phraseText.text = textToType.Substring(0, x + 1);
			yield return new WaitForSeconds(0.05f);
			if (textToType[x] == '\n')
			{
				for (float y = 0; y < 1f; y += Time.deltaTime * 5f)
				{
					phraseText.lineSpacing = Easing.InOutCirc(y, 0f, 1f, 1f);
					yield return null;
				}
				phraseText.lineSpacing = 1f;
			}
		}

		yield break;
	}
	IEnumerator HandleNonBossModeStages()
    {
		while (!modSolved)
        {
			yield return AnimateDisplayStageUntilFalse(curInputIdx, () => { return curInputIdx == curStageIdx; });
			curStageIdx++;
		}
		yield return SolveAnim();
    }
	IEnumerator HandleBossModeStages()
	{
		delayCur = delayMax;
		while (curStageIdx < allStages.Count)
		{
			yield return AnimateDisplayStageUntilFalse(curStageIdx, () => { return delayCur > 0f || curStageIdx >= bombInfo.GetSolvedModuleIDs().Count(a => !ignoreIds.Contains(a)); });
			curStageIdx++;
			delayCur = delayMax;
		}
		QuickLog("Enough modules has been solved. Activating input phase.");
		canInput = true;
		while (!modSolved)
		{
			if (showStageRecovery)
			{
				yield return new WaitWhile(() => { return stageCoroutineRunning; });
				var lastInputIdx = curInputIdx;
				yield return AnimateDisplayStageUntilFalse(curInputIdx, () => { return showStageRecovery && lastInputIdx == curInputIdx; });
			}
			yield return null;
		}
		yield return SolveAnim();
	}
	IEnumerator AnimateDisplayStageUntilFalse(int stageIdx, Func<bool> condition)
    {
		stageCoroutineRunning = true;
		StageInOrder currentStage = allStages.ElementAtOrDefault(stageIdx);
		if (currentStage == null)
			yield break;
		shapeRenderer.enabled = true;
		digitText.text = possibleDigitDisplays[currentStage.idxDigit] + (colorblindActive ? colorblindPossibleColors[currentStage.idxColor] : "");
		var phraseSelected = possiblePhraseDisplays[currentStage.idxPhrase];
		//phraseText.text = possiblePhraseDisplays[currentStage.idxPhrase];
		stageNumText.text = (stageIdx + 1).ToString();
		shapeFilter.mesh = possibleShapes[currentStage.idxShape];
		var selectedModifierSize = modifierShapeSizes[currentStage.idxShape];
		if (digitText.transform.parent == shapeRenderer.transform)
			digitText.transform.localScale = new Vector3(1 / selectedModifierSize.x, 1 / selectedModifierSize.y, 1 / selectedModifierSize.z);
		shapeRenderer.material.color = possibleColors[currentStage.idxColor];
		digitText.color = idxColorToInvertTxtColor.Contains(currentStage.idxColor) ? Color.white : Color.black;
		var t = 0f;
		var lastColorblindState = colorblindActive;
		do
		{
			if (condition())
			{
				if (t < 1f)
					t += Time.deltaTime;
				else
					t = 1f;
			}
			else
				t -= Time.deltaTime;
			var curEase = Easing.InOutSine(t, 0, 1f, 1f);
			//shapeRenderer.transform.localRotation *= Quaternion.Euler(0, 0, 60 * Time.deltaTime * curEase);
			shapeRenderer.transform.localScale = modifierShapeSizes[currentStage.idxShape] * curEase;

			stageNumText.color = Color.black * curEase;
			phraseText.color = Color.black * curEase;
			phraseText.text = phraseSelected.Substring(0, Mathf.Max(Mathf.Min(phraseSelected.Length, Mathf.RoundToInt(phraseSelected.Length * t)), 0));
			if (lastColorblindState ^ colorblindActive)
            {
				lastColorblindState = colorblindActive;
				digitText.text = possibleDigitDisplays[currentStage.idxDigit] + (colorblindActive ? colorblindPossibleColors[currentStage.idxColor] : "");
			}

			yield return null;
		}
		while (t > 0f);
		digitText.text = "";
		phraseText.text = "";
		stageNumText.text = "";
		shapeRenderer.enabled = false;
		stageCoroutineRunning = false;
		yield break;
	}

	void DisplayStageImmediately(int stageIdx)
    {
		StageInOrder currentStage = allStages.ElementAtOrDefault(stageIdx);
		if (currentStage == null)
        {
			digitText.text = "";
			phraseText.text = "";
			stageNumText.text = "";
			shapeRenderer.enabled = false;
			return;
		}
		shapeRenderer.enabled = true;
		digitText.text = possibleDigitDisplays[currentStage.idxDigit];
		phraseText.text = possiblePhraseDisplays[currentStage.idxPhrase];
		stageNumText.text = (stageIdx + 1).ToString();
		shapeFilter.mesh = possibleShapes[currentStage.idxShape];
		shapeRenderer.transform.localScale = modifierShapeSizes[currentStage.idxShape];
		shapeRenderer.material.color = possibleColors[currentStage.idxColor];
		digitText.color = idxColorToInvertTxtColor.Contains(currentStage.idxColor) ? Color.white : Color.black;
	}

	// Update is called once per frame
	void Update () {
		if (!hasStarted) return;
		for (var x = 0; x < 4; x++)
		{
			arrowInterior[x].material.color = timesArrows[x] <= 0f ? Color.white : Color.black;
			if (pauseDecreaseArrows[x]) continue;
			timesArrows[x] -= timesArrows[x] < 0 ? 0 : Time.deltaTime;
		}
		if (!bossModeActive) return;
		if (delayCur > 0f)
			delayCur -= Time.deltaTime * (requestAutosolve ? 4f : 1f);
	}

	IEnumerator TwitchHandleForcedSolve()
    {
		requestAutosolve = true;
		QuickLogDebug("Autosolve requested via TP Handler.");
		while (!canInput)
			yield return true;
		var expectedArrowsAll = allStages.Skip(curInputIdx).Select(a => numDirReference[a.calculatedValue]);
		foreach (var remainingArrow in expectedArrowsAll)
        {
			arrowsSelectable[(int)remainingArrow].OnInteract();
			yield return new WaitForSeconds(0.1f);
        }
    }
#pragma warning disable 414
	private readonly string TwitchHelpMessage = "Press the specified arrow button with \"!{0} up/right/down/left\" Words can be substituted as one letter (Ex. right as r). Multiple directions can be issued in one command by spacing them out or as 1 word when abbrevivabted, I.E \"!{0} udlrrrll\". Alternatively, when abbreviated, you may space out the presses in the command. I.E. \"!{0} lluur ddlr urd\" Toggle colorblind mode with \"!{0} colorblind\"";
#pragma warning restore 414
	IEnumerator ProcessTwitchCommand(string command)
	{
		if (!hasStarted)
		{
			yield return "sendtochaterror The module is not accepting any commands at this moment.";
			yield break;
		}
		if (Regex.IsMatch(command, @"^\s*colou?rblind\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			yield return null;
			colorblindActive = !colorblindActive;
			yield break;
		}
		else if (Regex.IsMatch(command, @"^\s*[uldr\s]+\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			var usableCommand = command.Trim().ToLowerInvariant();
			List<int> allPresses = new List<int>();
			foreach (string directionportion in usableCommand.Split())
			{
				foreach (char dir in directionportion)
				{
					switch (dir)
					{
						case 'u':
							allPresses.Add(0);
							break;
						case 'd':
							allPresses.Add(2);
							break;
						case 'l':
							allPresses.Add(3);
							break;
						case 'r':
							allPresses.Add(1);
							break;
						default:
							yield return string.Format("sendtochaterror I do not know what direction \"{0}\" is supposed to be.", dir);
							yield break;
					}
				}
			}
			if (allPresses.Any())
			{
				var hasStruck = false;
				for (int x = 0; x < allPresses.Count && !hasStruck; x++)
				{
					var expectedArrowsAll = allStages.Select(a => numDirReference[a.calculatedValue]);
					yield return null;
					if (GetDirection(allPresses[x]) != expectedArrowsAll.ElementAt(curInputIdx) && allPresses.Count > 1)
					{
						yield return string.Format("strikemessage by incorrectly pressing {0} after {1} press(es) in the TP command!", debugDirections[allPresses[x]], x + 1);
						hasStruck = true;
					}
					arrowsSelectable[allPresses[x]].OnInteract();
					yield return new WaitForSeconds(0.1f);
				}
			}
		}
		else
		{
			string[] cmdSets = command.Trim().Split();
			List<KMSelectable> allPresses = new List<KMSelectable>();
			for (int x = 0; x < cmdSets.Length; x++)
			{
				if (Regex.IsMatch(cmdSets[x], @"^\s*u(p)?\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
				{
					allPresses.Add(arrowsSelectable[0]);
				}
				else if (Regex.IsMatch(cmdSets[x], @"^\s*d(own)?\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
				{
					allPresses.Add(arrowsSelectable[2]);
				}
				else if (Regex.IsMatch(cmdSets[x], @"^\s*l(eft)?\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
				{
					allPresses.Add(arrowsSelectable[3]);
				}
				else if (Regex.IsMatch(cmdSets[x], @"^\s*r(ight)?\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
				{
					allPresses.Add(arrowsSelectable[1]);
				}
				else
				{
					yield return string.Format("sendtochaterror I do not know what direction \"{0}\" is supposed to be.", cmdSets[x]);
					yield break;
				}
			}
			var expectedArrowsAll = allStages.Select(a => numDirReference[a.calculatedValue]);
			for (var x = 0; x < allPresses.Count; x++)
			{
				yield return null;
				var debugIdx = Array.IndexOf(arrowsSelectable, allPresses[x]);
				if (GetDirection(debugIdx) != expectedArrowsAll.ElementAt(curInputIdx) && allPresses.Count > 1)
					yield return string.Format("strikemessage by incorrectly pressing {0} after {1} press(es) in the TP command!", debugDirections[debugIdx], x + 1);
				allPresses[x].OnInteract();
				yield return new WaitForSeconds(0.1f);
			}
			yield break;
		}
	}
}
