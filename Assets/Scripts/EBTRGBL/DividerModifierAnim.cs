using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DividerModifierAnim : MonoBehaviour {

	public GameObject[] allDividersH, allDividersV;
	public Transform blCorner, anchorSizeModifier;
	public float dividerWidth = 0.2f, dividerHeight = 0.2f;

    public void HandleResize(int size = 8)
    {
		for (var x = 0; x < allDividersH.Length; x++)
		{
			var idxX = x % size;

			allDividersH[x].SetActive(idxX < size - 1);
			var sizeModifier = new Vector3(anchorSizeModifier.localScale.x / size, 1, anchorSizeModifier.localScale.z / size);
            var Vector3LocalNewPos = new Vector3(sizeModifier.x * (idxX + 1), 0, anchorSizeModifier.localScale.z / 2) + blCorner.transform.localPosition;
			if (idxX < size - 1)
			{
				allDividersH[x].transform.localPosition = Vector3LocalNewPos;
				allDividersH[x].transform.localScale = new Vector3(dividerWidth, dividerHeight, anchorSizeModifier.localScale.z);
			}
		}
		for (var x = 0; x < allDividersV.Length; x++)
		{
			var idxY = x % size;

			allDividersV[x].SetActive(idxY < size - 1);
			var sizeModifier = new Vector3(anchorSizeModifier.localScale.x / size, 1, anchorSizeModifier.localScale.z / size);
            var Vector3LocalNewPos = new Vector3(anchorSizeModifier.localScale.x / 2, 0, sizeModifier.z * (idxY + 1)) + blCorner.transform.localPosition;
			if (idxY < size - 1)
			{
				allDividersV[x].transform.localPosition = Vector3LocalNewPos;
				allDividersV[x].transform.localScale = new Vector3(anchorSizeModifier.localScale.x, dividerHeight, dividerWidth);
			}
		}
	}
}
