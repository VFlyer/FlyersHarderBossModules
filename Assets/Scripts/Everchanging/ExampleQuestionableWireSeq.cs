using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ExampleQuestionableWireSeq : MonoBehaviour {

	public QuestionableWireSequencesCore WireSequencesCore;
	public KMBombModule modSelf;
	public KMAudio mAudio;
	public TextMesh digitsDisplayMesh, panelMesh;
	public Animation[] doors;
	public int stagesToGenerate = 10;
	List<int> calculatedValues = new List<int>();
	bool[] wiresCut = new bool[0];
	readonly int[][] possibleWireCountsSpecific = new int[][] {
		new[] { 3, 3, 3, 1 },
        new[] { 3, 3, 2, 2 },
	};
	int[] selectedWireCountOrder;
	bool interactable = false, timerRunning;
	float timeLeftCurPanel = 3f, startTime = 3f;
	static int modIDCnt;
	int firstPanelIdxUnfinished = 0, targetPanelIdx, modID;
    Color[] listColors = { Color.white, Color.red, new Color(0.1f, 0.1f, 1) };
	bool? buttonDirHeld = null;
	Vector3 initLeftArrowLocalPos, initRightArrowLocalPos;
	// Use this for initialization
	void Start () {
		modID = ++modIDCnt;
		for (int x = 0; x < stagesToGenerate; x++)
			calculatedValues.Add(Random.Range(0, 10));
		wiresCut = new bool[stagesToGenerate];
		WireSequencesCore.AssignRuleSeedTargetValues();
		WireSequencesCore.AssignObtainedValues(calculatedValues);

		selectedWireCountOrder = possibleWireCountsSpecific.PickRandom().ToArray().Shuffle();

		WireSequencesCore.arrowDown.OnInteract += delegate {
			WireSequencesCore.arrowDown.AddInteractionPunch();
			mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, WireSequencesCore.arrowDown.transform);
			buttonDirHeld = true;
			if (interactable)
				TryIncrement();
			return false;
		};
		WireSequencesCore.arrowDown.OnInteractEnded += delegate {
			mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, WireSequencesCore.arrowDown.transform);
			buttonDirHeld = null;
		};
		WireSequencesCore.arrowUp.OnInteract += delegate {
			WireSequencesCore.arrowUp.AddInteractionPunch();
			mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, WireSequencesCore.arrowUp.transform);
			buttonDirHeld = false;
			if (interactable)
				TryDecrement();
			return false;
		};
		WireSequencesCore.arrowUp.OnInteractEnded += delegate {
			mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, WireSequencesCore.arrowUp.transform);
			buttonDirHeld = null;
		};
        for (var x = 0; x < WireSequencesCore.wiresAllCompact.Length; x++)
        {
			int y = x;
			WireSequencesCore.wiresAllCompact[x].wireSelectable.OnInteract = delegate {
				if (!WireSequencesCore.wiresAllCompact[y].isCut && interactable)
				{
					WireSequencesCore.wiresAllCompact[y].wireSelectable.AddInteractionPunch();
					mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSnip, WireSequencesCore.wiresAllCompact[y].transform);
					HandleWireCut(y);
				}
				return false;
			};
        }

		modSelf.OnActivate += delegate {
			StartCoroutine(HandleOpenAnim());
			digitsDisplayMesh.text = calculatedValues.Join("");
		};
		WireSequencesCore.MimicLogging(string.Format("[Questionable Wire Sequence #{0}]", modID));
		QuickLog("Selected wire count groups: {0}", selectedWireCountOrder.Join(", "));
		digitsDisplayMesh.text = "";
		WireSequencesCore.displayTextsAllWires.Select(a => a.text = "");
		panelMesh.text = "";
		initLeftArrowLocalPos = WireSequencesCore.arrowUp.transform.localPosition;
		initRightArrowLocalPos = WireSequencesCore.arrowDown.transform.localPosition;
	}
	void QuickLog(string toLog, params object[] args)
    {
		Debug.LogFormat("[Questionable Wire Sequence #{0}] {1}", modID, string.Format(toLog, args));
    }

	void HandleWireCut(int deltaModifier)
    {
		var panelIdx = WireSequencesCore.currentPanelIdx;
		var allowedOffsets = Enumerable.Range(0, selectedWireCountOrder[panelIdx]);
		if (!allowedOffsets.Contains(deltaModifier)) return;
		var wireCurIdx = selectedWireCountOrder.Take(panelIdx).Sum() + deltaModifier;
		wiresCut[wireCurIdx] = true;
		if (!WireSequencesCore.wiresToCut[wireCurIdx])
		{
			QuickLog("Cutting wire #{0} in panel #{1} is not safe! That's a strike.", deltaModifier + 1, panelIdx + 1);
			modSelf.HandleStrike();
		}
		UpdateCurrentPanel();
	}

	void SolveModule()
    {
		mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSequenceMechanism, transform);
		interactable = false;
		modSelf.HandlePass();
		doors.First().Play("DoorAnimCloseT");
		doors.Last().Play("DoorAnimCloseB");
	}

	IEnumerator HandleOpenAnim()
    {
		mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSequenceMechanism, transform);
		UpdateCurrentPanel();
		yield return null;
		doors.First().Play("DoorAnimOpenT");
		doors.Last().Play("DoorAnimOpenB");
		interactable = true;
	}
	IEnumerator DelayNextSwitchingAnim()
    {
		timerRunning = true;
		while (timeLeftCurPanel > 0f)
		{
			panelMesh.text = targetPanelIdx.ToString();
			panelMesh.color = targetPanelIdx < firstPanelIdxUnfinished ? Color.green : Color.yellow;
			timeLeftCurPanel -= Time.deltaTime;
			yield return null;
		}
		while (buttonDirHeld != null)
		{
			targetPanelIdx = (bool)buttonDirHeld ? firstPanelIdxUnfinished : 0;
			panelMesh.text = targetPanelIdx.ToString();
			panelMesh.color = targetPanelIdx < firstPanelIdxUnfinished ? Color.green : Color.yellow;
			yield return null;
		}
		timerRunning = false;
		if (targetPanelIdx != WireSequencesCore.currentPanelIdx)
		{
			WireSequencesCore.currentPanelIdx = targetPanelIdx;
			yield return AnimateSwitchingAnim();
		}
		timeLeftCurPanel = startTime;
    }

	IEnumerator AnimateSwitchingAnim()
    {
		interactable = false;
		mAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSequenceMechanism, transform);
		doors.First().Play("DoorAnimCloseT");
		doors.Last().Play("DoorAnimCloseB");
		while (doors.Any(a => a.isPlaying))
			yield return null;
		UpdateCurrentPanel();
		doors.First().Play("DoorAnimOpenT");
		doors.Last().Play("DoorAnimOpenB");
		while (doors.Any(a => a.isPlaying))
			yield return null;
		interactable = true;
	}
	bool IsCurrentPanelInvalid()
    {
		var panelIdx = WireSequencesCore.currentPanelIdx;
		var startIdx = selectedWireCountOrder.Take(panelIdx).Sum();

		return Enumerable.Range(startIdx, selectedWireCountOrder[panelIdx]).Any(a => WireSequencesCore.wiresToCut[a] && !wiresCut[a]);
    }
	void TryIncrement()
    {
		if (IsCurrentPanelInvalid())
		{
			QuickLog("At least 1 wire in panel #{0} is not cut! That's a strike.", WireSequencesCore.currentPanelIdx + 1);
			modSelf.HandleStrike();
			return;
		}
		if (targetPanelIdx < firstPanelIdxUnfinished || (!timerRunning && targetPanelIdx == firstPanelIdxUnfinished))
        {
			targetPanelIdx++;
		}
		if (!timerRunning)
			if (firstPanelIdxUnfinished <= WireSequencesCore.currentPanelIdx)
			{
				WireSequencesCore.currentPanelIdx = targetPanelIdx;
				firstPanelIdxUnfinished = targetPanelIdx;
				if (WireSequencesCore.currentPanelIdx < selectedWireCountOrder.Length)
				{
					StartCoroutine(AnimateSwitchingAnim());
				}
				else
					SolveModule();
			}
			else
			{
				StartCoroutine(DelayNextSwitchingAnim());
			}
	}
	void TryDecrement()
    {
		if (targetPanelIdx > 0)
		{
			targetPanelIdx--;
		}
		if (!timerRunning)
			{
				StartCoroutine(DelayNextSwitchingAnim());
			}
	}
	void UpdateCurrentPanel()
    {
		var panelIdx = WireSequencesCore.currentPanelIdx;
		var wireStartIdx = selectedWireCountOrder.Take(panelIdx).Sum();
        for (var x = 0; x < WireSequencesCore.wiresAllCompact.Length; x++)
        {
			if (x < selectedWireCountOrder[panelIdx])
            {
				var curIdx = wireStartIdx + x;
				var curWire = WireSequencesCore.wiresAllCompact[x];
				curWire.gameObject.SetActive(true);
				WireSequencesCore.displayTextsAllWires[x].text = (1 + WireSequencesCore.valueIndexes[curIdx]).ToString();
				curWire.isCut = wiresCut[curIdx];
				for (var e = 0; e < curWire.wireRendersModifyable.Length; e++)
					curWire.wireRendersModifyable[e].material.color = listColors[WireSequencesCore.wireIdxColors[curIdx]];
				curWire.UpdateRenderer();
			}
			else
            {
				WireSequencesCore.wiresAllCompact[x].gameObject.SetActive(false);
				WireSequencesCore.displayTextsAllWires[x].text = "";
			}
        }
		panelMesh.text = panelIdx.ToString();
		panelMesh.color = panelIdx < firstPanelIdxUnfinished ? Color.green : Color.yellow;
	}
	void Update()
    {
		WireSequencesCore.arrowDown.transform.localPosition = initRightArrowLocalPos + (buttonDirHeld == true ? Vector3.down : Vector3.zero) * 0.01f;
		WireSequencesCore.arrowUp.transform.localPosition = initLeftArrowLocalPos + (buttonDirHeld == false ? Vector3.down : Vector3.zero) * 0.01f;
	}
}