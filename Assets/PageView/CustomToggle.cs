using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CustomToggle : Toggle
{
	// Custom 定义 default as int
	public PageView manager;
	public int Index;

	public override void OnPointerClick(PointerEventData eventData)
	{
		if (!manager.IndexPointTouchable)
			return;

		base.OnPointerClick(eventData);
		manager.JumpToPageByIndex(Index);
	}
}
