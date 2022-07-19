using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestSystem : MonoBehaviour
{
    public GameObject contents;
    public GameObject contentsPrifab;
    private RectTransform contentsRectTransform;

    public Image monsterImage;
    public TextMeshProUGUI monsterName;
    public TextMeshProUGUI clearGold;
    public TextMeshProUGUI questName;
    public TextMeshProUGUI questContents;

    public GameObject orderButton;

    private int target;

    public Sprite sword;
    private Player player;
    private void Awake()
    {
        contentsRectTransform = contents.GetComponent<RectTransform>();
        player = FindObjectOfType<Player>();
    }
    void Start()
    {
        for (int i = 0; i < DataManager.instance.questList.Count; i++)
        {
            CombiContentsUI temp = Instantiate(contentsPrifab, contents.transform).GetComponent<CombiContentsUI>();
            temp.image.sprite = sword;
            temp.textMeshProUGUI.text = DataManager.instance.questList[i].questName;
            int tempint = i;
            temp.gameObject.GetComponent<Button>().onClick.AddListener(() => { QuestView(tempint); });
            contentsRectTransform.sizeDelta = new Vector2(contentsRectTransform.sizeDelta.x, contentsRectTransform.sizeDelta.y + 50);
        }
        orderButton.SetActive(false);
    }
    public void QuestView(int num)
    {
        target = num;
        Quest quest = DataManager.instance.questList[num];
        //monsterImage = 몬스터리스트[quest.targetMonster].image
        //monsterName = 몬스터리스트[quest.targetMonster].name
        clearGold.text = "보상 : "+quest.clearGold.ToString()+"골드";
        questName.text = quest.questName;
        questContents.text = quest.questContents;
        orderButton.SetActive(true);
    }
    public void OrderButton()
    {
        //수락했다는 UI 작동
        player.orderQuest = DataManager.instance.questList[target];
    }
}
