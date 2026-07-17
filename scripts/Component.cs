using System;
using Godot;

public partial class Component : Node2D
{
    [Export]
    public Pin[] pins { get; private set; }
}
