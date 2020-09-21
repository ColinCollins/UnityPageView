using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 控制 Toggle point 对象，不需要自定义
public class Point : MonoBehaviour, IPointerDownHandler
{
	private int index = 0;

	public Text numTxt;

	[HideInInspector]
	public Toggle toggle = null;
	[HideInInspector]
	public RectTransform rect = null;
	
	public void Init() 
	{
		toggle = this.GetComponent<Toggle>();
		rect = this.GetComponent<RectTransform>();
	}

	public void UpdateData(int newIndex, bool isOn)
	{
		this.index = newIndex;
		numTxt.text = (index + 1).ToString();
		toggle.isOn = isOn;
	}

	public int getIndex() 
	{
		return index;
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		// throw new System.NotImplementedException();

		Debug.Log("Point touched");
	}
}
