using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RowRenderers : MonoBehaviour {

	public MeshRenderer[] wallRenderers;
	public bool[] canRender;
	public bool disableUpdateRender;

	// Use this for initialization
	void Start () {

	}

	// Update is called once per frame
	void Update () {
		for (int x = 0; x < Mathf.Min(canRender.Length, wallRenderers.Length) && !disableUpdateRender; x++)
        {
			wallRenderers[x].enabled = canRender[x];
        }
	}
}
