using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class QuestionableWireSequencesCore : MonoBehaviour {

	public KMSelectable arrowUp, arrowDown;
	public WireHandler[] wiresAllCompact;
	public TextMesh[] displayTextsAllWires;
	public TextMesh panelIDxDisplay;
	int redWireCnt = 0, blueWireCnt = 0, whiteWireCnt = 0;
	public int currentPanelIdx = 0;

	int[][] redWireTargetValues = new int[13][], blueWireTargetValues = new int[13][], whiteWireTargetValues = new int[13][];

	public List<bool> wiresToCut = new List<bool>();
	public List<int> wireIdxColors = new List<int>();
	List<int> obtainedValues = new List<int>();
	public List<int> valueIndexes = new List<int>();
	List<string> itemsToLog = new List<string>();
	public void MimicLogging(string startText)
    {
		foreach (string anItem in itemsToLog)
			Debug.LogFormat("{0} {1}", startText, anItem);
    }
	public void AssignRuleSeedTargetValues(MonoRandom seededRandom = null)
    {
		if (seededRandom == null || seededRandom.Seed == 1)
        {
			redWireTargetValues = new int[][] {
				new[] { 0, 1, 2, 3, 7 },
				new[] { 0, 2, 5, 8, 9 },
				new[] { 2, 3, 5, 6, 9 },
				new[] { 1, 2, 5, 6, 8 },
				new[] { 1, 2, 3, 5, 8 },
				new[] { 0, 4, 6, 8, 9 },
				new[] { 0, 3, 4, 7, 9 },
				new[] { 0, 4, 6, 7, 9 },
				new[] { 3, 5, 6, 7, 9 },
				new[] { 1, 2, 4, 5, 6 },
				new[] { 1, 3, 4, 7, 8 },
				new[] { 0, 1, 4, 7, 8 },
			};
			blueWireTargetValues = new int[][] {
				new[] { 1, 3, 4, 5, 8 },
				new[] { 0, 1, 3, 6, 7 },
				new[] { 4, 5, 6, 8, 9 },
				new[] { 0, 1, 2, 8, 9 },
				new[] { 2, 3, 4, 6, 9 },
				new[] { 0, 1, 4, 5, 6 },
				new[] { 1, 2, 7, 8, 9 },
				new[] { 0, 2, 3, 5, 6 },
				new[] { 3, 4, 7, 8, 9 },
				new[] { 0, 2, 5, 6, 7 },
				new[] { 2, 3, 4, 7, 8 },
				new[] { 0, 1, 5, 7, 9 },
			};
			whiteWireTargetValues = new int[][] {
				new[] { 2, 4, 5, 8, 9 },
				new[] { 0, 2, 4, 5, 6 },
				new[] { 1, 3, 5, 6, 8 },
				new[] { 3, 4, 5, 6, 7 },
				new[] { 2, 3, 4, 5, 6 },
				new[] { 0, 1, 4, 6, 9 },
				new[] { 1, 2, 4, 7, 9 },
				new[] { 0, 1, 3, 7, 8 },
				new[] { 1, 2, 6, 8, 9 },
				new[] { 0, 1, 2, 7, 8 },
				new[] { 0, 3, 5, 7, 9 },
				new[] { 0, 3, 7, 8, 9 },
			};
		}
		else
        {
			int[] baseItems = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
			// Assign White Wire Target Values
			for (int x = 0; x < 13; x++)
			{
				int maxItemsInList = seededRandom.Next(0, 6);
				if (maxItemsInList <= 5)
				{
					int[] valuesInList = new int[maxItemsInList];
					seededRandom.ShuffleFisherYates(baseItems);
					for (int y = 0; y < maxItemsInList; y++)
						valuesInList[y] = baseItems[y];
					whiteWireTargetValues[x] = valuesInList;
				}
				else
                {
					whiteWireTargetValues[x] = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
				}
			}
			// Assign Red Wire Target Values
			for (int x = 0; x < 13; x++)
			{
				int maxItemsInList = seededRandom.Next(0, 6);
				if (maxItemsInList <= 5)
				{
					int[] valuesInList = new int[maxItemsInList];
					seededRandom.ShuffleFisherYates(baseItems);
					for (int y = 0; y < maxItemsInList; y++)
						valuesInList[y] = baseItems[y];
					redWireTargetValues[x] = valuesInList;
				}
				else
				{
					redWireTargetValues[x] = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
				}
			}
			// Assign Blue Wire Target Values
			for (int x = 0; x < 13; x++)
			{
				int maxItemsInList = seededRandom.Next(0, 6);
				if (maxItemsInList <= 5)
				{
					int[] valuesInList = new int[maxItemsInList];
					seededRandom.ShuffleFisherYates(baseItems);
					for (int y = 0; y < maxItemsInList; y++)
						valuesInList[y] = baseItems[y];
					blueWireTargetValues[x] = valuesInList;
				}
				else
				{
					blueWireTargetValues[x] = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
				}
			}
			
		}
    }

	public void AssignObtainedValues(IEnumerable<int> calculatedValues)
    {
		ResetInstance();
		obtainedValues = calculatedValues.ToList();

		for (int x = 0; x < obtainedValues.Count; x++)
			valueIndexes.Add(x);
		valueIndexes.Shuffle();
		itemsToLog.Add(string.Format("Assigned sequence of values: {0}", obtainedValues.Join(", ")));
		for (int x = 0; x < obtainedValues.Count; x++)
        {
			var randomValue = Random.Range(0, 3);
			wireIdxColors.Add(randomValue);
			var valueFromStage = obtainedValues[valueIndexes[x]];
			switch (randomValue)
            {
				case 0:
					{
						wiresToCut.Add(whiteWireTargetValues[whiteWireCnt].Contains(valueFromStage));
						itemsToLog.Add(string.Format("Using #{1} white wire's set of values for position #{2} in given sequence. Result: {0}", whiteWireTargetValues[whiteWireCnt].Contains(valueFromStage) ? "MUST CUT" : "DO NOT CUT", whiteWireCnt + 1, valueIndexes[x] + 1));
						//Debug.LogFormat("{0} ({3}): {2} ? {1}", x, whiteWireTargetValues[whiteWireCnt].Join(""), valueFromStage, valueIndexes[x]);
						whiteWireCnt = (whiteWireCnt + 1) % 13;
						break;
					}
				case 1:
					{
						wiresToCut.Add(redWireTargetValues[redWireCnt].Contains(valueFromStage));
						itemsToLog.Add(string.Format("Using #{1} red wire's set of values for position #{2} in given sequence. Result: {0}", redWireTargetValues[redWireCnt].Contains(valueFromStage) ? "MUST CUT" : "DO NOT CUT", redWireCnt + 1, valueIndexes[x] + 1));
						//Debug.LogFormat("{0} ({3}): {2} ? {1}", x, redWireTargetValues[redWireCnt].Join(""), valueFromStage, valueIndexes[x]);
						redWireCnt = (redWireCnt + 1) % 13;
						break;
					}
				case 2:
					{
						wiresToCut.Add(blueWireTargetValues[blueWireCnt].Contains(valueFromStage));
						itemsToLog.Add(string.Format("Using #{1} blue wire's set of values for position #{2} in given sequence. Result: {0}", blueWireTargetValues[blueWireCnt].Contains(valueFromStage) ? "MUST CUT" : "DO NOT CUT", blueWireCnt + 1, valueIndexes[x] + 1));
						//Debug.LogFormat("{0} ({3}): {2} ? {1}", x, blueWireTargetValues[blueWireCnt].Join(""), valueFromStage, valueIndexes[x]);
						blueWireCnt = (blueWireCnt + 1) % 13;
						break;
					}
				default:
                    {
						wiresToCut.Add(false);
						break;
					}
				
            }
			
        }
		
		currentPanelIdx = 0;
    }
	public void ResetInstance()
    {
		redWireCnt = 0;
		whiteWireCnt = 0;
		blueWireCnt = 0;
		obtainedValues.Clear();
		wiresToCut.Clear();
		wireIdxColors.Clear();
		valueIndexes.Clear();
		currentPanelIdx = -1;
		itemsToLog.Clear();
	}
}
