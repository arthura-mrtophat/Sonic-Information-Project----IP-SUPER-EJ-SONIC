using Godot;
using System;

public partial class Ring : P_Entity
{
	public float MaxSpeed = 4f;
	public float ToPickUp;

	public float UnspawnTimer = 15000;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		base._Ready();
		AnimatedSprite.Frame = GD.RandRange(0, 3);
		AnimatedSprite.Play();
		Area.Monitorable = false;
		ToPickUp = Time.GetTicksMsec()+1000;
		GroundSpeed = (float)GD.RandRange(-MaxSpeed, MaxSpeed);
		AddVelocity = new(0, -Mathf.Abs(GroundSpeed*2)-2);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		var currentTime = Time.GetTicksMsec();
		Area.Monitorable = ToPickUp<currentTime;
		if (ToPickUp+UnspawnTimer < currentTime)
			QueueFree();
		base._Process(delta);
	}
}
