using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using uernd = UnityEngine.Random;
/** Summary:
  * Start at a given point and walk in a random direction to any unvisited cells. Add that visited cell into a bag.
  * Pick any cell that are visited so far.
  * If there are no adjacent unvisited cells around the picked cell, remove that cell from the list. Otherwise, walk in a random direction for that cell.
  * Repeat until all cells are cleared from the bag.
  */
public class MazeGrowingTree : Maze {

    int weightNewest, weightOldest, weightRandom;
    public MazeGrowingTree(int length, int width)
    {
        maze = new string[length, width];
        markSpecial = new bool[length, width];
        curLength = length;
        curWidth = width;
        weightNewest = 1;
        weightOldest = 1;
        weightRandom = 1;
    }
    public MazeGrowingTree(int length, int width, int newestWeight = 0, int oldestWeight = 0, int randomWeight = 0)
    {
        maze = new string[length, width];
        markSpecial = new bool[length, width];
        curLength = length;
        curWidth = width;
        weightNewest = newestWeight;
        weightOldest = oldestWeight;
        weightRandom = randomWeight;
    }

    public override IEnumerator AnimateGeneratedMaze(float delay)
    {
        isGenerating = true;
        bool[,] isRevealed = new bool[curLength, curWidth];
        var visitedCells = new List<int[]>() { new[] { curX, curY } };
        // Assign the weight of that given cell selection.
        List<int> idxOdds = new List<int>();
        for (int x = 0; x < weightNewest; x++)
            idxOdds.Add(0);
        for (int x = 0; x < weightOldest; x++)
            idxOdds.Add(1);
        for (int x = 0; x < weightRandom; x++)
            idxOdds.Add(2);

        isRevealed[curX, curY] = true;
        markSpecial[curX, curY] = true;
        while (visitedCells.Any())
        {
            //Debug.LogFormat("({0})",visitedCells.Select(a => a.Join(",")).Join(");("));
            int[] curPos;
            // Check for any values determined from the bag.
            if (idxOdds.Any())
            {
                switch(idxOdds.PickRandom())
                {
                    case 0:
                        curPos = visitedCells.Last();
                        break;
                    case 1:
                        curPos = visitedCells.First();
                        break;
                    case 2:
                    default:
                        curPos = visitedCells.PickRandom();
                        break;
                }
            }
            else
            {
                switch (uernd.Range(0, 3))
                {
                    case 0:
                        curPos = visitedCells.Last();
                        break;
                    case 1:
                        curPos = visitedCells.First();
                        break;
                    case 2:
                    default:
                        curPos = visitedCells.PickRandom();
                        break;
                }
            }

            curX = curPos[0];
            curY = curPos[1];
            
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
                isRevealed[curX, curY] = true;
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

