using DG.Tweening;
using DG.Tweening.Core.Easing;
using System.Collections;
using System.Collections.Generic;
using Unity.UIWidgets.widgets;
using UnityEngine;
using UnityEngine.UI;

public class IndicesCtrl : MonoBehaviour
{
	// toggle prefab
	public Point IndexPointPrefab;

	public int MaxPointCount = 3;
	public float ToggleSizeX = 20;
	public float OffsetX = 10;

	private bool pointTouchable = true;
	public bool PointTouchable
	{
		get
		{
			return pointTouchable;
		}

		set
		{
			pointTouchable = value;
		}
	}

	[HideInInspector]
	public List<Point> Points;

	private ScrollRect scroll;
	private RectTransform scrollRect;
	private RectTransform content;

	private ToggleGroup group;
	private PageView owner = null;

	private float lastPosX = 0;

	private int count = 0;

	public void Init(PageView ctrl)
	{
		owner = ctrl;

		scroll = this.GetComponent<ScrollRect>();
		scrollRect = scroll.GetComponent<RectTransform>();
		content = scroll.content;

		group = this.GetComponent<ToggleGroup>();
		Points = new List<Point>();

		for (int i = 0; i < MaxPointCount; i++)
		{
			generatePoint(i);
		}
	}

	public void AddPoints(int count)
	{
		this.count = count;

		for (int i = 0; i < Points.Count; i++) 
		{
			if (i < count)
				Points[i].UpdateData(i, owner.Index == i);
			else
				Points[i].gameObject.SetActive(false);
		}

		fitContent();
	}

	private void fitContent() 
	{
		scroll.GetComponent<RectTransform>().sizeDelta = new Vector2(MaxPointCount * (ToggleSizeX + OffsetX), 40);
		content.sizeDelta = new Vector2 (count * (ToggleSizeX + OffsetX), 0);
	}

	public void generatePoint(int index)
	{
		var newPoint = Instantiate(IndexPointPrefab);
		newPoint.Init();
		newPoint.transform.SetParent(content);
		newPoint.toggle.group = group;
		newPoint.transform.localScale = Vector3.one;
		newPoint.rect.sizeDelta = Vector2.one * ToggleSizeX;
		newPoint.rect.anchoredPosition = new Vector3(index * (ToggleSizeX + OffsetX) + OffsetX, 0, 0);
		newPoint.toggle.isOn = false;
		newPoint.toggle.interactable = pointTouchable;
		newPoint.toggle.onValueChanged.AddListener(owner.JumpToPageByIndex);
		Points.Add(newPoint);

		if (Points.Count == 1)
			newPoint.toggle.isOn = true;
	}

	public void ClearAll ()
	{
		for (int i = 0; i < content.childCount; i++) 
		{
			DestroyImmediate(content.GetChild(i));
		}

		Points.Clear();
	}

	public void SwitchOn(int index) 
	{
		int p = index % Points.Count;
		int lp = Points.FindIndex(x => x.toggle.isOn);
		Points[p].toggle.isOn = true;
		content.DOAnchorPosX( - index * (ToggleSizeX + OffsetX), 0.2f);
	}

	private Vector3 getPositionInScreen(RectTransform point) 
	{
		Vector3 worldPos = content.TransformPoint(point.anchoredPosition);
		return scrollRect.InverseTransformPoint(worldPos);	
	}

	// Update is called once per frame
	void Update()
    {
		float buffer = this.scrollRect.sizeDelta.x / 2;
		bool isLeft = content.position.x < this.lastPosX;
		float offset = (ToggleSizeX + OffsetX) * Points.Count;
		for (int i = 0; i < Points.Count; ++i)
		{
			Vector3 viewPos = this.getPositionInScreen(Points[i].rect);
			Vector3 localPos = Points[i].rect.localPosition;
			if (isLeft)
			{
				// if away from buffer zone and not reaching top of content
				if (viewPos.x < -buffer && localPos.x + offset < content.sizeDelta.x)
				{
					localPos.x += offset;
					Points[i].rect.localPosition = localPos;
					int index = Points[i].getIndex() + Points.Count; // update item id
					Points[i].UpdateData(index, owner.Index == index);
				}
			}
			else
			{
				if (viewPos.x > buffer && viewPos.x - offset / 2 > 0 && localPos.x - offset > 0)
				{
					localPos.x -= offset;
					Points[i].rect.localPosition = localPos;
					int index = Points[i].getIndex() - Points.Count;
					Points[i].UpdateData(index, owner.Index == index);
				}
			}
		}

		lastPosX = content.position.x;
	}
}
