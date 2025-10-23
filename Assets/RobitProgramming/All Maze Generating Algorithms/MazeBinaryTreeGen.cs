using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public class MazeBinaryTree : Maze {

    bool biasEast, biasSouth;

    public MazeBinaryTree(int length, int width)
    {
        maze = new string[length, width];
        markSpecial = new bool[length, width];
        curLength = length;
        curWidth = width;
        biasEast = false;
        biasSouth = false;
    }

    public MazeBinaryTree(int length, int width, bool LRBias, bool TBBias)
    {
        maze = new string[length, width];
        markSpecial = new bool[length, width];
        curLength = length;
        curWidth = width;
        biasEast = LRBias;
        biasSouth = TBBias;
    }

    public void ChangeBias(bool newLR, bool newTB)
    {
        biasEast = newLR;
        biasSouth = newTB;
    }

	public override IEnumerator AnimateGeneratedMaze(float delay)
    {
        isGenerating = true;
        for (int x = 0; x < curLength; x++)
        {
            for (int y = 0; y < curWidth; y++)
            {
                /*
                curX = biasEast ? x : curLength - 1 - x;
                curY = biasSouth ? y : curWidth - 1 - y;
                */
                curX = x;
                curY = y;
                bool[] validDirections = {
                    curY - 1 >= 0 && !biasSouth,
                    curY + 1 < curWidth && biasSouth,
                    curX + 1 < curLength && biasEast,
                    curX - 1 >= 0 && !biasEast,
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
                if (delay > 0)
                    yield return new WaitForSeconds(delay);
            }
        }
        isGenerating = false;
		yield return null;
    }

}

