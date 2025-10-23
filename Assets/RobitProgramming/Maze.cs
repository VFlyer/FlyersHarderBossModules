using System.Collections;

public partial class Maze {

	protected const int directionUp = 0, directionDown = 1, directionRight = 2, directionLeft = 3;

	public string[,] maze;
	public bool[,] markSpecial;
	protected int curLength, curWidth, curX, curY;
	public int startingRow, startingCol;
	protected bool isGenerating = false;
	public Maze()
	{
		maze = new string[1, 1];
		curWidth = 1;
		curLength = 1;
	}
	public Maze(int length, int width)
	{
		maze = new string[length, width];
		markSpecial = new bool[length, width];
		curLength = length;
		curWidth = width;
	}
	public int ObtainOppositeDirection(int directionIdx)
	{
		switch(directionIdx)
        {
			case directionUp:
				return directionDown;
			case directionDown:
				return directionUp;
			case directionRight:
				return directionLeft;
			case directionLeft:
				return directionRight;
        }
		return -1;
	}
	public int GetLength()
    {
		return curLength;
    }
	public int GetWidth()
    {
		return curWidth;
    }
	public bool GetState()
    {
		return isGenerating;
    }

	public int GetCurX()
	{
		return curX;
	}

	public int GetCurY()
	{
		return curY;
	}

	public void FillMaze()
    {
		for (int x = 0; x < maze.GetLength(0); x++)
		{
			for (int y = 0; y < maze.GetLength(1); y++)
			{
				maze[x, y] = "";
			}
		}
	}
	/**
	 * <summary>Creates a passage with the specified direction index ranging from 0 - 3 inclusive from the current position.</summary>
	 * <param name="directionIdx">The index of the given direction to carve it to.</param>
	 */
	public void CreatePassage(int directionIdx)
    {
		switch (directionIdx)
        {
			case directionUp:
                {
					maze[curX, curY - 1] += "D";
					maze[curX, curY] += "U";
					break;
                }
			case directionDown:
				{
					maze[curX, curY + 1] += "U";
					maze[curX, curY] += "D";
					break;
				}
			case directionRight:
				{
					maze[curX + 1, curY] += "L";
					maze[curX, curY] += "R";
					break;
				}
			case directionLeft:
				{
					maze[curX - 1, curY] += "R";
					maze[curX, curY] += "L";
					break;
				}
			default:
				break;
        }
    }
	/**
	 * <summary>Creates a passage with the specified direction index ranging from 0 - 3 inclusive from a specified point.</summary>
	 * <param name="directionIdx">The index of the given direction to carve it to.</param>
	 * <param name="xCord">The x-index of the given direction to carve it from.</param>
	 * <param name="yCord">The y-index of the given direction to carve it from.</param>
	 */
	public void CreatePassageFrom(int xCord, int yCord, int directionIdx)
	{
		if (xCord < 0 || xCord >= curLength || yCord < 0 || yCord >= curWidth)
			return;

		switch (directionIdx)
		{
			case directionUp:
				{
					maze[xCord, yCord - 1] += "D";
					maze[xCord, yCord] += "U";
					break;
				}
			case directionDown:
				{
					maze[xCord, yCord + 1] += "U";
					maze[xCord, yCord] += "D";
					break;
				}
			case directionRight:
				{
					maze[xCord + 1, yCord] += "L";
					maze[xCord, yCord] += "R";
					break;
				}
			case directionLeft:
				{
					maze[xCord - 1, yCord] += "R";
					maze[xCord, yCord] += "L";
					break;
				}
			default:
				break;
		}
	}

	public void MoveToNewPosition(int newX, int newY)
    {
		curX = newX;
		curY = newY;
    }

	public IEnumerator AnimateGeneratedMaze()
    {
		yield return AnimateGeneratedMaze(0f);
    }

	public virtual IEnumerator AnimateGeneratedMaze(float delay)
	{
		yield return null;
	}

}
