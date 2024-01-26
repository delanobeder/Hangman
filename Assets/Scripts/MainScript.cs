using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine.EventSystems;
using UnityEngine.Networking;

using SimpleJSON;

public class MainScript : MonoBehaviour {

    public Text QuestionText, DashesText, ResultsText, ActionText;
    public Image HangmanImage, FinalImage;
    public Sprite[] HangmanSprites;
    public Sprite WinSprite, LoseSprite;

    public GameObject MainDialogue, FinalDialogue;

    private int currentHangmanSprite = 0;
    private const int TOTAL_HANGMAN_SPRITES = 8;
    private const char PLACEHOLDER = '*';
    private const string DICT_FILE_NAME = "Dictionary";

    private Dictionary<string, string> gameDict;
    private string answer, userInput;
    
	void Start () {
        gameDict = new Dictionary<string, string>();
        LoadDictionary();
    }
	
    public void OnRestartClicked() {
        MainDialogue.SetActive(true);
        FinalDialogue.SetActive(false);
        currentHangmanSprite = 0;
        HangmanImage.sprite = HangmanSprites[currentHangmanSprite];
        PickRandomQuestion();
    }

    public void OnGuessSubmitted(Button button) {
        char letter = button.GetComponentInChildren<Text>().text.ToCharArray()[0];
        if ( answer.Contains(letter) ) {
            UpdateAnswerText(letter);
            if ( CheckWinCondition() ) {
                Debug.Log("You won the game !");
                ShowFinalDialogue(true);
            }
        }
        else
        {
            if (CheckLoseCondition()) {
                Debug.Log("You lost the game");
                ShowFinalDialogue(false);
            }
            else { DrawNextHangmanPart(); }
        }
    }

    private void PickRandomQuestion()
    {
        int randInt = Random.Range(0, gameDict.Count);
        QuestionText.text = gameDict.ElementAt(randInt).Value;
        answer = gameDict.ElementAt(randInt).Key.ToUpper();
        StringBuilder sb = new StringBuilder("");
        for (int i = 0; i < answer.Length; i++) { sb.Append(PLACEHOLDER); }
        DashesText.text = sb.ToString();
        userInput = sb.ToString();
        Debug.Log("Answer: " + answer);
    }

    private void LoadDictionary()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
            StartCoroutine(GetByHTTP());
        #else
            GetByBSA();
        #endif
    }

    private IEnumerator GetByHTTP() {
        Debug.Log(Application.streamingAssetsPath);
        string URL = Path.Combine(Application.streamingAssetsPath, DICT_FILE_NAME + ".json");
        Debug.Log(URL);
        using(UnityWebRequest www = UnityWebRequest.Get(URL)) {
        	yield return www.SendWebRequest();
 
        	if (www.result == UnityWebRequest.Result.ProtocolError || 
                www.result == UnityWebRequest.Result.ConnectionError) {
            	Debug.Log(www.error);
        	}
        	else {
            	// Show results as text
            	Debug.Log(www.downloadHandler.text);
                var jsonText = www.downloadHandler.text;
                
                HangmanInfo info = JsonUtility.FromJson<HangmanInfo>(jsonText);
                
                ActionText.text = info.phrase;
                
                for (int i = 0; i < info.challenges.Length; i++) {
                    gameDict[info.challenges[i].answer] = info.challenges[i].clue;
                    Debug.Log(info.challenges[i].answer + " - " + info.challenges[i].clue);
                }

                PickRandomQuestion();
            }
        }
    }

    private void GetByBSA() {
        BetterStreamingAssets.Initialize();

        var jsonText = BetterStreamingAssets.ReadAllText(DICT_FILE_NAME + ".json");
        HangmanInfo info = JsonUtility.FromJson<HangmanInfo>(jsonText);

        ActionText.text = info.phrase;
        
        for (int i = 0; i < info.challenges.Length; i++) {
            gameDict[info.challenges[i].answer] = info.challenges[i].clue;
            Debug.Log(info.challenges[i].answer + " - " + info.challenges[i].clue);
        }

        PickRandomQuestion();
    }

    private void UpdateAnswerText(char letter) {
        char[] userInputArray = userInput.ToCharArray();
        for (int i = 0; i < answer.Length; i++) {
            if (userInputArray[i] != PLACEHOLDER) { continue; } // already guessed
            if (answer[i] == letter) { userInputArray[i] = letter; }
        }
        userInput = new string(userInputArray);
        DashesText.text = userInput;
    }

    private void DrawNextHangmanPart() {
        currentHangmanSprite = ++currentHangmanSprite % TOTAL_HANGMAN_SPRITES;
        HangmanImage.sprite = HangmanSprites[currentHangmanSprite];
    }

    private bool CheckWinCondition() { return answer.Equals(userInput); }
    private bool CheckLoseCondition() { return currentHangmanSprite == TOTAL_HANGMAN_SPRITES-1; }

    private void ShowFinalDialogue(bool win) {
        MainDialogue.SetActive(false);
        FinalDialogue.SetActive(true);
        FinalImage.sprite = win ? WinSprite : LoseSprite;
        ResultsText.text = win ? "Victory !" : "Defeat !!!";
    }
}

[System.Serializable]
public class Challenge
{
    public string answer;
    public string clue;
}

[System.Serializable]
public class HangmanInfo {
    public string phrase;
    public Challenge[] challenges;
}