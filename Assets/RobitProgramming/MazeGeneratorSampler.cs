using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeGeneratorSampler : MonoBehaviour {

	public GridDisplayer gridRenderer;
	public Maze selectedMaze;
	public Color[] highlightColors;
	public bool generateSpecifiedMaze;
	// Use this for initialization
	void Start () {
		selectedMaze = new MazeEllers(5, 5, true, false, true);
		if (generateSpecifiedMaze)
			StartCoroutine(GenerateMazeXTimes());
		else
			StartCoroutine(SampleAllGeneratedMazes());
	}
	IEnumerator SampleAllGeneratedMazes()
    {
		// Backtracking
		selectedMaze = new MazeBacktracker(5, 5);
		yield return GenerateMazeXTimes();
		// Hunt And Kill
		selectedMaze = new MazeHuntAndKill(5, 5);
		yield return GenerateMazeXTimes();
		selectedMaze = new MazeHuntAndKill(5, 5, true, true, true);
		yield return GenerateMazeXTimes();
		// Binary Tree
		selectedMaze = new MazeBinaryTree(5, 5);
		yield return GenerateMazeXTimes();
		// Prim's
		selectedMaze = new MazePrims(5, 5);
		yield return GenerateMazeXTimes();
		// Kruskal's
		selectedMaze = new MazeKruskal(5, 5);
		yield return GenerateMazeXTimes();
		// Aldous-Broder
		selectedMaze = new MazeAldousBroder(5, 5);
		yield return GenerateMazeXTimes();
		// Sidewinder
		selectedMaze = new MazeSidewinder(5, 5);
		yield return GenerateMazeXTimes();
		// Growing Tree
		selectedMaze = new MazeGrowingTree(5, 5, 1, 0, 1);
		yield return GenerateMazeXTimes();
		// Wilson's
		selectedMaze = new MazeWilsons(5, 5);
		yield return GenerateMazeXTimes();
		// Recursive Division
		selectedMaze = new MazeRecursiveDivision(5, 5);
		yield return GenerateMazeXTimes();
		// Eller's
		selectedMaze = new MazeEllers(5, 5, true);
		yield return GenerateMazeXTimes();
	}
	IEnumerator GenerateMazeXTimes(int repeatCount = 1)
    {
		for (int x = 0; x < repeatCount; x++)
		{
			selectedMaze.MoveToNewPosition(selectedMaze.GetLength() / 2, selectedMaze.GetWidth() / 2);
			selectedMaze.FillMaze();
			yield return selectedMaze.AnimateGeneratedMaze(0.1f);
		}
    }

	void UpdateDisplay()
    {
        for (int x = 0; x < gridRenderer.rowRenderers.Length; x++)
        {
			for (int y = 0; y < Mathf.Min(gridRenderer.rowRenderers[x].wallRenderers.Length, gridRenderer.rowRenderers[x].canRender.Length); y++)
				gridRenderer.rowRenderers[x].wallRenderers[y].enabled = gridRenderer.rowRenderers[x].canRender[y];

		}
		for (int x = 0; x < selectedMaze.maze.GetLength(0); x++)
			for (int y = 0; y < selectedMaze.maze.GetLength(1); y++)
			{
				if (selectedMaze.GetState())
                    gridRenderer.rowRenderers[y * 2].wallRenderers[x * 2].material.color = selectedMaze.GetCurX() == x && selectedMaze.GetCurY() == y ? highlightColors[1] : selectedMaze.markSpecial[x, y] ? highlightColors[2] : highlightColors[0];
				else
					gridRenderer.rowRenderers[y * 2].wallRenderers[x * 2].material.color = highlightColors[0];
			}
	}

	// Update is called once per frame
	void Update () {
        for (int x = 0; x < selectedMaze.maze.GetLength(0); x++)
        {
            for (int y = 0; y < selectedMaze.maze.GetLength(1); y++)
            {
				if (x + 1 < selectedMaze.maze.GetLength(0))
					gridRenderer.rowRenderers[y * 2].canRender[x * 2 + 1] = !selectedMaze.maze[x, y].Contains("R");
                
				if (x - 1 >= 0)
					gridRenderer.rowRenderers[y * 2].canRender[x * 2 - 1] = !selectedMaze.maze[x, y].Contains("L");
				
				if (y - 1 >= 0)
					gridRenderer.rowRenderers[y * 2 - 1].canRender[x * 2] = !selectedMaze.maze[x, y].Contains("U");
				
				if (y + 1 < selectedMaze.maze.GetLength(1))
					gridRenderer.rowRenderers[y * 2 + 1].canRender[x * 2] = !selectedMaze.maze[x, y].Contains("D");

				gridRenderer.rowRenderers[y * 2].canRender[x * 2] = selectedMaze.GetState() && ((selectedMaze.GetCurX() == x && selectedMaze.GetCurY() == y) || selectedMaze.markSpecial[x, y]);
			}
        }
		for (int x = 1; x < gridRenderer.rowRenderers.Length; x += 2)
        {
			for (int y = 1; y < gridRenderer.rowRenderers[x].canRender.Length; y += 2)
			{
				gridRenderer.rowRenderers[x].canRender[y] = true;
			}
		}
		for (int x = 1; x < gridRenderer.rowRenderers.Length; x += 2)
		{
			for (int y = 1; y < gridRenderer.rowRenderers[x].canRender.Length; y += 2)
			{
				gridRenderer.rowRenderers[x].canRender[y] =
					gridRenderer.rowRenderers[x + 1].canRender[y] ||
					gridRenderer.rowRenderers[x - 1].canRender[y] ||
					gridRenderer.rowRenderers[x].canRender[y + 1] ||
					gridRenderer.rowRenderers[x].canRender[y - 1];
			}
		}
		UpdateDisplay();
	}
}
