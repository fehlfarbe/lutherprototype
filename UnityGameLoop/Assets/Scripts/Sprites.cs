using UnityEngine;
using System.Collections;


public class Sprites : MonoBehaviour {

	public Sprite[] sprites;
	private SpriteRenderer spriteRenderer;

	public int standardIndex = 10;

	// Use this for initialization
	void Start () {
		spriteRenderer = renderer as SpriteRenderer;
		spriteRenderer.sprite = sprites[standardIndex];
	}
	
	// Update is called once per frame
	void Update () {

	}

	public void setIndex(int i) {
		spriteRenderer.sprite = sprites[i];
	}
}
