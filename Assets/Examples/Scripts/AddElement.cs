using UnityEngine;
using UnityEngine.UI;
using System;

public class AddElement : MonoBehaviour {
	[SerializeField] LayoutElement layoutElementPrefab;

	public void Awake() {
		Button button = GetComponent<Button>();
		button.onClick.AddListener(() => {
			var canvas = GetComponentInParent<Canvas>();
			var snap = canvas.GetComponentInChildren<ScrollSnap>();
			snap.PushLayoutElement(Instantiate(layoutElementPrefab));
		});
	}
}
