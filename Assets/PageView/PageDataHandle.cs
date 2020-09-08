using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PageDataHandle
{
	public Color Color;
	public void Init () 
	{
		Color = Random.ColorHSV(0, 1);
	}
}
