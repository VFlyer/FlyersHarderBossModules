using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CubeDisplayer : MonoBehaviour {
	public MeshRenderer[] spheresAll;
    public Vector3 GlobalOffset;
	public Vector3[] axisModifier;
	private bool isRunningCoroutine;
	void Start()
    {
		StartCoroutine(SampleSphereHandler());
    }
	public bool IsCouroutineRunning()
    {
		return isRunningCoroutine;
	}
	public IEnumerator SampleSphereHandler()
    {
		yield return RevealSpheres();
		for (var x = 0; x < 9; x++)
        {
			var axisA = x % 3;
			var axisB = x / 3;
			yield return SimulateCustomRotation(axisA, axisB, 4);
		}
		for (var x = 0; x < 36; x++)
        {
			var shuffledValues = Enumerable.Range(0, 3).ToArray().Shuffle();
			yield return SimulateCustomRotation(shuffledValues.First(), shuffledValues.Last());
        }

		yield return HideSpheres();
	}
	public IEnumerator RevealSpheres()
    {
		isRunningCoroutine = true;
		Vector3[] allSphereInitialCoordinates = new Vector3[(int)Mathf.Pow(2, axisModifier.Length)];
		for (var x = 0; x < allSphereInitialCoordinates.Length; x++)
		{
			allSphereInitialCoordinates[x] = GlobalOffset;
			var expectedBoolState = new bool[axisModifier.Length];
			var curPower2 = 1;
			for (var y = 0; y < expectedBoolState.Length; y++)
			{
				expectedBoolState[y] = x / curPower2 % 2 >= 1;
				curPower2 *= 2;
			}
			for (var y = 0; y < expectedBoolState.Length; y++)
			{
				if (expectedBoolState[y])
					allSphereInitialCoordinates[x] += axisModifier[y];
			}
		}
		for (float t = 0; t <= 1f; t += Time.deltaTime / 2)
		{
			for (var x = 0; x < spheresAll.Length; x++)
			{
				spheresAll[x].transform.localPosition =
					Easing.InOutCirc(t, 0f, 1f, 1f) * allSphereInitialCoordinates[x];
			}
			yield return null;
		}
		for (var x = 0; x < spheresAll.Length; x++)
		{
			spheresAll[x].transform.localPosition = allSphereInitialCoordinates[x];
		}
		isRunningCoroutine = false;
	}
	public IEnumerator HideSpheres()
	{
		isRunningCoroutine = true;
		Vector3[] allSphereInitialCoordinates = new Vector3[(int)Mathf.Pow(2, axisModifier.Length)];
		for (var x = 0; x < allSphereInitialCoordinates.Length; x++)
		{
			allSphereInitialCoordinates[x] = GlobalOffset;
			var expectedBoolState = new bool[axisModifier.Length];
			var curPower2 = 1;
			for (var y = 0; y < expectedBoolState.Length; y++)
			{
				expectedBoolState[y] = x / curPower2 % 2 >= 1;
				curPower2 *= 2;
			}
			for (var y = 0; y < expectedBoolState.Length; y++)
			{
				if (expectedBoolState[y])
					allSphereInitialCoordinates[x] += axisModifier[y];
			}
		}
		for (float t = 0; t <= 1f; t += Time.deltaTime / 2)
		{
			for (var x = 0; x < spheresAll.Length; x++)
			{
				spheresAll[x].transform.localPosition =
					Easing.InOutCirc(1f - t, 0f, 1f, 1f) * allSphereInitialCoordinates[x];
			}
			yield return null;
		}
		for (var x = 0; x < spheresAll.Length; x++)
		{
			spheresAll[x].transform.localPosition = Vector3.zero;
		}
		isRunningCoroutine = false;
	}
	public IEnumerator SimulateCustomRotation(int axisStartIdx, int axisEndIdx, int repeatCount = 1, float speed = 2f)
    {
		isRunningCoroutine = true;
		Vector3[] allSphereInitialCoordinates = new Vector3[(int)Mathf.Pow(2, axisModifier.Length)];
		for (var x = 0; x < allSphereInitialCoordinates.Length; x++)
        {
			allSphereInitialCoordinates[x] = GlobalOffset;
			var expectedBoolState = new bool[axisModifier.Length];
			var curPower2 = 1;
			for (var y = 0; y < expectedBoolState.Length; y++)
			{
				expectedBoolState[y] = x / curPower2 % 2 >= 1;
				curPower2 *= 2;
			}
			for (var y = 0; y < expectedBoolState.Length; y++)
			{
				if (expectedBoolState[y])
					allSphereInitialCoordinates[x] += axisModifier[y];
			}
		}
		Vector3[] allSphereExpectedCoordinates = new Vector3[allSphereInitialCoordinates.Length];
		for (var x = 0; x < allSphereExpectedCoordinates.Length; x++)
		{
			allSphereExpectedCoordinates[x] = allSphereInitialCoordinates[x];
			var expectedBoolState = new bool[axisModifier.Length];
			var curPower2 = 1;
			for (var y = 0; y < expectedBoolState.Length; y++)
			{
				expectedBoolState[y] = x / curPower2 % 2 == 1;
				curPower2 *= 2;
			}
			switch ((expectedBoolState[axisEndIdx] ? 2 : 0) + (expectedBoolState[axisStartIdx] ? 1 : 0))
            {
				case 0:
					allSphereExpectedCoordinates[x] += axisModifier[axisStartIdx];
					break;
				case 1:
					allSphereExpectedCoordinates[x] += axisModifier[axisEndIdx];
					break;
				case 2:
					allSphereExpectedCoordinates[x] -= axisModifier[axisEndIdx];
					break;
				case 3:
					allSphereExpectedCoordinates[x] -= axisModifier[axisStartIdx];
					break;
            }
		}
		for (var curRep = 0; curRep < repeatCount; curRep++)
		{
			for (float t = 0; t < 1f; t += Time.deltaTime * speed)
			{
				var curTime = Easing.InOutCirc(t, 0f, 1f, 1f);
				for (var x = 0; x < spheresAll.Length; x++)
					spheresAll[x].transform.localPosition = curTime * allSphereExpectedCoordinates[x] + (1f - curTime) * allSphereInitialCoordinates[x];
				yield return null;
			}
		}
		isRunningCoroutine = false;
		yield break;
    }
}
