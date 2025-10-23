using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class FootoverScript : MonoBehaviour {

	public TextMesh[] textTiles;
	public KMSelectable[] columnSelectables, rowSelectables;
	public KMSelectable resetSelectable;
	public KMBombModule modSelf;
	public KMAudio mAudio;
	public FootoverSolveAnim solveAnimScript;

	const string base36 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ",
		alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
	static readonly int[] solvedBoardArrangement = Enumerable.Range(0, 36).ToArray();
	int[] curBoard, initialBoard;
	List<string> encodedMoves = new List<string>();
	static int modIDCnt;
	int moduleID;

	bool moduleSolved, shiftBelowRows, shiftRightCols, TPRequestAutosolve;
	void QuickLogDebug(string toLog, params object[] args)
    {
		Debug.LogFormat("<{0} #{1}> {2}", modSelf.ModuleDisplayName, moduleID, string.Format(toLog, args));
    }
	void QuickLog(string toLog, params object[] args)
    {
		Debug.LogFormat("[{0} #{1}] {2}", modSelf.ModuleDisplayName, moduleID, string.Format(toLog, args));
    }

	// Use this for initialization
	void Start() {
		moduleID = ++modIDCnt;
		curBoard = solvedBoardArrangement.ToArray();
		shiftBelowRows = Random.value < 0.5f;
		shiftRightCols = Random.value < 0.5f;
		QuickLog("Shifting a row will also affect the row {0} it.", shiftBelowRows ? "above" : "below");
		QuickLog("Shifting a column will also affect the column to the {0} of it.", shiftRightCols ? "right" : "left");
		var movesToShuffle = 25 + Enumerable.Range(0, 50).Count(a => Random.value < 0.5f);
		ScrambleBoard(movesToShuffle);
		QuickLogDebug("Applied moves in this order: {0}", encodedMoves.Join(", "));
		QuickLog("A total of {0} moves have been performed to shuffle this board. See filtered log for the moves performed.", encodedMoves.Count);
		QuickLog("Initial State:");
        for (var x = 0; x < 6; x++)
			QuickLog("{0}", curBoard.Skip(6 * x).Take(6).Select(a => base36[a]).Join());
		initialBoard = curBoard.ToArray();
		UpdateBoard();

		for (var x = 0; x < columnSelectables.Length; x++)
        {
			var y = x;
			columnSelectables[x].OnInteract += delegate {
				if (!moduleSolved)
                {
					columnSelectables[y].AddInteractionPunch(0.1f);
					mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, columnSelectables[y].transform);
					HandleColumnShift(y % 6, y > 5 ? 35 : 1);
					HandleColumnShift((y + (shiftRightCols ? 1 : 5)) % 6, y > 5 ? 35 : 1);
					UpdateBoard();
					HandleBoardCheck();
                }
				return false;
			};
        }
		for (var x = 0; x < rowSelectables.Length; x++)
        {
			var y = x;
			rowSelectables[x].OnInteract += delegate {
				if (!moduleSolved)
                {
					rowSelectables[y].AddInteractionPunch(0.1f);
					mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, rowSelectables[y].transform);
					HandleRowShift(y % 6, y > 5 ? 35 : 1);
					HandleRowShift((y + (shiftBelowRows ? 1 : 5)) % 6, y > 5 ? 35 : 1);
					UpdateBoard();
					HandleBoardCheck();
                }
				return false;
			};
        }
		resetSelectable.OnInteract += delegate
		{
			if (!moduleSolved)
            {
				resetSelectable.AddInteractionPunch(0.1f);
				mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, resetSelectable.transform);
				curBoard = initialBoard.ToArray();
				UpdateBoard();
			}
			return false;
		};


	}
	
	void ScrambleBoard(int movesToScramble = 0)
    {
		retryScramble:
		encodedMoves.Clear();
		var lastIDx = -1;
		var lastColShift = false;
		for (var x = 0; x < movesToScramble; x++)
        {
			var pickedIDx = Random.Range(0, 6);
			var colShift = Random.value < 0.5f;

			if (pickedIDx == lastIDx && !(lastColShift ^ colShift))
            {
				x--;
				continue;
            }
			lastIDx = pickedIDx;
			lastColShift = colShift;
			var adjacentAffected = (pickedIDx + 1) % 6;
			var amountToAffect = Random.Range(1, 36);
			if (colShift)
            {
				HandleColumnShift(pickedIDx, amountToAffect);
				HandleColumnShift(adjacentAffected, amountToAffect);
				encodedMoves.Add(string.Format("{0}^{1}", new[] { pickedIDx, adjacentAffected }.OrderBy(a => a).Select(b => alphabet[b]).Join(""), amountToAffect));
            }
			else
            {
				HandleRowShift(pickedIDx, amountToAffect);
				HandleRowShift(adjacentAffected, amountToAffect);
				encodedMoves.Add(string.Format("{0}<{1}", new[] { pickedIDx, adjacentAffected }.OrderBy(a => a).Select(b => (b + 1)).Join(""), amountToAffect));
			}
        }
		if (curBoard.SequenceEqual(solvedBoardArrangement))
			goto retryScramble;
    }
    #region Move Handling
    void HandleColumnShift(int colIdx, int amount)
    {
		// Shift the column up by that amount;
		var amountToShiftUp = amount % 6;
		var lastBoard = curBoard.ToArray();
        for (var x = 0; x < 6; x++)
			curBoard[x * 6 + colIdx] = lastBoard[(x + amountToShiftUp) % 6 * 6 + colIdx];
        // Then adjust the values in that column by that amount;
        for (var x = 0; x < 6; x++)
			curBoard[6 * x + colIdx] = (curBoard[6 * x + colIdx] + amount) % 36;
    }
	void HandleRowShift(int rowIdx, int amount)
    {
		// Shift the row left by that amount;
		var amountToShiftLeft = amount % 6;
		var lastBoard = curBoard.ToArray();
        for (var x = 0; x < 6; x++)
			curBoard[rowIdx * 6 + x] = lastBoard[rowIdx * 6 + (x + amountToShiftLeft) % 6];
		// Then adjust the values in that row by that amount;
		var invAmnt = 36 - amount;
        for (var x = 0; x < 6; x++)
			curBoard[6 * rowIdx + x] = (curBoard[6 * rowIdx + x] + invAmnt) % 36;
    }
    #endregion
    void HandleBoardCheck()
    {
		if (curBoard.SequenceEqual(solvedBoardArrangement))
        {
			moduleSolved = true;
			QuickLog("Solved!{0}", !TPRequestAutosolve ? " Give yourself a rest from all of this." : "");
			modSelf.HandlePass();
			if (solveAnimScript != null)
				StartCoroutine(solveAnimScript.StartSolveAnim());
		}
    }

	void UpdateBoard()
    {
		for (var x = 0; x < textTiles.Length; x++)
			textTiles[x].text = base36[curBoard[x]].ToString();
    }

#pragma warning disable 414
	private readonly string TwitchHelpMessage = "\"!{0} AD10 2R5\" [Shifts column A down by 10, then shifts the 2nd row right by 5. Specify rows with numbers from top to bottom (1-6), columns as letters from left to right (A-F). Spaces chain presses of other rows/columns. Loopover/Goofier Game commands supported and can be mixed with each other. (I.E. \"!{0} r1u4 r3lllll AD17\")] | \"!{0} pressspeed/delay 0.1\" [Sets the press speed to 0.1 seconds per interaction. Can be set from 0.0 to 0.5 seconds per interaction.] | \"!{0} reset\" [Resets the board to its initial state.]";
	private float TPPressSpeed = 0.05f;
#pragma warning restore 414
	IEnumerator ProcessTwitchCommand(string cmd)
    {
		if (cmd.ToLowerInvariant() == "reset")
        {
			yield return null;
			resetSelectable.OnInteract();
			yield break;
        }
		var rgxAdjustPressSpeed = Regex.Match(cmd, @"^(pressspeed|delay)\s\d+(\.\d+)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		if (rgxAdjustPressSpeed.Success)
        {
			var possibleDelay = rgxAdjustPressSpeed.Value.Split().Last();
			float newPressSpeed;
			if (!float.TryParse(possibleDelay, out newPressSpeed) || newPressSpeed > 0.5f || newPressSpeed < 0f)
			{
				yield return string.Format("sendtochaterror I cannot set the delay of the presses to this value: {0}", possibleDelay);
				yield break;
			}
			yield return null;
			TPPressSpeed = newPressSpeed;
			yield return string.Format("sendtochat {0}, the press speed for #{2} has now been set to {1} seconds","{0}", possibleDelay, "{1}");
			yield break;
		}
		var splitCmdParts = cmd.Split().Where(a => !string.IsNullOrEmpty(a)).ToList();
		var allDirectionsExpected = new List<KeyValuePair<KMSelectable, int>>();
		foreach (var part in splitCmdParts)
		{
			var rgxShiftCur = Regex.Match(part, @"^[ABCDEF123456][DLRU]\d+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
			var rgxShiftLoopover = Regex.Match(part, @"^[CR][123456][DLRU]\d+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
			var rgxShiftGoofier = Regex.Match(part, @"^[CR][123456][DLRU]+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
			if (rgxShiftCur.Success)
			{ // Current TP Handling
				var matchedVal = rgxShiftCur.Value.ToUpperInvariant();
				var dirIdx = "UDLR".IndexOf(matchedVal[1]);
				var amountPotentially = matchedVal.Substring(2);
				int amountToShift;
				if (!int.TryParse(amountPotentially, out amountToShift) || amountToShift <= 0 || amountToShift > 35)
                {
					yield return string.Format("sendtochaterror I can not shift a row/column by this amount: {0}", amountPotentially);
					yield break;
				}
				if (dirIdx < 2)
                {
					var idxCol = "ABCDEF".IndexOf(matchedVal[0]);
					if (idxCol == -1)
					{
						yield return "sendtochaterror You can not shift a row up or down by any amount! Do you mean to shift a row left to right?";
						yield break;
					}
                    allDirectionsExpected.Add(new KeyValuePair<KMSelectable, int>(columnSelectables[6 * dirIdx + idxCol], amountToShift));
                }
				else
                {
					var idxRow = "123456".IndexOf(matchedVal[0]);
					if (idxRow == -1)
					{
						yield return "sendtochaterror You can not shift a column left or right by any amount! Do you mean to shift a column up or down?";
						yield break;
					}
					allDirectionsExpected.Add(new KeyValuePair<KMSelectable, int>(rowSelectables[6 * (dirIdx - 2) + idxRow], amountToShift));
				}
			}
			else if (rgxShiftLoopover.Success)
            { // Loopover's syntax handling
				var matchedVal = rgxShiftLoopover.Value.ToUpperInvariant();
				var dirIdx = "UDLR".IndexOf(matchedVal[2]);
				var amountPotentially = matchedVal.Substring(3);
				int amountToShift;
				if (!int.TryParse(amountPotentially, out amountToShift) || amountToShift <= 0 || amountToShift > 35)
				{
					yield return string.Format("sendtochaterror I can not shift a row/column by this amount: {0}", amountPotentially);
					yield break;
				}
				if (dirIdx < 2 && matchedVal[0] == 'C')
				{
					var idxCol = "123456".IndexOf(matchedVal[1]);
					allDirectionsExpected.Add(new KeyValuePair<KMSelectable, int>(columnSelectables[6 * dirIdx + idxCol], amountToShift));
				}
				else if (matchedVal[0] == 'R')
				{
					var idxRow = "123456".IndexOf(matchedVal[1]);
					allDirectionsExpected.Add(new KeyValuePair<KMSelectable, int>(rowSelectables[6 * (dirIdx - 2) + idxRow], amountToShift));
				}
				else
                {
					yield return "sendtochaterror You can not shift a column left or right, nor a row up or down, by any amount!";
					yield break;
				}
			}
			else if (rgxShiftGoofier.Success)
			{ // Goofier Game's syntax handling
				var matchedVal = rgxShiftGoofier.Value.ToUpperInvariant();
				var firstDirIdx = "UDLR".IndexOf(matchedVal[2]);
				if (matchedVal[0] == 'C' && firstDirIdx < 2)
				{
					var idxCol = "123456".IndexOf(matchedVal[1]);
					var amountToShift = 1;
					foreach (var dirChr in matchedVal.Substring(3))
                    {
						if ("UDLR".IndexOf(dirChr) != firstDirIdx)
                        {
							yield return "sendtochaterror Only one direction is allowed if you are specifying via Goofier Game's TP handling!";
							yield break;
                        }
						amountToShift++;
                    }
					allDirectionsExpected.Add(new KeyValuePair<KMSelectable, int>(columnSelectables[6 * firstDirIdx + idxCol], amountToShift));
				}
				else if (matchedVal[0] == 'R' && firstDirIdx >= 2)
				{
					var idxRow = "123456".IndexOf(matchedVal[1]);
					var amountToShift = 1;
					foreach (var dirChr in matchedVal.Substring(3))
					{
						if ("UDLR".IndexOf(dirChr) != firstDirIdx)
						{
							yield return "sendtochaterror Only one direction is allowed if you are specifying via Goofier Game's TP handling!";
							yield break;
						}
						amountToShift++;
					}
					allDirectionsExpected.Add(new KeyValuePair<KMSelectable, int>(rowSelectables[6 * (firstDirIdx - 2) + idxRow], amountToShift));
				}
				else
				{
					yield return "sendtochaterror You are attempting to perform an invalid shift on a row or column! Check your command for typos.";
					yield break;
				}
			}
			else
			{
				yield return string.Format("sendtochaterror I do not know what \"{0}\" corresponds to. Check your command for typos.", part);
				yield break;
			}
		}
		if (allDirectionsExpected.Any())
        {
			yield return null;
            for (int y = 0; y < allDirectionsExpected.Count; y++)
            {
                KeyValuePair<KMSelectable, int> directions = allDirectionsExpected[y];
                for (var x = 0; x < directions.Value; x++)
                {
					directions.Key.OnInteract();
					if (TPPressSpeed > 0)
						yield return string.Format("trywaitcancel {0} The command has been canceled after {1} press(es) on interaction #{2} in the command!", TPPressSpeed, x + 1, y + 1);
                }
            }
        }
		yield break;
    }

	IEnumerator TwitchHandleForcedSolve()
    {
		TPRequestAutosolve = true;
		resetSelectable.OnInteract();
		yield return new WaitForSeconds(0.02f);
		var reversedEncodings = encodedMoves.Reverse<string>().ToList();
		foreach (var encoding in reversedEncodings)
        {
			var amountShifted = int.Parse(encoding.Substring(3));
			if (encoding[2] == '^')
            {
				var affectedIdxes = encoding.Substring(0, 2).Select(a => alphabet.IndexOf(a)).ToArray();
				var btnToOffset = affectedIdxes.First(a => affectedIdxes.Contains((a + (shiftRightCols ? 1 : 5)) % 6));
				if (2 * amountShifted < 36)
					for (var x = 0; x < amountShifted; x++)
					{
						columnSelectables[6 + btnToOffset].OnInteract();
						yield return new WaitForSeconds(0.02f);
					}
				else
					for (var x = 0; x < 36 - amountShifted; x++)
					{
						columnSelectables[btnToOffset].OnInteract();
						yield return new WaitForSeconds(0.02f);
					}
			}
			else
            {
				var affectedIdxes = encoding.Substring(0, 2).Select(a => "123456".IndexOf(a)).ToArray();
				var btnToOffset = affectedIdxes.First(a => affectedIdxes.Contains((a + (shiftBelowRows ? 1 : 5)) % 6));
				if (2 * amountShifted < 36)
					for (var x = 0; x < amountShifted; x++)
					{
						rowSelectables[6 + btnToOffset].OnInteract();
						yield return new WaitForSeconds(0.02f);
					}
				else
					for (var x = 0; x < 36 - amountShifted; x++)
					{
						rowSelectables[btnToOffset].OnInteract();
						yield return new WaitForSeconds(0.02f);
					}
			}
        }
    }
}
