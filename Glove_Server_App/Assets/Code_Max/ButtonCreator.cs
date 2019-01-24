using UnityEngine;
using UnityEngine.UI;

public class ButtonCreator : MonoBehaviour
{
    public GameObject buttonPrefab;
    public GameObject panelToAttachButtonsTo;

    public int numberOfButtons = 36;
    private GameObject[] buttonList;

    public Scrollbar bar;

    public GameObject imagePlaceholder;

    private GameObject image1;
    private GameObject image2;

    int buttonHighlight = 0;
    bool buttonClicked = false;

    Vector2 buttonBig = new Vector2(160, 50);
    Vector2 buttonSmall = new Vector2(160, 30);
    Vector2 buttonClickBig = new Vector2(160, 70);
    float pressedButtonBonus = 0.9f;

    void Start()//Creates a button and sets it up
    {
        buttonList = new GameObject[numberOfButtons];

        image1 = (GameObject)Instantiate(imagePlaceholder);
        image1.transform.SetParent(panelToAttachButtonsTo.transform);//Setting button parent

        for (int i = 0; i < numberOfButtons; i++)
        {
            buttonList[i] = (GameObject)Instantiate(buttonPrefab);

            buttonList[i].transform.SetParent(panelToAttachButtonsTo.transform);//Setting button parent
            buttonList[i].GetComponent<Button>().onClick.AddListener(OnClick);//Setting what button does when clicked
                                                                              //Next line assumes button has child with text as first gameobject like button created from GameObject->UI->Button
            buttonList[i].transform.GetChild(0).GetComponent<Text>().text = "Button " + i ;//Changing text
        }

        image2 = (GameObject)Instantiate(imagePlaceholder);
        image2.transform.SetParent(panelToAttachButtonsTo.transform);//Setting button parent
    }

    void Update()
    {
        int buttonHighlightNow = (int)(numberOfButtons * (1 - bar.value));

        float buttonHighlightNowFloat = ((float)numberOfButtons * (1 - bar.value));

        //Debug.Log((int) 3.9f);

        // this is to make the currently selected button selection favored over changing the button
        if ((((buttonHighlightNowFloat + pressedButtonBonus) > buttonHighlight) && (buttonHighlightNowFloat - pressedButtonBonus < buttonHighlight + 1)) || (((buttonHighlightNowFloat - pressedButtonBonus) < buttonHighlight + 1) && ((buttonHighlightNowFloat + pressedButtonBonus) > buttonHighlight)))
        {
            buttonHighlightNow = buttonHighlight;
        }

        // set the borders
        if (buttonHighlightNow < 0)
            buttonHighlightNow = 0;
        if (buttonHighlightNow > (numberOfButtons - 1))
            buttonHighlightNow = numberOfButtons - 1;        

        // increase button size and change color
        if (buttonHighlight != buttonHighlightNow)
        {
            Image im1 = buttonList[buttonHighlight].GetComponent<Image>();
            im1.color = Color.white;

            Image im2 = buttonList[buttonHighlightNow].GetComponent<Image>();
            im2.color = Color.grey;

            RectTransform RT1 = buttonList[buttonHighlight].GetComponent<RectTransform>();
            RT1.sizeDelta = buttonSmall;

            RectTransform RT2 = buttonList[buttonHighlightNow].GetComponent<RectTransform>();
            RT2.sizeDelta = buttonBig;
        }

        buttonHighlight = buttonHighlightNow;

        if (buttonClicked == true)
        {
            buttonList[buttonHighlight].GetComponent<Button>().onClick.Invoke();

            buttonClicked = false;
        }
    }

    void OnClick()
    {
        Debug.Log("clicked button with fist");

        RectTransform RT1 = buttonList[buttonHighlight].GetComponent<RectTransform>();
        RT1.sizeDelta = buttonClickBig;

        Image im1 = buttonList[buttonHighlight].GetComponent<Image>();
        im1.color = Color.red;
    }

    public void clicked()
    {
        buttonClicked = true;
        //buttonList[buttonHighlight].GetComponent<Button>().onClick.Invoke();
    }
}
