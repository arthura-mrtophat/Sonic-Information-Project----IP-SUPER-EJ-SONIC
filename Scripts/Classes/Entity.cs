using Godot;
using System;
using Godot.Collections;
using sonic.Scripts.Handlers;

//Apparently godot's coordinate system is swapped for Y LOL LOL LOL !!!!
public partial class P_Entity : AnimatedSprite2D
{
	public uint GroundLayer = 3;
	public Vector2 CollisionSize = new(0, 0);

	public AudioHandler AudioHandler;
	public EffectHandler EffectHandler;

	private RectangleShape2D RectangleShape;
	public CollisionShape2D CollisionShape;
	public Area2D Area;
	public AnimatedSprite2D AnimatedSprite;

	public Node2D SensorRotation;

	public bool LockRotation = false;
	
	public int EntityTurn = 1;
	
	public float GroundSpeed = 0f;
	public float GroundAngle = 0f;
	public Vector2 Velocity = new();
	public Vector2 AddVelocity = new();

	public float Gravity_Force = PlayerGlobalScore.Gravity_Force;
	public float SensorSnapAngle = Mathf.Pi/6;
	public float SpriteAngleChange = 0.75f;

	public Vector2 LimitAddVelocity = new Vector2(0, PlayerGlobalScore.Gravity_Force * 100);

	private int lastDirectionWall;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		AudioHandler = GetNode<AudioHandler>("/root/MainScene/AudioHandler");
		EffectHandler = GetNode<EffectHandler>("/root/MainScene/EffectHandler");
		
		Area = GetNode<Area2D>("Area");
		CollisionShape = Area.GetNode<CollisionShape2D>("Collision");
		SensorRotation = GetNode<Node2D>("SensorRotation");
		AnimatedSprite = this;

		RectangleShape = CollisionShape.Shape as RectangleShape2D;
		CollisionSize = RectangleShape!.Size * Scale.X;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		ApplyGravity();
		
		UpdateAngle();
		
		AnimatedSprite.SetFlipH(EntityTurn != 1);

		
		Velocity = TransformGroundSpeedToNormal() + AddVelocity;
		CheckForWalls();
		AddPush();
		GlobalPosition += Velocity;
		GlobalPosition = GlobalPosition.Clamp(new Vector2(0, 0), new Vector2(32768, 32768));
	}
	
	public virtual void TakeDamage() => GD.Print("Undefined");

	public void CheckForWalls()
	{
		var wallRay = VelocityWallRay();
		if (wallRay == null) return;
		var distance = Mathf.Abs((float)wallRay["distance"]);
		GroundSpeed = 0;
		Velocity -= new Vector2(distance * Mathf.Sign(Velocity.X), 0);
	}
	private float SnapAngle(float Angle, float SnapAmount)
	{
		return Mathf.Floor(Angle / SnapAmount) * SnapAmount;
	}
	public float CalculateAngle() => GetNormal().Angle() + Mathf.Pi/2;
	private void UpdateAngle()
	{
		GroundAngle = CalculateAngle();
		
		SensorRotation.Rotation = LockRotation ? 0 : GroundAngle;

		if (Time.GetTicksMsec() % 3 == 0)
			Rotation = Mathf.Lerp(Rotation, SensorRotation.Rotation, SpriteAngleChange);
	}
	private void ApplyGravity()
	{
		if (!IsGrounded())
			AddVelocity = (AddVelocity + new Vector2(0, Gravity_Force)).Clamp(-LimitAddVelocity, LimitAddVelocity);
		else
			AddVelocity = new Vector2();
	}

	private void AddPush()
	{
		var groundRay = GroundRay();
		if (groundRay == null) return;
		float currentDistance = (float)groundRay["distance"];
		float distanceCalculation = currentDistance - CollisionSize.Y / 2;
		Translate(Vector2.Down*distanceCalculation);
	}

	private Vector2 TransformGroundSpeedToNormal()
		=> new (Mathf.Cos(GroundAngle) * GroundSpeed, Mathf.Sin(GroundAngle) * GroundSpeed);
	
	public void ChangeColliderSize(Vector2 NewSize)
	{
		RectangleShape.SetSize(NewSize);
		CollisionSize = NewSize;
	}

	public Vector2 GetNormal()
	{
		var ray = GroundRay();
		if (ray == null)
			return Vector2.Up;
		var normal = (Vector2)ray["normal"];
		return normal;
	}
	public bool IsGrounded()
		=> GroundRay() != null;

	public Dictionary? VelocityWallRay()
	{
		var signSpeed = Mathf.Sign(GroundSpeed);
		var direction = lastDirectionWall;
		if (signSpeed != 0)
		{
			direction = signSpeed;
			lastDirectionWall = signSpeed;
		}
		return WallRay(direction);
	}
	public Dictionary? WallRay(float? CustomDirection)
	{
		float sizeDirection = (CollisionSize.X / 2 + 4);
		var directionRay = SensorRotation.GlobalTransform.X.Normalized() * (CustomDirection ?? Mathf.Sign(GroundSpeed)) * sizeDirection;
		
		var ray = rayAtPosition(CollisionShape.GlobalPosition, directionRay);
		if (ray != null) 
			ray["distance"] = (sizeDirection-1) - (float)ray["distance"];
		return ray;
	}
	public Dictionary? GroundRay()
	{
		if (AddVelocity.Y < 0)
			return null;

		Vector2 sizeDown = SensorRotation.GlobalTransform.Y.Normalized() * (CollisionSize.Y / 2 + 1f);
		Vector2 xShift = SensorRotation.GlobalTransform.X.Normalized() * (CollisionSize.X / 2);
		Dictionary ray = null;
		float lastDistance = -1f;

		for (int i = 0; i<=1; i++)
		{
			var rayPosition = CollisionShape.GlobalPosition + xShift * (1 - 2 * i);
			var checkGroundRay = rayAtPosition(rayPosition, Direction: sizeDown);
			if (checkGroundRay == null)
				continue;
			var currentDistance = (float)checkGroundRay["distance"];
			if (lastDistance < currentDistance && lastDistance != -1)
				continue;
			lastDistance = currentDistance;
			ray = checkGroundRay;
		}

		return ray;
	}
	private Dictionary? rayAtPosition(Vector2 Position, Vector2 Direction)
	{
		if (CollisionShape.Disabled) return null;
		var spaceState = PhysicsServer2D.SpaceGetDirectState(GetWorld2D().Space);
		PhysicsRayQueryParameters2D ray = new()
		{
			From = Position,
			To = Position+Direction,
			Exclude = [Area.GetRid()],
			CollisionMask = 3,
		};
		var intersection = spaceState.IntersectRay(ray);
		if (intersection.Count > 0)
		{
			intersection.Add("distance", (Position - (Vector2)intersection["position"]).Length());
			intersection.Add("generalDistance", Position.Length());
			intersection.Add("direction", Direction);
		}
		else
		{
			intersection = null;
		}
		return intersection;
	}
}
