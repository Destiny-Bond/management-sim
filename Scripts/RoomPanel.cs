using Godot;
using System;

public partial class RoomPanel : Control
{
	[Export] public NodePath SkillImagePath;      // TextureRect to show skill art
	[Export] public NodePath TimerLabelPath;      // Label to show remaining time
	[Export] public float ServiceSeconds = 8f;    // how long each service lasts
	[Export] public int EnergyPerSecond = 3;      // prostitute energy drain

	private TextureRect skillImage;
	private Label timerLabel;
	private Timer serviceTimer;

	// Runtime state
	private Prostitute currentProstitute;
	private string currentSkill;
	private Customer currentCustomer;

	public bool HasCustomer => currentCustomer != null;
	public bool Busy => currentProstitute != null;

	public override void _Ready()
	{
		// This helps the Control receive drop events even if children are on top
		MouseFilter = MouseFilterEnum.Stop;

		skillImage = GetNodeOrNull<TextureRect>(SkillImagePath)
					 ?? FindChild("SkillImage", true, false) as TextureRect;

		timerLabel = GetNodeOrNull<Label>(TimerLabelPath)
					 ?? FindChild("TimerLabel", true, false) as Label;

		serviceTimer = new Timer { OneShot = true, WaitTime = ServiceSeconds };
		AddChild(serviceTimer);
		serviceTimer.Timeout += OnServiceTimeout;

		UpdateUIIdle();
	}

	// Called by screen when a customer spawns here
	public void AssignCustomer(Customer c)
	{
		if (Busy) return;
		currentCustomer = c;
		UpdateUIWaiting();
	}

	public void ClearCustomer()
	{
		currentCustomer = null;
		UpdateUIIdle();
	}

	// ---- Drag & Drop ----
	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		if (!HasCustomer || Busy) return false;
		if (data.VariantType != Variant.Type.Dictionary) return false;

		var dict = data.AsGodotDictionary();
		return dict.ContainsKey("type") && dict["type"].AsString() == "prostitute";
	}

	public override void _DropData(Vector2 atPosition, Variant data)
	{
		if (!_CanDropData(atPosition, data)) return;

		var dict = data.AsGodotDictionary();
		if (!dict.ContainsKey("name")) return;

		string name = dict["name"].AsString();
		var session = GetNode<GameSession>("/root/GameSession");
		if (!session.TryGetProstitute(name, out var p)) return;

		// Start service with this prostitute
		currentProstitute = p;
		currentSkill = currentCustomer.DesiredSkill;
		StartService();

		GD.Print($"[Drop] Assigned {name} to room {Name}");
	}

	private void StartService()
	{
		// Show skill image (generic icon; replace with your own lookup if you like)
		if (skillImage != null)
		{
			var tex = GD.Load<Texture2D>($"res://art/skills/{currentSkill}.png");
			skillImage.Texture = tex;
		}

		// Start timer
		serviceTimer.WaitTime = ServiceSeconds;
		serviceTimer.Start();

		// Update label immediately
		if (timerLabel != null)
			timerLabel.Text = $"{Math.Ceiling(ServiceSeconds)}s";

		SetProcess(true);
	}

	public override void _Process(double delta)
	{
		if (!Busy) return;

		// Decrease prostitute energy
		if (currentProstitute != null && EnergyPerSecond > 0)
		{
			int drain = (int)Math.Ceiling(EnergyPerSecond * delta);
			if (drain > 0) currentProstitute.SpendEnergy(drain);
		}

		// Update countdown text
		if (timerLabel != null)
			timerLabel.Text = $"{Math.Max(0, Math.Ceiling(serviceTimer.TimeLeft))}s";
	}

	private void OnServiceTimeout()
	{
		// Compute payout
		int pay = ComputeEarnings(currentProstitute, currentCustomer);
		var screen = GetTree().CurrentScene as ProstitutionScreen;
		screen?.AddMoney(pay);

		// Clear state
		currentProstitute = null;
		currentSkill = null;
		ClearCustomer();
		SetProcess(false);
	}

	private int ComputeEarnings(Prostitute p, Customer c)
	{
		int basePay = c.Wealth switch
		{
			WealthTier.Low  => 20,
			WealthTier.Mid  => 40,
			WealthTier.High => 80,
			_ => 30
		};

		int skill = p.GetSkill(c.DesiredSkill);
		float skillFactor = 1.0f + (skill / 100.0f) * 0.5f; // up to +50% at 100 skill
		float profMult = ProfessionRules.GetEarningsMult(p.Profession, c.DesiredSkill);

		float total = basePay * skillFactor * profMult;
		return (int)MathF.Round(total);
	}

	private void UpdateUIIdle()
	{
		if (timerLabel != null) timerLabel.Text = "—";
		if (skillImage != null) skillImage.Texture = null;
		SetProcess(false);
	}

	private void UpdateUIWaiting()
	{
		if (timerLabel != null) timerLabel.Text = $"{currentCustomer.DesiredSkill}";
		if (skillImage != null)
		{
			var tex = GD.Load<Texture2D>($"res://art/skills/{currentCustomer.DesiredSkill}.png");
			skillImage.Texture = tex;
		}
	}
}
