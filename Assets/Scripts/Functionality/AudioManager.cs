using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum SoundEffect
{
  ButtonClick,
  LobbyButton,
  BetOption,
  OnBet,
  BetLocked,
  OnCancelUndo,
  CardFlip,
  CardsReset,
  RoundStart,
  RoundResult,
  OnRoundEnd,
  ChipsWon,
  CountDownTimer,
  TimeIsRunningOut,
  NoMoreBets
}

public class AudioManager : MonoBehaviour
{
  [SerializeField] private AudioSource bgMusicSource;
  [SerializeField] private AudioSource buttonClickSource;
  [SerializeField] private AudioSource RoundResultSource;
  [SerializeField] private AudioSource BetLockedSource;
  [SerializeField] private AudioSource OnRoundEndSource; //When all the cards are revealed
  [SerializeField] private AudioSource CardsResetSource;
  [SerializeField] private AudioSource CardFlipSource;
  [SerializeField] private AudioSource BetOptionSource;
  [SerializeField] private AudioSource LobbyButtonSource;
  [SerializeField] private AudioSource TimeIsRunningOutSource;
  [SerializeField] private AudioSource OnBetSource;
  [SerializeField] private AudioSource OnChipsWonSource;
  [SerializeField] private AudioSource CountDownTimerSource;
  [SerializeField] private AudioSource OnRoundStartSource;
  [SerializeField] private AudioSource NoMoreBetsSource;
  [SerializeField] private AudioSource OnCancelUndoSource;
  [SerializeField] private AudioClip player8WinsClip;
  [SerializeField] private AudioClip player9WinsClip;
  [SerializeField] private AudioClip player10WinsClip; 
  [SerializeField] private AudioClip player11WinsClip;
  

  private bool isSoundMuted = false;
  private bool isMusicMuted = false;

  internal void PlayPlayerWinSFX(int playerIndex)
  {
    if (isSoundMuted) return;

    AudioClip clipToPlay = playerIndex switch
    {
      8 => player8WinsClip,
      9 => player9WinsClip,
      10 => player10WinsClip,
      11 => player11WinsClip,
      _ => null
    };

    RoundResultSource.clip = clipToPlay;
    if (clipToPlay != null) RoundResultSource.Play();
  }

  internal void PlaySFX(SoundEffect sfx)
  {
    if (isSoundMuted) return;

    AudioSource source = sfx switch
    {
      SoundEffect.ButtonClick => buttonClickSource,
      SoundEffect.LobbyButton => LobbyButtonSource,
      SoundEffect.BetOption => BetOptionSource,
      SoundEffect.OnBet => OnBetSource,
      SoundEffect.BetLocked => BetLockedSource,
      SoundEffect.OnCancelUndo => OnCancelUndoSource,
      SoundEffect.CardFlip => CardFlipSource,
      SoundEffect.CardsReset => CardsResetSource,
      SoundEffect.RoundStart => OnRoundStartSource,
      SoundEffect.OnRoundEnd => OnRoundEndSource,
      SoundEffect.ChipsWon => OnChipsWonSource,
      SoundEffect.CountDownTimer => CountDownTimerSource,
      SoundEffect.TimeIsRunningOut => TimeIsRunningOutSource,
      SoundEffect.NoMoreBets => NoMoreBetsSource,
      _ => null
    };

    if (source != null) source.Play();
  }

  internal void PlayBgMusic()
  {
    if (bgMusicSource != null) bgMusicSource.Play();
  }

  internal void ToggleSoundMute(bool mute)
  {
    isSoundMuted = mute;
    AudioSource[] sfxSources = {
            buttonClickSource, RoundResultSource, BetLockedSource, OnRoundEndSource,
            CardsResetSource, CardFlipSource, BetOptionSource, LobbyButtonSource,
            TimeIsRunningOutSource, OnBetSource, OnChipsWonSource, CountDownTimerSource,
            OnRoundStartSource, NoMoreBetsSource, OnCancelUndoSource
        };
    foreach (var src in sfxSources)
      if (src != null) src.mute = mute;
  }

  internal void ToggleMusicMute(bool mute)
  {
    isMusicMuted = mute;
    if (bgMusicSource != null) bgMusicSource.mute = mute;
  }

  internal void PauseAllAudio()
  {
    if (bgMusicSource != null) bgMusicSource.Pause();
    AudioSource[] sfxSources = {
        buttonClickSource, RoundResultSource, BetLockedSource, OnRoundEndSource,
        CardsResetSource, CardFlipSource, BetOptionSource, LobbyButtonSource,
        TimeIsRunningOutSource, OnBetSource, OnChipsWonSource, CountDownTimerSource,
        OnRoundStartSource, NoMoreBetsSource, OnCancelUndoSource
    };
    foreach (var src in sfxSources)
      if (src != null) src.Stop();
  }

  internal void ResumeAudio()
  {
    if (bgMusicSource != null && !isMusicMuted) bgMusicSource.UnPause();
  }
}
