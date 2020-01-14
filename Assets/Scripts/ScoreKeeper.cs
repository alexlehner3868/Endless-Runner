using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEngine.SceneManagement;

[RequireComponent (typeof(CollisionManager))]
public class ScoreKeeper : MonoBehaviour
{
    class Leaderboard
    {
        public const int NUMSLOTS = 8;
        
        public LeaderboardEntry[] entries;
        int place = -1; // place of new high score
        ulong score;

        // Initials entry
        int currInitial = 0;
        float blinkTimer = 0;
        float blinkTime = 0.3f; // seconds between blinks
        char[] initialChars;
        int currInitialChar = 0;
        float vPrev = 0f; // vertical input axis on previous frame
        
        // UI
        public Transform Root;
        public LeaderboardUIEntry[] ui_entries;
        public Text ui_ContextMessage;

        // default leaderboard data
        string[] defaultLeaderboard =
        {
             "1:Ham:150000",
             "2:Bur:120000",
             "3:TJ:100000",
             "4:Was:80000",
             "5:Ada:70000",
             "6:JQA:50000",
             "7:Mad:40000",
             "8:Peg:1",
        };

        public Leaderboard()
        {
            entries = new LeaderboardEntry[NUMSLOTS];

            // Initialize initials characters
            initialChars = new char[26];
            int index = 0;
            for (char c = 'A'; c <= 'Z'; c++)
            {
                initialChars[index] = c;
                ++index;
            }

            try
            {
                Load();
            }
            catch(FileNotFoundException e)
            {
                Debug.LogError("Leaderboard data not found. Reverting to default.");
                Debug.LogException(e);
                Wipe();
            }
            catch(System.IndexOutOfRangeException e)
            {
                Debug.LogError("Leaderboard data is corrupted. Reverting to default.");
                Debug.LogException(e);
                Wipe();
            }
            catch (System.FormatException e)
            {
                Debug.LogError("Leaderboard data is corrupted. Reverting to default.");
                Debug.LogException(e);
                Wipe();
            }
            catch (System.ArgumentException e)
            {
                Debug.LogError("Leaderboard data is corrupted. Reverting to default.");
                Debug.LogException(e);
                Wipe();
            }
        }

        void Load()
        {
            // Load text save file
            string[] loadedItems = File.ReadAllLines(Application.dataPath + "/_data/leaderboard_data.sav");

            for (int i = 0; i < loadedItems.Length; i++)
            {
                // components of an entry are separted by ':'
                string[] loadedItemProperties = loadedItems[i].Split(':');

                //================================================
                // LOADED ITEM PROPERTIES:
                // Place:Initials:Score
                // [0] = Place, [1] = Initials, [2] = Score
                //================================================
                int place = int.Parse(loadedItemProperties[0]);
                string initials = loadedItemProperties[1];
                ulong score = ulong.Parse(loadedItemProperties[2]);

                entries[place - 1] = new LeaderboardEntry(initials, score);
            }
        }

        // Overwrites leaderboard data with current leaderboard standings
        void Save()
        {
            string[] data = new string[entries.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                data[i] = "" + (i + 1) + ':' + entries[i].initials + ':' + entries[i].score;
            }
            
            File.WriteAllLines(Application.dataPath + "/_data/leaderboard_data.sav", data);
        }

        // Wipe leaderboard to default state
        void Wipe()
        {
            File.WriteAllLines(Application.dataPath + "/_data/leaderboard_data.sav", defaultLeaderboard);

            try
            {
                Load();
            }
            catch (System.Exception e)
            {
                Debug.LogError("Could not revert to default leaderboard data. Something is seriously wrong.");
                Debug.LogException(e);
            }
        }

        public void InitializeUI(Transform canvas)
        {
            // Initialize entries
            ui_entries = new LeaderboardUIEntry[NUMSLOTS];
            for (int i = 0; i < ui_entries.Length; i++)
            {
                ui_entries[i].initials = new Text[3];
            }

            // Get leaderboard parent
            Root = canvas.Find("ui_leaderboard");

            for (int i = 0; i < ui_entries.Length; i++)
            {
                // empty entry parent
                Transform EntryRoot = Root.Find("entry_" + (i + 1));

                // Initials text components
                for (int j = 0; j < 3; j++)
                {
                    ui_entries[i].initials[j] = EntryRoot.Find("initial_" + (j + 1)).GetComponent<Text>();
                }

                // score text component
                ui_entries[i].Score = EntryRoot.Find("score").GetComponent<Text>();
            }

            ui_ContextMessage = Root.Find("context_message").GetComponent<Text>();
        }
        // ScoreKeeper.Leaderboard
        public void Update()
        {
            if (place < 0)
            {
                // check for "any key" to reset game
                if (Input.anyKeyDown)
                {
                    GameManager.Instance.ResetLevel();
                }
            }
            else
            {
                // Initials input loop
                blinkTimer += Time.deltaTime;
                if (blinkTimer >= blinkTime)
                {
                    blinkTimer -= blinkTime;

                    // Toggle initial visibility
                    ui_entries[place].initials[currInitial].enabled = !ui_entries[place].initials[currInitial].enabled;
                }

                // Vertical input
                float vNew = Input.GetAxisRaw("Vertical");
                float vDelta = vNew - vPrev;
                if (Mathf.Abs(vDelta) > 0.3f && Mathf.Abs(vNew) >= 0.5f)
                {
                    // go to next/prev character
                    int dir = Mathf.RoundToInt(vNew);
                    currInitialChar = Helpers.Mod(currInitialChar + dir, initialChars.Length);
                    ui_entries[place].initials[currInitial].text = initialChars[currInitialChar].ToString();
                }
                else if (Input.anyKeyDown)
                {
                    ui_entries[place].initials[currInitial].enabled = true;

                    string initials = "" + (currInitial == 0 ? initialChars[currInitialChar] : entries[place].initials[0]) +
                                           (currInitial == 1 ? initialChars[currInitialChar] : entries[place].initials[1]) +
                                           (currInitial == 2 ? initialChars[currInitialChar] : entries[place].initials[2]);

                    entries[place].initials = initials;

                    // move to next initial
                    if (++currInitial > 2)
                    {
                        // finished entering initials
                        currInitial = 0;
                        place = -1;
                        Save();
                        ui_ContextMessage.text = "PRESS ANY KEY";                   
                    }
                }
               
                vPrev = vNew;
            }
        }

        // Display leaderboard UI
        public void Display()
        {
            // Activate UI
            Root.gameObject.SetActive(true);

            // Set entries' text
            for (int i = 0; i < ui_entries.Length; i++)
            {
                string initials = entries[i].initials;
                for (int j = 0; j < 3; j++)
                {
                    ui_entries[i].initials[j].text = initials[j].ToString();
                }

                ui_entries[i].Score.text = entries[i].score.ToString();
            }
        }

        public void EnterHighscore(int place, ulong score)
        {
            this.place = place;
            this.score = score;
            entries[place] = new LeaderboardEntry("AAA", score);
        }

        public void Close()
        {
            Save();
            Root.gameObject.SetActive(false);
        }

        public struct LeaderboardEntry
        {
            public string initials;
            public ulong score;

            public LeaderboardEntry(string initials, ulong score)
            {
                this.initials = initials;
                this.score = score;
            }
        }
     
        public struct LeaderboardUIEntry
        {
            public Text[] initials;
            public Text Score;
        }
    }


    Leaderboard leaderboard;
    double score;

    // UI elements
    public GameObject uiscore;
    Text ui_Score;

    // Inspector parameters
    [SerializeField]
    int scoreRate = 250; // Points per second

    
	// Use this for initialization
	void Start ()
    {
        score = 0.0;

        // Subscribe game reset code to obstacle collision event
        GetComponent<CollisionManager>().OnObstacleCollision += DisplayLeaderboard;

        // Leaderboard initialization
        leaderboard = new Leaderboard();

        InitializeUI();
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (leaderboard.Root.gameObject.activeSelf)
        {
            leaderboard.Update();
        }
        else
        {
            score += Time.deltaTime * scoreRate;
            ui_Score.text = "Score: " + (ulong)score;
        }
	}

    // Store references to UI elements
    void InitializeUI()
    {    
        Transform canvas = transform.Find("Canvas");

        leaderboard.InitializeUI(canvas);

        ui_Score = canvas.Find("ui_score").GetComponent<Text>();
        uiscore = canvas.Find("ui_score").gameObject;
        
    }
 
    void DisplayLeaderboard()
    {
        // Did the score make the leaderboard?
        if (score > leaderboard.entries[Leaderboard.NUMSLOTS - 1].score)
        {
            // Context Message
            leaderboard.ui_ContextMessage.text = "HIGH SCORE! ENTER YOUR INITIALS";           

            // Find position in leaderboard
            int curr = Leaderboard.NUMSLOTS - 1;
            while (--curr >= 0)
            {
                if ((ulong)score <= leaderboard.entries[curr].score)
                {
                    break;
                }             
            }
            ++curr;

            // Shift leaderboard down for new entry
            for (int i = Leaderboard.NUMSLOTS - 1; i > curr; i--)
            {
                leaderboard.entries[i] = leaderboard.entries[i - 1];
            }

            // Enter new score
            leaderboard.EnterHighscore(curr, (ulong)score);
            
        }
        else
        {
            // Did not make leaderboard
            leaderboard.ui_ContextMessage.text = "YOU LOSE. PRESS ANY KEY.";
        }

        // Hide score
        ui_Score.enabled = false;

        // Display leaderboard
        leaderboard.Display();
    }

    public void CloseLeaderboard()
    {
        leaderboard.Close();

        ui_Score.enabled = true;
        score = 0.0;
    }
}
