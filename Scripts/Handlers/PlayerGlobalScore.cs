using Godot;
using System;

public static class PlayerGlobalScore
{
	public static float Gravity_Force = 0.21875f;
	
	public static float TimeStarted = 0;
	public static int ScorePoints = 0;
	public static int AMOUNT_OF_SCORE_ZEROS = 8;

	public static void NewTimeStart() => TimeStarted = Time.GetTicksMsec();
	public static float SecondsSinceStart() => (Time.GetTicksMsec() - TimeStarted) / 1000;
	public static string TurnIntoScore(int score)
	{
		string textScore = Convert.ToString(score);
		return new string('0', AMOUNT_OF_SCORE_ZEROS-textScore.Length-2) + textScore + "00";
	}

}
