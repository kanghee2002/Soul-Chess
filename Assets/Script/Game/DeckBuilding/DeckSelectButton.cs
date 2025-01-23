using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


public class DeckSelectButton : MonoBehaviour
{
    public TextMeshProUGUI deckname;
    public int deck_index;
    public void DeckSelect()
    {
        //������ ���õǾ� �ִ� ���� ���̴� ����
        if(GetComponentInParent<LobbySceneUI>().SelectedDeckIndex != -1)
            GetComponentInParent<LobbySceneUI>().GetSelectedDeckButton().ControlShader(false);

        //���� �� ���̴� Ȱ��ȭ
        ControlShader(true);

        GetComponentInParent<LobbySceneUI>().SelectedDeckIndex = deck_index;

        //ī���� ���ٸ� ���⼭�� �߰��ؾ���
        GetComponentInParent<LobbySceneUI>().ShowSelectedDeckCardList();
    }

    public void ControlShader(bool activate)
    { }
}
