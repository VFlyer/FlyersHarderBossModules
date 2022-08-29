﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class EBTRGBLScript : MonoBehaviour {

	public KMBossModuleExtensions bossHandler;
	public KMBombInfo bombInfo;
	public KMBombModule modSelf;
	public KMAudio mAudio;
	public MeshRenderer[] gridRenderers, miscULs;
	public TextMesh[] displayTexts;
	public OutlineFillAnim[] boolAOutAnim, boolBOutAnim, logicOutAnim, arrowOutAnim;
	public SizeModifierAnim squareResizer;
	public DividerModifierAnim dividerHandler;
	public KMSelectable[] arrowSelectables, logicSelectables, grid;

	static int modIDCnt;
	int moduleID;
	int stageIdx;
	int squareLength, clrCur, hldIdx, maxStageAhd, maxStageBhd, solveCountNonIgnored, strSub;
	float cooldown = 10f;
	private bool animating, tickCooldown, bossActive, started = false, xoring, recoverable, enforceExhibiton;
	[SerializeField]
	private bool debugBossMode;
	int[] initialBoard, currentBoard, expectedBoard;
	List<bool[]> stageGrids;
	List<int> logicOper, allChannels, requiredStages, stgodr;
	List<bool> invertA, invertB, vld;
	string[] ignoreList;
    readonly Dictionary<int, Color32> clr = new Dictionary<int, Color32>
	{
		{0, Color.black },
		{1, new Color32(85, 85, 255, 0) },
		{2, Color.green },
		{3, Color.cyan },
		{4, Color.red },
		{5, Color.magenta },
		{6, Color.yellow },
		{7, Color.white },
	};
    readonly Dictionary<int, int[]> conflicts = new Dictionary<int, int[]>
	{
		{ 0, new[] { 1, 3, 5, 8 } },
		{ 1, new[] { 4, 5, 7, 9 } },
		{ 2, new[] { 6, 2 } },
		{ 3, new[] { 1, 5, 7, 9 } },
		{ 4, new[] { 0, 1, 3, 8 } },
		{ 5, new[] { 0, 4, 7, 9 } },
		{ 6, new[] { 2, 6 } },
		{ 7, new[] { 0, 3, 4, 8 } },
		{ 8, new[] { 1, 3, 5, 9 } },
		{ 9, new[] { 0, 4, 7, 8 } },
	};
	const string clrAbrev = "KBGCRMYW";
	static readonly string[] logicOperRef = { "OR", "AND", "XOR", "IMP", "NOR", "NAND", "XNOR", "IMPBY", "NIMP", "NIMPBY" };
	FlyersBossierSettings globalSettings = new FlyersBossierSettings();
	// Use this for initialization
	void QuickLog(string value, params object[] args)
    {
		Debug.LogFormat("[{0} #{1}]: {2}", "Slight Gibberish Twist", moduleID, string.Format(value, args));
    }
	void QuickLogDebug(string value, params object[] args)
    {
		Debug.LogFormat("<{0} #{1}>: {2}", "Slight Gibberish Twist", moduleID, string.Format(value, args));
    }
	void Awake()
    {
		if (debugBossMode)
		{
			modSelf.ModuleDisplayName = "Forget Me Not";
		}
		try
        {
			var modSettings = new ModConfig<FlyersBossierSettings>("FlyersBossierSettings");
			globalSettings = modSettings.Settings;
			modSettings.Settings = globalSettings;
			enforceExhibiton = globalSettings.SGTExhibitionMode;
			maxStageAhd = globalSettings.SGTMaxStagesAhead;
			maxStageBhd = globalSettings.SGTMaxStagesBehind;
        }
		catch
        {
			enforceExhibiton = false;
			maxStageAhd = 15;
			maxStageBhd = 5;
        }
    }
	void CalculateExpectedBoard(bool recalcing = false)
    {
		if (!recalcing) requiredStages = new List<int>();
		else requiredStages.Clear();
		strSub = 0;
		for (var x = 0; x < 3; x++)
		{
			requiredStages.AddRange(Enumerable.Range(1, stageGrids.Count).Where(a => allChannels[a - 1] == x).ToArray().Shuffle().Take(3));
		}
		requiredStages.Shuffle();
		if (recalcing)
			QuickLog("WARNING! Activating Recovery Mode changed the required stages to disarm the module in the particular order: {0}", requiredStages.Select(a => a + 1).Join(", "));
		else
			QuickLog("Required stages to solve in order: {0}", requiredStages.Select(a => a + 1).Join(", "));
		expectedBoard = initialBoard.ToArray();

		for (var cnt = 0; cnt < requiredStages.Count; cnt++)
		{
			var curI = logicOper[requiredStages[cnt]];
			if (!vld[requiredStages[cnt]])
			{
				QuickLog("Stage {0} was not a valid stage to calculate. Current board should be left as is after applying this stage.", requiredStages[cnt] + 1);
				continue;
			}
			var chn = allChannels[requiredStages[cnt] - 1];
			var prevChan = expectedBoard.Select(a => (a >> chn) % 2 == 1);
			var resChan = Enumerable.Range(0, squareLength * squareLength).Select(a => Oper(prevChan.ElementAt(a), stageGrids[requiredStages[cnt] - 1][a], curI));
			for (var zp = 0; zp < expectedBoard.Length; zp++)
			{
				if (((expectedBoard[zp] >> chn) % 2 == 1) ^ resChan.ElementAt(zp))
					expectedBoard[zp] ^= 1 << chn;
			}
			QuickLog("Stage {0} was a valid stage with the operator: {1}", requiredStages[cnt] + 1, logicOperRef[curI]);
			QuickLog("Board after modification on stage {1} (from left to right, top to bottom): {0}",
				Enumerable.Range(0, squareLength).Select(a => Enumerable.Range(0, squareLength).Select(b => clrAbrev[expectedBoard[a * squareLength + b]]).Join("")).Join(","), requiredStages[cnt] + 1);
		}
		for (var cnt = 0; cnt < 3; cnt++)
		{
			QuickLogDebug("Expected {1} state (from left to right, top to bottom): {0}", expectedBoard.Select(a => (a >> cnt) % 2 == 1 ? "T" : "F").Join(""), clrAbrev[1 << cnt]);
		}
		if (recalcing) QuickLog("New expected final state (from left to right, top to bottom): {0}",
			Enumerable.Range(0, squareLength).Select(a => Enumerable.Range(0, squareLength).Select(b => clrAbrev[expectedBoard[a * squareLength + b]]).Join("")).Join(","));
        else QuickLog("Expected board to submit (from left to right, top to bottom): {0}",
            Enumerable.Range(0, squareLength).Select(a => Enumerable.Range(0, squareLength).Select(b => clrAbrev[expectedBoard[a * squareLength + b]]).Join("")).Join(","));

    }
	void Start()
    {
        moduleID = ++modIDCnt;
        QuickLog("This idea of a manual challenge for this module was given up due to the fact that it was too difficult to construct a manual even for a dedicated player. If anyone does want to attempt this as an actual manual challenge do let me know, as the creator.");

        var p = new[] { 4, 7, 10, 13 };
        ignoreList = bossHandler.GetAttachedIgnoredModuleIDs(modSelf,
			Application.isEditor ? new[] { "slightGibberishTwistModule" } : new string[0]);
		var maxExtraStages = bombInfo.GetSolvableModuleIDs().Count(a => !ignoreList.Contains(a)) - 1;
        if (ignoreList != null && ignoreList.Any())
        {
            bossActive = true;
            squareLength = 8 - p.Count(a => maxExtraStages >= a);
			QuickLog("Total extra stages generatable: {0}", maxExtraStages < 0 ? 0 : maxExtraStages);
		}
        else
        {
			recoverable = true;
            maxExtraStages = 9;
            squareLength = 6;
			QuickLog("Boss mode is not active. Generating 9 extra stages.");
		}
		if (maxExtraStages <= 2)
        {
			bossActive = false;
			recoverable = true;
			maxExtraStages = 3;
			QuickLog("Insufficient non-ignored modules. Generating 3 extra stages instead, and disabling boss mode.");
		}
		else if (enforceExhibiton)
        {
			bossActive = false;
			recoverable = true;
			QuickLog("Disabling boss mode due to settings preventing this.");
		}
		QuickLog("Allocating board size {0} by {0}.", squareLength);
		squareResizer.HandleResize(squareLength);
		dividerHandler.HandleResize(squareLength);
		initialBoard = new int[squareLength * squareLength];
		currentBoard = new int[squareLength * squareLength];
		for (var x = 0; x < initialBoard.Length; x++)
            initialBoard[x] = Random.Range(0, 8);
		QuickLog("-------------- Stage 1 ---------------");
		QuickLog("Initial board (from left to right, top to bottom): {0}",
			Enumerable.Range(0, squareLength).Select(a => Enumerable.Range(0, squareLength).Select(b => clrAbrev[initialBoard[a * squareLength + b]]).Join("")).Join(","));


		logicOper = new List<int>();
        allChannels = new List<int>();
		
		stageGrids = new List<bool[]>();
		invertA = new List<bool>();
		invertB = new List<bool>();
		vld = new List<bool>();
		
		logicOper.Add(Random.Range(0, 10));
		invertA.Add(Random.value < 0.5f);
		invertB.Add(Random.value < 0.5f);
		vld.Add(true);
		QuickLog("Displayed Operator: {0}", logicOperRef[logicOper.First()]);

		var arrayIdxes = Enumerable.Range(0, 3).ToArray().Shuffle();
		var l = 0;
        for (var x = 0; x < maxExtraStages; x++)
        {
			QuickLog("-------------- Stage {0} ---------------", x + 2);
			var curOper = Random.Range(0, 10);
			logicOper.Add(curOper);
			QuickLog("Displayed Operator: {0}", logicOperRef[curOper]);
			invertA.Add(Random.value < 0.5f);
			invertB.Add(Random.value < 0.5f);
			if (l >= arrayIdxes.Length)
            {
				l = 0;
				arrayIdxes.Shuffle();
            }
			allChannels.Add(arrayIdxes[l]);
			QuickLog("Assigned Channel: {0}", clrAbrev[1 << arrayIdxes[l]]);
			l++;
			var newStage = new bool[squareLength * squareLength];
            for (var y = 0; y < newStage.Length; y++)
				newStage[y] = Random.value < 0.5f;
			QuickLog("Displayed Grid (GOL Style): {0}", Enumerable.Range(0, squareLength).Select(a => Enumerable.Range(0, squareLength).Any(b => newStage[a * squareLength + b]) ? Enumerable.Range(0, squareLength).Where(b => newStage[a * squareLength + b]).Select(b => b + 1).Join("") : "-").Join(","));
			stageGrids.Add(newStage);
			var curVld = !(conflicts.ContainsKey(curOper) && conflicts[curOper].Contains(logicOper[x]));
			if (x >= 1)
			{
				var temp = vld.TakeLast(2);
				curVld = !temp.All(a => a) && (temp.All(a => !a) || curVld);
			}
			QuickLog("Validity on current stage: {0}", curVld ? "VALID" : "INVALID");
			vld.Add(curVld);
		}
		QuickLog("--------------- Stage Ordering ---------------");
		for (var x = 0; x < displayTexts.Length; x++)
			displayTexts[x].text = "";

		if (bossActive)
		{
			stgodr = new List<int>();
			stgodr.AddRange(Enumerable.Range(0, maxExtraStages + 1));
			if (maxStageBhd > 0 && maxStageAhd > 0)
			{
				var allStages = new List<int>();
				allStages.AddRange(stgodr);
				stgodr.Clear();
				var why = new int[allStages.Count];
                for (var x = 0; x < why.Length; x++)
                {
					var min = x - maxStageAhd;
					var stgIdxes = allStages.Where(a => a <= x + maxStageBhd && a >= x - maxStageAhd).ToList();
					if (!stgIdxes.Any())
                    {
						var low = -1;
						var high = -1;
						foreach (var sidx in stgIdxes)
							if (sidx < x) low = Mathf.Max(low, sidx);
							else high = Mathf.Min(high, sidx);
						if (low != -1) stgIdxes.Add(low);
						if (high != -1) stgIdxes.Add(high);
					}
					var newStg = stgIdxes.PickRandom();
					if (stgIdxes.Contains(min)) newStg = min;
					why[x] = newStg;
					allStages.Remove(newStg);
                }
				stgodr = why.ToList();

				QuickLog("Stages will be displayed in this order in accordiance to the settings, max {1} stage(s) behind, max {2} stage(logicSelectables) ahead: {0}", stgodr.Select(a => a + 1).Join(", "), maxStageBhd, maxStageAhd);
			}
			else if (maxStageAhd <= 0 && maxStageBhd > 0)
			{
				stgodr.Reverse();
				QuickLog("Stages will be displayed in this order in accordiance to the settings, max {1} stage(s) behind, 1 stage ahead: {0}", stgodr.Select(a => a + 1).Join(", "), maxStageBhd);
			}
			else if (maxStageAhd <= 0 && maxStageBhd <= 0)
			{
				stgodr.Shuffle();
				QuickLog("Stages will be displayed in this order in accordiance to the settings, as many stages behind, as many stages ahead: {0}", stgodr.Select(a => a + 1).Join(", "));
			}
			else
			{
				QuickLog("Stages will be displayed in this order in accordiance to the settings, max 1 stage behind, {1} stages ahead: {0}", stgodr.Select(a => a + 1).Join(", "), maxStageAhd);
			}
		}
		else
			QuickLog("Not available.");
		QuickLog("--------------- Submission ---------------");
		CalculateExpectedBoard();
		QuickLog("--------------- User Interactions ---------------");
		for (var x = 0; x < arrowSelectables.Length; x++)
        {
			var y = x;
			arrowSelectables[x].OnInteract += delegate {
				arrowSelectables[y].AddInteractionPunch(0.1f);
				mAudio.PlaySoundAtTransform("BlipSelect", arrowSelectables[y].transform);
				if (!animating && started)
                {
					if (IsInSubmission() && recoverable && !tickCooldown)
                    {
						CalculateExpectedBoard(true);
						stageIdx = 0;
						StartCoroutine(UncolorizePallete());
						StartCoroutine(BigAnim());
					}
					else if ((!bossActive || recoverable) && !IsInSubmission())
					{
						cooldown = 2f;
						if (!tickCooldown)
						{
							tickCooldown = true;
							StartCoroutine(TickDelayStg(stageIdx));
						}
						stageIdx = (stageIdx + (y == 0 ? -1 : 1) + logicOper.Count) % logicOper.Count;
						hldIdx = y;
					}

				}
				return false;
			};
			arrowSelectables[x].OnInteractEnded += delegate {
				hldIdx = -1;
			};
        }
        for (var x = 0; x < logicSelectables.Length; x++)
        {
			var y = x;
			logicSelectables[x].OnInteract += delegate {
				logicSelectables[y].AddInteractionPunch(0.1f);
				mAudio.PlaySoundAtTransform("Douga", logicSelectables[y].transform);
				if (!animating && started)
                {
					if (IsInSubmission())
					{
						if (y == 3)
							xoring ^= true;
						else
							clrCur ^= 1 << y;
						logicOutAnim[y].filled = y == 3 ? xoring : (clrCur >> y) % 2 == 1;
						cooldown = 5f;
						if (xoring && clrCur == 0 && !tickCooldown)
						{
							StartCoroutine(TickDelaySub());
						}
					}
					else if ((!bossActive || recoverable) && !tickCooldown && !IsInSubmission())
                    {
						stageIdx = logicOper.Count;
						StartCoroutine(BigAnim());
                    }
					else if ((!bossActive || recoverable) && tickCooldown)
                    {
						cooldown = 0f;
					}
				}
				return false;
			};
        }
        for (var x = 0; x < grid.Length; x++)
        {
			var y = x;
			if (!grid[x].gameObject.activeSelf) continue;
			grid[x].OnInteract += delegate {
				grid[y].AddInteractionPunch(0.1f);
				mAudio.PlaySoundAtTransform("BlipSelect", grid[y].transform);
				if (!animating && started && IsInSubmission())
                {
					var idxX = y % 8;
					var idxY = y / 8;

					if (idxX < squareLength && idxY < squareLength)
						currentBoard[idxY * squareLength + idxX] = xoring ? currentBoard[idxY * squareLength + idxX] ^ clrCur : clrCur;

					UpdateInputBoard();
				}
				return false;
			};
        }
		modSelf.OnActivate += delegate {
			StartCoroutine(BigAnim());
		};
	}
	IEnumerator TickDelayStg(int lastStageIdx)
    {
		while (cooldown > 0f)
		{
			yield return null;
			cooldown -= Time.deltaTime;
			if (cooldown < 1.5f && hldIdx != -1)
            {
				stageIdx = (stageIdx + (hldIdx == 0 ? -1 : 1) + logicOper.Count) % logicOper.Count;
				mAudio.PlaySoundAtTransform("BlipSelect", arrowSelectables[hldIdx].transform);
				cooldown = 1.6f;
			}
            yield return AnimateASet(Enumerable.Range(-4, 9).Select(a => string.Format( a == 0 ? ">{0}<" : "{0}", (stageIdx + a + logicOper.Count) % logicOper.Count + 1)).ToArray());
		}
		tickCooldown = false;

		if (stageIdx != lastStageIdx)
			yield return BigAnim();
		else
        {
			StartCoroutine(AnimateASet(delay: 0, txt: new[] { "STAGE", (stageIdx + 1).ToString("000"), "" }));
			StartCoroutine(AnimateASet(delay: 0f, offset: 3, txt: new[] { "CHN", stageIdx >= 1 ? clrAbrev[1 << allChannels[stageIdx - 1]].ToString() : "RGB", "" }));
			StartCoroutine(bossActive ? AnimateASet(delay: 0, offset: 6, txt: new[] { "READY", "TO", "SOLVE" }) : AnimateASet(delay: 0, offset: 6, txt: new[] { "BOSS", "MODE", "OFF" }));
		}
    }
	IEnumerator TickDelaySub()
    {
		var combinedX = boolAOutAnim.Concat(boolBOutAnim).Reverse();
		while (cooldown > 0f && clrCur == 0 && xoring)
		{
			cooldown -= Time.deltaTime;
			for (var x = 0; x < combinedX.Count(); x++)
            {
				combinedX.ElementAt(x).filled = x + 1 < cooldown;
            }
			yield return null;
		}
		tickCooldown = false;
		if (clrCur == 0 && xoring)
		{
			animating = true;
			logicOutAnim.Last().filled = false;
			for (var x = 0; x < arrowOutAnim.Length; x++)
			{
				arrowOutAnim[x].filled = false;
			}
			StartCoroutine(AnimateASet(0.2f, 0, "", "", "", "", "", "", "", "", ""));
			mAudio.PlaySoundAtTransform("Douga", transform);
			mAudio.PlaySoundAtTransform("InputCheck", transform);
			var horizScan = Random.value < 0.5f;
            for (float time = 0; time <= 1f; time += Time.deltaTime)
			{
				for (var x = 0; x < squareLength * squareLength; x++)
				{
					var idxX = x % squareLength;
					var idxY = x / squareLength;
					gridRenderers[8 * idxY + idxX].material.color = horizScan ?
						time * squareLength >= idxY ? clr[0] : clr[currentBoard[x]] :
						time * squareLength >= idxX ? clr[0] : clr[currentBoard[x]];
				}
				yield return null;
			}
            for (float time = 0; time <= 1f; time += Time.deltaTime * 2)
			{
				for (var x = 0; x < squareLength * squareLength; x++)
				{
					var idxX = x % squareLength;
					var idxY = x / squareLength;
					gridRenderers[8 * idxY + idxX].material.color = horizScan ?
						time * squareLength >= idxY ? clr[7] : clr[0] :
						time * squareLength >= idxX ? clr[7] : clr[0];
				}
				yield return null;
			}
            for (float time = 0; time <= 1f; time += Time.deltaTime * 2)
			{
				for (var x = 0; x < squareLength * squareLength; x++)
				{
					var idxX = x % squareLength;
					var idxY = x / squareLength;
					gridRenderers[8 * idxY + idxX].material.color = horizScan ?
						time * squareLength >= idxY ? clr[0] : clr[7] :
						time * squareLength >= idxX ? clr[0] : clr[7];
				}
				yield return null;
			}
			for (var x = 0; x < squareLength * squareLength; x++)
			{
				var idxX = x % squareLength;
				var idxY = x / squareLength;
				gridRenderers[8 * idxY + idxX].material.color = clr[7];
			}
			yield return new WaitForSeconds(0.2f);

			var correct = Enumerable.Range(0, squareLength * squareLength).Where(a => currentBoard[a] == expectedBoard[a]);

			for (var x = 0; x < squareLength * squareLength; x++)
			{
				var idxX = x % squareLength;
				var idxY = x / squareLength;
				gridRenderers[8 * idxY + idxX].material.color = strSub == 0 ? x < correct.Count() ? clr[2] : clr[4] : correct.Contains(x) ? clr[2] : clr[4];
			}
			QuickLog("Submitted the current state: (from left to right, top to bottom): {0}",
				Enumerable.Range(0, squareLength).Select(a => Enumerable.Range(0, squareLength).Select(b => clrAbrev[currentBoard[a * squareLength + b]]).Join("")).Join(","));
			if (currentBoard.SequenceEqual(expectedBoard))
            {
				QuickLog("SOLVED. No errors detected.");
				mAudio.HandlePlaySoundAtTransform("540321__colorscrimsontears__system-shutdown", transform);
				modSelf.HandlePass();
				for (float time = 0; time <= 1f; time += Time.deltaTime / 4)
				{
					for (var x = 0; x < miscULs.Length; x++)
					{
						miscULs[x].material.color = time * Color.black + (1f - time) * Color.white;
					}
					
					for (var x = 0; x < squareLength * squareLength; x++)
					{
						var idxX = x % squareLength;
						var idxY = x / squareLength;
						gridRenderers[8 * idxY + idxX].material.color = time * (Color)clr[0] + (1f - time) * (Color)clr[2];
					}
					for (var x = 0; x < combinedX.Count(); x++)
					{
						combinedX.ElementAt(x).filledRenderer.material.color = (1f - time) * Color.yellow + time * Color.black;
						combinedX.ElementAt(x).outlineRenderer.material.color = (1f - time) * Color.yellow + time * Color.black;
					}
					for (var x = 0; x < logicOutAnim.Length; x++)
					{
						logicOutAnim[x].filledRenderer.material.color = (1f - time) * (Color)clr[x == 3 ? 7 : 1 << x] + time * Color.black;
						logicOutAnim[x].outlineRenderer.material.color = (1f - time) * (Color)clr[x == 3 ? 7 : 1 << x] + time * Color.black;
					}
					for (var x = 0; x < arrowOutAnim.Length; x++)
					{
						arrowOutAnim[x].filledRenderer.material.color = (1f - time) * Color.white + time * Color.black;
						arrowOutAnim[x].outlineRenderer.material.color = (1f - time) * Color.white + time * Color.black;
					}
					yield return null;
				}
				for (var x = 0; x < squareLength * squareLength; x++)
				{
					var idxX = x % squareLength;
					var idxY = x / squareLength;
					gridRenderers[8 * idxY + idxX].material.color = clr[0];
				}
				for (var x = 0; x < combinedX.Count(); x++)
				{
					combinedX.ElementAt(x).filledRenderer.material.color = Color.black;
					combinedX.ElementAt(x).outlineRenderer.material.color = Color.black;
				}
				for (var x = 0; x < logicOutAnim.Length; x++)
				{
					logicOutAnim[x].filledRenderer.material.color = Color.black;
					logicOutAnim[x].outlineRenderer.material.color = Color.black;
				}
				for (var x = 0; x < arrowOutAnim.Length; x++)
				{
					arrowOutAnim[x].filledRenderer.material.color = Color.black;
					arrowOutAnim[x].outlineRenderer.material.color = Color.black;
				}
				yield break;
            }
			strSub++;
			QuickLog("STRIKE. Correct cells filled: {0} / {1}", correct.Count(), squareLength * squareLength);
			QuickLog("With the current displayed stages in order, the user has struck {0} time(logicSelectables).", strSub);
			mAudio.HandlePlaySoundAtTransform("249300__suntemple__access-denied", transform);
			modSelf.HandleStrike();
			xoring = false;
			recoverable = true;
			if (strSub >= 2)
				for (var y = 0; y < squareLength * squareLength; y++)
				{
					var p = y % squareLength;
					var invertB = y / squareLength;
					currentBoard[y] = expectedBoard[y] != currentBoard[y] ? 0 : currentBoard[y];
				}
			yield return new WaitForSeconds(2f);
			UpdateInputBoard();
			for (var x = 0; x < combinedX.Count(); x++)
			{
				combinedX.ElementAt(x).filled = true;
			}
			for (var x = 0; x < arrowOutAnim.Length; x++)
			{
				arrowOutAnim[x].filled = recoverable;
			}
			StartCoroutine(AnimateASet(delay: 0.1f, txt: requiredStages.Select(a => (a + 1).ToString("000")).ToArray()));
			animating = false;
		}
		else
			for (var x = 0; x < combinedX.Count(); x++)
			{
				combinedX.ElementAt(x).filled = true;
			}
	}
	void UpdateInputBoard()
    {
		for (var y = 0; y < squareLength * squareLength; y++)
		{
			var p = y % squareLength;
			var invertB = y / squareLength;
			gridRenderers[8 * invertB + p].material.color = clr[currentBoard[y]];
		}
	}

	bool IsInSubmission()
    {
		return stageIdx >= logicOper.Count;

	}

	bool Oper(bool a, bool b, int idx)
    {
		switch(idx)
        {
			case 0:
				return a || b;
			case 1:
				return a && b;
			case 2:
				return a ^ b;
			case 3:
				return !a || b;
			case 4:
				return !(a || b);
			case 5:
				return !(a && b);
			case 6:
				return !a ^ b;
			case 7:
				return a || !b;
			case 8:
				return !(!a || b);
			case 9:
				return !(a || !b);
        }
		return a;
    }

	IEnumerator BigAnim()
    {
		animating = true;
        yield return null;
		mAudio.PlaySoundAtTransform("Simpletonic", transform);
		StartCoroutine(AnimateASet(0.1f, 0, "A", "D", "V", "A", "N", "C", "I", "N", "G"));
		if (stageIdx >= logicOper.Count)
			StartCoroutine(ColorizePallete());
		for (var x = 0; x < arrowOutAnim.Length; x++)
		{
			arrowOutAnim[x].filled = false;
		}
		for (var x = 0; x < 8; x++)
        {
            for (var y = 0; y < squareLength * squareLength; y++)
            {
				var p = y % squareLength;
				var invertB = y / squareLength;
				var curStg = !bossActive || recoverable ? stageIdx : stgodr.ElementAtOrDefault(stageIdx);
				gridRenderers[8 * p + invertB].material.color = curStg == 0 || stageIdx >= logicOper.Count ? clr[Random.Range(0, 8)] : Random.value < 0.5f ? clr[1 << Random.Range(0, 3)] : clr[0];
			}
            for (var y = 0; y < boolAOutAnim.Length; y++)
            {
				boolAOutAnim[y].filled = Random.value < 0.5f;
			}
            for (var y = 0; y < boolBOutAnim.Length; y++)
            {
				boolBOutAnim[y].filled = Random.value < 0.5f;
			}
            for (var y = 0; y < logicOutAnim.Length; y++)
            {
				logicOutAnim[y].filled = Random.value < 0.5f;
			}
			yield return new WaitForSeconds(0.25f);
        }

		if (IsInSubmission())
		{
			for (var y = 0; y < squareLength * squareLength; y++)
			{
				var p = y % squareLength;
				var invertB = y / squareLength;
				gridRenderers[8 * invertB + p].material.color = clr[currentBoard[y]];
			}
			StartCoroutine(AnimateASet(delay: 0.1f, txt: requiredStages.Select(a => (a + 1).ToString("000")).Concat(Enumerable.Repeat("", 9 - requiredStages.Count())).ToArray()));
			for (var y = 0; y < boolAOutAnim.Length; y++)
			{
				boolAOutAnim[y].filled = true;
			}
			for (var y = 0; y < boolBOutAnim.Length; y++)
			{
				boolBOutAnim[y].filled = true;
			}
			for (var y = 0; y < logicOutAnim.Length; y++)
			{
				logicOutAnim[y].filled = y == 3 ? xoring : (clrCur >> y) % 2 == 1;
			}
			for (var x = 0; x < arrowOutAnim.Length; x++)
			{
				arrowOutAnim[x].filled = recoverable;
			}
			animating = false;
			if (TwitchPlaysActive)
				TwitchHelpMessage = HelpSubmission;
			yield break;
		}
		else
		{
			var curStage = !bossActive || recoverable ? stageIdx : stgodr.ElementAtOrDefault(stageIdx);
			if (curStage == 0)
			{
				for (var y = 0; y < squareLength * squareLength; y++)
				{
					var p = y % squareLength;
					var invertB = y / squareLength;
					gridRenderers[8 * invertB + p].material.color = clr[initialBoard[y]];
				}
			}
			else
			{
				for (var y = 0; y < squareLength * squareLength; y++)
				{
					var p = y % squareLength;
					var invertB = y / squareLength;
					gridRenderers[8 * invertB + p].material.color = stageGrids[curStage - 1][y] ? clr[1 << allChannels[curStage - 1]] : clr[0];
				}
			}
			StartCoroutine(AnimateASet(delay: 0.1f, txt: new[] { "STAGE", (curStage + 1).ToString("000"), "" }));
			StartCoroutine(AnimateASet(delay: 0.1f, offset: 3, txt: new[] { "CHN", curStage >= 1 ? clrAbrev[1 << allChannels[curStage - 1]].ToString() : "RGB", "" }));
			StartCoroutine(AnimateASet(delay: 0.1f, offset: 6, txt: bossActive ? recoverable ? new[] { "READY", "TO", "SOLVE" } : new[] { "STAGES", "QUEUED" } : new[] { "BOSS", "MODE", "OFF" }));
			if (bossActive && !recoverable)
			{
				StartCoroutine(AnimateASet(delay: 0f, offset: 8, txt: (solveCountNonIgnored - stageIdx).ToString("00000")));
			}
			for (var x = 0; x < boolAOutAnim.Length; x++)
			{
				boolAOutAnim[x].filled = x == 1 ^ invertA[curStage];
			}
			for (var x = 0; x < boolBOutAnim.Length; x++)
			{
				boolBOutAnim[x].filled = x == 1 ^ invertB[curStage];
			}

			for (var x = 0; x < logicOutAnim.Length; x++)
			{
				var xVal = x % 2 == 1 ^ invertA[curStage];
				var yVal = x / 2 == 1 ^ invertB[curStage];
				logicOutAnim[x].filled = Oper(xVal, yVal, logicOper[curStage]);
			}
			for (var x = 0; x < arrowOutAnim.Length; x++)
			{
				arrowOutAnim[x].filled = !bossActive || recoverable;
			}
			started = true;
			tickCooldown = bossActive && !recoverable;
			animating = false;
			if (TwitchPlaysActive)
				TwitchHelpMessage = HelpRecoverExhibiton;
		}
    }
	IEnumerator AnimateASet(params string[] txt)
    {
		yield return AnimateASet(0, 0, txt);
    }
	IEnumerator AnimateASet(float delay = 0.1f, int offset = 0, params string[] txt)
    {
		for (var x = 0; x + offset < displayTexts.Length && x < txt.Length; x++)
        {
			displayTexts[x + offset].text = txt[x];
			if (delay > 0f)
				yield return new WaitForSeconds(delay);
        }
    }

	IEnumerator ColorizePallete()
    {
		for (float time = 0; time < 1f; time += Time.deltaTime / 2)
		{
			for (var x = 0; x < boolAOutAnim.Length; x++)
			{
				boolAOutAnim[x].outlineRenderer.material.color = time * Color.yellow + (1f - time) * Color.white;
				boolAOutAnim[x].filledRenderer.material.color = time * Color.yellow + (1f - time) * Color.white;
			}
			for (var x = 0; x < boolBOutAnim.Length; x++)
			{
				boolBOutAnim[x].filledRenderer.material.color = time * Color.yellow + (1f - time) * Color.white;
				boolBOutAnim[x].outlineRenderer.material.color = time * Color.yellow + (1f - time) * Color.white;
			}
			for (var x = 0; x < logicOutAnim.Length - 1; x++)
			{
				logicOutAnim[x].filledRenderer.material.color = time * (Color)clr[1 << x] + (1f - time) * Color.white;
				logicOutAnim[x].outlineRenderer.material.color = time * (Color)clr[1 << x] + (1f - time) * Color.white;
			}
			yield return null;
		}
		for (var x = 0; x < boolAOutAnim.Length; x++)
		{
			boolAOutAnim[x].outlineRenderer.material.color = Color.yellow;
			boolAOutAnim[x].outlineRenderer.material.color = Color.yellow;
		}
		for (var x = 0; x < boolBOutAnim.Length; x++)
		{
			boolBOutAnim[x].filledRenderer.material.color = Color.yellow;
			boolBOutAnim[x].outlineRenderer.material.color = Color.yellow;
		}
		for (var x = 0; x < logicOutAnim.Length - 1; x++)
		{
			logicOutAnim[x].filledRenderer.material.color = clr[1 << x];
			logicOutAnim[x].outlineRenderer.material.color = clr[1 << x];
		}
	}
	IEnumerator UncolorizePallete()
    {
		for (float time = 0; time < 1f; time += Time.deltaTime / 2)
		{
			for (var x = 0; x < boolAOutAnim.Length; x++)
			{
				boolAOutAnim[x].outlineRenderer.material.color = (1f - time) * Color.yellow + time * Color.white;
				boolAOutAnim[x].filledRenderer.material.color = (1f - time) * Color.yellow + time * Color.white;
			}
			for (var x = 0; x < boolBOutAnim.Length; x++)
			{
				boolBOutAnim[x].filledRenderer.material.color = (1f - time) * Color.yellow + time * Color.white;
				boolBOutAnim[x].outlineRenderer.material.color = (1f - time) * Color.yellow + time * Color.white;
			}
			for (var x = 0; x < logicOutAnim.Length - 1; x++)
			{
				logicOutAnim[x].filledRenderer.material.color = (1f - time) * (Color)clr[1 << x] + time * Color.white;
				logicOutAnim[x].outlineRenderer.material.color = (1f - time) * (Color)clr[1 << x] + time * Color.white;
			}
			yield return null;
		}
		for (var x = 0; x < boolAOutAnim.Length; x++)
		{
			boolAOutAnim[x].outlineRenderer.material.color = Color.white;
			boolAOutAnim[x].outlineRenderer.material.color = Color.white;
		}
		for (var x = 0; x < boolBOutAnim.Length; x++)
		{
			boolBOutAnim[x].filledRenderer.material.color = Color.white;
			boolBOutAnim[x].outlineRenderer.material.color = Color.white;
		}
		for (var x = 0; x < logicOutAnim.Length - 1; x++)
		{
			logicOutAnim[x].filledRenderer.material.color = Color.white;
			logicOutAnim[x].outlineRenderer.material.color = Color.white;
		}
	}

	// Update is called once per frame
	void Update () {
		if (!(bossActive && started) || animating || recoverable) return;
		if (tickCooldown && cooldown > 0f)
			cooldown -= Time.deltaTime;
		var curSolveCnt = bombInfo.GetSolvedModuleIDs().Count(a => !ignoreList.Contains(a));
		if (solveCountNonIgnored != curSolveCnt)
			solveCountNonIgnored = curSolveCnt;
		if (stageIdx < stgodr.Count)
			StartCoroutine(AnimateASet(delay: 0f, offset: 8, txt: (solveCountNonIgnored - stageIdx).ToString("00000")));
		if (cooldown <= float.Epsilon)
        {
			if (stageIdx < solveCountNonIgnored)
			{
				stageIdx++;
				tickCooldown = false;
				cooldown = 10f;
				StartCoroutine(BigAnim());
			}
        }
	}

	bool TwitchPlaysActive;
	readonly static string HelpRecoverExhibiton = "Go to the specified stage with \"!{0} stage ###\"; enter submission with \"!{0} submit\".",
		HelpSubmission = "Toggle that channel with \"!{0} R/G/B\" or the XOR operator with \"!{0} X\" or both a channel and the XOR operator with \"!{0} [R/G/B]X\" Press that button in the specified coordinate with \"!{0} X#\". Rows are labeled 1-8 from top to bottom; columns are labeled A-H from left to right (dependent on board size)." +
		" The previous 2 can be chained with spaces, I.E. \"!{0} R A1 X G G7 F5 D3 B B2\"... Submit the ENTIRE board with \"!{0} submit RGBCMYWKRGBCMYWK\" (dependent on board size). Enter stage recovery with \"!{0} recover\"";
	string TwitchHelpMessage = "Go to the specified stage with \"!{0} stage ###\"; enter submission with \"!{0} submit\".";
	IEnumerator ProcessTwitchCommand(string command)
    {
		if (!started || animating)
        {
			yield return "sendtochaterror The module is refusing to accept input right now. Wait a bit.";
			yield break;
        }

		Match regexSub = Regex.Match(command, @"^s(ub(mit)?)?\s[RGBCMYKW\s]+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
			regexStage = Regex.Match(command, @"^stage\s\d+$"),
			regexRecover = Regex.Match(command, @"^recover$"),
			regexExitRecoverExhib = Regex.Match(command, @"^s(ub(mit)?)?$");

		if (regexRecover.Success)
        {
			if (!IsInSubmission())
			{
				yield return "sendtochat {0}, Slight Gibberish Twist #{1} is not ready to solve yet.";
				yield break;
			}
			else if (!recoverable)
            {
				yield return "sendtochat {0}, Slight Gibberish Twist #{1} can't recover yet.";
				yield break;
			}
			yield return null;
			arrowSelectables[0].OnInteract();
			arrowSelectables[0].OnInteractEnded();
		}
		else if (regexStage.Success)
        {
			if (IsInSubmission())
			{
				yield return "sendtochat {0}, Slight Gibberish Twist #{1} is ready to solve. This command is useless if the module is in this state.";
				yield break;
			}
			else if (!recoverable)
            {
				yield return "sendtochat {0}, Slight Gibberish Twist #{1} is preventing you from accessing the stages orderly... You must get all of the stages disorderly...";
				yield break;
			}
			var splitCmdLst = command.Split().Select(a => a.Trim().ToUpperInvariant()).Last();
			var nextStageIdx = -1;
			if (!int.TryParse(splitCmdLst, out nextStageIdx) || nextStageIdx < 1 && nextStageIdx > logicOper.Count)
            {
				yield return string.Format("sendtochaterror {0} does not correspond to a valid stage! That stage is either inaccessible or it doesn't exist.", splitCmdLst);
				yield break;
			}
			yield return null;
			var distanceFurtherUp = Mathf.Abs(stageIdx - nextStageIdx + 1) < Mathf.Abs(nextStageIdx + 1 - stageIdx + logicOper.Count);

			arrowSelectables[distanceFurtherUp ? 0 : 1].OnInteract();
			while (stageIdx + 1 != nextStageIdx)
            {
				yield return null;
            }
			arrowSelectables[distanceFurtherUp ? 0 : 1].OnInteractEnded();
			logicSelectables.PickRandom().OnInteract();
		}
		else if (regexExitRecoverExhib.Success)
        {
			if (IsInSubmission())
			{
				yield return "sendtochat {0}, Slight Gibberish Twist #{1} is ready to solve. This command is useless if the module is in this state.";
				yield break;
			}
			else if (!recoverable)
			{
				yield return "sendtochat {0}, Slight Gibberish Twist #{1} is preventing you from immediately entering submission. Solve enough modules to get there...";
				yield break;
			}
			yield return null;
			logicSelectables.PickRandom().OnInteract();
		}
		else if (regexSub.Success)
        {
			if (!IsInSubmission())
			{
				yield return "sendtochat {0}, Slight Gibberish Twist #{1} is not ready to solve yet.";
				yield break;
			}
			var splittedCmds = command.Split().Select(a => a.Trim().ToUpperInvariant());
			var idxesColors = new List<int>();
			for (var x = 1; x < splittedCmds.Count(); x++)
            {
				foreach (var AClr in splittedCmds.ElementAt(x))
                {
					var idx = clrAbrev.IndexOf(AClr);
					if (idx == -1)
                    {
						yield return string.Format("sendtochaterror {0} does not correspond to a correct color!", AClr);
						yield break;
                    }
					idxesColors.Add(idx);
                }
            }
			if (idxesColors.Count != squareLength * squareLength)
            {
				yield return string.Format("sendtochaterror You provided {0} color(s) when the module expected exactly {1}!", idxesColors.Count, squareLength * squareLength);
				yield break;
			}
			yield return null;
			if (xoring)
				logicSelectables.Last().OnInteract();
			for (var x = 0; x < 8; x++)
            {
				var filteredIdxColors = Enumerable.Range(0, squareLength * squareLength).Where(a => idxesColors[a] == x);
				if (filteredIdxColors.Any())
				{
					for (var p = 0; p < 3; p++)
					{
						if ((clrCur >> p) % 2 != (x >> p) % 2)
						{
							logicSelectables[p].OnInteract();
							yield return new WaitForSeconds(0.1f);
						}
					}
					foreach(var idxFiltered in filteredIdxColors)
                    {
						if (currentBoard[idxFiltered] != x)
						{
							grid[idxFiltered].OnInteract();
							yield return new WaitForSeconds(0.1f);
						}
					}
				}
            }
			for (var p = 0; p < logicSelectables.Length; p++)
			{
				if (p >= 3 || (clrCur >> p & 1) != 0)
				logicSelectables[p].OnInteract();
				yield return new WaitForSeconds(0.1f);
			}
			yield return "solve";
			yield return "strike";
		}
		else
        {
			var splittedCmds = command.Split().Select(a => a.Trim().ToUpperInvariant()).Where(a => !string.IsNullOrEmpty(a));
			var buttonsToPress = new List<KMSelectable>();
			foreach (var oneCmd in splittedCmds)
            {
				Match regexCoordinate = Regex.Match(oneCmd, @"^[ABCDEFGH][12345678]$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
				regexColor = Regex.Match(oneCmd, @"^([RGB]|X|[RGB]X)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

				if (regexCoordinate.Success)
                {
					var possibleCoordinate = regexCoordinate.Value;
					var rowIdxCmd = "ABCDEFGH".Substring(0, squareLength).IndexOf(possibleCoordinate[0]);
					var colIdxCmd = "12345678".Substring(0, squareLength).IndexOf(possibleCoordinate[1]);
					if (rowIdxCmd == -1 || colIdxCmd == -1)
                    {
						yield return string.Format("sendtochaterror {0} would be out of bounds in a {1}x{1} board! Stopping here.", possibleCoordinate, squareLength);
						yield break;
					}
					buttonsToPress.Add(grid[colIdxCmd * squareLength + rowIdxCmd]);
				}
				else if (regexColor.Success)
                {
					var possibleColor = regexColor.Value;

					if (possibleColor.Contains('X'))
						buttonsToPress.Add(logicSelectables[3]);
					var idxColor = "BGR".IndexOf(possibleColor.First());
					if (idxColor != -1)
						buttonsToPress.Add(logicSelectables[idxColor]);
				}
				else
                {
					yield return string.Format("sendtochaterror \"{0}\" was detected as an unknown command. Stopping here.", oneCmd);
					yield break;
				}
			}
			if (!IsInSubmission())
			{
				yield return "sendtochat {0}, Slight Gibberish Twist #{1} is not ready to solve yet. Not sure why you wanted to color a board now, but it's not a good time to do such a thing.";
				yield break;
			}
			yield return null;
			foreach (var btn in buttonsToPress)
            {
				btn.OnInteract();
				yield return new WaitForSeconds(0.1f);
            }
			if (xoring && clrCur == 0)
			{
				yield return "solve";
				yield return "strike";
			}
		}

		yield break;
    }
}