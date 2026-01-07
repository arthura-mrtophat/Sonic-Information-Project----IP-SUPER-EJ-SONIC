using Godot;
using System;
using System.Security.Cryptography.X509Certificates;

public enum SoundEffectType 
{
	JumpSFX,
	ExplodeSFX,
	SonicHurtSFX,
	SonicSpinSFX,
	RingSFX,
	RingLossSFX,
	
	EnemyHurtSFX,
	MenuAcceptSFX,
	SpringSFX,
	
	MUSIC_Title,
	MUSIC_Zone1,
	MUSIC_Boss,
}

[GlobalClass]
public partial class SoundEffect : Resource
{
	[Export] public int PlayLimit = 5;
	[Export] public SoundEffectType SoundType;
	[Export] public float Volume = 0;
	[Export] public AudioStream Sound;

	public int CurrentlyPlayingCount = 0;

	public void ChangePlayingCount(int Amount = 1) 
		=> CurrentlyPlayingCount = Mathf.Max(0, CurrentlyPlayingCount + Amount);
	public bool CanPlay() => CurrentlyPlayingCount < PlayLimit;
	public void AudioFinishedPlaying() => ChangePlayingCount(-1);

	public AudioStreamPlayer? CreateAudio2D(Node ParentNode)
	{
		if (!CanPlay()) 
			return null;
		var audioInstance = new AudioStreamPlayer();
		ParentNode.AddChild(audioInstance);
		audioInstance.Stream = Sound;
		audioInstance.SetVolumeDb(Volume);
		audioInstance.Finished += AudioFinishedPlaying;
		audioInstance.Finished += audioInstance.QueueFree;
		audioInstance.Play();
		return audioInstance;
	}
	public AudioStreamPlayer2D? CreateAudio2D(Node ParentNode, Vector2 Position)
	{
		if (!CanPlay()) 
			return null;
		var audioInstance = new AudioStreamPlayer2D();
		ParentNode.AddChild(audioInstance);
		audioInstance.Position = Position;
		audioInstance.Stream = Sound;
		audioInstance.SetVolumeDb(Volume);
		audioInstance.Finished += AudioFinishedPlaying;
		audioInstance.Finished += audioInstance.QueueFree;
		audioInstance.Play();
		return audioInstance;
	}
}
