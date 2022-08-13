using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text.RegularExpressions;

public class ExampleTenDigitKeypad : MonoBehaviour {

	public KMAudio mAudio;
	public KMBombModule modSelf;
	public TenDigitKeypadCore keypadCore;
	public TextMesh statusTextMesh, digitDisplayMesh;

	static int modIdCnt;
	int modId;
	int[] generatedValues;
	public int valuesToGenerate = 15;
	bool modSolved = false, hasStruck = false, moduleStarted = false;
	IEnumerator flashingAnim;
	// Use this for initialization
	void Start () {
		modId = ++modIdCnt;
        generatedValues = new int[valuesToGenerate > 0 ? valuesToGenerate : Random.Range(8, 18)];
		for (var x = 0; x < generatedValues.Length; x++)
			generatedValues[x] = Random.Range(0, 10);
		keypadCore.AssignObtainedValues(generatedValues);
		modSelf.OnActivate += delegate
		{
			StartCoroutine(flashingAnim = FlashGeneratedDigits());
			moduleStarted = true;
			statusTextMesh.text = "____";
		};
        for (var x = 0; x < keypadCore.digits.Length; x++)
        {
			var y = x;
			keypadCore.digits[x].OnInteract += delegate {
				keypadCore.digits[y].AddInteractionPunch();
				mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, keypadCore.digits[y].transform);
				if (!modSolved && moduleStarted)
                {
					CheckInput(y);
				}
				return false;
			};
        }
		Debug.LogFormat("[Ten Digit Keypad #{0}] Digits generated: {1}", modId, keypadCore.obtainedValues.Count);
		keypadCore.MimicLogging(string.Format("[Ten Digit Keypad #{0}]", modId));
		statusTextMesh.text = digitDisplayMesh.text = "";
	}
	void UpdateInputDisplay()
    {
		var startIdx = Mathf.Max(Mathf.Min(keypadCore.currentInputIdx, keypadCore.submissionValues.Count - 1), 3);
		statusTextMesh.text = Enumerable.Range(-3, 4).Select(a => a + startIdx < keypadCore.currentInputIdx ? keypadCore.submissionValues[a + startIdx].ToString() : "_").Join("");
    }
	void CheckInput(int digit)
    {
		if (keypadCore.submissionValues[keypadCore.currentInputIdx] == digit)
		{
			keypadCore.currentInputIdx++;
			if (keypadCore.currentInputIdx >= keypadCore.submissionValues.Count)
			{
				Debug.LogFormat("[Ten Digit Keypad #{0}] Module disarmed.", modId);
				modSolved = true;
				modSelf.HandlePass();
				StopCoroutine(flashingAnim);
				digitDisplayMesh.text = "";
				
			}
			UpdateInputDisplay();
		}
		else
		{
			hasStruck = true;
			modSelf.HandleStrike();
			Debug.LogFormat("[Ten Digit Keypad #{0}] Struck. {2} was incorrectly inputted for digit No. {1}.", modId, keypadCore.currentInputIdx + 1, digit);
		}
	}
	IEnumerator FlashGeneratedDigits()
    {
		while (!modSolved)
		{
			for (var x = 0; x < generatedValues.Length; x++)
			{
				digitDisplayMesh.text = generatedValues[x].ToString();
                for (double t = 0; t < 1f; t += Time.deltaTime)
                {
					digitDisplayMesh.color = keypadCore.currentInputIdx == x ? Color.yellow : keypadCore.currentInputIdx > x ? Color.green : Color.white;
					yield return null;
                }					
				digitDisplayMesh.text = "";
				yield return new WaitForSeconds(0.05f);
			}
		}
		yield break;
	}
	// TP Handler begins here
	IEnumerator TwitchHandleForcedSolve()
    {
        for (var x = keypadCore.currentInputIdx; x < keypadCore.submissionValues.Count; x++)
        {
			yield return null;
			var curDigitInput = keypadCore.submissionValues[x];
			keypadCore.digits[curDigitInput].OnInteract();
			yield return new WaitForSeconds(0.1f);
        }
    }
	readonly string TwitchHelpMessage = "Input the digits 0,1,2,3,4,5,6,7,8,9 in that order with \"!{0} press 531820...\" or \"!{0} submit 531820...\"";
    public IEnumerator ProcessTwitchCommand(string cmd)
    {
        List<int> digits = new List<int>();
        List<string> cmdlist = cmd.Split(' ', ',').ToList();
        if (!(cmdlist[0].EqualsIgnoreCase("press") || cmdlist[0].EqualsIgnoreCase("submit")))// Is the starting command valid?
        {
            yield return "sendtochaterror Your command is invalid. The command must start with \"press\" or \"submit\" followed by a string of digits.";
            yield break;
        }
        cmdlist.RemoveAt(0);
        foreach (string dgtcmd in cmdlist)// Check for each portion of the command in the string.
        {
            char[] chrcmd = dgtcmd.ToCharArray();
            for (int i = 0; i < chrcmd.Length; i++)
            {
                //string singlecmd = chrcmd[i].ToString();
                if (char.IsDigit(chrcmd[i]))
                {
                    digits.Add(chrcmd[i] - '0');
                }
                else
                {
                    yield return "sendtochaterror Your command is invalid. The character \"" + chrcmd[i] + "\" is invalid.";
                    yield break;
                }
            }
        }

        if (!digits.Any()) // Operates the same as (digits.Count <= 0)
        {
            yield break;
        }
        //yield return "Forget It Not"; // Suggestively unnecessary 
        //yield return "multiple strikes";
        for (int i = 0; !hasStruck && i < digits.Count; i++)
        {
            int d = digits[i];
            yield return null;
            keypadCore.digits[d].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        //yield return "end multiple strikes";
        //yield return "end waiting music";
        yield break;
    }
	/*
    IEnumerator ProcessTwitchCommand(string cmd)
    {
		var newCmd = cmd.ToString();
		Match startCmdMatch = Regex.Match(cmd, @"^((s(ubmit)?|p(ress)?)\s)?(\d+[,;\s]?)+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		if (startCmdMatch.Success)
		{
			var buttonsToPress = new List<KMSelectable>();
			var matchedCmdFiltered = startCmdMatch.Value;
			foreach (string portion in matchedCmdFiltered.Split())
            {
				foreach (char oneDigit in portion)
				{
					if (char.IsDigit(oneDigit))
						buttonsToPress.Add(keypadCore.digits[oneDigit - '0']);
				}
			}
			if (!buttonsToPress.Any())
            {
				yield return "sendtochaterror The command specified gave no buttons to press!";
				yield break;
            }
			//
			yield return null;
			hasStruck = false;
			for (var x = 0; !hasStruck && x < buttonsToPress.Count; x++)
            {
				buttonsToPress[x].OnInteract();
				yield return new WaitForSeconds(0.1f);
            }
		}
		yield break;
    }*/
}
