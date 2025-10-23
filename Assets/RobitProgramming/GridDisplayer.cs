using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridDisplayer : MonoBehaviour {

	public RowRenderers[] rowRenderers;

	// Use this for initialization
	void Start () {
		//StartCoroutine(TestRowRenderers());
	}

	IEnumerator TestRowRenderers()
	{
		for (int z = 0; z < 25; z = (z + 1) % 17)
		{
			for (int x = 0; x < rowRenderers.Length; x++)
				for (int y = 0; y < rowRenderers[x].canRender.Length; y++)
				{
					rowRenderers[x].canRender[y] = x + y == z;


				}
			yield return new WaitForSeconds(0.2f);
		}
    }

	// Update is called once per frame
	void Update () {

	}
}
