using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class TrainingScreen : Control
{
	// --- Skills (keys used for buttons and folders)
	private readonly string[] skills = new string[]
	{
		"softcore", "nude", "foreplay", "mouth", "sex", "anal", "fetish"
	};
	
	private TextureRect portraitImage;
	private Label nameLabel;
	private Label professionLabel;
	
	// Finish training button implimentation
	[Export] private string WorkScenePath = "res://scenes/prostitution_screen.tscn"; // set to your scene
	private Button proceedButton;
	
	// Display names for labels only
	private readonly Dictionary<string, string> skillDisplayNames = new Dictionary<string, string>
	{
		{ "softcore", "Softcore" },
		{ "nude", "Nude Modeling" },
		{ "foreplay", "Foreplay" },
		{ "mouth", "Oral" },
		{ "sex", "Sex" },
		{ "anal", "Anal" },
		{ "fetish", "Fetish" }
	};

	// Current stats for the active prostitute
	private readonly Dictionary<string, int> prostituteSkills = new Dictionary<string, int>();

	// Fast refs to bars & buttons
	private readonly Dictionary<string, ProgressBar> barRefs = new Dictionary<string, ProgressBar>();
	private readonly List<Button> skillButtons = new List<Button>();
	private bool barsBuilt = false;

	// Scene refs
	[Export] private NodePath PlayerPath;   // drag your Player node here in the Inspector
	private Player player;                  // scene-referenced Player
	private TextureRect trainingOverlay;    // full-screen image we fade and use to block clicks
	private Prostitute currentProstitute;   // your trainee (2B)
	private ProgressBar playerEnergyBar;
	// Input lock
	private bool isTraining = false;

	public override void _Ready()
	{
		GD.Print("[READY] TrainingScreen start");
		try
		{
			// 1) Resolve Player (optional)
			if (PlayerPath != null && !PlayerPath.IsEmpty)
			{
				player = GetNodeOrNull<Player>(PlayerPath);
				GD.Print($"[READY] Player via scene path: {player?.Name ?? "null"}");
			}

			// 2) Resolve selected prostitute (fallback to 2B)
			Prostitute fromSession = null;
			var session = GetNodeOrNull<GameSession>("/root/GameSession");
			if (session == null)
			{
				GD.PrintErr("[READY] GameSession autoload not found at /root/GameSession.");
			}
			else
			{
				fromSession = session.CurrentProstitute;
			}

			// IMPORTANT: assign to the field
			currentProstitute = fromSession ?? new Prostitute("2B", ProfessionType.Whore, "res://art/portraits/2B.png");
			GD.Print($"[READY] Using prostitute: {currentProstitute?.Name}");

			// 3) Header nodes (safe resolve)
			portraitImage = GetNodeOrNull<TextureRect>("Header/PortraitImage")
							?? FindChild("PortraitImage", true, false) as TextureRect;

			nameLabel = GetNodeOrNull<Label>("Header/Info/NameLabel")
						?? FindChild("NameLabel", true, false) as Label;

			professionLabel = GetNodeOrNull<Label>("Header/Info/ProfessionLabel")
							  ?? FindChild("ProfessionLabel", true, false) as Label;

			if (portraitImage == null) GD.PrintErr("[READY] PortraitImage not found (expected at Header/PortraitImage).");
			if (nameLabel == null) GD.PrintErr("[READY] NameLabel not found (expected at Header/Info/NameLabel).");
			if (professionLabel == null) GD.PrintErr("[READY] ProfessionLabel not found (expected at Header/Info/ProfessionLabel).");

			if (portraitImage != null)
			{
				portraitImage.StretchMode = TextureRect.StretchModeEnum.KeepAspect;
				portraitImage.Set("ignore_texture_size", true);
			}

			UpdateProstituteHeader();

			// 4) Overlay node (needed for PlayTrainingAnimation)
			trainingOverlay = GetNodeOrNull<TextureRect>("TrainingOverlay");
			if (trainingOverlay == null)
			{
				GD.PrintErr("[READY] TrainingOverlay (TextureRect) not found.");
			}
			else
			{
				trainingOverlay.Visible = false;
				trainingOverlay.Modulate = new Color(1, 1, 1, 0);
				trainingOverlay.ZIndex = 1000;
				trainingOverlay.MouseFilter = MouseFilterEnum.Stop;
			}

			// 5) Load stats into dictionary BEFORE building bars
			prostituteSkills.Clear();
			foreach (var s in skills)
				prostituteSkills[s] = currentProstitute.GetSkill(s);

			// 6) Build bars, connect buttons, sync UI
			SetupSkillBars();
			ConnectTrainingButtons();
			UpdateSkillBars();
			
			//Code for finish training button
			proceedButton = GetNodeOrNull<Button>("ProceedToWorkButton")
							?? FindChild("ProceedToWorkButton", true, false) as Button;

			if (proceedButton != null)
			{
				proceedButton.Disabled = false;
				proceedButton.Pressed += OnProceedToWorkPressed;
				GD.Print("[Connect] ProceedToWorkButton connected.");
			}
			else
			{
				GD.PrintErr("[Connect] ProceedToWorkButton not found. Add a Button named exactly 'ProceedToWorkButton'.");
			}
			
			//Player energy
			playerEnergyBar = GetNodeOrNull<ProgressBar>("Header/Info/PlayerEnergyBar")
				  ?? FindChild("PlayerEnergyBar", true, false) as ProgressBar;

			if (playerEnergyBar == null)
			{
				GD.PrintErr("[READY] PlayerEnergyBar not found.");
			}
			else
			{
				InitPlayerEnergyUI();
			}
		}
		catch (Exception ex)
		{
			GD.PrintErr("[READY] ERROR: " + ex);
		}
		GD.Print("[READY] TrainingScreen done");
	}

	// Build bars dynamically under a VBoxContainer called "SkillBars"
	private void SetupSkillBars()
	{
		if (barsBuilt) return; // guard against re-entry (hot-reload etc.)

		var container = FindChild("SkillBars", recursive: true, owned: false) as VBoxContainer;
		if (container == null)
		{
			GD.PrintErr("[UI] VBoxContainer 'SkillBars' not found.");
			return;
		}

		// Clear any old rows (avoid duplicates)
		foreach (Node child in container.GetChildren())
			child.QueueFree();
		barRefs.Clear();

		container.MouseFilter = MouseFilterEnum.Pass;
		container.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		container.SizeFlagsVertical = SizeFlags.ShrinkBegin;

		foreach (string skill in skills)
		{
			var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

			var label = new Label
			{
				Text = skillDisplayNames[skill],
				CustomMinimumSize = new Vector2(150, 0)
			};

			var bar = new ProgressBar
			{
				Name = $"{skill}_bar",
				MaxValue = 100,
				Value = prostituteSkills[skill],
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			};

			row.AddChild(label);
			row.AddChild(bar);
			container.AddChild(row);

			barRefs[skill] = bar; // keep a direct reference
		}

		barsBuilt = true;
	}

	// Auto-connect buttons named TrainSoftcoreButton, TrainNudeButton, ...
	private void ConnectTrainingButtons()
	{
		skillButtons.Clear();

		foreach (string skill in skills)
		{
			string id = ToPascal(skill);
			string expected = $"Train{id}Button";

			var node = FindChild(expected, recursive: true, owned: false);
			var btn = node as Button;

			if (btn != null)
			{
				skillButtons.Add(btn);
				btn.Disabled = false;
				GD.Print($"[Connect] {expected} -> {btn.GetPath()}");
				btn.Pressed += async () => await StartTraining(skill);
			}
			else
			{
				GD.PrintErr($"[Connect] Button NOT found: {expected}");
			}
		}

		GD.Print($"[Connect] Total buttons linked: {skillButtons.Count}");
	}

	private string ToPascal(string key)
	{
		if (string.IsNullOrEmpty(key)) return key;
		if (key.Length == 1) return key.ToUpper();
		return char.ToUpper(key[0]) + key.Substring(1);
	}

	private void SetButtonsEnabled(bool enabled)
	{
		foreach (var b in skillButtons)
			b.Disabled = !enabled;

		if (proceedButton != null)
			proceedButton.Disabled = !enabled;
	}
	
	private void UpdateProstituteHeader()
	{
		if (currentProstitute == null) return;

		if (nameLabel != null)
			nameLabel.Text = currentProstitute.Name ?? "Unknown";

		if (professionLabel != null)
			professionLabel.Text = ProfessionToDisplay(currentProstitute.Profession);

		if (portraitImage != null)
		{
			Texture2D tex = null;
			if (!string.IsNullOrEmpty(currentProstitute.PortraitPath))
				tex = GD.Load<Texture2D>(currentProstitute.PortraitPath);

			if (tex == null)
				tex = GD.Load<Texture2D>("res://art/portraits/placeholder.png"); // optional fallback

			portraitImage.Texture = tex;
		}
	}
	// Called on button press
	private async Task StartTraining(string skill)
	{
		if (isTraining) return; // ignore re-entries
		isTraining = true;
		SetButtonsEnabled(false);

		// Optional: energy cost per training click
		int energyCost = 5;
		if (player != null && !player.SpendEnergy(energyCost))
		{
			GD.Print("[Train] Not enough energy!");
			SetButtonsEnabled(true);
			isTraining = false;
			return;
		}
		
		UpdatePlayerEnergyUI();

		GD.Print($"[Click] Training: {skill}");

		await PlayTrainingAnimation(skill);

		// Apply gain based on Player stats (with diminishing returns)
		int current = prostituteSkills[skill];
		int gain = CalculateTrainingGain(skill, current);
		int newValue = Math.Clamp(current + gain, 0, 100);

		prostituteSkills[skill] = newValue;
		currentProstitute.SetSkill(skill, newValue);

		UpdateSkillBars();
		GD.Print($"[Stat] {currentProstitute.Name}.{skill} +{gain} → {newValue} (energy now {player?.Energy})");
		UpdatePlayerEnergyUI();
		
		SetButtonsEnabled(true);
		isTraining = false;
	}

	private int CalculateTrainingGain(string skill, int currentValue)
	{
		// Player-side factors (you already have these)
		float baseGain = player != null ? player.GetBaseGain(skill) : 1.0f;
		float ability  = player != null ? player.TrainingAbility : 1.0f;

		// Profession-side factor
		var p = currentProstitute.Profession;
		float professionMult = ProfessionRules.GetTrainingMult(p, skill); // ← NEW

		// Diminishing returns
		float progressFactor = 1.0f - (currentValue / 100.0f);
		float raw = baseGain * ability * professionMult;
		raw *= 0.35f + 0.65f * progressFactor;

		int gain = System.Math.Max(1, (int)System.MathF.Round(raw));
		return gain;
	}

	private void UpdateSkillBars()
	{
		foreach (var kv in barRefs)
		{
			string skill = kv.Key;
			var bar = kv.Value;
			if (bar != null)
				bar.Value = prostituteSkills[skill];
		}
		var session = GetNode<GameSession>("/root/GameSession");
		SaveSystem.SaveGame(session);
	}

	// Fade the overlay image itself; overlay blocks clicks while visible
	private async Task PlayTrainingAnimation(string skill)
	{
		var tex = GetRandomTrainingImage(skill);
		if (tex == null)
		{
			GD.PrintErr($"[Image] Missing: res://art/training/{currentProstitute.Name}/{skill}");
			return;
		}

		trainingOverlay.Texture = tex;
		trainingOverlay.Visible = true;
		trainingOverlay.Modulate = new Color(1, 1, 1, 0); // transparent

		// Fade IN
		var fadeIn = CreateTween();
		fadeIn.TweenProperty(trainingOverlay, "modulate:a", 1.0f, 0.35);
		await ToSignal(fadeIn, "finished");
		GD.Print($"[Image] Showing: {tex.ResourcePath}");

		// Wait for confirm (left click or ui_accept)
		await WaitForConfirmAsync();

		// Fade OUT
		var fadeOut = CreateTween();
		fadeOut.TweenProperty(trainingOverlay, "modulate:a", 0.0f, 0.35);
		await ToSignal(fadeOut, "finished");

		trainingOverlay.Visible = false;
	}

	// Poll-based confirm: left mouse edge or ui_accept
	private async Task WaitForConfirmAsync()
	{
		bool lastDown = false;
		while (true)
		{
			await ToSignal(GetTree(), "process_frame");

			bool nowDown = Input.IsMouseButtonPressed(MouseButton.Left);
			if (nowDown && !lastDown)
				break;
			lastDown = nowDown;

			if (Input.IsActionJustPressed("ui_accept"))
				break;
		}
	}

	// res://art/training/{Name}/{skill}/*.(png|webp|jpg)
	private Texture2D GetRandomTrainingImage(string skill)
	{
		string basePath = $"res://art/training/{currentProstitute.Name}/{skill}";
		var dir = DirAccess.Open(basePath);
		if (dir == null)
		{
			GD.PrintErr($"[Image] Folder not found: {basePath}");
			return null;
		}

		var files = new List<string>();
		dir.ListDirBegin();
		string file = dir.GetNext();
		while (file != "")
		{
			if (!dir.CurrentIsDir() &&
				(file.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
				 file.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ||
				 file.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)))
			{
				files.Add($"{basePath}/{file}");
			}
			file = dir.GetNext();
		}
		dir.ListDirEnd();

		if (files.Count == 0)
		{
			GD.PrintErr($"[Image] No images in {basePath}");
			return null;
		}

		var rng = new Random();
		string pick = files[rng.Next(files.Count)];
		GD.Print($"[Image] Picked: {pick}");
		return GD.Load<Texture2D>(pick);
	}
	private string ProfessionToDisplay(ProfessionType p)
	{
		// Adjust display text if you want prettier labels
		return p switch
		{
			ProfessionType.Unassigned => "Unassigned",
			ProfessionType.Dancer     => "Dancer",
			ProfessionType.Model      => "Model",
			ProfessionType.Courtesan  => "Courtesan",
			ProfessionType.Dominatrix => "Dominatrix",
			ProfessionType.Escort     => "Escort",
			ProfessionType.Whore     => "Whore",
			_ => p.ToString()
		};
	}
	private void InitPlayerEnergyUI()
	{
		if (playerEnergyBar == null || player == null) return;
		playerEnergyBar.MaxValue = player.MaxEnergy;
		playerEnergyBar.Value = player.Energy;
	}

	private void UpdatePlayerEnergyUI()
	{
		if (playerEnergyBar == null || player == null) return;
		playerEnergyBar.Value = player.Energy;
	}

	private void OnProceedToWorkPressed()
	{
		if (isTraining) return; // don’t leave in the middle of a fade

		// Optional: quick autosave before leaving
		var session = GetNodeOrNull<GameSession>("/root/GameSession");
		if (session != null)
		{
			// Make sure the current prostitute’s latest stats are in the session roster
			if (currentProstitute != null)
				session.AddOrUpdateProstitute(currentProstitute);

			SaveSystem.SaveGame(session);
		}

		if (string.IsNullOrEmpty(WorkScenePath))
		{
			GD.PrintErr("[Proceed] WorkScenePath is empty. Set it in the Inspector.");
			return;
		}

		GD.Print("[Proceed] Leaving Training → ", WorkScenePath);
		GetTree().ChangeSceneToFile(WorkScenePath);
	}
}
