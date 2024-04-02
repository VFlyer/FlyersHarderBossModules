using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KeepCoding;
using System.Text.RegularExpressions;
using Random = UnityEngine.Random;

public class PointerPointerScript : MonoBehaviour {
	public KMBossModule bossHandler;
	public KMAudio mAudio;
	public KMSelectable mainSelectable;
	public KMSelectable[] screenSelectables, screenSelectables_4x4;
	public GameObject _6x6Board, _4x4Board;
	public KMBombModule modSelf;
	public KMBombInfo bombInfo;
	public KMColorblindMode colorblindMode;
	public MeshRenderer[] screenRenderers, arrowRenderers, ledRenderers, screenRenderers_4x4, arrowRenderers_4x4;
	public TextMesh[] colorblindTextMeshes, colorblindTextMeshes_4x4;
	static int moduleIDCnt;
	int moduleID;

	IEnumerable<string> ignoredModules;
	int curStageIdx, reachableStageIdx, lastSolveCount, PPAToGive;

	MeshRenderer[] usedScreenRenderers, usedArrowRenderers;
	TextMesh[] usedCBTextMeshes;

	readonly static Color[] refColors = { Color.black, Color.red, Color.green, Color.blue, Color.white, Color.yellow, Color.magenta, Color.cyan };
	readonly static string[] directionRefAbbrev = { "U", "UR", "R", "DR", "D", "DL", "L", "UL" },
		colorRefAbbrev = { "K", "R", "G", "B", "W", "Y", "M", "C", };

	const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
	const int authorPPAScore = 2;
	int squareLength = 6, maxTilesVisit = 15, minTilesVisit = 9;

	List<StagePointerPointer> allPointerStages;

	bool started, inputting, moduleSolved, disableStrike, colorblindDetected, revealed = false, altGen;
	IEnumerator screenFlashingAnim, specificTileFlasher, ledFlashAnim;
	FlyersBossierSettings globalSettings = new FlyersBossierSettings();

	void QuickLog(string message, params object[] args)
    {
		Debug.LogFormat("[{0} #{1}] {2}", modSelf.ModuleDisplayName, moduleID, string.Format(message, args));
    }
	void QuickLogDebug(string message, params object[] args)
    {
		Debug.LogFormat("<{0} #{1}> {2}", modSelf.ModuleDisplayName, moduleID, string.Format(message, args));
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
		var regexPPOverride = Regex.Match(description, @"\[PPOverride\]\s(true|false)");
		if (regexPPOverride.Success)
		{
			try
			{
				successful = true;
				var valuedMatched = regexPPOverride.Value;
				var lastPartOnly = valuedMatched.Split().Last();
				squareLength = bool.Parse(lastPartOnly) ? 6 : 4;
			}
			catch
			{
				successful = false;
				QuickLogDebug("EXCEPTION THROWN, OVERRIDE COUNTED AS FAILURE.");
			}
		}
		return successful;
	}

	void Awake()
    {
		try
		{
			var obtainedSettings = new ModConfig<FlyersBossierSettings>("FlyersBossierSettings");
			globalSettings = obtainedSettings.Settings;
			obtainedSettings.Settings = globalSettings;
			altGen = globalSettings.PPUseAlternativeGen;
			PPAToGive = globalSettings.UseAuthorDynamicScoring ? authorPPAScore : globalSettings.PPPointsPerActivation;
			squareLength = globalSettings.PPUse6x6Board ? 6 : 4;
		}
		catch
		{
			altGen = false;
			PPAToGive = authorPPAScore;
			squareLength = 4;
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
	// Use this for initialization
	void Start () {
		moduleID = ++moduleIDCnt;
		TryOverrideMission();
		if (squareLength == 4)
        {
			usedScreenRenderers = screenRenderers_4x4;
			usedArrowRenderers = arrowRenderers_4x4;
			usedCBTextMeshes = colorblindTextMeshes_4x4;
			mainSelectable.Children = screenSelectables_4x4;
			minTilesVisit = 4;
			maxTilesVisit = 6;
			_6x6Board.SetActive(false);
		}
		else
        {
			usedScreenRenderers = screenRenderers;
			usedArrowRenderers = arrowRenderers;
			usedCBTextMeshes = colorblindTextMeshes;
			mainSelectable.Children = screenSelectables;
			_4x4Board.SetActive(false);
		}
		mainSelectable.ChildRowLength = squareLength;
		mainSelectable.UpdateChildrenProperly();
		//var detectedIgnoredModules = bossHandler.GetIgnoredModules(modSelf);
		ignoredModules = bossHandler.GetIgnoredModules(modSelf, DefaultIgnoreList.ignoreListNames);
		modSelf.OnActivate += delegate {
			if (altGen)
				GenerateStagesAlt();
			else
				GenerateStages();
		};
		reachableStageIdx = bombInfo.GetSolvableModuleNames().Count(a => !ignoredModules.Contains(a)) - 1;
		allPointerStages = new List<StagePointerPointer>();
		for (var x = 0; x < arrowRenderers.Length; x++)
			arrowRenderers[x].enabled = false;
		for (var x = 0; x < colorblindTextMeshes.Length; x++)
			colorblindTextMeshes[x].text = "";
		for (var x = 0; x < arrowRenderers_4x4.Length; x++)
			arrowRenderers_4x4[x].enabled = false;
		for (var x = 0; x < colorblindTextMeshes_4x4.Length; x++)
			colorblindTextMeshes_4x4[x].text = "";

		for (var x = 0; x < screenSelectables.Length; x++)
        {
			int y = x;
			screenSelectables[x].OnInteract += delegate {
				screenSelectables[y].AddInteractionPunch(0.1f);
				mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, screenSelectables[y].transform);
				CheckIdxPressed(y);
				return false; };
			screenSelectables[x].gameObject.SetActive(squareLength == 6);
        }

		for (var x = 0; x < screenSelectables_4x4.Length; x++)
        {
			int y = x;
			screenSelectables_4x4[x].OnInteract += delegate {
				screenSelectables_4x4[y].AddInteractionPunch(0.1f);
				mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, screenSelectables_4x4[y].transform);
				CheckIdxPressed(y);
				return false; };
			screenSelectables_4x4[x].gameObject.SetActive(squareLength == 4);
		}

	}

	void CheckIdxPressed(int idx)
    {
		if (reachableStageIdx < 0 && !moduleSolved && started && !inputting)
        {
			StopAllCoroutines();
			mAudio.PlaySoundAtTransform("UI_Numpad_B_Amp", transform);
			moduleSolved = true;
			QuickLog("All of the reachable stages have been completed. Module disarmed.");
			if (bombInfo.GetTime() < 60f)
				modSelf.HandlePass();
			ledFlashAnim = FlashSequentialLEDsGreen();
			StartCoroutine(FlashScreensOrderly(4f));
			StartCoroutine(ledFlashAnim);
		}

		if (!inputting || moduleSolved || !started) return;
		var curStage = allPointerStages.ElementAt(curStageIdx);
		StopCoroutine(screenFlashingAnim);
		if (curStage.endIdx == idx)
        {
			QuickLog("Correctly pressed for stage {2}: {0}{1}", alphabet[curStage.endIdx % squareLength], curStage.endIdx / squareLength + 1, curStageIdx + 1);
			curStageIdx++;
			if (ledFlashAnim != null)
				StopCoroutine(ledFlashAnim);
			if (curStageIdx > reachableStageIdx)
            {
				StopAllCoroutines();
				mAudio.PlaySoundAtTransform("UI_Numpad_B_Amp", transform);
				moduleSolved = true;
				QuickLog("All of the reachable stages have been completed. Module disarmed.");
				if (bombInfo.GetTime() < 60f)
					modSelf.HandlePass();
				ledFlashAnim = FlashSequentialLEDsGreen();
				StartCoroutine(FlashScreensOrderly(4f));
            }
			else
            {
				mAudio.PlaySoundAtTransform("UI_Numpad_B_Amp", transform);
				revealed = false;
				started = false;
				inputting = false;
				if (ledFlashAnim != null)
					StopCoroutine(ledFlashAnim);
				ledFlashAnim = FlashLEDsGreen();
				QuickLog("Showing stage {0}.", curStageIdx + 1);
				screenFlashingAnim = AnimateDisplayedStage(allPointerStages[curStageIdx], 1);
				StartCoroutine(screenFlashingAnim);
				started = true;
			}
			StartCoroutine(ledFlashAnim);
		}
		else
        {
			QuickLog("Strike! {0}{1} was incorrectly pressed on stage {2}!", alphabet[idx % squareLength], idx / squareLength + 1, curStageIdx + 1);
			CauseStrikeMercy();
		}

    }
	void GenerateStagesAlt()
    {
		QuickLog("Total Stages Generatable: {0}", reachableStageIdx + 1);
		QuickLog("Using an alternative stage generation algorithm to generate stages. This may create short paths that can make the module quick.");
		QuickLog("Selected board size: {0}x{0}", squareLength);
		var gridSize = squareLength * squareLength;
		for (var x = 0; x <= reachableStageIdx; x++)
		{

			var newStage = new StagePointerPointer();
			var newArrowIdxes = new int[gridSize];
			var newColorIdxes = new int[gridSize];

			newStage.truthDirectionIdxes = newArrowIdxes;
			newStage.colorDisplayIdxes = newColorIdxes;

			var selectedPossibleStartIdxes = x > 0 ? Enumerable.Repeat(allPointerStages[x - 1].endIdx, 5) : Enumerable.Range(0, squareLength * squareLength).ToArray().Shuffle().Take(5);
			var visitedCellIdxes = new List<int>();
			for (var p = 0; p < selectedPossibleStartIdxes.Count(); p++)
			{
				// Generate a random set of directions associated with this module.
				for (var y = 0; y < newArrowIdxes.Length; y++)
					newArrowIdxes[y] = Random.Range(0, 8);

				visitedCellIdxes.Clear();
				var curCell = selectedPossibleStartIdxes.ElementAt(p);

				newStage.startIdx = curCell;

				do
				{
					visitedCellIdxes.Add(curCell);
					var curRow = curCell / squareLength;
					var curCol = curCell % squareLength;
					switch (newArrowIdxes[curCell])
					{
						case 0: // Up
							curRow = (curRow + squareLength - 1) % squareLength;
							break;
						case 1: // Up-Right
							curRow = (curRow + squareLength - 1) % squareLength;
							curCol = (curCol + 1) % squareLength;
							break;
						case 2: // Right
							curCol = (curCol + 1) % squareLength;
							break;
						case 3: // Down-Right
							curRow = (curRow + 1) % squareLength;
							curCol = (curCol + 1) % squareLength;
							break;
						case 4: // Down
							curRow = (curRow + 1) % squareLength;
							break;
						case 5: // Down-Left
							curRow = (curRow + 1) % squareLength;
							curCol = (curCol + squareLength - 1) % squareLength;
							break;
						case 6: // Left
							curCol = (curCol + squareLength - 1) % squareLength;
							break;
						case 7: // Up-Left
							curCol = (curCol + squareLength - 1) % squareLength;
							curRow = (curRow + squareLength - 1) % squareLength;
							break;
					}
					curCell = curRow * squareLength + curCol;
				}
				while (!visitedCellIdxes.Contains(curCell));
				newStage.endIdx = curCell;
				if (visitedCellIdxes.Count >= minTilesVisit && visitedCellIdxes.Count <= maxTilesVisit)
					break;
			}
			
			for (var y = 0; y < newColorIdxes.Length; y++)
				newColorIdxes[y] = Random.Range(0, refColors.Length);
			newStage.pathIdxTaken = visitedCellIdxes;
			QuickLog("------------------------- Stage {0} -------------------------", x + 1);
			QuickLog("Colors displayed:");
			for (var y = 0; y < squareLength; y++)
				QuickLog("{0}", newColorIdxes.Skip(y * squareLength).Take(squareLength).Select(a => colorRefAbbrev[a]).Join());

			QuickLog("Arrows displayed:");
			for (var y = 0; y < squareLength; y++)
			{
				var curIdxes = Enumerable.Range(squareLength * y, squareLength);
				QuickLog("{0}", curIdxes.Select(a => directionRefAbbrev[(newArrowIdxes[a] - newColorIdxes[a] + 8) % 8]).Join());
			}

			QuickLog("Truth directions:");
			for (var y = 0; y < squareLength; y++)
				QuickLog("{0}", newArrowIdxes.Skip(y * squareLength).Take(squareLength).Select(a => directionRefAbbrev[a]).Join());


			QuickLog("Starting Coordinate: {0}{1}", alphabet[newStage.startIdx % squareLength], newStage.startIdx / squareLength + 1);
			QuickLog("Ending Coordinate: {0}{1}", alphabet[newStage.endIdx % squareLength], newStage.endIdx / squareLength + 1);
			QuickLog("Path Taken: {0} -> {1}", visitedCellIdxes.Select(a => string.Format("{0}{1}", alphabet[a % squareLength], a / squareLength + 1)).Join(" -> "), string.Format("{0}{1}", alphabet[newStage.endIdx % squareLength], newStage.endIdx / squareLength + 1));

			allPointerStages.Add(newStage);
		}
		StartModule();
	}
	void GenerateStages()
    {
		QuickLog("Total Stages Generatable: {0}", reachableStageIdx + 1);
		QuickLog("Selected board size: {0}x{0}", squareLength);
		var gridSize = squareLength * squareLength;
		// Generate a node grid of all of the connected cells.
		var idxConnectedCells = new List<List<int>>();
		for (var p = 0; p < gridSize; p++)
		{
			var nextIdxesCur = new List<int>();
			for (var a = 0; a < 8; a++)
			{
				var curRow = p / squareLength;
				var curCol = p % squareLength;
				switch (a)
				{
					case 0: // Up
						curRow = (curRow + squareLength - 1) % squareLength;
						break;
					case 1: // Up-Right
						curRow = (curRow + squareLength - 1) % squareLength;
						curCol = (curCol + 1) % squareLength;
						break;
					case 2: // Right
						curCol = (curCol + 1) % squareLength;
						break;
					case 3: // Down-Right
						curRow = (curRow + 1) % squareLength;
						curCol = (curCol + 1) % squareLength;
						break;
					case 4: // Down
						curRow = (curRow + 1) % squareLength;
						break;
					case 5: // Down-Left
						curRow = (curRow + 1) % squareLength;
						curCol = (curCol + squareLength - 1) % squareLength;
						break;
					case 6: // Left
						curCol = (curCol + squareLength - 1) % squareLength;
						break;
					case 7: // Up-Left
						curCol = (curCol + squareLength - 1) % squareLength;
						curRow = (curRow + squareLength - 1) % squareLength;
						break;
				}
				var destCur = curRow * squareLength + curCol;
				nextIdxesCur.Add(destCur);
			}
			idxConnectedCells.Add(nextIdxesCur);
		}
		for (var x = 0; x <= reachableStageIdx; x++)
        {

			var newStage = new StagePointerPointer();
			var newArrowIdxes = new int[gridSize];
			var newColorIdxes = new int[gridSize];

			newStage.truthDirectionIdxes = newArrowIdxes;
			newStage.colorDisplayIdxes = newColorIdxes;

			var selectedPossibleStartIdxes = x > 0 ? allPointerStages[x - 1].endIdx : Enumerable.Range(0, squareLength * squareLength).PickRandom();
			var visitedCellIdxes = new List<int>();
			{
				// Select a random number of cells to visit and the starting location.
				var curCell = selectedPossibleStartIdxes;
				var cntCellsToVisit = Random.Range(minTilesVisit - 1, maxTilesVisit);
				var blacklistCellsToVisit = new List<int>();
				newStage.startIdx = curCell;
				
				visitedCellIdxes.Add(curCell);
				while (visitedCellIdxes.Count() < cntCellsToVisit)
				{
					var lastCell = visitedCellIdxes.Last();
					var nextCellsToVisit = idxConnectedCells[lastCell].Where(a => !(visitedCellIdxes.Contains(a) || blacklistCellsToVisit.Contains(a)));
					if (nextCellsToVisit.Any())
                    {
						var nextCell = nextCellsToVisit.PickRandom();
						visitedCellIdxes.Add(nextCell);
                    }
					else
                    {
						blacklistCellsToVisit.Add(lastCell);
						visitedCellIdxes.Remove(lastCell);
                    }
				}
				// Upon reaching ending the loop above, grab the last cell visited.
				var lastCellVisited = visitedCellIdxes.Last();
				// Obtain a series of cells from the last cell visited to overlap another cell, and pick one at random.
				var finalCellsToVisit = idxConnectedCells[lastCellVisited].Where(a => visitedCellIdxes.Contains(a));
				var selectedEndIdx = finalCellsToVisit.PickRandom();
				visitedCellIdxes.Add(selectedEndIdx);
				newStage.endIdx = selectedEndIdx;
				for (var y = 0; y < newArrowIdxes.Length; y++)
				{
					var idxFirstCell = visitedCellIdxes.IndexOf(y);
					newArrowIdxes[y] = idxFirstCell >= 0 ? idxConnectedCells[y].IndexOf(visitedCellIdxes[idxFirstCell + 1]) : Random.Range(0, 8);
				}
				visitedCellIdxes.RemoveAt(visitedCellIdxes.Count - 1);
			}

			for (var y = 0; y < newColorIdxes.Length; y++)
				newColorIdxes[y] = Random.Range(0, refColors.Length);
			newStage.pathIdxTaken = visitedCellIdxes;
			QuickLog("------------------------- Stage {0} -------------------------", x + 1);
			QuickLog("Colors displayed:");
			for (var y = 0; y < squareLength; y++)
				QuickLog("{0}", newColorIdxes.Skip(y * squareLength).Take(squareLength).Select(a => colorRefAbbrev[a]).Join());
			
			QuickLog("Arrows displayed:");
			for (var y = 0; y < squareLength; y++)
            {
				var curIdxes = Enumerable.Range(squareLength * y, squareLength);
				QuickLog("{0}", curIdxes.Select(a => directionRefAbbrev[(newArrowIdxes[a] - newColorIdxes[a] + 8) % 8]).Join());
			}

			QuickLog("Truth directions:");
			for (var y = 0; y < squareLength; y++)
				QuickLog("{0}", newArrowIdxes.Skip(y * squareLength).Take(squareLength).Select(a => directionRefAbbrev[a]).Join());
			

			QuickLog("Starting Coordinate: {0}{1}", alphabet[newStage.startIdx % squareLength], newStage.startIdx / squareLength + 1);
			QuickLog("Ending Coordinate: {0}{1}", alphabet[newStage.endIdx % squareLength], newStage.endIdx / squareLength + 1);
			QuickLog("Path Taken: {0} -> {1}", visitedCellIdxes.Select(a => string.Format("{0}{1}", alphabet[a % squareLength], a / squareLength + 1)).Join(" -> "), string.Format("{0}{1}", alphabet[newStage.endIdx % squareLength], newStage.endIdx / squareLength + 1));

			allPointerStages.Add(newStage);
		}
		StartModule();
	}
	void StartModule()
    {
		QuickLog("------------------------- Actions Performed -------------------------", reachableStageIdx + 1);
		started = true;
		if (allPointerStages.Any())
		{
			screenFlashingAnim = AnimateDisplayedStage(allPointerStages.First(), 1);
			StartCoroutine(screenFlashingAnim);
		}
		else
			QuickLog("Oh, no stages, huh? I guess just press any screen to solve this module then... Don't ask why this message is here.");
	}

	void CauseStrikeMercy()
    {
		modSelf.HandleStrike();
		mAudio.PlaySoundAtTransform("UI_Numpad_Deny_Amp", transform);
		if (ledFlashAnim != null)
			StopCoroutine(ledFlashAnim);
		ledFlashAnim = FlashLEDsRedBriefly();
		StartCoroutine(ledFlashAnim);
		if (!revealed)
		{
			StartCoroutine(RevealTilesCurrentStage(Enumerable.Range(0, 36)));
			revealed = true;
		}
	}

	IEnumerator PlaySoundXTimes(string soundname, float delay, int count = 1)
    {
        for (var x = 0; x < count; x++)
        {
			mAudio.PlaySoundAtTransform(soundname, transform);
			yield return new WaitForSeconds(delay);
        }
    }
	IEnumerator FlashLEDsRedBriefly()
    {
        for (var x = 0; x < 5; x++)
        {
			foreach (var led in ledRenderers)
			{
				led.material.color = Color.red;
			}
			yield return new WaitForSeconds(0.125f);
			foreach (var led in ledRenderers)
			{
				led.material.color = Color.black;
			}
			yield return new WaitForSeconds(0.125f);
		}
    }
	IEnumerator FlashLEDsGreen()
    {
        for (var x = 0; x < 5; x++)
        {
			foreach (var led in ledRenderers)
			{
				led.material.color = Color.green;
			}
			yield return new WaitForSeconds(0.125f);
			foreach (var led in ledRenderers)
			{
				led.material.color = Color.black;
			}
			yield return new WaitForSeconds(0.125f);
		}
		var quarterOfLength = ledRenderers.Length / 4;
		while (moduleSolved)
        {
            for (var x = 0; x < quarterOfLength; x++)
            {
                for (var y = 0; y < 4; y++)
                {
					ledRenderers[quarterOfLength * y + x].material.color = Color.green;
				}
				yield return new WaitForSeconds(0.05f);
			}
            for (var x = 0; x < quarterOfLength; x++)
            {
                for (var y = 0; y < 4; y++)
                {
                    ledRenderers[quarterOfLength * y + x].material.color = Color.black;
                }
				yield return new WaitForSeconds(0.05f);
			}
        }
    }
	IEnumerator FlashSequentialLEDsGreen()
    {
		var quarterOfLength = ledRenderers.Length / 4;
		while (moduleSolved)
        {
            for (var x = 0; x < quarterOfLength; x++)
            {
                for (var y = 0; y < 4; y++)
                {
					ledRenderers[quarterOfLength * y + x].material.color = Color.green;
				}
				yield return new WaitForSeconds(0.05f);
			}
            for (var x = 0; x < quarterOfLength; x++)
            {
                for (var y = 0; y < 4; y++)
                {
                    ledRenderers[quarterOfLength * y + x].material.color = Color.black;
                }
				yield return new WaitForSeconds(0.05f);
			}
        }
    }

	IEnumerator RevealTilesCurrentStage(IEnumerable<int> values, float speed = 2f)
    {
		StopCoroutine(specificTileFlasher);
		var curStage = allPointerStages[curStageIdx];
		for (var x = 0; x < values.Count(); x++)
		{
			var y = values.ElementAt(x);
			var colorModifier = 8 - curStage.colorDisplayIdxes[y];
			usedArrowRenderers[y].enabled = true;
			usedArrowRenderers[y].transform.localRotation = Quaternion.Euler(-90, 180 + 45 * (curStage.truthDirectionIdxes[y] + colorModifier), 0);
			colorblindTextMeshes[y].text = colorblindDetected ? colorRefAbbrev[curStage.colorDisplayIdxes[y]] : "";
		}

		for (float t = 0; t <= 1f; t += Time.deltaTime * speed)
		{
			for (var x = 0; x < values.Count(); x++)
			{
				var y = values.ElementAt(x);
				usedArrowRenderers[y].material.color = refColors[curStage.colorDisplayIdxes[y]] * t;
				colorblindTextMeshes[y].color = new Color(0.8f * t, 0.8f * t, 0.8f * t);
				usedScreenRenderers[y].material.color = new Color(0.5f * t, 0.25f * t, 0.25f * t);
			}
			yield return null;
        }
		specificTileFlasher = FlashSpecificTile(curStage.startIdx, true);
		StartCoroutine(specificTileFlasher);
	}

	IEnumerator AnimateDisplayedStage(StagePointerPointer curStage, float speed = 0.25f)
    {
		if (specificTileFlasher != null)
			StopCoroutine(specificTileFlasher);
		specificTileFlasher = FlashSpecificTile(curStage.startIdx, !inputting);
		for (var x = 0; x < Mathf.Min(usedArrowRenderers.Length, curStage.colorDisplayIdxes.Length); x++)
			usedArrowRenderers[x].enabled = true;

		StartCoroutine(PlaySoundXTimes("dial", 1 / speed / 8, 8));
		for (float t = 0; t <= 1f; t += Time.deltaTime * speed)
        {
			var timeModifier = Mathf.FloorToInt( (1 - t) * 32) % 8;
			for (var x = 0; x < Mathf.Min(usedArrowRenderers.Length, curStage.colorDisplayIdxes.Length); x++)
            {
				var colorModifier = 8 - curStage.colorDisplayIdxes[x];
				usedArrowRenderers[x].material.color = refColors[curStage.colorDisplayIdxes[x]] * t;
				usedArrowRenderers[x].transform.localRotation = Quaternion.Euler(-90, 180 + 45 * (timeModifier + curStage.truthDirectionIdxes[x] + colorModifier), 0);
				colorblindTextMeshes[x].text = colorblindDetected ? colorRefAbbrev[curStage.colorDisplayIdxes[x]] : "";
                colorblindTextMeshes[x].color = new Color(0.8f * t, 0.8f * t, 0.8f * t);
			}
			foreach (var renderer in usedScreenRenderers)
				renderer.material.color = Color.gray * t;
			yield return null;
        }
		for (var x = 0; x < Mathf.Min(usedArrowRenderers.Length, curStage.colorDisplayIdxes.Length); x++)
		{
			var colorModifier = 8 - curStage.colorDisplayIdxes[x];
			usedArrowRenderers[x].material.color = refColors[curStage.colorDisplayIdxes[x]];
			usedArrowRenderers[x].transform.localRotation = Quaternion.Euler(-90, 180 + 45 * (curStage.truthDirectionIdxes[x] + colorModifier), 0);
			colorblindTextMeshes[x].text = colorblindDetected ? colorRefAbbrev[curStage.colorDisplayIdxes[x]] : "";
			colorblindTextMeshes[x].color = new Color(0.8f, 0.8f, 0.8f);
		}
		foreach (var renderer in usedScreenRenderers)
			renderer.material.color = Color.gray;
		if (curStageIdx == 0)
			StartCoroutine(specificTileFlasher);
	}
	IEnumerator FlashScreensOrderly(float speed = 3f)
    {
		yield return HideBoard(5f);
		mAudio.PlaySoundAtTransform("UI_Numpad_Affirm_Amp_Trimmed", transform);
		var idxesToGreen = squareLength == 6 ? new[] { 1, 2, 3, 4, 6, 12, 18, 20, 21, 22, 23, 24, 29, 31, 32, 33, 34 } : Enumerable.Range(0, squareLength * squareLength).Where(a => a % squareLength % 2 != 1 && (a / squareLength) != squareLength - 2);
		var allVals = Enumerable.Range(0, squareLength * squareLength).Where(a => idxesToGreen.Contains(a));
		for (var x = 0; x < 2; x++)
		{
			for (float t = 0; t < 1f; t += Time.deltaTime * speed)
			{
				foreach (var idx in allVals)
					usedScreenRenderers[idx].material.color = Color.green * t;
				yield return null;
			}
			for (float t = 0; t < 1f; t += Time.deltaTime * speed)
			{
				foreach (var idx in allVals)
					usedScreenRenderers[idx].material.color = Color.green * (1f - t);
				yield return null;
			}
			foreach (var idx in allVals)
				usedScreenRenderers[idx].material.color = Color.black;
		}
		modSelf.HandlePass();
		yield return FlashScreensRandomly(moduleSolved, speed);
    }

	IEnumerator FlashScreensRandomly(bool looping = false, float speed = 3f)
    {
		yield return HideBoard(5f);
		var allVals = Enumerable.Range(0, squareLength * squareLength);
		var reversed = Random.value < 0.5f;
		var modificationType = Random.Range(0,3);
		do
		{
			var selectedIdxGlowPattern = Random.Range(0, 8);
			switch (selectedIdxGlowPattern)
			{
				case 0:
					for (var x = 0; x < squareLength; x++)
					{
						var selectedIdxes = allVals.Where(a => a % squareLength == (reversed ? squareLength - 1 - x : x));
						for (float t = 0; t < 1f; t += Time.deltaTime * speed)
						{
							foreach (var idx in selectedIdxes)
								usedScreenRenderers[idx].material.color = Color.green * t;
							yield return null;
						}
						for (float t = 0; t < 1f; t += Time.deltaTime * speed)
						{
							foreach (var idx in selectedIdxes)
								usedScreenRenderers[idx].material.color = Color.green * (1f - t);
							yield return null;
						}
						foreach (var idx in selectedIdxes)
							usedScreenRenderers[idx].material.color = Color.black;
					}
					break;
				case 1:
					for (var x = 0; x < squareLength; x++)
					{
						var selectedIdxes = allVals.Where(a => a / squareLength == (reversed ? squareLength - 1 - x : x));
						for (float t = 0; t < 1f; t += Time.deltaTime * speed)
						{
							foreach (var idx in selectedIdxes)
								usedScreenRenderers[idx].material.color = Color.green * t;
							yield return null;
						}
						for (float t = 0; t < 1f; t += Time.deltaTime * speed)
						{
							foreach (var idx in selectedIdxes)
								usedScreenRenderers[idx].material.color = Color.green * (1f - t);
							yield return null;
						}
						foreach (var idx in selectedIdxes)
							usedScreenRenderers[idx].material.color = Color.black;
					}
					break;
				case 2:
					for (var x = 0; x < squareLength * 2 - 1; x++)
					{
						var selectedIdxes = allVals.Where(a => a / squareLength + a % squareLength == (reversed ? squareLength * 2 - 2 - x : x));
						for (float t = 0; t < 1f; t += Time.deltaTime * speed)
						{
							foreach (var idx in selectedIdxes)
								usedScreenRenderers[idx].material.color = Color.green * t;
							yield return null;
						}
						for (float t = 0; t < 1f; t += Time.deltaTime * speed)
						{
							foreach (var idx in selectedIdxes)
								usedScreenRenderers[idx].material.color = Color.green * (1f - t);
							yield return null;
						}
						foreach (var idx in selectedIdxes)
							usedScreenRenderers[idx].material.color = Color.black;
					}
					break;
				case 3:
					for (var x = 0; x < squareLength * 2 - 1; x++)
					{
						var selectedIdxes = allVals.Where(a => a / squareLength + squareLength - 1 - a % squareLength == (reversed ? squareLength * 2 - 2 - x : x));
						for (float t = 0; t < 1f; t += Time.deltaTime * speed)
						{
							foreach (var idx in selectedIdxes)
								usedScreenRenderers[idx].material.color = Color.green * t;
							yield return null;
						}
						for (float t = 0; t < 1f; t += Time.deltaTime * speed)
						{
							foreach (var idx in selectedIdxes)
								usedScreenRenderers[idx].material.color = Color.green * (1f - t);
							yield return null;
						}
						foreach (var idx in selectedIdxes)
							usedScreenRenderers[idx].material.color = Color.black;
					}
					break;
				case 4:
					for (var x = 0; x < 2; x++)
					{
						var selectedIdxes = allVals.Where(a => (a / squareLength + a % squareLength) % 2 == x ^ reversed);
						for (float t = 0; t < 1f; t += Time.deltaTime * speed)
						{
							foreach (var idx in selectedIdxes)
								usedScreenRenderers[idx].material.color = Color.green * t;
							yield return null;
						}
						for (float t = 0; t < 1f; t += Time.deltaTime * speed)
						{
							foreach (var idx in selectedIdxes)
								usedScreenRenderers[idx].material.color = Color.green * (1f - t);
							yield return null;
						}
						foreach (var idx in selectedIdxes)
							usedScreenRenderers[idx].material.color = Color.black;
					}
					break;
				case 5:
					for (var x = 0; x < 2; x++)
					{
						var selectedIdxes = allVals.Where(a => a / squareLength % 2 == x ^ reversed);
						for (float t = 0; t < 1f; t += Time.deltaTime * speed)
						{
							foreach (var idx in selectedIdxes)
								usedScreenRenderers[idx].material.color = Color.green * t;
							yield return null;
						}
						for (float t = 0; t < 1f; t += Time.deltaTime * speed)
						{
							foreach (var idx in selectedIdxes)
								usedScreenRenderers[idx].material.color = Color.green * (1f - t);
							yield return null;
						}
						foreach (var idx in selectedIdxes)
							usedScreenRenderers[idx].material.color = Color.black;
					}
					break;
				case 6:
					for (var x = 0; x < 2; x++)
					{
						var selectedIdxes = allVals.Where(a => a % squareLength % 2 == x ^ reversed);
						for (float t = 0; t < 1f; t += Time.deltaTime * speed)
						{
							foreach (var idx in selectedIdxes)
								usedScreenRenderers[idx].material.color = Color.green * t;
							yield return null;
						}
						for (float t = 0; t < 1f; t += Time.deltaTime * speed)
						{
							foreach (var idx in selectedIdxes)
								usedScreenRenderers[idx].material.color = Color.green * (1f - t);
							yield return null;
						}
						foreach (var idx in selectedIdxes)
							usedScreenRenderers[idx].material.color = Color.black;
					}
					break;
				default:
					for (float t = 0; t < 1f; t += Time.deltaTime * speed)
					{
						foreach (var idx in allVals)
							usedScreenRenderers[idx].material.color = Color.green * t;
						yield return null;
					}
					for (float t = 0; t < 1f; t += Time.deltaTime * speed)
					{
						foreach (var idx in allVals)
							usedScreenRenderers[idx].material.color = Color.green * (1f - t);
						yield return null;
					}
					foreach (var idx in allVals)
						usedScreenRenderers[idx].material.color = Color.black;
					break;
			}
			reversed = modificationType == 2 ? Random.value < 0.5f : reversed ^ modificationType == 1;
		}
		while (looping);
	}

	IEnumerator FlashSpecificTile(int idx, bool looping = false, float speed = 1f)
    {
		var curStage = allPointerStages[curStageIdx];
		do
		{
			var referredColorCurTile = refColors[curStage.colorDisplayIdxes[idx]];

			for (float t = 0; t <= 1f; t += Time.deltaTime * speed)
			{
				usedArrowRenderers[idx].material.color = new Color(referredColorCurTile.r, referredColorCurTile.g, referredColorCurTile.b, 1f - t);
				yield return null;
			}
			for (float t = 0; t <= 1f; t += Time.deltaTime * speed)
			{
				usedArrowRenderers[idx].material.color = new Color(referredColorCurTile.r, referredColorCurTile.g, referredColorCurTile.b, t);
				yield return null;
			}

		}
		while (looping);
	}
	IEnumerator HideBoard(float speed = 2f)
    {
		var lastColorsAllScreens = usedScreenRenderers.Select(a => a.material.color).ToArray();
		var lastColorsAllArrows = usedArrowRenderers.Select(a => a.material.color).ToArray();
		var lastColorsAllColorblindDisplays = usedCBTextMeshes.Select(a => a.color).ToArray();
		for (float t = 1f; t >= 0f; t -= Time.deltaTime * speed)
		{
            for (int i = 0; i < usedScreenRenderers.Length; i++)
            {
				usedScreenRenderers[i].material.color = lastColorsAllScreens.ElementAt(i) * t;
            }
            for (int i = 0; i < usedArrowRenderers.Length; i++)
            {
				usedArrowRenderers[i].material.color = lastColorsAllArrows.ElementAt(i) * t;
				usedCBTextMeshes[i].color = lastColorsAllColorblindDisplays[i] * t;
			}
            yield return null;
		}
		for (int i = 0; i < usedScreenRenderers.Length; i++)
			usedScreenRenderers[i].material.color = Color.black;

		for (int i = 0; i < usedArrowRenderers.Length; i++)
		{
			usedArrowRenderers[i].material.color = Color.clear;
			usedArrowRenderers[i].enabled = false;
			usedCBTextMeshes[i].color = Color.clear;
		}
		
	}
	void HandleColorblindModeToggle()
    {
		if (!inputting)
        {
            var curStage = allPointerStages[curStageIdx];
            for (var x = 0; x < Mathf.Min(usedCBTextMeshes.Length, curStage.colorDisplayIdxes.Length); x++)
				usedCBTextMeshes[x].text = colorblindDetected ? colorRefAbbrev[curStage.colorDisplayIdxes[x]] : "";
		}
		else
        {
			var curStage = allPointerStages[curStageIdx];
			for (var x = 0; x < Mathf.Min(usedCBTextMeshes.Length, curStage.colorDisplayIdxes.Length); x++)
				usedCBTextMeshes[x].text = colorblindDetected && revealed ? colorRefAbbrev[curStage.colorDisplayIdxes[x]] : "";
		}
    }
	// Update is called once per frame
	void Update () {
		if (started && !moduleSolved)
		{
			var curSolveCount = bombInfo.GetSolvedModuleNames().Count(a => !ignoredModules.Contains(a));
			if (lastSolveCount != curSolveCount)
            {
				var differenceSolveCounts = curSolveCount - lastSolveCount;
				var requireStrike = false;
				var countStagesRemoved = 0;
				for (var x = 0; x < differenceSolveCounts; x++)
                {
					if (!inputting)
                    {
						StopCoroutine(screenFlashingAnim);
						StopCoroutine(specificTileFlasher);
						inputting = true;
						QuickLog("Solve detected, hiding arrows on stage {0}.", curStageIdx + 1);
						screenFlashingAnim = HideBoard();
						StartCoroutine(screenFlashingAnim);
                    }					
					else
                    {
						reachableStageIdx--;
						countStagesRemoved++;
						requireStrike = true;
                    }
                }
				lastSolveCount = curSolveCount;
				if (requireStrike && !disableStrike)
				{
					QuickLog("Strike! A stage is still waiting for input. The number of required stages to disarm the module has decreased by {0}.", countStagesRemoved);
					CauseStrikeMercy();
				}
            }
		}
	}
	IEnumerator AutosolveHandler()
    {
		while (!moduleSolved)
		{
			while (!inputting)
				yield return true;
			var curStage = allPointerStages[curStageIdx];
			screenSelectables[curStage.endIdx].OnInteract();
		}
	}

	void TwitchHandleForcedSolve()
	{
		disableStrike = true;
		QuickLog("Requesting autosolve via TP. Disabling strike handling on skipped stages.");
		StartCoroutine(AutosolveHandler());
	}
	readonly static string TwitchHelpMessage = "Press that button in the specified coordinate with \"!{0} X#\", \"press\" or \"submit\" is optional. Rows are labeled 1-6 from top to bottom; columns are labeled A-F from left to right. Toggle colorblind mode with \"!{0} colorblind/colourblind\".";
	IEnumerator ProcessTwitchCommand(string cmd)
    {
		if (moduleSolved)
        {
			yield return "sendtochaterror This module is refusing to accept commands onto this module.";
			yield break;
        }

		var regexColorblind = Regex.Match(cmd, @"^colou?rblind$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		var regex6x6Press = Regex.Match(cmd, @"^((submit|press)\s)?[A-F][1-6]$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		var regex4x4Press = Regex.Match(cmd, @"^((submit|press)\s)?[A-D][1-4]$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		if (regexColorblind.Success)
        {
			yield return null;
			colorblindDetected ^= true;
			HandleColorblindModeToggle();
        }
		else if (regex6x6Press.Success && squareLength == 6)
        {
			var expectedCoordinate = regex6x6Press.Value.Split().Last().ToUpper();
			var expectedCol = alphabet.IndexOf(expectedCoordinate[0]);
			var expectedRow = expectedCoordinate[1] - '1';
			if (expectedCol < 0 || expectedRow < 0)
			{
				yield return "sendtochaterror The expected coordinate given \"" + expectedCoordinate + "\" is not valid!";
				yield break;
			}
			var lastStageCnt = curStageIdx * 1;
			yield return null;
			screenSelectables[6 * expectedRow + expectedCol].OnInteract();
			if (curStageIdx != lastStageCnt && curStageIdx <= reachableStageIdx)
				yield return string.Format("awardpoints {0}", PPAToGive);
			else if (curStageIdx > reachableStageIdx)
				yield return "solve";
		}
		else if (regex4x4Press.Success && squareLength == 4)
        {
			var expectedCoordinate = regex4x4Press.Value.Split().Last().ToUpper();
			var expectedCol = alphabet.IndexOf(expectedCoordinate[0]);
			var expectedRow = expectedCoordinate[1] - '1';
			if (expectedCol < 0 || expectedRow < 0)
			{
				yield return "sendtochaterror The expected coordinate given \"" + expectedCoordinate + "\" is not valid!";
				yield break;
			}
			var lastStageCnt = curStageIdx * 1;
			yield return null;
			screenSelectables_4x4[4 * expectedRow + expectedCol].OnInteract();
			if (curStageIdx != lastStageCnt && curStageIdx <= reachableStageIdx)
				yield return string.Format("awardpoints {0}", PPAToGive);
			else if (curStageIdx > reachableStageIdx)
				yield return "solve";
		}
    }

}
