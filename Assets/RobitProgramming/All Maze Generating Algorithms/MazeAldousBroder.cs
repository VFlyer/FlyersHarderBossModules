using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MazeAldousBroder : Maze {

    public MazeAldousBroder(int length, int width)
    {
        maze = new string[length, width];
        markSpecial = new bool[length, width];
        curLength = length;
        curWidth = width;
    }

    bool IsAllRevealed(bool[,] revealArray)
    {
        bool output = true;
        for (int x = 0; x < revealArray.GetLength(0); x++)
        {
            for (int y = 0; y < revealArray.GetLength(1); y++)
            {
                output &= revealArray[x, y];
            }
        }

        return output;
    }

    public override IEnumerator AnimateGeneratedMaze(float delay)
    {
        isGenerating = true;
        bool[,] isRevealed = new bool[curLength, curWidth];
        isRevealed[curX, curY] = true;
        while (!IsAllRevealed(isRevealed))
        {
            bool[] validDirections = {
                curY - 1 >= 0,
                curY + 1 < curWidth,
                curX + 1 < curLength,
                curX - 1 >= 0,
            };
            if (validDirections.Any(a => a))
            {
                switch (new[] { directionUp, directionDown, directionRight, directionLeft }.Where(a => validDirections[a]).PickRandom())
                {
                    case directionUp:
                        {
                            if (!isRevealed[curX, curY - 1])
                                CreatePassage(directionUp);
                            curY--;
                            break;
                        }
                    case directionDown:
                        {
                            if (!isRevealed[curX, curY + 1])
                                CreatePassage(directionDown);
                            curY++;
                            break;
                        }
                    case directionRight:
                        {
                            if (!isRevealed[curX + 1, curY])
                                CreatePassage(directionRight);
                            curX++;
                            break;
                        }
                    case directionLeft:
                        {
                            if (!isRevealed[curX - 1, curY])
                                CreatePassage(directionLeft);
                            curX--;
                            break;
                        }
                }
            }
            isRevealed[curX, curY] = true;
            if (delay > 0)
                yield return new WaitForSeconds(delay);
        }
        isGenerating = false;
		yield return null;
    }

}

