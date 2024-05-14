using System.Collections.Generic;

public class FlyersBossierSettings {
    public bool UseAuthorDynamicScoring = true;
    public bool CCEasyMode = false;
    public bool EnableExperimentalStrikeRedirect = false;
    public bool ECExhibitionMode = false;
    public bool ECDynamicStageGen = true;
    public List<int> ECBlacklistStageTypeIdxes = new List<int>();
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
