using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MonochromeArrowsCore : MonoBehaviour {

    public KMSelectable[] arrowSelectables;
	public MeshRenderer[] arrowRenderers;
	public List<int> obtainedValues, expectedPressIdxes;
    public int[] arrowDirectionIdxes, arrowColorIdxes;
    public int currentInputIdx;
	readonly int[,] latinSquareTable = new int[,]
		{
            { 4, 5, 2, 1, 0, 7, 3, 6, },
            { 0, 3, 7, 5, 1, 6, 4, 2, },
            { 5, 6, 4, 3, 7, 0, 2, 1, },
            { 6, 7, 1, 4, 5, 2, 0, 3, },
            { 7, 1, 6, 2, 4, 3, 5, 0, },
            { 1, 0, 3, 7, 2, 5, 6, 4, },
            { 2, 4, 0, 6, 3, 1, 7, 5, },
            { 3, 2, 5, 0, 6, 4, 1, 7, },
        };
    List<string> itemsToLog = new List<string>();
    public void MimicLogging(string startText)
    {
        foreach (string anItem in itemsToLog)
            Debug.LogFormat("{0} {1}", startText, anItem);
    }
    public void AssignObtainedValues(IEnumerable<int> calculatedValues, int totalInputs = 1)
	{
		ResetInstance();
		obtainedValues = calculatedValues.ToList();
        var adjustedValues = obtainedValues.Select(a => (a + totalInputs) % 10);
        itemsToLog.Add(string.Format("After adjustment: {0}", adjustedValues.Join(", ")));
        arrowColorIdxes = Enumerable.Range(0, 4).ToArray().Shuffle();
        arrowDirectionIdxes = Enumerable.Range(0, 4).ToArray().Shuffle();
        var curColIdx = adjustedValues.Count(a => a == 9) % 8;
        var curRowIdx = adjustedValues.Count(a => a == 0) % 8;
        itemsToLog.Add(string.Format("Starting on {0},{1} where 0,0 is the top-left of the 8x8 grid, in row,col format.",curRowIdx,curColIdx));
        var traitsIdxAll = new List<int>();
        var non0s_9s = adjustedValues.Where(a => a != 9 && a != 0);
        itemsToLog.Add(string.Format("Remaining non-zero, non-nine adjusted numbers: {0}", non0s_9s.Join(", ")));
        var curDirectionIdx = -1;
        for (var x = 0; x < non0s_9s.Count(); x++)
        {
            var itemDirIdx = non0s_9s.ElementAt(x);
            if (curDirectionIdx != itemDirIdx)
            {
                itemsToLog.Add(string.Format("{0} Noting down the letter on {1},{2}.", curDirectionIdx == -1 ? "This is the first direction to move." : "Switching directions.", curRowIdx, curColIdx));
                curDirectionIdx = itemDirIdx;
                traitsIdxAll.Add(latinSquareTable[curRowIdx, curColIdx]);
            }
            switch (curDirectionIdx)
            {
                case 1:
                    curColIdx = (curColIdx + 7) % 8;
                    curRowIdx = (curRowIdx + 7) % 8;
                    break;
                case 2:
                    curRowIdx = (curRowIdx + 7) % 8;
                    break;
                case 3:
                    curColIdx = (curColIdx + 1) % 8;
                    curRowIdx = (curRowIdx + 7) % 8;
                    break;
                case 4:
                    curColIdx = (curColIdx + 7) % 8;
                    break;
                case 5:
                    curColIdx = (curColIdx + 1) % 8;
                    break;
                case 6:
                    curColIdx = (curColIdx + 7) % 8;
                    curRowIdx = (curRowIdx + 1) % 8;
                    break;
                case 7:
                    curRowIdx = (curRowIdx + 1) % 8;
                    break;
                case 8:
                    curColIdx = (curColIdx + 1) % 8;
                    curRowIdx = (curRowIdx + 1) % 8;
                    break;
                default:
                    break;
            }
        }
        traitsIdxAll.Add(latinSquareTable[curRowIdx, curColIdx]);
        itemsToLog.Add(string.Format("Noting down the letter on {0},{1}.", curRowIdx, curColIdx));
        itemsToLog.Add(string.Format("This gives the traits to scan for: {0}", traitsIdxAll.Select(a => "ABCDEFGH"[a]).Join()));
        // Idxes 0-3 reference colors, 4-7 reference directions
        for (var x = 0; x < traitsIdxAll.Count; x++)
        {
            var curTraitIdx = traitsIdxAll[x];
            switch (curTraitIdx)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                    expectedPressIdxes.Add(Array.IndexOf(arrowColorIdxes, curTraitIdx));
                    break;
                case 4:
                case 5:
                case 6:
                case 7:
                    expectedPressIdxes.Add(Array.IndexOf(arrowDirectionIdxes, curTraitIdx - 4));
                    break;
            }
        }
    }
	public void ResetInstance()
    {
        obtainedValues.Clear();
        expectedPressIdxes.Clear();
        itemsToLog.Clear();
    }
}
