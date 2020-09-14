using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public enum PageMoveType
{
	Next,
	Last
}

// not allow slider 
[RequireComponent(typeof(ScrollRect))]
public class PageView : MonoBehaviour
{
	#region Prefabs

	// Page prefab
	public Page PagePrefab;

	#endregion

	#region Setting prop

	// 通常情况下，两个 page 就够用了
	public int MaxPageCount = 2;
	public float PageSlideInterval = 0.3f;

	public bool ShowIndexpoints = true;

	#endregion

	#region Basic prop

	private ScrollRect scroll;
	private RectTransform rectTrans;
	private RectTransform content;

	private int curIndex = 0;              // 当前页码
	public int Index {
		get => curIndex;
		set {
			curIndex = value;
		}
	}

	private bool isMoving = false;

	#endregion

	public System.Action NextPageCallback;
	public System.Action LastPageCallback;

	// points ctrl
	[HideInInspector]
	public IndicesCtrl Indices;

	[HideInInspector]
	public List<Page> Pages;
	private List<PageDataHandle> datas;

	private Queue<PageMoveType> commands;

	public void Init()
	{
		scroll = GetComponent<ScrollRect>();
		scroll.vertical = false;
		scroll.horizontal = false;
		content = scroll.content;

		rectTrans = scroll.GetComponent<RectTransform>();

		Pages = new List<Page>();
		datas = new List<PageDataHandle>();
		commands = new Queue<PageMoveType>();

		Indices = this.GetComponentInChildren<IndicesCtrl>();
		Indices.Init(this);

		curIndex = 0;
		isMoving = false;

		for (int i = 0; i < MaxPageCount; i++)
		{
			generatePage();
		}

		fitContent();
	}

	public Page GetCurPage()
	{
		if (Pages.Count <= 0)
			return null;

		return Pages[curIndex];
	}

	public void AddPages(List<PageDataHandle> datas)
	{
		this.datas = datas;

		if (!ShowIndexpoints)
			return;

		Indices.AddPoints(datas.Count);
		Pages[0].UpdateData(datas[Pages.Count - 1]);
		return;
	}

	private void generatePage()
	{
		var newPage = Instantiate(PagePrefab);
		newPage.transform.SetParent(content);
		newPage.transform.localScale = Vector3.one;
		newPage.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(rectTrans.rect.width, rectTrans.rect.height);

		newPage.GetComponent<RectTransform>().anchorMax = Vector2.one * 0.5f;
		newPage.GetComponent<RectTransform>().anchorMin = Vector2.one * 0.5f;
		newPage.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(Pages.Count * rectTrans.rect.width, 0, 0);

		Pages.Add(newPage);
	}

	private void fitContent() 
	{
		float width = (Pages.Count % 2 != 0 ? Pages.Count : (Pages.Count - 1)) / 2f * rectTrans.rect.width;
		content.anchoredPosition3D = new Vector3(-width, 0, 0);
	}

	// 默认一处当前一个 page
	public void RemovePageByIndex(int index = -1)
	{
		//if (index > datas.Count || index < 0)
		//	index = curIndex;

		//DestroyImmediate(points[index].gameObject);
		//points.RemoveAt(index);

		//datas.RemoveAt(index);

		//// if data count < max page count, remove page
		//if (datas.Count <= MaxPageCount)
		//{
		//	DestroyImmediate(Pages[index].gameObject);
		//	Pages.RemoveAt(index);
		//}
	}

	public void ClearAll() 
	{
		Indices.ClearAll();

		for (int j = 0; j < Pages.Count; j++) 
		{
			DestroyImmediate(Pages[j].gameObject);
		}	
		Pages.Clear();

		datas.Clear();

		curIndex = -1;
	}

	public void NextPage()
	{
		if (isMoving)
			return;

		commands.Enqueue(PageMoveType.Next);
		checkFinished();
	}

	public void LastPage()
	{
		if (isMoving)
			return;

		commands.Enqueue(PageMoveType.Last);
		checkFinished();
	}

	public void JumpToPageByIndex(bool isOn)
	{
		if (!isOn || isMoving)
			return;

		int index = Indices.Points.Find((t) => { return t.toggle.isOn; }).getIndex();
		int d = index - curIndex;

		for (int i = 0; i < Mathf.Abs(d); i++) 
		{
			commands.Enqueue(d > 0 ? PageMoveType.Next : PageMoveType.Last);
		}

		checkFinished();
	}

	// play animation

	private void moveToPageAnim(int index, PageMoveType type)
	{
		isMoving = true;
		bool isNext = type == PageMoveType.Next;

		Page p1 = Pages[curIndex % 2];
		Page p2 = Pages[1 - curIndex % 2];

		// update
		p2.UpdateData(datas[index]);

		RectTransform r1 = p1.GetComponent<RectTransform>();
		RectTransform r2 = p2.GetComponent<RectTransform>();
		r2.anchoredPosition = new Vector2((isNext ? 1 : -1) * r1.sizeDelta.x, 0);

		Sequence seq = DOTween.Sequence();
		seq.Append(r1.DOAnchorPosX((isNext ? -1 : 1) * r1.sizeDelta.x, PageSlideInterval));
		seq.Join(r2.DOAnchorPosX(0, PageSlideInterval));
		seq.AppendCallback(() =>
		{
			if (isNext && NextPageCallback != null)
			{
				NextPageCallback();
			}
			else if (!isNext && LastPageCallback != null) 
			{
				LastPageCallback();
			}

			curIndex = index;
			checkFinished();
		});
	}

	private void checkFinished() 
	{
		if (commands.Count <= 0) 
		{
			isMoving = false;
			// Indices.SwitchOn(curIndex, true);

			return;
		}
			
		int index = -1;
		// go ahead
		PageMoveType type = commands.Dequeue();
		switch (type) 
		{
			case PageMoveType.Next:
				index = curIndex + 1;
				if (index >= datas.Count) 
				{
					checkFinished();
					return;
				}
				moveToPageAnim(index, type);
				break;
			case PageMoveType.Last:
				index = curIndex - 1;
				if (index < 0) 
				{
					checkFinished();
					return;
				}
				moveToPageAnim(index, type);
				break;
			default:
				Debug.LogError("");
				break;
		}
	}
}
