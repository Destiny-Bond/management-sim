using System.Collections.Generic;

public class ProstituteState
{
	public string Name;
	public string Profession; // store enum as string for easy JSON save
	public string PortraitPath;
	public Dictionary<string, int> Stats;
}
