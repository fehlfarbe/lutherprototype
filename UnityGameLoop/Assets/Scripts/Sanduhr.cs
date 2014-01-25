using UnityEngine;
using System.Collections;

public class Sanduhr : MonoBehaviour {

	public float startY;
	public float endY;
	private bool isMoving = false;
	private float moveValue;

	// Use this for initialization
	void Start () {
		this.transform.position = new Vector3(this.transform.position.x, startY, this.transform.position.z);
	}
	
	// Update is called once per frame
	void Update () {
		// Textanimation
		if(isMoving) {
			float incr = moveValue * Time.deltaTime;
			float y = this.transform.position.y;
			float blend = y - incr;
			if(blend > startY) {
				y = startY;
				this.transform.position = new Vector3(this.transform.position.x, y, this.transform.position.z);
			}
			else if(blend < endY) {
				y = endY;
				this.transform.position = new Vector3(this.transform.position.x, y, this.transform.position.z);
			}
			else 
				this.transform.Translate(0, -incr, 0, Space.World);
			if(blend == startY || blend == endY) {
				isMoving = false;
			}
		}
	}

	public void move(float f) {
		moveValue = f;
		isMoving = true;
	}
}
