using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

public class Charakter : MonoBehaviour {

	public AudioClip[] audioclips;					// Sprachsamples
	private Dictionary<string, AudioSource> speech;	// Sprachsamples mit Tags gemapt

	public GameObject pl_arrow;	// geschweifte Klammer
	public GameObject pl_text;	// Schriftzug des Namens

	private Vector3 pl_text_start_pos;	// Startposition des Schriftzuges
	public float pl_arrow_Xoffset;		// Verschiebung vom Nullpunkt

	private int faceID;		// Zuweisung des Gesichtes (durch oscHandler gegeben)

	public Vector2 range = new Vector2(-5.0f, 5.0f);	// Bewegungsbereich
	public float maxTextX = -3.5f;	// maximale Bewegung des Textes, je nach Orientierung der Klammer

	// fuer Animationen: Pruefvariablen und Animationsparameter
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

	private string keyPlaying = "";	// Key fuer Sprachsample-Map

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

	// Spiele zufaelligen Sound aus gegebener Kategorie ab
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

	// Blinkanimation
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

	// Textblinken bei Aktivierung Charakter
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
