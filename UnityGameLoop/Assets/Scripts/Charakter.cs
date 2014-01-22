using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Charakter : MonoBehaviour {

	public AudioClip[] audioclips;
	private Dictionary<string, AudioSource> speech;

	public GameObject pl_text;

	private int faceID;

	public bool fromLeft;

	private Vector2 position;

	public float initialPos = 0.0f;
	public Vector2 range = new Vector2(-5.0f, 5.0f);

	public bool isFading = false;
	private bool isLooking = false;
	private bool isTextMoving = false;
	private float fadeValue;
	private float lookValue;
	private float textMoveValue;
	private int motionFactor = 0;

	private string keyPlaying = "";

	// Use this for initialization
	void Start () {
		faceID = -1;

		this.renderer.material.SetFloat("_Blend2", 1.0f);

		// initialisiere AudioSources
		speech = new Dictionary<string, AudioSource>();
		if(audioclips.Length == 0) return;
		foreach(AudioClip ac in audioclips) {
			AudioSource asource = this.gameObject.AddComponent<AudioSource>();
			asource.clip = ac;
			asource.playOnAwake = false;
			string key = ac.name.Substring(ac.name.LastIndexOf("_") + 1);
			speech.Add(key, asource);
		}
	}

	// Update is called once per frame
	void Update () {
		/*if(isFading) {
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
			if(color.a == 0.0f)
				this.transform.position = new Vector3(this.transform.position.x, 0, this.transform.position.z);
		}*/
		// Ein-/Ausblend-Animation
		if(isFading) {
			float incr = fadeValue * Time.deltaTime;
			float blend = 1.0f - this.renderer.material.GetFloat("_Blend2");
			if(blend + incr > 1.0f) 
				blend = 1.0f;
			else if(blend + incr < 0.0f)
				blend = 0.0f;
			else 
				blend += incr;
			this.renderer.material.SetFloat("_Blend2", 1.0f - blend);
			pl_text.renderer.material.SetFloat("_Blend", blend);
			if(blend == 1.0f || blend == 0.0f) {
				isFading = false;
			}
		}
		// Rede-Animation
		if(isLooking) {
			float incr = lookValue * Time.deltaTime;
			float blend = this.renderer.material.GetFloat("_Blend");
			if(blend + incr > 1.0f) 
				blend = 1.0f;
			else if(blend + incr < 0.0f)
				blend = 0.0f;
			else 
				blend += incr;
			this.renderer.material.SetFloat("_Blend", blend);
			if(blend == 1.0f || blend == 0.0f) {
				isLooking = false;
			}
		}
		// Textanimation
		if(isTextMoving) {
			float incr = textMoveValue * Time.deltaTime;
			float x = pl_text.transform.position.x;
			float blend = x - incr;
			if(blend > 0.0f) {
				x = 0.0f;
				pl_text.transform.position.Set(x, pl_text.transform.position.y, pl_text.transform.position.z);
			}
			else if(blend < -3.5f) {
				x = -3.5f;
				pl_text.transform.position.Set(x, pl_text.transform.position.y, pl_text.transform.position.z);
			}
			else 
				pl_text.transform.Translate(-incr, 0, 0, Space.World);
			if(blend == -3.5f || blend == 0.0f) {
				isTextMoving = false;
			}
		}

		if(motionFactor != 0) {
			float diffrange = (range.y - range.x) / 10;
			this.transform.Translate(0.0f, 0.1f * diffrange * -motionFactor * Time.deltaTime, 0.0f, Space.World);
		}

		if(!keyPlaying.Equals("")) {
			if(!speech[keyPlaying].isPlaying) {
				Debug.Log("isplaying false");
				GameObject.Find("init").GetComponent<worms>().isTalking = false;
				keyPlaying = "";
				look(-1.2f);
			}
		}
	}

	public void playSpeech(string key) {
		int i = 1;
		bool listEnd = false;
		ArrayList list = new ArrayList();
		while (!listEnd) {
			string s = key + i.ToString();
			i++;
			Debug.Log("/"+s+"/");
			Debug.Log("speech " + speech);
			if(speech.ContainsKey(s)) {
				Debug.Log("add");
				list.Add(s);
			} else
				listEnd = true;
		}

		if(list.Count == 0) return;

		System.Random rand = new System.Random();
		string rkey = list[rand.Next(0,list.Count)].ToString();
		speech[rkey].Play();
		keyPlaying = rkey;
		GameObject.Find("init").GetComponent<worms>().isTalking = true;
		look(1.2f);
	}

	public void setFaceID(int id) {
		faceID = id;
	}

	public int getFaceID() {
		return faceID;
	}

	public void look(float f) {
		lookValue = f;
		isLooking = true;
	}

	public void fade(float f) {
		fadeValue = f;
		isFading = true;
	}

	public void animateText(float f) {
		textMoveValue = f;
		isTextMoving = true;
	}

	public void setMotionFactor(int f) {
		motionFactor = f;
	}

	public void resetCharakter() {
		setFaceID(-1);
		fade(-0.5f);
		animateText(-2.0f);
		setMotionFactor(0);
	}
}
