using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WireHandler : MonoBehaviour {

	public KMSelectable wireSelectable;
	public GameObject wireCut, wireUncut;
	public MeshRenderer[] wireRendersModifyable;
	public bool isCut = false;
	public bool updateRendererConstantly = false;
	// Use this for initialization
	void Start () {
		wireSelectable.OnInteract += delegate {
			isCut = true;
			return false;
		};

	}

	public void UpdateRenderer()
    {
		wireCut.SetActive(isCut);
		wireUncut.SetActive(!isCut);
	}

	// Update is called once per frame
	void Update () {
		if (updateRendererConstantly)
			UpdateRenderer();
	}
}
