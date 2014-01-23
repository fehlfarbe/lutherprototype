using UnityEngine;
using System.Collections;

public class TextElement : MonoBehaviour {

	private bool isFading = false;
	private float fadeValue;

	// Use this for initialization
	void Start () {
		// erstmal ausblenden
		Color color = this.renderer.material.color;
		color.a = 0.0f;
		this.renderer.material.color = color;
	}
	
	// Update is called once per frame
	void Update () {
		// Ein-/Ausblend-Animation
		if(isFading) {
			Color color = this.renderer.material.color;
			float incr = fadeValue * Time.deltaTime;
			if(color.a + incr > 1.0f) 
				color.a = 1.0f;
			else if(color.a + incr < 0.0f)
				color.a = 0.0f;
			else 
				color.a += incr;
			this.renderer.material.color = color;
			if(color.a == 1.0f || color.a == 0.0f) {
				isFading = false;
			}
		}
	}

	public void fade(float f) {
		fadeValue = f;
		isFading = true;
	}
}
