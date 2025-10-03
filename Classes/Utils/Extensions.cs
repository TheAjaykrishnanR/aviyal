/*
	MIT License
    Copyright (c) 2025 Ajaykrishnan R	
*/

using System;

public static class Extensions
{
	public static bool ContainsFlag(this uint flag, uint flagToCheck)
	{
		if ((flag & flagToCheck) != 0)
		{
			return true;
		}
		return false;
	}
}
