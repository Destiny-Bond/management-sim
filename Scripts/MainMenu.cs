using Godot;
using System;

public partial class MainMenu : Control
{
	public override void _Ready()
	{
		GetNode<Button>("VBoxContainer/StartButton").Pressed += OnStartPressed;
		GetNode<Button>("VBoxContainer/LoadButton").Pressed += OnLoadPressed;
		GetNode<Button>("VBoxContainer/OptionsButton").Pressed += OnOptionsPressed;
		GetNode<Button>("VBoxContainer/QuitButton").Pressed += OnQuitPressed;
	}

	private void OnStartPressed()
	{
		GD.Print("Starting game...");
		GetTree().ChangeSceneToFile("res://scenes/prostitute_select.tscn"); // Placeholder
	}

	private void OnLoadPressed()
	{
		GD.Print("Load Game - not implemented yet.");
	}

	private void OnOptionsPressed()
	{
		GD.Print("Options - not implemented yet.");
	}

	private void OnQuitPressed()
	{
		GetTree().Quit();
	}
}
