using UnityEngine;
using System.Collections;

public class Charakter : MonoBehaviour {

	private int faceID;

	public bool fromLeft;

	private Vector2 position;

	public float initialPos = 0.0f;
	public Vector2 range = new Vector2(-5.0f, 5.0f);

	private bool isFading;
	private float fadeValue;

	private int motionFactor = 0;

	// Use this for initialization
	void Start () {
		faceID = -1;
	}
	
	// Update is called once per frame
	void Update () {
		if(isFading) {
			Color color = this.renderer.material.color;
			float incr = fadeValue * Time.deltaTime;
			if(color.a + incr > 1.0f) 
				color.a = 1.0f;
			else if( color.a + incr < 0.0f)
				color.a = 0.0f;
			else 
				color.a += incr;
			this.renderer.material.color = color;
			if(color.a == 1.0f || color.a == 0.0f) {
				isFading = false;
			}
			if(color.a == 0.0f)
				this.transform.position = new Vector3(this.transform.position.x, 0, this.transform.position.z);
		}

		if(motionFactor != 0) {
			float diffrange = (range.y - range.x) / 10;
			this.transform.Translate(0.0f, 0.1f * diffrange * -motionFactor * Time.deltaTime, 0.0f, Space.World);
		}
	}

	public void setFaceID(int id) {
		faceID = id;
	}

	public int getFaceID() {
		return faceID;
	}

	public void fade(float f) {
		fadeValue = f;
		isFading = true;
	}

	public void setMotionFactor(int f) {
		motionFactor = f;
	}

	public void resetCharakter() {
		setFaceID(-1);
		fade(-0.5f);
		setMotionFactor(0);
	}
}
