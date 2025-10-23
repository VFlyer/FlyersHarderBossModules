using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
/** <summary>Summary:
* Start at a given point and walk in a random direction to any unvisited cells.
* Keep walking until the currently visited cell has no adjacent unvisited cells.
* EITHER:
* - Randomly pick an unvisited cell and check if it is adjacent to a visited cell. Connect that cell and continue from there.
* - Scan row by row, column by column until an unvisited cell adjacent to a visited cell is picked. Connect that cell and continue from there.</summary>
*/
public class MazeHuntAndKill : Maze {

    bool leftToRightScan, topToBottomScan, vertFirstScan, randomScan;
    public MazeHuntAndKill(int length, int width)
    {
        maze = new string[length, width];
        markSpecial = new bool[length, width];
        curLength = length;
        curWidth = width;
        leftToRightScan = false;
        topToBottomScan = false;
        vertFirstScan = false;
        randomScan = true;
    }
    public MazeHuntAndKill(int length, int width, bool scanRandomly)
    {
        maze = new string[length, width];
        markSpecial = new bool[length, width];
        curLength = length;
        curWidth = width;
        leftToRightScan = false;
        topToBottomScan = false;
        vertFirstScan = false;
        randomScan = scanRandomly;
    }
    /** VFirst: Scan vertically first before scanning horizontally, known as column to column.
     * TBScan: Scan top to bottom if true. Otherwise, scan bottom to top.
     * LRScan: Scan left to right if true. Otherwise, scan right to left.
     */
    public MazeHuntAndKill(int length, int width, bool LRScan, bool TBScan, bool VFirst)
    {
        maze = new string[length, width];
        markSpecial = new bool[length, width];
        curLength = length;
        curWidth = width;
        leftToRightScan = LRScan;
        topToBottomScan = TBScan;
        vertFirstScan = VFirst;
        randomScan = false;
    }
    public void UseRandomScan()
    {
        randomScan = true;
    }
    public void UseHorizVertScanning(bool newLR, bool newTB, bool newVFirst)
    {
        randomScan = false;
        leftToRightScan = newLR;
        topToBottomScan = newTB;
        vertFirstScan = newVFirst;
    }

	public override IEnumerator AnimateGeneratedMaze(float delay)
    {
        isGenerating = true;
        bool[,] isRevealed = new bool[curLength, curWidth];
        while (curX != -1 && curY != -1)
        {
            //Debug.LogFormat("({0})",visitedCells.Select(a => a.Join(",")).Join(");("));
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
            }
            else
            {
                if (randomScan)
                {
                    List<int[]> coordinatesUnvisited = new List<int[]>();

                    for (int x = 0; x < curLength; x++)
                    {
                        for (int y = 0; y < curWidth; y++)
                        {
                            if (!isRevealed[x, y])
                                coordinatesUnvisited.Add(new[] { x, y });
                        }
                    }
                    if (coordinatesUnvisited.Any())
                    {
                        do
                        {
                            int[] randomCoordinate = coordinatesUnvisited.PickRandom();
                            curX = randomCoordinate[0];
                            curY = randomCoordinate[1];
                            if (delay > 0)
                                yield return new WaitForSeconds(delay);
                        }
                        while (!((curX - 1 >= 0 && isRevealed[curX - 1, curY]) ||
                                        (curX + 1 < curLength && isRevealed[curX + 1, curY]) ||
                                        (curY - 1 >= 0 && isRevealed[curX, curY - 1]) ||
                                        (curY + 1 < curWidth && isRevealed[curX, curY + 1])));
                        if (curX >= 0 && curX < curLength && curY < curWidth && curY >= 0)
                        {
                            bool[] validConnections = {
                        curY - 1 >= 0 && isRevealed[curX, curY - 1],
                        curY + 1 < curWidth && isRevealed[curX, curY + 1],
                        curX + 1 < curLength && isRevealed[curX + 1, curY],
                        curX - 1 >= 0 && isRevealed[curX - 1, curY],
                        };
                            if (validConnections.Any(a => a))
                            {
                                switch (new[] { directionUp, directionDown, directionRight, directionLeft }.Where(a => validConnections[a]).PickRandom())
                                {
                                    case directionUp:
                                        {
                                            CreatePassage(directionUp);
                                            break;
                                        }
                                    case directionDown:
                                        {
                                            CreatePassage(directionDown);
                                            break;
                                        }
                                    case directionRight:
                                        {
                                            CreatePassage(directionRight);
                                            break;
                                        }
                                    case directionLeft:
                                        {
                                            CreatePassage(directionLeft);
                                            break;
                                        }
                                }
                            }
                        }
                    }
                    else
                        break;
                }
                else
                {
                    var foundX = -1;
                    var foundY = -1;
                    if (vertFirstScan)
                    {
                        for (var scanX = 0; scanX < curLength && foundX == -1 && foundY == -1; scanX++)
                        {
                            for (var scanY = 0; scanY < curWidth && foundX == -1 && foundY == -1; scanY++)
                            {
                                var curScanX = leftToRightScan ? scanX : curLength - 1 - scanX;
                                var curScanY = topToBottomScan ? scanY : curWidth - 1 - scanY;
                                curX = curScanX;
                                curY = curScanY;
                                if (delay > 0)
                                    yield return new WaitForSeconds(delay);
                                if (!isRevealed[curScanX, curScanY])
                                {
                                    if ((curScanX - 1 >= 0 && isRevealed[curScanX - 1, curScanY]) ||
                                        (curScanX + 1 < curLength && isRevealed[curScanX + 1, curScanY]) ||
                                        (curScanY - 1 >= 0 && isRevealed[curScanX, curScanY - 1]) ||
                                        (curScanY + 1 < curWidth && isRevealed[curScanX, curScanY + 1])
                                        )
                                    {
                                        foundX = curScanX;
                                        foundY = curScanY;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        for (var scanY = 0; scanY < curWidth && foundX == -1 && foundY == -1; scanY++)
                        {
                            for (var scanX = 0; scanX < curLength && foundX == -1 && foundY == -1; scanX++)
                            {
                                var curScanX = leftToRightScan ? scanX : curLength - 1 - scanX;
                                var curScanY = topToBottomScan ? scanY : curWidth - 1 - scanY;
                                curX = curScanX;
                                curY = curScanY;
                                if (delay > 0)
                                    yield return new WaitForSeconds(delay);
                                if (!isRevealed[curScanX, curScanY])
                                {
                                    if ((curScanX - 1 >= 0 && isRevealed[curScanX - 1, curScanY]) ||
                                        (curScanX + 1 < curLength && isRevealed[curScanX + 1, curScanY]) ||
                                        (curScanY - 1 >= 0 && isRevealed[curScanX, curScanY - 1]) ||
                                        (curScanY + 1 < curWidth && isRevealed[curScanX, curScanY + 1])
                                        )
                                    {
                                        foundX = curScanX;
                                        foundY = curScanY;
                                        break;
                                    }
                                }

                            }
                        }
                    }
                    curX = foundX;
                    curY = foundY;
                    if (curX >= 0 && curX < curLength && curY < curWidth && curY >= 0)
                    {
                        bool[] validConnections = {
                        curY - 1 >= 0 && isRevealed[curX, curY - 1],
                        curY + 1 < curWidth && isRevealed[curX, curY + 1],
                        curX + 1 < curLength && isRevealed[curX + 1, curY],
                        curX - 1 >= 0 && isRevealed[curX - 1, curY],
                        };
                        if (validConnections.Any(a => a))
                        {
                            switch (new[] { directionUp, directionDown, directionRight, directionLeft }.Where(a => validConnections[a]).PickRandom())
                            {
                                case directionUp:
                                    {
                                        CreatePassage(directionUp);
                                        break;
                                    }
                                case directionDown:
                                    {
                                        CreatePassage(directionDown);
                                        break;
                                    }
                                case directionRight:
                                    {
                                        CreatePassage(directionRight);
                                        break;
                                    }
                                case directionLeft:
                                    {
                                        CreatePassage(directionLeft);
                                        break;
                                    }
                            }
                        }
                    }
                }
            }
            if (delay > 0)
                yield return new WaitForSeconds(delay);
        }
        isGenerating = false;
		yield return null;
    }

}

