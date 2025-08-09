public class Prostitute
{
	public string Name;
	public ProfessionType Profession;
	public string PortraitPath;

	public int MaxEnergy = 100;
	public int Energy = 100;

	private System.Collections.Generic.Dictionary<string, int> stats;

	public Prostitute(string name, ProfessionType profession = ProfessionType.Unassigned, string portraitPath = null)
	{
		Name = name;
		Profession = profession;
		PortraitPath = portraitPath ?? $"res://art/portraits/{name}.png";

		stats = new()
		{
			{ "softcore", 0 }, { "nude", 0 }, { "foreplay", 0 },
			{ "mouth", 0 }, { "sex", 0 }, { "anal", 0 }, { "fetish", 0 }
		};
	}

	public int GetSkill(string key) => stats.TryGetValue(key, out var v) ? v : 0;
	public void SetSkill(string key, int v) { if (stats.ContainsKey(key)) stats[key] = v; }

	public bool SpendEnergy(int cost)
	{
		if (Energy < cost) { Energy = 0; return false; }
		Energy -= cost; return true;
	}

	// --- (De)serialization you already added for saving ---
	public ProstituteState ToState() => new()
	{
		Name = Name,
		Profession = Profession.ToString(),
		PortraitPath = PortraitPath,
		Stats = new(stats)
	};
	public static Prostitute FromState(ProstituteState s)
	{
		if (!System.Enum.TryParse(s.Profession, out ProfessionType prof)) prof = ProfessionType.Unassigned;
		var p = new Prostitute(s.Name, prof, s.PortraitPath);
		if (s.Stats != null) foreach (var kv in s.Stats) p.SetSkill(kv.Key, kv.Value);
		return p;
	}
}
