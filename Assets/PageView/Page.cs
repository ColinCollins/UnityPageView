using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Custom page content
public class Page : MonoBehaviour
{
	public void UpdateData(PageDataHandle data) 
	{
		GetComponent<Image>().color = data.Color;
	}
}
