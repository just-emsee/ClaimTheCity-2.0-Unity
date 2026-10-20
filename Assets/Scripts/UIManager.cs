using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    public enum ScreenState {
        SEARCHING_SERVER,
        LOGIN,
        GAME,
        TASK,
        ADMIN_CONSOLE
    }


    [Header("Game State")]
    [SerializeField] ScreenState screenState = ScreenState.SEARCHING_SERVER;

    [Header("Login Screen")]
    [SerializeField] GameObject loginScreen;
    [SerializeField] Button loginButton;
    [SerializeField] TMP_InputField loginField;
    [SerializeField] TextMeshProUGUI loginDebugText;

    [Header("Game Screen")]
    [SerializeField] GameObject gameScreen;
    [SerializeField] TextMeshProUGUI teamNameText;
    [SerializeField] GameObject taskDisplayParent;
    [SerializeField] GameObject taskDisplayPrefab;
    [SerializeField] GameObject adminButton;

    [Header("Task Screen")]
    [SerializeField] GameObject taskScreen;
    [SerializeField] TextMeshProUGUI taskTitle;

    [Header("Admin Console Screen")]
    [SerializeField] GameObject adminConsoleScreen;

    string debugText = "";
    string teamName = "";
    GameData gameData = null;
    string currentOpenTask = null;

    private void Start() {
        if (Instance != null) {
            Debug.LogError("Multiple UIManagers detected");
        } else
            Instance = this;
        loginButton.onClick.AddListener(TryLogin);
        loginDebugText.text = debugText;
    }
    void Update()
    {
        loginScreen.SetActive(screenState==ScreenState.LOGIN);
        gameScreen.SetActive(screenState==ScreenState.GAME);
        taskScreen.SetActive(screenState==ScreenState.TASK);
        adminConsoleScreen.SetActive(screenState==ScreenState.ADMIN_CONSOLE);
        adminButton.SetActive(MainNetworking.Instance.IsAdmin);
        loginDebugText.text = debugText;
        teamNameText.text = teamName;
    }

    void TryLogin() {
        MainNetworking.Instance.AttemptLogin(loginField.text);
    }

    public void ServerFound() {
        screenState = ScreenState.LOGIN;
    }

    public void LoggedInWrongPass() {
        debugText = "Wrong Password";
    }

    public void LoggedIn() {
        screenState=ScreenState.GAME;
    }


    public void SetTeamName(string teamName) {
        string finalTeamName = teamName.Replace("_", " ");

        teamName = $"Team Name: {finalTeamName}";
    }

    public void UpdateGameData(GameData gameData) {
        this.gameData = gameData;
        UpdateGameDataToScreen();
    }

    private void UpdateGameDataToScreen() {
        if (gameData == null) return;

        // Clear existing task displays
        foreach (Transform child in taskDisplayParent.transform) {
            Destroy(child.gameObject);
        }

        // Create new task displays
        foreach (KeyValuePair<string, GameTask> pair in gameData.GetTasks()) {
            string taskName = pair.Key;
            GameTask task = pair.Value;
            task.RecalculateBestScore();
            GameObject obj = Instantiate(taskDisplayPrefab, taskDisplayParent.transform);
            obj.GetComponent<TaskDisplay>().UpdateTaskDisplay(taskName, task, MainNetworking.Instance.GetTeamPass());
            obj.name = taskName;
        }
    }

    public void SetScreen(ScreenState newState) {
        screenState = newState;
    }

    public void OpenTaskDisplay(string name) {
        currentOpenTask = name;
        taskTitle.text = name;
        screenState = ScreenState.TASK;
    }

    public string CurrentOpenTask() {
        return currentOpenTask;
    }
}
