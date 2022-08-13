using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExampleScript : MonoBehaviour {

	public OutlineFillAnim[] outlineFillAnims;

	// Use this for initialization
	void Start () {
		StartCoroutine(HandleCycleAnim());
	}
	IEnumerator HandleCycleAnim()
    {
		var cnt = 0;
		while (enabled)
        {
			var binCnt = cnt;
			for (var x = 0; x < outlineFillAnims.Length; x++)
				outlineFillAnims[x].filled = (binCnt >> x) % 2 == 1;
			yield return new WaitForSeconds(1f);
			cnt++;
		}
    }
}
