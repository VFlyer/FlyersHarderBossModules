using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

public class TenDigitKeypadCore : MonoBehaviour {
    public KMSelectable[] digits;
    public TextMesh[] digitTexts;
    public TextMesh progressText, lastInputText;
    public KMBombInfo bombInfo;
    public int currentInputIdx, missingKeyValue;
    public List<int> obtainedValues, submissionValues;
    List<string> itemsToLog = new List<string>();
    Vector3[] storedDigitLocalPos;
    void Start()
    {
        storedDigitLocalPos = digits.Select(a => a.transform.localPosition).ToArray();
    }
    public void MimicLogging(string startText)
    {
        foreach (string anItem in itemsToLog)
            Debug.LogFormat("{0} {1}", startText, anItem);
    }


    public void AlterDigitPositions(bool resetPositions = false)
    {
        var shuffledDigitsLocalPos = resetPositions ? storedDigitLocalPos : storedDigitLocalPos.ToArray().Shuffle();
        for (var x = 0; x < digits.Length; x++)
            digits[x].transform.localPosition = shuffledDigitsLocalPos[x];
    }

    public void AssignObtainedValues(IEnumerable<int> calculatedValues)
    {
        if (obtainedValues == null)
            obtainedValues = new List<int>();
        if (submissionValues == null)
            submissionValues = new List<int>();
        ResetInstance();
        AlterDigitPositions();
        obtainedValues.AddRange(calculatedValues);
        missingKeyValue = Random.Range(0, 10);
        itemsToLog.Add(string.Format("Obtained Values: {0}", obtainedValues.Join()));
        var snDigits = bombInfo.GetSerialNumberNumbers();
        var snLetters = bombInfo.GetSerialNumberLetters();
        for (var x = 0; x < obtainedValues.Count; x++)
        {
            var curValue = obtainedValues[x];
            switch (x)
            {
                case 0:
                    {
                        var onIndicators = bombInfo.GetOnIndicators();
                        var offIndicators = bombInfo.GetOffIndicators();

                        if (bombInfo.IsIndicatorOff(Indicator.FRK))
                        {
                            curValue -= 2;
                            itemsToLog.Add(string.Format("Digit #{0}, applying modifier of -2.", x + 1));
                        }
                        else if (bombInfo.IsIndicatorOn(Indicator.FRK))
                        {
                            curValue += 2;
                            itemsToLog.Add(string.Format("Digit #{0}, applying modifier of +2.", x + 1));
                        }
                        else if (!onIndicators.Any())
                        {
                            curValue -= offIndicators.Count() % 10;
                            itemsToLog.Add(string.Format("Digit #{0}, applying modifier of -{1}.", x + 1, offIndicators.Count() % 10));
                        }
                        else if (onIndicators.Count() > offIndicators.Count())
                        {
                            curValue -= 7;
                            itemsToLog.Add(string.Format("Digit #{0}, applying modifier of -7.", x + 1));
                        }
                        else
                        {
                            curValue += snDigits.ElementAtOrDefault(1);
                            itemsToLog.Add(string.Format("Digit #{0}, applying modifier of +{1}.", x + 1, bombInfo.GetSerialNumberNumbers().ElementAtOrDefault(1)));
                        }
                        break;
                    }
                case 1:
                    {
                        if (!bombInfo.GetPortPlates().Any(a => a.Length == 0))
                        {
                            if (submissionValues.First() % 2 == 1)
                            {
                                curValue += obtainedValues.Count() % 10;
                                itemsToLog.Add(string.Format("Digit #{0}, applying modifier of +{1}.", x + 1, obtainedValues.Count() % 10));
                            }
                            else
                            {
                                curValue -= submissionValues.First() - 1;
                                itemsToLog.Add(string.Format("Digit #{0}, applying modifier of -{1}.", x + 1, submissionValues.First() - 1));
                            }
                        }
                        else
                        {
                            itemsToLog.Add(string.Format("Digit #{0}, applying modifier of 0.", x + 1));
                        }
                        break;
                    }
                default:
                    {
                        var last2Inputs = submissionValues.TakeLast(2).ToArray();
                        if (x % 2 == 0) // 2, 4, 6, 8, 10, 12, ...
                        {
                            if (last2Inputs.Contains(0))
                            {
                                //var FMWGraphReference = new[] { 1, 2, 3, 3, 5, 5, 7, 7, 10, 10, 12, 12, 15 };
                                curValue += snDigits.FirstOrDefault();
                                itemsToLog.Add(string.Format("Digit #{0}, applying modifier of +{1}.", x + 1, bombInfo.GetSerialNumberNumbers().FirstOrDefault()));
                            }
                            else if (last2Inputs.All(a => a % 2 == 1))
                            {
                                var oddDigitsInSerial = snDigits.Where(a => a % 2 == 1);
                                curValue += oddDigitsInSerial.Sum();
                                itemsToLog.Add(string.Format("Digit #{0}, applying modifier of +{1}.", x + 1, oddDigitsInSerial.Sum() % 10));
                            }
                            else
                            {
                                var sumLastCalcedValues = last2Inputs.Sum();
                                //Debug.Log(sumLastCalcedValues);
                                //Debug.Log(sumLastCalcedValues.ToString("0").First());
                                curValue += sumLastCalcedValues > 9 ? 1 : sumLastCalcedValues;
                                itemsToLog.Add(string.Format("Digit #{0}, applying modifier of +{1}.", x + 1, sumLastCalcedValues.ToString("0").First() - '0'));
                            }
                        }
                        else // 3, 5, 7, 9, 11, 13, ...
                        {
                            if (last2Inputs.All(a => a % 2 == 0))
                            {
                                var DRSumLastCalcedValues = last2Inputs.Sum() % 9 == 0 ? 9 : (last2Inputs.Sum() % 9);
                                curValue += last2Inputs.All(a => a == 0) ? 0 : DRSumLastCalcedValues;
                                itemsToLog.Add(string.Format("Digit #{0}, applying modifier of +{1}.", x + 1, last2Inputs.All(a => a == 0) ? 0 : DRSumLastCalcedValues));
                            }
                            else if (last2Inputs.Count(a => a == 5) == 1)
                            {
                                var alphaPos1stLetterSN = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(snLetters.First());
                                curValue += alphaPos1stLetterSN % 10;
                                itemsToLog.Add(string.Format("Digit #{0}, applying modifier of +{1}.", x + 1, alphaPos1stLetterSN % 10));
                            }
                            else
                            {
                                var higherBatPorts = Mathf.Max(bombInfo.GetPortCount(), bombInfo.GetBatteryCount());
                                curValue += higherBatPorts;
                                itemsToLog.Add(string.Format("Digit #{0}, applying modifier of +{1}.", x + 1, higherBatPorts));
                            }
                        }
                        break;
                    }
            }
            curValue = 10 + missingKeyValue - curValue; // Originally table procedure.
            submissionValues.Add(((curValue % 10) + 10) % 10);
        }
        itemsToLog.Add(string.Format("Values to submit: {0}", submissionValues.Join()));

    }
    public void ResetInstance()
    {
        obtainedValues.Clear();
        submissionValues.Clear();
        itemsToLog.Clear();
    }
}
