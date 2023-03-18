public class FlyersBossierSettings {
    public bool UseAuthorDynamicScoring = true;
    public bool EnableExperimentalStrikeRedirect = false;
    public bool ECExhibitionMode = false;
    public bool ECDynamicStageGen = true;
    public bool SGTExhibitionMode = false;
    public bool SGTPlayCamelliaTracks = false;
    public int SGTMaxStagesAhead = 15;
    public int SGTMaxStagesBehind = 5;
    public float[] SGTDynamicScalingRanges = new[] { 2, 2, 2, 1.5f, 1, 0.75f };
    public bool PPUseAlternativeGen = false;
    public int PPPointsPerActivation = 2;
}
