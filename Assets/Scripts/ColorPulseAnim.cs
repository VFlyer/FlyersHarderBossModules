using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorPulseAnim : MonoBehaviour {

	public Renderer affectedRenderer;
	public Color initialColor, endColor;
	public float speed = 1f, delayBetweenPulse = 5f;
	// Use this for initialization
	void Start () {
		StartCoroutine(PulseSequence());
	}
	
	IEnumerator PulseSequence()
    {
		while (enabled)
        {
            for (float t = 0; t < 1f; t += Time.deltaTime * speed)
            {
				affectedRenderer.material.color = initialColor * (1f - t) + endColor * t;
				yield return null;
            }
			affectedRenderer.material.color = endColor;
			yield return new WaitForSeconds(delayBetweenPulse);
        }
    }
}
