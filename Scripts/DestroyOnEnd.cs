using Godot;
using System;

public partial class DestroyOnEnd : AnimatedSprite2D
{
	public override void _Ready()
	{
		Frame = 0;
		Play();
		AnimationFinished += QueueFree;
	}
}
