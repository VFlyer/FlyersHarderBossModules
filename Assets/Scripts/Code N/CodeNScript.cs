using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CodeNScript : MonoBehaviour {

	public KMBombModule modSelf;
	public KMBombInfo bombInfo;
	public KMAudio mAudio;
	public KMRuleSeedable ruleSeed;
	public KMSelectable[] digitsSelectable, operatorsSelectable;
	public KMSelectable ACBtn, CBtn, EquBtn;
	public TextMesh displayMesh;
	char[][] encodingTable = new char[][] {
		new[] { '0', '9', '8', '7', '6', '5', '4', '3', '2', '1', },
		new[] { '1', '0', '9', '8', '7', '6', '5', '4', '3', '2', },
		new[] { '2', '1', '0', '9', '8', '7', '6', '5', '4', '3', },
		new[] { '3', '2', '1', '0', '9', '8', '7', '6', '5', '4', },
		new[] { '4', '3', '2', '1', '0', '9', '8', '7', '6', '5', },
		new[] { '5', '4', '3', '2', '1', '0', '9', '8', '7', '6', },
		new[] { '6', '5', '4', '3', '2', '1', '0', '9', '8', '7', },
		new[] { '7', '6', '5', '4', '3', '2', '1', '0', '9', '8', },
		new[] { '8', '7', '6', '5', '4', '3', '2', '1', '0', '9', },
		new[] { '9', '8', '7', '6', '5', '4', '3', '2', '1', '0', },
	};
	int moduleID;
	static int modIDCnt;

	int rowUsed;
	List<int> allowedIdxOpers = new List<int>(), operBtnIdx;
	static Dictionary<int, List<string>> rsExpressions = new Dictionary<int, List<string>>();
	List<string> usedExpressions;

	void QuickLog(string toLog, params object[] args)
    {
		Debug.LogFormat("[{0} #{1}] {2}", modSelf.ModuleDisplayName, moduleID, string.Format(toLog, args));
    }

	void HandleRuleSeed()
    {
		var randomizer = ruleSeed == null ? new MonoRandom(1) : ruleSeed.GetRNG();
		var curSeed = randomizer.Seed;
		if (rsExpressions.ContainsKey(curSeed))
			usedExpressions = rsExpressions[curSeed];
    }

	// Use this for initialization
	void Start () {
		moduleID = ++modIDCnt;
		rowUsed = Random.Range(0, 10);
		operBtnIdx = Enumerable.Range(0, 4).ToList();

		QuickLog("The individual sum of each pair of encrypted and decrypted digits add up to {0}, modulo 10.", rowUsed);
	}
	
	string EvaluateOperands(int operIdx, int a, int b)
    {
		string output = "";
		var larger = a > b ? a : b;
		var smaller = a > b ? b : a;
		switch (operIdx)
        {
			case 0:
                output += (a + b).ToString();
				break;
            case 1:
				output += (larger - smaller).ToString();
				break;
			case 2:
				output += (a * b).ToString();
				break;
			case 3:
				{
					var remainder = larger;
                    for (var x = 0; x < 4; x++)
                    {
						output += (remainder / smaller).ToString();
						remainder = 10 * (remainder % smaller);
                    }
				}
				break;
		}

		return output;
    }

	// Update is called once per frame
	void Update () {
		
	}
}
