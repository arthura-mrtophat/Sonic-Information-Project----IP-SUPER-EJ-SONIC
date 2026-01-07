using Godot;
using System;

public partial class Media : VideoStreamPlayer
{
	// Called when the node enters the scene tree for the first time.
	public async override void _Ready()
	{
		var A = GD.RandRange(1, 100000) == 1 ? "sonic-dancer" : "Sequence-01";
		Finished += OnVideoFinished;
		Stream = GD.Load<VideoStream>($"res://Movies/{A}.ogv");
		Play();
		Vector2 screenSize = GetViewportRect().Size;
		Vector2 textureSize = GetSize();
		Scale = screenSize / textureSize;
	}

	public void OnVideoFinished() {
		
		GetTree().ChangeSceneToFile($"res://Scenes/TitleScreen.tscn");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsAnythingPressed()) OnVideoFinished();
	}
}
