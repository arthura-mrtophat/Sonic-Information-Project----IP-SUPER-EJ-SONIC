using Godot;
using System;

public partial class P_Enemy : P_Entity
{
	public float HomePosition = 0f;
	public float SecondsOfNoDamage = 1;
	public float TimeDamageTaken = 0;
	public int Health = 1;
	
	public override void _Ready()
	{
		HomePosition = Position.X;
		base._Ready();
		
		AnimatedSprite.Play();
	}
	public override void _Process(double delta)
	{
		base._Process(delta);
	}

	public override void TakeDamage()
	{
		if (Time.GetTicksMsec() < TimeDamageTaken) return;
		TimeDamageTaken = SecondsOfNoDamage*1000 + Time.GetTicksMsec();
		Health -= 1;
		if (Health <= 0)
		{
			AudioHandler.CreateAudio(SoundEffectType.ExplodeSFX, GlobalPosition);
			EffectHandler.CreateEffect("kaboom", GlobalPosition);
			Dying();
		}
		else
			AudioHandler.CreateAudio(SoundEffectType.EnemyHurtSFX, GlobalPosition);
	}

	public virtual void Dying()
	{
		QueueFree();
	}
}