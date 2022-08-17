using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

public class TenDigitKeypadCore : MonoBehaviour {
    public KMSelectable[] digits;
    public KMBombInfo bombInfo;
    public int currentInputIdx;
    public List<int> obtainedValues, submissionValues;
    List<string> itemsToLog = new List<string>();
    public void MimicLogging(string startText)
    {
        foreach (string anItem in itemsToLog)
            Debug.LogFormat("{0} {1}", startText, anItem);
    }
    public void AssignObtainedValues(IEnumerable<int> calculatedValues)
    {
        if (obtainedValues == null)
            obtainedValues = new List<int>();
        if (submissionValues == null)
            submissionValues = new List<int>();
        ResetInstance();
        obtainedValues.AddRange(calculatedValues);
        itemsToLog.Add(string.Format("Obtained Values: {0}", obtainedValues.Join()));
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
                            curValue += bombInfo.GetSerialNumberNumbers().ElementAtOrDefault(1);
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
                        if (submissionValues.ElementAt(x - 1) == 0 || submissionValues.ElementAt(x - 2) == 0)
                        {
                            //var FMWGraphReference = new[] { 1, 2, 3, 3, 5, 5, 7, 7, 10, 10, 12, 12, 15 };
                            curValue += bombInfo.GetSerialNumberNumbers().FirstOrDefault();
                            itemsToLog.Add(string.Format("Digit #{0}, applying modifier of +{1}.", x + 1, bombInfo.GetSerialNumberNumbers().FirstOrDefault()));
                        }
                        else if (submissionValues.ElementAt(x - 1) % 2 == 1 && submissionValues.ElementAt(x - 2) % 2 == 0)
                        {
                            var oddDigitsInSerial = bombInfo.GetSerialNumberNumbers().Where(a => a % 2 == 1);
                            curValue += oddDigitsInSerial.Sum();
                            itemsToLog.Add(string.Format("Digit #{0}, applying modifier of +{1}.", x + 1, oddDigitsInSerial.Sum() % 10));
                        }
                        else
                        {
                            var sumLastCalcedValues = submissionValues.ElementAt(x - 1) + submissionValues.ElementAt(x - 2);
                            //Debug.Log(sumLastCalcedValues);
                            //Debug.Log(sumLastCalcedValues.ToString("0").First());
                            curValue += sumLastCalcedValues > 9 ? 1 : sumLastCalcedValues;
                            itemsToLog.Add(string.Format("Digit #{0}, applying modifier of +{1}.", x + 1, sumLastCalcedValues.ToString("0").First() - '0'));
                        }
                        break;
                    }
            }
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
