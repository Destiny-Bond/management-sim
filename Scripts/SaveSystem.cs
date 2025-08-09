using Godot;
using System.Collections.Generic;

public static class SaveSystem
{
	private const string SAVE_PATH = "user://save.json";

	public static bool SaveGame(GameSession session)
	{
		if (session == null) return false;

		var root = new Godot.Collections.Dictionary();

		// Roster -> Array<Dictionary>
		var prosArr = new Godot.Collections.Array();
		foreach (var kv in session.Roster)
		{
			var st = kv.Value.ToState();            // Prostitute.ToState()
			prosArr.Add(FromState(st));             // -> Godot dict
		}
		root["Prostitutes"] = prosArr;

		// Current selection
		root["Current"] = session.CurrentProstituteName ?? "";

		// Player snapshot (optional)
		if (session.Player != null)
		{
			root["PlayerEnergy"]          = session.Player.Energy;
			root["PlayerMaxEnergy"]       = session.Player.MaxEnergy;
			root["PlayerTrainingAbility"] = session.Player.TrainingAbility;
			root["DevMode"]               = session.Player.DevMode;
		}

		var json = Json.Stringify(root, "\t");
		using var f = FileAccess.Open(SAVE_PATH, FileAccess.ModeFlags.Write);
		if (f == null) return false;
		f.StoreString(json);
		GD.Print("[Save] Wrote ", SAVE_PATH);
		return true;
	}

	public static bool LoadGame(GameSession session)
	{
		if (session == null) return false;
		if (!FileAccess.FileExists(SAVE_PATH)) return false;

		using var f = FileAccess.Open(SAVE_PATH, FileAccess.ModeFlags.Read);
		if (f == null) return false;

		var text = f.GetAsText();
		Variant parsed = Json.ParseString(text);
		if (parsed.VariantType != Variant.Type.Dictionary)
		{
			GD.PrintErr("[Save] Invalid save format.");
			return false;
		}

		var root = (Godot.Collections.Dictionary)parsed;

		// --- Roster ---
		session.Roster.Clear();
		var prosArr = Get(root, "Prostitutes", new Godot.Collections.Array());

		// Enumerate as Variant and cast after checking type
		foreach (Variant item in prosArr)
		{
			if (item.VariantType == Variant.Type.Dictionary)
			{
				var d  = (Godot.Collections.Dictionary)item;
				var st = ToState(d);                // Godot dict -> ProstituteState
				var p  = Prostitute.FromState(st);  // -> Prostitute
				if (p != null) session.Roster[p.Name] = p;
			}
		}

		// --- Current selection ---
		var currentName = Get(root, "Current", "");
		if (!string.IsNullOrEmpty(currentName))
		{
			// Use GameSession's API instead of setting a private setter
			session.SetCurrentByName(currentName);
		}

		// --- Player snapshot (optional) ---
		if (session.Player != null)
		{
			session.Player.Energy            = Get(root, "PlayerEnergy", session.Player.Energy);
			session.Player.MaxEnergy         = Get(root, "PlayerMaxEnergy", session.Player.MaxEnergy);
			session.Player.TrainingAbility   = Get(root, "PlayerTrainingAbility", session.Player.TrainingAbility);
			session.Player.DevMode           = Get(root, "DevMode", session.Player.DevMode);
		}

		GD.Print("[Save] Loaded ", SAVE_PATH);
		return true;
	}

	public static bool DeleteSave()
	{
		if (!FileAccess.FileExists(SAVE_PATH)) return true;
		var err = DirAccess.RemoveAbsolute(SAVE_PATH);
		if (err == Error.Ok)
		{
			GD.Print("[Save] Deleted ", SAVE_PATH);
			return true;
		}
		GD.PrintErr("[Save] Delete failed: ", err);
		return false;
	}

	// ---------- helpers ----------

	// ProstituteState -> Godot dict
	private static Godot.Collections.Dictionary FromState(ProstituteState s)
	{
		var stats = new Godot.Collections.Dictionary();
		if (s.Stats != null)
			foreach (var kv in s.Stats) stats[kv.Key] = kv.Value;

		return new Godot.Collections.Dictionary
		{
			["Name"]         = s.Name,
			["Profession"]   = s.Profession,               // enum stored as string
			["PortraitPath"] = s.PortraitPath ?? "",
			["Stats"]        = stats
		};
	}

	// Godot dict -> ProstituteState
	private static ProstituteState ToState(Godot.Collections.Dictionary d)
	{
		var st = new ProstituteState
		{
			Name         = Get(d, "Name", ""),
			Profession   = Get(d, "Profession", "Unassigned"),
			PortraitPath = Get(d, "PortraitPath", "")
		};

		var statsIn = Get(d, "Stats", new Godot.Collections.Dictionary());
		st.Stats = new Dictionary<string, int>();
		foreach (var key in statsIn.Keys)
			st.Stats[key.ToString()] = (int)statsIn[key];

		return st;
	}

	// typed getter for Godot dictionaries
	private static T Get<[MustBeVariant] T>(Godot.Collections.Dictionary d, string key, T def)
	{
		if (d == null || !d.ContainsKey(key)) 
			return def;

		Variant v = (Variant)d[key];
		if (v.VariantType == Variant.Type.Nil) 
			return def;

		try
		{
			return v.As<T>(); // Godot 4.4 safe conversion
		}
		catch
		{
			GD.PrintErr($"[SaveSystem] Could not convert key '{key}' to type {typeof(T).Name}, returning default.");
			return def;
		}
	}
}
