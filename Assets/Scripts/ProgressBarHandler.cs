using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProgressBarHandler : MonoBehaviour {

	public GameObject barLeft, barRight;
	public float curProgress = 0, maxProgress = 1;

	// Use this for initialization
	void Start () {



	}

	void displayPercentage()
	{
		float percentageInvert = Mathf.Max(0, Mathf.Min(maxProgress - curProgress, maxProgress)) / maxProgress;
		float percentageBase = Mathf.Max(0,Mathf.Min(curProgress,maxProgress)) / maxProgress;


		barLeft.transform.localPosition = new Vector3(-0.5f * percentageInvert, barLeft.transform.localPosition.y, barLeft.transform.localPosition.z);
		barRight.transform.localPosition = new Vector3(0.5f * percentageBase, barLeft.transform.localPosition.y, barLeft.transform.localPosition.z);
		barLeft.transform.localScale = new Vector3(percentageBase, 1, 1);
		barRight.transform.localScale = new Vector3(percentageInvert, 1, 1);
		barLeft.SetActive(curProgress > 0);
		barRight.SetActive(curProgress < maxProgress);

	}

	// Update is called once per frame
	void Update () {
		displayPercentage();
	}
}
