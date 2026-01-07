using Godot;
using System;
using sonic.Scripts.Entities;

public partial class UIHandler : Node
{
	public RichTextLabel ScoreText;
	[Export]
	public Player Player;

	public Control StageTitle;
	public ColorRect BlackOut;
	[Export]
	public Sprite2D TrackerRedBand;
	[Export]
	public Sprite2D RedBand;
	[Export]
	public Sprite2D TrackerUnderBand;
	[Export]
	public Sprite2D UnderBand;
	[Export]
	public Sprite2D TrackerBackgroundBand;
	[Export]
	public Sprite2D BackgroundBand;

	public float PositionRed = 0; // this sucks
	public float PositionUnder = 0;
	public int PixelsToLoop = 16;

	public bool Transferring;
	public float SecondsOfMaxIdle = 30;
	public AudioStreamPlayer Music;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Player.SetProcess(false);
		Player.SetUiHandler(this);
		StageTitle = GetNode<Control>("StageTitle");
		BlackOut = StageTitle.GetNode<ColorRect>("ColorRect");
		var zoneName = StageTitle.GetNode<Node2D>("ZoneText");
		ScoreText = GetNode<RichTextLabel>("ScoreText");
		
		Node2D[] allSprites = [TrackerRedBand, TrackerUnderBand, TrackerBackgroundBand, zoneName];
		Tween[] allTweens = [CreateTween(), CreateTween(), CreateTween(), CreateTween()];
		
		Music = Player.AudioHandler.CreateAudio(SoundEffectType.MUSIC_Zone1)!;
		
		for (int i = 0; i <= 3; i++)
		{
			float offset = 1920 * Mathf.Sign(((i * 2) - 1));
			Vector2 actualOffset = new Vector2(offset, 0);
			var sprite = allSprites[i];
			sprite.Position += actualOffset;
			var index = (float)i;
			var tween = allTweens[i];
			tween.TweenInterval(1);
			tween.TweenInterval(.6+index/100);
			tween.TweenProperty(sprite, "position", sprite.Position-actualOffset, .55+index/100);
			tween.TweenInterval(2);
			if (i == 1)
			{
				tween.TweenProperty(BlackOut, "modulate:a", 0, 0.1);
				tween.Finished += () =>
				{
					Player.P_ChangeState(IPlayerStates.Idle);
					Player.SetProcess(true);
					PlayerGlobalScore.NewTimeStart();
				};
			}
			tween.TweenProperty(sprite, "position", sprite.Position+actualOffset, .25);
		}
	

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		BackgroundBand.Position = TrackerBackgroundBand.Position;
		RedBand.Position = TrackerRedBand.Position + new Vector2(0, PositionRed);
		UnderBand.Position = TrackerUnderBand.Position - new Vector2(PositionUnder, 0);
		var speed = (float)delta * RedBand.Scale.X * 3;
		PositionRed = (PositionRed + speed) % (PixelsToLoop*RedBand.Scale.X);
		PositionUnder = (PositionUnder + speed) % (PixelsToLoop*UnderBand.Scale.X);
		ScoreText.Text = 
			$"SCORE: [color=\"FFFFFF\"]{PlayerGlobalScore.TurnIntoScore(PlayerGlobalScore.ScorePoints)}[br][/color]" +
			$"TIME: [color=\"FFFFFF\"]{NumberToTime(PlayerGlobalScore.SecondsSinceStart())}[/color][br]" +
			$"RINGS: [color=\"FFFFFF\"]{Player.Rings}[/color]";
		CheckIfStandsALongTime();
	}

	public async void ShutMusic()
	{
		var tween = CreateTween();
		tween.TweenProperty(Music, "volume_db", -80.0f, 3);
		await ToSignal(tween, Tween.SignalName.Finished);
		Music.QueueFree();
	}
	public void BossMusic()
	{
		var boss = GetNode<Eggman>("/root/MainScene/Items/Boss");
		boss.AddVelocity = new Vector2(0, 1);
		if (Music != null)
			Music.QueueFree();
		Music = Player.AudioHandler.CreateAudio(SoundEffectType.MUSIC_Boss)!;
		boss.TreeExited += EndLevel;
	}

	public void FasterMusic()
	{
		Music.SetPitchScale(1.2f);
	}

	public void EndLevel()
	{
		ShutMusic();
		CreateTween().TweenProperty(ScoreText, "position:x", -1920, 1);
		// var tween = CreateTween();
		// tween.TweenProperty(GetNode<RichTextLabel>("EndText"), "position:y", 0, 6);
		// tween.Finished += Transfer;
	}
	
	public string NumberToTime(float time)
	{
		var minutes = (int)(time / 60);
		var seconds = (int)(time % 60);
		return string.Format("{0}:{1:D2}", minutes, seconds);
	}

	public void CheckIfStandsALongTime()
	{
		if (Player.CurrentState == IPlayerStates.Died) Transfer();
		if (Player.CurrentState != IPlayerStates.IdleWait || Time.GetTicksMsec() - Player.TimeStartedState < SecondsOfMaxIdle*1000)
			return;
		Player.P_ChangeState(IPlayerStates.Transferring);
		Transfer();
	}

	public void Transfer()
	{
		if (Transferring) return;
		Transferring = true;
		ShutMusic();
		var tween = CreateTween();
		tween.TweenProperty(BlackOut, "modulate:a", 1, 3);
		tween.Finished += ()=>GetTree().ChangeSceneToFile("res://Scenes/TitleScreen.tscn");
	}
}
