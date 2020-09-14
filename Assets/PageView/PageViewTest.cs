using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class PageViewTest : MonoBehaviour
{
    public PageView pageView;

    public Button Left;
    public Button Right;

	public void Start()
	{
		Left.onClick.AddListener(() => {
			pageView.NextPage();
		});

		Right.onClick.AddListener(() => {
			pageView.LastPage();
		});

		pageView.Init();
		List<PageDataHandle> pages = new List<PageDataHandle>();
		for (int i = 0; i < 40; i++) 
		{
			var data = new PageDataHandle();
			data.Init();
			pages.Add(data);
		}

		pageView.AddPages(pages);

	}
}
