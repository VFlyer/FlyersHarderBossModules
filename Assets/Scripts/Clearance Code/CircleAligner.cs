using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleAligner : MonoBehaviour {

	public Transform[] AffectedObjects;

	public enum AxisHandling
    {
		Unaffected,
		SinOffset,
		CosOffset
    }
	public AxisHandling xAdjust, yAdjust, zAdjust;
	public float radius;
	public float percentOffset;
	public Vector3 posOffset;
	// Use this for initialization
	void Start () {
		UpdatePositions();
	}
	public void UpdatePositions()
    {
		var affectedObjectCnt = AffectedObjects.Length;
		for (var x = 0; x < affectedObjectCnt; x++)
		{
			float curPercentage = (percentOffset + (float)x / affectedObjectCnt) % 1f;
			float curPercent2Pi = Mathf.PI * 2 * curPercentage;
			var newLocalPos = new Vector3();
			newLocalPos += posOffset;
			switch (xAdjust)
			{
				case AxisHandling.Unaffected:
					break;
				case AxisHandling.CosOffset:
					newLocalPos += Vector3.right * Mathf.Cos(curPercent2Pi) * radius; break;
				case AxisHandling.SinOffset:
					newLocalPos += Vector3.right * Mathf.Sin(curPercent2Pi) * radius; break;
			}
			switch (yAdjust)
			{
				case AxisHandling.Unaffected:
					break;
				case AxisHandling.CosOffset:
					newLocalPos += Vector3.up * Mathf.Cos(curPercent2Pi) * radius; break;
				case AxisHandling.SinOffset:
					newLocalPos += Vector3.up * Mathf.Sin(curPercent2Pi) * radius; break;
			}
			switch (zAdjust)
			{
				case AxisHandling.Unaffected:
					break;
				case AxisHandling.CosOffset:
					newLocalPos += Vector3.forward * Mathf.Cos(curPercent2Pi) * radius; break;
				case AxisHandling.SinOffset:
					newLocalPos += Vector3.forward * Mathf.Sin(curPercent2Pi) * radius; break;
			}
			AffectedObjects[x].transform.localPosition = newLocalPos;
		}
	}
}
