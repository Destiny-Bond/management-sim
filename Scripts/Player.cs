using Godot;
using System.Collections.Generic;

public partial class Player : Node
{
	// --- Debug / cheats ---
	[Export] public bool DevMode = false;   // Toggle at runtime; when ON, SpendEnergy won't drain

	// --- Identity (optional)
	[Export] public string PlayerName = "Coach";

	// --- Training power
	[Export] public float TrainingAbility = 1.6f; // Multiplies training gains globally

	// --- Energy
	[Export] public int MaxEnergy = 100;
	[Export] public int Energy = 100;

	// Optional: signal so UI can react automatically when energy changes
	[Signal] public delegate void EnergyChangedEventHandler(int energy, int maxEnergy);

	// --- Per-skill base gains (tweak to taste)
	// Keys should match your skill keys exactly: "softcore","nude","foreplay","mouth","sex","anal","fetish"
	public Dictionary<string, float> BaseGains = new()
	{
		{ "softcore", 1.2f },
		{ "nude",     1.0f },
		{ "foreplay", 1.0f },
		{ "mouth",    0.9f },
		{ "sex",      0.8f },
		{ "anal",     0.7f },
		{ "fetish",   0.6f }
	};

	public float GetBaseGain(string skill)
		=> BaseGains.TryGetValue(skill, out var g) ? g : 1.0f;

	// --- Energy API ---
	public bool SpendEnergy(int cost)
	{
		if (DevMode) // god mode: no drain, always succeeds
			return true;

		if (Energy < cost)
			return false;

		Energy -= cost;
		EmitSignal(SignalName.EnergyChanged, Energy, MaxEnergy);
		return true;
	}

	public void AddEnergy(int amount)
	{
		if (amount == 0) return;
		Energy = Mathf.Clamp(Energy + amount, 0, MaxEnergy);
		EmitSignal(SignalName.EnergyChanged, Energy, MaxEnergy);
	}

	public void SetEnergy(int value)
	{
		Energy = Mathf.Clamp(value, 0, MaxEnergy);
		EmitSignal(SignalName.EnergyChanged, Energy, MaxEnergy);
	}

	public void ResetEnergyToMax()
	{
		Energy = MaxEnergy;
		EmitSignal(SignalName.EnergyChanged, Energy, MaxEnergy);
	}

	public void ToggleDevMode()
	{
		DevMode = !DevMode;
		GD.Print($"[DEV] DevMode {(DevMode ? "ON" : "OFF")} (energy drain {(DevMode ? "disabled" : "enabled")}).");
	}

	// Optional: quick hotkey toggle (bind an Input Map action named "toggle_dev")
	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("toggle_dev"))
			ToggleDevMode();
	}
}
