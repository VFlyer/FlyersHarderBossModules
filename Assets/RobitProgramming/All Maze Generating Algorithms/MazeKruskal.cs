using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MazeKruskal : Maze {

    public MazeKruskal(int length, int width)
    {
        maze = new string[length, width];
        markSpecial = new bool[length, width];
        curLength = length;
        curWidth = width;
    }

	public override IEnumerator AnimateGeneratedMaze(float delay)
    {
        isGenerating = true;
        List<List<int[]>> groupedTiles = new List<List<int[]>>();
        List<int[][]> connectedTiles = new List<int[][]>();
        // Generate all possible edges that the maze can utilize
        for (int x = 0; x < curLength; x++)
        {
            for (int y = 0; y < curWidth; y++)
            {
                if (x + 1 < curLength)
                {
                    connectedTiles.Add(new[] { new[] { x, y }, new[] { x + 1, y } });
                }
                if (y + 1 < curWidth)
                {
                    connectedTiles.Add(new[] { new[] { x, y }, new[] { x, y + 1 } });
                }
            }
        }
        
        connectedTiles.Shuffle();

        foreach (int[][] eachEdge in connectedTiles)
        {

            int[] coordFrom = eachEdge[0];
            int[] coordTo = eachEdge[1];

            curX = coordFrom[0];
            curY = coordFrom[1];

            int idxFrom = -1, idxTo = -1;
            // Check which group the starting coordinate pair is at.
            for (int x = 0; x < groupedTiles.Count && idxFrom == -1; x++)
            {
                foreach (int[] coord in groupedTiles[x])
                {
                    if (coord.SequenceEqual(coordFrom))
                    {
                        idxFrom = x;
                        break;
                    }
                }
            }
            // Check which group the ending coordinate pair is at.
            for (int x = 0; x < groupedTiles.Count && idxTo == -1; x++)
            {
                foreach (int[] coord in groupedTiles[x])
                {
                    if (coord.SequenceEqual(coordTo))
                    {
                        idxTo = x;
                        break;
                    }
                }
            }

            List<int[]> referencedList;
            if (idxFrom == -1 && idxTo == -1) // If the edge pair has none connected to a group (which should always happen first)
            {
                referencedList = new List<int[]>();
                referencedList.Add(coordFrom);
                referencedList.Add(coordTo);
                groupedTiles.Add(referencedList);
            }
            else if ((idxFrom == -1 && idxTo != -1) || (idxTo == -1 && idxFrom != -1)) // If the edge pair has one connected to a group but the other is not...
            {
                if (idxFrom != -1)
                {
                    referencedList = groupedTiles[idxFrom];
                    referencedList.Add(coordTo);
                }
                else
                {
                    referencedList = groupedTiles[idxTo];
                    referencedList.Add(coordFrom);
                }
            }
            else if (idxFrom != idxTo) // If the edge pair have cells connected to different groups
            {
                referencedList = groupedTiles[idxFrom];
                List<int[]> secondaryList = groupedTiles[idxTo]; // Grab the cells connected to the secondary list.

                referencedList.AddRange(secondaryList); // Grab the cells connected to the secondary list.
                groupedTiles.Remove(secondaryList);
            }
            else
            {
                continue;
            }
            if (delay > 0)
                yield return new WaitForSeconds(delay);
            curX = coordTo[0];
            curY = coordTo[1];
            int[] coordDifference = { coordFrom[0] - coordTo[0], coordFrom[1] - coordTo[1] };
            if (coordDifference.SequenceEqual(new[] { 1, 0 }))
            {
                CreatePassageFrom(coordFrom[0], coordFrom[1], directionLeft);
            }
            else if (coordDifference.SequenceEqual(new[] { -1, 0 }))
            {
                CreatePassageFrom(coordFrom[0], coordFrom[1], directionRight);
            }
            else if (coordDifference.SequenceEqual(new[] { 0, 1 }))
            {
                CreatePassageFrom(coordFrom[0], coordFrom[1], directionUp);
            }
            else if (coordDifference.SequenceEqual(new[] { 0, -1 }))
            {
                CreatePassageFrom(coordFrom[0], coordFrom[1], directionDown);
            }
            if (delay > 0)
                yield return new WaitForSeconds(delay);
        }
        isGenerating = false;
		yield return null;
    }

}

