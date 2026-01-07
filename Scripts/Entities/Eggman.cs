using Godot;
using System;
using sonic.Scripts.Entities;

public enum IEggmanStates
{
	Descending,
	Attacker,
	Dying,
}

public partial class Eggman : P_Enemy
{
	[Export] public Player Player;
	public float MaxRadiusTravel = 125;
	public IEggmanStates EggmanState;
	
	public float WaitTimer = 0f;
	public float MaxSpeed = 2;
	
	public Sprite2D WreckingBall;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Gravity_Force = 0;
		Health = 5;
		base._Ready();
		AnimatedSprite.Play();
		AnimatedSprite.Animation = "default";
		WreckingBall = GetNode<Sprite2D>("WreckingBall");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		base._Process(delta);
		var currentTime = (float)Time.GetTicksMsec();
		var randomPosition = new Vector2(GD.RandRange(-2, 2), GD.RandRange(-2, 2))*10;
		if (EggmanState == IEggmanStates.Dying)
		{
			Area.Monitorable = false;
			AnimatedSprite.Animation = "Hurt";
			var beforeFleeing = currentTime < TimeDamageTaken;
			GroundSpeed = beforeFleeing ? 0: 2.5f;
			var thisThing = beforeFleeing ? 3 : 16;
			EntityTurn = -1;
			if (currentTime % thisThing == 0)
			{
				AudioHandler.CreateAudio(SoundEffectType.EnemyHurtSFX, GlobalPosition);
				EffectHandler.CreateEffect("kaboom", GlobalPosition + randomPosition);
			}
			if (Position.X > 8930)
			{
				EffectHandler.CreateEffect("capsule", new(8529.0f, 1008.75f));
				QueueFree();
			}
			return;
		}

		Area.Monitorable = currentTime >= TimeDamageTaken;
		AnimatedSprite.Animation = currentTime < TimeDamageTaken ? "Hurt" : "default";
		//Gravity_Force = Mathf.Cos(currentTime) / 100;

		if (Position.Y > 1235)
		{
			AddVelocity = Vector2.Zero;
			EggmanState = IEggmanStates.Attacker;
		}

		var power = Mathf.Pi / 3;
		var speed = 400;
		if (Health <= 2)
		{
			if (currentTime % 64 == 0)
			{
				AudioHandler.CreateAudio(SoundEffectType.EnemyHurtSFX, GlobalPosition);
				EffectHandler.CreateEffect("kaboom", GlobalPosition + randomPosition);
			}

			AnimatedSprite.Modulate = new(1, .5f, .5f, 1);
			power = Mathf.Pi / 2;
			speed = 120;
		}
		WreckingBall.Rotation = Mathf.Sin(currentTime / speed) * power;
		
		if (EggmanState != IEggmanStates.Attacker) return;

		GroundSpeed = currentTime  >= WaitTimer ? MaxSpeed*-EntityTurn : 0;
		if (FinishedMoving() || VelocityWallRay() != null)
		{
			WaitTimer = Time.GetTicksMsec() + 700;
			EntityTurn = -EntityTurn;
		}
		
	}

	public override void Dying()
	{
		EggmanState = IEggmanStates.Dying;
		TimeDamageTaken = Time.GetTicksMsec()+2500;
		WreckingBall.QueueFree();
	}
	public override void TakeDamage()
	{
		if (Time.GetTicksMsec() < TimeDamageTaken) return;
		base.TakeDamage();
		if (Health <= 2)
			Player.UiHandler.FasterMusic();
		Player.GroundSpeed = -Player.GroundSpeed;
		Player.AddVelocity = -Player.AddVelocity;
	}
	
	public bool FinishedMoving()
	{
		var wantToPosition = HomePosition + MaxRadiusTravel * -EntityTurn;
		if (EntityTurn == 1)
			return Position.X < wantToPosition;
		else
			return Position.X > wantToPosition;
	}
}
