using UnityEngine;
using UnityEngine.UI;

public class FieldNavigationButton : MonoBehaviour
{
    public enum ButtonType { Next, Previous, Mine }
    public ButtonType buttonType;

    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnButtonClick);
    }

    void OnButtonClick()
    {
        // Find the controller each time (handles scene reloads)
        FieldCycleController controller = FindObjectOfType<FieldCycleController>();

        if (controller == null)
        {
            Debug.LogWarning("FieldCycleController not found!");
            return;
        }

        switch (buttonType)
        {
            case ButtonType.Next:
                controller.ViewNextField();
                break;
            case ButtonType.Previous:
                controller.ViewPreviousField();
                break;
            case ButtonType.Mine:
                controller.ViewMyField();
                break;
        }
    }
}