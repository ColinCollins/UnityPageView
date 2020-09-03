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

// not allow slider 
[RequireComponent(typeof(ScrollRect))]
public class PageView : MonoBehaviour
{
	#region Prefabs

	// for test
	public Page PagePrefab;

	public ToggleGroup Toggles;
	public CustomToggle IndexPointPrefab;

	#endregion

	#region Setting prop

	public float PageSlideInterval = 0.3f;
	[HideInInspector]
	private int PageCount = 2;                // 当前模式为无限限模式时，最多只会产生规定数值 n 个 page，之后可以在多个 page 中进行循环	
	public bool isInfiniteModel = true;
	public bool IsInfiniteModel 
	{
		get => isInfiniteModel;
		set 
		{
			isInfiniteModel = value;
			if (value)
			{
				Switch2InfiniteModel();
			}
			else 
			{
				ClearAll();
			}
		}
	}

	public bool ShowIndexPoints = true;
	public bool IndexPointTouchable = true;

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

	public List<Page> Pages;
	public List<CustomToggle> Points;

	public void Awake()
	{
		scroll = GetComponent<ScrollRect>();
		scroll.vertical = false;
		scroll.horizontal = false;

		content = scroll.content;
		content.GetComponent<HorizontalLayoutGroup>().enabled = !isInfiniteModel;
		content.GetComponent<ContentSizeFitter>().enabled = !isInfiniteModel;

		Points = new List<CustomToggle>();
		Pages = new List<Page>();

		rectTrans = scroll.GetComponent<RectTransform>();
		curIndex = 0;

		isMoving = false;
	}

	public void Start()
	{
		if (!isInfiniteModel)
			return;

		Switch2InfiniteModel();
	}

	public void Switch2InfiniteModel() 
	{
		ShowIndexPoints = false;
		IndexPointTouchable = false;

		for (int i = 0; i < PageCount; i++)
		{
			generatePage();
		}
	}

	public Page GetCurPage() 
	{
		if (Pages.Count <= 0)
			return null;

		return Pages[curIndex];
	}

	// 无限模式下无法增加新的页面
	public Page AddPage()
	{
		if (isInfiniteModel)
			return null;

		generatePage();
		addIndexPoint();
		fitContent();

		return Pages[Pages.Count - 1];
	}

	private void generatePage()
	{
		var newPage = Instantiate(PagePrefab);
		newPage.transform.SetParent(content);
		newPage.transform.localScale = Vector3.one;
		newPage.GetComponent<RectTransform>().sizeDelta = new Vector2(rectTrans.rect.width, rectTrans.rect.height);

		if (isInfiniteModel)
		{
			newPage.GetComponent<RectTransform>().anchorMax = Vector2.one * 0.5f;
			newPage.GetComponent<RectTransform>().anchorMin = Vector2.one * 0.5f;
			newPage.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(Pages.Count * rectTrans.rect.width, 0, 0);
		}

		Pages.Add(newPage);
	}

	private void fitContent() 
	{
		content.anchoredPosition3D = new Vector3(Pages.Count % 2 != 0 ? Pages.Count / 2 * rectTrans.rect.width : (Pages.Count - 1) / 2f * rectTrans.rect.width, 0, 0);
	}

	// 减少 page 数量
	public void PopPage()
	{
		RemovePageByIndex(curIndex);
		JumpToPageByIndex(curIndex);
	}

	public void RemovePageByIndex(int index) 
	{
		if (index > Pages.Count || index < 0) 
		{
			Debug.LogWarning("Array exception");
			return;
		}

		if (isInfiniteModel)
		{
			// ----------------- temporary ---------------------
			if (Pages.Count != PageCount)
			{
				Destroy(Pages[index].gameObject);
				Pages.RemoveAt(index);
			}

			JumpToPageByIndex(curIndex);
			return;
		}

		Destroy(Pages[index].gameObject);
		Pages.RemoveAt(index);

		Points.RemoveAt(index);
		for (int i = index; i < Points.Count; i++)
		{
			Points[i].Index = i;
		}
	}

	public void ClearAll() 
	{
		Points.ForEach(point => 
		{
			Destroy(point.gameObject);	
		});

		Points.Clear();

		Pages.ForEach(page => 
		{
			Destroy(page.gameObject);
		});

		Pages.Clear();

		curIndex = 0;
	}

	private void addIndexPoint() 
	{
		var newPoint = Instantiate(IndexPointPrefab);
		newPoint.transform.SetParent(Toggles.transform);
		newPoint.group = Toggles;
		newPoint.transform.localScale = Vector3.one;
		newPoint.isOn = false;
		newPoint.manager = this;
		newPoint.Index = Points.Count;
		newPoint.interactable = IndexPointTouchable;
		var child = newPoint.GetComponentsInChildren<Image>();

		for (int i = 0; i < child.Length; i++)
			child[i].enabled = ShowIndexPoints;

		Points.Add(newPoint);
	}

	public void NextPage() 
	{
		if (isMoving)
			return;

		isMoving = true;
		int index = curIndex + 1;

		if (index > Pages.Count)
			return;

		if (isInfiniteModel)
			moveToPage(index);
		else
			JumpToPageByIndex(index);
	}

	public void LastPage() 
	{
		if (isMoving)
			return;

		isMoving = true;
		int index = curIndex - 1;

		if (index < 0)
			return;

		if (isInfiniteModel)
			moveToPage(index, -1);
		else
			JumpToPageByIndex(index);
	}

	public void JumpToPageByIndex(int index) 
	{
		if (curIndex == index) 
		{
			if (!isInfiniteModel)
				Points[curIndex].isOn = true;

			return;
		}

		Points.ForEach(point => 
		{
			if (point.Index == index)
			{
				point.isOn = true;
				moveToPage(index);
				// move to current
				curIndex = index;
			}
			else
				point.isOn = false;
		});
	}

	private void moveToPage(int index, float isNext = 1) 
	{
		if (isInfiniteModel) 
		{
			var p1 = Pages[curIndex % 2];
			var p2 = Pages[1 - curIndex % 2];

			var r1 = p1.GetComponent<RectTransform>();
			var r2 = p2.GetComponent<RectTransform>();

			r2.anchoredPosition = new Vector2(isNext * r2.sizeDelta.x, 0);

			Sequence seq = DOTween.Sequence();
			seq.Append(r1.DOAnchorPosX(isNext * - r1.sizeDelta.x, PageSlideInterval));
			seq.Join(r2.DOAnchorPosX(0, PageSlideInterval));
			seq.AppendCallback(() => 
			{
				isMoving = false;
				curIndex += (int)isNext;
			});
		}
		else 
		{
			var page = Pages[index];

			var tx = (Pages.Count % 2 != 0 ? (Pages.Count / 2 - index) : ((Pages.Count - 1) / 2f - index)) * rectTrans.rect.width;
			DOTween.To(() => content.anchoredPosition.x, end =>
			{
				content.anchoredPosition3D = new Vector3(end, 0, 0);
			}, tx, Mathf.Clamp(PageSlideInterval * Mathf.Abs(curIndex - index), PageSlideInterval, 1.5f)).OnComplete(() => 
			{
				isMoving = false;
			});
		}
	}
}
