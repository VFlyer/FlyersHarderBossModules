using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KModkit;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using Random = UnityEngine.Random;

public class ExampleMonochromeArrowsScript : MonoBehaviour {
	public KMBombModule modSelf;
	public KMBombInfo mBombInfo;
    public KMColorblindMode colorblindMode;
	public MonochromeArrowsCore mArrowsCore;
	public TextMesh textDisplay, colorblindTextIndc;
    public MeshRenderer backingRenderer;
    public KMAudio mAudio;
    public Color[] colorIdxesAll;
    readonly string[] debugDirections = { "Up", "Right", "Down", "Left" };
    static int modIdCnt;
    int modId;
    bool isanimating = true, moduleSolved, allowRunCoroutine = true, colorblindDetected = false, hasStruck;
    IEnumerator textCycleAnim;
	// Use this for initialization
	void Start () {
        modId = ++modIdCnt;
		modSelf.OnActivate += delegate {
            textCycleAnim = CycleTextDigits();
            StartCoroutine(textCycleAnim);
            isanimating = false;

        };
        StartCoroutine(PulseBacking());
        mBombInfo.OnBombExploded += delegate {
            allowRunCoroutine = false;
            StopAllCoroutines();
        };

        List<int> generatedStartOffsetValues = new List<int>(), directionIdxValues = new List<int>();
        var lastSerialNoDigit = mBombInfo.GetSerialNumberNumbers().Last();
        var xOffset = Random.Range(0, 8);
        for (var p = 0; p < xOffset; p++)
        {
            generatedStartOffsetValues.Add(9 - lastSerialNoDigit);
        }
        var yOffset = Random.Range(0, 8);
        for (var p = 0; p < yOffset; p++)
        {
            generatedStartOffsetValues.Add((10 - lastSerialNoDigit) % 10);
        }
        generatedStartOffsetValues.Shuffle(); // Offset calculations can be scrambled.
        var lastDirection = -1;
        for (var x = 0; x < 5; x++)
        {
            lastDirection = Enumerable.Range(1, 8).Where(a => a != lastDirection).PickRandom();
            var repeatCount = Random.Range(1, 5);
            for (var p = 0; p < repeatCount; p++)
                directionIdxValues.Add((lastDirection + 10 - lastSerialNoDigit) % 10);

        }

        textDisplay.text = "";
        var finalGeneratedValues = new List<int>();
        while (generatedStartOffsetValues.Any() || directionIdxValues.Any())
        {
            var idxToAdd = new List<int>();
            if (generatedStartOffsetValues.Any())
                idxToAdd.Add(0);
            if (directionIdxValues.Any())
                idxToAdd.Add(1);

            switch (idxToAdd.PickRandom())
            {
                case 0:
                    finalGeneratedValues.Add(generatedStartOffsetValues.First());
                    generatedStartOffsetValues.RemoveAt(0);
                    break;
                case 1:
                    finalGeneratedValues.Add(directionIdxValues.First());
                    directionIdxValues.RemoveAt(0);
                    break;
            }
        }

        mArrowsCore.AssignObtainedValues(finalGeneratedValues, lastSerialNoDigit);
        for (var x = 0; x < mArrowsCore.arrowDirectionIdxes.Length; x++)
        {
            var curIdxDirection = mArrowsCore.arrowDirectionIdxes[x];
            mArrowsCore.arrowRenderers[curIdxDirection].material.color = colorIdxesAll[mArrowsCore.arrowColorIdxes[x]];
            int y = x;
            mArrowsCore.arrowSelectables[x].OnInteract += delegate {
                if (!(isanimating || moduleSolved))
                {
                    mArrowsCore.arrowSelectables[y].AddInteractionPunch();
                    mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, mArrowsCore.arrowSelectables[y].transform);
                    CheckArrow(y);
                }
                return false;
            };
        }
        //Debug.LogFormat("<Monochromic Arrows #{0}> Debug directions, unsorted {1}", modId, Enumerable.Range(0, 4).Select(a => debugDirections[mArrowsCore.arrowDirectionIdxes[a]]).Join());
        //Debug.LogFormat("<Monochromic Arrows #{0}> Debug brightness, unsorted {1}", modId, Enumerable.Range(0, 4).Select(a => mArrowsCore.arrowColorIdxes[a]).Join());

        Debug.LogFormat("[Monochromic Arrows #{0}] Arrows ordered from brightest to darkest (by color): {1}", modId, Enumerable.Range(0,4).OrderBy(a => mArrowsCore.arrowColorIdxes[a]).Select(b => debugDirections[ mArrowsCore.arrowDirectionIdxes[b]]).Join(", "));
        Debug.LogFormat("[Monochromic Arrows #{0}] Displayed Digits: {1}", modId, mArrowsCore.obtainedValues.Join(", "));
        mArrowsCore.MimicLogging(string.Format("[Monochromic Arrows #{0}]", modId));
        Debug.LogFormat("[Monochromic Arrows #{0}] Expected directions to press: {1}", modId, mArrowsCore.expectedPressIdxes.Select(a => debugDirections[mArrowsCore.arrowDirectionIdxes[a]]).Join(", "));

        try
        {
            colorblindDetected = colorblindMode.ColorblindModeActive;
        }
        catch
        {
            colorblindDetected = false;
        }
        finally
        {
            HandleColorblindToggle();
        }
    }
    void HandleColorblindToggle()
    {
        colorblindTextIndc.gameObject.SetActive(colorblindDetected);
    }
    void CheckArrow(int directionIdx)
    {
        var expectedIdxDirection = mArrowsCore.arrowDirectionIdxes[mArrowsCore.expectedPressIdxes[mArrowsCore.currentInputIdx]];
        if (expectedIdxDirection == directionIdx)
        {
            mArrowsCore.currentInputIdx++;
            if (mArrowsCore.currentInputIdx >= mArrowsCore.expectedPressIdxes.Count)
            {
                StopCoroutine(textCycleAnim);
                StartCoroutine(victory());
                Debug.LogFormat("[Monochromic Arrows #{0}] Directions inputted successfully. Disarming module...", modId);
            }
        }
        else
        {
            Debug.LogFormat("[Monochromic Arrows #{0}] For arrow No.{3} to press, {1} was pressed when it expected {2}! Resetting input to the beginning.", modId, debugDirections[directionIdx], debugDirections[expectedIdxDirection], mArrowsCore.currentInputIdx + 1);
            hasStruck = true;
            modSelf.HandleStrike();
            mArrowsCore.currentInputIdx = 0;
        }
    }
    IEnumerator PulseBacking()
    {
        var lastColor = backingRenderer.material.color;
        while (allowRunCoroutine)
        {
            for (float t = 0; t < 1f; t += Time.deltaTime)
            {
                backingRenderer.material.color = Color.white * t + lastColor * (1f - t);
                yield return null;
            }
            backingRenderer.material.color = Color.white;
            yield return new WaitForSeconds(2f);
        }
    }
    IEnumerator CycleTextDigits()
    {
        var curOffset = -2;
        while (allowRunCoroutine)
        {
            textDisplay.text = Enumerable.Range(0, 2).Select(a => a + curOffset < 0 || a + curOffset >= mArrowsCore.obtainedValues.Count ? "" : mArrowsCore.obtainedValues.ElementAt(a + curOffset).ToString()).Join("");
            if (curOffset + 1 > mArrowsCore.obtainedValues.Count)
                curOffset = -1;
            else
                curOffset++;
            for (float t = 0; t < 1f; t += Time.deltaTime)
            {
                yield return null;
                textDisplay.color = Color.white * (1f - t);
            }
        }
        yield break;
    }

    protected virtual IEnumerator victory() // The default victory animation from eXish's Arrows bretherns
    {
        var initialTextDisplayLength = textDisplay.text.Length;
        var lastTextColor = textDisplay.color;
        isanimating = true;
        for (int i = 0; i < 100; i++)
        {
            int rand1 = Random.Range(0, 10);
            int rand2 = Random.Range(0, 10);
            if (i < 50)
            {
                textDisplay.text = initialTextDisplayLength == 2 ? rand1 + "" + rand2 : rand2.ToString();
                textDisplay.color = Color.white * (i / 49f) + lastTextColor * (49 - i) / 49f;
            }
            else
            {
                textDisplay.text = "G" + rand2;
                textDisplay.color = Color.white * ((i - 50) / 50f);
            }
            yield return new WaitForSeconds(0.025f);
        }
        textDisplay.text = "GG";
        moduleSolved = true;
        isanimating = false;
        modSelf.HandlePass();
        for (float t = 0; t < 1f; t += Time.deltaTime)
        {
            
            textDisplay.color = Color.white * t;
            yield return null;
        }
        textDisplay.color = Color.white;
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = "Press the specified arrow button with \"!{0} up/right/down/left\" Words can be substituted as one letter (Ex. right as r). Multiple directions can be issued in one command by spacing them out or as 1 word when abbrevivabted, I.E \"!{0} udlrrrll\". Alternatively, when abbreviated, you may space out the presses in the command. I.E. \"!{0} lluur ddlr urd\" Toggle colorblind mode with \"!{0} colorblind\"";
#pragma warning restore 414
    protected IEnumerator ProcessTwitchCommand(string command)
    {
        if (moduleSolved || isanimating)
        {
            yield return "sendtochaterror The module is not accepting any commands at this moment.";
            yield break;
        }
        if (Regex.IsMatch(command, @"^\s*colou?rblind\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            colorblindDetected = !colorblindDetected;
            HandleColorblindToggle();
            yield break;
        }
        else if (Regex.IsMatch(command, @"^\s*[uldr\s]+\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            var usableCommand = command.Trim().ToLowerInvariant();
            List<int> allPresses = new List<int>();
            foreach (string directionportion in usableCommand.Split())
            {
                foreach (char dir in directionportion)
                {
                    switch (dir)
                    {
                        case 'u':
                            allPresses.Add(0);
                            break;
                        case 'd':
                            allPresses.Add(2);
                            break;
                        case 'l':
                            allPresses.Add(3);
                            break;
                        case 'r':
                            allPresses.Add(1);
                            break;
                        default:
                            yield return string.Format("sendtochaterror I do not know what direction \"{0}\" is supposed to be.", dir);
                            yield break;
                    }
                }
            }
            if (allPresses.Any())
            {
                hasStruck = false;
                for (int x = 0; x < allPresses.Count && !hasStruck; x++)
                {
                    yield return null;
                    var directionExpected = mArrowsCore.arrowDirectionIdxes[mArrowsCore.expectedPressIdxes[mArrowsCore.currentInputIdx]];
                    if (allPresses[x] != directionExpected && allPresses.Count > 1)
                        yield return string.Format("strikemessage by incorrectly pressing {0} after {1} press(es) in the TP command!", debugDirections[allPresses[x]], x + 1);
                    mArrowsCore.arrowSelectables[allPresses[x]].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                    if (moduleSolved) yield return "solve";
                }
            }
        }
        else
        {
            string[] cmdSets = command.Trim().Split();
            List<KMSelectable> allPresses = new List<KMSelectable>();
            for (int x = 0; x < cmdSets.Length; x++)
            {
                if (Regex.IsMatch(cmdSets[x], @"^\s*u(p)?\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                {
                    allPresses.Add(mArrowsCore.arrowSelectables[0]);
                }
                else if (Regex.IsMatch(cmdSets[x], @"^\s*d(own)?\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                {
                    allPresses.Add(mArrowsCore.arrowSelectables[2]);
                }
                else if (Regex.IsMatch(cmdSets[x], @"^\s*l(eft)?\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                {
                    allPresses.Add(mArrowsCore.arrowSelectables[3]);
                }
                else if (Regex.IsMatch(cmdSets[x], @"^\s*r(ight)?\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                {
                    allPresses.Add(mArrowsCore.arrowSelectables[1]);
                }
                else
                {
                    yield return string.Format("sendtochaterror I do not know what direction \"{0}\" is supposed to be.", cmdSets[x]);
                    yield break;
                }
            }
            hasStruck = false;
            for (var x = 0; x < allPresses.Count && !hasStruck; x++)
            {
                yield return null;
                var debugIdx = Array.IndexOf(mArrowsCore.arrowSelectables, allPresses[x]);
                var directionExpected = mArrowsCore.arrowDirectionIdxes[mArrowsCore.expectedPressIdxes[mArrowsCore.currentInputIdx]];
                if (debugIdx != directionExpected && allPresses.Count > 1)
                    yield return string.Format("strikemessage by incorrectly pressing {0} after {1} press(es) in the TP command!", debugDirections[debugIdx], x + 1);
                allPresses[x].OnInteract();
                yield return new WaitForSeconds(0.1f);
                if (moduleSolved) { yield return "solve"; }
            }
            yield break;
        }
    }
    protected IEnumerator TwitchHandleForcedSolve()
    {
        while (isanimating) { yield return true; };
        while (mArrowsCore.currentInputIdx < mArrowsCore.expectedPressIdxes.Count)
        {
            var directionExpected = mArrowsCore.arrowDirectionIdxes[mArrowsCore.expectedPressIdxes[mArrowsCore.currentInputIdx]];
            mArrowsCore.arrowSelectables[directionExpected].OnInteract();
            yield return new WaitForSeconds(0.1f);
            //Debug.LogFormat("{0}",directionExpected);
        }
        while (isanimating) { yield return true; };
    }

}
