using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 持有数据对象，用于自定义
public class PageDataHandle
{
	public Color Color;
	public void Init () 
	{
		Color = Random.ColorHSV(0, 1);
	}
}
