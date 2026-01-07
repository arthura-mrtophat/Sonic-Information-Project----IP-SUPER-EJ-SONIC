using System;
using Godot;
using Godot.Collections;

namespace sonic.Scripts.Entities;

public enum IPlayerStates
{
	None,
	Idle,
	Walking,
	Running,
	Rolling,
	Jumping,
	LookUp,
	Ducking,
	Spindash,
	IdleWait,
	Spring,
	
	Hurt,
	Died,
	Transferring
}; 

public partial class Player : P_Entity
{
	public IPlayerStates CurrentState = IPlayerStates.None;

	public float TimeStartedState = 0f;

	public float StartWaitAnimation = 5 * 1000;
	
	public float acceleration_speed = 0.046875f;
	public float air_acceleration_speed = 0.046875f / 2;
	public float deceleration_speed = 0.5f;
	public float friction_speed = 0.046875f;
	public float top_speed = 7f;

	public float roll_friction_speed = .0234375f;
	public float roll_deceleration_speed = .125f;

	public float slope_factor_rollup = 0.078125f;
	public float slope_factor_rolldown = 0.3125f;
	public float slope_factor_normal = .125f;

	public float max_jump_hold = .5f * 1000; // for milliseconds
	public float jump_force = 3f;
	public float CurrentJumpForce = 6.5f;

	public float ForceDirection = 0f;

	public UIHandler UiHandler;

	public int Rings { get; private set; } = 0;

	public ItemHandler ItemHandler;
	
	public Dictionary<IPlayerStates, float> SpecialStateSpeed = new()
	{
		[IPlayerStates.Rolling] = .05f,
		[IPlayerStates.Spindash] = 0f,
		[IPlayerStates.Ducking] = 0f,
		[IPlayerStates.LookUp] = 0f,
		[IPlayerStates.Hurt] = 0f,
		[IPlayerStates.Died] = 0f,
		[IPlayerStates.Transferring] = 0f,
	};
	private Dictionary<string, string> _differentAnimation = new()
	{
		["Jumping"] = "Rolling"
	};
	private Array<IPlayerStates> _idles = [IPlayerStates.Idle, IPlayerStates.Walking, IPlayerStates.Running];

	public float InvincibiltyTimer = -1;
	public float TimeOfInvincibility = 5000;

	public ulong InvincibilityFlicker = 512;
	
	private int _spindashBuildUp = 1;
	
	private bool jumpHeld = false;
	private Vector2 jumpingNormal = new();
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		base._Ready();
		ItemHandler = GetNode<ItemHandler>("/root/MainScene/Items");
		AnimatedSprite.Play();
	}
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (CurrentState != IPlayerStates.Died)
		{
			CheckForNextGround();
			P_MovementDirection();
		
			P_CheckForJump();
			P_CheckForDuck();
			HandlerStates();
		
			CheckCollision();
		
			ConvertGravityToSpeed();

			var timer = Time.GetTicksMsec();
			AnimatedSprite.Visible = (timer > InvincibiltyTimer) || timer % InvincibilityFlicker * 2 <= InvincibilityFlicker;
		}
		else DiedActions();
		
		base._Process(delta);
	}

	public void DiedActions()
	{
		CollisionShape.Disabled = true;
	}
	public override void TakeDamage(){
		if (CurrentState == IPlayerStates.Hurt || InvincibiltyTimer > Time.GetTicksMsec())
			return;
		P_ChangeState(Rings > 0 ? IPlayerStates.Hurt : IPlayerStates.Died);
	}

	public bool Bounce()
	{
		if (AddVelocity.Y > 0 && CurrentState == IPlayerStates.Jumping) AddVelocity = new(0, -AddVelocity.Y * 1.1f);
		return CurrentState is IPlayerStates.Jumping or IPlayerStates.Rolling or IPlayerStates.Spindash;
	}

	public void SetUiHandler(UIHandler newUiHandler)
	{
		UiHandler = newUiHandler;
	}
	public void ConvertGravityToSpeed()
	{
		if (AddVelocity.Y <= Gravity_Force*2)
			return;
		var groundRay = GroundRay();
		if (groundRay == null)
			return;
		float angle = (CalculateAngle());
		float absAngle = MathF.Abs(angle);
		if (Mathf.Abs(absAngle) < .3) return;
		float power = absAngle > .8 ? 1 : .5f;
		GroundSpeed = power * AddVelocity.Y * Mathf.Sign(angle);
	}

	public void CheckForNextGround()
	{
		if (GroundAngle < Mathf.Pi/4-.01)
			return;
		if (IsGrounded())
			return;
		float tempAngle = 0;
		var distance = Mathf.Abs(Mathf.Abs(GroundAngle) - tempAngle);
		if  (distance >= Mathf.Pi/2)
			ThrowPlayer();
	}

	private void ThrowPlayer()
	{
		AddVelocity = new(0, GroundSpeed*Mathf.Sign(Mathf.Sin(GroundAngle)));
		GroundSpeed = 0;
	}
	public void CheckCollision()
	{
		var overlappingAreas = Area.GetOverlappingAreas();
		if (overlappingAreas.Count <= 0) return;
		var collidingArea = overlappingAreas[0];
		var collidingNode = (Node2D) collidingArea.GetParent();
		ItemHandler.CheckCollision(collidingNode, this);
	}
	public void HandlerStates()
	{
		if (CurrentState == IPlayerStates.Died)
			return;
		bool isIdling = CurrentState != IPlayerStates.Idle && Convert.ToString(CurrentState).Contains("Idle");
		var absSpeed = Mathf.Abs(GroundSpeed);
		AnimatedSprite.SetSpeedScale(isIdling ? 1 : (absSpeed / 2));
		
		bool grounded = IsGrounded();
		
		switch (CurrentState)
		{
			case IPlayerStates.Jumping:
			case IPlayerStates.Hurt:
				AnimatedSprite.SetSpeedScale(2);
				if (grounded)
					P_ChangeState(IPlayerStates.Idle);
				break;
			case IPlayerStates.Rolling:
				if (!grounded || absSpeed <= deceleration_speed)
					P_ChangeState(IPlayerStates.Jumping);
				break;
			case IPlayerStates.Spindash:
				AnimatedSprite.SetSpeedScale(_spindashBuildUp);
				break;
			case IPlayerStates.Spring when AddVelocity.Y >= 0:
				P_ChangeState(IPlayerStates.Idle);
				break;
		}
		if (CurrentState == IPlayerStates.Hurt)
			return;

		if (!_idles.Contains(CurrentState) && !isIdling)
			return;
		
		if (absSpeed <= friction_speed)
		{
			if (isIdling)
				return;
			P_ChangeState(IPlayerStates.Idle);
			if (Time.GetTicksMsec()-TimeStartedState >= StartWaitAnimation)
				P_ChangeState(IPlayerStates.IdleWait);
		}
		else if (absSpeed <= top_speed / 1.25)
			P_ChangeState(IPlayerStates.Walking);
		else
			P_ChangeState(IPlayerStates.Running);
	}
	public void P_ChangeState(IPlayerStates NewState)
	{
		bool changedToNewState = NewState != CurrentState;
		
		switch (CurrentState)
		{
			case IPlayerStates.Spindash:
				if (changedToNewState)
				{
					GroundSpeed = _spindashBuildUp*EntityTurn*1.5f;
					_spindashBuildUp = 1;
					break;
				}
				_spindashBuildUp = Mathf.Clamp(_spindashBuildUp + 1, 1, 6);
				break;
		}
		
		if (!changedToNewState) return;

		switch (NewState)
		{
			case IPlayerStates.Hurt:
				InvincibiltyTimer = Time.GetTicksMsec() + TimeOfInvincibility;
				AudioHandler.CreateAudio(SoundEffectType.RingLossSFX, GlobalPosition);
				for (int i = 1; i <= Mathf.Clamp(Rings,0,20); i++)
				{
					EffectHandler.CreateEffect("ringdamaged", GlobalPosition);
				}
				Rings = 0;
				AddVelocity = new Vector2(0, -jump_force * 1.5f);
				GroundSpeed = top_speed / 2 * -EntityTurn;
				break;
			case IPlayerStates.Died:
				CollisionShape.Disabled = true;
				AddVelocity = new Vector2(0, -10);
				GroundSpeed = 0;
				AudioHandler.CreateAudio(SoundEffectType.SonicHurtSFX, GlobalPosition);
				break;
			case IPlayerStates.Rolling:
				AudioHandler.CreateAudio(SoundEffectType.SonicSpinSFX, GlobalPosition);
				break;
			case IPlayerStates.Jumping:
				AudioHandler.CreateAudio(SoundEffectType.JumpSFX, GlobalPosition);
				break;
		}
		
		TimeStartedState = Time.GetTicksMsec();
		
		var namedState = Convert.ToString(NewState);
		_differentAnimation.TryGetValue(namedState, out var hasADifferentAnimation);
		namedState = hasADifferentAnimation ?? namedState;
		AnimatedSprite.SetAnimation(namedState);
		CurrentState = NewState;
	}
	
	public void P_MovementDirection()
	{
		float specialSpeed = 1;
		if (SpecialStateSpeed.ContainsKey(CurrentState))
			specialSpeed = SpecialStateSpeed[CurrentState];
		bool isRolling = CurrentState == IPlayerStates.Rolling;
		float absSpeed = Mathf.Abs(GroundSpeed);
		var speedSign = Mathf.Sign(GroundSpeed);
		float rollDirection = isRolling ? speedSign : 0;
		
		float inputDirection = ForceDirection != 0 ? ForceDirection : Input.GetAxis("left", "right");
		if (CurrentState == IPlayerStates.Hurt)
			inputDirection = 0;
		
		float moveDirection =
			inputDirection * specialSpeed + rollDirection;

		
		if (Mathf.Sign(moveDirection) == speedSign && speedSign != 0)
			EntityTurn = MathF.Sign(moveDirection);
		if (absSpeed < top_speed && !isRolling) 
			GroundSpeed += acceleration_speed*moveDirection;
		
		var deceleration = isRolling ? roll_deceleration_speed : deceleration_speed;
		if (Mathf.Sign(inputDirection) != speedSign && inputDirection != 0)
			GroundSpeed -= deceleration * speedSign;
		
		if (!IsGrounded())
			return;

		var friction = isRolling ? roll_friction_speed : friction_speed;

		if (GroundAngle > .05f)
			friction = 0;
		
		if (inputDirection == 0)
		{
			if (absSpeed > friction)
				GroundSpeed -= friction * speedSign;
			else
				GroundSpeed = 0;
		}
		
		ApplySlopiness();
	}
	
	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("jump") && IsGrounded())
		{
			CurrentJumpForce = jump_force;
			P_ChangeState(CurrentState is (IPlayerStates.Ducking or IPlayerStates.Spindash) ? IPlayerStates.Spindash 
				: IPlayerStates.Jumping);
		}
		else if (@event.IsActionReleased("jump"))
			jumpHeld = false;
	}
	public void P_CheckForJump()
	{
		jumpHeld = Input.IsActionPressed("jump");
		if (!jumpHeld)
			return;
		if (Time.GetTicksMsec() - TimeStartedState >= max_jump_hold || CurrentState != IPlayerStates.Jumping)
		{
			jumpHeld = false;
			return;
		}
		jumpingNormal = GetNormal() * CurrentJumpForce;
		CurrentJumpForce *= Gravity_Force*3;

		SensorRotation.Rotation = 0;
		GroundAngle = 0;
		
		AddVelocity += new Vector2(0, jumpingNormal.Y);
		GroundSpeed += Mathf.Abs(jumpingNormal.X) > .1 ? jumpingNormal.X : 0;
	}

	public void P_CheckForDuck()
	{
		bool isDucking = Input.IsActionPressed("down");
		bool isSpindashing = CurrentState == IPlayerStates.Spindash;
		
		if (!isDucking && isSpindashing) P_ChangeState(IPlayerStates.Rolling);
		if (!isDucking || !IsGrounded())
		{
			if (CurrentState is IPlayerStates.Ducking) P_ChangeState(IPlayerStates.Idle);
			return;
		}
		if (isSpindashing)
			return;

		P_ChangeState(Mathf.Abs(GroundSpeed) > .25f ? IPlayerStates.Rolling : IPlayerStates.Ducking);
	}
	public void ApplySlopiness()
	{
		var angleSin = Mathf.Sin(GroundAngle);
		float slopeFactor = slope_factor_normal;
		if (CurrentState == IPlayerStates.Rolling)
		{
			slopeFactor =
				Mathf.Sign(GroundSpeed) != Mathf.Sign(angleSin) ? slope_factor_rollup : slope_factor_rolldown;
		}

		GroundSpeed -= slopeFactor*-angleSin;
	}

	public void AddRings(int Amount)
	{
		AudioHandler.CreateAudio(SoundEffectType.RingSFX, Position);
		Rings += Amount;
	}
}
