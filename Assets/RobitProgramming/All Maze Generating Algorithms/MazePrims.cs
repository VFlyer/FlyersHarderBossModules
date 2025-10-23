using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MazePrims : Maze {

    bool leftToRightScan, topToBottomScan, HorizFirstScan;

    public MazePrims(int length, int width)
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
        List<int[]> coordGeneratable = new List<int[]>() { new[] { curX, curY } };

        while (coordGeneratable.Any())
        {
            //Debug.Log(coordGeneratable.Select(a => a.Join(",")).Join(" ; "));
            int[] randomlySelectedTile = coordGeneratable.PickRandom();
            curX = randomlySelectedTile[0];
            curY = randomlySelectedTile[1];
            isRevealed[curX, curY] = true;
            //Debug.LogFormat("({0})",visitedCells.Select(a => a.Join(",")).Join(");("));
            bool[] validDirections = {
                curY - 1 >= 0 && isRevealed[curX, curY - 1],
                curY + 1 < curWidth && isRevealed[curX, curY + 1],
                curX + 1 < curLength && isRevealed[curX + 1, curY],
                curX - 1 >= 0 && isRevealed[curX - 1, curY],
            };
            if (validDirections.Any(a => a))
            {
                switch (new[] { directionUp, directionDown, directionRight, directionLeft }.Where(a => validDirections[a]).PickRandom())
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

            if (curX - 1 >= 0 && !isRevealed[curX - 1, curY])
            {
                coordGeneratable.Add(new[] { curX - 1, curY });
                markSpecial[curX - 1, curY] = true;
            }
            if (curX + 1 < curLength && !isRevealed[curX + 1, curY])
            {
                coordGeneratable.Add(new[] { curX + 1, curY });
                markSpecial[curX + 1, curY] = true;
            }

            if (curY - 1 >= 0 && !isRevealed[curX, curY - 1])
            {
                coordGeneratable.Add(new[] { curX, curY - 1 });
                markSpecial[curX, curY - 1] = true;
            }

            if (curY + 1 < curWidth && !isRevealed[curX, curY + 1])
            {
                coordGeneratable.Add(new[] { curX, curY + 1 });
                markSpecial[curX, curY + 1] = true;
            }

            markSpecial[curX, curY] = false;
            coordGeneratable.Remove(randomlySelectedTile);

            List<int[]> remainingCoordinates = new List<int[]>();
            for (int x = 0; x < coordGeneratable.Count; x++)
            {
                int[] curCoordinate = coordGeneratable[x];
                bool isDupe = false;
                for (int y = 0; y < remainingCoordinates.Count && !isDupe; y++)
                {
                    if (remainingCoordinates[y].SequenceEqual(curCoordinate))
                        isDupe = true;
                }
                if (!isDupe)
                    remainingCoordinates.Add(curCoordinate);
            }
            coordGeneratable = remainingCoordinates;
            if (delay > 0)
                yield return new WaitForSeconds(delay);
        }
        isGenerating = false;
		yield return null;
    }

}

