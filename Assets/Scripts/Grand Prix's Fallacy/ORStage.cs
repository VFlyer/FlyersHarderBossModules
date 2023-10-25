namespace ObscureRace
{
    public enum FlagRule
    {
        None,
        Yellow,
        SafetyCar,
        Red,
        Retirement,
        Disqualification,
    }
}

public class ORStage {
    public int[] deltasMS, idxOrderFinished;
    public ObscureRace.FlagRule currentFlagRule;
    public int extraArgFlagRule = -1;
}
