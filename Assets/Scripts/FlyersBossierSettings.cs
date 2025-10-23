using FlyersBossierEnums;
using System.Collections.Generic;

namespace FlyersBossierEnums
{
    public enum _1ll4Modifier
    {
        Stabilized = -1,
        Normal = 0,
        Chaotic = 1,
        Custom = 2
    }
}

public class FlyersBossierSettings {
    public bool UseAuthorDynamicScoring = true;
    public _1ll4Modifier _1ll4Difficulty = _1ll4Modifier.Normal;
    public int _1ll4UpperMaxValBal = 35;
    public int _1ll4LowerMaxValBal = 25;
    public bool _1ll4FullRandomColors = false;
    public bool _1ll4CycleComponentColors = false;
    public bool _1ll4AllowEarlySubmission = false;
    public int _1ll4EarlySubmissionStageCountDrought = 10;
    public bool CCEasyMode = false;
    public bool EnableExperimentalStrikeRedirect = false;
    public bool ECExhibitionMode = false;
    public bool ECDynamicStageGen = true;
    public int[] ECBlacklistStageTypeIdxes = new int[0];
    public bool SGTExhibitionMode = false;
    public bool SGTPlayCamelliaTracks = false;
    public int SGTMaxStagesAhead = 15;
    public int SGTMaxStagesBehind = 5;
    public float[] SGTDynamicScalingRanges = new[] { 2.5f, 2, 2, 1.5f, 1.25f, 1f };
    public bool SGTRequireAllStages = false;
    public bool PPUseAlternativeGen = false;
    public int PPPointsPerActivation = 2;
    public bool PPUse6x6Board = false;
}
