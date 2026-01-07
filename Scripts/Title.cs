using Godot;
using System;

public partial class Title : Node
{
	[ExportGroup("Blackout")]
	[Export] public ColorRect Blackout;
	[Export] public ColorRect Whiteout;
	[Export] public RichTextLabel CreditsText;

	[ExportGroup("Title Screen")] 
	[Export] public Sprite2D PressStart;
	[Export] public Sprite2D Background;
	[Export] public Sprite2D Emblem;
	[Export] public Sprite2D Sonic;
	[Export] public Sprite2D SonicHand;
	[Export] public Sprite2D Rubber;
	[Export] public Sprite2D PositionTracker_Emblem;

	public Vector2 EmblemPosition;
	public Vector2 EmblemWantPosition;
	public Vector2 Offset_Rubber = new(0, 55);
	public Vector2 Offset_Sonic = new(0, 0);
	public Vector2 Offset_HandSonic = new(18.4f, 19.6f);

	public float Snapiness = 3f;
	public float DecimalSaveSnap = 100;
	public float FallBehindEmblem = .25f;

	public float TitleStarted = 0;
	public float MusicStarted = 0;
	public float MaxWaitTimer = 37;
	public Vector2 EmblemVelocity;
	public float Speed = 1f;
	public int StartFlickerPeriod = 1024;

	public AudioHandler AudioHandler;

	public Sprite2D QRCode;

	private bool _skipping;
	
	// Called when the node enters the scene tree for the first time.
	public override async void _Ready()
	{
		AudioHandler = GetNode<AudioHandler>("/root/TitleScreen/AudioHandler");
		PressStart.Visible = false;
		MusicStarted = Time.GetTicksMsec();
		TitleStarted = Time.GetTicksMsec();

		QRCode = GetNode<Sprite2D>("ActualTitle/QRCode");
		
		EmblemWantPosition = Emblem.Position;
		PositionTracker_Emblem.Position = EmblemWantPosition + new Vector2(125, 300);
		var tween1 = CreateTween();
		CreditsText.Modulate = new Color(1, 1, 1, 0);
		tween1.TweenProperty(SonicHand, "modulate:a", 0, 0);
		tween1.TweenInterval(1);
		tween1.TweenProperty(CreditsText, "modulate:a", 1, 0f);
		tween1.TweenInterval(1);
		tween1.TweenProperty(CreditsText, "modulate:a", 0, 1f);
		tween1.TweenInterval(.5f);
		tween1.TweenProperty(Blackout, "modulate:a", 0, .5f);
		tween1.TweenInterval(.15f);
		tween1.SetTrans(Tween.TransitionType.Back);
		tween1.TweenProperty(PositionTracker_Emblem, "position", EmblemWantPosition, 2);
		tween1.SetTrans(Tween.TransitionType.Linear);
		tween1.TweenProperty(Whiteout, "modulate:a", 1, 0);
		tween1.TweenProperty(SonicHand, "modulate:a", 1, 0);
		tween1.TweenProperty(Sonic, "frame", 1, 0);
		tween1.TweenProperty(Whiteout, "modulate:a", 0, .75);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		EmblemPosition = EmblemPosition.Lerp(PositionTracker_Emblem.Position, FallBehindEmblem);
		float millisecondsSinceStart = Time.GetTicksMsec() - TitleStarted;
		float currentTimer = (millisecondsSinceStart) / 1000;
		float musicTimer = (Time.GetTicksMsec() - TitleStarted) / 1000;
		Emblem.Position = EmblemPosition + new Vector2(Mathf.Cos(currentTimer/2)*3f, Mathf.Sin(currentTimer*2) * 2);
		SonicHand.Rotation = SnapFloat(Mathf.Cos(currentTimer * 3) / 5);
		Background.Position -= new Vector2((float)delta, 0);
		
		if (currentTimer >= 5.15f)
			PlayMusic();
			
		if (currentTimer <= 6f)
			return;
		QRCode.Visible = true;
		if (musicTimer >= MaxWaitTimer)
			GoAway(true);
		else if(Input.IsActionPressed("jump") || Input.IsActionPressed("start")) 
			GoAway(false);
		PressStart.Visible = millisecondsSinceStart % StartFlickerPeriod * 2 <= StartFlickerPeriod;
		// EmblemVelocity += (PositionTracker_Emblem.Position - EmblemWantPosition).Normalized();
		// PositionTracker_Emblem.Position += EmblemPosition;
	}

	private bool musicPlaying;
	public void PlayMusic()
	{
		if (musicPlaying)
			return;
		musicPlaying = true;
		MusicStarted = Time.GetTicksMsec();
		AudioHandler.CreateAudio(SoundEffectType.MUSIC_Title);
	}
	public async void GoAway(bool Cutscene) {
		if (_skipping)
			return;
		_skipping = true;
		if (!Cutscene)
			AudioHandler.CreateAudio(SoundEffectType.MenuAcceptSFX);
		var tween = CreateTween();
		tween.TweenProperty(Blackout, "modulate:a", 1, 1);
		tween.TweenInterval(1.5f);
		await ToSignal(tween, Tween.SignalName.Finished);
		var whatScene = Cutscene ? "Movie" : "Test";
		GetTree().ChangeSceneToFile($"res://Scenes/{whatScene}.tscn");
	}
	public float SnapFloat(float toSnap)
		=> Mathf.Round(toSnap / Snapiness * DecimalSaveSnap) * Snapiness / DecimalSaveSnap;
}
