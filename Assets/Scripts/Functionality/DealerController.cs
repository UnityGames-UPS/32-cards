using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DealerController : MonoBehaviour
{
    [Header("ImageAnimations")]
    [SerializeField]
    private ImageAnimation DealerImageAnim_IA;
    [SerializeField]
    private ImageAnimation BoxAnim_IA;
    [SerializeField]
    private ImageAnimation LeftHandAnim_IA;
    [SerializeField]
    private ImageAnimation RightHandAnim_IA;
    [SerializeField]
    private ImageAnimation BothHandAnim_IA;

    [Header("Sprites")]
    [SerializeField]
    private List<Sprite> BoxOpen_Sprites;
    [SerializeField]
    private List<Sprite> BoxClose_Sprites;
    [SerializeField]
    private List<Sprite> DealerShuffle_Sprites;
    [SerializeField]
    private List<Sprite> DealerReset_Sprites;
    [SerializeField]
    private List<Sprite> DealerDeal_Sprites;
    [SerializeField]
    private List<Sprite> DealerRest_Sprites;
    [SerializeField]
    private List<Sprite> BothhandsShuffle_Sprites;
    [SerializeField]
    private List<Sprite> BothHandsReset_Sprites;

    [Header("GameObjects")]
    [SerializeField]
    private GameObject BothHands_Object;
    [SerializeField]
    private GameObject LeftHand_Object;
    [SerializeField]
    private GameObject RightHand_Object;
    [SerializeField]
    private GameObject DealerMoving_Object;
    [SerializeField]
    private GameObject DealerRest_Object;

    [Header("Transforms")]
    [SerializeField]
    private Transform BothHands_Transform;
    [SerializeField]
    private Transform LeftHand_Transform;
    [SerializeField]
    private Transform RightHand_Transform;
    [SerializeField]
    private Transform BettingPanel_Transform;
    [SerializeField]
    private Transform Box_Transform;
    [SerializeField]
    private CardController cardManager;

    private void ShuffleCardsAnimation()
    {
        DealerImageAnim_IA.textureArray.Clear();
        DealerImageAnim_IA.textureArray.TrimExcess();
        DealerImageAnim_IA.textureArray.AddRange(DealerShuffle_Sprites);

        BothHandAnim_IA.textureArray.Clear();
        BothHandAnim_IA.textureArray.TrimExcess();
        BothHandAnim_IA.textureArray.AddRange(BothhandsShuffle_Sprites);

        DealerImageAnim_IA.AnimationSpeed = 90;
        BothHandAnim_IA.AnimationSpeed = 90;

        BothHands_Object.SetActive(true);
        LeftHand_Object.SetActive(false);
        RightHand_Object.SetActive(false);

        DealerImageAnim_IA.cardDeal = false;
        DealerImageAnim_IA.cardReset = false;
        DealerImageAnim_IA.cardShuffle = true;

        DealerMoving_Object.SetActive(true);
        DealerRest_Object.SetActive(false);

        DealerImageAnim_IA.StopAnimation();
        BothHandAnim_IA.StopAnimation();
        DealerImageAnim_IA.StartAnimation();
        BothHandAnim_IA.StartAnimation();
    }

    private void resetCardsAnimation()
    {
        DealerImageAnim_IA.textureArray.Clear();
        DealerImageAnim_IA.textureArray.TrimExcess();
        DealerImageAnim_IA.textureArray.AddRange(DealerReset_Sprites);

        BothHandAnim_IA.textureArray.Clear();
        BothHandAnim_IA.textureArray.TrimExcess();
        BothHandAnim_IA.textureArray.AddRange(BothHandsReset_Sprites);

        DealerImageAnim_IA.AnimationSpeed = 15;
        BothHandAnim_IA.AnimationSpeed = 15;

        BothHands_Object.SetActive(true);
        LeftHand_Object.SetActive(false);
        RightHand_Object.SetActive(false);

        DealerImageAnim_IA.cardDeal = false;
        DealerImageAnim_IA.cardReset = true;
        DealerImageAnim_IA.cardShuffle = false;

        DealerMoving_Object.SetActive(true);
        DealerRest_Object.SetActive(false);

        DealerImageAnim_IA.StopAnimation();
        BothHandAnim_IA.StopAnimation();
        DealerImageAnim_IA.StartAnimation();
        BothHandAnim_IA.StartAnimation();
    }

    private void DealCards()
    {
        DealerImageAnim_IA.textureArray.Clear();
        DealerImageAnim_IA.textureArray.TrimExcess();
        DealerImageAnim_IA.textureArray.AddRange(DealerDeal_Sprites);

        DealerImageAnim_IA.AnimationSpeed = 215;

        BothHands_Object.SetActive(false);
        LeftHand_Object.SetActive(true);
        RightHand_Object.SetActive(true);

        DealerImageAnim_IA.cardDeal = true;
        DealerImageAnim_IA.cardReset = false;
        DealerImageAnim_IA.cardShuffle = false;

        DealerMoving_Object.SetActive(true);
        DealerRest_Object.SetActive(false);

        DealerImageAnim_IA.StopAnimation();
        LeftHandAnim_IA.StopAnimation();
        RightHandAnim_IA.StopAnimation();
        DealerImageAnim_IA.StartAnimation();
        LeftHandAnim_IA.StartAnimation();
        RightHandAnim_IA.StartAnimation();
    }

    internal void BoxOpenAnimation()
    {
        BoxAnim_IA.textureArray.Clear();
        BoxAnim_IA.textureArray.TrimExcess();
        BoxAnim_IA.textureArray.AddRange(BoxOpen_Sprites);
        BoxAnim_IA.AnimationSpeed = 20;
        BoxAnim_IA.StopAnimation();
        BoxAnim_IA.StartAnimation();
    }
    internal void BoxCloseAnimation()
    {
        BoxAnim_IA.textureArray.Clear();
        BoxAnim_IA.textureArray.TrimExcess();
        BoxAnim_IA.textureArray.AddRange(BoxClose_Sprites);
        BoxAnim_IA.AnimationSpeed = 20;
        BoxAnim_IA.StopAnimation();
        BoxAnim_IA.StartAnimation();
    }

    internal void SetLayeringForBothHands(bool IsAbove)
    {
        if (IsAbove)
        {
            BothHands_Transform.SetSiblingIndex(Box_Transform.GetSiblingIndex());
        }
        else
        {
            BothHands_Transform.SetSiblingIndex(BettingPanel_Transform.GetSiblingIndex());
        }
    }
    internal void SetLayeringForLeftHand(bool IsAbove)
    {
        if (IsAbove)
        {
            LeftHand_Transform.SetSiblingIndex(Box_Transform.GetSiblingIndex());
        }
        else
        {
            LeftHand_Transform.SetSiblingIndex(BettingPanel_Transform.GetSiblingIndex());
        }
    }
    internal void SetLayeringForRightHand(bool IsAbove)
    {
        if (IsAbove)
        {
            RightHand_Transform.SetSiblingIndex(Box_Transform.GetSiblingIndex());
        }
        else
        {
            RightHand_Transform.SetSiblingIndex(BettingPanel_Transform.GetSiblingIndex());
        }
    }

    internal void SwitchToRest()
    {
        DealerMoving_Object.SetActive(false);
        DealerRest_Object.SetActive(true);

        BothHands_Object.SetActive(false);
        LeftHand_Object.SetActive(false);
        RightHand_Object.SetActive(false);
    }

    internal void moveCard(int type)
    {
        cardManager.SpawnCard(type);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            ShuffleCardsAnimation();
        }
        else if (Input.GetKeyDown(KeyCode.B))
        {
            resetCardsAnimation();
        }
        else if (Input.GetKeyDown(KeyCode.C))
        {
            DealCards();
        }
    }

}
