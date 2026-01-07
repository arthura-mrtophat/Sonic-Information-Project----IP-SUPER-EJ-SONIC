using System.Collections.Generic;
using Godot;

namespace sonic.Scripts.Handlers;

public partial class EffectHandler: Node
{
    [Export] public PackedScene[] Assets;
    public Dictionary<string, PackedScene> LoadedEffects = new(); 
    
    public override void _Ready()
    {
        foreach (var scene in Assets)
        {
            var name = System.IO.Path.GetFileNameWithoutExtension(scene.ResourcePath);
            LoadedEffects[name] = scene;
        }
    }

    public void CreateEffect(string EffectName, Vector2 Position)
    {
        bool success = LoadedEffects.TryGetValue(EffectName, out var effect);
        if (!success)
            return;
        var newEffect = effect.Instantiate<AnimatedSprite2D>();
        newEffect.Position = Position;
        GetTree().GetCurrentScene().AddChild(newEffect);
    }
}