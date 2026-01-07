using Godot;
using System;
using sonic.Scripts.Entities;

public partial class CameraController : Camera2D
{
	private const float ChangeOffsetSpeed = .5f;
	private const float MaxOffset = 0;
	private const float MaximumDifferenceX = 10f;
	private const float MaximumDifferenceY = 12f;
	
	private Vector2 previousNodePosition = Vector2.Zero;
	
	[Export]
	public Player FollowPlayer;
	[Export]
	public Node2D MaximumCameraCorner;
	[Export]
	public Node2D MinimumCameraCorner;

	public float MaxCameraSpeed = 30.5f;
	public float CurrentCameraOffset = 0f;

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (FollowPlayer.Position.Y > MaximumCameraCorner.Position.Y)
			FollowPlayer.P_ChangeState(IPlayerStates.Died);
		if (FollowPlayer.CurrentState == IPlayerStates.Died) return;
		FollowPlayer.GlobalPosition =
			FollowPlayer.GlobalPosition.Clamp(MinimumCameraCorner.Position, MaximumCameraCorner.Position);
		var nodeDirection = FollowPlayer.Position - previousNodePosition;
		
		var screenSize = GetViewport().GetVisibleRect().Size;
		var canvasTransform = GetViewport().GetCanvasTransform().AffineInverse();
		
		Vector2 lerpTo = Vector2.Zero;
		
		Vector2 difference = new Vector2(FollowPlayer.Position.X + CurrentCameraOffset * Mathf.Sign(nodeDirection.X), FollowPlayer.Position.Y) - Position;
		
		bool isOnX = Mathf.Abs(difference.X) >= MaximumDifferenceX - CurrentCameraOffset ;
		bool isOnY = Mathf.Abs(difference.Y) >= MaximumDifferenceY ;
		
		if (isOnX)
			lerpTo = new(Mathf.Clamp(difference.X, -MaxCameraSpeed, MaxCameraSpeed) - MaximumDifferenceX * Mathf.Sign(difference.X), lerpTo.Y);
		if (isOnY) 
			lerpTo = new(lerpTo.X, Mathf.Clamp(difference.Y - MaximumDifferenceY * Mathf.Sign(difference.Y), -MaxCameraSpeed, MaxCameraSpeed));
		
		CurrentCameraOffset = Mathf.Clamp(CurrentCameraOffset + ChangeOffsetSpeed * (Mathf.Abs(nodeDirection.X) > 1.2f ? 1 : -5), 0, MaxOffset);
		
		Translate(lerpTo);
		float setX = Position.X;
		float setY = Position.Y;
		
		Vector2 bottomRight = canvasTransform * screenSize;
		Vector2 topLeft = canvasTransform * Vector2.Zero;
		Vector2 DistanceToTopLeft = new(Mathf.Abs(Mathf.Abs(Position.X) - Mathf.Abs(topLeft.X)), Mathf.Abs(Mathf.Abs(Position.Y) - Mathf.Abs(topLeft.Y)));
		Vector2 DistanceToBottomRight = new(Mathf.Abs(Mathf.Abs(Position.X) - Mathf.Abs(bottomRight.X)), Mathf.Abs(Mathf.Abs(Position.Y) + Mathf.Abs(bottomRight.Y)));
		
		previousNodePosition = FollowPlayer.Position;


		LimitLeft = (int)MinimumCameraCorner.Position.X;
		LimitRight = (int)MaximumCameraCorner.Position.X;

		LimitTop = (int)MinimumCameraCorner.Position.Y;
		LimitBottom = (int)MaximumCameraCorner.Position.Y;

		// if (topLeft.X < MinimumCameraCorner.Position.X) setX = MinimumCameraCorner.Position.X + DistanceToTopLeft.X;
		// if (bottomRight.X > MaximumCameraCorner.Position.X) setX = MaximumCameraCorner.Position.X - DistanceToBottomRight.X;
		// if (topLeft.Y < MinimumCameraCorner.Position.Y) setY = MinimumCameraCorner.Position.Y + DistanceToTopLeft.Y;
		// if (bottomRight.Y > MaximumCameraCorner.Position.Y) setY = MaximumCameraCorner.Position.Y - DistanceToBottomRight.Y;
		// Position = new(setX, setY);
	}
}
