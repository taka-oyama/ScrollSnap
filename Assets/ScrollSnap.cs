using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

[RequireComponent(typeof(ScrollRect))]
public class ScrollSnap : MonoBehaviour, IDragHandler, IEndDragHandler {
	[SerializeField] public int currentIndex = 0;
	[SerializeField] public float lerpTimeMilliSeconds = 200f;
	[SerializeField] public float triggerPercent = 20f;
	[Range(0.0f, 10.0f)] public float triggerAcceleration = 2f;
	
	public delegate void LerpDelegate(Vector2 targetPosition, float transition, int direction);
	public LerpDelegate onLerp;

	ScrollRect scrollRect;
	GridLayoutGroup layoutGroup;
	int dragDirection;
	bool isFlipTriggered = false;
	bool isLerping = false;
	DateTime lerpStartedAt;
	Vector2 releasedPosition;
	Vector2 targetPosition;

	void Start() {
		this.scrollRect = GetComponent<ScrollRect>();
		this.layoutGroup = scrollRect.content.GetComponent<GridLayoutGroup>();
		
		// setup initial position
		scrollRect.content.anchoredPosition = new Vector2(
			-layoutGroup.cellSize.x * currentIndex,
			scrollRect.content.anchoredPosition.y
		);
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
			isFlipTriggered = true;
		}
		dragDirection = dx < 0f ? 1 : -1;
	}

	public void OnEndDrag(PointerEventData data) {
		if(IndexShouldChange(data)) {
			int newIndex = Mathf.Max(currentIndex + dragDirection, 0);
			var maxIndex = scrollRect.content.GetComponentsInChildren<LayoutElement>().Length - 1;

			// when it's the same it means it tried to go out of bounds
			if(newIndex >= 0 && newIndex <= maxIndex) {
				currentIndex = newIndex;
			}
		}
		releasedPosition = scrollRect.content.anchoredPosition;
		targetPosition = CalculateTargetPoisition();
		lerpStartedAt = DateTime.Now;
		isLerping = true;
	}

	bool IndexShouldChange(PointerEventData data) {
		if(isFlipTriggered) {
			isFlipTriggered = false;
			return true;
		}
		return scrollRect.horizontalNormalizedPosition * 100f > triggerPercent;
	}

	void LerpToElement() {
		float t = (float)((DateTime.Now - lerpStartedAt).TotalMilliseconds / lerpTimeMilliSeconds);
		float newX = Mathf.Lerp(releasedPosition.x, targetPosition.x, t);
		if(onLerp != null) {
			onLerp(targetPosition, t, dragDirection);
		}
		scrollRect.content.anchoredPosition = new Vector2(newX, scrollRect.content.anchoredPosition.y);
	}

	Vector2 CalculateTargetPoisition() {
		return new Vector2(-layoutGroup.cellSize.x * currentIndex, scrollRect.content.anchoredPosition.y);
	}

	bool ShouldStopLerping() {
		return Mathf.Approximately(scrollRect.content.anchoredPosition.x, targetPosition.x);
	}
}
