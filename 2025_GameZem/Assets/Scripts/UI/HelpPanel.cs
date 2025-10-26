using UnityEngine;
using UnityEngine.UI;

public class HelpPanel : MonoBehaviour
{
    [SerializeField] private Sprite[] helpImages;
    [SerializeField] private Image descriptionImage;
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private Button closeButton;

    private int currentIndex = 0;
    private int maxIndex = 3;

    void Awake()
    {
        leftButton.onClick.AddListener(OnLeftButtonClicked);
        rightButton.onClick.AddListener(OnRightButtonClicked);
        closeButton.onClick.AddListener(OnCloseButtonClicked);
    }


    private void OnCloseButtonClicked()
    {
        gameObject.SetActive(false);
    }
    private void OnLeftButtonClicked()
    {

        currentIndex--;
        if(currentIndex < 0)
        {
            currentIndex = maxIndex;
        }
        descriptionImage.sprite = helpImages[currentIndex];
    }
    private void OnRightButtonClicked()
    {
        currentIndex++;
        if(currentIndex > maxIndex)
        {
            currentIndex = 0;
        }
        descriptionImage.sprite = helpImages[currentIndex];
    }


    
    
}
