using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EBTRGBLScript : MonoBehaviour {

	public KMBossModuleExtensions bossHandler;
	public KMBombInfo bombInfo;
	public KMBombModule modSelf;
	public KMAudio mAudio;
	public MeshRenderer[] a, miscULs;
	public TextMesh[] f;
	public OutlineFillAnim[] b, c, d, m;
	public SizeModifierAnim n;
	public DividerModifierAnim div;
	public KMSelectable[] r, s, grid;

	static int modIDCnt;
	int moduleID;
	[SerializeField]
	int stageIdx;
	int k, clrCur, hldIdx, maxStageAhd, maxStageBhd, solveCountNonIgnored;
	float cooldown = 10f;
	private bool animating, tickCooldown, bossActive, started = false, xoring, recoverable, enforceExhibiton;
	[SerializeField]
	private bool debugBossMode;
	int[] g, v, w;
	List<bool[]> h;
	List<int> i, j, u, stgodr;
	List<bool> o, q, vld;
	string[] ignoreList;
    readonly Dictionary<int, Color32> clr = new Dictionary<int, Color32>
	{
		{0, Color.black },
		{1, new Color32(85, 85, 255, 0) },
		{2, Color.green },
		{3, Color.cyan },
		{4, Color.red },
		{5, Color.magenta },
		{6, Color.yellow },
		{7, Color.white },
	};
    readonly Dictionary<int, int[]> conflicts = new Dictionary<int, int[]>
	{
		{ 0, new[] { 1, 3, 5, 8 } },
		{ 1, new[] { 4, 5, 7, 9 } },
		{ 2, new[] { 6 } },
		{ 3, new[] { 1, 5, 7, 9 } },
		{ 4, new[] { 0, 1, 3, 8 } },
		{ 5, new[] { 0, 4, 7, 9 } },
		{ 6, new[] { 2 } },
		{ 7, new[] { 0, 3, 4, 8 } },
		{ 8, new[] { 1, 3, 5, 9 } },
		{ 9, new[] { 0, 4, 7, 8 } },
	};
	const string clrAbrev = "KBGCRMYW";
	FlyersBossierSettings globalSettings = new FlyersBossierSettings();
	// Use this for initialization
	void QuickLog(string value, params object[] args)
    {
		Debug.LogFormat("[{0} #{1}]: {2}", "Slight Gibberish Twist", moduleID, string.Format(value, args));
    }
	void QuickLogDebug(string value, params object[] args)
    {
		Debug.LogFormat("<{0} #{1}>: {2}", "Slight Gibberish Twist", moduleID, string.Format(value, args));
    }
	void Awake()
    {
		if (debugBossMode)
		{
			modSelf.ModuleDisplayName = "Forget Me Not";
		}
		try
        {
			var modSettings = new ModConfig<FlyersBossierSettings>("FlyersBossierSettings");
			globalSettings = modSettings.Settings;
			modSettings.Settings = globalSettings;
			enforceExhibiton = globalSettings.SGTExhibitionMode;
			maxStageAhd = globalSettings.SGTMaxStagesAhead;
			maxStageBhd = globalSettings.SGTMaxStagesBehind;
        }
		catch
        {
			enforceExhibiton = false;
			maxStageAhd = 15;
			maxStageBhd = 5;
        }
    }
    void Start()
    {
        moduleID = ++modIDCnt;
        QuickLog("What do you mean this is a manual challenge!? I thought the goal was to not decompile stuff like this, and get away with it, for being what it is.");

        var p = new[] { 4, 7, 10, 13 };
        ignoreList = bossHandler.GetAttachedIgnoredModuleIDs(modSelf,
			Application.isEditor ? new[] { "slightGibberishTwistModule" } : new string[0]);
		var maxExtraStages = bombInfo.GetSolvableModuleIDs().Count(a => !ignoreList.Contains(a)) - 1;
        if (ignoreList != null && ignoreList.Any())
        {
            bossActive = true;
            k = 8 - p.Count(a => maxExtraStages >= a);
			QuickLog("Total extra stages generatable: {0}", maxExtraStages);
		}
        else
        {
			recoverable = true;
            maxExtraStages = 9;
            k = 4;
			QuickLog("Boss mode is not active. Generating 9 extra stages.");
		}
		if (maxExtraStages <= 2)
        {
			bossActive = false;
			recoverable = true;
			maxExtraStages = 3;
			QuickLog("Insufficient non-ignored modules. Generating 3 extra stages instead, and disabling boss mode.");
		}
		else if (enforceExhibiton)
        {
			bossActive = false;
			recoverable = true;
			QuickLog("Disabling boss mode due to settings preventing this.");
		}
		QuickLog("Allocating board size {0} by {0}.", k);
		n.HandleResize(k);
		div.HandleResize(k);
		g = new int[k * k];
		v = new int[k * k];
		for (var x = 0; x < g.Length; x++)
            g[x] = Random.Range(0, 8);
		w = g.ToArray();
		//QuickLog("-------------- Stage 1 ---------------");
		//QuickLog("Initial board (from left to right, top to bottom): {0}", g.Select(a => clrAbrev[a]).Join(""));
		QuickLog("Initial board (from left to right, top to bottom): {0}",
			Enumerable.Range(0, k).Select(a => Enumerable.Range(0, k).Select(b => clrAbrev[g[a * k + b]]).Join("")).Join(","));
		for (var x = 0; x < 3; x++)
		{
			QuickLogDebug("Initial {1} state (from left to right, top to bottom): {0}", g.Select(a => (a >> x) % 2 == 1 ? "T" : "F").Join(""), clrAbrev[1 << x]);
		}


		i = new List<int>();
        j = new List<int>();
		
		h = new List<bool[]>();
		o = new List<bool>();
		q = new List<bool>();
		vld = new List<bool>();

        i.Add(Random.Range(0, 10));
		o.Add(Random.value < 0.5f);
		q.Add(Random.value < 0.5f);
		vld.Add(true);
		//QuickLog("Displayed Operator: {0}", i.First());
		var arrayIdxes = Enumerable.Range(0, 3).ToArray().Shuffle();
		var l = 0;
        for (var x = 0; x < maxExtraStages; x++)
        {
			//QuickLog("-------------- Stage {0} ---------------", x + 2);
			var curI = Random.Range(0, 10);
			i.Add(curI);
			//QuickLog("Displayed Operator: {0}", curI);
			o.Add(Random.value < 0.5f);
			q.Add(Random.value < 0.5f);
			if (l >= arrayIdxes.Length)
            {
				l = 0;
				arrayIdxes.Shuffle();
            }
			j.Add(arrayIdxes[l]);
			l++;
			var newStage = new bool[k * k];
            for (var y = 0; y < newStage.Length; y++)
				newStage[y] = Random.value < 0.5f;
			//QuickLog("Displayed Grid (GOL Style): {0}", Enumerable.Range(0, k).Select(a => Enumerable.Range(0, k).Any(b => newStage[a * k + b]) ? Enumerable.Range(0, k).Where(b => newStage[a * k + b]).Select(b => b + 1).Join("") : "-").Join(","));
			h.Add(newStage);
			var curVld = !(conflicts.ContainsKey(curI) && conflicts[curI].Contains(i[x]));
			if (x >= 2)
			{
				var temp = vld.TakeLast(2);
				curVld = temp.All(a => a) ? false : temp.All(a => !a) ? true : curVld;
			}
			//QuickLog("Validity on current stage: {0}", curVld ? "VALID" : "INVALID");
			vld.Add(curVld);
		}
		//QuickLog("-----------------------------");
		for (var x = 0; x < f.Length; x++)
			f[x].text = "";
		u = new List<int>();
		for (var x = 0; x < 3; x++)
		{
			u.AddRange(Enumerable.Range(1, maxExtraStages).Where(a => j[a - 1] == x).ToArray().Shuffle().Take(3));
		}
		u.Shuffle();

		if (bossActive)
		{
			stgodr = new List<int>();
			stgodr.AddRange(Enumerable.Range(0, maxExtraStages + 1));
			if (maxStageBhd > 1 && maxStageAhd > 1)
			{
				var iterCount = 0;
				var maxIterCount = 100;
				do
				{
					stgodr.Shuffle();
					iterCount++;
				}
				while (iterCount < maxIterCount && Enumerable.Range(0, maxExtraStages).Any(a => (stgodr[a + 1] - stgodr[a] > maxStageAhd) || (stgodr[a] - stgodr[a + 1] < maxStageBhd)));
				if (iterCount >= maxIterCount && Enumerable.Range(0, maxExtraStages).Any(a => (stgodr[a + 1] - stgodr[a] > maxStageAhd) || (stgodr[a] - stgodr[a + 1] < maxStageBhd)))
					QuickLog("After {0} iteration{1}, the module was unable to generate stages with a valid max behind and max forward.", maxIterCount, maxIterCount == 1 ? "" : "s");
				QuickLog("Stages will be displayed in this order in accordiance to the settings, max {1} stage(s) behind, max {2} stage(s) ahead: {0}", stgodr.Select(a => a + 1).Join(", "), maxStageBhd, maxStageAhd);
			}
			else if (maxStageAhd <= 1 && maxStageBhd > 1)
			{
				stgodr.Reverse();
				QuickLog("Stages will be displayed in this order in accordiance to the settings, max {1} stage(s) behind, 1 stage ahead: {0}", stgodr.Select(a => a + 1).Join(", "), maxStageBhd);
			}
			else if (maxStageAhd <= 1 && maxStageBhd <= 1)
			{
				stgodr.Shuffle();
				QuickLog("Stages will be displayed in this order in accordiance to the settings, as many stages behind, as many stages ahead: {0}", stgodr.Select(a => a + 1).Join(", "));
			}
			else
			{
				QuickLog("Stages will be displayed in this order in accordiance to the settings, max 1 stage behind, {1} stages ahead: {0}", stgodr.Select(a => a + 1).Join(", "), maxStageAhd);
			}
		}
		QuickLog("Required stages to solve: {0}", u.Select(a => a + 1).Join(", "));
        for (var cnt = 0; cnt < u.Count; cnt++)
        {
			var curI = i[u[cnt]];
			if (!vld[u[cnt]]) continue;
			var chn = j[u[cnt] - 1];
			var prevChan = w.Select(a => (a >> chn) % 2 == 1);
            var resChan = Enumerable.Range(0, k * k).Select(a => Oper(prevChan.ElementAt(a), h[u[cnt] - 1][a], curI));
            for (var zp = 0; zp < w.Length; zp++)
            {
				if (((w[zp] >> chn) % 2 == 1) ^ resChan.ElementAt(zp))
					w[zp] ^= 1 << chn;
            }
		}
		for (var x = 0; x < 3; x++)
		{
			QuickLogDebug("Expected {1} state (from left to right, top to bottom): {0}", w.Select(a => (a >> x) % 2 == 1 ? "T" : "F").Join(""), clrAbrev[1 << x]);
		}
        //QuickLog("Expected final state (from left to right, top to bottom): {0}", w.Select(a => clrAbrev[a]).Join(""));
		QuickLog("Expected board to submit (from left to right, top to bottom): {0}",
			Enumerable.Range(0, k).Select(a => Enumerable.Range(0, k).Select(b => clrAbrev[w[a * k + b]]).Join("")).Join(","));
		for (var x = 0; x < r.Length; x++)
        {
			var y = x;
			r[x].OnInteract += delegate {
				r[y].AddInteractionPunch(0.1f);
				mAudio.PlaySoundAtTransform("BlipSelect", r[y].transform);
				if (!animating && started)
                {
					if (!bossActive && stageIdx != i.Count)
					{
						cooldown = 2f;
						if (!tickCooldown)
						{
							tickCooldown = true;
							StartCoroutine(TickDelayStg(stageIdx));
						}
						stageIdx = (stageIdx + (y == 0 ? -1 : 1) + i.Count) % i.Count;
						hldIdx = y;
					}
					else if (stageIdx == i.Count && recoverable && !tickCooldown)
                    {
						u.Clear();
						for (var z = 0; z < 3; z++)
						{
							u.AddRange(Enumerable.Range(1, maxExtraStages).Where(a => j[a - 1] == z).ToArray().Shuffle().Take(3));
						}
						u.Shuffle();
						QuickLog("WARNING! Activating Recovery Mode changed the required stages to disarm the module in the particular order: {0}", u.Select(a => a + 1).Join(", "));
						w = g.ToArray();
						for (var cnt = 0; cnt < u.Count; cnt++)
						{
							var curI = i[u[cnt]];
							if (!vld[u[cnt]]) continue;
							var chn = j[u[cnt] - 1];
							var prevChan = w.Select(a => (a >> chn) % 2 == 1);
							var resChan = Enumerable.Range(0, k * k).Select(a => Oper(prevChan.ElementAt(a), h[u[cnt] - 1][a], curI));
							for (var zp = 0; zp < w.Length; zp++)
							{
								if (((w[zp] >> chn) % 2 == 1) ^ resChan.ElementAt(zp))
									w[zp] ^= 1 << chn;
							}
						}
						for (var cnt = 0; cnt < 3; cnt++)
						{
							QuickLogDebug("New expected {1} state (from left to right, top to bottom): {0}", w.Select(a => (a >> x) % 2 == 1 ? "T" : "F").Join(""), clrAbrev[1 << x]);
						}
						QuickLog("New expected final state (from left to right, top to bottom): {0}",
							Enumerable.Range(0, k).Select(a => Enumerable.Range(0, k).Select(b => clrAbrev[w[a * k + b]]).Join("")).Join(","));
						stageIdx = 0;
						StartCoroutine(UncolorizePallete());
						StartCoroutine(BigAnim());
					}
				}
				return false;
			};
			r[x].OnInteractEnded += delegate {
				hldIdx = -1;
			};
        }
        for (var x = 0; x < s.Length; x++)
        {
			var y = x;
			s[x].OnInteract += delegate {
				s[y].AddInteractionPunch(0.1f);
				mAudio.PlaySoundAtTransform("Douga", s[y].transform);
				if (!animating && started)
                {
					if (!bossActive && !tickCooldown && stageIdx != i.Count)
                    {
						stageIdx = i.Count;
						StartCoroutine(BigAnim());
                    }
					else if (stageIdx == i.Count)
                    {
						if (y == 3)
							xoring ^= true;
						else
							clrCur ^= 1 << y;
						d[y].filled = y == 3 ? xoring : (clrCur >> y) % 2 == 1;
						cooldown = 5f;
						if (xoring && clrCur == 0 && !tickCooldown)
						{
							StartCoroutine(TickDelaySub());
						}
                    }
					else if (!bossActive && tickCooldown)
                    {
						cooldown = 0f;
					}
				}
				return false;
			};
        }
        for (var x = 0; x < grid.Length; x++)
        {
			var y = x;
			if (!grid[x].gameObject.activeSelf) continue;
			grid[x].OnInteract += delegate {
				grid[y].AddInteractionPunch(0.1f);
				mAudio.PlaySoundAtTransform("BlipSelect", grid[y].transform);
				if (!animating && started && stageIdx == i.Count)
                {
					var idxX = y % 8;
					var idxY = y / 8;

					if (idxX < k && idxY < k)
						v[idxY * k + idxX] = xoring ? v[idxY * k + idxX] ^ clrCur : clrCur;

					UpdateSomething();
				}
				return false;
			};
        }
		modSelf.OnActivate += delegate {
			StartCoroutine(BigAnim());
		};
	}
	IEnumerator TickDelayStg(int lastStageIdx)
    {
		while (cooldown > 0f)
		{
			yield return null;
			cooldown -= Time.deltaTime;
			if (cooldown < 1.5f && hldIdx != -1)
            {
				stageIdx = (stageIdx + (hldIdx == 0 ? -1 : 1) + i.Count) % i.Count;
				mAudio.PlaySoundAtTransform("BlipSelect", r[hldIdx].transform);
				cooldown = 1.6f;
			}
            yield return AnimateASet(Enumerable.Range(-4, 9).Select(a => string.Format( a == 0 ? ">{0}<" : "{0}", (stageIdx + a + i.Count) % i.Count + 1)).ToArray());
		}
		tickCooldown = false;

		if (stageIdx != lastStageIdx)
			yield return BigAnim();
		else
        {
			StartCoroutine(AnimateASet(delay: 0, txt: new[] { "STAGE", (stageIdx + 1).ToString("000"), "" }));
			StartCoroutine(AnimateASet(delay: 0f, offset: 3, txt: new[] { "CHN", stageIdx >= 1 ? clrAbrev[1 << j[stageIdx - 1]].ToString() : "RGB", "" }));
			StartCoroutine(bossActive ? AnimateASet(delay: 0, offset: 6, txt: new[] { "STAGES", "QUEUED", (bombInfo.GetSolvedModuleIDs().Count(a => !ignoreList.Contains(a)) - stageIdx).ToString("00000") }) : AnimateASet(delay: 0, offset: 6, txt: new[] { "BOSS", "MODE", "OFF" }));
		}
    }
	IEnumerator TickDelaySub()
    {
		var combinedX = b.Concat(c).Reverse();
		while (cooldown > 0f && clrCur == 0 && xoring)
		{
			cooldown -= Time.deltaTime;
			for (var x = 0; x < combinedX.Count(); x++)
            {
				combinedX.ElementAt(x).filled = x + 1 < cooldown;
            }
			yield return null;
		}
		tickCooldown = false;
		if (clrCur == 0 && xoring)
		{
			animating = true;
			d.Last().filled = false;
			for (var x = 0; x < m.Length; x++)
			{
				m[x].filled = false;
			}
			StartCoroutine(AnimateASet(0.2f, 0, "", "", "", "", "", "", "", "", ""));
			mAudio.PlaySoundAtTransform("Douga", transform);
			mAudio.PlaySoundAtTransform("InputCheck", transform);
			var horizScan = Random.value < 0.5f;
            for (float time = 0; time <= 1f; time += Time.deltaTime)
			{
				for (var x = 0; x < k * k; x++)
				{
					var idxX = x % k;
					var idxY = x / k;
					a[8 * idxY + idxX].material.color = horizScan ?
						time * k >= idxY ? clr[0] : clr[v[x]] :
						time * k >= idxX ? clr[0] : clr[v[x]];
				}
				yield return null;
			}
            for (float time = 0; time <= 1f; time += Time.deltaTime * 2)
			{
				for (var x = 0; x < k * k; x++)
				{
					var idxX = x % k;
					var idxY = x / k;
					a[8 * idxY + idxX].material.color = horizScan ?
						time * k >= idxY ? clr[7] : clr[0] :
						time * k >= idxX ? clr[7] : clr[0];
				}
				yield return null;
			}
            for (float time = 0; time <= 1f; time += Time.deltaTime * 2)
			{
				for (var x = 0; x < k * k; x++)
				{
					var idxX = x % k;
					var idxY = x / k;
					a[8 * idxY + idxX].material.color = horizScan ?
						time * k >= idxY ? clr[0] : clr[7] :
						time * k >= idxX ? clr[0] : clr[7];
				}
				yield return null;
			}
			for (var x = 0; x < k * k; x++)
			{
				var idxX = x % k;
				var idxY = x / k;
				a[8 * idxY + idxX].material.color = clr[7];
			}
			yield return new WaitForSeconds(0.2f);

			var correctCnt = Enumerable.Range(0, k * k).Count(a => v[a] == w[a]);

			for (var x = 0; x < k * k; x++)
			{
				var idxX = x % k;
				var idxY = x / k;
				a[8 * idxY + idxX].material.color = x < correctCnt ? clr[2] : clr[4];
			}
			QuickLog("Submitted the current state: (from left to right, top to bottom): {0}",
				Enumerable.Range(0, k).Select(a => Enumerable.Range(0, k).Select(b => clrAbrev[v[a * k + b]]).Join("")).Join(","));
			if (v.SequenceEqual(w))
            {
				QuickLog("SOLVED. No errors detected.");
				mAudio.HandlePlaySoundAtTransform("540321__colorscrimsontears__system-shutdown", transform);
				modSelf.HandlePass();
				for (float time = 0; time <= 1f; time += Time.deltaTime / 4)
				{
					for (var x = 0; x < miscULs.Length; x++)
					{
						miscULs[x].material.color = time * Color.black + (1f - time) * Color.white;
					}
					
					for (var x = 0; x < k * k; x++)
					{
						var idxX = x % k;
						var idxY = x / k;
						a[8 * idxY + idxX].material.color = time * (Color)clr[0] + (1f - time) * (Color)clr[2];
					}
					for (var x = 0; x < combinedX.Count(); x++)
					{
						combinedX.ElementAt(x).filledRenderer.material.color = (1f - time) * Color.yellow + time * Color.black;
						combinedX.ElementAt(x).outlineRenderer.material.color = (1f - time) * Color.yellow + time * Color.black;
					}
					for (var x = 0; x < d.Length; x++)
					{
						d[x].filledRenderer.material.color = (1f - time) * (Color)clr[x == 3 ? 7 : 1 << x] + time * Color.black;
						d[x].outlineRenderer.material.color = (1f - time) * (Color)clr[x == 3 ? 7 : 1 << x] + time * Color.black;
					}
					for (var x = 0; x < m.Length; x++)
					{
						m[x].filledRenderer.material.color = (1f - time) * Color.white + time * Color.black;
						m[x].outlineRenderer.material.color = (1f - time) * Color.white + time * Color.black;
					}
					yield return null;
				}
				for (var x = 0; x < k * k; x++)
				{
					var idxX = x % k;
					var idxY = x / k;
					a[8 * idxY + idxX].material.color = clr[0];
				}
				for (var x = 0; x < combinedX.Count(); x++)
				{
					combinedX.ElementAt(x).filledRenderer.material.color = Color.black;
					combinedX.ElementAt(x).outlineRenderer.material.color = Color.black;
				}
				for (var x = 0; x < d.Length; x++)
				{
					d[x].filledRenderer.material.color = Color.black;
					d[x].outlineRenderer.material.color = Color.black;
				}
				for (var x = 0; x < m.Length; x++)
				{
					m[x].filledRenderer.material.color = Color.black;
					m[x].outlineRenderer.material.color = Color.black;
				}
				yield break;
            }
			QuickLog("STRIKE. Correct cells filled: {0} / {1}", correctCnt, k * k);
			mAudio.HandlePlaySoundAtTransform("249300__suntemple__access-denied", transform);
			modSelf.HandleStrike();
			xoring = false;
			recoverable = true;
			yield return new WaitForSeconds(2f);
			UpdateSomething();
			for (var x = 0; x < combinedX.Count(); x++)
			{
				combinedX.ElementAt(x).filled = true;
			}
			for (var x = 0; x < m.Length; x++)
			{
				m[x].filled = recoverable;
			}
			StartCoroutine(AnimateASet(delay: 0.1f, txt: u.Select(a => (a + 1).ToString("000")).ToArray()));
			animating = false;
		}
		else
			for (var x = 0; x < combinedX.Count(); x++)
			{
				combinedX.ElementAt(x).filled = true;
			}
	}
	void UpdateSomething()
    {
		for (var y = 0; y < k * k; y++)
		{
			var p = y % k;
			var q = y / k;
			a[8 * q + p].material.color = clr[v[y]];
		}
	}

	bool Oper(bool a, bool b, int idx)
    {
		switch(idx)
        {
			case 0:
				return a || b;
			case 1:
				return a && b;
			case 2:
				return a ^ b;
			case 3:
				return !a || b;
			case 4:
				return !(a || b);
			case 5:
				return !(a && b);
			case 6:
				return !a ^ b;
			case 7:
				return a || !b;
			case 8:
				return !(!a || b);
			case 9:
				return !(a || !b);
        }
		return a;
    }

	IEnumerator BigAnim()
    {
		animating = true;
        yield return null;
		mAudio.PlaySoundAtTransform("Simpletonic", transform);
		StartCoroutine(AnimateASet(0.1f, 0, "A", "D", "V", "A", "N", "C", "I", "N", "G"));
		if (stageIdx >= i.Count)
			StartCoroutine(ColorizePallete());
		for (var x = 0; x < m.Length; x++)
		{
			m[x].filled = false;
		}
		for (var x = 0; x < 8; x++)
        {
            for (var y = 0; y < k * k; y++)
            {
				var p = y % k;
				var q = y / k;
				var curStg = !bossActive || recoverable ? stageIdx : stgodr.ElementAtOrDefault(stageIdx);
				a[8 * p + q].material.color = curStg == 0 || stageIdx >= i.Count ? clr[Random.Range(0, 8)] : Random.value < 0.5f ? clr[1 << Random.Range(0, 3)] : clr[0];
			}
            for (var y = 0; y < b.Length; y++)
            {
				b[y].filled = Random.value < 0.5f;
			}
            for (var y = 0; y < c.Length; y++)
            {
				c[y].filled = Random.value < 0.5f;
			}
            for (var y = 0; y < d.Length; y++)
            {
				d[y].filled = Random.value < 0.5f;
			}
			yield return new WaitForSeconds(0.25f);
        }

		if (stageIdx >= i.Count)
		{
			for (var y = 0; y < k * k; y++)
			{
				var p = y % k;
				var q = y / k;
				a[8 * q + p].material.color = clr[v[y]];
			}
			StartCoroutine(AnimateASet(delay: 0.1f, txt: u.Select(a => (a + 1).ToString("000")).Concat(Enumerable.Repeat("", 9 - u.Count())).ToArray()));
			for (var y = 0; y < b.Length; y++)
			{
				b[y].filled = true;
			}
			for (var y = 0; y < c.Length; y++)
			{
				c[y].filled = true;
			}
			for (var y = 0; y < d.Length; y++)
			{
				d[y].filled = y == 3 ? xoring : (clrCur >> y) % 2 == 1;
			}
			for (var x = 0; x < m.Length; x++)
			{
				m[x].filled = recoverable;
			}
			animating = false;
			yield break;
		}
		else
		{
			var curStage = !bossActive || recoverable ? stageIdx : stgodr.ElementAtOrDefault(stageIdx);
			if (curStage == 0)
			{
				for (var y = 0; y < k * k; y++)
				{
					var p = y % k;
					var q = y / k;
					a[8 * q + p].material.color = clr[g[y]];
				}
			}
			else
			{
				for (var y = 0; y < k * k; y++)
				{
					var p = y % k;
					var q = y / k;
					a[8 * q + p].material.color = h[curStage - 1][y] ? clr[1 << j[curStage - 1]] : clr[0];
				}
			}
			StartCoroutine(AnimateASet(delay: 0.1f, txt: new[] { "STAGE", (curStage + 1).ToString("000"), "" }));
			StartCoroutine(AnimateASet(delay: 0.1f, offset: 3, txt: new[] { "CHN", curStage >= 1 ? clrAbrev[1 << j[curStage - 1]].ToString() : "RGB", "" }));
			StartCoroutine(AnimateASet(delay: 0.1f, offset: 6, txt: bossActive ? recoverable ? new[] { "READY", "TO", "SOLVE" } : new[] { "STAGES", "QUEUED" } : new[] { "BOSS", "MODE", "OFF" }));
			if (bossActive && !recoverable)
			{
				StartCoroutine(AnimateASet(delay: 0f, offset: 8, txt: (solveCountNonIgnored - stageIdx).ToString("00000")));
			}
			for (var x = 0; x < b.Length; x++)
			{
				b[x].filled = x == 1 ^ o[curStage];
			}
			for (var x = 0; x < c.Length; x++)
			{
				c[x].filled = x == 1 ^ q[curStage];
			}

			for (var x = 0; x < d.Length; x++)
			{
				var xVal = x % 2 == 1 ^ o[curStage];
				var yVal = x / 2 == 1 ^ q[curStage];
				d[x].filled = Oper(xVal, yVal, i[curStage]);
			}
			for (var x = 0; x < m.Length; x++)
			{
				m[x].filled = !bossActive;
			}
			started = true;
			tickCooldown = bossActive;
			animating = false;
		}
    }
	IEnumerator AnimateASet(params string[] txt)
    {
		yield return AnimateASet(0, 0, txt);
    }
	IEnumerator AnimateASet(float delay = 0.1f, int offset = 0, params string[] txt)
    {
		for (var x = 0; x + offset < f.Length && x < txt.Length; x++)
        {
			f[x + offset].text = txt[x];
			if (delay > 0f)
				yield return new WaitForSeconds(delay);
        }
    }

	IEnumerator ColorizePallete()
    {
		for (float time = 0; time < 1f; time += Time.deltaTime / 2)
		{
			for (var x = 0; x < b.Length; x++)
			{
                b[x].outlineRenderer.material.color = time * Color.yellow + (1f - time) * Color.white;
				b[x].filledRenderer.material.color = time * Color.yellow + (1f - time) * Color.white;
			}
			for (var x = 0; x < c.Length; x++)
			{
				c[x].filledRenderer.material.color = time * Color.yellow + (1f - time) * Color.white;
				c[x].outlineRenderer.material.color = time * Color.yellow + (1f - time) * Color.white;
			}
			for (var x = 0; x < d.Length - 1; x++)
			{
				d[x].filledRenderer.material.color = time * (Color)clr[1 << x] + (1f - time) * Color.white;
				d[x].outlineRenderer.material.color = time * (Color)clr[1 << x] + (1f - time) * Color.white;
			}
			yield return null;
		}
		for (var x = 0; x < b.Length; x++)
		{
			b[x].outlineRenderer.material.color = Color.yellow;
			b[x].outlineRenderer.material.color = Color.yellow;
		}
		for (var x = 0; x < c.Length; x++)
		{
			c[x].filledRenderer.material.color = Color.yellow;
			c[x].outlineRenderer.material.color = Color.yellow;
		}
		for (var x = 0; x < d.Length - 1; x++)
		{
			d[x].filledRenderer.material.color = clr[1 << x];
			d[x].outlineRenderer.material.color = clr[1 << x];
		}
	}
	IEnumerator UncolorizePallete()
    {
		for (float time = 0; time < 1f; time += Time.deltaTime / 2)
		{
			for (var x = 0; x < b.Length; x++)
			{
				b[x].outlineRenderer.material.color = (1f - time) * Color.yellow + time * Color.white;
				b[x].filledRenderer.material.color = (1f - time) * Color.yellow + time * Color.white;
			}
			for (var x = 0; x < c.Length; x++)
			{
				c[x].filledRenderer.material.color = (1f - time) * Color.yellow + time * Color.white;
				c[x].outlineRenderer.material.color = (1f - time) * Color.yellow + time * Color.white;
			}
			for (var x = 0; x < d.Length - 1; x++)
			{
				d[x].filledRenderer.material.color = (1f - time) * (Color)clr[1 << x] + time * Color.white;
				d[x].outlineRenderer.material.color = (1f - time) * (Color)clr[1 << x] + time * Color.white;
			}
			yield return null;
		}
		for (var x = 0; x < b.Length; x++)
		{
			b[x].outlineRenderer.material.color = Color.white;
			b[x].outlineRenderer.material.color = Color.white;
		}
		for (var x = 0; x < c.Length; x++)
		{
			c[x].filledRenderer.material.color = Color.white;
			c[x].outlineRenderer.material.color = Color.white;
		}
		for (var x = 0; x < d.Length - 1; x++)
		{
			d[x].filledRenderer.material.color = Color.white;
			d[x].outlineRenderer.material.color = Color.white;
		}
	}

	// Update is called once per frame
	void Update () {
		if (!(bossActive && started) || animating || recoverable) return;
		if (tickCooldown && cooldown > 0f)
			cooldown -= Time.deltaTime;
		var curSolveCnt = bombInfo.GetSolvedModuleIDs().Count(a => !ignoreList.Contains(a));
		if (solveCountNonIgnored != curSolveCnt)
			solveCountNonIgnored = curSolveCnt;
		if (stageIdx < stgodr.Count)
			StartCoroutine(AnimateASet(delay: 0f, offset: 8, txt: (solveCountNonIgnored - stageIdx).ToString("00000")));
		if (cooldown <= float.Epsilon)
        {
			if (stageIdx < solveCountNonIgnored)
			{
				stageIdx++;
				tickCooldown = false;
				cooldown = 10f;
				StartCoroutine(BigAnim());
			}
        }
	}

	public class SlightGibberishSettings
    {
		public bool exhibitionMode = false;
		public int maxStagesAhead = 15;
		public int maxStagesBehind = 5;
    }

}
