using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DoorAnimScript : MonoBehaviour {
	// Use this for initialization
	public bool increasingValue = false;
	public float speedModifier = 1f;
	public GameObject doorT, doorB;
	private float transformedValue = 0f;

	// Update is called once per frame
	void Update () {
		transformedValue = increasingValue ?
			Mathf.Min(transformedValue + Time.deltaTime * speedModifier, 1f) :
			Mathf.Max(transformedValue - Time.deltaTime * speedModifier, 0f);
		doorB.SetActive(transformedValue < 1f);
		doorT.SetActive(transformedValue < 1f);
		doorT.transform.localPosition = Vector3.forward * 0.25f + Vector3.forward * 0.25f * transformedValue;
		doorT.transform.localScale = new Vector3(1, 1, 0.5f * (1f - transformedValue));
		doorB.transform.localPosition = Vector3.back * 0.25f + Vector3.back * 0.25f * transformedValue;
		doorB.transform.localScale = new Vector3(1, 1, 0.5f * (1f - transformedValue));
	}
}
