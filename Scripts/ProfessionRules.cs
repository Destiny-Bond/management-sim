using System.Collections.Generic;

public static class ProfessionRules
{
	// Per-skill training multipliers (how fast each skill trains)
	public static readonly Dictionary<ProfessionType, Dictionary<string, float>> TrainingMult =
		new()
		{
			[ProfessionType.Unassigned] = new() {
				{ "softcore", 1.0f }, { "nude", 1.0f }, { "foreplay", 1.0f },
				{ "mouth", 1.0f }, { "sex", 1.0f }, { "anal", 1.0f }, { "fetish", 1.0f }
			},
			[ProfessionType.Dancer] = new() {
				{ "softcore", 1.3f }, { "nude", 1.15f }, { "foreplay", 1.1f },
				{ "mouth", 0.9f }, { "sex", 0.9f }, { "anal", 0.8f }, { "fetish", 0.9f }
			},
			[ProfessionType.Model] = new() {
				{ "softcore", 1.25f }, { "nude", 1.25f }, { "foreplay", 0.95f },
				{ "mouth", 0.9f }, { "sex", 0.85f }, { "anal", 0.8f }, { "fetish", 0.9f }
			},
			[ProfessionType.Courtesan] = new() {
				{ "softcore", 1.05f }, { "nude", 1.05f }, { "foreplay", 1.2f },
				{ "mouth", 1.15f }, { "sex", 1.15f }, { "anal", 0.9f }, { "fetish", 0.95f }
			},
			[ProfessionType.Dominatrix] = new() {
				{ "softcore", 0.9f }, { "nude", 0.9f }, { "foreplay", 1.0f },
				{ "mouth", 0.9f }, { "sex", 1.0f }, { "anal", 1.0f }, { "fetish", 1.35f }
			},
			[ProfessionType.Escort] = new() {
				{ "softcore", 1.0f }, { "nude", 1.0f }, { "foreplay", 1.1f },
				{ "mouth", 1.15f }, { "sex", 1.2f }, { "anal", 1.05f }, { "fetish", 0.95f }
			},
			[ProfessionType.Whore] = new() {
				{ "softcore", 1.2f }, { "nude", 1.2f }, { "foreplay", 1.2f },
				{ "mouth", 1.2f }, { "sex", 1.2f }, { "anal", 1.2f }, { "fetish", 1.2f }
			},
		};

	//Work/earnings multipliers by “content type” (use same skill keys or your own work tags)
	public static readonly Dictionary<ProfessionType, Dictionary<string, float>> EarningsMult =
		new()
		{
			[ProfessionType.Unassigned] = new() {
				{ "softcore", 1.0f }, { "nude", 1.0f }, { "foreplay", 1.0f },
				{ "mouth", 1.0f }, { "sex", 1.0f }, { "anal", 1.0f }, { "fetish", 1.0f }
			},
			[ProfessionType.Dancer] = new() {
				{ "softcore", 1.25f }, { "nude", 1.15f }, { "foreplay", 1.05f },
				{ "mouth", 0.95f }, { "sex", 0.9f }, { "anal", 0.9f }, { "fetish", 1.0f }
			},
			[ProfessionType.Model] = new() {
				{ "softcore", 1.3f }, { "nude", 1.3f }, { "foreplay", 1.0f },
				{ "mouth", 0.95f }, { "sex", 0.9f }, { "anal", 0.85f }, { "fetish", 0.95f }
			},
			[ProfessionType.Courtesan] = new() {
				{ "softcore", 1.05f }, { "nude", 1.05f }, { "foreplay", 1.15f },
				{ "mouth", 1.15f }, { "sex", 1.2f }, { "anal", 1.0f }, { "fetish", 1.0f }
			},
			[ProfessionType.Dominatrix] = new() {
				{ "softcore", 0.9f }, { "nude", 0.9f }, { "foreplay", 1.0f },
				{ "mouth", 0.95f }, { "sex", 1.05f }, { "anal", 1.1f }, { "fetish", 1.4f }
			},
			[ProfessionType.Escort] = new() {
				{ "softcore", 1.0f }, { "nude", 1.0f }, { "foreplay", 1.1f },
				{ "mouth", 1.15f }, { "sex", 1.25f }, { "anal", 1.05f }, { "fetish", 0.95f }
			},
			[ProfessionType.Whore] = new() {
				{ "softcore", 1.2f }, { "nude", 1.2f }, { "foreplay", 1.2f },
				{ "mouth", 1.2f }, { "sex", 1.2f }, { "anal", 1.2f }, { "fetish", 1.2f }
			},
		};

	public static float GetTrainingMult(ProfessionType p, string skill)
		=> TrainingMult.TryGetValue(p, out var m) && m.TryGetValue(skill, out var v) ? v : 1f;

	public static float GetEarningsMult(ProfessionType p, string content)
		=> EarningsMult.TryGetValue(p, out var m) && m.TryGetValue(content, out var v) ? v : 1f;
}
