using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

[RequireComponent(typeof(ScrollRect))]
public class ScrollSnap : MonoBehaviour, IDragHandler, IEndDragHandler {
	[SerializeField] public int currentIndex = 0;
	[SerializeField] public float lerpTimeMilliSeconds = 200f;
	[SerializeField] public float triggerPercent = 20f;
	[Range(0f, 10f)] public float triggerAcceleration = 2f;
	
	public delegate void LerpDelegate(Vector2 targetPosition, float transition, int direction);
	public LerpDelegate onLerp;

	ScrollRect scrollRect;
	RectTransform content;
	Vector2 cellSize;
	int dragDirection;
	bool indexChangeTriggered = false;
	bool isLerping = false;
	DateTime lerpStartedAt;
	Vector2 releasedPosition;
	Vector2 targetPosition;

	void Start() {
		this.scrollRect = GetComponent<ScrollRect>();
		this.content = scrollRect.content;
		this.cellSize = content.GetComponent<GridLayoutGroup>().cellSize;
		// enforce content width
		content.sizeDelta = new Vector2(cellSize.x * ElementCount(), content.rect.width);
		// setup initial position
		content.anchoredPosition = new Vector2(-cellSize.x * currentIndex, content.anchoredPosition.y);
	}

	void LateUpdate() {
		if(isLerping) {
			LerpToElement();
			if(ShouldStopLerping()) {
				isLerping = false;
			}
		}
	}
	
	public void OnDrag(PointerEventData data) {
		float dx = data.delta.x;
		float dt = Time.deltaTime * 1000f;
		float acceleration = Mathf.Abs(dx / dt);
		if(acceleration > triggerAcceleration && acceleration != Mathf.Infinity) {
			indexChangeTriggered = true;
		}
		dragDirection = dx < 0f ? 1 : -1;
	}

	public void OnEndDrag(PointerEventData data) {
		if(IndexShouldChangeFromDrag(data)) {
			int newIndex = Mathf.Max(currentIndex + dragDirection, 0);
			int maxIndex = ElementCount() - 1;

			// when it's the same it means it tried to go out of bounds
			if(newIndex >= 0 && newIndex <= maxIndex) {
				currentIndex = newIndex;
			}
		}
		LerpToIndex(currentIndex);
	}

	public void LerpToIndex(int index) {
		releasedPosition = content.anchoredPosition;
		targetPosition = CalculateTargetPoisition(index);
		lerpStartedAt = DateTime.Now;
		isLerping = true;
	}

	bool IndexShouldChangeFromDrag(PointerEventData data) {
		// acceleration was above threshold
		if(indexChangeTriggered) {
			indexChangeTriggered = false;
			return true;
		}
		// dragged beyond trigger threshold
		return scrollRect.horizontalNormalizedPosition * 100f > triggerPercent;
	}

	void LerpToElement() {
		float t = (float)((DateTime.Now - lerpStartedAt).TotalMilliseconds / lerpTimeMilliSeconds);
		float newX = Mathf.Lerp(releasedPosition.x, targetPosition.x, t);
		if(onLerp != null) {
			onLerp(targetPosition, t, dragDirection);
		}
		content.anchoredPosition = new Vector2(newX, content.anchoredPosition.y);
	}

	Vector2 CalculateTargetPoisition(int index) {
		return new Vector2(-cellSize.x * index, content.anchoredPosition.y);
	}

	bool ShouldStopLerping() {
		return Mathf.Approximately(content.anchoredPosition.x, targetPosition.x);
	}

	int ElementCount() {
		return content.GetComponentsInChildren<LayoutElement>().Length;
	}
}
