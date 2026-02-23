using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageAnimation : MonoBehaviour
{
	public enum ImageState
	{
		NONE,
		PLAYING,
		PAUSED
	}

	public List<Sprite> textureArray;

	public Image rendererDelegate;

	public bool useSharedMaterial = true;

	public bool doLoopAnimation = true;
	[SerializeField] private bool StartOnAwake;

	[SerializeField] private bool StartonEnable;

	[SerializeField] private bool DisableonComplete;

	[HideInInspector]
	public ImageState currentAnimationState;

	private int indexOfTexture;

	private float idealFrameRate = 0.0416666679f;

	private float delayBetweenAnimation;

	public float AnimationSpeed = 5f;

	public float delayBetweenLoop;
	[SerializeField]
	private DealerController dealControl;

	internal bool cardShuffle = false;

	internal bool cardReset = false;

	internal bool cardDeal = false;

	private void Awake()
	{
		if (StartOnAwake)
		{
			StartAnimation();
		}
	}

	void Start()
	{
		//rendererDelegate= this.GetComponent<Image>();
	}
	private void OnEnable()
	{
		if (StartonEnable) StartAnimation();
	}

	private void OnDisable()
	{
		//rendererDelegate.sprite = textureArray[0];
		StopAnimation();
	}

	private void AnimationProcess()
	{
		SetTextureOfIndex();
		indexOfTexture++;
		if (indexOfTexture == textureArray.Count)
		{
			indexOfTexture = 0;
			if (doLoopAnimation)
			{
				Invoke("AnimationProcess", delayBetweenAnimation + delayBetweenLoop);
			}
			else if (DisableonComplete)
			{
				this.gameObject.SetActive(false);
			}
		}
		else
		{
			Invoke("AnimationProcess", delayBetweenAnimation);
		}
		if (dealControl != null && cardShuffle)
		{
			ShufffleController();
		}
		if (dealControl != null && cardReset)
		{
			ResetController();
		}
		if (dealControl != null && cardDeal)
		{
			CardDealController();
		}
	}
	void CardDealController()
	{
		switch (indexOfTexture)
		{
			case 8:
				dealControl.SetLayeringForRightHand(true);
				dealControl.MoveCard(8);
				break;
			case 30:
				dealControl.SetLayeringForRightHand(false);
				break;
			case 67:
				dealControl.SetLayeringForRightHand(true);
				dealControl.MoveCard(9);
				break;
			case 89:
				dealControl.SetLayeringForRightHand(false);
				break;
			case 126:
				dealControl.SetLayeringForRightHand(true);
				dealControl.MoveCard(10);
				break;
			case 192:
				dealControl.MoveCard(11);
				break;
			case 14:
				dealControl.SetLayeringForLeftHand(true);
				break;
			case 53:
				dealControl.SetLayeringForLeftHand(false);
				break;
			case 72:
				dealControl.SetLayeringForLeftHand(true);
				break;
			case 116:
				dealControl.SetLayeringForLeftHand(false);
				break;
			case 235:
				cardDeal = false;
				dealControl.SwitchToRest();
				dealControl.SetLayeringForLeftHand(false);
				dealControl.SetLayeringForRightHand(false);
				break;
		}
	}

	void ResetController()
	{
		switch (indexOfTexture)
		{
			case 4:
				dealControl.SetLayeringForBothHands(true);
				break;
			case 17:
				dealControl.SetLayeringForBothHands(false);
				break;
			case 19:
				cardReset = false;
				dealControl.SwitchToRest();
				break;
		}
	}

	void ShufffleController()
	{
		switch (indexOfTexture)
		{
			case 3:
				dealControl.SetLayeringForBothHands(true);
				break;
			case 2:
				dealControl.BoxOpenAnimation();
				break;
			case 80:
				dealControl.BoxCloseAnimation();
				break;
			case 95:
				dealControl.SetLayeringForBothHands(false);
				break;
			case 107:
				cardShuffle = false;
				dealControl.SwitchToRest();
				break;
		}
	}

	public void StartAnimation()
	{
		indexOfTexture = 0;
		if (currentAnimationState == ImageState.NONE)
		{
			RevertToInitialState();
			delayBetweenAnimation = idealFrameRate * (float)textureArray.Count / AnimationSpeed;
			currentAnimationState = ImageState.PLAYING;
			Invoke("AnimationProcess", delayBetweenAnimation);
		}
	}

	public void PauseAnimation()
	{
		if (currentAnimationState == ImageState.PLAYING)
		{
			CancelInvoke("AnimationProcess");
			currentAnimationState = ImageState.PAUSED;
		}
	}

	public void ResumeAnimation()
	{
		if (currentAnimationState == ImageState.PAUSED && !IsInvoking("AnimationProcess"))
		{
			Invoke("AnimationProcess", delayBetweenAnimation);
			currentAnimationState = ImageState.PLAYING;
		}
	}

	public void StopAnimation()
	{
		if (currentAnimationState != 0)
		{
			rendererDelegate.sprite = textureArray[0];
			CancelInvoke("AnimationProcess");
			currentAnimationState = ImageState.NONE;
		}
	}

	public void RevertToInitialState()
	{
		indexOfTexture = 0;
		SetTextureOfIndex();
	}

	private void SetTextureOfIndex()
	{
		if (useSharedMaterial)
		{
			rendererDelegate.sprite = textureArray[indexOfTexture];
		}
		else
		{
			rendererDelegate.sprite = textureArray[indexOfTexture];
		}
	}
}
