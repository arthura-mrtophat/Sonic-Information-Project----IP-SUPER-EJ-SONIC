using Godot;
using System;
using System.Threading.Tasks;

public partial class Debris : P_Entity
{
	// Called when the node enters the scene tree for the first time.
	public async override void _Ready()
	{
		base._Ready();
		GroundSpeed = GD.RandRange(-1, 1)*3;
		AddVelocity = new Vector2(0, 10);
		await Task.Delay(5000);
		QueueFree();
	}
}
