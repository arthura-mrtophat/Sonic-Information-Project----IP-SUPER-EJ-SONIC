using Godot;
using System;
using System.Collections.Generic;

public partial class AudioHandler : Node
{
	public Dictionary<SoundEffectType, SoundEffect> ActiveSounds = new();
	[Export] public SoundEffect[] SoundEffects = [];
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		foreach (var sound in SoundEffects)
			ActiveSounds[sound.SoundType] = sound;
	}
	public AudioStreamPlayer? CreateAudio(SoundEffectType SoundType)
	{
		if (!ActiveSounds.ContainsKey(SoundType))
			return null;
		return ActiveSounds[SoundType].CreateAudio2D(this);
	}
	public AudioStreamPlayer2D? CreateAudio(SoundEffectType SoundType, Vector2 Position)
	{
		if (!ActiveSounds.ContainsKey(SoundType))
			return null; 
		return ActiveSounds[SoundType].CreateAudio2D(this, Position);
	}
}
