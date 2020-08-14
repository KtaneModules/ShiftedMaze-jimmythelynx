using System;
﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;

public class shiftedMazeScript : MonoBehaviour
{
	public KMAudio Audio;
	public KMBombInfo bomb;
    //Directional Buttons
	public KMSelectable moveUp;
	public KMSelectable moveDown;
	public KMSelectable moveLeft;
	public KMSelectable moveRight;

	private int xPos = 0; //marks your current position
	private int yPos = 0;

	//these are the offsets (1-5) to the right (x) or down (y)
	private int xOffset = 0;
	private int yOffset = 0;

	//maps every position in the maze to a unique number
	private int[,] maze = new int[6,6] { {0, 1, 2, 3, 4, 5},
																			 {6, 7, 8, 9, 10, 11},
																			 {12, 13, 14, 15, 16, 17},
																			 {18, 19, 20, 21, 22, 23},
																			 {24, 25, 26, 27, 28, 29},
 																		 	 {30, 31, 32, 33, 34, 35} };

	//maps each unique number to the corresponding dot on the grid
	public TextMesh[] mazeIndex;
	//links a number to each marker
	public TextMesh[] markers; // 0 = (1,1), 1 = (1,4), 2 = (4,1) and 3 = (4,4) "(x,y)"

	//hardcoding the values of the 4 marker onto the grid
	private int[,] markerIndex = new int[6,6] { {0, 0, 0, 0, 0, 0},
																						  {0, 0, 0, 0, 1, 0},
																						  {0, 0, 0, 0, 0, 0},
																						  {0, 0, 0, 0, 0, 0},
																						  {0, 2, 0, 0, 3, 0},
																						  {0, 0, 0, 0, 0, 0} };

	//hardcoding if its possible to move left from any maze position: 0 = no, 1 = yes
	private int[,] validMovLeft = new int[6,6] { {1, 1, 1, 1, 1, 0},
 																							 {0, 0, 1, 0, 1, 1},
																						 	 {0, 1, 0, 0, 1, 0},
																						   {1, 1, 0, 1, 0, 0},
																						 	 {0, 0, 1, 0, 0, 1},
																						   {0, 1, 0, 1, 1, 1} };

    //hardcoding if its possible to move up from any maze position: 0 = no, 1 = yes
	private int[,] validMovUp = new int[6,6] { {0, 1, 0, 0, 1, 1},
																						 {1, 0, 1, 1, 0, 1},
																					 	 {1, 1, 1, 1, 0, 1},
																					 	 {0, 1, 1, 1, 1, 1},
																					 	 {1, 1, 0, 1, 1, 1},
																					 	 {1, 1, 1, 1, 1, 0} };

    private int[] possibleStart = new int[2] {1, 4}; //holds the two possible position for inital xPos and yPos (1,1 / 1,4 / 4,1 / 4,4)

	private int[,] goals = new int[4, 2] { {0, 0}, {0, 0}, {0, 0}, {0, 0} };// goals for stage 1, 2 and 3 and starting Position: (x, y)

	private int stage = 0; // keeps track of how many stages (3 total) are solved
	private int batteryCycle; // is the number D batteries mod 3

	public Color[] fontColors; //these are the colors to change font colors 0 = black, 1 = white, 2 = blue, 3 = yellow, 4 = magenta, 5 = invisible, 6 = neongreen, 7 = green
 	public Material[] screenMaterial; //materials for the background screen 0 = blue, 1 = red
	public MeshRenderer screen; // holds the connection to the screen object

    public KMColorblindMode colorblindMode; //the colorblind object attached to the module
    public TextMesh[] colorblindTexts; //the texts used to display the color if colorblind mode is enabled
    private bool colorblindActive = false; //a boolean used for knowing if colorblind mode is active

	//Logging
	static int moduleIdCounter = 1;
	int moduleId;
	private bool moduleSolved;
	private bool inStrike;
	private string[] markerLog = new string[3] {"diagonally opposite corner", "same row", "same column"};


	void Awake ()
	{
		moduleId = moduleIdCounter++;
        //check for colorblind mode
        colorblindActive = colorblindMode.ColorblindModeActive;
        //disable all text on the module until the lights turn on
        foreach (TextMesh text in mazeIndex)
        {
            text.gameObject.SetActive(false);
        }
        foreach (TextMesh text in markers)
        {
            text.gameObject.SetActive(false);
        }
        //delegate button press to method
        moveLeft.OnInteract += delegate () { PressLeft(); return false; };
		moveRight.OnInteract += delegate () { PressRight(); return false; };
		moveUp.OnInteract += delegate () { PressUp(); return false; };
		moveDown.OnInteract += delegate () { PressDown(); return false; };
        GetComponent<KMBombModule>().OnActivate += OnActivate;
	}

	// Use this for initialization
	void Start ()
	{
		CalculateStartingPoint();
		CalculateGoals();
		CalculateOffset();
	}

	// Update is called once per frame
/*/	void Update ()
	{

	}/*/

    void OnActivate()
    {
        //the lights have turned on, activate all text
        foreach (TextMesh text in mazeIndex)
        {
            text.gameObject.SetActive(true);
        }
        foreach (TextMesh text in markers)
        {
            text.gameObject.SetActive(true);
        }
        if (colorblindActive)
        {
            foreach (TextMesh text in colorblindTexts)
            {
                text.gameObject.SetActive(true);
            }
        }
    }

	void CalculateStartingPoint()
	{
		xPos = possibleStart[UnityEngine.Random.Range(0, 2)];
		yPos = possibleStart[UnityEngine.Random.Range(0, 2)];
		mazeIndex[maze[yPos, xPos]].color = fontColors[1]; // marks your current position in the grid white
		markers[markerIndex[yPos, xPos]].color = fontColors[1]; // makes the marker on the starting position white
        colorblindTexts[markerIndex[yPos, xPos]].gameObject.transform.localScale = new Vector3(0.006f, 0.006f, 0.007f); // makes the colorblind text for white fit
        colorblindTexts[markerIndex[yPos, xPos]].text = "W";
        goals[3, 0] = xPos; // remembering starting position
		goals[3, 1] = yPos;
		Debug.LogFormat("[Shifted Maze #{0}] Your starting position is: x:{1}, y:{2}. (With 1,1 being the top left corner.)", moduleId, xPos+1, yPos+1);
	}

	void CalculateGoals()
	{
		batteryCycle = bomb.GetBatteryCount(Battery.D) % 3;

		goals[Mod((0 - batteryCycle), 3), 0] = (xPos + 3) % 6; //this sets the diagonally opposite goal depening on number of D cells
		goals[Mod((0 - batteryCycle), 3), 1] = (yPos + 3) % 6;

		goals[Mod((1 - batteryCycle), 3), 0] = (xPos + 3) % 6; //this sets the horizotally opposite goal depening on number of D cells
		goals[Mod((1 - batteryCycle), 3), 1] = yPos;

		goals[2 - batteryCycle, 0] = xPos; // //this sets the vertically opposite goal depening on number of D cells
		goals[2 - batteryCycle, 1] = (yPos + 3) % 6;

		Debug.LogFormat("[Shifted Maze #{0}] Number of D-Batteries: {1}, thus cycle the goals forward by {2}", moduleId, bomb.GetBatteryCount(Battery.D), batteryCycle);
		Debug.LogFormat("[Shifted Maze #{0}] First go to the marker in the {1}, then to the marker in the {2} and finally to the marker in the {3}, relative to the starting position.", moduleId, markerLog[batteryCycle], markerLog[(batteryCycle + 1) %3], markerLog[(batteryCycle + 2) %3]);
		Debug.LogFormat("[Shifted Maze #{0}] Route is: x:{1}, y:{2} -> x:{3}, y:{4} -> x:{5}, y:{6}.", moduleId, goals[0, 0]+1, goals[0, 1]+1, goals[1, 0]+1, goals[1, 1]+1, goals[2, 0]+1, goals[2, 1]+1);
	}

	int Mod(int x, int m) // modulo function that always gives me a positive value back
	{
		return (x % m + m) % m;
	}

	void CalculateOffset()
	{
		xOffset = UnityEngine.Random.Range(0, 5);  // gives each direction a random offset between 0 and 5
		yOffset = UnityEngine.Random.Range(0, 5);
		Debug.LogFormat("[Shifted Maze #{0}] The maze is shifted {1} steps to the left and {2} steps up.", moduleId, xOffset, yOffset);
		//coloring the diagonal opposite marker
		if (xOffset > 2)
		{
			if (yOffset > 2) // if both offsets are over 2
			{
				markers[markerIndex[(yPos + 3) % 6, (xPos + 3) % 6]].color = fontColors[2]; // marker diagonaly opposite start = blue
                colorblindTexts[markerIndex[(yPos + 3) % 6, (xPos + 3) % 6]].text = "B";
				Debug.LogFormat("[Shifted Maze #{0}] Both the x and y offsets are over 2, so the diagonally opposite marker is BLUE", moduleId);
			}
			else // if x offset is over 2 but y is lower
			{
				markers[markerIndex[(yPos + 3) % 6, (xPos + 3) % 6]].color = fontColors[4]; // marker diagonaly opposite start = magenta
                colorblindTexts[markerIndex[(yPos + 3) % 6, (xPos + 3) % 6]].text = "M";
                Debug.LogFormat("[Shifted Maze #{0}] The x offset is above 2 but the y offset is below 2, so the diagonally opposite marker is MAGENTA", moduleId);
			}
		}
		else
		{
			if (yOffset > 2) // if x offset is unter 2 but y offset is over
			{
				markers[markerIndex[(yPos + 3) % 6, (xPos + 3) % 6]].color = fontColors[7]; // marker diagonaly opposite start = green
                colorblindTexts[markerIndex[(yPos + 3) % 6, (xPos + 3) % 6]].text = "G";
                Debug.LogFormat("[Shifted Maze #{0}] The x offset is below 2 but the y offset is above 2, so the diagonally opposite marker is GREEN", moduleId);
			}
			else // if both offsets are under 2
			{
				markers[markerIndex[(yPos + 3) % 6, (xPos + 3) % 6]].color = fontColors[3]; // marker diagonaly opposite start = yellow
                colorblindTexts[markerIndex[(yPos + 3) % 6, (xPos + 3) % 6]].text = "Y";
                Debug.LogFormat("[Shifted Maze #{0}] Both the x and y offsets are below 2, so the diagonally opposite marker is YELLOW", moduleId);
			}
		}
		// coloring the marker in the same row as start
		if (xOffset == 0 || xOffset == 4)
		{
			markers[markerIndex[yPos, (xPos + 3) % 6]].color = fontColors[3]; // marker horizontaly opposite start = yellow
            colorblindTexts[markerIndex[yPos, (xPos + 3) % 6]].text = "Y";
            Debug.LogFormat("[Shifted Maze #{0}] The horizontal offset is 0 or +4, so the marker in the same row is YELLOW", moduleId);
		}
		else if (xOffset == 1 || xOffset == 3)
		{
			markers[markerIndex[yPos, (xPos + 3) % 6]].color = fontColors[4]; // marker horizontaly opposite start = magenta
            colorblindTexts[markerIndex[yPos, (xPos + 3) % 6]].text = "M";
            Debug.LogFormat("[Shifted Maze #{0}] The horizontal offset is +1 or +3, so the marker in the same row is MAGENTA", moduleId);
		}
		else
		{
			markers[markerIndex[yPos, (xPos + 3) % 6]].color = fontColors[2]; // marker horizontaly opposite start = blue
            colorblindTexts[markerIndex[yPos, (xPos + 3) % 6]].text = "B";
            Debug.LogFormat("[Shifted Maze #{0}] The horizontal offset is +2 or +5, so the marker in the same row is BLUE", moduleId);
		}
		//coloring the marker in the same column as start
		if (yOffset == 0 || yOffset == 5)
		{
			markers[markerIndex[(yPos + 3) % 6, xPos]].color = fontColors[4]; // marker verticaly opposite start = magenta
            colorblindTexts[markerIndex[(yPos + 3) % 6, xPos]].text = "M";
            Debug.LogFormat("[Shifted Maze #{0}] The vertical offset is 0 or +5, so the marker in the same column is MAGENTA", moduleId);
		}
		else if (yOffset == 1 || yOffset == 4)
		{
			markers[markerIndex[(yPos + 3) % 6, xPos]].color = fontColors[2]; // marker verticaly opposite start = blue
            colorblindTexts[markerIndex[(yPos + 3) % 6, xPos]].text = "B";
            Debug.LogFormat("[Shifted Maze #{0}] The vertical offset is +1 or +4, so the marker in the same column is BLUE", moduleId);
		}
		else
		{
			markers[markerIndex[(yPos + 3) % 6, xPos]].color = fontColors[3]; // marker verticaly opposite start = yellow
            colorblindTexts[markerIndex[(yPos + 3) % 6, xPos]].text = "Y";
            Debug.LogFormat("[Shifted Maze #{0}] The vertical offset is +2 or +3, so the marker in the same column is YELLOW", moduleId);
		}
	}

	void CheckOnGoal()
	{
		if (stage == 0) // if you're in stage 1
		{
			if ((xPos == goals[0, 0]) && (yPos == goals[0, 1])) // and your position equals the first goal
			{
				stage ++; // increase the stage
				markers[markerIndex[yPos, xPos]].color = fontColors[6]; // makes marker on curren position green
                colorblindTexts[markerIndex[yPos, xPos]].text = "";
                Audio.PlaySoundAtTransform("beep", transform); //play beep sound
				Debug.LogFormat("[Shifted Maze #{0}] You reached the first goal.", moduleId);
			}
			else if ((xPos == goals[3, 0]) && (yPos == goals[3, 1])) // and you revisit the start
			{
				return; // do nothing
			}
			else // but if you're on the second or final goal in the first stage, give a strike
			{
				GetComponent<KMBombModule>().HandleStrike();
				StartCoroutine(Strike());
				Debug.LogFormat("[Shifted Maze #{0}] You moved on a goal which is not your first goal, strike!", moduleId);
			}
		}
		else if (stage == 1) // if you're in the secon Stage
		{
			if ((xPos == goals[1, 0]) && (yPos == goals[1, 1])) // and your position equals the second goals
			{
				stage ++; //increase the stage
				markers[markerIndex[yPos, xPos]].color = fontColors[6]; // makes marker on curren position green
                colorblindTexts[markerIndex[yPos, xPos]].text = "";
                Audio.PlaySoundAtTransform("beep", transform); //play beep sound
				Debug.LogFormat("[Shifted Maze #{0}] You reached the second goal.", moduleId);
			}
			else if ((xPos == goals[2, 0]) && (yPos == goals[2, 1])) // but you're on the position of the final goal, give a strike
		  {
			GetComponent<KMBombModule>().HandleStrike();
			StartCoroutine(Strike());
			Debug.LogFormat("[Shifted Maze #{0}] You moved on a goal which is not your second goal, strike!", moduleId);
		  }
			else // else, if you revisit the start or the position of goal 1, do nothing
			{
				return;
			}
		}
		else // if you're in the final stage
		{
			if ((xPos == goals[2, 0]) && (yPos == goals[2, 1])) // and you're in the position of the final goal, solve the module
			{
				moduleSolved = true;
				GetComponent<KMBombModule>().HandlePass();
				markers[markerIndex[yPos, xPos]].color = fontColors[6]; // makes marker on current position and start green
				markers[markerIndex[goals[3, 1], goals[3, 0]]].color = fontColors[6];
                colorblindTexts[markerIndex[yPos, xPos]].text = "";
                colorblindTexts[markerIndex[goals[3, 1], goals[3, 0]]].text = "";
                Audio.PlaySoundAtTransform("beep", transform);

				Debug.LogFormat("[Shifted Maze #{0}] You reached the last goal. The module is solved!", moduleId);
			}
			else // but if you revisit every other position (start, 1st goal, 2nd goal), do nothing
			{
				return;
			}
		}
	}



	void PressLeft()
	{
		if(moduleSolved || inStrike){return;}
		moveLeft.AddInteractionPunch();
		GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, moveLeft.transform);

		mazeIndex[maze[yPos, xPos]].color = fontColors[0]; // color the node you leave black
		if ((xPos > 0) && (validMovLeft[(yPos + yOffset) %6, (xPos + xOffset) %6] == 1)) // if the move is allowed (no wall, no edge), increase the x value
		{
			xPos --;
			Debug.LogFormat("[Shifted Maze #{0}] You moved left to x:{1}, y:{2}.", moduleId, xPos+1, yPos+1);
			if ((xPos == 1 || xPos == 4) && (yPos == 1 || yPos == 4)) // if you moved to any of the four corner positions
			{
				CheckOnGoal(); // check if you solved that stage or the module or if you moved on a goal too early
			}
		}
		else // if the move is not allowed handle a strike
		{
			GetComponent<KMBombModule>().HandleStrike();
			StartCoroutine(Strike());
			Debug.LogFormat("[Shifted Maze #{0}] Your tried to move left from x:{1}, y:{2} and ran into a wall, strike!", moduleId, xPos+1, yPos+1);
		}

		if (moduleSolved) {mazeIndex[maze[yPos, xPos]].color = fontColors[0];} // marks your current position in the grid black
		else {mazeIndex[maze[yPos, xPos]].color = fontColors[1];} // color the node you moved to white.
	}

	void PressRight()
	{
		if(moduleSolved || inStrike){return;}
		moveRight.AddInteractionPunch();
		GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, moveRight.transform);

		mazeIndex[maze[yPos, xPos]].color = fontColors[0];
		if ((xPos < 5) && (validMovLeft[(yPos + yOffset) %6, (xPos + xOffset + 1) %6] == 1))
		{
			xPos ++;
			Debug.LogFormat("[Shifted Maze #{0}] You moved right to x:{1}, y:{2}.", moduleId, xPos+1, yPos+1);
			if ((xPos == 1 || xPos == 4) && (yPos == 1 || yPos == 4)) // if you moved to any of the four corner positions
			{
				CheckOnGoal(); // check if you solved that stage or the module or if you moved on a goal too early
			}
		}
		else
		{
			GetComponent<KMBombModule>().HandleStrike();
			StartCoroutine(Strike());
			Debug.LogFormat("[Shifted Maze #{0}] Your tried to move right from x:{1}, y:{2} and ran into a wall, strike!", moduleId, xPos+1, yPos+1);
		}

		if (moduleSolved) {mazeIndex[maze[yPos, xPos]].color = fontColors[0];} // marks your current position in the grid black
		else {mazeIndex[maze[yPos, xPos]].color = fontColors[1];} // color the node you moved to white.
	}

	void PressUp()
	{
		if(moduleSolved || inStrike){return;}
		moveUp.AddInteractionPunch();
		GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, moveUp.transform);

		mazeIndex[maze[yPos, xPos]].color = fontColors[0];
		if ((yPos > 0) && (validMovUp[(yPos + yOffset) %6, (xPos + xOffset) %6] == 1))
		{
			yPos --;
			Debug.LogFormat("[Shifted Maze #{0}] You moved up to x:{1}, y:{2}.", moduleId, xPos+1, yPos+1);
			if ((xPos == 1 || xPos == 4) && (yPos == 1 || yPos == 4)) // if you moved to any of the four corner positions
			{
				CheckOnGoal(); // check if you solved that stage or the module or if you moved on a goal too early
			}
		}
		else
		{
			GetComponent<KMBombModule>().HandleStrike();
			StartCoroutine(Strike());
			Debug.LogFormat("[Shifted Maze #{0}] Your tried to move up from x:{1}, y:{2} and ran into a wall, strike!", moduleId, xPos+1, yPos+1);
		}

		if (moduleSolved) {mazeIndex[maze[yPos, xPos]].color = fontColors[0];} // marks your current position in the grid black
		else {mazeIndex[maze[yPos, xPos]].color = fontColors[1];} // color the node you moved to white.
	}

	void PressDown()
	{
		if(moduleSolved || inStrike){return;}
		moveDown.AddInteractionPunch();
		GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, moveDown.transform);

		mazeIndex[maze[yPos, xPos]].color = fontColors[0];
		if ((yPos < 5) && (validMovUp[(yPos + yOffset + 1) %6, (xPos + xOffset) %6] == 1))
		{
			yPos ++;
			Debug.LogFormat("[Shifted Maze #{0}] You moved down to x:{1}, y:{2}.", moduleId, xPos+1, yPos+1);
			if ((xPos == 1 || xPos == 4) && (yPos == 1 || yPos == 4)) // if you moved to any of the four corner positions
			{
				CheckOnGoal(); // check if you solved that stage or the module or if you moved on a goal too early
			}
		}
		else
		{
			GetComponent<KMBombModule>().HandleStrike();
			StartCoroutine(Strike());
			Debug.LogFormat("[Shifted Maze #{0}] Your tried to move down from x:{1}, y:{2} and ran into a wall, strike!", moduleId, xPos+1, yPos+1);
		}

		if (moduleSolved) {mazeIndex[maze[yPos, xPos]].color = fontColors[0];} // marks your current position in the grid black
		else {mazeIndex[maze[yPos, xPos]].color = fontColors[1];} // color the node you moved to white.
	}


    IEnumerator Strike()
	{
		inStrike = true;
		screen.material = screenMaterial[1]; // set screen to red
		yield return new WaitForSeconds(.8f);
		screen.material = screenMaterial[0]; // set screen to blue
		inStrike = false;
	}

    //twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} move udlr [Move in the specified directions in order; u = up, r = right, d = down, l = left] | !{0} colorblind [Toggles colorblind mode]";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        if (Regex.IsMatch(command, @"^\s*colorblind\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            if (colorblindActive)
            {
                colorblindActive = false;
                foreach (TextMesh text in colorblindTexts)
                {
                    text.gameObject.SetActive(false);
                }
            }
            else
            {
                colorblindActive = true;
                foreach (TextMesh text in colorblindTexts)
                {
                    text.gameObject.SetActive(true);
                }
            }
            yield break;
        }
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*move\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            if (parameters.Length > 2)
            {
                yield return "sendtochaterror Too many parameters!";
            }
            else if (parameters.Length == 1)
            {
                yield return "sendtochaterror Please specify which directions to move!";
            }
            else
            {
                char[] parameters2 = parameters[1].ToCharArray();
                var buttonsToPress = new List<KMSelectable>();
                foreach (char c in parameters2)
                {
                    if (c.Equals('u') || c.Equals('U'))
                    {
                        buttonsToPress.Add(moveUp);
                    }
                    else if (c.Equals('r') || c.Equals('R'))
                    {
                        buttonsToPress.Add(moveRight);
                    }
                    else if (c.Equals('d') || c.Equals('D'))
                    {
                        buttonsToPress.Add(moveDown);
                    }
                    else if (c.Equals('l') || c.Equals('L'))
                    {
                        buttonsToPress.Add(moveLeft);
                    }
                    else
                    {
                        yield return "sendtochaterror The specified direction to move '" + c + "' is invalid!";
                        yield break;
                    }
                }
                foreach (KMSelectable km in buttonsToPress)
                {
                    km.OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
            }
            yield break;
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (inStrike) { yield return true; yield return new WaitForSeconds(0.1f); }
        int start = stage;
        for (int j = start; j < 3; j++)
        {
            var q = new Queue<int[]>();
            var allMoves = new List<Movement>();
            var startPoint = new int[] { xPos, yPos };
            var targets = new int[3, 2] { { goals[0, 0], goals[0, 1] }, { goals[1, 0], goals[1, 1] }, { goals[2, 0], goals[2, 1] } };
            q.Enqueue(startPoint);
            while (q.Count > 0)
            {
                var next = q.Dequeue();
                if (next[0] == targets[j, 0] && next[1] == targets[j, 1])
                    goto readyToSubmit;
                string paths = "";
                if ((next[1] > 0) && (validMovUp[(next[1] + yOffset) % 6, (next[0] + xOffset) % 6] == 1) && checkPosWithGoal(next, targets, j)) { paths += "U"; }
                if ((next[0] < 5) && (validMovLeft[(next[1] + yOffset) % 6, (next[0] + xOffset + 1) % 6] == 1) && checkPosWithGoal(next, targets, j)) { paths += "R"; }
                if ((next[1] < 5) && (validMovUp[(next[1] + yOffset + 1) % 6, (next[0] + xOffset) % 6] == 1) && checkPosWithGoal(next, targets, j)) { paths += "D"; }
                if ((next[0] > 0) && (validMovLeft[(next[1] + yOffset) % 6, (next[0] + xOffset) % 6] == 1) && checkPosWithGoal(next, targets, j)) { paths += "L"; }
                var cell = paths;
                var allDirections = "URDL";
                var offsets = new int[,] { { 0, -1 }, { 1, 0 }, { 0, 1 }, { -1, 0 } };
                for (int i = 0; i < 4; i++)
                {
                    var check = new int[] { next[0] + offsets[i, 0], next[1] + offsets[i, 1] };
                    if (cell.Contains(allDirections[i]) && !allMoves.Any(x => x.start[0] == check[0] && x.start[1] == check[1]))
                    {
                        q.Enqueue(new int[] { next[0] + offsets[i, 0], next[1] + offsets[i, 1] });
                        allMoves.Add(new Movement { start = next, end = new int[] { next[0] + offsets[i, 0], next[1] + offsets[i, 1] }, direction = i });
                    }
                }
            }
            throw new InvalidOperationException("There is a bug in maze generation.");
            readyToSubmit:
            KMSelectable[] buttons = new KMSelectable[] { moveUp, moveRight, moveDown, moveLeft };
            if (allMoves.Count != 0) // Checks for position already being target
            {
                var target = new int[] { targets[j, 0], targets[j, 1] };
                var lastMove = allMoves.First(x => x.end[0] == target[0] && x.end[1] == target[1]);
                var relevantMoves = new List<Movement> { lastMove };
                while (lastMove.start != startPoint)
                {
                    lastMove = allMoves.First(x => x.end[0] == lastMove.start[0] && x.end[1] == lastMove.start[1]);
                    relevantMoves.Add(lastMove);
                }
                for (int i = 0; i < relevantMoves.Count; i++)
                {
                    buttons[relevantMoves[relevantMoves.Count - 1 - i].direction].OnInteract();
                    yield return new WaitForSeconds(.1f);
                }
            }
        }
    }

    bool checkPosWithGoal(int[] pos, int[,] goals, int stage)
    {
        if (stage == 0)
        {
            if ((goals[1, 0] == pos[0] && goals[1, 1] == pos[1]) || (goals[2, 0] == pos[0] && goals[2, 1] == pos[1]))
            {
                return false;
            }
        }
        else if (stage == 1)
        {
            if (goals[2, 0] == pos[0] && goals[2, 1] == pos[1])
            {
                return false;
            }
        }
        return true;
    }

    class Movement
    {
        public int[] start;
        public int[] end;
        public int direction;
    }
}
