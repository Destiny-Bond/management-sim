using Godot;
using System;
using System.Collections.Generic;

public enum WealthTier { Low, Mid, High }

public class Customer
{
	public string DesiredSkill;   // "softcore","nude","foreplay","mouth","sex","anal","fetish"
	public WealthTier Wealth;
	public Customer(string skill, WealthTier wealth) { DesiredSkill = skill; Wealth = wealth; }
}

public partial class ProstitutionScreen : Control
{
	[Export] public NodePath RoomsPath;         // GridContainer with RoomPanel children
	[Export] public NodePath ProstitutesPath;   // HBox/VBox with ProstituteCard children (optional; you might build elsewhere)
	[Export] public NodePath MoneyLabelPath;    // Label to show money

	[Export] public float SpawnInterval = 6f;   // seconds between customer spawns
	[Export] public int MaxConcurrentCustomers = 4;

	private GridContainer roomsContainer;
	private int lastCols = -1;
	private Label moneyLabel;
	private Timer spawnTimer;

	private readonly string[] skills = { "softcore","nude","foreplay","mouth","sex","anal","fetish" };
	private readonly Random rng = new();

	// Runtime state
	private List<RoomPanel> rooms = new();
	private int money = 0;

	public override void _Ready()
	{
		roomsContainer = GetNodeOrNull<GridContainer>(RoomsPath) ?? FindChild("Rooms", true, false) as GridContainer;
		moneyLabel = GetNodeOrNull<Label>(MoneyLabelPath) ?? FindChild("MoneyLabel", true, false) as Label;

		if (roomsContainer == null)
		{
			GD.PrintErr("[Prostitution] Rooms container not found.");
			return;
		}

		rooms.Clear();
		foreach (Node n in roomsContainer.GetChildren())
		{
			if (n is RoomPanel rp) rooms.Add(rp);
		}

		// Spawn timer
		spawnTimer = new Timer { WaitTime = SpawnInterval, OneShot = false, Autostart = true };
		AddChild(spawnTimer);
		spawnTimer.Timeout += TrySpawnCustomer;

		UpdateMoney(0);
	}

	private void TrySpawnCustomer()
	{
		// Count existing customers
		int current = 0;
		foreach (var r in rooms) if (r.HasCustomer) current++;

		if (current >= Math.Min(MaxConcurrentCustomers, rooms.Count)) return;

		// Find empty room (no customer and not busy)
		RoomPanel target = null;
		foreach (var r in rooms)
		{
			if (!r.HasCustomer && !r.Busy) { target = r; break; }
		}
		if (target == null) return;

		// Make a customer with a random desired skill and wealth
		string skill = skills[rng.Next(skills.Length)];
		WealthTier wealth = RollWealth();

		target.AssignCustomer(new Customer(skill, wealth));
	}

	private WealthTier RollWealth()
	{
		int roll = rng.Next(100);
		if (roll < 60) return WealthTier.Low;
		if (roll < 90) return WealthTier.Mid;
		return WealthTier.High;
	}

	public void AddMoney(int amount) => UpdateMoney(money + amount);

	private void UpdateMoney(int newValue)
	{
		money = Math.Max(0, newValue);
		if (moneyLabel != null) moneyLabel.Text = $"$ {money}";
	}
	
	public override void _Process(double delta)
	{
		if (roomsContainer == null) return;

		int w = (int)GetViewportRect().Size.X;
		int cols = w switch
		{
			<= 900  => 2,
			<= 1400 => 3,
			_       => 4
		};
		if (cols != lastCols)
		{
			roomsContainer.Columns = cols;
			lastCols = cols;
		}
	}
}
