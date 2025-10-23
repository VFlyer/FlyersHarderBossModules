using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FootoverSolveAnim : MonoBehaviour {

    public MeshRenderer[] affectedRenders;
    public TextMesh[] affectedMeshes;
    public KMAudio usedAudio;

	public IEnumerator StartSolveAnim()
    {
        var lastTexts = affectedMeshes.Select(a => a.text).ToArray();
        usedAudio.PlaySoundAtTransform("Angel (Drop)", usedAudio.transform);
        var usedIdxes = new IEnumerable<int>[] {
            Enumerable.Range(0, 18),
            Enumerable.Range(18, 18),
            Enumerable.Range(0, 6),
            Enumerable.Range(6, 6),
            Enumerable.Range(12, 6),
            Enumerable.Range(18, 6),
            Enumerable.Range(24, 6),
            Enumerable.Range(30, 6),
            Enumerable.Range(0, 6).Select(a => a * 6),
            Enumerable.Range(0, 6).Select(a => a * 6 + 1),
            Enumerable.Range(0, 6).Select(a => a * 6 + 2),
            Enumerable.Range(0, 6).Select(a => a * 6 + 3),
            Enumerable.Range(0, 6).Select(a => a * 6 + 4),
            Enumerable.Range(0, 6).Select(a => a * 6 + 5),
        };
        var overrideColors = new List<Color> {
            Color.white,
            Color.white,
            Color.green, Color.green, Color.green, Color.green, Color.green, Color.green,
            Color.white, Color.white, Color.white, Color.white, Color.white, Color.white,
        };
        var delays = new float[] {
            0.14f, 0.31f, 0.45f, 0.45f, 0.48f, 0.11f, 0.4f, 0.5f, 0.48f, 0.11f, 0.4f, 0.5f,
        };
        for (var x = 0; x < usedIdxes.Length; x++)
        {
            if (x < delays.Length)
                yield return new WaitForSecondsRealtime(delays[x]);
            var curIdxes = usedIdxes[x];
            foreach (var idx in curIdxes)
            {
                affectedRenders[idx].material.color = x >= overrideColors.Count ? Color.black : overrideColors[x];
                affectedMeshes[idx].text = "";
            }
        }

        yield break;
    }
}
