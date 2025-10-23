using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using uernd = UnityEngine.Random;

public class MazeRecursiveDivision : Maze {

    public MazeRecursiveDivision(int length, int width)
    {
        maze = new string[length, width];
        markSpecial = new bool[length, width];
        curLength = length;
        curWidth = width;
    }

    IEnumerator SplitMaze(int curXCut, int curYCut, int curLengthCut, int curWidthCut, float curDelay = 0f)
    {
        //Debug.LogFormat("({0},{1})({2}x{3})", curXCut, curYCut, curLengthCut, curWidthCut);
        if (curLengthCut > 1 && curWidthCut > 1)
        {
            bool cutHoriz = (curLengthCut > curWidthCut) || (curWidthCut <= curLengthCut && uernd.value < 0.5f);
            // Determine if the cut should be made horizontally.
            //cutHoriz = true;

            if (cutHoriz)
            {
                int selectedColumnCut = uernd.Range(curXCut, curXCut + curLengthCut - 1);
                for (int y = curYCut; y < curYCut + curWidthCut; y++)
                {
                    curX = selectedColumnCut;
                    curY = y;
                    maze[selectedColumnCut, y] = maze[selectedColumnCut, y].Replace("R", "");
                    maze[selectedColumnCut + 1, y] = maze[selectedColumnCut + 1, y].Replace("L", "");
                    if (curDelay > 0)
                        yield return new WaitForSeconds(curDelay);
                }
                int randomCorridorGen = uernd.Range(curYCut, curYCut + curWidthCut);
                curY = randomCorridorGen;
                CreatePassage(directionRight);
                if (curDelay > 0)
                    yield return new WaitForSeconds(curDelay);

                int leftHalfTLX = curXCut;
                int rightHalfTLX = selectedColumnCut + 1;
                //Debug.LogFormat("({0}|{1})", leftHalfTLX, rightHalfTLX);

                int leftHalfLength = selectedColumnCut - curXCut + 1;
                int rightHalfLength = curLengthCut - leftHalfLength;

                //Debug.LogFormat("({0} {1})", leftHalfLength, rightHalfLength);

                yield return SplitMaze(leftHalfTLX, curYCut, leftHalfLength, curWidthCut, curDelay);
                yield return SplitMaze(rightHalfTLX, curYCut, rightHalfLength, curWidthCut, curDelay);
            }
            else
            {
                int selectedRowCut = uernd.Range(curYCut, curYCut + curWidthCut - 1);
                for (int x = curXCut; x < curXCut + curLengthCut; x++)
                {
                    curY = selectedRowCut;
                    curX = x;
                    maze[x, selectedRowCut] = maze[x, selectedRowCut].Replace("D", "");
                    maze[x, selectedRowCut + 1] = maze[x, selectedRowCut + 1].Replace("U", "");
                    if (curDelay > 0)
                        yield return new WaitForSeconds(curDelay);
                }
                int randomCorridorGen = uernd.Range(curXCut, curXCut + curLengthCut);
                curX = randomCorridorGen;
                CreatePassage(directionDown);
                if (curDelay > 0)
                    yield return new WaitForSeconds(curDelay);

                int upperHalfTLY = curYCut;
                int bottomHalfTLY = selectedRowCut + 1;

                //Debug.LogFormat("({0}-{1})", upperHalfTLY,bottomHalfTLY);

                int topHalfWidth = selectedRowCut - curYCut + 1;
                int bottomHalfWidth = curWidthCut - topHalfWidth;

                //Debug.LogFormat("({0} {1})", topHalfWidth, bottomHalfWidth);

                yield return SplitMaze(curXCut, upperHalfTLY, curLengthCut, topHalfWidth, curDelay);
                yield return SplitMaze(curXCut, bottomHalfTLY, curLengthCut, bottomHalfWidth, curDelay);
            }
        }
        yield break;
    }

	public override IEnumerator AnimateGeneratedMaze(float delay)
    {
        isGenerating = true;
        for (int y = 0; y < curWidth; y++)
            for (int x = 0; x < curLength; x++)
            {
                curX = x;
                curY = y;
                if (x + 1 < curLength)
                    CreatePassage(directionRight);
                if (y + 1 < curWidth)
                    CreatePassage(directionDown);
                if (delay > 0)
                    yield return new WaitForSeconds(delay);
            }
        yield return SplitMaze(0, 0, curLength, curWidth, delay);


        isGenerating = false;
		yield return null;
    }

}

