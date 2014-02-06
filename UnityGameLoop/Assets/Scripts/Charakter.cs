using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

public class Charakter : MonoBehaviour {

	public AudioClip[] audioclips;
	private Dictionary<string, AudioSource> speech;

	public GameObject pl_arrow;
	public GameObject pl_text;

	private Vector3 pl_text_start_pos;
	public float pl_arrow_Xoffset;

	private int faceID;

	public bool fromLeft;

	private Vector2 position;

	public float initialPos = 0.0f;
	public Vector2 range = new Vector2(-5.0f, 5.0f);
	public float maxTextX = -3.5f;

	public bool isFading = false;
	private bool isLooking = false;
	private bool isTextMoving = false;
	public bool isTextFading = false;
	private float fadeValue;
	private float lookValue;
	private float textMoveValue;
	private float textFadeValue;

	private int motionFactor = 0;
	private float transformFactor = 0.1f;
	private bool isReseting = false;
	private float xpos;

	private string keyPlaying = "";

	// Use this for initialization
	void Start () {
		// inaktiv
		faceID = -1;

		// transformFactor aus config auslesen
		string path = GameObject.Find("init").GetComponent<worms>().configpath;
		try {
			string[] filecontent = File.ReadAllLines(path);
			//Debug.Log(Convert.ToSingle(filecontent[7]));
			transformFactor = Convert.ToSingle(filecontent[7]);
		}
		catch (Exception e) {
			Debug.Log("no access file "+path+e);
		}

		// ausblenden
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

		pl_text_start_pos = new Vector3(pl_text.transform.position.x, pl_text.transform.position.y, pl_text.transform.position.z);
	}

	// Update is called once per frame
	void Update () {

		// Tastenabfrage
		if (Input.GetKeyUp(KeyCode.UpArrow)) {
			transformFactor += 0.002f;
			Debug.Log("Plus transform "+ transformFactor);
		}
		else if (Input.GetKeyUp(KeyCode.DownArrow)) {
			transformFactor -= 0.002f;
		}

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
			//pl_text.renderer.material.SetFloat("_Blend", blend);
			if(blend == 1.0f || blend == 0.0f) {
				isFading = false;
			}
		}
		// Text-Ein-/Ausblend-Animation
		if(isTextFading) {
			float incr = textFadeValue * Time.deltaTime;
			float blend = 1.0f - pl_text.renderer.material.GetFloat("_Blend2");
			if(blend + incr > 1.0f) 
				blend = 1.0f;
			else if(blend + incr < 0.0f)
				blend = 0.0f;
			else 
				blend += incr;
			pl_text.renderer.material.SetFloat("_Blend2", 1.0f - blend);
			if(blend == 1.0f || blend == 0.0f) {
				isTextFading = false;
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

			pl_text.transform.position = Vector3.MoveTowards(pl_text.transform.position, pl_arrow.transform.position + new Vector3(pl_arrow_Xoffset,0,0), incr);

//			float x = pl_text.transform.position.x;
//			float blend = x - incr;
//			if(blend > 0.0f) {
//				x = 0.0f;
//				pl_text.transform.position = new Vector3(x, pl_text.transform.position.y, pl_text.transform.position.z);
//			}
//			else if(blend < maxTextX) {
//				x = maxTextX;
//				pl_text.transform.position = new Vector3(x, pl_text.transform.position.y, pl_text.transform.position.z);
//			}
//			else 
//				pl_text.transform.Translate(-incr, 0, 0, Space.World);			
			if(pl_text.transform.position.x == pl_arrow.transform.position.x + pl_arrow_Xoffset) {
				//isTextMoving = false;
			}
			/*float incr = textMoveValue * Time.deltaTime;
			float x = pl_text.transform.position.x;
			float blend = x - incr;
			if(blend > 0.0f) {
				x = 0.0f;
				pl_text.transform.position = new Vector3(x, pl_text.transform.position.y, pl_text.transform.position.z);
			}
			else if(blend < maxTextX) {
				x = maxTextX;
				pl_text.transform.position = new Vector3(x, pl_text.transform.position.y, pl_text.transform.position.z);
			}
			else 
				pl_text.transform.Translate(-incr, 0, 0, Space.World);
			if(blend == maxTextX || blend == 0.0f) {
				isTextMoving = false;
			}*/
		}
		// Bewegung anhand der User
		if(faceID != -1 || isReseting) {
			float diffrange = (range.y - range.x) / 10;
			this.transform.Translate(0.0f, transformFactor * diffrange * -motionFactor * Time.deltaTime, 0.0f, Space.World);
			if(isReseting) {
				if(motionFactor < 0 && this.transform.position.y > 0)
					isReseting = false;
				else if(motionFactor > 0 && this.transform.position.y < 0)
					isReseting = false;
			}

			pl_arrow.transform.position = new Vector3(pl_arrow.transform.position.x, -xpos * 7.2f + 3.6f, pl_arrow.transform.position.z);
			//Debug.Log(xpos);
		}
		// Fertig mit reden?
		if(!keyPlaying.Equals("")) {
			if(!speech[keyPlaying].isPlaying) {
				GameObject.Find("init").GetComponent<worms>().isTalking = false;
				GameObject.Find("init").GetComponent<worms>().setRandomWartezeit();
				keyPlaying = "";
				look(-1.2f);
			}
		}
	}

	// Spiele zufälligen Sound aus gegebener Kategorie ab
	public void playSpeech(string key) {
		int i = 1;
		bool listEnd = false;
		ArrayList list = new ArrayList();
		while (!listEnd) {
			string s = key + i.ToString();
			i++;
			//Debug.Log("/"+s+"/");
			if(speech.ContainsKey(s)) {
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

	// Spiele angegebenen Sound ab
	public void playSpecialSpeech(string key) {
		if(speech.ContainsKey(key)) {
			speech[key].Play();
			keyPlaying = key;
			GameObject.Find("init").GetComponent<worms>().isTalking = true;
			look(1.2f);
		}
	}

	private IEnumerator blinkText(int count, float speed) {
		for (int i = 0; i < count; i++) {
			pl_text.renderer.material.SetFloat("_Blend", 1.0f);
			yield return new WaitForSeconds(speed);
			pl_text.renderer.material.SetFloat("_Blend", 0.0f);
			yield return new WaitForSeconds(speed);
		}
		pl_text.renderer.material.SetFloat("_Blend", 1.0f);

		animateText(3.0f);
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

	public void fadeText(float f) {
		textFadeValue = f;
		isTextFading = true;
	}

	public void startBlink(int count, float speed) {
		StartCoroutine(blinkText(count, speed));
		isTextFading = false;
		isTextMoving = false;
		pl_text.transform.position = pl_text_start_pos;
		pl_text.renderer.material.SetFloat("_Blend2", 0.0f);
	}

	public void fadeArrow(float f) {
		pl_arrow.GetComponent<TextElement>().fade(f);
	}

	public void animateText(float f) {
		textMoveValue = f;
		isTextMoving = true;
	}

	public void setMotionFactor(int f) {
		motionFactor = f;
	}

	public void setXPos(float f) {
		xpos = f;
		//Vector3 newpos = new Vector3(pl_arrow.transform.position.x, -xpos * 7.2f + 3.1f, pl_arrow.transform.position.z);
		//pl_arrow.transform.position = Vector3.Lerp(pl_arrow.transform.position, newpos, Time.deltaTime);
	}

	// versetze Charakter in Ausgangszustand
	public void resetCharakter() {
		setFaceID(-1);
		fade(-0.5f);
		fadeText(-0.5f);
		fadeArrow(-0.5f);
		//animateText(-2.0f);
		if(this.transform.position.y < -0.2f || this.transform.position.y > 0.2f) {
			int mf = (int)((this.transform.position.y < 0) ? -2/transformFactor : 2/transformFactor);
			setMotionFactor(mf);
			isReseting = true;
		}
	}
}
