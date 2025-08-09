using Godot;
using System;
using System.Collections.Generic;


public partial class ProstituteSelect : Control
{
	// Optional: drag your GridContainer here in the Inspector
	[Export] private NodePath GridPath;

	// Simple data; replace with your real source
	private class Entry
	{
		public string Name;
		public string PortraitPath;
		public Entry(string name, string portrait) { Name = name; PortraitPath = portrait; }
	}

	private readonly List<Entry> entries = new List<Entry>
	{
		new Entry("2B",    "res://art/portraits/2B.png"),
		new Entry("Alice", "res://art/portraits/Alice.png"),
		new Entry("Eve",   "res://art/portraits/Eve.png"),
	};

	public override void _Ready()
	{
		var grid = ResolveGrid();
		if (grid == null)
		{
			GD.PrintErr("[Select] Could not create/resolve GridContainer.");
			return;
		}

		// Fill available space (ensure ScrollContainer is also full-rect)
		var scroll = grid.GetParent() as ScrollContainer;
		if (scroll != null)
		{
			scroll.SetAnchorsPreset(LayoutPreset.FullRect);
			scroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			scroll.SizeFlagsVertical   = SizeFlags.ExpandFill;
			scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Auto;
			scroll.VerticalScrollMode   = ScrollContainer.ScrollMode.Auto;
		}

		// Grid layout
		grid.Columns = 4; // tweak (3–6) depending on your card size
		grid.AddThemeConstantOverride("h_separation", 12);
		grid.AddThemeConstantOverride("v_separation", 12);

		// Clear & rebuild (hot-reload friendly)
		foreach (Node c in grid.GetChildren()) c.QueueFree();

		foreach (var e in entries)
			grid.AddChild(MakeCard(e));
	}

	private GridContainer ResolveGrid()
	{
		// 1) Preferred: explicit path via Inspector
		if (GridPath != null && !GridPath.IsEmpty)
		{
			var g = GetNodeOrNull<GridContainer>(GridPath);
			if (g != null) return g;
			GD.PrintErr($"[Select] GridPath set but not found: {GridPath}");
		}

		// 2) Common structure: ScrollContainer/Grid
		var common = GetNodeOrNull<GridContainer>("ScrollContainer/Grid");
		if (common != null) return common;

		// 3) Fallback: recursive search by name
		var found = FindChild("Grid", recursive: true, owned: false) as GridContainer;
		if (found != null) return found;

		// 4) Last resort: create ScrollContainer + Grid
		var scroll = new ScrollContainer { Name = "ScrollContainer" };
		AddChild(scroll);
		scroll.SetAnchorsPreset(LayoutPreset.FullRect);
		scroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		scroll.SizeFlagsVertical   = SizeFlags.ExpandFill;

		var grid = new GridContainer { Name = "Grid", Columns = 4 };
		grid.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		grid.SizeFlagsVertical   = SizeFlags.ExpandFill;

		scroll.AddChild(grid);
		return grid;
	}

	private Control MakeCard(Entry e)
	{
		// Outer padding so cards don't touch neighbors
		var margin = new MarginContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical   = SizeFlags.ShrinkCenter
		};
		margin.AddThemeConstantOverride("margin_left",   6);
		margin.AddThemeConstantOverride("margin_right",  6);
		margin.AddThemeConstantOverride("margin_top",    6);
		margin.AddThemeConstantOverride("margin_bottom", 6);

		// Card container
		var card = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical   = SizeFlags.ShrinkCenter,
			CustomMinimumSize   = new Vector2(240, 300) // smaller card
		};

		// Portrait that actually shrinks to fit the card
		var portrait = new TextureRect
		{
			// Key bits:
			StretchMode = TextureRect.StretchModeEnum.KeepAspect, // shrink-to-fit, no crop
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical   = SizeFlags.ExpandFill,
			CustomMinimumSize   = new Vector2(240, 240)
		};
		// Let layout control size; ignore the texture's native pixel size
		portrait.Set("ignore_texture_size", true); // same as Inspector: "Ignore Texture Size"

		var tex = GD.Load<Texture2D>(e.PortraitPath);
		if (tex != null) portrait.Texture = tex;

		// Name
		var name = new Label
		{
			Text = e.Name,
			HorizontalAlignment = HorizontalAlignment.Center
		};

		// Train button
		var train = new Button
		{
			Text = "Train",
			CustomMinimumSize = new Vector2(0, 28)
		};
		train.Pressed += () => OnPick(e.Name);

		// Assemble
		card.AddChild(portrait);
		card.AddChild(name);
		card.AddChild(train);

		margin.AddChild(card);
		return margin;
	}

	private void OnPick(string name)
	{
		// Example: pick profession per character for now
		var profession = name switch
		{
			"2B"    => ProfessionType.Whore,
			"Alice" => ProfessionType.Dancer,
			"Eve"   => ProfessionType.Model,
			_       => ProfessionType.Unassigned
		};

		var chosen = new Prostitute(
			name: name,
			profession: profession,
			portraitPath: $"res://art/portraits/{name}.png"
		);

		var session = GetNode<GameSession>("/root/GameSession");
		session.CurrentProstitute = chosen;        // also adds to roster
		session.Player ??= GetNodeOrNull<Player>("/root/Player"); // optional if you keep Player elsewhere

		SaveSystem.SaveGame(session);
		GetTree().ChangeSceneToFile("res://scenes/training.tscn");
	}
}
