using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SetUIScreenButton : MonoBehaviour
{
    [SerializeField] UIManager.ScreenState screenState;
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() => UIManager.Instance.SetScreen(screenState));
    }
}
