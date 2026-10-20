using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TaskDisplay : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI taskText;
    [SerializeField] TextMeshProUGUI bestTeamText;
    [SerializeField] RawImage image;
    [SerializeField] Button button;

    private void Start() {
        button.onClick.AddListener(OpenTask);
    }
    public void UpdateTaskDisplay(string name, GameTask task, string ownPass)
    {
        taskText.text = name;
        image.color = new Color(task.R(), task.G(), task.B());

        GameTask.Scoring bestScore = task.GetBestScore();

        if (bestScore != null) {
            bestTeamText.text = $"Holder: {bestScore.team.name}\n ";

            if (task.HasTeamScore(ownPass) || MainNetworking.Instance.IsAdmin) {
                bestTeamText.text += $"With Score {bestScore.score}";
            }
        } else {
            bestTeamText.text = $"Holder: None\n ";
        }
    }

    private void OpenTask() {
        UIManager.Instance.OpenTaskDisplay(name);
    }
}