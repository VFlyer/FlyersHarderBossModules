using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OutlineFillAnim : MonoBehaviour {

	public MeshRenderer filledRenderer, outlineRenderer;
	public bool filled = false;
	public float speed = 1f;
	public Vector3 maxScale;
	float animProgress = 0f;

	// Update is called once per frame
	void Update () {
		animProgress = Mathf.Min(1f, Mathf.Max(0f, animProgress + Time.deltaTime * speed * (filled ? 1 : -1)));

		outlineRenderer.enabled = animProgress < 1f;
		filledRenderer.enabled = animProgress > 0f;

		filledRenderer.transform.localScale = maxScale * Easing.InOutCirc(animProgress, 0f, 1f, 1f);


	}
}
