using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using KModkit;

public class EverchangingCore : MonoBehaviour {
	enum InputProcedure
    {
		None,
		TenDigitKeypad,
		WireSequences,
		MonochromeArrows,
    }
	static readonly InputProcedure[] rollableInputs = {
		//InputProcedure.TenDigitKeypad,
		//InputProcedure.WireSequences,
		InputProcedure.MonochromeArrows, };
	public KMSelectable debugButton;
	public KMBombModule modSelf;
	public KMBombInfo bombInfo;
	public KMAudio mAudio;
	public KMBossModuleExtensions bossHandler;
	public KMColorblindMode colorblindMode;
	// The input procedures.
	public QuestionableWireSequencesCore wireSequencesCore;
	public TenDigitKeypadCore tenDigitKeypadCore;
	public MonochromeArrowsCore mArrowsCore;
	// The visuals responsible for each stage.
	public KMSelectable[] timerSelectableHalves;
	public MeshRenderer[] segmentRenderers, cubeDotRenderers;
	public MeshRenderer ledRenderer, segmentDotRenderer;
	public CubeDisplayer alphaForgetCube;
	public MeshRenderer[] colorblindTextRenderers;
	public TextMesh[] cubeDigits;
	public TextMesh statusDisplay, timerDisplay, colorblindLEDText, colorblindDigitText;
	public Transform cubeTransform, sevenSegmentSet;
	public DoorAnimScript doorAnim;
	public ProgressBarHandler barHandler;

	public Color[] colorIndexes;
	public string[] colorNames;
	private string[] ignoreIds;
	private bool bossModeActive = false, requestNextStage = false, inputModeActive = false,
		hasStarted = false, TwitchPlaysActive, dynamicStageGen,
		allowTimerDrain = false, recoveryRequestNextStage = false, moduleSolved,
		colorblindDetected, showingInputs, revealProgress;
	List<StageComponentInfoAll> allGeneratedStages;
	List<int> calculatedValues, stageIdxCalcsVoids;
	static int modIDCnt = 1;
	int modID, stagesSinceLastInput = 0, curStageIdx = -1, totalPossibleStages, idxStartStageInputsAll;
	float timeBase = 15, curTimeLeft, animSpeedMultiplier = 0.5f;
    string TwitchHelpMessage = "This module does nothing! Sorry about that.";
	readonly string helpMessageBossAppend = " This help message changes every stage, so be on a lookout for when the stage changes!",
		baseHelpMessage = "(Default TP Command) Tilt the module with \"!{0} tilt [direction/degrees from north]\" to get a better view of the given module.",
		questionableWireSequenceHelpMessage = "",
		_10DigitKeypadHelpMessage = "Input the digits 0,1,2,3,4,5,6,7,8,9 in that order with \"!{0} press 531820...\" or \"!{0} submit 531820...\"",
		monochromicArrowsHelpMessage = "",
		exhibitionModeMessageAppend = " Advance to the next stage with \"!{0} advance/next/n\"";
	readonly static int[][] tableRefCubeDesyncValues = {
							new[] { 5, 8, 4, 1, 0, 3, 9, 2, 6, 7 },
							new[] { 1, 4, 7, 0, 8, 9, 6, 3, 5, 2 },
							new[] { 4, 2, 8, 9, 6, 5, 3, 0, 7, 1 },
							new[] { 3, 9, 0, 8, 7, 1, 4, 5, 2, 6 },
							new[] { 7, 3, 2, 6, 5, 0, 1, 8, 9, 4 },
							new[] { 9, 0, 1, 4, 2, 7, 8, 6, 3, 5 }};
	readonly static int[][][] cubeLitePairPossibleValues =
	{
		new int[][] { new[] { 1, 9 }, new[] { 2, 8 } , new[] { 3, 7 } , new[] { 4, 6 } },					// 0
		new int[][] { new[] { 0, 1 }, new[] { 2, 9 } , new[] { 3, 8 } , new[] { 4, 7 } , new[] { 5, 6 } },	// 1
		new int[][] { new[] { 0, 2 }, new[] { 3, 9 } , new[] { 4, 8 } , new[] { 5, 7 } },					// 2
		new int[][] { new[] { 0, 3 }, new[] { 1, 2 } , new[] { 4, 9 } , new[] { 5, 8 } , new[] { 6, 7 } },	// 3
		new int[][] { new[] { 0, 4 }, new[] { 1, 3 } , new[] { 5, 9 } , new[] { 6, 8 } },					// 4
		new int[][] { new[] { 0, 5 }, new[] { 1, 4 } , new[] { 2, 3 } , new[] { 6, 9 } , new[] { 7, 8 } },	// 5
		new int[][] { new[] { 0, 6 }, new[] { 1, 5 } , new[] { 2, 4 } , new[] { 7, 9 } },					// 6
		new int[][] { new[] { 0, 7 }, new[] { 1, 6 } , new[] { 2, 5 } , new[] { 3, 4 } , new[] { 8, 9 } },	// 7
		new int[][] { new[] { 0, 8 }, new[] { 1, 7 } , new[] { 2, 6 } , new[] { 3, 5 } },					// 8
		new int[][] { new[] { 0, 9 }, new[] { 1, 8 } , new[] { 2, 7 } , new[] { 3, 6 } , new[] { 4, 5 } },	// 9
	};
	readonly static bool[][] enabledDotStates = {
		Enumerable.Repeat(false, 9).ToArray(),	// 0
		new[] { false, false, false, false, true, false, false, false, false, },	// 1
		new[] { true, false, false, false, false, false, false, false, true, },		// 2
		new[] { true, false, false, false, true, false, false, false, true, },		// 3
		new[] { true, false, true, false, false, false, true, false, true, },	// 4
		new[] { true, false, true, false, true, false, true, false, true, },	// 5
		new[] { true, false, true, true, false, true, true, false, true, },	// 6
		new[] { true, false, true, true, true, true, true, false, true, },	// 7
		new[] { true, true, true, true, false, true, true, true, true, },	// 8
		Enumerable.Repeat(true, 9).ToArray(),	// 9
	}, enabledSegmentStates = {
		new[] { true, true, true, false, true, true, true, },	// 0
		new[] { false, false, true, false, false, true, false, },	// 1
		new[] { true, false, true, true, true, false, true, },		// 2
		new[] { true, false, true, true, false, true, true, },		// 3
		new[] { false, true, true, true, false, true, false, },	// 4
		new[] { true, true, false, true, false, true, true, },	// 5
		new[] { true, true, false, true, true, true, true, },	// 6
		new[] { true, false, true, false, false, true, false, },	// 7
		new[] { true, true, true, true, true, true, true, },	// 8
		new[] { true, true, true, true, false, true, true, },	// 9
	};
	static readonly string[] debugDirections = { "Up", "Right", "Down", "Left", };
	IEnumerable<int> lockFirstStageComponentsIdx, requiredStageIdxes;
	FlyersBossierSettings modSelfSettings;
	Vector3 storedCubeLocalPos, storedLEDLocalPos, storedSevenSegmentLocalPos, storedWireSeqLocalPos, storedMonochromeArrowsLocalPos;
	public int debugIdxStageIdx = -1;
	public bool debugBossMode, disableInputLinking;
	InputProcedure currentInputSet;
	// Use this for initialization
	void Start () {
		modID = modIDCnt++;
		string[] defaultIgnoreIDs = { "EverchangingModule" };
		allGeneratedStages = new List<StageComponentInfoAll>();
		stageIdxCalcsVoids = new List<int>();
		calculatedValues = new List<int>();
		ignoreIds = bossHandler.GetAttachedIgnoredModuleIDs(modSelf);
		var canEnableBossMode = false;
		if (ignoreIds != null && ignoreIds.Any())
			canEnableBossMode = true;
		else if (debugBossMode && Application.isEditor)
		{
			canEnableBossMode = true;
			ignoreIds = defaultIgnoreIDs;
			QuickLog("Debugging Boss Mode on TestHarness");
		}
		else
			QuickLog("Enforcing Exhibition Mode due to a missing list of ignored modules. This is done to prevent softlocks.");
		modSelf.OnActivate += delegate {
			totalPossibleStages = bombInfo.GetSolvableModuleIDs().Count(a => !ignoreIds.Contains(a));
			mAudio.PlaySoundAtTransform("181Faded", transform);
			StartCoroutine(StartBossModuleHandling());
			hasStarted = true;
		};
		for (var x = 0; x < timerSelectableHalves.Length; x++)
        {
			var y = x;
        }
		foreach (MeshRenderer dotRenderer in cubeDotRenderers)
			dotRenderer.enabled = false;
		try
        {
			ModConfig<FlyersBossierSettings> settingsFile = new ModConfig<FlyersBossierSettings>("FlyersBossierSettings");
			modSelfSettings = settingsFile.Settings;
			settingsFile.Settings = modSelfSettings;
			if (!modSelfSettings.ECExhibitionMode)
				bossModeActive = canEnableBossMode;
			else
				QuickLog("Enforcing Exhibition Mode by settings.");
			dynamicStageGen = modSelfSettings.ECDynamicStageGen;
        }
		catch
        {
			Debug.LogWarningFormat("<Everchanging #{0}> SETTINGS DO NOT WORK AS INTENDED, USING DEFAULT SETTINGS.", modID);
			dynamicStageGen = false;
			bossModeActive = canEnableBossMode;
        }
        finally
        {
			QuickLog("Dynamic Stage Generation: {0}", dynamicStageGen ? "Enabled" : "Disabled");
			try
            {
				colorblindDetected = colorblindMode.ColorblindModeActive;
            }
			catch
            {
				colorblindDetected = false;
            }
		}
		
		storedCubeLocalPos = cubeTransform.localPosition;
		storedLEDLocalPos = ledRenderer.transform.localPosition;
		storedSevenSegmentLocalPos = sevenSegmentSet.transform.localPosition;
		storedWireSeqLocalPos = wireSequencesCore.transform.localPosition;
		storedMonochromeArrowsLocalPos = mArrowsCore.transform.localPosition;

		debugButton.OnInteract += delegate {
			if (hasStarted && !moduleSolved)
            {
				if (!bossModeActive && allowTimerDrain)
				{
                    requestNextStage = true;
					barHandler.curProgress = 1f;
				}
            }
			return false;
		};
		if (!disableInputLinking)
        {
			var keypadSelectables = tenDigitKeypadCore.digits;
			for (var x = 0; x < keypadSelectables.Length; x++)
            {
				var y = x;
				keypadSelectables[x].OnInteract += delegate
				{
					keypadSelectables[y].AddInteractionPunch(0.1f);
					mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, keypadSelectables[y].transform);
					ProcessTenDigitKeypadInput(y);
					return false;
				};
            }
			var arrowSelectables = mArrowsCore.arrowSelectables;
			for (var x = 0; x < arrowSelectables.Length; x++)
            {
				var y = x;
				arrowSelectables[x].OnInteract += delegate
				{
					arrowSelectables[y].AddInteractionPunch(0.1f);
					mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, arrowSelectables[y].transform);
					ProcessMonochromeArrowsInput(y);
					return false;
				};
            }
        }
		tenDigitKeypadCore.bombInfo = bombInfo;
		alphaForgetCube.gameObject.SetActive(false);
		cubeTransform.gameObject.SetActive(false);
		sevenSegmentSet.gameObject.SetActive(false);
		ledRenderer.gameObject.SetActive(false);
		wireSequencesCore.gameObject.SetActive(false);
		mArrowsCore.gameObject.SetActive(false);
		tenDigitKeypadCore.gameObject.SetActive(false);
		statusDisplay.text = "";
		timerDisplay.text = "";
	}
	void ProcessTenDigitKeypadInput(int numPressed)
    {
		if (currentInputSet != InputProcedure.TenDigitKeypad) return;
		var allSubmissionValue = tenDigitKeypadCore.submissionValues ?? new List<int>();
		var curIdxInput = tenDigitKeypadCore.currentInputIdx;
		if (curIdxInput < allSubmissionValue.Count && allSubmissionValue[curIdxInput] == numPressed)
        {
			tenDigitKeypadCore.currentInputIdx++;
			if (tenDigitKeypadCore.currentInputIdx >= allSubmissionValue.Count)
            {
				QuickLog("Ten Digit Keypad Input Procedure has been completed.");
				HandleCompleteInputProcedure();
			}
		}
		else if (curIdxInput >= allSubmissionValue.Count)
        {
			QuickLog("Input Procedure, Ten Digit Keypad, has no required digits to input. Auto-completing...");
			HandleCompleteInputProcedure();
		}
		else
        {
			// Handle Recovery Anim here.
			revealProgress = true;
			QuickLog("Strike! #{0} was incorrectly pressed for digit #{1} to input!", numPressed, curIdxInput);
		}
    }
	void ProcessMonochromeArrowsInput(int idxPressed)
    {
		if (currentInputSet != InputProcedure.MonochromeArrows) return;
		var allSubmissionValue = mArrowsCore.expectedPressIdxes ?? new List<int>();
		var curIdxInput = mArrowsCore.currentInputIdx;
		if (curIdxInput < allSubmissionValue.Count && allSubmissionValue[curIdxInput] == idxPressed)
        {
			mArrowsCore.currentInputIdx++;
			if (mArrowsCore.currentInputIdx >= allSubmissionValue.Count)
            {
				QuickLog("Monochrome Arrows Input Procedure has been completed.");
				HandleCompleteInputProcedure();
			}
		}
		else if (curIdxInput >= allSubmissionValue.Count)
        {
			QuickLog("Input Procedure, Monochrome Arrows, has no required presses to input. Auto-completing...");
			HandleCompleteInputProcedure();
		}
		else
        {
			// Handle Recovery Anim here.
			QuickLog("Strike! The arrow in position {0} was incorrectly pressed!", idxPressed + 1);
			revealProgress = true;
			allowTimerDrain = false;
		}
    }
	
	void HandleCompleteInputProcedure()
    {
		currentInputSet = InputProcedure.None;
		mAudio.PlaySoundAtTransform("shutdown", transform);
		doorAnim.increasingValue = false;
		if (curStageIdx >= totalPossibleStages)
        {
			QuickLog("You did all of the stages. Go home. You are finally done.");
			modSelf.HandlePass();
			moduleSolved = true;
        }
		else
        {
			idxStartStageInputsAll = curStageIdx;
            QuickLog("Completing this input procedure has knocked out the following stages from being included in future input procedures: {0}", requiredStageIdxes.Select(a => a + 1).Join(", "));
		}
    }

	void QuickLog(string toLog, params object[] args)
    {
		Debug.LogFormat("[Everchanging #{0}] {1}", modID, string.Format(toLog, args));
    }
	void QuickLogDebug(string toLog, params object[] args)
    {
		Debug.LogFormat("<Everchanging #{0}> {1}", modID, string.Format(toLog, args));
	}
	void GenerateStage()
    {
		QuickLog("--------------- STAGE {0} ---------------", curStageIdx + 1);

		var newStage = new StageComponentInfoAll();
		if (curStageIdx + (bossModeActive ? 6 : 1) >= totalPossibleStages)
			stagesSinceLastInput = 0;
		var idxInputs = Random.Range((stagesSinceLastInput >= (bossModeActive ? 6 : 3)) ? 0 : 1, 8);
		if (curStageIdx >= totalPossibleStages)
			idxInputs = 0;
		else if (bossModeActive && curStageIdx < 3)
        {
			if (lockFirstStageComponentsIdx == null)
				lockFirstStageComponentsIdx = new[] { 1, 2, 4 }.Shuffle();
			idxInputs = lockFirstStageComponentsIdx.ElementAt(curStageIdx);
		}
		else if (Application.isEditor && debugIdxStageIdx > 0)
			idxInputs = debugIdxStageIdx;

		for (int x = 0; x < newStage.enabledComponents.Length; x++)
		{
			newStage.enabledComponents[x] = idxInputs / (1 << x) % 2 == 1;
		}
		// Enabled components are listed in this order: LED, Cube, Digit
        QuickLog("Enabled components for this stage: [{0}]", newStage.enabledComponents.Any(a => a) ? Enumerable.Range(0, 3).Where(a => newStage.enabledComponents[a]).Select(b => new[] { "LED", "Cube", "Digit", }[b]).Join() : "none");
		if (idxInputs != 0)
		{
			stagesSinceLastInput++;
			switch (idxInputs)
            {
				case 1:
					{ // Singular LED
						QuickLog("Singular LED will be active in this stage.");
                        var referenceTable = new[,] {
							{ 5, 6, 7, 8, 9, 0, 1, 2, 3, 4 },
							{ 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 },
						};
						var selectedColorIdx = Random.Range(0, 10);
						newStage.ledColorIdx = selectedColorIdx;
						var resultingCalculatedValue = referenceTable[(curStageIdx + 1) % 2, selectedColorIdx];
						QuickLog("The selected color will be {0} which corresponds to the value: {1}", colorNames[selectedColorIdx], resultingCalculatedValue);
						calculatedValues.Add(resultingCalculatedValue);
					}
					break;
				case 2:
					{ // Floating Cube
						QuickLog("Floating Cube will be active in this stage.");
						var netDisplay = new int[][] {
							new[] { 1, 2, 3, 5, 4, 6 },
							new[] { 1, 3, 3, 3, 5, 6 },
							new[] { 1, 1, 1, 4, 4, 9 },
							new[] { 0, 2, 4, 4, 4, 6 },
							new[] { 0, 1, 3, 3, 5, 7 },
							new[] { 2, 2, 2, 2, 6, 6 },
							new[] { 0, 4, 4, 4, 4, 4 },
							new[] { 0, 3, 3, 3, 3, 6 },
							new[] { 3, 3, 3, 3, 4, 4 },
							new[] { 1, 1, 1, 6, 6, 6 },
						};
						var calculatedIndexValues = Random.Range(0, 10);
						QuickLog("The selected net index is {0} which corresponds to these faces: {1}", calculatedIndexValues, netDisplay[calculatedIndexValues].Join());
						newStage.displayedNumbers = netDisplay[calculatedIndexValues];
						calculatedValues.Add(calculatedIndexValues);
					}
					break;
				case 3:
					{ // AlphaForget
						QuickLog("AlphaForget will be active in this stage.");
                        var ledDigitsDisplay = new[] { 7, 4, 1, 8, 5, 2, 9, 6, 3, 0 };
                        var fixedIndexRotation = new[] { 1, 2, 3, 5, 6, 7 }.PickRandom();
						var ledIndex = Random.Range(0, 10);
						var stageCalculatedValue = 0;
						var serialNoNumbers = bombInfo.GetSerialNumberNumbers();
						var ledValue = ledDigitsDisplay[ledIndex];

						newStage.fixedRotationIdx = new[] { fixedIndexRotation };
						newStage.ledColorIdx = ledIndex;
						QuickLog("Color of the LED: {0}", colorNames[ledIndex]);
						QuickLog("Rotation applied: {0}{1}", "XYZ"[fixedIndexRotation % 3], "XYZ"[fixedIndexRotation / 3]);
						switch (fixedIndexRotation)
                        {
							case 1:
								stageCalculatedValue = curStageIdx + 1 - ledValue;
								goto default;
							case 2:
								stageCalculatedValue = serialNoNumbers.FirstOrDefault() - (curStageIdx + 1);
								goto default;
							case 3:
                                stageCalculatedValue = ledValue + curStageIdx + 1;
								goto default;
							case 5:
								stageCalculatedValue = ledValue + serialNoNumbers.ElementAtOrDefault(1);
								goto default;
							case 6:
								stageCalculatedValue = curStageIdx + 1 + serialNoNumbers.FirstOrDefault();
								goto default;
							case 7:
								stageCalculatedValue = serialNoNumbers.ElementAtOrDefault(1) - ledValue;
								goto default;
							default:
								stageCalculatedValue = (stageCalculatedValue % 10 + 10) % 10;
								break;
                        }
						QuickLog("Calculated Value for this stage: {0}", stageCalculatedValue);
						calculatedValues.Add(stageCalculatedValue);
					}
					break;
				case 4:
                    {// Lonely Digit
						QuickLog("Lonely Digit will be active in this stage.");
						var initialValue = Random.Range(0, 10);
						newStage.displayedNumbers = new[] { initialValue };
						var stageCalculatedValue = initialValue;
						QuickLog("Initial Value: {0}", initialValue);
						if (!(bombInfo.IsPortPresent(Port.Parallel) && bombInfo.IsIndicatorOn(Indicator.NSA)))
                        {
							stageCalculatedValue -= bombInfo.GetSerialNumber().ToUpper().Any(a => "AEIOUaeiou".Contains(a)) ? 1 : 0;
							QuickLog("Value after serial no. vowel modifier: {0}", stageCalculatedValue);
							stageCalculatedValue += bombInfo.GetBatteryCount();
							QuickLog("Value after battery modifier: {0}", stageCalculatedValue);
							stageCalculatedValue += bombInfo.GetSerialNumberNumbers().LastOrDefault() % 2 == 0 ? 1 : 0;
							QuickLog("Value after last digit serial no. modifier: {0}", stageCalculatedValue);
							stageCalculatedValue += bombInfo.IsIndicatorPresent(Indicator.CAR) ? 1 : 0;
							QuickLog("Value after CAR indicator modifier: {0}", stageCalculatedValue);
						}
						else
							QuickLog("There is a parallel port and a lit NSA. Modifiers will not be used on this stage.");
						stageCalculatedValue %= 10;
						QuickLog("Calculated Value for this stage: {0}", stageCalculatedValue);
						calculatedValues.Add(stageCalculatedValue);
					}
					break;
				case 5:
                    {// Simple LED Math
						QuickLog("Simple LED Math will be active in this stage.");
                        var ledValuesAll = new[] { 4, 9, 0, 8, 1, 7, 2, 6, 5, 3 };
						var initialValue = Random.Range(0, 10);
						var initialLEDIdx = Random.Range(0, 10);
						QuickLog("Displayed Value: {0}", initialValue);
						QuickLog("Color of the LED: {0}", colorNames[initialLEDIdx]);
						var modifier = Enumerable.Range(0, 2).Select(a => 2 * a - 1).PickRandom();
						QuickLog("Operator used: {0}", modifier < 0 ? '-' : '+');
						var stageCalculatedValue = (initialValue + ledValuesAll[initialLEDIdx] * modifier) % 10;
						stageCalculatedValue = (stageCalculatedValue + 10) % 10;
						if (modifier < 0)
							newStage.fixedRotationIdx = new int[1];
						newStage.ledColorIdx = initialLEDIdx;
						newStage.displayedNumbers = new[] { initialValue };
						QuickLog("Calculated Value for this stage: {0}", stageCalculatedValue);
						calculatedValues.Add(stageCalculatedValue);
					}
					break;
				case 6:
                    {// Cube Desynchronication
						QuickLog("Cube Desynchronization will be active in this stage.");
						var finalValue = Random.Range(0, 10);
						calculatedValues.Add(finalValue);
						QuickLog("Calculated Value for this stage: {0}", finalValue);
						QuickLog("How do you get this value?");
						var exampleValue = Random.Range(0, 10);
						var debugRotationsCubeDesync = new[] { "X", "X'", "Y", "Y'", "Z", "Z'" };
						var rotationIdxSelected = Random.Range(0, 6);
						QuickLog("Take for example the last displayed value, {0}", exampleValue);
						QuickLog("And the corresponding rotation: {0}", debugRotationsCubeDesync[rotationIdxSelected]);
						QuickLog("Applying it to the table provided gives this value: {0}", tableRefCubeDesyncValues[rotationIdxSelected][exampleValue]);
						var nextValueExample = (tableRefCubeDesyncValues[rotationIdxSelected][exampleValue] + finalValue) % 10;
						newStage.displayedNumbers = new[] { exampleValue };
						newStage.fixedRotationIdx = new[] { rotationIdxSelected };
						QuickLog("From that value to {0}, it should take {1} step(s).", nextValueExample, finalValue);
					}
					break;
				case 7:
                    {// The Cube Lite
						QuickLog("The Cube Lite will be active in this stage.");
						
						var determinedCubeValues = new int[6];
						for (var x = 0; x < determinedCubeValues.Length; x++)
							determinedCubeValues[x] = Random.Range(0, 10);
						newStage.cubeDisplayTexts = determinedCubeValues.Select(a => a.ToString()).ToArray();
						QuickLog("Cube Faces' Displayed Digits: {0}", determinedCubeValues.Join(", "));
						var finalValue = 0;
						
						// First get the starting value based on picking a random LED color.
						var selectedLEDColorIdx = Random.Range(0, 6);
						QuickLog("LED Color Selected: {0}", new[] { "Red", "Orange", "White", "Green", "Blue", "Purple" }[selectedLEDColorIdx]);
						var ledValuesAll = new[] { bombInfo.GetModuleIDs().Count + 5, curStageIdx + 1, 2, 6 + bombInfo.GetSerialNumberNumbers().Sum(), 1 + bombInfo.GetSerialNumberLetters().Count(), determinedCubeValues.Sum() };
                        var ledColorIdxesAll = new[] { 1, 2, 0, 4, 7, 8 };
						finalValue = ledValuesAll[selectedLEDColorIdx];
						newStage.ledColorIdx = ledColorIdxesAll[selectedLEDColorIdx];

						var idxCubeValuesObtained = new[] { 4, 1, 5, 0, 2, 3 };
						var selectedIdxRotation = Random.Range(0, 6);
                        var selectedIdxNonUniqueRotation = selectedIdxRotation <= 1 ? Enumerable.Range(2, 4).PickRandom() : Enumerable.Range(0, 6).Where(a => a != selectedIdxRotation).PickRandom();

						newStage.fixedRotationIdx = Enumerable.Repeat(selectedIdxNonUniqueRotation, 4).Concat(new[] { selectedIdxRotation }).ToArray().Shuffle();
						//var determinedRotation = new[] { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
						QuickLog("Distinct Rotation Selected: {0}", new[] { "Rotate CW", "Rotate CCW", "Tilt Down", "Tilt Up", "Tip Left", "Tip Right" }[selectedIdxRotation]);
						finalValue += determinedCubeValues[idxCubeValuesObtained[selectedIdxRotation]];
						QuickLog("Value after adding LED value and rotation value: {0}", finalValue);
						var stageNumMod10 = (curStageIdx + 1) % 10;
						finalValue %= stageNumMod10 == 5 ? 8 : stageNumMod10 == 6 ? 9 : 10;
						QuickLog("After Modulo {1}: {0}", finalValue, stageNumMod10 == 5 ? 8 : stageNumMod10 == 6 ? 9 : 10);
						var selectedRandomValue = Random.Range(0, 10);
						var selectedNums = cubeLitePairPossibleValues[selectedRandomValue].PickRandom();
						newStage.displayedNumbers = selectedNums.ToArray().Shuffle();
						QuickLog("Selected Displayed Digits (Sum of {1} after mod 10): {0}", selectedNums.Join(", "), selectedRandomValue);
						finalValue += selectedNums.Sum();

						calculatedValues.Add(finalValue % 10);
						QuickLog("Calculated Value for this stage: {0}", finalValue % 10);
					}
					break;
            }
		}
		else
		{
			QuickLog("No components are active. Input Procedure will be active on this stage.");
			stagesSinceLastInput = 0;
			calculatedValues.Add(-1);
			stageIdxCalcsVoids.Add(curStageIdx);
			if (!dynamicStageGen)
				QuickLog("The module cannot log the input procedure here due to dynamic stage genreation being disabled. This will be determined at random.");
		}
		allGeneratedStages.Add(newStage);
	}
	IEnumerator FlickerBetweenTwoValues(IEnumerable<int> valuesToDisplay)
    {
		var curIdx = 0;
		while (!requestNextStage)
		{
			curIdx = (curIdx + 1) % valuesToDisplay.Count();
			var curValue = valuesToDisplay.ElementAt(curIdx);
			for (var x = 0; x < segmentRenderers.Length; x++)
			{
				segmentRenderers[x].material.color = enabledSegmentStates[curValue][x] ? colorIndexes[curIdx * 5] : Color.black;
			}
			segmentDotRenderer.material.color = curIdx % 2 == 1 ? colorIndexes[curIdx * 5] : Color.black;
			colorblindDigitText.text = colorblindDetected ? curIdx % 2 == 1 ? "GREEN" : "WHITE" : "";
			for (float t = 0; !requestNextStage && t < 1f; t += Time.deltaTime)
				yield return null;
		}
    }
	// Handles revealing the current stage for Everychanging
	IEnumerator HandleRevealSelectedStage(StageComponentInfoAll curStage, bool inRecoveryMode = false)
    {
		var idxStageToShow = Enumerable.Range(0, 3).Reverse().Select(a => (1 << a) * (curStage.enabledComponents[a] ? 1 : 0)).Sum();
		if (idxStageToShow > 0)
		{
			var selectedColorIdx = Enumerable.Range(0, 2).PickRandom() * 5;
			switch (idxStageToShow)
			{
				case 1: // Show Singular LED
					ledRenderer.gameObject.SetActive(true);
					ledRenderer.material.color = colorIndexes[curStage.ledColorIdx];
					for (float t = -1; t <= 1f; t += Time.deltaTime * animSpeedMultiplier)
					{
						ledRenderer.transform.localPosition = new Vector3(storedLEDLocalPos.x, storedLEDLocalPos.y * t, storedLEDLocalPos.z);
						yield return null;
					}
					ledRenderer.transform.localPosition = storedLEDLocalPos;
					allowTimerDrain |= !inRecoveryMode;
					while (!(inRecoveryMode ? recoveryRequestNextStage : requestNextStage))
						yield return null;
					for (float t = 1; t > -1f; t -= Time.deltaTime * animSpeedMultiplier)
					{
						ledRenderer.transform.localPosition = new Vector3(storedLEDLocalPos.x, storedLEDLocalPos.y * t, storedLEDLocalPos.z);
						yield return null;
					}
					ledRenderer.transform.localPosition = new Vector3(storedLEDLocalPos.x, storedLEDLocalPos.y * -1, storedLEDLocalPos.z);
					ledRenderer.gameObject.SetActive(false);
					break;
				case 2: // Show Floating Cube
					cubeTransform.gameObject.SetActive(true);
					for (var x = 0; x < 6; x++)
					{
						var curVal = curStage.displayedNumbers[x];
						for (var y = 0; y < 9; y++)
						{
							cubeDotRenderers[x * 9 + y].enabled = enabledDotStates[curVal][y];
							cubeDotRenderers[x * 9 + y].material.color = Color.cyan;
						}
						cubeDigits[x].text = "";
					}
					for (float t = -1; t <= 1f; t += Time.deltaTime * animSpeedMultiplier)
					{
						cubeTransform.transform.localPosition = new Vector3(storedCubeLocalPos.x, storedCubeLocalPos.y * t, storedCubeLocalPos.z);
						yield return null;
					}
					cubeTransform.transform.localPosition = storedCubeLocalPos;
					allowTimerDrain |= !inRecoveryMode;
					do
					{
						yield return null;
						var lastEulerAngles = cubeTransform.transform.localEulerAngles;
						var lastRotation = cubeTransform.transform.localRotation;
						var determinedRotation = new[] { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back }.PickRandom() * 90;
						var nextRotation = Quaternion.RotateTowards(lastRotation, Quaternion.Euler(lastEulerAngles + determinedRotation), 360);
						for (float t = 0; t < 1f; t += Time.deltaTime / 2f)
						{
							yield return null;
							cubeTransform.localRotation = Quaternion.Lerp(lastRotation, nextRotation, t);
						}
						cubeTransform.localRotation = nextRotation;
						for (float t = 0; !(inRecoveryMode ? recoveryRequestNextStage : requestNextStage) && t < 1f; t += Time.deltaTime)
							yield return null;
					}
					while (!(inRecoveryMode ? recoveryRequestNextStage : requestNextStage));
					for (float t = 1; t > -1f; t -= Time.deltaTime * animSpeedMultiplier)
					{
						cubeTransform.transform.localPosition = new Vector3(storedCubeLocalPos.x, storedCubeLocalPos.y * t, storedCubeLocalPos.z);
						yield return null;
					}
					cubeTransform.transform.localPosition = new Vector3(storedCubeLocalPos.x, storedCubeLocalPos.y * -1, storedCubeLocalPos.z);
					cubeTransform.gameObject.SetActive(false);
					break;
				case 3: // Show AlphaForget
					ledRenderer.gameObject.SetActive(true);
					cubeTransform.gameObject.SetActive(true);
					ledRenderer.material.color = colorIndexes[curStage.ledColorIdx];
					for (var x = 0; x < cubeDigits.Length; x++)
					{
						cubeDigits[x].text = "";
					}
					for (var y = 0; y < cubeDotRenderers.Length; y++)
					{
						cubeDotRenderers[y].enabled = false;
					}
					for (float t = -1; t <= 1f; t += Time.deltaTime * animSpeedMultiplier)
					{
						ledRenderer.transform.localPosition = new Vector3(storedLEDLocalPos.x, storedLEDLocalPos.y * t, storedLEDLocalPos.z);
						cubeTransform.transform.localPosition = new Vector3(storedCubeLocalPos.x, storedCubeLocalPos.y * t, storedCubeLocalPos.z);
						yield return null;
					}
					cubeTransform.transform.localPosition = storedCubeLocalPos;
					ledRenderer.transform.localPosition = storedLEDLocalPos;
					var lastLocalScale = cubeTransform.localScale;
					alphaForgetCube.gameObject.SetActive(true);
					StartCoroutine(alphaForgetCube.RevealSpheres());
					for (float t = 1; t > 0; t -= Time.deltaTime * 2)
					{
						cubeTransform.transform.localScale = lastLocalScale * t;
						yield return null;
					}
					cubeTransform.gameObject.SetActive(false);
					var rotationIdx = curStage.fixedRotationIdx.Single();
					while (alphaForgetCube.IsCouroutineRunning())
						yield return null;
					allowTimerDrain |= !inRecoveryMode;
					do
						yield return alphaForgetCube.SimulateCustomRotation(rotationIdx % 3, rotationIdx / 3);
					while (!(inRecoveryMode ? recoveryRequestNextStage : requestNextStage));
					yield return alphaForgetCube.HideSpheres();
					cubeTransform.gameObject.SetActive(true);
					for (float t = 0; t < 1f; t += Time.deltaTime * 2)
					{
						cubeTransform.transform.localScale = lastLocalScale * t;
						yield return null;
					}
					alphaForgetCube.gameObject.SetActive(false);
					for (float t = 1; t > -1f; t -= Time.deltaTime * animSpeedMultiplier)
					{
						cubeTransform.transform.localPosition = new Vector3(storedCubeLocalPos.x, storedCubeLocalPos.y * t, storedCubeLocalPos.z);
						ledRenderer.transform.localPosition = new Vector3(storedLEDLocalPos.x, storedLEDLocalPos.y * t, storedLEDLocalPos.z);
						yield return null;
					}
					cubeTransform.transform.localPosition = new Vector3(storedCubeLocalPos.x, storedCubeLocalPos.y * -1, storedCubeLocalPos.z);
					ledRenderer.transform.localPosition = new Vector3(storedLEDLocalPos.x, storedLEDLocalPos.y * -1, storedLEDLocalPos.z);
					cubeTransform.gameObject.SetActive(false);
					ledRenderer.gameObject.SetActive(false);
					break;
				case 4: // Show Lonely Digit
					sevenSegmentSet.gameObject.SetActive(true);
					for (var x = 0; x < segmentRenderers.Length; x++)
					{
						segmentRenderers[x].material.color = enabledSegmentStates[curStage.displayedNumbers.Single()][x] ? colorIndexes[selectedColorIdx] : Color.black;
					}
					segmentDotRenderer.material.color = selectedColorIdx >= 5 ? colorIndexes[selectedColorIdx] : Color.black;
					for (float t = -1; t <= 1f; t += Time.deltaTime * 0.5f)
					{
						sevenSegmentSet.localPosition = new Vector3(storedSevenSegmentLocalPos.x, storedSevenSegmentLocalPos.y * t, storedSevenSegmentLocalPos.z);
						yield return null;
					}
					sevenSegmentSet.localPosition = storedSevenSegmentLocalPos;
					allowTimerDrain |= !inRecoveryMode;
					while (!(inRecoveryMode ? recoveryRequestNextStage : requestNextStage))
						yield return null;
					for (float t = 1; t > -1f; t -= Time.deltaTime * animSpeedMultiplier)
					{
						sevenSegmentSet.transform.localPosition = new Vector3(storedSevenSegmentLocalPos.x, storedSevenSegmentLocalPos.y * t, storedSevenSegmentLocalPos.z);
						yield return null;
					}
					sevenSegmentSet.transform.localPosition = new Vector3(storedSevenSegmentLocalPos.x, storedSevenSegmentLocalPos.y * -1, storedSevenSegmentLocalPos.z);
					sevenSegmentSet.gameObject.SetActive(false);
					break;
				case 5: // Show Simple LED Math
					sevenSegmentSet.gameObject.SetActive(true);
					ledRenderer.gameObject.SetActive(true);
					ledRenderer.material.color = colorIndexes[curStage.ledColorIdx];
					for (var x = 0; x < segmentRenderers.Length; x++)
					{
						segmentRenderers[x].material.color = enabledSegmentStates[curStage.displayedNumbers.Single()][x] ? colorIndexes[curStage.fixedRotationIdx.Count() == 1 ? 5 : 0] : Color.black;
					}
					segmentDotRenderer.material.color = curStage.fixedRotationIdx.Count() == 1 ? colorIndexes[5] : Color.black;
					for (float t = -1; t <= 1f; t += Time.deltaTime * animSpeedMultiplier)
					{
						sevenSegmentSet.localPosition = new Vector3(storedSevenSegmentLocalPos.x, storedSevenSegmentLocalPos.y * t, storedSevenSegmentLocalPos.z);
						ledRenderer.transform.localPosition = new Vector3(storedLEDLocalPos.x, storedLEDLocalPos.y * t, storedLEDLocalPos.z);
						yield return null;
					}
					sevenSegmentSet.localPosition = storedSevenSegmentLocalPos;
					ledRenderer.transform.localPosition = storedLEDLocalPos;
					allowTimerDrain |= !inRecoveryMode;
					while (!(inRecoveryMode ? recoveryRequestNextStage : requestNextStage))
						yield return null;
					for (float t = 1; t > -1f; t -= Time.deltaTime * animSpeedMultiplier)
					{
						sevenSegmentSet.transform.localPosition = new Vector3(storedSevenSegmentLocalPos.x, storedSevenSegmentLocalPos.y * t, storedSevenSegmentLocalPos.z);
						ledRenderer.transform.localPosition = new Vector3(storedLEDLocalPos.x, storedLEDLocalPos.y * t, storedLEDLocalPos.z);
						yield return null;
					}
					sevenSegmentSet.transform.localPosition = new Vector3(storedSevenSegmentLocalPos.x, storedSevenSegmentLocalPos.y * -1, storedSevenSegmentLocalPos.z);
					ledRenderer.transform.localPosition = new Vector3(storedLEDLocalPos.x, storedLEDLocalPos.y * -1, storedLEDLocalPos.z);
					sevenSegmentSet.gameObject.SetActive(false);
					break;
				case 6: // Show Cube Desynchronization
					sevenSegmentSet.gameObject.SetActive(true);
					cubeTransform.gameObject.SetActive(true);
					for (var y = 0; y < cubeDotRenderers.Length; y++)
					{
						cubeDotRenderers[y].enabled = false;
					}
					for (var x = 0; x < cubeDigits.Length; x++)
					{
						cubeDigits[x].text = "";
					}
					for (var x = 0; x < segmentRenderers.Length; x++)
					{
						segmentRenderers[x].material.color = enabledSegmentStates[curStage.displayedNumbers.Single()][x] ? colorIndexes[selectedColorIdx] : Color.black;
					}
					segmentDotRenderer.material.color = Color.black;
					for (float t = -1; t <= 1f; t += Time.deltaTime * animSpeedMultiplier)
					{
						cubeTransform.transform.localPosition = new Vector3(storedCubeLocalPos.x, storedCubeLocalPos.y * t, storedCubeLocalPos.z);
						sevenSegmentSet.localPosition = new Vector3(storedSevenSegmentLocalPos.x, storedSevenSegmentLocalPos.y * t, storedSevenSegmentLocalPos.z);
						yield return null;
					}
					cubeTransform.transform.localPosition = storedCubeLocalPos;
					sevenSegmentSet.localPosition = storedSevenSegmentLocalPos;
					var idxRotationPickedCubeDesync = curStage.fixedRotationIdx.Single();
					var curDisplayedValueCubeDesync = curStage.displayedNumbers.Single();
					allowTimerDrain |= !inRecoveryMode;
					//var debugIdx = 0;
					//var debugRotations = new[] { 0, 2 };
					//idxRotationPickedCubeDesync = debugRotations[debugIdx];
					do
					{
						yield return null;
						var lastRotation = cubeTransform.localRotation;
						var determinedRotation = new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.back, Vector3.forward }[idxRotationPickedCubeDesync] * 90;
						//Debug.Log(new[] { "X", "X'", "Y", "Y'", "Z", "Z'" }[idxRotationPickedCubeDesync]);
						var nextRotation = Quaternion.Euler(determinedRotation) * lastRotation;
						for (float t = 0; t < 1f; t += Time.deltaTime)
						{
							yield return null;
							cubeTransform.localRotation = Quaternion.Lerp(lastRotation, nextRotation, t);
						}
						segmentDotRenderer.material.color = colorIndexes[selectedColorIdx];
						cubeTransform.localRotation = nextRotation;
						curDisplayedValueCubeDesync = (calculatedValues[curStageIdx] + tableRefCubeDesyncValues[idxRotationPickedCubeDesync][curDisplayedValueCubeDesync]) % 10;
						for (var x = 0; x < segmentRenderers.Length; x++)
						{
							segmentRenderers[x].material.color = enabledSegmentStates[curDisplayedValueCubeDesync][x] ? colorIndexes[selectedColorIdx] : Color.black;
						}
						for (float t = 0; !(inRecoveryMode ? recoveryRequestNextStage : requestNextStage) && t < 1f; t += Time.deltaTime)
							yield return null;
						segmentDotRenderer.material.color = Color.black;
						idxRotationPickedCubeDesync = Random.Range(0, 6);
						//debugIdx = (debugIdx + 1) % debugRotations.Length;
						//idxRotationPickedCubeDesync = debugRotations[debugIdx];
					}
					while (!(inRecoveryMode ? recoveryRequestNextStage : requestNextStage));
					for (float t = 1; t > -1f; t -= Time.deltaTime * animSpeedMultiplier)
					{
						cubeTransform.localPosition = new Vector3(storedCubeLocalPos.x, storedCubeLocalPos.y * t, storedCubeLocalPos.z);
						sevenSegmentSet.localPosition = new Vector3(storedSevenSegmentLocalPos.x, storedSevenSegmentLocalPos.y * t, storedSevenSegmentLocalPos.z);
						yield return null;
					}
					cubeTransform.transform.localPosition = new Vector3(storedCubeLocalPos.x, storedCubeLocalPos.y * -1, storedCubeLocalPos.z);
					sevenSegmentSet.transform.localPosition = new Vector3(storedSevenSegmentLocalPos.x, storedSevenSegmentLocalPos.y * -1, storedSevenSegmentLocalPos.z);
					cubeTransform.gameObject.SetActive(false);
					sevenSegmentSet.gameObject.SetActive(false);
					break;
				case 7: // Show The Cube Lite
					ledRenderer.gameObject.SetActive(true);
					ledRenderer.material.color = colorIndexes[curStage.ledColorIdx];
					cubeTransform.gameObject.SetActive(true);
					sevenSegmentSet.gameObject.SetActive(true);
					for (var y = 0; y < cubeDotRenderers.Length; y++)
					{
						cubeDotRenderers[y].enabled = y == 6 || y == 45;
						cubeDotRenderers[y].material.color = y == 45 ? Color.gray : Color.white;
					}
					for (var x = 0; x < cubeDigits.Length; x++)
					{
						cubeDigits[x].text = curStage.cubeDisplayTexts[x];
					}
					var curDisplayedValuesCoru = StartCoroutine(FlickerBetweenTwoValues(curStage.displayedNumbers));
					for (float t = -1; t <= 1f; t += Time.deltaTime * animSpeedMultiplier)
					{
						cubeTransform.transform.localPosition = new Vector3(storedCubeLocalPos.x, storedCubeLocalPos.y * t, storedCubeLocalPos.z);
						sevenSegmentSet.localPosition = new Vector3(storedSevenSegmentLocalPos.x, storedSevenSegmentLocalPos.y * t, storedSevenSegmentLocalPos.z);
						ledRenderer.transform.localPosition = new Vector3(storedLEDLocalPos.x, storedLEDLocalPos.y * t, storedLEDLocalPos.z);
						yield return null;
					}
					cubeTransform.transform.localPosition = storedCubeLocalPos;
					sevenSegmentSet.localPosition = storedSevenSegmentLocalPos;
					ledRenderer.transform.localPosition = storedLEDLocalPos;
					var curIdxRotation = 0;
					var idxRotationPickedCubeLite = curStage.fixedRotationIdx;
					allowTimerDrain |= !inRecoveryMode;
					do
					{
						yield return null;
						var lastEulerAngles = cubeTransform.transform.localEulerAngles;
						var lastRotation = cubeTransform.transform.localRotation;
						var determinedRotation = new[] { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back }[idxRotationPickedCubeLite[curIdxRotation]] * 90;
						var nextRotation = Quaternion.Euler(determinedRotation) * lastRotation;

						for (float t = 0; t < 1f; t += Time.deltaTime / 2f)
						{
							yield return null;
							cubeTransform.localRotation = Quaternion.Lerp(lastRotation, nextRotation, t);
						}
						cubeTransform.localRotation = nextRotation;
						for (float t = 0; !(inRecoveryMode ? recoveryRequestNextStage : requestNextStage) && t < 1f; t += Time.deltaTime / (curIdxRotation == 4 ? 3 : 1))
							yield return null;
						curIdxRotation = (curIdxRotation + 1) % idxRotationPickedCubeLite.Length;
					}
					while (!(inRecoveryMode ? recoveryRequestNextStage : requestNextStage));
					StopCoroutine(curDisplayedValuesCoru);
					for (float t = 1; t > -1f; t -= Time.deltaTime * animSpeedMultiplier)
					{
						cubeTransform.transform.localPosition = new Vector3(storedCubeLocalPos.x, storedCubeLocalPos.y * t, storedCubeLocalPos.z);
						sevenSegmentSet.localPosition = new Vector3(storedSevenSegmentLocalPos.x, storedSevenSegmentLocalPos.y * t, storedSevenSegmentLocalPos.z);
						ledRenderer.transform.localPosition = new Vector3(storedLEDLocalPos.x, storedLEDLocalPos.y * t, storedLEDLocalPos.z);
						yield return null;
					}
					cubeTransform.transform.localPosition = new Vector3(storedCubeLocalPos.x, storedCubeLocalPos.y * -1, storedCubeLocalPos.z);
					sevenSegmentSet.localPosition = new Vector3(storedSevenSegmentLocalPos.x, storedSevenSegmentLocalPos.y * -1, storedSevenSegmentLocalPos.z);
					ledRenderer.transform.localPosition = new Vector3(storedLEDLocalPos.x, storedLEDLocalPos.y * -1, storedLEDLocalPos.z);
					cubeTransform.gameObject.SetActive(false);
					sevenSegmentSet.gameObject.SetActive(false);
					ledRenderer.gameObject.SetActive(false);
					break;
				default:
					QuickLogDebug("This is not supposed to happen.");
					break;
			}
			yield break;
		}
    }
	// Handle revealing the input procedure for Everchanging.
	IEnumerator HandleRevealInputProcedure(InputProcedure procedure)
    {
		showingInputs = true;
		switch (procedure)
        {
			case InputProcedure.MonochromeArrows:
				
                for (float t = -1; t < 1f; t += Time.deltaTime)
                {
					mArrowsCore.transform.localPosition = new Vector3(storedMonochromeArrowsLocalPos.x, storedMonochromeArrowsLocalPos.y * t, storedMonochromeArrowsLocalPos.z);
					yield return null;
                }
				mArrowsCore.transform.localPosition = storedMonochromeArrowsLocalPos;
				do
					yield return null;
				while (inputModeActive);
				for (float t = 1; t > -1f; t -= Time.deltaTime)
				{
					mArrowsCore.transform.localPosition = new Vector3(storedMonochromeArrowsLocalPos.x, storedMonochromeArrowsLocalPos.y * t, storedMonochromeArrowsLocalPos.z);
					yield return null;
				}
				mArrowsCore.transform.localPosition = new Vector3(storedMonochromeArrowsLocalPos.x, storedMonochromeArrowsLocalPos.y * -1, storedMonochromeArrowsLocalPos.z);
				mArrowsCore.gameObject.SetActive(false);
				break;
			case InputProcedure.WireSequences:
				
                for (float t = -1; t < 1f; t += Time.deltaTime)
                {
					wireSequencesCore.transform.localPosition = new Vector3(storedWireSeqLocalPos.x, storedWireSeqLocalPos.y * t, storedWireSeqLocalPos.z);
					yield return null;
                }
				wireSequencesCore.transform.localPosition = storedWireSeqLocalPos;
				do
					yield return null;
				while (inputModeActive);
				for (float t = 1; t > -1f; t -= Time.deltaTime)
				{
					wireSequencesCore.transform.localPosition = new Vector3(storedWireSeqLocalPos.x, storedWireSeqLocalPos.y * t, storedWireSeqLocalPos.z);
					yield return null;
				}
				wireSequencesCore.transform.localPosition = new Vector3(storedWireSeqLocalPos.x, storedWireSeqLocalPos.y * -1, storedWireSeqLocalPos.z);
				wireSequencesCore.gameObject.SetActive(false);
				break;
			default:
				yield break;
        }
		showingInputs = false;

	}

	// Handles boss module handling for Everchanging.
	IEnumerator StartBossModuleHandling()
    {
		yield return null;
		QuickLog(bossModeActive ? "Boss Mode has been activated." : "Exhibition Mode has been activated.");
		QuickLog("Total stages generatable (including early input procedures): {0}", totalPossibleStages);
		if (totalPossibleStages > 0)
		{
			if (!dynamicStageGen)
			{
				for (var x = 0; x < totalPossibleStages + 1; x++)
				{
					curStageIdx++;
					GenerateStage();
				}
				curStageIdx = -1;
			}
			doorAnim.increasingValue = true;
			do
			{
				curStageIdx++;
				if (curStageIdx >= allGeneratedStages.Count && dynamicStageGen)
					GenerateStage();
				requestNextStage = false;
				revealProgress = false;
				allowTimerDrain = false;
				curTimeLeft = timeBase;
				barHandler.curProgress = bossModeActive ? 1f : 0f;
				var curStage = allGeneratedStages[curStageIdx];
                if (curStage.enabledComponents.Any(a => a))
                {
					statusDisplay.text = string.Format("STAGE {0}\n", curStageIdx + 1);
					HandleColorblindModeToggle();
					yield return HandleRevealSelectedStage(curStage, false);
                }
                else
				{
					inputModeActive = true;
					statusDisplay.text = "INPUT\n";
					requiredStageIdxes = Enumerable.Range(idxStartStageInputsAll, curStageIdx - idxStartStageInputsAll).Where(a => !stageIdxCalcsVoids.Contains(a));
					QuickLog("Required stages to input: {0}", requiredStageIdxes.Select(a => a + 1).Join(", "));
					var specifiedCalculatedValues = requiredStageIdxes.Select(a => calculatedValues[a]);
					currentInputSet = rollableInputs.PickRandom();
					switch (currentInputSet)
                    {
						case InputProcedure.MonochromeArrows:
							QuickLog("Monochrome Arrows has been selected as the input procedure for this stage.");
							mArrowsCore.gameObject.SetActive(true);
							mArrowsCore.ResetInstance();
							mArrowsCore.AssignObtainedValues(specifiedCalculatedValues);
							mArrowsCore.MimicLogging(string.Format("[Everchanging #{0}] ", modID));
							for (var x = 0; x < mArrowsCore.arrowRenderers.Length; x++)
                            {
								var directionIdx = mArrowsCore.arrowDirectionIdxes[x];
								var colorIdx = mArrowsCore.arrowColorIdxes[x];
								mArrowsCore.arrowRenderers[x].material.color = Color.white * (colorIdx / 3f) + Color.black * ((3 - colorIdx) / 3f);
                                mArrowsCore.arrowRenderers[x].transform.localRotation = Quaternion.Euler(-90, 180 + 90 * directionIdx, 0);
							}
							QuickLog("Directions of the arrows: {0}", mArrowsCore.arrowDirectionIdxes.Select(a => debugDirections[a]).Join(", "));
							QuickLog("When ordering the colors of the arrows from darkest to brightest, the order is: {0}", Enumerable.Range(1, 4).OrderBy(a => mArrowsCore.arrowColorIdxes[a - 1]).Join(", "));
							QuickLog("Expected buttons to press: {0}", mArrowsCore.expectedPressIdxes.Select(a => a + 1).Join(", "));
							break;
						case InputProcedure.TenDigitKeypad:
							break;
						case InputProcedure.WireSequences:
							break;
                    }
					StartCoroutine(HandleRevealInputProcedure(currentInputSet));
					allowTimerDrain = true;
					do
					{
						yield return null;
					}
					while (!requestNextStage);
					if (inputModeActive)
					{
						currentInputSet = InputProcedure.None;
						QuickLog("The input procedure was not completed!");
						while (showingInputs)
							yield return true;
					}
				}
            }
            while (curStageIdx < totalPossibleStages);
        }
        else if (bossModeActive)
		{
			QuickLog("There were no stages to generate...");
			curStageIdx = 0;
			HandleCompleteInputProcedure();
		}
		yield break;
    }
	void HandleColorblindModeToggle()
	{
		var curStage = allGeneratedStages[curStageIdx];
		foreach (var textRenderer in colorblindTextRenderers)
			textRenderer.enabled = colorblindDetected;
		colorblindLEDText.text = curStage.enabledComponents[0] ? colorNames[curStage.ledColorIdx].Substring(0, 1) : "";
		var decodedStageIdx = Enumerable.Range(0, 3).Select(b => curStage.enabledComponents[b] ? 1 << b : 0).Sum();
		switch (decodedStageIdx)
		{
			case 5:
				colorblindDigitText.text = curStage.fixedRotationIdx.Count() == 1 ? "GREEN" : "WHITE";
				break;
			case 7:
				break;
			default:
				colorblindDigitText.text = "";
				break;
		}
	}

    // Update is called once per frame (based on refresh rate of monitors)
    void Update () {
		if (hasStarted && !moduleSolved)
		{
			if (bossModeActive)
			{
				if (allowTimerDrain)
				{
					if (curTimeLeft > 0f)
						curTimeLeft -= Time.deltaTime;
					else
					{
						var curSolvedNonIgnored = bombInfo.GetSolvedModuleIDs().Where(a => !ignoreIds.Contains(a)).Count();
						if (curStageIdx < curSolvedNonIgnored)
							requestNextStage = true;
					}
					barHandler.curProgress = curTimeLeft / (inputModeActive ? timeBase * 8 : timeBase);
				}
				var ceilTimeLeft = Mathf.CeilToInt(curTimeLeft);
				var timeFormatted = new[] { ceilTimeLeft / 60, ceilTimeLeft % 60 };
				timerDisplay.text = "\n" + timeFormatted.Select(a => a.ToString("00")).Join(":");
			}
			else
				timerDisplay.text = "\nEXHIBITION";
		}
	}
}
