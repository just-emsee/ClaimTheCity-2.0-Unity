using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AdminConsole : MonoBehaviour
{
    private static AdminConsole Instance;

    [SerializeField] TMP_InputField input;
    [SerializeField] TextMeshProUGUI consoleText;
    [SerializeField] ScrollRect scrollView;
    void Start()
    {
        if (Instance != null) {
            Debug.LogError("Multiple admin consoles detected");
        } else
            Instance = this;
        input.onSubmit.AddListener(InputFieldFinished);
    }

    private void InputFieldFinished(string text) {
        input.text = "";
        logText("you:"+text);
        MainNetworking.Instance.SendAdminCommand(text);
    }

    private void logText(string text) {
        consoleText.text += "\n";
        consoleText.text += text;
        Vector2 size = (consoleText.transform as RectTransform).sizeDelta;
        size.y = consoleText.preferredHeight;
        (consoleText.transform as RectTransform).sizeDelta = size;

        scrollView.normalizedPosition = new Vector2(0,0);
    }

    public static void logConsoleText(string text) {
        Instance?.logText(text);
    }
}
