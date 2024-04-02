using KModkit;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ObscureRace;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class ObscureRacesScript : MonoBehaviour {

	public KMBombModule modself;
	public KMBossModule bossHandler;
	public KMBombInfo bombInfo;
	public KMAudio mAudio;
	public KMSelectable[] namesSelectable;
	public KMSelectable flagSelectable;

	public TextMesh[] namesMesh;
	public TextMesh statusMesh;
	public Renderer flagDisplayer;
	public Texture[] possibleFlags;
	string[] ignoreModIDs;
	static int moduleIdCounter = 1;
	int moduleId;
	List<ORStage> obscureRaceStages;
	int maxStages, curStageIdx = -1, expectedStageLockIdx;
	const string base36Digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ", alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ", digits = "0123456789";
	bool moduleSolved, inSubmission, activated;
	public string[] racerAbbrevAll = new[] {
		"HAM", "BOT", "VER", "PER", "NOR",
		"RIC", "LEC", "SAI", "ALO", "OCO",
		"GAS", "TSU", "VET", "STR", "RUS",
		"RAI", "GIO", "MSC", "MAZ", "LAT" };
	string[] selectedRacerAbbrev;
	// Internal handlers to determine final positions.
	int[] msTimesTaken, curOrderIdx;
	Dictionary<int, List<int>> storedFinalPositions;
	List<int[]> allExpectedOrderIdxes;
	Stopwatch timer = new Stopwatch();

	void QuickLogDebug(string toLog, params object[] args)
    {
		Debug.LogFormat("<{0} #{1}> {2}", modself.ModuleDisplayName, moduleId, string.Format(toLog, args));
    }
	void QuickLog(string toLog, params object[] args)
    {
		Debug.LogFormat("[{0} #{1}] {2}", modself.ModuleDisplayName, moduleId, string.Format(toLog, args));
    }

	// Use this for initialization
	void Start () {
		moduleId = moduleIdCounter++;
		string[] detectedModIDs = bossHandler.GetIgnoredModuleIDs(modself);
		if (detectedModIDs != null && detectedModIDs.Any())
		{
			ignoreModIDs = detectedModIDs;
		}
		else
		{
			QuickLogDebug("Using default ignore list! This will cause issues when running with other boss modules present!");
			ignoreModIDs = DefaultIgnoreList.ignoreListIDs;
		}
		modself.OnActivate += () => { HandleActivation(); };
		maxStages = bombInfo.GetSolvableModuleNames().Count(a => !ignoreModIDs.Contains(a));
		GenerateAllStages();
		ProcessAllStages();
		for (var x = 0; x < namesMesh.Length; x++)
			namesMesh[x].text = "";
		statusMesh.text = "";
        for (var x = 0; x < namesSelectable.Length; x++)
        {
			var y = x;
			namesSelectable[x].OnInteract += delegate { HandleIdxRacerPress(y); return false; };
        }
	}
	void HandleIdxRacerPress(int idx)
    {
		namesSelectable[idx].AddInteractionPunch(0.5f);
		mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, namesSelectable[idx].transform);
		if (!inSubmission)
        {
			modself.HandleStrike();
			QuickLog("Not yet! Current rules do not allow swapping racers.");
			return;
        }
    }

	void GenerateAllStages()
    {
		obscureRaceStages = new List<ORStage>();
		storedFinalPositions = new Dictionary<int, List<int>>();
		selectedRacerAbbrev = racerAbbrevAll.ToArray().Shuffle().Take(5).ToArray();
		QuickLog("Maxmium laps possible: {0}", Mathf.Max(0, maxStages));
		var lapsToGenerate = Random.Range(maxStages / 2, maxStages - 1);
		var blackFlagUsed = false;
		var racerIdxesLeft = Enumerable.Range(0, 5).ToList();
        for (var x = 0; x < lapsToGenerate; x++)
        {
			var newStage = new ORStage();
			newStage.idxOrderFinished = Enumerable.Range(0, 5).ToArray().Shuffle();
			var deltasCreated = new int[5];
            for (var o = 0; o < 5; o++)
				deltasCreated[o] = Random.Range(0, 10000);
			newStage.deltasMS = deltasCreated;
			var gimmickFlagRules = new List<FlagRule> { FlagRule.Yellow, FlagRule.SafetyCar, FlagRule.Red, FlagRule.Retirement, FlagRule.Disqualification };
			if (blackFlagUsed) gimmickFlagRules.Remove(FlagRule.Disqualification);
			var pickedFlag = x + 1 >= lapsToGenerate ? FlagRule.Yellow : Random.value < 0.6f ? FlagRule.None : gimmickFlagRules.PickRandom();
			// Module has a 60% change of requiring normal stage gen, last stage will ALWAYS be yellow.
			while (racerIdxesLeft.Count <= 1 && (pickedFlag == FlagRule.Disqualification || pickedFlag == FlagRule.Retirement))
				pickedFlag = gimmickFlagRules.PickRandom();
			// Prevent exception of retiring/disqualifying the 0 racers.
			if (pickedFlag == FlagRule.Disqualification) blackFlagUsed = true;
			newStage.currentFlagRule = pickedFlag;
			switch (pickedFlag)
            {
				case FlagRule.Retirement:
				case FlagRule.Disqualification:
					{
						var pickedIdx = racerIdxesLeft.PickRandom();
							racerIdxesLeft.Remove(pickedIdx);
						newStage.extraArgFlagRule = pickedIdx;
					}
					break;
				case FlagRule.Red:
					{
						if (racerIdxesLeft.Count > 1 && Random.value < 0.5f)
							goto case FlagRule.Retirement;
					}
					break;
			}
			obscureRaceStages.Add(newStage);
		}
		QuickLog("Module generated with {0} lap(s) to complete.", lapsToGenerate);
	}
	void ProcessAllStages()
    {
		var finalLapIdx = obscureRaceStages.Count + 1;
		var serialNo5Chrs = bombInfo.GetSerialNumber().Take(5);
		var lastSNDigit = bombInfo.GetSerialNumberNumbers().Last();
		msTimesTaken = serialNo5Chrs.Select(a => base36Digits.IndexOf(a) * (lastSNDigit % 5 + 1) * 10).ToArray();
		QuickLog("Initial times for all racers: {0}", Enumerable.Range(0, 5).Select(a => string.Format("[{0}: {1}.{2}]", selectedRacerAbbrev[a], msTimesTaken[a] / 1000, (msTimesTaken[a] % 1000).ToString("000"))).Join("; "));
		var racerIdxesLeft = Enumerable.Range(0, 5).ToList();
		var lastFlagRed = false;
		for (var x = 0; x < obscureRaceStages.Count; x++)
        {
			var racersLeft = racerIdxesLeft.Count;
			if (racersLeft <= 1)
			{
				finalLapIdx = x - 1;
				QuickLog("Laps {0} and beyond will no longer be tracked as the last lap caused an action that left only 1 racer behind.", x + 1);
				break;
			}
			QuickLog("Lap {0}:", x + 1);
			var curLap = obscureRaceStages[x];
			if (lastFlagRed)
            {
				lastFlagRed = false;
				switch (curLap.currentFlagRule)
				{
					case FlagRule.None:
						QuickLog("Current Flag: Track Clear");
						goto default;
					case FlagRule.Retirement:
						{
							QuickLog("Current Flag: Retirement");
							var argIdxPicked = curLap.extraArgFlagRule;
							if (argIdxPicked != -1)
							{
								storedFinalPositions.Add(racersLeft, new List<int> { argIdxPicked });
								QuickLog("Action: Racer {0} is now out of the race, placed in position {1}", selectedRacerAbbrev[argIdxPicked], racersLeft);
								racerIdxesLeft.Remove(argIdxPicked);
							}
						}
						goto default;
					case FlagRule.Disqualification:
						{
							QuickLog("Current Flag: Disqualification");
							var argIdxPicked = curLap.extraArgFlagRule;
							var keysBeforeElim = storedFinalPositions.Keys;
							storedFinalPositions.Add(racersLeft, null);
							for (var y = racersLeft; y < 5; y++)
							{
								storedFinalPositions[y] = storedFinalPositions[y + 1];
							}
							storedFinalPositions[5] = new List<int> { argIdxPicked };
							QuickLog("Action: Racer {0} is now out of the race, placed in position 5.", selectedRacerAbbrev[argIdxPicked]);
							racerIdxesLeft.Remove(argIdxPicked);
						}
						goto default;
					case FlagRule.Yellow:
						QuickLog("Current Flag: Warning");
						goto default;
					case FlagRule.SafetyCar:
						QuickLog("Current Flag: Warning + Safety Car");
						QuickLog("Action: Last flag was red. Ignoring the times and action on this lap.");
						break;
					case FlagRule.Red:
						{
							QuickLog("Current Flag: Suspension");
							QuickLog("Action: Last flag was red. Ignoring the times on this lap, also applying red flag rule.");
							lastFlagRed = true;
							var argIdxPicked = curLap.extraArgFlagRule;
							if (argIdxPicked != -1)
							{
								storedFinalPositions.Add(racersLeft, new List<int> { argIdxPicked });
								QuickLog("Action (continued): Racer {0} is now out of the race, placed in position {1}.", selectedRacerAbbrev[argIdxPicked], racersLeft);
								racerIdxesLeft.Remove(argIdxPicked);
							}
						}
						break;
					default:
						{
							QuickLog("Action: Last flag was red. Ignoring the times on this lap.");
						}
						break;
				}
			}
			else
				switch (curLap.currentFlagRule)
				{
					case FlagRule.None:
						{
							QuickLog("Current Flag: Track Clear");
							QuickLog("Action: Tracking times of completed lap.");
							goto default;
						}
					case FlagRule.Retirement:
						{
							QuickLog("Current Flag: Retirement");
							var argIdxPicked = curLap.extraArgFlagRule;
							storedFinalPositions.Add(racersLeft, new List<int> { argIdxPicked });
							QuickLog("Action: Racer {0} is now out of the race, placed in position {1}", selectedRacerAbbrev[argIdxPicked], racersLeft);
							racerIdxesLeft.Remove(argIdxPicked);
						}
						goto default;
					case FlagRule.Disqualification:
						{
							QuickLog("Current Flag: Disqualification");
							var argIdxPicked = curLap.extraArgFlagRule;
							var keysBeforeElim = storedFinalPositions.Keys;
							storedFinalPositions.Add(racersLeft, null);
							for (var y = racersLeft; y < 5; y++)
                            {
								storedFinalPositions[y] = storedFinalPositions[y + 1];
                            }
							storedFinalPositions[5] = new List<int> { argIdxPicked };
							QuickLog("Action: Racer {0} is now out of the race, placed in position 5.", selectedRacerAbbrev[argIdxPicked]);
							racerIdxesLeft.Remove(argIdxPicked);
						}
						goto default;
					case FlagRule.Red:
						{
							QuickLog("Current Flag: Suspension");
							QuickLog("Action: Ignoring the times on this lap, also applying red flag rule.");
							lastFlagRed = true;
							var argIdxPicked = curLap.extraArgFlagRule;
							if (argIdxPicked != -1)
							{
								storedFinalPositions.Add(racersLeft, new List<int> { argIdxPicked });
								QuickLog("Action (continued): Racer {0} is now out of the race, placed in position {1}.", selectedRacerAbbrev[argIdxPicked], racersLeft);
								racerIdxesLeft.Remove(argIdxPicked);
							}
						}
						break;
					case FlagRule.Yellow:
						QuickLog("Current Flag: Warning");
						QuickLog("Action: Ignoring times of completed lap.");
						break;
					case FlagRule.SafetyCar:
						QuickLog("Current Flag: Warning + Safety Car");
						QuickLog("Action: Ignoring times of completed lap, also setting deltas of remaining racers to 2.000 sec.");
						var currentPosOrderedIdxes = racerIdxesLeft.OrderBy(a => msTimesTaken[a]).ToArray();
                        for (var y = 0; y < currentPosOrderedIdxes.Count(); y++)
							msTimesTaken[currentPosOrderedIdxes.ElementAt(y)] = 2000 * y;
						QuickLog("Current times for all racers after current lap: {0}", Enumerable.Range(0, 5).Select(a => racerIdxesLeft.Contains(a) ? string.Format("[{0}: {1}.{2}]", selectedRacerAbbrev[a], msTimesTaken[a] / 1000, (msTimesTaken[a] % 1000).ToString("000")) : string.Format("[{0}: OUT]", selectedRacerAbbrev[a])).Join("; "));
						break;
					default:
						{
							QuickLog("Times of each racer's completed lap: {0}", Enumerable.Range(0, 5).Select(a => string.Format("[{0}: {1}.{2}]", selectedRacerAbbrev[curLap.idxOrderFinished[a]], curLap.deltasMS.Take(a + 1).Sum() / 1000, (curLap.deltasMS.Take(a + 1).Sum() % 1000).ToString("000"))).Join("; "));
							foreach (var y in racerIdxesLeft)
                            {
								var idxCurRacer = Enumerable.Range(0, 5).Single(a => curLap.idxOrderFinished[a] == y);
								msTimesTaken[y] += curLap.deltasMS.Take(idxCurRacer + 1).Sum();
                            }
							QuickLog("Current times for all racers after current lap: {0}", Enumerable.Range(0, 5).Select(a => racerIdxesLeft.Contains(a) ? string.Format("[{0}: {1}.{2}]", selectedRacerAbbrev[a], msTimesTaken[a] / 1000, (msTimesTaken[a] % 1000).ToString("000")) : string.Format("[{0}: OUT]", selectedRacerAbbrev[a])).Join("; "));
						}
						break;
				}
		}
		expectedStageLockIdx = finalLapIdx;
		QuickLog("Final times for all remaining racers: {0}", Enumerable.Range(0, 5).Select(a => racerIdxesLeft.Contains(a) ? string.Format("[{0}: {1}.{2}]", selectedRacerAbbrev[a], msTimesTaken[a] / 1000, (msTimesTaken[a] % 1000).ToString("000")) : string.Format("[{0}: OUT]", selectedRacerAbbrev[a])).Join("; "));
		while (racerIdxesLeft.Any())
        {
			var finalTimesSlowest = racerIdxesLeft.Select(a => msTimesTaken[a]).Max();
			var racersIdxMatchingSlowTimes = racerIdxesLeft.Where(a => msTimesTaken[a] >= finalTimesSlowest);
			QuickLogDebug("{0}: {1}", racerIdxesLeft.Count, racersIdxMatchingSlowTimes.Join());
			storedFinalPositions.Add(racerIdxesLeft.Count, racersIdxMatchingSlowTimes.ToList());
			racerIdxesLeft.RemoveAll(a => racersIdxMatchingSlowTimes.Contains(a));
        }
		var finalCombos = new List<int[]> { new int[0] };
		foreach (var position in storedFinalPositions.Keys.OrderBy(a => a))
        {
			var idxesInCurPos = storedFinalPositions[position];
			var combinations = GeneratePermutations(idxesInCurPos, idxesInCurPos.Count);
			var newOutput = new List<int[]>();
			foreach (var curCombo in finalCombos)
				foreach (var candidate in combinations)
					newOutput.Add(curCombo.Concat(candidate).ToArray());
			finalCombos = newOutput;
		}
		QuickLog("Allowed finishing racer orders: [{0}]", finalCombos.Select(a => a.Select(b => selectedRacerAbbrev[b]).Join(", ")).Join("]; ["));
	}
	List<int[]> GeneratePermutations(IEnumerable<int> possibleValues, int countIterations = 1)
    {
		var output = new List<int[]> { new int[0] };
		for (var x = 0; x < countIterations; x++)
        {
			var newOutput = new List<int[]>();
			foreach (var curCombo in output)
            {
				var remainingCandidates = possibleValues.Where(a => !curCombo.Contains(a));
				foreach (var candidate in remainingCandidates)
					newOutput.Add(curCombo.Concat(new[] { candidate }).ToArray());
            }
			output = newOutput;
		}
		return output;
    }
	void HandleRevealStageIdx(int idx)
    {
		var timePassedSeconds = timer.Elapsed.Seconds;
		var timePassedMinutes = (int)timer.Elapsed.TotalMinutes;
		var curStage = obscureRaceStages.ElementAtOrDefault(idx);
		var deltasAllCurStage = curStage.deltasMS;
		var combinedTimesAll = Enumerable.Range(0, 5).Select(a => deltasAllCurStage.Take(a + 1).Sum());
		Debug.Log(combinedTimesAll.Join());
		var orderFinishedLap = curStage.idxOrderFinished;
		StartCoroutine(AnimateChangeText(statusMesh, string.Format("LAP\n{0}/{1}", idx + 1, obscureRaceStages.Count + 1), 2f));
		for (int i = 0; i < orderFinishedLap.Length; i++)
        {
            int racerIdx = orderFinishedLap[i];
            var y = racerIdx;
			var offsettedTimeMS = combinedTimesAll.ElementAt(i) + timePassedSeconds * 1000;
			
			StartCoroutine(AnimateChangeText(namesMesh[y],
				string.Format("???\n{2}:{1}.{0}", (offsettedTimeMS % 1000).ToString("000"), (offsettedTimeMS / 1000 % 60).ToString("00"), timePassedMinutes + (offsettedTimeMS / 60000), 2f)));
		}
		timer.Reset();
		timer.Start();
    }
	void HandleFinalLapDisplay()
    {
		timer.Stop();
		StartCoroutine(AnimateChangeText(statusMesh, "FINAL\nLAP", 2f));
		for (var x = 0; x < namesMesh.Length; x++)
		{
			var y = x;
			StartCoroutine(AnimateChangeText(namesMesh[y], selectedRacerAbbrev[x] + "\n?:??.???", 2f));
		}
	}
	void HandleActivation()
    {
		StartCoroutine(AnimateRevealText(statusMesh, "GET\nREADY", 2f));
		for (var x = 0; x < namesMesh.Length; x++)
        {
			var y = x;
			StartCoroutine(AnimateRevealText(namesMesh[y], selectedRacerAbbrev[x] + "\n?:??.???", 2f));
        }
		timer.Start();
		activated = true;
	}
	IEnumerator AnimateChangeText(TextMesh affectedText, string newText = "", float delay = 2.5f)
    {
		if (delay <= 0f)
        {
			affectedText.text = newText;
			yield break;
        }
        for (float x = 0; affectedText.text.Any() && x < 1; x += Time.deltaTime / delay * 2)
        {
			var deltaOffset = Mathf.FloorToInt(30 * x);
			affectedText.text = affectedText.text.Select(a => digits.Contains(a) ? digits[(digits.IndexOf(a) + deltaOffset) % 10] : alphabet.Contains(a) ? alphabet[(alphabet.IndexOf(a) + deltaOffset) % 26] : a).Join("");
			affectedText.color = Color.Lerp(Color.white, Color.clear, x);
			yield return null;
        }
        for (float x = 0; newText.Any() && x < 1; x += Time.deltaTime / delay * 2)
        {
			var deltaOffset = Mathf.FloorToInt(30 * x);
			affectedText.text = newText.Select(a => digits.Contains(a) ? digits[(digits.IndexOf(a) + deltaOffset) % 10] : alphabet.Contains(a) ? alphabet[(alphabet.IndexOf(a) + deltaOffset) % 26] : a).Join("");
			affectedText.color = Color.Lerp(Color.clear, Color.white, x);
			yield return null;
        }
		affectedText.text = newText;
		affectedText.color = Color.white;
	}
	IEnumerator AnimateRevealText(TextMesh affectedText, string newText = "", float delay = 2.5f)
    {
		if (delay <= 0f)
        {
			affectedText.text = newText;
			yield break;
        }
        for (float x = 0; newText.Any() && x < 1; x += Time.deltaTime / delay)
        {
			var deltaOffset = Mathf.FloorToInt(30 * x);
			affectedText.text = newText.Select(a => digits.Contains(a) ? digits[(digits.IndexOf(a) + deltaOffset) % 10] : alphabet.Contains(a) ? alphabet[(alphabet.IndexOf(a) + deltaOffset) % 26] : a).Join("");
			affectedText.color = Color.Lerp(Color.clear, Color.white, x);
			yield return null;
        }
		affectedText.text = newText;
		affectedText.color = Color.white;
	}

	// Update is called once per frame
	void Update () {
		if (!activated || moduleSolved) return;
		var nonIgnoredSolves = bombInfo.GetSolvedModuleIDs().Count(a => !ignoreModIDs.Contains(a));
		if (nonIgnoredSolves > curStageIdx + 1 && !inSubmission && timer.ElapsedMilliseconds >= 5000)
        {
			curStageIdx++;
			if (curStageIdx >= obscureRaceStages.Count)
            {
				inSubmission = true;
				QuickLog("All laps have been displayed.");
				HandleFinalLapDisplay();
			}
			else
				HandleRevealStageIdx(curStageIdx);
			if (expectedStageLockIdx < curStageIdx + 1)
            {
				QuickLog("Advanced a stage with only 1 racer left. Striking.");
				modself.HandleStrike();
            }
			
        }
	}
}
