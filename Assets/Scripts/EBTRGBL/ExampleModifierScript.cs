using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExampleModifierScript : MonoBehaviour {
	public DividerModifierAnim dividerAnim;
	public SizeModifierAnim sizeAnim;

	// Use this for initialization
	void Start () {
		StartCoroutine(UpdateAnimations());
	}

	IEnumerator UpdateAnimations()
    {
		while (enabled)
        {
			for (var x = 1; x <= 8; x++)
			{
				yield return new WaitForSeconds(3f);
				dividerAnim.HandleResize(x);
				sizeAnim.HandleResize(x);
			}
        }
    }
}
