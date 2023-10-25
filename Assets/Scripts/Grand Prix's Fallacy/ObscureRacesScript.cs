using KModkit;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ObscureRace;

public class ObscureRacesScript : MonoBehaviour {

	public KMBombModule modself;
	public KMBossModule bossHandler;
	public KMBombInfo bombInfo;

	public TextMesh[] namesMesh;
	public TextMesh statusMesh;
	public Renderer flagDisplayer;
	string[] ignoreModNames;
	static int moduleIdCounter = 1;
	int moduleId;
	List<ORStage> obscureRaceStages;
	int maxStages, curStageIdx, expectedStageLockIdx;
	const string base36Digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
	bool moduleSolved, inSubmission, activated;
	public string[] racerAbbrevAll = new[] {
		"HAM", "BOT", "VER", "PER", "NOR",
		"RIC", "LEC", "SAI", "ALO", "OCO",
		"GAS", "TSU", "VET", "STR", "RUS",
		"RAI", "GIO", "MSC", "MAZ", "LAT" };
	string[] selectedRacerAbbrev;
	// Internal handlers to determine final positions.
	List<int> remainingRacerIdxes = Enumerable.Range(0, 5).ToList();
	int[] msTimesTaken, curOrderIdx;
	Dictionary<int, List<int>> storedFinalPositions;
	List<int[]> allExpectedOrderIdxes;

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
		string[] detectedModNames = bossHandler.GetIgnoredModules(modself);
		if (detectedModNames != null && detectedModNames.Any())
		{
			ignoreModNames = detectedModNames;
		}
		else
		{
			QuickLogDebug("Using default ignore list! This will cause issues when running with other boss modules present!");
			ignoreModNames = DefaultIgnoreList.ignoreListNames;
		}
		modself.OnActivate += () => { activated = true; };
		maxStages = bombInfo.GetSolvableModuleNames().Count(a => !ignoreModNames.Contains(a));
		GenerateAllStages();
		ProcessAllStages();

	}
	void GenerateAllStages()
    {
		obscureRaceStages = new List<ORStage>();
		storedFinalPositions = new Dictionary<int, List<int>>();
		selectedRacerAbbrev = racerAbbrevAll.ToArray().Shuffle().Take(5).ToArray();
		QuickLog("Maxmium laps possible: {0}", Mathf.Max(0, maxStages));
		var lapsToGenerate = Random.Range(maxStages / 2, maxStages);
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
			var pickedFlag = x + 1 >= lapsToGenerate || Random.value < 0.6f ? FlagRule.None : gimmickFlagRules.PickRandom();
			// Module has a 60% change of requiring normal stage gen.
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
			
        }
		QuickLog("Module generated with {0} lap(s) to complete.", lapsToGenerate);
	}
	void ProcessAllStages()
    {
		var finalLapIdx = obscureRaceStages.Count;
		var serialNo5Chrs = bombInfo.GetSerialNumber().Take(5);
		var lastSNDigit = bombInfo.GetSerialNumberNumbers().Last();
		msTimesTaken = serialNo5Chrs.Select(a => base36Digits.IndexOf(a) * (lastSNDigit % 5 + 1) * 10).ToArray();
		QuickLog("Initial times for all racers: {0}", Enumerable.Range(0, 5).Select(a => string.Format("[{0}: {1}.{2}]", selectedRacerAbbrev[a], msTimesTaken[a] / 1000, (msTimesTaken[a] % 1000).ToString("000"))).Join("; "));
		var racerIdxesLeft = Enumerable.Range(0, 5).ToList();
		var lastFlagRed = false;
		for (var x = 0; x < finalLapIdx; x++)
        {
			QuickLog("Lap {0}:", x + 1);
			var curLap = obscureRaceStages[x];
			switch(curLap.currentFlagRule)
            {
				case FlagRule.None:
                    {
						if (lastFlagRed)
						{
							QuickLog("Current Flag: Track Clear. However, last flag was red. Ignoring the times on this lap.");
							break;
						}
						QuickLog("Current Flag: Track Clear. Tracking times of completed lap.");
						goto default;
					}
				case FlagRule.Retirement:
                    {

                    }
					break;
				case FlagRule.Yellow:
					QuickLog("Current Flag: Warning. Ignoring times of completed lap.");
					break;
				default:
                    {

                    }
					break;
            }
			

		}
	}

	// Update is called once per frame
	void Update () {
		if (!activated) return;
	}
}
