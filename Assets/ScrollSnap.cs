using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using UnityEngine.Events;

[DisallowMultipleComponent]
[ExecuteInEditMode]
[RequireComponent(typeof(ScrollRect))]
public class ScrollSnap : UIBehaviour, IDragHandler, IEndDragHandler {
	[SerializeField] public int currentIndex = 0;
	[SerializeField] public bool wrapAround = false;
	[SerializeField] public float lerpTimeMilliSeconds = 200f;
	[SerializeField] public float triggerPercent = 10f;
	[Range(0f, 10f)] public float triggerAcceleration = 1f;

	public UnityEvent onLerpComplete;
	public class OnIndexChangeEvent : UnityEvent<int> {}
	public OnIndexChangeEvent onIndexChanged;

	ScrollRect scrollRect;
	CanvasGroup canvasGroup;
	RectTransform content;
	Vector2 cellSize;
	bool indexChangeTriggered = false;
	bool isLerping = false;
	DateTime lerpStartedAt;
	Vector2 releasedPosition;
	Vector2 targetPosition;

	protected override void Start() {
		base.Start();
		this.onLerpComplete = new UnityEvent();
		this.onIndexChanged = new OnIndexChangeEvent();
		this.scrollRect = GetComponent<ScrollRect>();
		this.canvasGroup = GetComponent<CanvasGroup>();
		this.content = scrollRect.content;
		this.cellSize = content.GetComponent<GridLayoutGroup>().cellSize;
		content.anchoredPosition = new Vector2(-cellSize.x * currentIndex, content.anchoredPosition.y);		
		SetContentSize(LayoutElementCount());
	}

	void LateUpdate() {
		if(isLerping) {
			LerpToElement();
			if(ShouldStopLerping()) {
				isLerping = false;
				canvasGroup.blocksRaycasts = true;
				onLerpComplete.Invoke();
				onLerpComplete.RemoveListener(WrapElementAround);
			}
		}
	}
	
	public void PushLayoutElement(LayoutElement element) {
		element.transform.SetParent(content.transform, false);
		SetContentSize(LayoutElementCount());
	}
	
	public void PopLayoutElement() {
		LayoutElement[] elements = content.GetComponentsInChildren<LayoutElement>();
		Destroy(elements[elements.Length - 1].gameObject);
		SetContentSize(LayoutElementCount() - 1);
		if(currentIndex == CalculateMaxIndex()) {
			currentIndex -= 1;
		}
	}
	
	public void UnshiftLayoutElement(LayoutElement element) {
		currentIndex += 1;
		element.transform.SetParent(content.transform, false);
		element.transform.SetAsFirstSibling();
		SetContentSize(LayoutElementCount());
		content.anchoredPosition = new Vector2(content.anchoredPosition.x - cellSize.x, content.anchoredPosition.y);
	}
	
	public void ShiftLayoutElement() {
		Destroy(GetComponentInChildren<LayoutElement>().gameObject);
		SetContentSize(LayoutElementCount() - 1);
		currentIndex -= 1;
		content.anchoredPosition = new Vector2(content.anchoredPosition.x + cellSize.x, content.anchoredPosition.y);
	}
	
	public int LayoutElementCount() {
		return content.GetComponentsInChildren<LayoutElement>().Length;
	}
	
	public void OnDrag(PointerEventData data) {
		float dx = data.delta.x;
		float dt = Time.deltaTime * 1000f;
		float acceleration = Mathf.Abs(dx / dt);
		if(acceleration > triggerAcceleration && acceleration != Mathf.Infinity) {
			indexChangeTriggered = true;
		}
	}

	public void OnEndDrag(PointerEventData data) {
		if(IndexShouldChangeFromDrag(data)) {
			int direction = (data.pressPosition.x - data.position.x) > 0f ? 1 : -1;			
			int newIndex = Mathf.Max(currentIndex + direction, 0);
			int maxIndex = CalculateMaxIndex();
			if(wrapAround && maxIndex > 0) {
				currentIndex = newIndex;
				onLerpComplete.AddListener(WrapElementAround);
			} else {
				// when it's the same it means it tried to go out of bounds
				if(newIndex >= 0 && newIndex <= maxIndex) {
					currentIndex = newIndex;
				}
			}
			onIndexChanged.Invoke(currentIndex);
		}
		LerpToIndex(currentIndex);
	}

	public void LerpToIndex(int index) {
		releasedPosition = content.anchoredPosition;
		targetPosition = CalculateTargetPoisition(index);
		lerpStartedAt = DateTime.Now;
		canvasGroup.blocksRaycasts = false;
		isLerping = true;
	}

	int CalculateMaxIndex() {
		int cellPerFrame = Mathf.FloorToInt(scrollRect.GetComponent<RectTransform>().sizeDelta.x / cellSize.x);
		return LayoutElementCount() - cellPerFrame;
	}

	bool IndexShouldChangeFromDrag(PointerEventData data) {
		// acceleration was above threshold
		if(indexChangeTriggered) {
			indexChangeTriggered = false;
			return true;
		}
		// dragged beyond trigger threshold
		var offset = scrollRect.content.anchoredPosition.x + currentIndex * cellSize.x;
		var normalizedOffset = Mathf.Abs(offset / cellSize.x);
		return normalizedOffset * 100f > triggerPercent;
	}

	void LerpToElement() {
		float t = (float)((DateTime.Now - lerpStartedAt).TotalMilliseconds / lerpTimeMilliSeconds);
		float newX = Mathf.Lerp(releasedPosition.x, targetPosition.x, t);
		content.anchoredPosition = new Vector2(newX, content.anchoredPosition.y);
	}

	void WrapElementAround() {
		if(currentIndex <= 0) {
			var elements = content.GetComponentsInChildren<LayoutElement>();
			elements[elements.Length - 1].transform.SetAsFirstSibling();
			currentIndex += 1;
			content.anchoredPosition = new Vector2(content.anchoredPosition.x - cellSize.x, content.anchoredPosition.y);
		}
		if(currentIndex >= CalculateMaxIndex()) {
			var element = content.GetComponentInChildren<LayoutElement>();
			element.transform.SetAsLastSibling();
			currentIndex -= 1;
			content.anchoredPosition = new Vector2(content.anchoredPosition.x + cellSize.x, content.anchoredPosition.y);
		}
	}
	
	void SetContentSize(int elementCount) {
		content.sizeDelta = new Vector2(cellSize.x * elementCount, content.rect.height);
	}
	
	Vector2 CalculateTargetPoisition(int index) {
		return new Vector2(-cellSize.x * index, content.anchoredPosition.y);
	}

	bool ShouldStopLerping() {
		return Mathf.Approximately(content.anchoredPosition.x, targetPosition.x);
	}
}
