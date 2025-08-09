using Godot;
using System.Collections.Generic;

public partial class GameSession : Node
{
	// --- Live game state (persists across scene changes)
	public Dictionary<string, Prostitute> Roster { get; private set; } = new();
	public string CurrentProstituteName { get; private set; } = null;

	// Optional: keep a global Player reference here so scenes can grab it
	public Player Player { get; set; }

	// Signal if you want UI to react when selection changes
	[Signal] public delegate void CurrentProstituteChangedEventHandler(string name);

	public Prostitute CurrentProstitute
	{
		get
		{
			if (CurrentProstituteName != null && Roster.TryGetValue(CurrentProstituteName, out var p))
				return p;
			return null;
		}
		set
		{
			if (value == null) return;
			Roster[value.Name] = value;
			CurrentProstituteName = value.Name;

			// emit the name (variant-friendly)
			EmitSignal(SignalName.CurrentProstituteChanged, CurrentProstituteName);
		}
	}

	public override void _Ready()
	{
		// Try to load a previous save; if nothing exists, we just continue fresh
		bool loaded = false;
		try
		{
			loaded = SaveSystem.LoadGame(this);
		}
		catch (System.Exception e)
		{
			GD.PrintErr("[GameSession] Load failed: ", e);
		}

		if (!loaded)
			GD.Print("[GameSession] No save found; starting new session.");
	}

	// --- Roster helpers ------------------------------------------------------

	public void AddOrUpdateProstitute(Prostitute p)
	{
		if (p == null || string.IsNullOrEmpty(p.Name)) return;
		Roster[p.Name] = p;
	}

	public bool TryGetProstitute(string name, out Prostitute p)
		=> Roster.TryGetValue(name, out p);

	public void SetCurrentByName(string name)
	{
		if (string.IsNullOrEmpty(name)) return;
		if (Roster.TryGetValue(name, out var p))
			CurrentProstitute = p;
	}

	// --- Player helpers (optional) ------------------------------------------

	public void EnsurePlayerFrom(NodePath path)
	{
		if (Player != null) return;
		if (path == null || path.IsEmpty) return;
		Player = GetNodeOrNull<Player>(path);
	}

	// --- Save/Load wrappers --------------------------------------------------

	public bool Save()
	{
		try
		{
			return SaveSystem.SaveGame(this);
		}
		catch (System.Exception e)
		{
			GD.PrintErr("[GameSession] Save failed: ", e);
			return false;
		}
	}

	public bool Load()
	{
		try
		{
			return SaveSystem.LoadGame(this);
		}
		catch (System.Exception e)
		{
			GD.PrintErr("[GameSession] Load failed: ", e);
			return false;
		}
	}
}
