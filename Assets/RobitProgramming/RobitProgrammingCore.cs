using KModkit;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using uernd = UnityEngine.Random;
public class RobitProgrammingCore : MonoBehaviour {

	private const int dirU = 0, dirD = 1, dirR = 2, dirL = 3;
    readonly string[] directionReference = { "Up", "Down", "Right", "Left" };

	public Color[] highlightColors, quadrantColors;
	public string[] quadrantColorNames, abbreviatedColorNames;
	public KMAudio mAudio;
	public KMColorblindMode colorblindMode;
	public GridDisplayer gridToDisplay;
	public KMBombModule modSelf;
	public KMBombInfo bombInfo;
	public GameObject delayHandler, movementHandler, botPosition;
    public KMSelectable generateSelectable, playPauseSelectable, bit0, bit1, backspaceArrow;
	public KMSelectable[] deciSecArrows, centSecArrows, terminalArrows;
	public TextMesh bitText, delayCurText, bitMarkerText;

	public MeshRenderer[] quadrantRenderers, quadrantCornerMarkers;
	public TextMesh[] colorblindTextQuadrants;

	private static int moduleIDCnt = 1;

	private int moduleID,
		mazeGenIdx = -1, currentXPos = 4, currentYPos = 4, idxQuadrantModifier = -1,
		commandIdxProcess, commandIdxStartDisplay, mazeGenDelay = 10, lastDirIdx = 0;
	private int[] directions = new int[0], quadrantQuirks = new int[4];
	private Maze generatedMaze;
	private bool mazeDetermined = false, lockMazeGen = false, isRobitRunning = false,
		interactable = false, isHoldingBackspace = false,
		isHoldingWhileInteractable = false, colorblindActive = false;
	string binaryString = "";
	float backspaceTimeHeld = 0f, animPercentage = 0f;
	List<int> collectedCorners = new List<int>();
	Vector3[] storedLocalPositions;
	void Awake()
    {
        try
        {
			colorblindActive = colorblindMode.ColorblindModeActive;
        }
		catch
        {
			colorblindActive = false;
        }
    }
	// Use this for initialization
	void Start () {
		moduleID = moduleIDCnt++;
		storedLocalPositions = quadrantCornerMarkers.Select(a => a.transform.localPosition).Concat(
		quadrantRenderers.Select(a => a.transform.localPosition).Concat(
		new[] { botPosition.transform.localPosition })).ToArray();
		generateSelectable.OnInteract += delegate {
			HandleGenerateButtonPress();
			return false;
		};
		// Adjust the delay on the maze generator.
		for (int x = 0; x < deciSecArrows.Length; x++)
        {
			int y = x;
			deciSecArrows[x].OnInteract += delegate {
				if (!lockMazeGen && interactable)
				{
					mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, deciSecArrows[y].transform);
					deciSecArrows[y].AddInteractionPunch(0.2f);
					mazeGenDelay = Mathf.Max(0, Mathf.Min(100, mazeGenDelay + 10 * (2 * y - 1)));
					delayCurText.text = (mazeGenDelay / 100) + "." + (mazeGenDelay % 100).ToString("00");
				}
				return false;
			};
        }
		for (int x = 0; x < centSecArrows.Length; x++)
		{
			int y = x;
			centSecArrows[x].OnInteract += delegate {
				if (!lockMazeGen && interactable)
				{
					mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, centSecArrows[y].transform);
					centSecArrows[y].AddInteractionPunch(0.2f);
					mazeGenDelay = Mathf.Max(0, Mathf.Min(100, mazeGenDelay + (2 * y - 1)));
					delayCurText.text = (mazeGenDelay / 100) + "." + (mazeGenDelay % 100).ToString("00");
				}
				return false;
			};
		}
		// Controlling the robit and programming the robit
		playPauseSelectable.OnInteract += delegate
		{
			if (interactable)
            {
				if (mazeDetermined)
				{
					if (!lockMazeGen)
					{
						StartCoroutine(UnflipDelayModifier());
						lockMazeGen = true;
						QuickLog("The maze has now been locked into the following:");
						LogMaze();
					}
					if (!isRobitRunning)
					{
						StartCoroutine(HandleDirectionalMovement());
						isRobitRunning = true;
					}
					else
                    {
						isRobitRunning = false;
                    }						
				}
            }
			return false;
		};
		bit0.OnInteract += delegate
		{
			if (interactable)
            {
				if (mazeDetermined && !isRobitRunning)
                {
					mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, bit0.transform);
					bit0.AddInteractionPunch(0.1f);
					binaryString += "0";
					commandIdxStartDisplay = Mathf.Max((11 + binaryString.Length - 144) / 12, 0);
					UpdateBitRenderer(commandIdxStartDisplay * 12);
					commandIdxProcess = 0;
				}
            }
			return false;
		};
		bit1.OnInteract += delegate
		{
			if (interactable)
            {
				if (mazeDetermined && !isRobitRunning)
                {
					mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, bit1.transform);
					bit1.AddInteractionPunch(0.1f);
					binaryString += "1";
					commandIdxStartDisplay = Mathf.Max((11 + binaryString.Length - 144) / 12, 0);
					UpdateBitRenderer(commandIdxStartDisplay * 12);
					commandIdxProcess = 0;
				}
            }
			return false;
		};
		backspaceArrow.OnInteract += delegate
		{
			isHoldingBackspace = true;
			if (interactable)
			{
				if (mazeDetermined)
				{
					mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, backspaceArrow.transform);
					backspaceArrow.AddInteractionPunch(0.1f);
					
					isHoldingWhileInteractable = true;
				}
			}
			return false;
		};
		backspaceArrow.OnInteractEnded += delegate {
			isHoldingBackspace = false;
			if (interactable)
			{
				if (mazeDetermined && isHoldingWhileInteractable && !isRobitRunning)
				{
					if (backspaceTimeHeld >= 2f)
					{
						binaryString = "";
						mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.TitleMenuPressed, backspaceArrow.transform);
					}
					else if (!string.IsNullOrEmpty(binaryString))
					{
						binaryString = binaryString.Substring(0, binaryString.Length - 1);
						mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, backspaceArrow.transform);
					}
					commandIdxStartDisplay = Mathf.Max((11 + binaryString.Length - 144) / 12, 0);
					UpdateBitRenderer(12 * commandIdxStartDisplay);
					commandIdxProcess = 0;
				}
				isHoldingWhileInteractable = false;
			}
			backspaceTimeHeld = 0f;
		};
		// Viewing the display
		for (int x = 0; x < terminalArrows.Length; x++)
		{
			int y = x;
			terminalArrows[x].OnInteract += delegate {
				if (mazeDetermined && interactable)
				{
					mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, terminalArrows[y].transform);
					terminalArrows[y].AddInteractionPunch(0.2f);
					var maxRowIdxOffsetable = Mathf.Max((11 + binaryString.Length - 144) / 12, 0);
					commandIdxStartDisplay = Mathf.Max(0, Mathf.Min(commandIdxStartDisplay + (2 * y - 1), maxRowIdxOffsetable));
					UpdateBitRenderer(commandIdxStartDisplay * 12);
				}
				return false;
			};
		}
		bitText.text = "";
		bitMarkerText.text = "";
		modSelf.OnActivate += delegate
		{
			interactable = true;
			StartCoroutine(FlipDelayModifier());
			QuickLog(string.Format("Given Quadrant Colors in reading order: {0}",
				quadrantQuirks.Select(a => quadrantColorNames.ElementAtOrDefault(a)).Join(", ")));
		};
		//StartCoroutine(HandleSecondarySection());
		// Generate the quadrant quirks
		var rolledRareQuirk = false;
		for (var x = 0; x < quadrantQuirks.Length; x++)
		{
			if (!rolledRareQuirk && uernd.value < 0.05f)
			{
				quadrantQuirks[x] = uernd.Range(4, 7);
				rolledRareQuirk = true;
			}
			else
				quadrantQuirks[x] = uernd.Range(0, 4);
		}
        for (var x = 0; x < quadrantCornerMarkers.Length; x++)
        {
			quadrantCornerMarkers[x].material.color = quadrantColors[quadrantQuirks[x]];
        }
        for (var x = 0; x < quadrantRenderers.Length; x++)
        {
			quadrantRenderers[x].material.color = quadrantColors[quadrantQuirks[x]] * 0.5f;
        }
		UpdateColorblind();
	}
	void UpdateColorblind()
    {
		for (var x = 0; x < colorblindTextQuadrants.Length; x++)
		{
			colorblindTextQuadrants[x].text = colorblindActive ? abbreviatedColorNames[quadrantQuirks[x]] : "";
		}
	}
	void QuickLog(string value)
    {
		Debug.LogFormat("[Robit Programming #{0}] {1}", moduleID, value);
    }
	void LogMaze()
    {
		QuickLog("XXXXXXXXXXX");
		foreach (var aRenderer in gridToDisplay.rowRenderers)
        {
			QuickLog(string.Format("X{0}X", aRenderer.canRender.Select(a => a ? "X" : " ").Join("")));
        }
		QuickLog("XXXXXXXXXXX");
	}
	void UpdateBitRenderer(int offset = 0)
    {
		string valueToDisplay = "";
		for (var x = 0; x < Mathf.Min(binaryString.Length - offset, 144); x++)
        {
			valueToDisplay += (x % 12 == 0 ? "\n" : "") + binaryString[x + offset];
			valueToDisplay = valueToDisplay.Trim();
        }

		bitText.text = valueToDisplay;
		bitMarkerText.text = "";
    }


	void HandleGenerateButtonPress()
    {
		if (lockMazeGen || !interactable) return;
		mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, generateSelectable.transform);
		generateSelectable.AddInteractionPunch();
		if (!mazeDetermined)
        {
			mazeDetermined = true;
			GenerateFirstTimeMaze();
			StartCoroutine(FlipInputSection());
        }
		else
        {
			//StartCoroutine(HandleSecondarySection());
		}
		if (!generatedMaze.GetState())
        {
			StartCoroutine(GenerateMaze());
        }
    }
	IEnumerator FlipDelayModifier()
    {
		yield return null;
        for (float x = 1f; x >= 0; x -= Time.deltaTime)
        {
			yield return null;
			delayHandler.transform.localEulerAngles = Vector3.forward * 180 * x;
        }
		delayHandler.transform.localEulerAngles = Vector3.zero;
	}
	IEnumerator UnflipDelayModifier()
    {
		yield return null;
        for (float x = 0; x <= 1f; x += Time.deltaTime)
        {
			yield return null;
			delayHandler.transform.localEulerAngles = Vector3.forward * 180 * x;
        }
		delayHandler.transform.localEulerAngles = Vector3.forward * 180;
	}
	IEnumerator FlipInputSection()
    {
		yield return null;
		for (float x = 1f; x >= 0; x -= Time.deltaTime)
		{
			yield return null;
			movementHandler.transform.localEulerAngles = Vector3.forward * 180 * x;
		}
		movementHandler.transform.localEulerAngles = Vector3.zero;
	}

	void UpdateVisuals()
    {
		for (int x = 0; x < gridToDisplay.rowRenderers.Length; x++)
		{
			for (int y = 0; y < Mathf.Min(gridToDisplay.rowRenderers[x].wallRenderers.Length, gridToDisplay.rowRenderers[x].canRender.Length); y++)
			{
				/*
				if (gridToDisplay.rowRenderers[x].wallRenderers[y].enabled != gridToDisplay.rowRenderers[x].canRender[y])
					if (x % 2 != 0 || y % 2 != 0)
						if (gridToDisplay.rowRenderers[x].canRender[y])
							mAudio.PlaySoundAtTransform("Plop", transform);
						else
							mAudio.PlaySoundAtTransform("Plip", transform);
				*/
				/*
					else if (mazeDetermined)
						if (gridToDisplay.rowRenderers[x].canRender[y])
							mAudio.PlaySoundAtTransform("273766__n-audioman__pong", transform);
						else
							mAudio.PlaySoundAtTransform("275897__n-audioman__blip", transform);
				*/
				gridToDisplay.rowRenderers[x].wallRenderers[y].enabled = gridToDisplay.rowRenderers[x].canRender[y];
			}

		}
		for (int x = 0; x < generatedMaze.maze.GetLength(0); x++)
			for (int y = 0; y < generatedMaze.maze.GetLength(1); y++)
			{
				if (generatedMaze.GetState())
					gridToDisplay.rowRenderers[y * 2].wallRenderers[x * 2].material.color = generatedMaze.GetCurX() == x && generatedMaze.GetCurY() == y ? highlightColors[1] : generatedMaze.markSpecial[x, y] ? highlightColors[2] : highlightColors[0];
				else
					gridToDisplay.rowRenderers[y * 2].wallRenderers[x * 2].material.color = highlightColors[0];
			}
	}
	IEnumerator UpdateCanRendersMaze()
    {
		yield return null;
		do
		{
			for (int x = 0; x < generatedMaze.maze.GetLength(0); x++)
			{
				for (int y = 0; y < generatedMaze.maze.GetLength(1); y++)
				{
					if (x + 1 < generatedMaze.maze.GetLength(0))
						gridToDisplay.rowRenderers[y * 2].canRender[x * 2 + 1] = !generatedMaze.maze[x, y].Contains("R");

					if (x - 1 >= 0)
						gridToDisplay.rowRenderers[y * 2].canRender[x * 2 - 1] = !generatedMaze.maze[x, y].Contains("L");

					if (y - 1 >= 0)
						gridToDisplay.rowRenderers[y * 2 - 1].canRender[x * 2] = !generatedMaze.maze[x, y].Contains("U");

					if (y + 1 < generatedMaze.maze.GetLength(1))
						gridToDisplay.rowRenderers[y * 2 + 1].canRender[x * 2] = !generatedMaze.maze[x, y].Contains("D");

					gridToDisplay.rowRenderers[y * 2].canRender[x * 2] = generatedMaze.GetState() && ((generatedMaze.GetCurX() == x && generatedMaze.GetCurY() == y) || generatedMaze.markSpecial[x, y]);
				}
			}
			for (int x = 1; x < gridToDisplay.rowRenderers.Length; x += 2)
			{
				for (int y = 1; y < gridToDisplay.rowRenderers[x].canRender.Length; y += 2)
				{
					gridToDisplay.rowRenderers[x].canRender[y] = true;
				}
			}
			for (int x = 1; x < gridToDisplay.rowRenderers.Length; x += 2)
			{
				for (int y = 1; y < gridToDisplay.rowRenderers[x].canRender.Length; y += 2)
				{
					gridToDisplay.rowRenderers[x].canRender[y] =
						gridToDisplay.rowRenderers[x + 1].canRender[y] ||
						gridToDisplay.rowRenderers[x - 1].canRender[y] ||
						gridToDisplay.rowRenderers[x].canRender[y + 1] ||
						gridToDisplay.rowRenderers[x].canRender[y - 1];
				}
			}
			UpdateVisuals();
			yield return null;
		}
		while (generatedMaze.GetState());
	}
	IEnumerator GenerateMaze()
    {
		interactable = false;
		delayCurText.color = Color.gray;
		yield return null;
		generatedMaze.FillMaze();
		generatedMaze.MoveToNewPosition(generatedMaze.GetLength() / 2, generatedMaze.GetWidth() / 2);
		for (int x = 0; x < generatedMaze.maze.GetLength(0); x++)
		{
			for (int y = 0; y < generatedMaze.maze.GetLength(1); y++)
			{
				if (x + 1 < generatedMaze.maze.GetLength(0))
					gridToDisplay.rowRenderers[y * 2].canRender[x * 2 + 1] = !generatedMaze.maze[x, y].Contains("R");

				if (x - 1 >= 0)
					gridToDisplay.rowRenderers[y * 2].canRender[x * 2 - 1] = !generatedMaze.maze[x, y].Contains("L");

				if (y - 1 >= 0)
					gridToDisplay.rowRenderers[y * 2 - 1].canRender[x * 2] = !generatedMaze.maze[x, y].Contains("U");

				if (y + 1 < generatedMaze.maze.GetLength(1))
					gridToDisplay.rowRenderers[y * 2 + 1].canRender[x * 2] = !generatedMaze.maze[x, y].Contains("D");

			}
		}
		for (int x = 1; x < gridToDisplay.rowRenderers.Length; x += 2)
		{
			for (int y = 1; y < gridToDisplay.rowRenderers[x].canRender.Length; y += 2)
			{
				gridToDisplay.rowRenderers[x].canRender[y] =
					gridToDisplay.rowRenderers[x + 1].canRender[y] ||
					gridToDisplay.rowRenderers[x - 1].canRender[y] ||
					gridToDisplay.rowRenderers[x].canRender[y + 1] ||
					gridToDisplay.rowRenderers[x].canRender[y - 1];
			}
		}
		
		for (int x = 0; x < gridToDisplay.rowRenderers.Length; x ++)
		{
			for (int y = 0; y < gridToDisplay.rowRenderers[x].wallRenderers.Length; y++)
			{
				if (gridToDisplay.rowRenderers[x].wallRenderers[y].enabled != gridToDisplay.rowRenderers[x].canRender[y])
				{
					gridToDisplay.rowRenderers[x].wallRenderers[y].enabled = gridToDisplay.rowRenderers[x].canRender[y];
					/*
					if (gridToDisplay.rowRenderers[x].canRender[y])
						mAudio.PlaySoundAtTransform("Plop", transform);
					else
						mAudio.PlaySoundAtTransform("Plip", transform);
					*/
					yield return new WaitForSeconds(.05f);
				}
			}
		}

		StartCoroutine(UpdateCanRendersMaze());
		yield return generatedMaze.AnimateGeneratedMaze(mazeGenDelay / 100f);
		yield return UpdateCanRendersMaze();
		delayCurText.color = Color.white;
		interactable = true;
	}

	readonly string[] mazeAlgorithmNames = {
		"Recursive Backtracking",
		"Binary Tree",
		"Hunt And Kill",
		"Prim's",
		"Kruskal's",
		"Growing Tree",
		"Aldous-Broder",
		"Wilson's",
		"Sidewinder",
		"Recursive Division",
		"Eller's" };
	void GenerateFirstTimeMaze()
    {
		mazeGenIdx = uernd.Range(0, 11);
		//mazeGenIdx = 8; // To Debug.
		switch(mazeGenIdx)
        {
			case 0: // Backtracking Generator
                {
					generatedMaze = new MazeBacktracker(5, 5);
					directions = new int[] { dirU, dirD, dirL, dirR };
					break;
                }
			case 1: // Binary Tree
                {
					bool skewEast = uernd.value < 0.5f;
					bool skewSouth = uernd.value < 0.5f;
					generatedMaze = new MazeBinaryTree(5, 5, skewEast, skewSouth);
					switch (skewEast)
                    {
						case true:
							{
								switch (skewSouth)
								{
									case true:
                                        directions = new int[] { dirR, dirU, dirL, dirD };
										break;
									case false:
										directions = new int[] { dirU, dirR, dirD, dirL };
										break;
								}
								break;
							}
						case false:
							{
								switch (skewSouth)
								{
									case true:
										directions = new int[] { dirL, dirD, dirR, dirU };
										break;
									case false:
										directions = new int[] { dirD, dirL, dirU, dirR };
										break;
								}
								break;
							}
                    }
					break;
                }
			case 2: // Hunt And Kill
                {
					bool randomScan = uernd.value < 0.5f;
					if (randomScan)
                    {
						generatedMaze = new MazeHuntAndKill(5, 5);
						directions = new int[] { dirL, dirR, dirU, dirD };
					}
					else
                    {
						bool vertScan = uernd.value < 0.5f, topToBottom = uernd.value < 0.5f, leftToRight = uernd.value < 0.5f;
						generatedMaze = new MazeHuntAndKill(5, 5, leftToRight, topToBottom, vertScan);
						directions = vertScan ? (new int[] { dirL, dirD, dirU, dirR }) : (new int[] { dirL, dirU, dirD, dirR });
                    }
					break;
                }
			case 3: // Prim's
                {
					generatedMaze = new MazePrims(5, 5);
                    directions = new int[] { dirU, dirL, dirD, dirR };
					break;
                }
			case 4: // Kruskal's
                {
					generatedMaze = new MazeKruskal(5, 5);
                    directions = new int[] { dirU, dirR, dirL, dirD };
					break;
                }
			case 5: // Growing Tree
				{
					generatedMaze = new MazeGrowingTree(5, 5, uernd.Range(1, 6), uernd.Range(0, 3), uernd.Range(1, 6));
					directions = new int[] { dirL, dirR, dirD, dirU };
					break;
				}
			case 6: // Aldous-Broder
				{
					generatedMaze = new MazeAldousBroder(5, 5);
                    directions = new int[] { dirL, dirU, dirR, dirD };
					break;
				}
			case 7: // Wilson's
				{
					generatedMaze = new MazeWilsons(5, 5);
					directions = new int[] { dirD, dirL, dirR, dirU };
					break;
				}
			case 8: // Sidewinder
				{
					bool vertGen = uernd.value < 0.5f,
						topToBottom = uernd.value < 0.5f,
						leftToRight = uernd.value < 0.5f;

					generatedMaze = new MazeSidewinder(5, 5, leftToRight, topToBottom, vertGen);
					directions = vertGen ?
						leftToRight ? new int[] { dirR, dirD, dirL, dirU } : new int[] { dirR, dirD, dirU, dirL }
						:
						topToBottom ? new int[] { dirD, dirR, dirL, dirU } : new int[] { dirD, dirR, dirU, dirL }
						;
					break;
				}
			case 9: // Recursive Division
				{
					generatedMaze = new MazeRecursiveDivision(5, 5);
					directions = new int[] { dirD, dirU, dirR, dirL };
					break;
				}
			case 10: // Eller's
				{
					bool vertGen = uernd.value < 0.5f, topToBottom = uernd.value < 0.5f, leftToRight = uernd.value < 0.5f;

					generatedMaze = new MazeEllers(5, 5, leftToRight, topToBottom, vertGen);
					directions = vertGen ? (new int[] { dirD, dirU, dirL, dirR }) : (new int[] { dirU, dirD, dirR, dirL });
                    break;
				}
			default:
				{
					generatedMaze = new Maze(5, 5);
                    directions = new int[] { -1, -1, -1, -1 };
					break;
				}
        }
		QuickLog(string.Format("The maze the module has selected to generate uses this algorithm: {0}", mazeGenIdx < mazeAlgorithmNames.Length ? mazeAlgorithmNames[mazeGenIdx] : "unknown"));
		QuickLog(string.Format("This gives the starting directions: [ {0} ]", directions.Select(a => directionReference.ElementAtOrDefault(a)).Join(", ")));
	}
	void UpdateBinaryStringWithMarker(int offset = 0, int markIdxPairStart = 0)
    {
		string valueToDisplay = "", secondaryValueToDisplay = "";
		for (var x = 0; x < Mathf.Min(binaryString.Length - offset, 144); x++)
		{
			valueToDisplay += (x % 12 == 0 ? "\n" : "") + ((markIdxPairStart == x + offset || markIdxPairStart + 1 == x + offset) ? ' ' : binaryString[x + offset]);
			secondaryValueToDisplay += (x % 12 == 0 ? "\n" : "") + ((markIdxPairStart == x + offset || markIdxPairStart + 1 == x + offset) ? binaryString[x + offset] : ' ');
			if (x == 0)
			{
				valueToDisplay = valueToDisplay.Replace("\n", "");
				secondaryValueToDisplay = secondaryValueToDisplay.Replace("\n", "");
			}
		}

		bitText.text = valueToDisplay;
		bitMarkerText.text = secondaryValueToDisplay;
	}
	IEnumerator HandleNewRotation(int idx)
	{
		var rotationIdxes = new[] { 0, 180, 90, -90 };
		var lastRotation = botPosition.transform.localRotation;
		var modifiedRotation = Quaternion.Euler(0, rotationIdxes[idx], 0);
		for (float x = 0; x < 1f; x += 15 * Time.deltaTime)
        {
			yield return null;
			botPosition.transform.localRotation = Quaternion.Lerp(lastRotation, modifiedRotation, x);
		}
		botPosition.transform.localRotation = modifiedRotation;

	}
	IEnumerator HandleDirectionalMovement()
    {
		interactable = false;
		var readableString = binaryString.Substring(0, binaryString.Length / 2 * 2);
		var allDirections = new List<int>();
		var requireStrike = false;
        for (var x = 0; x < readableString.Length; x += 2)
        {
			var curSection = readableString.Substring(x, 2);
            switch (curSection)
            {
				case "00":
					allDirections.Add(directions[0]);
					break;
				case "01":
					allDirections.Add(directions[1]);
					break;
				case "10":
					allDirections.Add(directions[2]);
					break;
				case "11":
					allDirections.Add(directions[3]);
					break;
				default:
					allDirections.Add(-1);
					break;
            }
        }
		if (commandIdxProcess == 0)
		{
			QuickLog(string.Format("The robit has started processing commands at column {0}, row {1} (0-indexed at TL)", currentXPos, currentYPos));
			QuickLog(string.Format("And the robit has decided to process the following string, {0}", readableString));
			QuickLog(string.Format("Into these directions before the quadrant quirks: [{0}]", allDirections.Select(a => directionReference.ElementAt(a)).Join(", ")));
		}
		else
        {
			QuickLog(string.Format("The robit has resumed processing commands at column {0}, row {1} (0-indexed at TL)", currentXPos, currentYPos));
		}
		yield return null;
		var stopRobitForcefully = false;
		while (commandIdxProcess < allDirections.Count)
		{
			UpdateBinaryStringWithMarker(commandIdxStartDisplay, 2 * commandIdxProcess);

			var wantedDirection = allDirections[commandIdxProcess];
			idxQuadrantModifier = -1;
			if (currentXPos > 4 && currentYPos > 4)
				idxQuadrantModifier = collectedCorners.Contains(3) ? -1 : quadrantQuirks[3];
			else if (currentXPos < 4 && currentYPos > 4)
				idxQuadrantModifier = collectedCorners.Contains(2) ? -1 : quadrantQuirks[2];
			else if (currentXPos > 4 && currentYPos < 4)
				idxQuadrantModifier = collectedCorners.Contains(1) ? -1 : quadrantQuirks[1];
			else if (currentXPos < 4 && currentYPos < 4)
				idxQuadrantModifier = collectedCorners.Contains(0) ? -1 : quadrantQuirks[0];

			if (lastDirIdx != wantedDirection)
            {
				lastDirIdx = wantedDirection;
				StartCoroutine(HandleNewRotation(wantedDirection));
            }

			switch (idxQuadrantModifier)
			{
				case 0:
					wantedDirection = wantedDirection == dirD ? dirU :
						wantedDirection == dirR ? dirL :
						wantedDirection == dirL ? dirR :
						wantedDirection == dirU ? dirD :
						-1;
					break;
				case 1:
					wantedDirection = commandIdxProcess % 4 == 0 ? -1 : wantedDirection;
					break;
				case 2:
					if (commandIdxProcess % 2 == 0) goto case 0;
					break;
				case 3:
					{
						var serialNo = bombInfo.GetSerialNumber();
						if (char.IsDigit(serialNo[commandIdxProcess % 6])) goto case 0;
					}
					break;
				case 4:
					switch (commandIdxProcess % 4)
					{
						case 1:
							wantedDirection = wantedDirection == dirD ? dirL :
							wantedDirection == dirR ? dirD :
							wantedDirection == dirL ? dirU :
							wantedDirection == dirU ? dirR :
								-1;
							break;
						case 2:
							wantedDirection = wantedDirection == dirD ? dirU :
							wantedDirection == dirR ? dirL :
							wantedDirection == dirL ? dirR :
							wantedDirection == dirU ? dirD :
							-1;
							break;
						case 3:
							wantedDirection = wantedDirection == dirD ? dirR :
							wantedDirection == dirR ? dirU :
							wantedDirection == dirL ? dirD :
							wantedDirection == dirU ? dirL :
							-1;
							break;
					}
					break;
				case 5:
					if (commandIdxProcess % 10 == 0) stopRobitForcefully = true;
					break;
				case 6:
                    {
						var commandModif = "X**--**-*-*-**-";
						if (commandModif[commandIdxProcess % 15] == 'X') stopRobitForcefully = true;
						else if (commandModif[commandIdxProcess % 15] == '-') goto case 0;
					}
					break;
				default:
					break;
            }

			var oldPos = botPosition.transform.localPosition;
			mAudio.PlaySoundAtTransform("step_quieter", botPosition.transform);
			switch (wantedDirection)
			{
				case dirU:
					{
						if (currentYPos > 0 && !gridToDisplay.rowRenderers[currentYPos - 1].canRender[currentXPos])
						{
							for (float y = 0; y < 1f; y += 4 * Time.deltaTime)
							{
								botPosition.transform.localPosition = oldPos + Vector3.forward * y;
								yield return null;
							}
							currentYPos--;
							goto default;
						}
						else
						{
							QuickLog("That's a wall above the robit. The robit has crashed into it again.");
							for (float y = 0; y < 1f; y += 4 * Time.deltaTime)
							{
								botPosition.transform.localPosition = oldPos + Vector3.forward * 0.1f * (0.5f - Mathf.Abs(y - 0.5f));
								yield return null;
							}
							requireStrike = true;
							stopRobitForcefully = true;
							break;
						}
					}
				case dirL:
					{
						if (currentXPos > 0 && !gridToDisplay.rowRenderers[currentYPos].canRender[currentXPos - 1])
						{
							for (float y = 0; y < 1f; y += 4 * Time.deltaTime)
							{
								botPosition.transform.localPosition = oldPos + Vector3.left * y;
								yield return null;
							}
							currentXPos--;
							goto default;
						}
						else
						{
							QuickLog("That's a wall to the left of the robit. The robit has crashed into it again.");
							for (float y = 0; y < 1f; y += 4 * Time.deltaTime)
							{
								botPosition.transform.localPosition = oldPos + Vector3.left * 0.1f * (0.5f - Mathf.Abs(y - 0.5f));
								yield return null;
							}
							requireStrike = true;
							stopRobitForcefully = true;
							break;
						}
					}
				case dirR:
					{
						if (currentXPos < 8 && !gridToDisplay.rowRenderers[currentYPos].canRender[currentXPos + 1])
						{
							for (float y = 0; y < 1f; y += 4 * Time.deltaTime)
							{
								botPosition.transform.localPosition = oldPos + Vector3.right * y;
								yield return null;
							}
							currentXPos++;
							goto default;
						}
						else
						{
							QuickLog("That's a wall to the right of the robit. The robit has crashed into it again.");
							for (float y = 0; y < 1f; y += 4 * Time.deltaTime)
							{
								botPosition.transform.localPosition = oldPos + Vector3.right * 0.1f * (0.5f - Mathf.Abs(y - 0.5f));
								yield return null;
							}
							requireStrike = true;
							stopRobitForcefully = true;
							break;
						}
					}
				case dirD:
                    {
						if (currentYPos < 8 && !gridToDisplay.rowRenderers[currentYPos + 1].canRender[currentXPos])
                        {
							
							for (float y = 0; y < 1f; y += 4 * Time.deltaTime)
                            {
								botPosition.transform.localPosition = oldPos + Vector3.back * y;
								yield return null;
                            }
							currentYPos++;
							goto default;
						}
						else
                        {
							QuickLog("That's a wall directly below the robit. The robit has crashed into it again.");
							for (float y = 0; y < 1f; y += 4 * Time.deltaTime)
							{
								botPosition.transform.localPosition = oldPos + Vector3.back * 0.1f * (0.5f - Mathf.Abs(y - 0.5f));
								yield return null;
							}
							requireStrike = true;
							stopRobitForcefully = true;
							break;
                        }
                    }
				case -1:
					yield return new WaitForSeconds(0.25f);
					goto default;
				default:
					commandIdxProcess++;
					break;
			}
			botPosition.transform.localPosition = new Vector3(currentXPos - 4, 0, 4 - currentYPos);
			if (collectedCorners.Contains(0) && collectedCorners.Contains(1)
				&& collectedCorners.Contains(2) && collectedCorners.Contains(3) && (currentXPos == 4 || currentYPos == 4))
			{
				QuickLog("The robit has reached the center row/column. You're done.");
				StartCoroutine(HandleSolveAnim());
				yield break;
			}
			if (stopRobitForcefully) {
				QuickLog(string.Format("The robit has stopped on index {0} on the pair of binary bits currently processed.", commandIdxProcess));
				if (!requireStrike)
					mAudio.PlaySoundAtTransform("strike", botPosition.transform);
				break;
			}
			if (!collectedCorners.Contains(0) && currentXPos == 0 && currentYPos == 0)
            {
				QuickLog("Successfully collected the TL cube. The TL quirk will now stop applying.");
				collectedCorners.Add(0);
				yield return DisappearQuadrantSection(0);
            }
			else if (!collectedCorners.Contains(1) && currentXPos == 8 && currentYPos == 0)
            {
				QuickLog("Successfully collected the TR cube. The TR quirk will now stop applying.");
				collectedCorners.Add(1);
				yield return DisappearQuadrantSection(1);
            }
			else if (!collectedCorners.Contains(2) && currentXPos == 0 && currentYPos == 8)
            {
				QuickLog("Successfully collected the BL cube. The BL quirk will now stop applying.");
				collectedCorners.Add(2);
				yield return DisappearQuadrantSection(2);
            }
			else if (!collectedCorners.Contains(3) && currentXPos == 8 && currentYPos == 8)
            {
				QuickLog("Successfully collected the BR cube. The BR quirk will now stop applying.");
				collectedCorners.Add(3);
				yield return DisappearQuadrantSection(3);
            }
			yield return null;
		}
		QuickLog(string.Format("The robit is now in column {0}, row {1} (0-indexed at TL)", currentXPos, currentYPos));
		if (allDirections.Count <= commandIdxProcess && (
			(currentXPos < 4 && currentYPos < 4 && !collectedCorners.Contains(0)) ||
			(currentXPos > 4 && currentYPos < 4 && !collectedCorners.Contains(1)) ||
			(currentXPos < 4 && currentYPos > 4 && !collectedCorners.Contains(2)) ||
			(currentXPos > 4 && currentYPos > 4 && !collectedCorners.Contains(3))
			))
        {
			QuickLog("The robit has ran out of commands to process while in an active quadrant. The robit does not like that...");
			requireStrike = true;
        }
		if (requireStrike)
        {
			modSelf.HandleStrike();
			mAudio.PlaySoundAtTransform("strike", botPosition.transform);
		}
		interactable = true;
		isRobitRunning = false;
    }
	IEnumerator DisappearQuadrantSection(int idx)
	{
		if (idx < 0 || idx >= 4) yield break;
		var lastColor = quadrantRenderers[idx].material.color;
        for (float x = 1f; x > 0; x -= 5 * Time.deltaTime)
		{
			quadrantCornerMarkers[idx].transform.localScale = Vector3.one * 0.5f * x;
			quadrantRenderers[idx].material.color = lastColor * x;
			colorblindTextQuadrants[idx].color = Color.white * x;
			yield return null;
		}
		quadrantCornerMarkers[idx].transform.localScale = Vector3.zero;
		quadrantRenderers[idx].enabled = false;
		colorblindTextQuadrants[idx].color = Color.clear;
		colorblindTextQuadrants[idx].text = "";
	}



	IEnumerator HandleSecondarySection()
    {

        for (float x = 0; x < 1f; x += 5 * Time.deltaTime)
        {
            for (var u = 0; u < quadrantCornerMarkers.Length; u++)
            {
				quadrantCornerMarkers[u].transform.localPosition = storedLocalPositions[u] + (Vector3.down * x);
            }
			for (var u = 0; u < quadrantRenderers.Length; u++)
			{
				quadrantRenderers[u].transform.localPosition = storedLocalPositions[u + 4] + (Vector3.down * x);
			}
			botPosition.transform.localPosition = storedLocalPositions.Last() + (Vector3.down * x);
			yield return null;
		}
		for (var u = 0; u < quadrantCornerMarkers.Length; u++)
		{
			quadrantCornerMarkers[u].transform.localPosition = storedLocalPositions[u] + Vector3.down;
		}
		for (var u = 0; u < quadrantRenderers.Length; u++)
		{
			quadrantRenderers[u].transform.localPosition = storedLocalPositions[u + 4] + Vector3.down;
		}
		botPosition.transform.localPosition = storedLocalPositions.Last() + Vector3.down;
		do
			yield return null;
		while (!mazeDetermined || !interactable);
			

		for (float x = 1f; x >= 0; x -= 5 * Time.deltaTime)
		{
			for (var u = 0; u < quadrantCornerMarkers.Length; u++)
			{
				quadrantCornerMarkers[u].transform.localPosition = storedLocalPositions[u] + (Vector3.down * x);
			}
			for (var u = 0; u < quadrantRenderers.Length; u++)
			{
				quadrantRenderers[u].transform.localPosition = storedLocalPositions[u + 4] + (Vector3.down * x);
			}
			botPosition.transform.localPosition = storedLocalPositions.Last() + (Vector3.down * x);
			yield return null;
		}
		for (var u = 0; u < quadrantCornerMarkers.Length; u++)
		{
			quadrantCornerMarkers[u].transform.localPosition = storedLocalPositions[u];
		}
		for (var u = 0; u < quadrantRenderers.Length; u++)
		{
			quadrantRenderers[u].transform.localPosition = storedLocalPositions[u + 4];
		}
		botPosition.transform.localPosition = storedLocalPositions.Last();
	}
	IEnumerator AnimateClearDisplay()
    {
		mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, backspaceArrow.transform);
		backspaceArrow.AddInteractionPunch(0.1f);
		yield return null;
		bitText.text = "";
		bitMarkerText.text = "";
		mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.TitleMenuPressed, backspaceArrow.transform);
		var bitReshow = "0110101111001001010101100001010010001001100110000110010011010010100100";
        for (var x = 0; x < bitReshow.Length && mazeDetermined; x++)
        {
			binaryString = bitReshow.Substring(0, x + 1);
			commandIdxStartDisplay = Mathf.Max((11 + binaryString.Length - 144) / 12, 0);
			if (bitReshow[x] == '0')
			{
				mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, bit0.transform);
				bit0.AddInteractionPunch(0.1f);
			}
			else if (bitReshow[x] == '1')
			{
				mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, bit1.transform);
				bit1.AddInteractionPunch(0.1f);
			}
			UpdateBitRenderer();
			yield return null;
        }
	}
	IEnumerator HandleFloatDownAnim(Transform selectedObject, Vector3 deltaModifier, float delay = 0f)
    {
		yield return new WaitForSecondsRealtime(delay);
		for (float x = 0; x < 1f; x += Time.deltaTime * 9)
		{
			selectedObject.localPosition += deltaModifier * Time.deltaTime;
			yield return null;
		}
		selectedObject.localPosition = deltaModifier;
	}

	IEnumerator HandleSolveAnim()
    {
		modSelf.HandlePass();
		mAudio.PlaySoundAtTransform("hiss", transform);
		yield return null;
		StartCoroutine(AnimateClearDisplay());
        for (int i = 0; i < gridToDisplay.rowRenderers.Length; i++) 
        {
            RowRenderers renderers = gridToDisplay.rowRenderers[i];
            for (int c = 0; c < renderers.wallRenderers.Length; c++)
			{
                MeshRenderer wallRender = renderers.wallRenderers[c];
				StartCoroutine(HandleFloatDownAnim(wallRender.transform, Vector3.down, (Mathf.Abs(i - currentYPos) + Mathf.Abs(c - currentXPos)) / 8f));
			}
		}
	}
	void Update()
    {
		if (isHoldingBackspace && backspaceTimeHeld < 6f)
			backspaceTimeHeld += Time.deltaTime;

        if (mazeDetermined && interactable || lockMazeGen)
        {
            animPercentage = Mathf.Max(0, animPercentage - Time.deltaTime * 5);
        }
        else
        {
            animPercentage = Mathf.Min(1, animPercentage + Time.deltaTime * 5);
        }
		if (!lockMazeGen)
		{
			if (storedLocalPositions != null)
			{
				for (var u = 0; u < quadrantCornerMarkers.Length; u++)
				{
					quadrantCornerMarkers[u].transform.localPosition = storedLocalPositions[u] + (Vector3.down * animPercentage);
				}
				for (var u = 0; u < quadrantRenderers.Length; u++)
				{
					quadrantRenderers[u].transform.localPosition = storedLocalPositions[u + 4] + (Vector3.down * animPercentage);
				}
				botPosition.transform.localPosition = storedLocalPositions.Last() + (Vector3.down * animPercentage);
			}
		}
	}


#pragma warning disable IDE0051 // Remove unused private members
	private readonly string TwitchHelpMessage = "Generate at the current rate with \"!{0} generate/create\", or with a specific delay with \"!{0} generate/create at/on X.XX\". (.0 - 1.0 only) " +
		"Generate at the current rate and focus the module with \"!{0} generate/create focus #\" where # is optional on how many times the user wants to generate the maze." +
		"Type in 0/1 bits with \"!{0} type/enter/input 0001101010...\". Delete the previous X bits with \"!{0} delete X\", or clear all the bits with \"!{0} clear\" or \"!{0} delete all\" " +
		"Play the entire command or continue where it left off with \"!{0} start/play\" Scroll the terminal up or down with \"!{0} scroll up/down\"";
#pragma warning restore IDE0051 // Remove unused private members
	void TwitchHandleForcedSolve()
    {
		interactable = false;
		StartCoroutine(HandleSolveAnim());
		if (!lockMazeGen) StartCoroutine(UnflipDelayModifier());
		if (!collectedCorners.Contains(0)) StartCoroutine(DisappearQuadrantSection(0));
		if (!collectedCorners.Contains(1)) StartCoroutine(DisappearQuadrantSection(1));
		if (!collectedCorners.Contains(2)) StartCoroutine(DisappearQuadrantSection(2));
		if (!collectedCorners.Contains(3)) StartCoroutine(DisappearQuadrantSection(3));
	}

	IEnumerator ProcessTwitchCommand(string cmd)
    {
		Match cmdColorblind = Regex.Match(cmd, @"^colou?rblind$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		if (cmdColorblind.Success)
        {
			yield return null;
			colorblindActive = !colorblindActive;
			UpdateColorblind();
        }
		if (!interactable)
        {
			yield return "sendtochaterror The module is not allowing certain commands at this moment. Wait a bit until the module is able to accept these commands.";
			yield break;
        }
		Match cmdSetGenerateSpecifiedRate = Regex.Match(cmd, @"^(create|generate)\s((at|on)\s)?\d?\.\d{1,2}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
			cmdGenerate = Regex.Match(cmd, @"^(create|generate)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
			cmdGenerateFocus = Regex.Match(cmd, @"^(create|generate)\s?focus(\s\d+)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
			cmdAddBits = Regex.Match(cmd, @"^((type|input|enter)\s)?[01\s]+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
			cmdDeleteAllBits = Regex.Match(cmd, @"^(clear|delete\sall)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
			cmdDeleteXBits = Regex.Match(cmd, @"^delete\s\d+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
			cmdActivateRobit = Regex.Match(cmd, @"^(start|play)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
			cmdScroll = Regex.Match(cmd, @"^scroll\s(up?|d(own)?)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		
		if (cmdSetGenerateSpecifiedRate.Success)
        {
			if (lockMazeGen)
            {
				yield return "sendtochaterror The maze has been locked in place. You can no longer generate a maze.";
				yield break;
            }
			var cmdSets = cmdSetGenerateSpecifiedRate.Value.Split();
			var portionTimer = cmdSets.Last();

			if (portionTimer.RegexMatch(@"^\d?\.\d$"))
            {
				portionTimer += "0";
            }
			int timeSpecified = 0;
			if (!int.TryParse(portionTimer.Replace(".",""), out timeSpecified) || timeSpecified < 0 || timeSpecified > 100)
            {
				yield return string.Format("sendtochaterror I am not setting the generation delay to this value: {0}", portionTimer);
				yield break;
			}
			while (mazeGenDelay != timeSpecified)
            {
				yield return null;
				if (mazeGenDelay > timeSpecified)
                {
					if (mazeGenDelay - timeSpecified >= 10)
						deciSecArrows[0].OnInteract();
					else
						centSecArrows[0].OnInteract();
                }
				else
                {
					if (timeSpecified - mazeGenDelay >= 10)
						deciSecArrows[1].OnInteract();
					else
						centSecArrows[1].OnInteract();
				}
				yield return "trywaitcancel 0.1 The maze generation command has been canceled viva request.";
            }
			yield return null;
			generateSelectable.OnInteract();
		}
		else if (cmdGenerate.Success)
        {
			if (lockMazeGen)
			{
				yield return "sendtochaterror The maze has been locked in place. You can no longer generate a maze.";
				yield break;
			}
			yield return null;
			generateSelectable.OnInteract();
		}
		else if (cmdGenerateFocus.Success)
        {
			if (lockMazeGen)
			{
				yield return "sendtochaterror The maze has been locked in place. You can no longer generate a maze and focus on the module.";
				yield break;
			}
			var lastNum = cmdGenerateFocus.Value.Split().Last();
			int repeatCount;
			if (!int.TryParse(lastNum, out repeatCount) || repeatCount <= 0)
            {
				yield return string.Format("sendtochaterror I am not generating the maze this many times: {0}. It's too much/little or the number is not a number at all.", lastNum);
				yield break;
			}
			for (var x = 0; x < repeatCount; x++)
			{
				yield return null;
				generateSelectable.OnInteract();
				do
				{
					yield return "trycancel Focusing on the module has been canceled viva commands!";
				}
				while (generatedMaze.GetState() || !interactable);
			}
		}
		else if (cmdAddBits.Success)
        {
			if (!mazeDetermined)
            {
				yield return "sendtochaterror The terminal is not accessible right now. Generate a maze first and wait until you are able to access the terminal.";
				yield break;
			}
			var allBitsDetermined = cmdAddBits.Value.Split().Where(a => a.All(b => "01".Contains(b)));
			if (!allBitsDetermined.Any())
            {
				yield return "sendtochaterror There are no bits to input in the command provided.";
				yield break;
			}
			foreach (string allBinaryBits in allBitsDetermined)
            {
				foreach (char aBit in allBinaryBits)
				{
					switch (aBit)
                    {
						case '0':
							yield return null;
							bit0.OnInteract();
							break;
						case '1':
							yield return null;
							bit1.OnInteract();
							break;
					}
					yield return "trywaitcancel 0.1";
				}
			}
        }
		else if (cmdDeleteAllBits.Success)
        {
			if (!mazeDetermined)
			{
				yield return "sendtochaterror The terminal is not accessible right now. Generate a maze first and wait until you are able to access the terminal.";
				yield break;
			}
			yield return null;
			yield return backspaceArrow;
			while (backspaceTimeHeld < 2f)
				yield return "trycancel";
			yield return backspaceArrow;
		}
		else if (cmdDeleteXBits.Success)
        {
			if (!mazeDetermined)
			{
				yield return "sendtochaterror The terminal is not accessible right now. Generate a maze first and wait until you are able to access the terminal.";
				yield break;
			}
			var valueProvided = cmdDeleteXBits.Value.Split().Last();
			int deleteCount = 0;
			if (!int.TryParse(valueProvided,out deleteCount) || deleteCount < 0 || deleteCount > binaryString.Length)
            {
				yield return string.Format("sendtochaterror I am not deleting this many bits on the terminal: {0}. There are either not enough bits to warrent this or an invalid number specified.", valueProvided);
				yield break;
			}
			for (var x = 0; x < deleteCount; x++)
            {
				yield return null;
				yield return backspaceArrow;
				yield return backspaceArrow;
				yield return "trywaitcancel 0.1";
			}
        }
		else if (cmdActivateRobit.Success)
        {
			if (!mazeDetermined)
			{
				yield return "sendtochaterror The terminal is not accessible right now. Generate a maze first and wait until you are able to access the terminal.";
				yield break;
			}
			yield return null;
			playPauseSelectable.OnInteract();
			yield return "strike";
			yield return "solve";
		}
		else if (cmdScroll.Success)
        {
			if (!mazeDetermined)
			{
				yield return "sendtochaterror The terminal is not accessible right now. Generate a maze first and wait until you are able to access the terminal.";
				yield break;
			}
			switch (cmdScroll.Value.Split().Last().ToLower())
            {
				case "up":
				case "u":
					yield return null;
					terminalArrows[0].OnInteract();
					break;
				case "d":
				case "down":
					yield return null;
					terminalArrows[1].OnInteract();
					break;
            }
		}

		yield break;
    }

}
