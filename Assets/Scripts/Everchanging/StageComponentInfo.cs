public class StageComponentInfoAll
{
    // Individual
    public bool[] enabledComponents;
    public string[] cubeDisplayTexts;
    public int ledColorIdx;
    public int[] displayedNumbers;
    public int[] fixedRotationIdx;
    public StageComponentInfoAll()
    {
        enabledComponents = new bool[3];
        cubeDisplayTexts = new string[6];
        ledColorIdx = 0;
        displayedNumbers = new int[1];
        fixedRotationIdx = new int[0];
    }
}
