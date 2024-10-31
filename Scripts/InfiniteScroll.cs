using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using StylizedWater2;
using UnityEngine.UI;

public abstract class InfiniteScroll : ScrollRect
{
	[HideInInspector]
	public bool
		initOnAwake;//

	protected RectTransform t {
		get {
			if (_t == null)
				_t = GetComponent<RectTransform> ();
			return _t;
		}
	}

	private RectTransform _t;

	private RectTransform[] prefabItems;
	private int itemTypeStart = 0;
	private int itemTypeEnd = 0;

	public Text ContentPos;
	public Text ContentsizeDelta;
	public Text TsizeDelta;

	private bool init;

	private Vector2 dragOffset = Vector2.zero;

	#region abstracts	
	protected abstract float GetSize (RectTransform item);
	
	protected abstract float GetDimension (Vector2 vector);
	
	protected abstract Vector2 GetVector (float value);

	protected abstract float GetPos (RectTransform item);

	protected abstract int OneOrMinusOne ();
	#endregion

	#region core
	new void Awake ()
	{
		if (!Application.isPlaying)
			return;

		if (initOnAwake)
			Init ();
	}

	public void Init ()
	{
		init = true;

		//Creating an array of prefab items and disabling them

		var tempStack = new Stack<RectTransform> ();
		foreach (RectTransform child in content) {
			if (!child.gameObject.activeSelf)
				continue;
			tempStack.Push (child);
			child.gameObject.SetActive (false);
		}
		prefabItems = tempStack.ToArray ();

		float containerSize = 0;
		//Filling up the scrollview with initial items
		while (containerSize < GetDimension(t.sizeDelta)) {
			RectTransform nextItem = NewItemAtEnd ();
			containerSize += GetSize (nextItem);
		}
	}
	private void Update ()
	{
		if (!Application.isPlaying || !init)
			return;
		//content.sizeDelta 是LayoutGroup&LayoutElement&ContentSizeFitter算出来的Content的高/长度   450
		//content节点锚点在上中，初始化默认posY=0; 竖直滑动列表这个位置计算×1   0
		//GetDimension(t.sizeDelta) VertScroll节点的的高/长度  
		// 输出content的sizeDelta
		ContentsizeDelta.text = "Content sizeDelta y: " + content.sizeDelta.y;
		// 输出content的localPosition
		ContentPos.text = "ContentPos y: " + content.localPosition.y;
		TsizeDelta.text = "T sizeDelta.y: " + t.sizeDelta.y;
		// Debug.Log(gameObject.name + "  "+transform.localPosition.y);
		// 输出t的sizeDelta
		if (GetDimension (content.sizeDelta) - (GetDimension (content.localPosition) * OneOrMinusOne ()) < GetDimension (t.sizeDelta)) {
			Debug.Log("new item at end");
			NewItemAtEnd ();
			
			
			//margin is used to Destroy objects.边缘用作销毁对象
			//We add them at half the margin 我们在半边缘处（content.localPos&小于scroll高度的一半）添加它们
			//(if we do it at full margin, we continuously add and delete objects) 如果是全边缘，我们将持续地添加和删除对象
		} else if (GetDimension (content.localPosition) * OneOrMinusOne () < (GetDimension (t.sizeDelta) * 0.5f)) {
			Debug.Log("New Item At Start");
			NewItemAtStart ();
			
			
			//Using else because when items get added, sometimes the properties in UnityGUI are only updated at the end of the frame.
			//这里用else是因为当items被添加时，有时候UGUI属性会在每帧的末尾更新，如果没有新的item添加则只有销毁对象（当滑动很快的时候，对性能友好）
			//Only Destroy objects if nothing new was added (also nice performance saver while scrolling fast).
		} else {
			//Looping through all items.遍历所有的item
			foreach (RectTransform child in content) {
				//Our prefabs are inactive我们的预设是失活的
				if (!child.gameObject.activeSelf)
					continue;
				//We Destroy an item from the end if it's too far如果一个item太远了(坐标大于scroll的高度），我们就从尾端销毁它
				if (GetPos (child) > GetDimension (t.sizeDelta)) {
					Destroy (child.gameObject);
					//我们更新container的位置，因为在从顶部删除item后，container往上移动所有子内容
					//We update the container position, since after we delete something from the top, the container moves all of it's content up
					content.localPosition -= (Vector3)GetVector (GetSize (child));
					dragOffset -= GetVector (GetSize (child));//拖拽偏移？？？
					Add (ref itemTypeStart);//开始的元素索引
				} else if (GetPos (child) < -(GetDimension (t.sizeDelta) + GetSize (child))) {
					Destroy (child.gameObject);
					Subtract (ref itemTypeEnd);
				}
			}
		}
	}

	private RectTransform NewItemAtStart ()
	{
		Subtract (ref itemTypeStart);
		RectTransform newItem = InstantiateNextItem (itemTypeStart);
		newItem.SetAsFirstSibling ();

		content.localPosition += (Vector3)GetVector (GetSize (newItem));
		dragOffset += GetVector (GetSize (newItem));
		return newItem;
	}

	private RectTransform NewItemAtEnd ()
	{
		RectTransform newItem = InstantiateNextItem (itemTypeEnd);
		Add (ref itemTypeEnd);
		return newItem;
	}

	private RectTransform InstantiateNextItem (int itemType)
	{
		RectTransform nextItem = Instantiate (prefabItems [itemType]) as RectTransform;
		nextItem.name = prefabItems [itemType].name;
		nextItem.transform.SetParent (content.transform, false);
		nextItem.gameObject.SetActive (true);
		return nextItem;
	}
	#endregion

	#region overrides
	public override void OnBeginDrag (UnityEngine.EventSystems.PointerEventData eventData)
	{
		dragOffset = Vector2.zero;
		base.OnBeginDrag (eventData);
	}

	public override void OnDrag (UnityEngine.EventSystems.PointerEventData eventData)
	{
		//TEMP method until I found a better solution
		if (dragOffset != Vector2.zero) {
			OnEndDrag (eventData);
			OnBeginDrag (eventData);
			dragOffset = Vector2.zero;
		}

		base.OnDrag (eventData);
	}
	#endregion

	#region convenience


	private void Subtract (ref int i)
	{
		i--;
		if (i == -1) {
			i = prefabItems.Length - 1;
		}
	}

	private void Add (ref int i)
	{
		i ++;
		if (i == prefabItems.Length) {
			i = 0; 
		}
	}
	#endregion
}
