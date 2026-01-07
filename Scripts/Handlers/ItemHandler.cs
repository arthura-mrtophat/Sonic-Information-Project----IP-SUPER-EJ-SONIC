using Godot;
using System;
using System.Linq;
using Godot.Collections;
using sonic.Scripts.Entities;
using sonic.Scripts.Handlers;

public partial class ItemHandler: Node2D
{
	private static Items HolderItems = new();
	public Node2D[] ActiveItems = [];
	public Node2D[] UpdateItems = [];
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		var children = GetChildren();
		foreach (var e in children)
		{
			var node = (Node2D)e;
			var (success, item) = checkNodeForItem(node);
			if (!success) continue;
			item.Ready(node);
			ActiveItems = ActiveItems.Append(node).ToArray();
			if (item.Updatable)
				UpdateItems = UpdateItems.Append(node).ToArray();
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		foreach (var node in UpdateItems)
		{
			var (success, item) = checkNodeForItem(node);
			if (!success) continue;
			item.Update(node);
		}
	}

	public void CheckCollision(Node2D Node, Player Player)
	{
		var (success, item) = checkNodeForItem(Node);
		if (!success)
			return;
		item.OnTouch(Node, Player);
	}
	private (bool, Items.IItem) checkNodeForItem(Node2D Node)
	{
		string itemType = (string)Node.GetMeta("ItemType");
		var success = HolderItems.RealisedItems.TryGetValue(itemType, out var item);
		return (success, item);
	}
}
