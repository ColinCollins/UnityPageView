using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Point : MonoBehaviour
{
	private int index = 0;

	[HideInInspector]
	public Toggle toggle = null;
	[HideInInspector]
	public RectTransform rect = null;
	
	public void Init() 
	{
		toggle = this.GetComponent<Toggle>();
		rect = this.GetComponent<RectTransform>();
	}

	public void UpdateData(int newIndex)
	{
		this.index = newIndex;
	}

	public int getIndex() 
	{
		return index;
	}
}
