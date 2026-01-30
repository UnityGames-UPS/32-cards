using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using DG.Tweening;

public class CardController : MonoBehaviour
{
    [SerializeField]
    private Transform SpawnPoint_Transform;
    [SerializeField]
    private Transform Card8Pos_Transform;
    [SerializeField]
    private Transform Card9Pos_Transform;
    [SerializeField]
    private Transform Card10Pos_Transform;
    [SerializeField]
    private Transform Card11Pos_Transform;
    [SerializeField]
    private GameObject Card8_Object;
    [SerializeField]
    private GameObject Card9_Object;
    [SerializeField]
    private GameObject Card10_Object;
    [SerializeField]
    private GameObject Card11_Object;

    internal void SpawnCard(int type)
    {
        GameObject cardref = null;
        switch (type)
        {
            case 8:
                cardref = GameObject.Instantiate(Card8_Object, SpawnPoint_Transform);
                ShiftCardToPos(cardref, 1.5f, Card8Pos_Transform);
                break;
            case 9:
                cardref = GameObject.Instantiate(Card9_Object, SpawnPoint_Transform);
                ShiftCardToPos(cardref, 1.2f, Card9Pos_Transform);
                break;
            case 10:
                cardref = GameObject.Instantiate(Card10_Object, SpawnPoint_Transform);
                ShiftCardToPos(cardref, 1.2f, Card10Pos_Transform);
                break;
            case 11:
                cardref = GameObject.Instantiate(Card11_Object, SpawnPoint_Transform);
                ShiftCardToPos(cardref, 1.2f, Card11Pos_Transform);
                break;
        }
    }

    private void ShiftCardToPos(GameObject card, float time, Transform FinalPos, int number = 0)
    {
        card.transform.SetParent(FinalPos);
        card.transform.DOLocalMove(Vector3.zero, time).OnComplete(() =>
        {
            card.transform.DOLocalRotate(new Vector3(180f, 0f, 0f), 0.4f, RotateMode.LocalAxisAdd);
        });

    }
}
