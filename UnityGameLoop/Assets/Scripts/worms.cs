﻿using UnityEngine;
using System.Collections;
using System;
using System.IO.Ports;
using System.IO;



public class worms : MonoBehaviour {

	public MovieTexture timeTexture;
	public GameObject[] charaktere;
	//public Dictionary<GameObject,int> charaktereMap;

	private int anzPersonen;

	private bool personIn;
	private bool personOut;
	private bool removeAll = false;

	public bool isTalking;

	public GUIText guitext;
	public GUISkin guiSkin;

	public enum Status {
		Idle,
		Warten,
		Beginn,
		Abbruch
	}

	public Status status;

	GameObject pl_begin;
	GameObject pl_abbruch;

	private float oscZeitstempel;
	private float letzterZeitstempel;
	private float letzterSprachZeitstempel;

	System.Random rand;

	void OnGUI() {

		Vector2 pivot = new Vector2(Screen.width*0.1f, Screen.height*0.1f);
		//Vector2 pivot = new Vector2(80, 80);
		GUIUtility.RotateAroundPivot(-90, pivot);
		GUI.skin = guiSkin;
		if(status == Status.Warten)
			GUI.Label(new Rect(pivot.x-50, pivot.y-10, 100, 20), Mathf.RoundToInt(120 - Time.time + (float)letzterZeitstempel).ToString()); 
		else
			GUI.Label(new Rect(pivot.x-50, pivot.y-10, 100, 20), "120"); 
	}

	// Use this for initialization
	void Start() {
		GameObject pl_timer = GameObject.Find("pl_timer");
		pl_timer.renderer.material.mainTexture = timeTexture;

		pl_begin = GameObject.Find("pl_begin");
		setVisibility(pl_begin, false);
		pl_abbruch = GameObject.Find("pl_abbruch");
		setVisibility(pl_abbruch, false);

		// blende alle Charaktere aus
		foreach (GameObject charakter in charaktere) {
			setVisibility(charakter, false);
		}

		status = Status.Idle;
		letzterZeitstempel = Time.time;
		oscZeitstempel = Time.time;

		rand = new System.Random();

		guitext.transform.position = new Vector3(-7.25f, 3.5f, -0.2f);
		guitext.text = Mathf.RoundToInt(120 - Time.time + (float)letzterZeitstempel).ToString();
		guitext.enabled = true;

	}

	// Update is called once per frame
	void Update () {

		// Tastenabfrage
		if (Input.GetKeyUp(KeyCode.F1)) {
			//setMovTexture(1);
			//status = Status.Idle;
			personIn = true;
		}
		else if (Input.GetKeyUp(KeyCode.F2)) {
			//setMovTexture(2);
			//status = Status.Aufmerksam;
			personOut = true;
		}
		else if(Input.GetKeyUp(KeyCode.A)){
			// trigger Auswurf
			triggerDrucker();
		}

		//guitext.text = Mathf.RoundToInt(120 - Time.time + (float)letzterZeitstempel).ToString();
		guitext.text = "120";


		// GameLoop
		switch(status) {
		case Status.Idle :
			idle();
			break;
		case Status.Warten :
			warten();
			break;
		case Status.Beginn :
			begin();
			break;
		case Status.Abbruch :
			abbruch();
			break;
		}
	}
	
	/*private void setMovTexture(int nr, bool loop) {
		renderer.material.mainTexture = movTexture;
		movTexture.loop = loop;
		movTexture.Play();
	}*/

	private void setIdle() {
		status = Status.Idle;
		foreach(GameObject charakter in charaktere) {
			charakter.GetComponent<Charakter>().resetCharakter();
		}
		isTalking = false;
		timeTexture.Stop();
		setVisibility(pl_begin, false);
		setVisibility(pl_abbruch, false);
		Debug.Log("Status.IDLE");
	}

	private void idle() {
		// versetze Charaktere in den Ausgangszustand
		/*if(removeAll) {
			foreach(GameObject charakter in charaktere) {
				charakter.GetComponent<Charakter>().resetCharakter();
			}
			removeAll = false;
		}*/

		// gehe in Warten-Modus sobald erste Person erkannt bzw. starte Spiel
		/*if (personIn) {
			status = Status.Warten;
			letzterZeitstempel = Time.time;
			timeTexture.Play();
		}*/
	}

	private void checkZeitLetzteOscMessage(float sec) {
		// wenn 4 sec kein Gesicht mehr erkannt wurde -> brich Spiel ab
		if(Time.time - oscZeitstempel > sec) {
			setIdle();
		}
	}

	private void setWarten() {
		status = Status.Warten;
		letzterZeitstempel = Time.time;
		letzterSprachZeitstempel = Time.time;
		timeTexture.Play();
	}

	private void warten() {
		// wenn 4 sec kein Gesicht mehr erkannt wurde -> brich Spiel ab
		checkZeitLetzteOscMessage(4.0f);

		/*if (personIn) {
			// Protokollant immer zuerst
			if (anzPersonen == 0) {
				//setTransparenz(charaktere[0], 0.5f);
			} else {

				//setTransparenz(charaktere[anzPersonen], 0.5f);
				//TODO Einblenden nach Richtung
			}

			anzPersonen++;

			personIn = false;
		}*/

		/*if (personOut) {
			anzPersonen--;
			//setTransparenz(charaktere[anzPersonen], -0.5f);

			personOut = false;
			//if(anzPersonen == 0)
			//	status = Status.Idle;
		}*/

		if ((Time.time - letzterZeitstempel) > 120/* && anzPersonen < charaktere.Length*/) {
			setAbbruch();
		}

		if((Time.time - letzterSprachZeitstempel) > 5.0f) {
			if(!isTalking) {
				ArrayList list = new ArrayList();
				for(int i=0; i<charaktere.Length; i++) {
					if(charaktere[i].GetComponent<Charakter>().getFaceID() != -1)
						list.Add(i);
				}
				charaktere[(int)list[rand.Next(0, list.Count)]].GetComponent<Charakter>().playSpeech("idle");
			}
			letzterSprachZeitstempel = Time.time;
		}

		// wenn Anzahl der Personen erreicht, beginnt Verhandlung
		/*if (anzPersonen >= charaktere.Length) {
			status = Status.Beginn;
			timeTexture.Pause();
			sounds[0].Play();
		}*/
		// wenn nicht, breche Verhandlung ab

	}

	// trigger Auswurfmechanismus
	private void triggerDrucker() {
		string path = "auswurf_port.txt";
		try {
			string[] filecontent = File.ReadAllLines(path);
			Debug.Log(filecontent[0]);
			Debug.Log(Convert.ToInt32(filecontent[1]));
			
			#region Druckeransteuerung
			// create SerialPort("COM3", 9600);
			SerialPort port = new SerialPort(filecontent[0], Convert.ToInt32(filecontent[1]));
			try
			{
				port.Open();
				port.Write("1"); // "write something, nevermind what
			}
			catch (Exception e)
			{
				Debug.Log(e);
			}
			finally
			{
				port.Close();
			}
			#endregion
		}
		catch (Exception e) {
			Debug.Log("no access file "+path+e);
		}
	}

	private void setBegin() {
		status = Status.Beginn;
		timeTexture.Pause();
		setVisibility(pl_begin, true);
		triggerDrucker();
	}
	
	private void begin() {
		if(!isTalking) {
			this.gameObject.GetComponents<AudioSource>()[0].Play();
			isTalking = true;
		}

		checkZeitLetzteOscMessage(4.0f);

		/*Debug.Log("Begin");

		if (personOut) {
			if(anzPersonen != 0)
				anzPersonen--;
			
			personOut = false;
			if(anzPersonen == 0) {
				status = Status.Idle;
				timeTexture.Stop();
				setVisibility(pl_begin, false);
			}
		}*/
	}

	private void setAbbruch() {
		status = Status.Abbruch;
		setVisibility(pl_abbruch, true);
	}

	private void abbruch() {
		checkZeitLetzteOscMessage(4.0f);
		/*Debug.Log("Abbruch");


		if (personOut) {
			if(anzPersonen != 0)
				anzPersonen--;
			
			personOut = false;
			if(anzPersonen == 0) {
				status = Status.Idle;
				timeTexture.Stop();
				setVisibility(pl_abbruch, false);
			}
		}*/
	}

	// Hide/Unhide Objects
	private void setVisibility(GameObject pl, bool a) {
		Color color = pl.renderer.material.color;
		if(a) color.a = 1.0f;
		else color.a = 0.0f;
		pl.renderer.material.color = color;
	}

	// aktualisiere Bewegung des Charakters
	public void handleFacelist(int id, int mfactor) {
		oscZeitstempel = Time.time;
		foreach(GameObject charakter in charaktere) {
			if(charakter.GetComponent<Charakter>().getFaceID() == id) {
				charakter.GetComponent<Charakter>().setMotionFactor(mfactor);
				break;
			}
		}
	}

	// neues Gesicht wurde erkannt
	public void triggerPersonIn(int id) {
		oscZeitstempel = Time.time;

		if(!(status == Status.Idle || status == Status.Warten)) return;

		// Protokollant aktiv?
		if(charaktere[0].GetComponent<Charakter>().getFaceID() != -1) {
			// Kaiser UND Johannes aktiv?
			if(charaktere[1].GetComponent<Charakter>().getFaceID() != -1 && charaktere[2].GetComponent<Charakter>().getFaceID() != -1) {
				// wenn Luther nicht aktiv (für Sonderfall: neue Person erscheint während Fadevorgang)
				if(charaktere[3].GetComponent<Charakter>().getFaceID() == -1) {
					aktiviereCharakter(3, id);
					// Endlosschleife, um Status erst nach Fade-Ende zu setzen
					//while (charaktere[3].GetComponent<Charakter>().isFading) {}
					setBegin();
				}
			}
			else {
				// Kaiser ODER Johannes aktiv?
				if(charaktere[1].GetComponent<Charakter>().getFaceID() != -1 || charaktere[2].GetComponent<Charakter>().getFaceID() != -1) {
					// Kaiser aktiv?
					if(charaktere[1].GetComponent<Charakter>().getFaceID() != -1) {
						aktiviereCharakter(2, id);
					}
					else {
						aktiviereCharakter(1, id);
					}
				}
				else {
					aktiviereCharakter(rand.Next(1,3), id);
				}
			}
		}
		else {
			if(status == Status.Idle)
				setWarten();
			aktiviereCharakter(0, id);
		}
	}

	// Hilfsmethode zum aktivieren der Charaktere
	private void aktiviereCharakter(int c, int id) {
		if(!isTalking) {
			charaktere[c].GetComponent<Charakter>().playSpeech("in");
		}
		charaktere[c].GetComponent<Charakter>().setFaceID(id);
		charaktere[c].GetComponent<Charakter>().fade(0.5f);
		
		personIn = true;
	}

	// Gesicht wird nicht mehr erkannt
	public void triggerPersonOut(int id) {
		oscZeitstempel = Time.time;
		//if(id > charaktere.Length - 1) return;

		// versetze Charakter in Ausgangszustand
		foreach(GameObject charakter in charaktere) {
			if(charakter.GetComponent<Charakter>().getFaceID() == id) {
				if(status == Status.Beginn || status == Status.Abbruch)
					charakter.GetComponent<Charakter>().setMotionFactor(0);
				else {
					charakter.GetComponent<Charakter>().resetCharakter();
					if(!isTalking) {
						charakter.GetComponent<Charakter>().playSpeech("out");
					}
				}
				break;
			}
		}

		personOut = true;
	}
}
