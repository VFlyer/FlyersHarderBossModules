using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
/** Summary:
  * Start at a given point and walk in a random direction to any unvisited cells.
  * Keep walking until the currently visited cell has no adjacent unvisited cells.
  * Backtrack to the previous cell and check if that cell has any adjacent unvisited cells. If so, walk randomly towards those cells.
  * Repeat backtracking until the starting cell is reached.
  * If at this point the starting cell has no adjacent unvisited cells, the maze is considered generated.
  */
public class MazeBacktracker : Maze {
    public MazeBacktracker(int length, int width)
    {
        maze = new string[length, width];
        markSpecial = new bool[length, width];
        curLength = length;
        curWidth = width;
    }

	public override IEnumerator AnimateGeneratedMaze(float delay)
    {
        isGenerating = true;
        bool[,] isRevealed = new bool[curLength, curWidth];
        var visitedCells = new List<int[]>() { new[] { curX, curY } };
        markSpecial[curX, curY] = true;
        while (visitedCells.Any())
        {
            //Debug.LogFormat("({0})",visitedCells.Select(a => a.Join(",")).Join(");("));
            int[] curPos = visitedCells.LastOrDefault();
            curX = curPos[0];
            curY = curPos[1];
            isRevealed[curX, curY] = true;
            bool[] validDirections = {
                curY - 1 >= 0 && !isRevealed[curX, curY - 1],
                curY + 1 < curWidth && !isRevealed[curX, curY + 1],
                curX + 1 < curLength && !isRevealed[curX + 1, curY],
                curX - 1 >= 0 && !isRevealed[curX - 1, curY],
            };
            if (validDirections.Any(a => a))
            {
                switch (new[] { directionUp, directionDown, directionRight, directionLeft }.Where(a => validDirections[a]).PickRandom())
                {
                    case directionUp:
                        {
                            CreatePassage(directionUp);
                            curY--;
                            break;
                        }
                    case directionDown:
                        {
                            CreatePassage(directionDown);
                            curY++;
                            break;
                        }
                    case directionRight:
                        {
                            CreatePassage(directionRight);
                            curX++;
                            break;
                        }
                    case directionLeft:
                        {
                            CreatePassage(directionLeft);
                            curX--;
                            break;
                        }
                }
                markSpecial[curX, curY] = true;
                visitedCells.Add(new[] { curX, curY });
            }
            else
            {
                markSpecial[curX, curY] = false;
                visitedCells.Remove(curPos);
            }
            if (delay > 0)
                yield return new WaitForSeconds(delay);
        }
        isGenerating = false;
		yield return null;
    }

}

