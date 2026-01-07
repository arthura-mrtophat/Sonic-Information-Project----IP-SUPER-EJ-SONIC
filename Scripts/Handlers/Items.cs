using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using sonic.Scripts.Entities;

namespace sonic.Scripts.Handlers;

public struct Items()
{
	private const float SPRING_DEBOUNCE_TIME = 1000;
	
	public interface IItem
	{
		public void Ready(Node2D Node){}
		public void OnTouch(Node2D Node, Player Player){}
		public void Update(Node2D Node){}
		public bool Updatable => false;
	}

	public Dictionary<string, IItem> RealisedItems = new()
	{
		["Ring"] = new Ring(),
		["Spring"] = new Spring(),
		["PlatformFall"] = new PlatformFall(),
		["Monitor"] = new Monitor(),
		["Enemy"] = new Item_Enemy(),
		["Hurt"] = new Hurt(),
		["BossArena"] = new BossArena(),
		["Capsule"] = new Capsule(),
	};

	public class Ring: IItem
	{
		public void Ready(Node2D Node)
		{
			if (Node is not AnimatedSprite2D animatedSprite2D) return;
			animatedSprite2D.Play();
			animatedSprite2D.SetFrame(GD.RandRange(0, 3));
		}

		public void OnTouch(Node2D Node, Player Player)
		{
			Node.QueueFree();
			Player.EffectHandler.CreateEffect("ringeffect", Node.GlobalPosition);
			Player.AddRings(1);
		}
	}
	public class Spring: IItem
	{
		public void Ready(Node2D Node)
		{
			var attributes = Node.GetMetaList();
			if (!attributes.Contains("Power"))
				Node.SetMeta("Power", 5);
			Node.SetMeta("Debounce", 0);
		}

		public void OnTouch(Node2D Node, Player Player)
		{
			var currentTime = Time.GetTicksMsec();
			var debounce = (float)Node.GetMeta("Debounce");
			if (currentTime-debounce < SPRING_DEBOUNCE_TIME) return;
			Player.AudioHandler.CreateAudio(SoundEffectType.SpringSFX, Node.GlobalPosition);
			Node.SetMeta("Debounce",currentTime);
			var power = (float)Node.GetMeta("Power");
			var normal = -Node.GlobalTransform.Y.Normalized();
			Player.AddVelocity = new Vector2(0, normal.Y * power);
			Player.GroundSpeed = normal.X * power;
			Player.P_ChangeState(IPlayerStates.Spring);
		}
	}

	public static float PLATFORMFALL_SPEED = .125f;
	public class PlatformFall: IItem
	{
		public bool Updatable { get; } = true;

		public void Ready(Node2D Node)
		{
			Node.SetMeta("speed", 0);
			Node.SetMeta("time", -1);
		}

		public void OnTouch(Node2D Node, Player Player)
		{
			var deb = (float)Node.GetMeta("time");
			if (deb != -1) return;
			Node.SetMeta("time", Time.GetTicksMsec()+1000);
		}

		public void Update(Node2D Node)
		{
			var deb = (float)Node.GetMeta("time");
			if (deb == -1 || Time.GetTicksMsec() - deb < 0) return;
			float newMeta = (float)Node.GetMeta("speed") + PLATFORMFALL_SPEED;
			Node.SetMeta("speed", newMeta);
			Node.Position += new Vector2(0, newMeta);
		}
	}
	
	public class Monitor: IItem
	{
		public void Ready(Node2D Node){}

		public void OnTouch(Node2D Node, Player Player)
		{
			Player.AudioHandler.CreateAudio(SoundEffectType.ExplodeSFX, Node.GlobalPosition);
			if ((string)Node.GetMeta("ItemType") == "Monitor")
			{
				if (!Player.Bounce()) return;
				Player.AudioHandler.CreateAudio(SoundEffectType.ExplodeSFX, Node.GlobalPosition);
				Player.EffectHandler.CreateEffect("kaboom", Node.GlobalPosition);
				Player.AddRings(10);
			}
			if (Node is AnimatedSprite2D animatedSprite2D)
				animatedSprite2D.Animation = "broke";
			foreach (var child in Node.GetChildren())
				child.QueueFree();
		}
	}
	public class Item_Enemy: IItem
	{
		public void Ready(Node2D Node){}
		public void OnTouch(Node2D Node, Player Player)
		{
			if (Player.Bounce())
				(Node.GetParent().GetNode<P_Enemy>($"{Node.Name}")).TakeDamage();
			else
				Player.TakeDamage();
		}
	}
	public class Hurt: IItem
	{
		public void Ready(Node2D Node){}
		public void OnTouch(Node2D Node, Player Player)=>Player.TakeDamage();
	}

	public class BossArena : IItem
	{
		public void Ready(Node2D Node){}
		public void OnTouch(Node2D Node, Player Player)
		{
			var collisionShape = Node.GetNode<CollisionShape2D>("Area2D/CollisionShape2D");
			var camera = Node.GetNode<CameraController>("/root/MainScene/Camera2D");
			var rectangleShape = collisionShape.Shape as RectangleShape2D;
			camera.MinimumCameraCorner.CreateTween().TweenProperty(camera.MinimumCameraCorner, "position",
				collisionShape.GlobalPosition - rectangleShape.Size / 2, 1);
			camera.MaximumCameraCorner.CreateTween().TweenProperty(camera.MaximumCameraCorner, "position",
				collisionShape.GlobalPosition + rectangleShape.Size / 2, 1);
			Player.UiHandler.BossMusic();
			Node.QueueFree();
		}
	}
	public class Capsule : IItem
	{
		public void Ready(Node2D Node){}
		public async void OnTouch(Node2D Node, Player Player)
		{
			Player.ForceDirection = 1;
			if (Node is P_Entity entity)
			{
				entity.Gravity_Force = 0;
				entity.CollisionShape.Disabled = true;
				entity.AnimatedSprite.Animation = "broken";
			}
			Player.EffectHandler.CreateEffect("kaboom", Node.GlobalPosition+new Vector2(1, -4)*8);
			Player.EffectHandler.CreateEffect("kaboom", Node.GlobalPosition+new Vector2(-1, -3)*8);
			Player.EffectHandler.CreateEffect("kaboom", Node.GlobalPosition+new Vector2(1, -2)*8);
			Player.EffectHandler.CreateEffect("kaboom", Node.GlobalPosition+new Vector2(-1, -1)*8);
			Player.EffectHandler.CreateEffect("kaboom", Node.GlobalPosition);
			Player.AudioHandler.CreateAudio(SoundEffectType.ExplodeSFX, Node.GlobalPosition);
			await Task.Delay(2000);
			Player.UiHandler.Transfer();
		}
	}
}
