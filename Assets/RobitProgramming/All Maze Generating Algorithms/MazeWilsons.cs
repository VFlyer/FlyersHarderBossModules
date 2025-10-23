using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MazeWilsons : Maze {

    public MazeWilsons(int length, int width)
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
        int[,] directionsGoal = new int[curLength, curWidth];
        isRevealed[curX, curY] = true;
        while (!IsAllRevealed(isRevealed))
        {
            List<int[]> possibleStartingCoords = new List<int[]>();
            for (int x = 0; x < curLength; x++)
            {
                for (int y = 0; y < curWidth; y++)
                {
                    markSpecial[x, y] = false;
                    directionsGoal[x, y] = -1;
                    if (!isRevealed[x, y])
                        possibleStartingCoords.Add(new[] { x, y });
                }
            }
            int[] selectedCoord = possibleStartingCoords.PickRandom();

            curX = selectedCoord[0];
            curY = selectedCoord[1];
            while (!isRevealed[curX, curY])
            {
                markSpecial[curX, curY] = true;
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

                                directionsGoal[curX, curY] = directionUp;
                                curY--;
                                break;
                            }
                        case directionDown:
                            {
                                directionsGoal[curX, curY] = directionDown;
                                curY++;
                                break;
                            }
                        case directionRight:
                            {
                                directionsGoal[curX, curY] = directionRight;
                                curX++;
                                break;
                            }
                        case directionLeft:
                            {
                                directionsGoal[curX, curY] = directionLeft;
                                curX--;
                                break;
                            }
                    }
                }
                if (delay > 0)
                    yield return new WaitForSeconds(delay);
            }
            curX = selectedCoord[0];
            curY = selectedCoord[1];

            while (!isRevealed[curX,curY])
            {
                isRevealed[curX, curY] = true;
                markSpecial[curX, curY] = false;
                switch (directionsGoal[curX,curY])
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
                if (delay > 0)
                    yield return new WaitForSeconds(delay);
            }
        }
        isGenerating = false;
		yield return null;
    }

}

