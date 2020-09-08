using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/**
 * 1. 限制数据应该在对接方，那么 PageView 需要做的应该是在 Destroy 时选择是否直接消除对象， pageObj
 * 2. pageObj 需要 OnUpdate 用于及时更新数据
 * 3. 无限模式就固定 page，并且估计需要关闭 content 的 layout 布局保证不会自动定位错误。
 * 4. Page 和 Data 和 point 三者都需要分离，目前无限模式不支持滑动效果，其实目前来看，两个 page 也可以实现功能， 无限模式 indexPoint 不显示
 * 5. Order 模式控制命令进行 
 * 6. 现在的模式并不好，两套并存之后很多内容耦合度很高，建议创建子类，进行代码分离
 */

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
	
	// toggle srcoll rect
	public ScrollRect Toggles;

	// toggle prefab
	public Toggle IndexPointPrefab;

	private ToggleGroup group;

	#endregion

	#region Setting prop

	// 通常情况下，两个 page 就够用了
	public int MaxPageCount = 2;
	public float PageSlideInterval = 0.3f;

	public bool ShowIndexpoints = true;
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

	[HideInInspector]
	public List<Page> Pages;
	private List<Toggle> points;
	private List<PageDataHandle> datas;

	private Queue<PageMoveType> commands;
	
	public void Init()
	{
		scroll = GetComponent<ScrollRect>();
		scroll.vertical = false;
		scroll.horizontal = false;
		content = scroll.content;

		group = Toggles.GetComponent<ToggleGroup>();
		rectTrans = scroll.GetComponent<RectTransform>();

		Pages = new List<Page>();
		points = new List<Toggle>();
		datas = new List<PageDataHandle>();
		commands = new Queue<PageMoveType>();

		curIndex = 0;
		isMoving = false;
	}

	public Page GetCurPage() 
	{
		if (Pages.Count <= 0)
			return null;

		return Pages[curIndex];
	}

	// 无限模式下无法增加新的页面
	public void AddPage(PageDataHandle data)
	{
		datas.Add(data);
		addIndexPoint();

		if (Pages.Count >= MaxPageCount)
			return;

		generatePage();
		fitContent();

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
		newPage.UpdateData(datas[Pages.Count - 1]);
	}

	private void fitContent() 
	{
		float width = (Pages.Count % 2 != 0 ? Pages.Count : (Pages.Count - 1)) / 2f * rectTrans.rect.width;
		content.anchoredPosition3D = new Vector3(-width, 0, 0);
	}

	// 默认一处当前一个 page
	public void RemovePageByIndex(int index = -1)
	{
		if (index > datas.Count || index < 0)
			index = curIndex;

		DestroyImmediate(points[index].gameObject);
		points.RemoveAt(index);

		datas.RemoveAt(index);

		// if data count < max page count, remove page
		if (datas.Count <= MaxPageCount)
		{
			DestroyImmediate(Pages[index].gameObject);
			Pages.RemoveAt(index);
		}
	}

	public void ClearAll() 
	{
		for (int i = 0; i < points.Count; i++) 
		{
			DestroyImmediate(points[i].gameObject);
		}
		points.Clear();

		for (int j = 0; j < Pages.Count; j++) 
		{
			DestroyImmediate(Pages[j].gameObject);
		}	
		Pages.Clear();

		datas.Clear();

		curIndex = -1;
	}

	private void addIndexPoint() 
	{
		var newPoint = Instantiate(IndexPointPrefab);
		newPoint.transform.SetParent(Toggles.content.transform);
		newPoint.group = group;
		newPoint.transform.localScale = Vector3.one;
		newPoint.isOn = false;
		newPoint.interactable = pointTouchable;
		newPoint.onValueChanged.AddListener(JumpToPageByIndex);

		newPoint.gameObject.SetActive(ShowIndexpoints);
		points.Add(newPoint);

		if (points.Count == 1)
			newPoint.isOn = true;
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

		int index = points.FindIndex((t) => { return t.isOn; });
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

			points[index].isOn = true;
			curIndex = index;
			checkFinished();
		});
	}

	private void checkFinished() 
	{
		if (commands.Count <= 0) 
		{
			isMoving = false;
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
