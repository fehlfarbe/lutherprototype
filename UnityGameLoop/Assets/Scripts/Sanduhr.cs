using UnityEngine;
using System.Collections;

public class Sanduhr : MonoBehaviour {

	public float startY;
	public float endY;
	private bool isMoving = false;
	private bool isRotating = false;
	private float moveValue;
	private float rotValue;
	public AudioSource sotSound;
	public AudioSource sandSound;
	private float volumeAdd = 0.0f;

	private Quaternion startQuat;

	private float lastTimeStamp;

	// Use this for initialization
	void Start () {
		this.transform.position = new Vector3(this.transform.position.x, startY, this.transform.position.z);
		sotSound = this.GetComponents<AudioSource>()[0];
		sandSound = this.GetComponents<AudioSource>()[1];
		sandSound.volume = 0.5f;
		lastTimeStamp = Time.time;
		startQuat = this.transform.rotation;
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

		if(isRotating) {
			if ((Time.time - lastTimeStamp) > 0.25f) {
				rotValue = -rotValue;
				lastTimeStamp = Time.time;
			}
//			
			this.transform.Rotate(transform.forward, rotValue * sotSound.volume * Time.deltaTime, Space.World);
		}

		if(sotSound.isPlaying) {
			sotSound.volume += volumeAdd * Time.deltaTime;
		}
	}

	public void move(float f) {
		moveValue = f;
		isMoving = true;
	}

	public void playSandOfTime(float f) {
		volumeAdd = f;
		sotSound.volume = 0.0f;
		sotSound.Play();
	}

	public void playSand() {
		sandSound.Play();
	}

	public void rotateSandUhr(bool t) {
		rotValue = 30f;
		isRotating = t;

		if (!t) {
			this.transform.rotation = startQuat;
		}
		//Vector3.RotateTowards(this.transform.rotation, this.transform.rotation, 30f, 30f);
		//this.transform.Rotate(Vector3.forward, 30f);
		//yield return new WaitForSeconds(0.25f);
		//this.transform.Rotate(Vector3.forward, -30f);
		//Vector3.RotateTowards(this.transform.rotation, this.transform.rotation, - 60f, 30f, 30f);
	}

	public void stopSounds() {
		sotSound.Stop();
		sandSound.Stop();
	}
}
