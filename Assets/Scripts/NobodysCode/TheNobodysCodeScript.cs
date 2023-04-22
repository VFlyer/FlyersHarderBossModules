using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using KModkit;
using System.Text.RegularExpressions;

public class TheNobodysCodeScript : MonoBehaviour {

	public KMBombModule modSelf;
	public KMAudio mAudio;
	public KMBombInfo bombInfo;
	public KMSelectable[] keyNumSelectables;
	public KMSelectable submitSelectable, clrSelectable;
	public KMBossModuleExtensions bossHandler;
	public TextMesh stageText, digitsText;

	string[] ignoreModuleIDsList;
	List<string> allStageDisplays;
	bool interactable = false, recovering, readyToSolve, solveOnPressAny, started, autosolveRequest;

	static int modIDCnt;
	int moduleID;
	int curStageIdx = -1, curCycleIdx, curResetCnt;
	string expectedAnswer, inputtedAnswer = "";
	int[] requiredStageIdxes;

	const int stageLimit = 99;
	const float startCooldown = 5f;
	const string digitString = "0123456789";
	const string evenNums = "02468";
	float curCooldown = 0f;
	IEnumerator curFlickerAnim;
	void QuickLog(string value, params object[] args)
	{
		Debug.LogFormat("[The Nobody’s Code #{0}]: {1}", moduleID, string.Format(value, args));
	}
	void QuickLogDebug(string value, params object[] args)
	{
		Debug.LogFormat("<The Nobody’s Code #{0}>: {1}", moduleID, string.Format(value, args));
	}

	// Use this for initialization
	void Start () {
		moduleID = ++modIDCnt;
		var obtainedIgnoreList = bossHandler.GetAttachedIgnoredModuleIDs(modSelf);
		if (obtainedIgnoreList == null || !obtainedIgnoreList.Any())
		{
			QuickLogDebug("Using default ignore IDs! This is due to the result of Boss Module Manager being disabled or not present.");
			ignoreModuleIDsList = DefaultIgnoreList.ignoreListIDs;
		}
		else
			ignoreModuleIDsList = obtainedIgnoreList;
		submitSelectable.OnInteract += delegate {
			if (interactable)
            {
				submitSelectable.AddInteractionPunch(0.5f);
				mAudio.PlaySoundAtTransform("Button Press Alarm", submitSelectable.transform);
				HandleSubBtnPress();
			}
			return false;
		};
		clrSelectable.OnInteract += delegate {
			if (interactable)
            {
				clrSelectable.AddInteractionPunch(0.5f);
				mAudio.PlaySoundAtTransform("Button Press Alarm", clrSelectable.transform);
				if (readyToSolve)
					mAudio.PlaySoundAtTransform("Activation Sound Trimmed Alarm", clrSelectable.transform);
				HandleClrBtnPress();
            }
			return false;
		};
        for (var x = 0; x < keyNumSelectables.Length; x++)
        {
			var y = x;
			keyNumSelectables[x].OnInteract += delegate {
				if (interactable)
				{
					keyNumSelectables[y].AddInteractionPunch(0.5f);
					mAudio.PlaySoundAtTransform("Button Press Alarm", keyNumSelectables[y].transform);
					HandleKeyNumPress(y);
				}
				return false;
			};
		}

		modSelf.OnActivate += delegate {
			GenerateStages();
			
			CalculateSolution();
			QuickLog("------------------------------------ Actions Performed ------------------------------------");
		};
		digitsText.text = "";
		stageText.text = "";
	}
	void HandleKeyNumPress(int idx)
    {
		if (solveOnPressAny)
		{
			SolveModule();
			return;
		}
		//if (inputtedAnswer.Length < 8)
			inputtedAnswer += idx.ToString();
		//else
		//	inputtedAnswer = inputtedAnswer.Substring(1) + idx.ToString();
    }
	void HandleSubBtnPress()
    {
		if (solveOnPressAny)
		{
			SolveModule();
			return;
		}
		

		if (inputtedAnswer.Length < 8 && inputtedAnswer.Length > 0 && !inputtedAnswer.StartsWith("0"))
        {
			QuickLog("Attempting code check...");
			if (!recovering)
            {
				QuickLog("Module was not cycling through stages. Causing strike in addition...");
				modSelf.HandleStrike();
            }
			int possibleStageIdx;
			if (int.TryParse(inputtedAnswer, out possibleStageIdx) && possibleStageIdx >= 1 && possibleStageIdx <= curStageIdx)
            {
				QuickLog("Detected valid stage: {0}, displaying said stage.", possibleStageIdx);
				curCycleIdx = possibleStageIdx - 1;
				if (curFlickerAnim != null)
					StopCoroutine(curFlickerAnim);
				curFlickerAnim = HandleStageCycleAnim(false);
				StartCoroutine(curFlickerAnim);
			}
			else
            {
				QuickLog("Detected invalid stage: {0}.", inputtedAnswer);
			}
        }
		else if (readyToSolve)
		{
			if (inputtedAnswer.SequenceEqual(expectedAnswer) || inputtedAnswer == expectedAnswer)
            {
				QuickLog("Submitted the correct number.");
				SolveModule();
            }
			else
            {
				if (inputtedAnswer.Length == 8)
					QuickLog("Submitted the incorrect 8-digit number: {0}", inputtedAnswer);
				else
					QuickLog("I don't think this number works for solving... ({1} digit{2} entered): {0}", inputtedAnswer, inputtedAnswer.Length, inputtedAnswer.Length == 1 ? "" : "s");
				mAudio.PlaySoundAtTransform("", transform);
				modSelf.HandleStrike();
				recovering = true;
				readyToSolve = false;
				if (curFlickerAnim != null)
					StopCoroutine(curFlickerAnim);
				curFlickerAnim = HandleStageCycleAnim();
				StartCoroutine(curFlickerAnim);
			}
		}
		else if (recovering)
        {
			recovering = false;
			readyToSolve = true;
			QuickLog("Reactivating submission...");
			CalculateSolution(true);
            curCycleIdx = 0;
			if (curFlickerAnim != null)
				StopCoroutine(curFlickerAnim);
			curFlickerAnim = HandleTransitionToSubmitAnim();
			StartCoroutine(curFlickerAnim);
		}
		inputtedAnswer = "";
	}
	void HandleClrBtnPress()
    {
		if (solveOnPressAny)
		{
			SolveModule();
			return;
		}
		inputtedAnswer = "";
		if (readyToSolve)
        {
			curCycleIdx = (curCycleIdx + 1) % requiredStageIdxes.Length;
			if (curFlickerAnim != null)
				StopCoroutine(curFlickerAnim);
			curFlickerAnim = HandleFlickerSubmitAnim();
			StartCoroutine(curFlickerAnim);
        }
	}

	void SolveModule()
    {
		StopAllCoroutines();
		QuickLog("Module disarmed.");
		mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
		modSelf.HandlePass();
		digitsText.text = "";
		stageText.text = "";
		interactable = false;
    }
	IEnumerator HandleStageAnim(int stageIdx)
    {
		var txtToDisplay = allStageDisplays.ElementAtOrDefault(stageIdx);
		if (txtToDisplay != null)
        {
			for (var x = 0; x < 5; x++)
			{
				digitsText.text = Random.Range(0, 100000000).ToString("00000000");
				stageText.text = Random.Range(0, 100).ToString("00");
				yield return new WaitForSeconds(0.1f);
			}
			digitsText.text = string.IsNullOrEmpty(allStageDisplays.ElementAtOrDefault(stageIdx)) ? "ERRORERR" : allStageDisplays.ElementAtOrDefault(stageIdx);
			stageText.text = ((stageIdx + 1) % 100).ToString("00");
		}

		yield break;
    }
	IEnumerator HandleTransitionToSubmitAnim()
    {
		for (var x = 0; x < 5; x++)
		{
			digitsText.text = Random.Range(0, 100000000).ToString("00000000");
			stageText.text = Random.Range(0, 100).ToString("00");
			yield return new WaitForSeconds(0.1f);
		}
		digitsText.text = requiredStageIdxes == null ? "B1GERROR" : string.Format("5TAGE-{0}", (requiredStageIdxes[curCycleIdx] + 1).ToString("00"));
		stageText.text = "--";
		yield break;
    }
	IEnumerator HandleFlickerSubmitAnim()
    {
		digitsText.text = "";
		yield return new WaitForSeconds(0.2f);
		digitsText.text = requiredStageIdxes == null ? "B1GERROR" : string.Format("{1}5TAGE-{0}{1}", (requiredStageIdxes[curCycleIdx] + 1).ToString("00"), curCycleIdx + 1 >= requiredStageIdxes.Length ? "." : "");
		yield break;
    }
	IEnumerator HandleStageCycleAnim(bool resetCurCycleIndex = true)
	{
		if (resetCurCycleIndex)
			curCycleIdx = 0;
		do
		{
			var txtToDisplay = allStageDisplays.ElementAtOrDefault(curCycleIdx);
			if (txtToDisplay != null)
			{
				for (var x = 0; x < 5; x++)
				{
					digitsText.text = Random.Range(0, 100000000).ToString("00000000");
					stageText.text = Random.Range(0, 100).ToString("00");
					yield return new WaitForSeconds(0.1f);
				}
				digitsText.text = string.IsNullOrEmpty(allStageDisplays.ElementAtOrDefault(curCycleIdx)) ? "ERRORERR" : allStageDisplays.ElementAtOrDefault(curCycleIdx);
				stageText.text = ((curCycleIdx + 1) % 100).ToString("00");
			}
			yield return new WaitForSeconds(5f);
			curCycleIdx = (curCycleIdx + 1) % allStageDisplays.Count;
		}
		while (recovering);
	}

	void GenerateStages()
	{
		var nonIgnoredModCount = bombInfo.GetSolvableModuleIDs().Count(a => !ignoreModuleIDsList.Contains(a));
		if (nonIgnoredModCount > stageLimit)
		{
			nonIgnoredModCount = stageLimit;
			QuickLog("Too many non-ignored modules. Capping to {0} stage(s).", stageLimit);
		}
		else
			QuickLog("Detected this many non-ignored modules: {0}", nonIgnoredModCount);
		allStageDisplays = new List<string>();
		for (var x = 0; x < nonIgnoredModCount; x++)
		{
			var numGenerated = Random.Range(0, 100000000).ToString("00000000");
			QuickLog("Stage {0}: {1}", x + 1, numGenerated);
			allStageDisplays.Add(numGenerated);
		}
		interactable = true;
		started = true;
    }
	void CalculateSolution(bool retrying = false)
    {
		QuickLog("------------------------------------ {0} ------------------------------------", retrying ? string.Format("Reattempt #{0}", ++curResetCnt) : "Solving");
		if (allStageDisplays.Count == 0)
        {
			QuickLog("Oh no stages, huh? I guess just press any button to solve this module. Nobody would ask for that anyway, would they?");
			solveOnPressAny = true;
			return;
		}
		requiredStageIdxes = new int[5];
		var randomPickedStagesAfterStage1 = Enumerable.Range(1, allStageDisplays.Count - 1).ToArray().Shuffle();
        for (var x = 0; x < 5; x++)
        {
			requiredStageIdxes[x] = x >= randomPickedStagesAfterStage1.Length ?
				randomPickedStagesAfterStage1.Any() ? randomPickedStagesAfterStage1.PickRandom() : 0
				: randomPickedStagesAfterStage1[x];
        }
		QuickLog("Assigned stages to numbers 1 - 5 in this order: {0}", requiredStageIdxes.Select(a => a + 1).Join(", "));

		
		var serialNum = bombInfo.GetSerialNumber();
		var lastDigitSerialEven = bombInfo.GetSerialNumberNumbers().LastOrDefault() % 2 == 0;
		var relevantNumbers = requiredStageIdxes.Select(a => allStageDisplays[a]);
		// Relevant numbers correspond to the numbers that were displayed in those stages.
		var convertedValues = allStageDisplays.First().Select(a => digitString.IndexOf(a)).ToArray();
		QuickLog("Digit 1 Modification:"); // Start of Digit 1 Conditions
		if (relevantNumbers.ElementAt(0).Contains(serialNum[2]) && relevantNumbers.ElementAt(0).Contains(serialNum[5]))
        {
			QuickLog("[NUMBER 1] has both 3rd and 6th characters in the serial number.");
			if (relevantNumbers.ElementAt(1).Contains('3'))
            {
				QuickLog("[NUMBER 2] has a 3. Modifier for first digit: {0}", lastDigitSerialEven ? 9 : 7);
				convertedValues[0] += lastDigitSerialEven ? 9 : 7;
			}
			else
            {
				QuickLog("[NUMBER 2] does not have a 3. Modifier for first digit: {0}", lastDigitSerialEven ? 1 : 4);
				convertedValues[0] += lastDigitSerialEven ? 1 : 4;
			}
		}
		else
        {
			QuickLog("[NUMBER 1] does not have both 3rd and 6th characters in the serial number.");
			if (relevantNumbers.ElementAt(2).Distinct().Count() != 8)
			{
				QuickLog("[NUMBER 3] contains duplicate digits. Modifier for first digit: {0}", lastDigitSerialEven ? 3 : 6);
				convertedValues[0] += lastDigitSerialEven ? 3 : 6;
			}
			else
			{
				QuickLog("[NUMBER 3] contains distinct digits. Modifier for first digit: {0}", lastDigitSerialEven ? 4 : 3);
				convertedValues[0] += lastDigitSerialEven ? 4 : 3;
			}
		}
		QuickLog("Digit 2 Modification:"); // Start of Digit 2 Conditions
		if (!relevantNumbers.ElementAt(3).Contains('9'))
		{
			QuickLog("[NUMBER 4] does not contain a 9.");
			if (relevantNumbers.ElementAt(0).Contains('0'))
            {
				QuickLog("[NUMBER 1] contains a 0. Modifier for second digit: {0}", 0);
				//convertedValues[1] += 0;
			}
			else
            {
				QuickLog("[NUMBER 1] does not contain a 0. Modifier for second digit: {0}", lastDigitSerialEven ? 6 : 1);
				convertedValues[1] += lastDigitSerialEven ? 6 : 1;
			}
		}
		else
        {
			QuickLog("[NUMBER 4] contains a 9.");
			if (!relevantNumbers.ElementAt(1).Contains('2'))
			{
				QuickLog("[NUMBER 2] does not contain a 2. Modifier for second digit: {0}", lastDigitSerialEven ? 5 : 8);
				convertedValues[1] += lastDigitSerialEven ? 5 : 8;
			}
			else
			{
				QuickLog("[NUMBER 2] contains a 2. Modifier for second digit: {0}", relevantNumbers.ElementAt(1).Last());
				convertedValues[1] += digitString.IndexOf(relevantNumbers.ElementAt(1).Last());
			}
		}
		QuickLog("Digit 3 Modification:"); // Start of Digit 3 Conditions
		if (relevantNumbers.ElementAt(2).Contains('9'))
		{
			QuickLog("[NUMBER 3] contains a 9.");
			if (evenNums.Contains(relevantNumbers.ElementAt(4).Last()))
			{
				QuickLog("The last digit of [NUMBER 5] is even. Modifier for third digit: {0}", lastDigitSerialEven ? 8 : 5);
				convertedValues[2] += lastDigitSerialEven ? 8 : 5;
			}
			else
			{
				QuickLog("The last digit of [NUMBER 5] is not even. Modifier for third digit: {0}", lastDigitSerialEven ? 4 : 9);
				convertedValues[2] += lastDigitSerialEven ? 4 : 9;
			}
		}
		else
        {
			QuickLog("[NUMBER 3] does not contain a 9.");
			if (!evenNums.Contains(relevantNumbers.ElementAt(3).First()))
			{
				QuickLog("The first digit of [NUMBER 4] is odd. Modifier for third digit: {0}", lastDigitSerialEven ? 9 : 3);
				convertedValues[2] += lastDigitSerialEven ? 9 : 3;
			}
			else
			{
				QuickLog("The first digit of [NUMBER 4] is not odd. Modifier for third digit: {0}", lastDigitSerialEven ? 3 : 7);
				convertedValues[2] += lastDigitSerialEven ? 3 : 7;
			}
		}
		QuickLog("Digit 4 Modification:"); // Start of Digit 4 Conditions
		if (relevantNumbers.ElementAt(1).Contains('7'))
        {
			QuickLog("[NUMBER 2] does contains a 7.");
			if (lastDigitSerialEven && evenNums.Contains(relevantNumbers.ElementAt(2).Last()))
            {
				QuickLog("Both the last digit of the serial number and [NUMBER 3] are even. Modifier for fourth digit: {0}", lastDigitSerialEven ? 7 : 1);
				convertedValues[3] += lastDigitSerialEven ? 7 : 1;
			}
			else
            {
				QuickLog("The last digit of the serial number or [NUMBER 3] are not even. Modifier for fourth digit: {0}", lastDigitSerialEven ? 3 : 0);
				convertedValues[3] += lastDigitSerialEven ? 3 : 0;
			}

		}
		else
        {
			QuickLog("[NUMBER 2] does not contain a 7.");
			if (relevantNumbers.ElementAt(3).Contains('3') && relevantNumbers.ElementAt(4).Contains('3'))
			{
				QuickLog("Both [NUMBER 4] and [NUMBER 5] contain a 3. Modifier for fourth digit: {0}", lastDigitSerialEven ? 7 : 4);
				convertedValues[3] += lastDigitSerialEven ? 7 : 4;
			}
			else
			{
				QuickLog("[NUMBER 4] or [NUMBER 5] do not contain a 3. Modifier for fourth digit: {0}", lastDigitSerialEven ? 2 : 8);
				convertedValues[3] += lastDigitSerialEven ? 2 : 8;
			}
		}
		QuickLog("Digit 5 Modification:"); // Start of Digit 5 Conditions
		if (relevantNumbers.ElementAt(2).Contains('4'))
        {
			QuickLog("[NUMBER 3] does contains a 4.");
			if (relevantNumbers.Count(a => evenNums.Contains(a.Last())) >= 3)
            {
				QuickLog("3 or more of the relevant numbers are even. Modifier for fifth digit: {0}", lastDigitSerialEven ? 9 : 8);
				convertedValues[4] += lastDigitSerialEven ? 9 : 8;
			}
			else
            {
				QuickLog("2 or fewer of the relevant numbers are even. Modifier for fifth digit: {0}", 3);
				convertedValues[4] += 3;
			}
		}
		else
        {
			QuickLog("[NUMBER 3] does not contain a 4.");
			if (relevantNumbers.Any(a => digitString.Any(b => a.Count(c => c == b) == 1)))
			{
				QuickLog("There is a relevant number whose digits appear only once. Modifier for fifth digit: {0}", lastDigitSerialEven ? 7 : 6);
				convertedValues[4] += lastDigitSerialEven ? 7 : 6;
			}
			else
			{
				QuickLog("There are no relevant numbers whose digits appear only once. Modifier for fifth digit: {0}", lastDigitSerialEven ? 8 : 9);
				convertedValues[4] += lastDigitSerialEven ? 8 : 9;
			}
		}
		QuickLog("Digit 6 Modification:"); // Start of Digit 6 Conditions
		if (relevantNumbers.Any(a => a.Contains("11")))
		{
			QuickLog("There is a relevant number that contains \"11\".");
			if (relevantNumbers.Any(a => a[1] == '0'))
            {
				QuickLog("There is a relevant number whose second digit is a 0. Modifier for sixth digit: {0}", 1);
				convertedValues[5] += 1;
			}
			else
            {
				QuickLog("There are no relevant numbers whose second digit is a 0. Modifier for sixth digit: {0}", lastDigitSerialEven ? 5 : 0);
				convertedValues[5] += lastDigitSerialEven ? 5 : 0;
			}
		}
		else
		{
			QuickLog("There are no relevant numbers that contain \"11\".");
			if (relevantNumbers.Any(a => a[2] == '5'))
			{
				QuickLog("There is a relevant number whose third digit is a 5. Modifier for sixth digit: {0}", lastDigitSerialEven ? 0 : 4);
				convertedValues[5] += lastDigitSerialEven ? 0 : 4;
			}
			else
			{
				QuickLog("There are no relevant numbers whose third digit is a 5. Modifier for sixth digit: {0}", lastDigitSerialEven ? 4 : 5);
				convertedValues[5] += lastDigitSerialEven ? 4 : 5;
			}
		}
		QuickLog("Digit 7 Modification:"); // Start of Digit 7 Conditions
		if (relevantNumbers.Count(a => "35".All(b => a.Contains(b))) == 2)
		{
			QuickLog("There are exactly 2 relevant numbers that contain both 3 and 5.");
			if (relevantNumbers.Count(a => evenNums.Contains(a.Last())) == 3)
			{
				QuickLog("There are exactly 3 relevant numbers that are even. Modifier for seventh digit: {0}", lastDigitSerialEven ? 2 : 5);
				convertedValues[6] += lastDigitSerialEven ? 2 : 5;
			}
			else
			{
				QuickLog("There are not exactly 3 relevant numbers that are even. Modifier for seventh digit: {0}", lastDigitSerialEven ? 6 : 9);
				convertedValues[6] += lastDigitSerialEven ? 6 : 9;
			}
		}
		else
		{
			QuickLog("There are not exactly 2 relevant numbers that contain both 3 and 5.");
			if (relevantNumbers.Count(a => a.Contains('9')) <= 3)
			{
				QuickLog("There are 3 or fewer relevant numbers that contain a 9. Modifier for seventh digit: {0}", lastDigitSerialEven ? 4 : 3);
				convertedValues[6] += lastDigitSerialEven ? 4 : 3;
			}
			else
			{
				QuickLog("There are more than 3 relevant numbers that contain a 9. Modifier for seventh digit: {0}", lastDigitSerialEven ? 9 : 7);
				convertedValues[6] += lastDigitSerialEven ? 9 : 7;
			}
		}
		QuickLog("Digit 8 Modification:"); // Start of Digit 8 Conditions
		if (relevantNumbers.Any(a => "15".All(b => a.Contains(b))))
		{
			QuickLog("There is a number that contains both 1 and 5.");
			if (int.Parse(relevantNumbers.ElementAt(1)) % 3 == 0)
			{
				QuickLog("[NUMBER 2] is divisible by 3. Modifier for eighth digit: {0}", digitString.IndexOf(relevantNumbers.ElementAt(0).Last()));
				convertedValues[7] += digitString.IndexOf(relevantNumbers.First().Last());
			}
			else
			{
				QuickLog("[NUMBER 2] is not divisible by 3. Modifier for eighth digit: {0}", digitString.IndexOf(relevantNumbers.ElementAt(4).Last()));
				convertedValues[7] += digitString.IndexOf(relevantNumbers.ElementAt(4).Last());
			}
		}
		else
		{
			QuickLog("There is not a number that contains both 1 and 5.");
			if (int.Parse(relevantNumbers.ElementAt(3)) % 7 == 0)
			{
				QuickLog("[NUMBER 4] is divisible by 7. Modifier for eighth digit: {0}", lastDigitSerialEven ? 0 : 4);
				convertedValues[7] += lastDigitSerialEven ? 0 : 4;
			}
			else
			{
				QuickLog("[NUMBER 4] is not divisible by 7. Modifier for eighth digit: {0}", digitString.IndexOf(relevantNumbers.ElementAt(2).Last()));
				convertedValues[7] += digitString.IndexOf(relevantNumbers.ElementAt(2).Last());
			}
		}
		expectedAnswer = convertedValues.Select(a => a % 10).Join("");
		QuickLog("The answer the module wants is {0}.", expectedAnswer);
		QuickLog("------------------------------------------------------------------------");
	}

	// Update is called once per frame
	void Update () {
		if (!started || readyToSolve) return;
		if (curCooldown > 0f)
			curCooldown -= Time.deltaTime * (autosolveRequest ? 5f : 1f);
		else
        {
			var nonIgnoredSolveCount = bombInfo.GetSolvedModuleIDs().Count(a => !ignoreModuleIDsList.Contains(a));
			if (curStageIdx < nonIgnoredSolveCount)
			{
				curStageIdx++;
				if (curStageIdx >= allStageDisplays.Count)
                {
					readyToSolve = true;
					StartCoroutine(HandleTransitionToSubmitAnim());
                }
				else
                {
					curCooldown = startCooldown;
					StartCoroutine(HandleStageAnim(curStageIdx));
                }
			}
        }
	}

	IEnumerator TwitchHandleForcedSolve()
	{
		QuickLogDebug("Autosolve requested via TP Handler.");
		autosolveRequest = true;
		while (!readyToSolve)
			yield return true;
		if (inputtedAnswer.Any())
		{
			clrSelectable.OnInteract();
			yield return new WaitForSeconds(0.1f);
		}
		for (var x = 0; x < 8; x++)
		{
			keyNumSelectables[digitString.IndexOf(expectedAnswer[x])].OnInteract();
			yield return new WaitForSeconds(0.1f);
		}
		submitSelectable.OnInteract();
	}
#pragma warning disable 414
	private readonly string TwitchHelpMessage = "Press the specified digits on the module with \"!{0} press ########\", submit the answer with \"!{0} submit ########\", or clear inputs with \"!{0} clear/reset/delete\". To just press the submit button, do not specify digits. Using the submit command will NOT clear previous inputs!";
#pragma warning restore 414
	IEnumerator ProcessTwitchCommand(string command)
	{
		Match matchPressDigits = Regex.Match(command, @"^press\s\d+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
			matchSubmitDigitsOrPlain = Regex.Match(command, @"^submit(\s\d+)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
			matchClear = Regex.Match(command, @"^(reset|clear|delete)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		if (matchClear.Success)
        {
			yield return null;
			clrSelectable.OnInteract();
        }
		else if (matchPressDigits.Success)
        {
			var possibleDigits = matchPressDigits.Value.Split().Last();
			var idxDigits = possibleDigits.Select(a => digitString.IndexOf(a));
			if (idxDigits.Any(a => a < 0))
            {
				yield return string.Format("sendtochaterror Detected an invalid digit: \"{0}\" Check your command for typos.", possibleDigits.First(a => !digitString.Contains(a)));
				yield break;
            }
			yield return null;
            for (var x = 0; x < idxDigits.Count(); x++)
            {
				keyNumSelectables[idxDigits.ElementAt(x)].OnInteract();
				yield return new WaitForSeconds(0.1f);
            }
        }
		else if (matchSubmitDigitsOrPlain.Success)
        {
			var detectedCmd = matchSubmitDigitsOrPlain.Value.Split();
			if (detectedCmd.Count() == 1)
            {
				yield return null;
				submitSelectable.OnInteract();
				yield break;
            }
			var possibleDigits = detectedCmd.Last();
			var idxDigits = possibleDigits.Select(a => digitString.IndexOf(a));
			if (idxDigits.Any(a => a < 0))
            {
				yield return string.Format("sendtochaterror Detected an invalid digit: \"{0}\" Check your command for typos.", possibleDigits.First(a => !digitString.Contains(a)));
				yield break;
            }
			yield return null;
            for (var x = 0; x < idxDigits.Count(); x++)
            {
				keyNumSelectables[idxDigits.ElementAt(x)].OnInteract();
				yield return new WaitForSeconds(0.1f);
            }
			submitSelectable.OnInteract();
        }


		yield break;
	}
}
