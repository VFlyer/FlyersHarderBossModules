using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SizeModifierAnim : MonoBehaviour {

	public GameObject[] allObjects;
	public Transform blCorner, anchorSizeModifier;

	[SerializeField]
	private int curSize, initializedSize;
    public void HandleResize(int size = 8)
    {
		initializedSize = Mathf.FloorToInt(Mathf.Sqrt(allObjects.Length));
		curSize = size;
		var newMaxIdx = size - 1;
		for (var x = 0; x < allObjects.Length; x++)
		{
			var idxX = x % initializedSize;
			var idxY = x / initializedSize;

			allObjects[x].SetActive(idxX < curSize && idxY < curSize);
			var sizeModifier = new Vector3(anchorSizeModifier.localScale.x / curSize, 1, anchorSizeModifier.localScale.x / curSize);
            var Vector3LocalNewPos = sizeModifier / 2 + blCorner.transform.localPosition + new Vector3(sizeModifier.x * idxX, 0, sizeModifier.z * (curSize - 1 - idxY));
			if (idxX < curSize && idxY < curSize)
			{
				allObjects[x].transform.localPosition = Vector3LocalNewPos;
				allObjects[x].transform.localScale = sizeModifier;
			}
		}
	}
}
