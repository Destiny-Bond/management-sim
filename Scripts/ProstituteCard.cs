using Godot;

public partial class ProstituteCard : Control
{
	[Export] public string ProstituteName = "";

	public override Variant _GetDragData(Vector2 atPosition)
	{
		if (string.IsNullOrEmpty(ProstituteName))
			return default; // Nil Variant

		// Optional: visual preview during drag
		if (GetChildCount() > 0)
		{
			var preview = Duplicate() as Control;
			if (preview != null) SetDragPreview(preview);
		}

		// Drag payload (Variant-friendly Dictionary)
		var payload = new Godot.Collections.Dictionary
		{
			["type"] = "prostitute",
			["name"] = ProstituteName
		};

		return Variant.CreateFrom(payload);
	}
}
