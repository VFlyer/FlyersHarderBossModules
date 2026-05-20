using FlyersBossierEnums;
using System.Collections.Generic;

namespace FlyersBossierEnums
{
    public enum _1ll4Modifier
    {
        Stabilized = -1,
        Glitched = 0,
        Chaotic = 1,
        Custom = 2
    }
}

public class FlyersBossierSettings {
    public bool UseAuthorDynamicScoring = true;
    public bool EnableExperimentalStrikeRedirect = false;
    // 1ll 4
    public bool _1ll4AllowEarlySubmission = false;
    public _1ll4Modifier _1ll4Difficulty = _1ll4Modifier.Glitched;
    public int _1ll4UpperMaxValBal = 35;
    public int _1ll4LowerMaxValBal = 25;
    public bool _1ll4FullRandomColors = false;
    public bool _1ll4CycleComponentColors = false;
    public int _1ll4EarlySubmissionStageCountDrought = 10;
    // Clearance Code
    public bool CCEasyMode = false;
    public int CCColorsToCycle = 8;
    public int CCDigitsDisplayed = 10;
    public int CCDigitsRequired = 4;
    public bool CCRequireLastStage = true;
    public int CCStagesPerCodeOperSwap = 0;
    public int CCStagesPerSeqDirSwap = 2;
    public int CCStagesPerDisDirSwap = 1;
    public int CCStagesPerDistOperSwap = 0;
    public bool CCShuffleDigitsAlways = false;
    // Everchanging
    public bool ECExhibitionMode = false;
    public bool ECDynamicStageGen = true;
    public bool ECQuickIntro = true;
    public int[] ECBlacklistStageTypeIdxes = new int[0];
    // Slight Gibberish Twist
    public bool SGTExhibitionMode = false;
    public bool SGTPlayCamelliaTracks = false;
    public int SGTMaxStagesAhead = 15;
    public int SGTMaxStagesBehind = 5;
    public float[] SGTDynamicScalingRanges = new[] { 2.5f, 2, 2, 1.5f, 1.25f, 1f };
    public bool SGTRequireAllStages = false;
    public bool SGTScaleRequiredStages = false;
    // Pointer Pointer
    public bool PPUseAlternativeGen = false;
    public int PPPointsPerActivation = 2;
    public bool PPUse6x6Board = false;
}
