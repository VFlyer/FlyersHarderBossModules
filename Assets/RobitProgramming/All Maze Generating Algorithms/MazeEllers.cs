using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using uernd = UnityEngine.Random;

public class MazeEllers : Maze {

    bool leftToRight, topToBottom, vertStart;

    public MazeEllers(int length, int width)
    {
        maze = new string[length, width];
        markSpecial = new bool[length, width];
        curLength = length;
        curWidth = width;
        leftToRight = false;
        topToBottom = false;
        vertStart = false;
    }
    public MazeEllers(int length, int width, bool VStart)
    {
        maze = new string[length, width];
        markSpecial = new bool[length, width];
        curLength = length;
        curWidth = width;
        leftToRight = false;
        topToBottom = false;
        vertStart = VStart;
    }
    public MazeEllers(int length, int width, bool LRBias, bool TBBias, bool VStart)
    {
        maze = new string[length, width];
        markSpecial = new bool[length, width];
        curLength = length;
        curWidth = width;
        leftToRight = LRBias;
        topToBottom = TBBias;
        vertStart = VStart;
    }
    public void FlipStart()
    {
        vertStart = !vertStart;
    }
    public void ChangeBias(bool newLR, bool newTB)
    {
        leftToRight = newLR;
        topToBottom = newTB;
    }

	public override IEnumerator AnimateGeneratedMaze(float delay)
    {
        isGenerating = true;
        if (vertStart) // Generating corridors column per column
        {
            int[] curGroupSet = new int[curWidth];
            int counterSetIdx = 1;
            for (int x = 0; x < curLength - 1; x++)
            {
                curX = leftToRight ? x : curLength - 1 - x;
                for (int y = 0; y < curWidth; y++)
                {
                    if (curGroupSet[y] == 0)
                        curGroupSet[y] = counterSetIdx++;
                }
                for (int y = 0; y < curWidth - 1; y++)
                {
                    curY = topToBottom ? y : curWidth - 1 - y;
                    bool generateVertCorridor = uernd.value < 0.5f && (curGroupSet[y + 1] != curGroupSet[y]);
                    if (generateVertCorridor) // Check if the corridor can be generated vertically.
                    {
                        int connectingGroupIdx = curGroupSet[y + 1];
                        for (int z = 0; z < curLength; z++)
                        {
                            if (curGroupSet[z] == connectingGroupIdx)
                                curGroupSet[z] = curGroupSet[y];
                        }
                        if (topToBottom)
                            CreatePassage(directionDown);
                        else
                            CreatePassage(directionUp);
                    }
                    if (delay > 0)
                        yield return new WaitForSeconds(delay);
                }
                //Debug.Log(curGroupSet.Join());
                int[] lastGroupSet = curGroupSet.ToArray();
                curGroupSet = new int[curLength];
                curX = leftToRight ? x + 1 : curLength - 2 - x;
                int[] distinctGroups = lastGroupSet.Distinct().ToArray();
                foreach (int oneGroup in distinctGroups)
                {
                    List<int> groupIdxsStretched = new List<int>();
                    for (int y = 0; y < curWidth; y++)
                    {
                        if (lastGroupSet[y] == oneGroup)
                        {
                            groupIdxsStretched.Add(y);
                        }
                    }
                    groupIdxsStretched.Shuffle();
                    //Debug.Log(groupIdxsStretched.Join());
                    foreach (int y in groupIdxsStretched)
                    {
                        if (groupIdxsStretched.First() != y && uernd.value < 0.5f) break;
                        curY = topToBottom ? y : curLength - 1 - y;
                        curGroupSet[y] = oneGroup;
                        if (leftToRight)
                        {
                            CreatePassage(directionLeft);
                        }
                        else
                        {
                            CreatePassage(directionRight);
                        }
                        if (delay > 0)
                            yield return new WaitForSeconds(delay);
                    }
                }
            }
            for (int x = 0; x < curWidth; x++)
            {
                if (curGroupSet[x] == 0)
                    curGroupSet[x] = counterSetIdx++;
            }
            //Debug.Log(curGroupSet.Join());
            for (int y = 0; y < curWidth - 1; y++)
            {
                curY = topToBottom ? y : curWidth - 1 - y;
                bool generateVertCorridor = curGroupSet[y + 1] != curGroupSet[y]; // Connect groups that belong in different sets.
                if (generateVertCorridor) // Check if the corridor can be generated horizontally.
                {
                    int connectingGroupIdx = curGroupSet[y + 1];
                    for (int z = 0; z < curWidth; z++)
                    {
                        if (curGroupSet[z] == connectingGroupIdx)
                            curGroupSet[z] = curGroupSet[y];
                    }
                    if (topToBottom)
                        CreatePassage(directionDown);
                    else
                        CreatePassage(directionUp);
                }
                if (delay > 0)
                    yield return new WaitForSeconds(delay);
            }
        }
        else
        { // Generating corridors row per row
            int[] curGroupSet = new int[curWidth];
            int counterSetIdx = 1;
            for (int y = 0; y < curWidth - 1; y++)
            {
                curY = topToBottom ? y : curWidth - 1 - y;
                for (int x = 0; x < curLength; x++)
                {
                    if (curGroupSet[x] == 0)
                        curGroupSet[x] = counterSetIdx++;
                }
                for (int x = 0; x < curLength - 1; x++)
                {
                    curX = leftToRight ? x : curLength - 1 - x;
                    bool generateHorizCorridor = uernd.value < 0.5f && (curGroupSet[x + 1] != curGroupSet[x]);
                    if (generateHorizCorridor) // Check if the corridor can be generated horizontally.
                    {
                        int connectingGroupIdx = curGroupSet[x + 1];
                        for (int z = 0; z < curLength; z++)
                        {
                            if (curGroupSet[z] == connectingGroupIdx)
                                curGroupSet[z] = curGroupSet[x];
                        }
                        
                        if (leftToRight)
                        {
                            CreatePassage(directionRight);
                        }
                        else
                        {
                            CreatePassage(directionLeft);
                        }
                    }
                    if (delay > 0)
                        yield return new WaitForSeconds(delay);
                }
                //Debug.Log(curGroupSet.Join());
                int[] lastGroupSet = curGroupSet.ToArray();
                curGroupSet = new int[curWidth];
                curY = topToBottom ? y + 1 : curWidth - 2 - y;
                int[] distinctGroups = lastGroupSet.Distinct().ToArray();
                foreach (int oneGroup in distinctGroups)
                {
                    List<int> groupIdxsStretched = new List<int>();
                    for (int x = 0; x < curLength; x++)
                    {
                        if (lastGroupSet[x] == oneGroup)
                        {
                            groupIdxsStretched.Add(x);
                        }
                    }
                    groupIdxsStretched.Shuffle();
                    //Debug.Log(groupIdxsStretched.Join());
                    foreach (int x in groupIdxsStretched)
                    {
                        if (groupIdxsStretched.First() != x && uernd.value < 0.5f) break;
                        curX = leftToRight ? x : curLength - 1 - x;
                        curGroupSet[x] = oneGroup;
                        if (topToBottom)
                            CreatePassage(directionUp);
                        else
                            CreatePassage(directionDown);
                        if (delay > 0)
                            yield return new WaitForSeconds(delay);
                    }
                }
            }
            for (int x = 0; x < curLength; x++)
            {
                if (curGroupSet[x] == 0)
                    curGroupSet[x] = counterSetIdx++;
            }
            //Debug.Log(curGroupSet.Join());
            for (int x = 0; x < curLength - 1; x++)
            {
                curX = leftToRight ? x : curLength - 1 - x;
                bool generateHorizCorridor = curGroupSet[x + 1] != curGroupSet[x]; // Check if groups belong in different sets.
                if (generateHorizCorridor) // Check if the corridor can be generated horizontally.
                {
                    int connectingGroupIdx = curGroupSet[x + 1];
                    for (int z = 0; z < curLength; z++)
                    {
                        if (curGroupSet[z] == connectingGroupIdx)
                            curGroupSet[z] = curGroupSet[x];
                    }
                    if (leftToRight)
                    {
                        CreatePassage(directionRight);
                    }
                    else
                    {
                        CreatePassage(directionLeft);
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

