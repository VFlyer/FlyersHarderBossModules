using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KModkit;
using System.Linq;
using System;
using Random = UnityEngine.Random;
using System.Text.RegularExpressions;

public class ScrambledRulesCore : MonoBehaviour {

	public const string formatConditionNoArgs = "?XX!QQ,1AA", formatConditionNoArgsEx = "?XX!QQ,1AA,2BB", formatPlainAction = "1AA";
	public class UsedRuleSeed
    {
		MonoRandom randomizerUsed;
		string[] assignedModifiers;
		string inspectModifier;
		public UsedRuleSeed(int ruleSeedUsed)
        {
			randomizerUsed = new MonoRandom(ruleSeedUsed);
			assignedModifiers = new string[10];
			if (ruleSeedUsed == 1)
            {
				assignedModifiers = new string[] { "=D4", "?SM!O,-D1", "!P#", "/SNS", "^D2",
					"+D5", "*BT", "?D0!O,^D9,^D8", "-D7", "?SR!E,-D3,^D6", };
				inspectModifier = "?SN-1!E";
				return;
            }
        }

		public string GetStringedRule(int idx)
        {
			return assignedModifiers.ElementAtOrDefault(idx);
        }
		public string GetInspectRule()
        {
			return inspectModifier;
        }
		public int GetUsedSeed()
        {
			return randomizerUsed.Seed;
        }
		public string[] GetAllStringedRules()
        {
			return assignedModifiers;
        }
    }

	public KMRuleSeedable ruleseedModifier;
	public KMAudio mAudio;
	public KMBombInfo bombInfo;
	public KMBombModule modSelf;
	public KMSelectable shiftSelectable, toggleSelectable, clearSelectable, submitSelectable;
	public ButtonPushAnim shiftBtnAnim, toggleBtnAnim, clrBtnAnim, subBtnAnim;
	public TextMesh[] displayMeshes;
	int[] followedRuleSeeds = new int[10], displayedValues = new int[10];
	int currentInput, expectedInput, curDisplayValIdx, curDisplaySeedIdx;
	Dictionary<int, UsedRuleSeed> allUsedRuleSeeds;

	static int modIDCnt;
	int moduleID;

	bool pressedToggleButton, isUndefined, moduleSolved = false, activated, rememeberedConditionApplied;
	IEnumerator screenCycleAnim;
	void QuickLog(string toLog, params object[] args)
    {
		Debug.LogFormat("[{0} #{1}] {2}", modSelf.ModuleDisplayName, moduleID, string.Format(toLog, args));
    }
	void QuickLogDebug(string toLog, params object[] args)
    {
		Debug.LogFormat("<{0} #{1}> {2}", modSelf.ModuleDisplayName, moduleID, string.Format(toLog, args));
    }

    // Use this for initialization
    void Start()
    {
		
		moduleID = ++modIDCnt;
        shiftSelectable.OnInteract += delegate
        {
			shiftSelectable.AddInteractionPunch(0.5f);
			shiftBtnAnim.AnimatePush();
			mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, shiftSelectable.transform);
			if (!moduleSolved && activated)
            {
				HandleShift();
			}
            return false;
        };
        toggleSelectable.OnInteract += delegate
        {
			toggleSelectable.AddInteractionPunch(0.5f);
			toggleBtnAnim.AnimatePush();
			mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, toggleSelectable.transform);
			if (!moduleSolved && activated)
            {
				StopCoroutine(screenCycleAnim);
				pressedToggleButton = true;
				currentInput ^= 1;
				UpdateDisplays();
            }
			return false;
        };
		clearSelectable.OnInteract += delegate
		{
			clearSelectable.AddInteractionPunch(0.5f);
			clrBtnAnim.AnimatePush();
			mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, clearSelectable.transform);
			if (!moduleSolved && activated)
			{
				ClearInput();
			}
			return false;
		};
		submitSelectable.OnInteract += delegate
		{
			submitSelectable.AddInteractionPunch(0.5f);
			subBtnAnim.AnimatePush();
			mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, submitSelectable.transform);
			if (!moduleSolved && activated)
				CheckSubmission();
			return false;
		};
		for (var x = 0; x < displayMeshes.Length; x++)
		{
			displayMeshes[x].text = "";
		}
		modSelf.OnActivate += delegate {
			PrepModuleCalcs();
		};
	}
	void HandleShift()
    {
		StopCoroutine(screenCycleAnim);

		if (pressedToggleButton)
		{
			currentInput <<= 1;
			UpdateDisplays();
		}
		else
		{
			if (curDisplayValIdx < displayedValues.Length)
				displayMeshes[rememeberedConditionApplied ? 0 : 1].text = displayedValues.ElementAtOrDefault(curDisplayValIdx).ToString();
			else
			{
				displayMeshes[rememeberedConditionApplied ? 0 : 1].text = "";
				curDisplayValIdx = -1;
			}

			if (curDisplaySeedIdx < followedRuleSeeds.Length)
				displayMeshes[rememeberedConditionApplied ? 1 : 0].text = followedRuleSeeds.ElementAtOrDefault(curDisplaySeedIdx).ToString();
			else
			{
				displayMeshes[rememeberedConditionApplied ? 1 : 0].text = "";
				curDisplaySeedIdx = -1;
			}
			curDisplayValIdx++;
			curDisplaySeedIdx++;
		}
	}

	void CheckSubmission()
    {
		CalculatedExpectedInput();
		if (isUndefined)
		{
			QuickLog("The expected answer is UNDEFINED after recalculating the answer.");
			if (!pressedToggleButton)
			{
				QuickLog("You submitted the correct answer, module disarmed.");
				moduleSolved = true;
				modSelf.HandlePass();
			}
			else
			{
				QuickLog("You submitted {0} which is not correct!", currentInput);
				modSelf.HandleStrike();
				ClearInput();
			}
			return;
		}
		else
		{
			QuickLog("The expected answer is {0} after recalculating the answer.", expectedInput);
			if (!pressedToggleButton)
			{
				QuickLog("You submitted UNDEFINED which is not correct!");
				modSelf.HandleStrike();
				ClearInput();
			}
            else if (currentInput != expectedInput)
            {
                QuickLog("You submitted {0} which is not correct!", currentInput);
                modSelf.HandleStrike();
                ClearInput();
            }
            else
            {
                QuickLog("You submitted the correct answer. Module disarmed.", currentInput);
                moduleSolved = true;
                modSelf.HandlePass();
            }
        }
	}
	void ClearInput()
    {
		currentInput = 0;
		pressedToggleButton = false;
		StopCoroutine(screenCycleAnim);
		screenCycleAnim = ScreenCycleAnimation(rememeberedConditionApplied);
		StartCoroutine(screenCycleAnim);
	}
	void UpdateDisplays()
    {
		if (pressedToggleButton)
        {
            for (var x = 0; x < displayMeshes.Length; x++)
            {
				displayMeshes[x].text = currentInput.ToString();
            }
        }
    }
	public static readonly string[] possibleValuesCombination = { "SN0", "SN1", "SN-1", "TM", "D0", "D1", "D2", "D3", "D4", "D5", "D6", "D7", "D8", "D9", "P#", "PP#", "SM", "UM", "SR" };
	int GetValue(string combination)
    {
		var allCalculatedValues = new Dictionary<string, int> {
			{ "SN0", bombInfo.GetSerialNumberNumbers().FirstOrDefault() },
			{ "SN1", bombInfo.GetSerialNumberNumbers().ElementAtOrDefault(1) },
			{ "SN-1", bombInfo.GetSerialNumberNumbers().LastOrDefault() },
			{ "SNS", bombInfo.GetSerialNumberNumbers().Sum() },
			{ "TM", bombInfo.GetModuleIDs().Count() },
			{ "D0", displayedValues[0] },
			{ "D1", displayedValues[1] },
			{ "D2", displayedValues[2] },
			{ "D3", displayedValues[3] },
			{ "D4", displayedValues[4] },
			{ "D5", displayedValues[5] },
			{ "D6", displayedValues[6] },
			{ "D7", displayedValues[7] },
			{ "D8", displayedValues[8] },
			{ "D9", displayedValues[9] },
			{ "P#", bombInfo.GetPortCount() },
			{ "PL#", bombInfo.GetPortPlateCount() },
			{ "SM", bombInfo.GetSolvedModuleIDs().Count() },
			{ "UM", bombInfo.GetSolvableModuleIDs().Count() - bombInfo.GetSolvedModuleIDs().Count() },
			{ "SR", bombInfo.GetStrikes() },
			{ "ID", bombInfo.GetIndicators().Count() },
			{ "BT", bombInfo.GetBatteryCount() },
			{ "BH", bombInfo.GetBatteryHolderCount() },
			};
		return allCalculatedValues.ContainsKey(combination) ? allCalculatedValues[combination] : 0;
	}
	bool CheckCondition(string combination, int firstValue, params int[] otherValues)
    {
		switch (combination)
		{
			case "E":
				return firstValue % 2 == 0;
			case "O":
				return firstValue % 2 == 1;
			case "<=":
				return firstValue <= otherValues.First();
			case ">=":
				return firstValue >= otherValues.First();
			case "!=":
				return firstValue != otherValues.First();
			case "=":
				return firstValue == otherValues.First();
			case "P|":
				return firstValue % 2 == otherValues.First() % 2;
			case "?=":
				return otherValues.Contains(firstValue);
		}
		return false;
    }

	void CalculatedExpectedInput()
    {
		QuickLog("The solution in this state has the following stats:");
		QuickLog("Time submit button pressed: {0}", bombInfo.GetFormattedTime());
		QuickLog("Solve count: {0}", bombInfo.GetSolvedModuleIDs().Count());
		QuickLog("Strike count: {0}", bombInfo.GetStrikes());
		try
        {
			expectedInput = 0;
            for (var x = 0; x < followedRuleSeeds.Length; x++)
            {
				var curInstructionSet = allUsedRuleSeeds[followedRuleSeeds[x]].GetStringedRule(x);
				if (curInstructionSet.StartsWith("?"))
				{
					var conditionsAndActions = curInstructionSet.Split(',');
				}
				else
				{
					var remainingPortion = curInstructionSet.Substring(1);
					var obtainedValue = GetValue(remainingPortion);
					switch (curInstructionSet[0])
					{
						case '=':
							expectedInput = obtainedValue;
							break;
						case '+':
							expectedInput += obtainedValue;
							break;
						case '-':
							expectedInput -= obtainedValue;
							break;
						case '*':
							expectedInput *= obtainedValue;
							break;
						case '/':
							if (obtainedValue == 0)
                            {
								QuickLog("Value after applying instruction #{0} with ruleseed {1} returns UNDEFINED. Stopping here.", x, followedRuleSeeds[x]);
								isUndefined = true;
								return;
                            }
							expectedInput /= obtainedValue;
							break;
						case '^':
							expectedInput ^= obtainedValue;
							break;
						case '&':
							expectedInput &= obtainedValue;
							break;
						case '|':
							expectedInput |= obtainedValue;
							break;
						case '!':
							expectedInput ^= 1 << (obtainedValue % 32);
							break;

					}
				}
				QuickLog("Value after applying instruction #{0} with ruleseed {1}: {2}", x, followedRuleSeeds[x], expectedInput);
			}
		}
		catch (Exception anException)
        {
			isUndefined = true;
			QuickLog("The module has thrown an exception upon trying to calculate the final value. The actual answer is UNDEFINED.");
			QuickLog("Please be sure to send logfile for this result alongside the following error: {0}", anException.Message);
			Debug.LogException(anException);
		}
    }
	void PrepModuleCalcs()
    {
		for (var x = 0; x < displayedValues.Length; x++)
			displayedValues[x] = Random.Range(int.MinValue, int.MaxValue);
		for (var x = 0; x < followedRuleSeeds.Length; x++)
			followedRuleSeeds[x] = 1;
			//followedRuleSeeds[x] = Random.Range(int.MinValue, int.MaxValue);
		allUsedRuleSeeds = new Dictionary<int, UsedRuleSeed>();
		allUsedRuleSeeds.Add(ruleseedModifier.GetRNG().Seed, new UsedRuleSeed(ruleseedModifier.GetRNG().Seed));
        for (var x = 0; x < followedRuleSeeds.Length; x++)
        {
			var nextUsedRuleSeed = new UsedRuleSeed(followedRuleSeeds[x]);
			if (!allUsedRuleSeeds.Any(a => followedRuleSeeds[x] == a.Value.GetUsedSeed()))
			allUsedRuleSeeds.Add(followedRuleSeeds[x], nextUsedRuleSeed);
        }
		

		QuickLog("Ruleseed used to display the values and other rule seeds: {0}", ruleseedModifier.GetRNG().Seed);
		QuickLog("Displayed values: {0}", displayedValues.Join(", "));
		QuickLog("Other ruleseeds used to calculate answer: {0}", followedRuleSeeds.Join(", "));

		var conditionApplied = false;
		var inspectRule = allUsedRuleSeeds.First().Value.GetInspectRule().Replace("?", "").Split('!');
		conditionApplied = CheckCondition(inspectRule.Last(), GetValue(inspectRule.First()));
		QuickLogDebug("{0} {1} {2}", inspectRule.Join("!"), GetValue(inspectRule.First()), conditionApplied);
		rememeberedConditionApplied = conditionApplied;
		screenCycleAnim = ScreenCycleAnimation(rememeberedConditionApplied);

		activated = true;
		StartCoroutine(screenCycleAnim);

	}

	IEnumerator ScreenCycleAnimation(bool screenUseValsOnTop)
    {
		curDisplayValIdx = 0;
		curDisplaySeedIdx = 0;
		while (!moduleSolved)
        {
			if (curDisplayValIdx < displayedValues.Length)
				displayMeshes[screenUseValsOnTop ? 0 : 1].text = displayedValues.ElementAtOrDefault(curDisplayValIdx).ToString();
			else
            {
				displayMeshes[screenUseValsOnTop ? 0 : 1].text = "";
				curDisplayValIdx = -1;
			}

			if (curDisplaySeedIdx < followedRuleSeeds.Length)
				displayMeshes[screenUseValsOnTop ? 1 : 0].text = followedRuleSeeds.ElementAtOrDefault(curDisplaySeedIdx).ToString();
			else
            {
				displayMeshes[screenUseValsOnTop ? 1 : 0].text = "";
				curDisplaySeedIdx = -1;
			}

			yield return new WaitForSeconds(1.5f);
			curDisplayValIdx++;
			curDisplaySeedIdx++;
        }

    }
}
