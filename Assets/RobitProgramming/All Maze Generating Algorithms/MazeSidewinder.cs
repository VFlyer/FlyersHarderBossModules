using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using uernd = UnityEngine.Random;

public class MazeSidewinder : Maze {

    bool leftToRight, topToBottom, vertGenerate;

    public MazeSidewinder(int length, int width)
    {
        maze = new string[length, width];
        markSpecial = new bool[length, width];
        curLength = length;
        curWidth = width;
        leftToRight = false;
        topToBottom = false;
        vertGenerate = false;
    }

    public MazeSidewinder(int length, int width, bool LRBias, bool TBBias, bool VStart)
    {
        maze = new string[length, width];
        markSpecial = new bool[length, width];
        curLength = length;
        curWidth = width;
        leftToRight = LRBias;
        topToBottom = TBBias;
        vertGenerate = VStart;
    }
    public void FlipStart()
    {
        vertGenerate = !vertGenerate;
    }
    public void ChangeBias(bool newLR, bool newTB)
    {
        leftToRight = newLR;
        topToBottom = newTB;
    }

	public override IEnumerator AnimateGeneratedMaze(float delay)
    {
        isGenerating = true;
        if (vertGenerate)
        { // Generating corridors vertically
            for (int y = 0; y < curWidth; y++)
            {
                curX = leftToRight ? 0 : curLength - 1;
                curY = topToBottom ? y : curWidth - 1 - y;
                if (y + 1 < curWidth)
                {
                    if (topToBottom)
                        CreatePassage(directionDown);
                    else
                        CreatePassage(directionUp);
                }
                if (delay > 0)
                    yield return new WaitForSeconds(delay);
            }
            for (int x = 1; x < curLength; x++)
            {
                List<int> curColIdxs = new List<int>();
                for (int y = 0; y < curWidth; y++)
                {
                    curX = leftToRight ? x : curLength - 1 - x;
                    curY = topToBottom ? y : curWidth - 1 - y;
                    curColIdxs.Add(curY);
                    bool forceVert = uernd.value < 0.5f;
                    if (y + 1 < curWidth)
                    {
                        if (!forceVert)
                        {
                            if (topToBottom)
                                CreatePassage(directionDown);
                            else
                                CreatePassage(directionUp);
                        }
                        else
                        {
                            int selectedRowIdx = curColIdxs.PickRandom();
                            curY = selectedRowIdx;
                            if (leftToRight)
                                CreatePassage(directionLeft);
                            else
                                CreatePassage(directionRight);
                            curColIdxs.Clear();
                        }
                    }
                    else
                    {
                        int selectedRowIdx = curColIdxs.PickRandom();
                        curY = selectedRowIdx;
                        if (leftToRight)
                            CreatePassage(directionLeft);
                        else
                            CreatePassage(directionRight);
                        curColIdxs.Clear();
                    }
                    if (delay > 0)
                        yield return new WaitForSeconds(delay);
                }
            }
        }
        else
        { // Generating corridors horizontally
            for (int x = 0; x < curLength; x++)
            {
                curX = leftToRight ? x : curLength - 1 - x;
                curY = topToBottom ? 0 : curWidth - 1;
                if (x + 1 < curLength)
                {
                    if (leftToRight)
                        CreatePassage(directionRight);
                    else
                        CreatePassage(directionLeft);
                }
                if (delay > 0)
                    yield return new WaitForSeconds(delay);
            }
            for (int y = 1; y < curWidth; y++)
            {
                List<int> curRowIdxs = new List<int>();
                for (int x = 0; x < curLength; x++)
                {
                    curX = leftToRight ? x : curLength - 1 - x;
                    curY = topToBottom ? y : curWidth - 1 - y;
                    curRowIdxs.Add(curX);
                    bool forceVert = uernd.value < 0.5f;
                    if (x + 1 < curLength)
                    {
                        if (!forceVert)
                        {
                            if (leftToRight)
                                CreatePassage(directionRight);
                            else
                                CreatePassage(directionLeft);
                        }
                        else
                        {
                            int selectedColIdx = curRowIdxs.PickRandom();
                            curX = selectedColIdx;
                            if (topToBottom)
                                CreatePassage(directionUp);
                            else
                                CreatePassage(directionDown);
                            curRowIdxs.Clear();
                        }
                    }
                    else
                    {
                        int selectedColIdx = curRowIdxs.PickRandom();
                        curX = selectedColIdx;
                        if (topToBottom)
                            CreatePassage(directionUp);
                        else
                            CreatePassage(directionDown);
                        curRowIdxs.Clear();
                    }
                    if (delay > 0)
                        yield return new WaitForSeconds(delay);
                }
            }
        }
        isGenerating = false;
		yield return null;
    }

}

