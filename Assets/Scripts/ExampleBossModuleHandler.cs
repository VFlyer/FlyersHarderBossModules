using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ExampleBossModuleHandler : MonoBehaviour {

	public KMBombModule modself;
	public KMBossModuleExtensions bossHandler;
	public KMBombInfo bombInfo;
	public KMSelectable selfSelectable;
	public TextMesh textStage;

	private string[] ignoreIDlist = new string[] {
		"oneModID",
		"anotherModID",
		"randomSimonBossID",
		"aBossModuleID",
		"anotherBossModID",
	};

	static int moduleIdCounter = 1;
	int moduleId;
	private bool moduleSolved;

	int stagesToGenerate = 0;
	int currentStage = 0;
	bool hasStarted, canSolve;
	// Use this for initialization
	void Start () {
		moduleId = moduleIdCounter++;
		modself.OnActivate += delegate
        {
			hasStarted = true;
        };
		selfSelectable.OnInteract += delegate {
			selfSelectable.AddInteractionPunch();
			if (canSolve)
			{
				if (!moduleSolved)
				{
					modself.HandlePass();
					moduleSolved = true;
				}
			}
			return false;
		};
		string[] detectedModIDs = bossHandler.GetAttachedIgnoredModuleIDs(modself);
		if (detectedModIDs != null && detectedModIDs.Any())
		{
			ignoreIDlist = detectedModIDs;
		}
		stagesToGenerate = bombInfo.GetSolvableModuleIDs().Where(a => !ignoreIDlist.Contains(a)).Count();
		Debug.LogFormat("[Example Boss Module #{0}]: Total stages generatable: {1}", moduleId, stagesToGenerate);
		Debug.LogFormat("[Example Boss Module #{0}]: Detected Module IDs to ignore: {1}", moduleId, ignoreIDlist.Join(", "));
	}
	float currentDelay = 3f, stageDelayEach = 1f;
	// Update is called once per frame
	void Update () {
		if (hasStarted)
        {
			if (!canSolve)
			{
				currentDelay = Mathf.Max(0, currentDelay - Time.deltaTime);
				if (currentDelay <= 0)
				{
					int curSolves = bombInfo.GetSolvedModuleIDs().Where(a => !ignoreIDlist.Contains(a)).Count();
					if (curSolves >= currentStage)
					{
						currentDelay = stageDelayEach;
						currentStage++;
						if (currentStage > stagesToGenerate)
							canSolve = true;
					}
				}
				if (stagesToGenerate == 0)
					canSolve = true;
			}
			textStage.text = canSolve ? string.Format("!") : string.Format("{0}/{1}", currentStage, stagesToGenerate);
		}
		else
        {
			textStage.text = "";
        }
	}
}
