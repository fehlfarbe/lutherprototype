using UnityEngine;
using System.Collections;
using System;
using System.IO.Ports;
using System.IO;



public class worms_backup : MonoBehaviour {

	public GameObject[] charaktere;
	//public Dictionary<GameObject,int> charaktereMap;

	public GameObject[] timerSprites;

	private int anzPersonen;

	public bool isTalking;
	private bool isFinalTextVorbei = false;

	public enum Status {
		Idle,
		Warten,
		Beginn,
		Abbruch,
		Intro1,
		Intro2,
		Intro3
	}

	public Status status;

	GameObject pl_begin;
	GameObject pl_abbruch;

	public int wartezeit = 120;

	private float oscZeitstempel;
	private float letzterZeitstempel;
	public float letzterSprachZeitstempel;

	private float randWaitSec;
	System.Random rand;


	// Use this for initialization
	void Start() {
		// Wartezeit aus config auslesen
		string path = "worms_config.txt";
		try {
			string[] filecontent = File.ReadAllLines(path);
			Debug.Log(Convert.ToInt32(filecontent[1]) + " sec");
			wartezeit = Convert.ToInt32(filecontent[1]);
		}
		catch (Exception e) {
			Debug.Log("no access file "+path+e);
		}

		pl_begin = GameObject.Find("pl_begin");
		setVisibility(pl_begin, false);
		pl_abbruch = GameObject.Find("pl_abbruch");
		setVisibility(pl_abbruch, false);
		setVisibility(GameObject.Find("pl_Poster"), false);

		// blende alle Charaktere aus
		foreach (GameObject charakter in charaktere) {
			setVisibility(charakter, false);
		}

		status = Status.Idle;
		letzterZeitstempel = Time.time;
		oscZeitstempel = Time.time;

		rand = new System.Random();


	}

	// Update is called once per frame
	void Update () {

		// Tastenabfrage
		if (Input.GetKeyUp(KeyCode.Escape)) {
			// Spiel beenden
			Application.Quit();
		}
		else if(Input.GetKeyUp(KeyCode.B)){
			setBegin();
		}
		else if(Input.GetKeyUp(KeyCode.A)){
			// trigger Auswurf
			triggerDrucker();
		}

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
		case Status.Intro1:
			intro1();
			break;
		case Status.Intro2:
			intro2();
			break;
		case Status.Intro3:
			intro3();
			break;
		}
	}

	private void setIdle() {
		status = Status.Idle;
		foreach(GameObject charakter in charaktere) {
			charakter.GetComponent<Charakter>().resetCharakter();
			charakter.GetComponent<Charakter>().fadeText(-0.5f);
		}
		isTalking = false;
		isFinalTextVorbei = false;
		hideWarteTexte();
		setVisibility(pl_begin, false);
		setVisibility(pl_abbruch, false);
		setVisibility(GameObject.Find("pl_Poster"), false);
	}

	private void idle() {

	}

	private void checkZeitLetzteOscMessage(float sec) {
		// wenn 4 sec kein Gesicht mehr erkannt wurde -> brich Spiel ab
		if(Time.time - oscZeitstempel > sec) {
			setIdle();
		}
	}
	
	private void playIntro(int id) {
		/**
		 * Phase 1: Protokollanten aktivieren Intro1 abspielen
		 * Phase 2: Namen einblenden (inl. anwesend/abwesen)
		 * Phase 3: Intro2 abspielen
		 * Phase 4: Timer einblenden und starten
		 **/
		aktiviereCharakter(0, id, true);
		charaktere[0].GetComponent<Charakter>().playSpecialSpeech("intro1");
		status = Status.Intro1;
		Debug.Log("intro1");
	}

	private void intro1() {
		if(!isTalking) {
			// Namen einblenden
			foreach(GameObject c in charaktere) {
				c.GetComponent<Charakter>().fadeText(0.5f);
			}
			GameObject.Find("pl_text_anwesend").GetComponent<TextElement>().fade(0.5f);
			GameObject.Find("pl_text_abwesend").GetComponent<TextElement>().fade(0.5f);
			status = Status.Intro2;
			Debug.Log("intro2");
		}
	}

	private void intro2() {
		if(!charaktere[0].GetComponent<Charakter>().isTextFading) {
			charaktere[0].GetComponent<Charakter>().playSpecialSpeech("intro2");
			status = Status.Intro3;
		}
	}

	private void intro3() {
		if(!isTalking) {
			// wenn alle schon aktiv, dann "super gemacht"
			setWarten();
			Debug.Log("warten");
		}
	}

	private void setWarten() {
		status = Status.Warten;
		letzterZeitstempel = Time.time;
		letzterSprachZeitstempel = Time.time;

		// Timer einblenden
		timerSprites[0].GetComponent<TextElement>().fade(1.5f);
		timerSprites[1].GetComponent<TextElement>().fade(1.5f);
		timerSprites[2].GetComponent<TextElement>().fade(1.5f);
		timerSprites[3].GetComponent<TextElement>().fade(1.5f);
	}


	private void warten() {

		int t0 = (int)(wartezeit - Time.time + (float)letzterZeitstempel) % 10;
		int t1 = (int)((wartezeit - Time.time + (float)letzterZeitstempel) / 10) % 6;
		int t2 = (int)((wartezeit - Time.time + (float)letzterZeitstempel) / 60) % 10;
		timerSprites[0].GetComponent<Sprites>().setIndex(t0);
		timerSprites[1].GetComponent<Sprites>().setIndex(t1);
		timerSprites[2].GetComponent<Sprites>().setIndex(t2);

		// wenn 4 sec kein Gesicht mehr erkannt wurde -> brich Spiel ab
		checkZeitLetzteOscMessage(4.0f);

		if ((Time.time - letzterZeitstempel) > wartezeit/* && anzPersonen < charaktere.Length*/) {
			setAbbruch();
		}

		if((Time.time - letzterSprachZeitstempel) > randWaitSec) {
			if(!isTalking) {
				ArrayList list = new ArrayList();
				for(int i=0; i<charaktere.Length; i++) {
					if(charaktere[i].GetComponent<Charakter>().getFaceID() != -1)
						list.Add(i);
				}
				charaktere[(int)list[rand.Next(0, list.Count)]].GetComponent<Charakter>().playSpeech("idle");
			}
			setRandomWartezeit();
		}

	}

	private void hideWarteTexte() {
		timerSprites[0].GetComponent<TextElement>().fade(-1.5f);
		timerSprites[1].GetComponent<TextElement>().fade(-1.5f);
		timerSprites[2].GetComponent<TextElement>().fade(-1.5f);
		timerSprites[3].GetComponent<TextElement>().fade(-1.5f);

		foreach(GameObject c in charaktere) {
			c.GetComponent<Charakter>().fadeText(-0.5f);
		}
		GameObject.Find("pl_text_anwesend").GetComponent<TextElement>().fade(-0.5f);
		GameObject.Find("pl_text_abwesend").GetComponent<TextElement>().fade(-0.5f);
	}
	
	// zufälliges Redeintervall
	public void setRandomWartezeit() {
		randWaitSec = rand.Next(500, 1000) / 100;
		letzterSprachZeitstempel = Time.time;
	}

	// trigger Auswurfmechanismus
	private void triggerDrucker() {
		string path = "auswurf_port.txt";
		try {
			string[] filecontent = File.ReadAllLines(path);
			Debug.Log(filecontent[1]);
			Debug.Log(Convert.ToInt32(filecontent[3]));
			
			#region Druckeransteuerung
			// create SerialPort("COM3", 9600);
			SerialPort port = new SerialPort(filecontent[1], Convert.ToInt32(filecontent[3]));
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
		setVisibility(pl_begin, true);
		triggerDrucker();
		hideWarteTexte();

	}

	// Verhandlungsanimation
	IEnumerator animateVerhandlung() {
		charaktere[0].GetComponent<Charakter>().look(1.2f);
		yield return new WaitForSeconds(3.0f);
		charaktere[0].GetComponent<Charakter>().look(-1.2f);
		charaktere[1].GetComponent<Charakter>().look(1.2f);
		yield return new WaitForSeconds(5.0f);
		charaktere[1].GetComponent<Charakter>().look(-1.2f);
		charaktere[3].GetComponent<Charakter>().look(1.2f);
		yield return new WaitForSeconds(5.0f);
		charaktere[3].GetComponent<Charakter>().look(-1.2f);
		charaktere[2].GetComponent<Charakter>().look(1.2f);
		yield return new WaitForSeconds(5.5f);
		charaktere[2].GetComponent<Charakter>().look(-1.2f);
	}
	
	private void begin() {
		if(!isTalking) {
			this.gameObject.GetComponents<AudioSource>()[0].Play();
			isTalking = true;
			isFinalTextVorbei = true;
			StartCoroutine(animateVerhandlung());
		}

		if(isFinalTextVorbei && !this.gameObject.GetComponents<AudioSource>()[0].isPlaying) {
			setVisibility(GameObject.Find("pl_Poster"), true);
		}

		checkZeitLetzteOscMessage(4.0f);
	}

	private void setAbbruch() {
		status = Status.Abbruch;
		setVisibility(pl_abbruch, true);
		hideWarteTexte();
		GameObject.Find("pl_Poster").GetComponent<TextElement>().fade(0.5f);
	}

	private void abbruch() {
		checkZeitLetzteOscMessage(4.0f);
		//Debug.Log("Abbruch");
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

		if(!(status == Status.Idle || 
		     status == Status.Warten || 
		     status == Status.Intro1 || 
		     status == Status.Intro2 || 
		     status == Status.Intro3)) 
			return;

		// Protokollant aktiv?
		if(charaktere[0].GetComponent<Charakter>().getFaceID() != -1) {
			// Kaiser UND Johannes aktiv?
			if(charaktere[1].GetComponent<Charakter>().getFaceID() != -1 && charaktere[2].GetComponent<Charakter>().getFaceID() != -1) {
				// wenn Luther nicht aktiv (für Sonderfall: neue Person erscheint während Fadevorgang)
				if(charaktere[3].GetComponent<Charakter>().getFaceID() == -1) {
					aktiviereCharakter(3, id, false); // aktiviere Luther
					setBegin();
				}
			}
			else {
				// Kaiser ODER Johannes aktiv?
				if(charaktere[1].GetComponent<Charakter>().getFaceID() != -1 || charaktere[2].GetComponent<Charakter>().getFaceID() != -1) {
					// Kaiser aktiv?
					if(charaktere[1].GetComponent<Charakter>().getFaceID() != -1) {
						aktiviereCharakter(2, id, false); // aktiviere Johannes
					}
					else {
						aktiviereCharakter(1, id, false); // aktiviere Kaiser
					}
				}
				else {
					aktiviereCharakter(rand.Next(1,3), id, false);	// aktiviere Kaiser oder Johannes
				}
			}
		}
		else {
			if(status == Status.Idle) {
				playIntro(id); // aktiviere Protokallant (in der playIntro)
			}
			else {
				aktiviereCharakter(0, id, false); // aktiviere Protokollant
			}
		}
	}

	// Hilfsmethode zum aktivieren der Charaktere
	private void aktiviereCharakter(int c, int id, bool quiet) {
		if(!isTalking && quiet == false && status == Status.Warten) {
			charaktere[c].GetComponent<Charakter>().playSpeech("in");
		}
		charaktere[c].GetComponent<Charakter>().setFaceID(id);
		charaktere[c].GetComponent<Charakter>().fade(0.5f);
		charaktere[c].GetComponent<Charakter>().animateText(2.0f);
	}

	// Gesicht wird nicht mehr erkannt
	public void triggerPersonOut(int id) {
		oscZeitstempel = Time.time;

		foreach(GameObject charakter in charaktere) {
			if(charakter.GetComponent<Charakter>().getFaceID() == id) {
				// wenn status Beginn oder Abbruch, noch nicht ausblenden
				if(status == Status.Beginn || status == Status.Abbruch)
					charakter.GetComponent<Charakter>().setMotionFactor(0);
				// versetze Charakter in Ausgangszustand
				else {
					charakter.GetComponent<Charakter>().resetCharakter();
					if(!isTalking) {
						// wenn Protokollant und alle anderen inaktiv, dann special out
						if(charakter == charaktere[0] && 
						   charaktere[1].GetComponent<Charakter>().getFaceID() == -1 && 
						   charaktere[2].GetComponent<Charakter>().getFaceID() == -1) {
							charakter.GetComponent<Charakter>().playSpecialSpeech("abbruch");
						}
						else {
							charakter.GetComponent<Charakter>().playSpeech("out");
						}
					}
				}
				break;
			}
		}
	}

}
