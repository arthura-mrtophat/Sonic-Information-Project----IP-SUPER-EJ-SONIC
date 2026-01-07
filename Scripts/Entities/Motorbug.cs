using Godot;
using System;

public enum IBugStates
{
	Idle, Wander,
}
public partial class Motorbug : P_Enemy
{
	public float WaitTimer = 0f;
	public float MaxSpeed = 1;
	public float MaxRadiusTravel = 100f;
	public IBugStates State = IBugStates.Idle;
	public new int Health = 1;
	public override void _Process(double delta)
	{
		Wander();
		base._Process(delta);
	}

	public void Wander()
	{
		AnimatedSprite.SetSpeedScale(GroundSpeed/MaxSpeed);
		if (State == IBugStates.Idle)
		{
			GroundSpeed = 0;
			if (Time.GetTicksMsec() >= WaitTimer)
			{
				EntityTurn = -EntityTurn;
				State = IBugStates.Wander;
			}
			return;
		}

		GroundSpeed = MaxSpeed*EntityTurn;
		if (FinishedMoving() || VelocityWallRay() != null)
		{
			WaitTimer = Time.GetTicksMsec() + 2000;
			State = IBugStates.Idle;
		}
	}

	public bool FinishedMoving()
	{
		var wantToPosition = HomePosition + MaxRadiusTravel * EntityTurn;
		if (EntityTurn == -1)
			return Position.X < wantToPosition;
		else
			return Position.X > wantToPosition;
	}
}
