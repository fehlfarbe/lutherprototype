using UnityEngine;
using System.Collections;
using System;
using System.IO.Ports;
using System.IO;


public class worms : MonoBehaviour {
	
	public GameObject[] charaktere;		// die 4 Charaktere Verknuepfung mit den GameObjects
	public GameObject[] timerSprites;	// Zahlen-Sprites fuer Timer
	public GameObject[] zuschauer;		// verschiedene Zustaende des Publikums

	private int anzPersonen;

	public bool isTalking;
	private bool isFinalTextVorbei = false;

	// einzelne Zustaende des Gameloops
	public enum Status {
		Idle,
		Warten,
		Beginn,
		Abbruch,
		Intro
	}
	public Status status;	// aktueller Status

	private int wartezeit = 120;	// Dauer des Timers (aus config)
	private string timeouttyp;		// Easteregg oder Normal (aus config)

	private float oscZeitstempel;			// letzter Zeitpunkt, an dem oscHandler getriggert wurde
	private float letzterZeitstempel;		// Zeitpunkt, wenn Timer anfaengt zu zaehlen
	public float letzterSprachZeitstempel;	// Zeitpunkt, an dem zuletzt Sprachsample gespielt wurde

	private float randWaitSec;				// zufalliger Wartebereich zwischen Sprachsamples
	System.Random rand;

	public string configpath = "worms_config.txt";


	// Use this for initialization
	void Start() {
		readConfig();

		// blende alle Charaktere aus
		foreach (GameObject charakter in charaktere) {
			setVisibility(charakter, false);
		}

		GameObject.Find("pl_Poster").GetComponent<TextElement>().fade(0.5f);

		anzPersonen = 0;

		status = Status.Idle;
		letzterZeitstempel = Time.time;
		oscZeitstempel = Time.time;

		rand = new System.Random();

		logStatus("starte");
		logStatus("Idle");
	}

	// Update is called once per frame
	void Update () {

		// Tastenabfrage
		if (Input.GetKeyUp(KeyCode.Escape)) {
			// Spiel beenden
			logStatus("beende\r\n");
			Application.Quit();
		}
		else if(Input.GetKeyUp(KeyCode.F5)){
			// Sanduhr
			setUhr("sanduhr");
		}
		else if(Input.GetKeyUp(KeyCode.F6)){
			// Timer
			setUhr("timer");
		}
		else if(Input.GetKeyUp(KeyCode.B)){
			setBegin();
		}
		else if(Input.GetKeyUp(KeyCode.A)){
			// trigger Auswurf
			triggerDrucker();
		}
		else if(Input.GetKeyUp(KeyCode.U)){
			// trigger Auswurf
			readConfig();
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
		case Status.Intro:
			intro();
			break;
		}
	}

	// config auslesen
	private void readConfig() {
		// Wartezeit und Uhr aus config auslesen
		try {
			string[] filecontent = File.ReadAllLines(configpath);
			Debug.Log(Convert.ToInt32(filecontent[5]) + " sec");
			wartezeit = Convert.ToInt32(filecontent[5]);
			setUhr(filecontent[9]);
			timeouttyp = filecontent[11];
		}
		catch (Exception e) {
			Debug.Log("no access file "+configpath+e);
		}
	}

	// schreibe Log mit Systemzeit in Datei
	private void logStatus(string s) {
		string path = "log.txt";
		string content = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + s + "\r\n";
		File.AppendAllText(path, content);
	}

	// entweder Timer oder Sanduhr einblenden
	private void setUhr(string typ) {
		if(typ.Equals("timer")) {
			GameObject.Find("timer_sanduhr").renderer.enabled = false;
			timerSprites[0].renderer.enabled = true;
			timerSprites[1].renderer.enabled = true;
			timerSprites[2].renderer.enabled = true;
			timerSprites[3].renderer.enabled = true;
		}
		else {
			GameObject.Find("timer_sanduhr").renderer.enabled = true;
			timerSprites[0].renderer.enabled = false;
			timerSprites[1].renderer.enabled = false;
			timerSprites[2].renderer.enabled = false;
			timerSprites[3].renderer.enabled = false;
		}
	}

	// entweder Timeout oder Hoeneß einblenden
	private void fadeTimeout(string typ, float f) {
		if(typ.Equals("hoeness")) {
			GameObject.Find("timeout_easteregg").GetComponent<TextElement>().fade(f);
		}
		else {
			GameObject.Find("timeout").GetComponent<TextElement>().fade(f);
		}
	}

	// initialisieren Idle-Status
	private void setIdle() {
		status = Status.Idle;
		logStatus("Idle");
		foreach(GameObject charakter in charaktere) {
			charakter.GetComponent<Charakter>().resetCharakter();
			//charakter.GetComponent<Charakter>().fadeText(-0.5f);
		}
		isTalking = false;
		isFinalTextVorbei = false;
		hideWarteTexte(true);
		GameObject.Find("timer_sanduhr").GetComponent<Sanduhr>().stopSounds();
		anzPersonen = 0;

		zuschauer[0].GetComponent<TextElement>().fade(-0.5f);
		zuschauer[1].GetComponent<TextElement>().fade(-0.5f);
		zuschauer[2].GetComponent<TextElement>().fade(-0.5f);

		GameObject.Find("pl_Poster").GetComponent<TextElement>().fade(0.5f);
		GameObject.Find("pl_Poster_timeout").GetComponent<TextElement>().fade(-0.5f);
		GameObject.Find("pl_Poster_infos").GetComponent<TextElement>().fade(-0.5f);
		fadeTimeout(timeouttyp, -0.5f);
	}

	// Idle-Routine
	private void idle() {

	}

	// prüfe, wie lange es her ist ist, seit der Facedetector das letzte Mal eine Message gesendet hat
	private void checkZeitLetzteOscMessage(float sec) {
		// wenn 4 sec kein Gesicht mehr erkannt wurde -> brich Spiel ab
		if(Time.time - oscZeitstempel > sec) {
			setIdle();
		}
	}

	// initialisieren Intro-Status
	private void setIntro() {
		status = Status.Intro;
		logStatus("Intro");
		StartCoroutine(playIntro());
	}

	// Intro-Routine
	private void intro() {

	}

	// Intro-Animation
	private IEnumerator playIntro() {
		// Phase 1: Intro1 abspielen
		charaktere[0].GetComponent<Charakter>().playSpecialSpeech("intro1");
		//Debug.Log("intro1");
		yield return new WaitForSeconds(3.5f);

		// Phase 2: Poster ausblenden
		//Namen einblenden (inkl. anwesend/abwesen)
		/*foreach(GameObject c in charaktere) {
			c.GetComponent<Charakter>().fadeText(0.5f);
		}*/
		//GameObject.Find("pl_text_anwesend").GetComponent<TextElement>().fade(0.5f);
		//GameObject.Find("pl_text_abwesend").GetComponent<TextElement>().fade(0.5f);
		GameObject.Find("pl_Poster").GetComponent<TextElement>().fade(-0.5f);
		GameObject.Find("pl_Poster_infos").GetComponent<TextElement>().fade(0.5f);

		//Debug.Log("intro2");
		yield return new WaitForSeconds(0.5f);

		// Phase 3: Intro2 abspielen
		charaktere[0].GetComponent<Charakter>().playSpecialSpeech("intro2");
		//Debug.Log("intro3");
		yield return new WaitForSeconds(1.5f);

		// Phase 4: Warten starten
		if(status == Status.Intro) {
			setWarten();
			//Debug.Log("warten");
		}
	}

	// initialisieren Warten-Status
	private void setWarten() {
		status = Status.Warten;
		logStatus("Warten");
		letzterZeitstempel = Time.time;
		letzterSprachZeitstempel = Time.time;

		// Timer einblenden
		timerSprites[0].GetComponent<TextElement>().fade(1.5f);
		timerSprites[1].GetComponent<TextElement>().fade(1.5f);
		timerSprites[2].GetComponent<TextElement>().fade(1.5f);
		timerSprites[3].GetComponent<TextElement>().fade(1.5f);
		GameObject.Find("timer_sanduhr").GetComponent<Sanduhr>().move(2.0f);
		GameObject.Find("timer_sanduhr").GetComponent<Sanduhr>().playSand();
		GameObject.Find("pl_mapbalken").GetComponent<TextElement>().fade(0.5f);

		for(int i=0; i<charaktere.Length; i++) {
			if(charaktere[i].GetComponent<Charakter>().getFaceID() != -1) {
				charaktere[i].GetComponent<Charakter>().fadeArrow(0.5f);
				charaktere[i].GetComponent<Charakter>().startBlink(4, 0.25f);
				//charaktere[i].GetComponent<Charakter>().animateText(2.0f);
			}
		}
	}

	// Warten-Routine
	private void warten() {

		int t0 = (int)(wartezeit - Time.time + letzterZeitstempel) % 10;
		int t1 = (int)((wartezeit - Time.time + letzterZeitstempel) / 10) % 6;
		int t2 = (int)((wartezeit - Time.time + letzterZeitstempel) / 60) % 10;
		timerSprites[0].GetComponent<Sprites>().setIndex(t0);
		timerSprites[1].GetComponent<Sprites>().setIndex(t1);
		timerSprites[2].GetComponent<Sprites>().setIndex(t2);

		int ts = (int)((1 - (wartezeit - Time.time + letzterZeitstempel) / wartezeit) * 10);
		GameObject.Find("timer_sanduhr").GetComponent<Sprites>().setIndex(ts);

		if(Time.time - letzterZeitstempel > wartezeit/1.5f && 
		   !GameObject.Find("timer_sanduhr").GetComponent<Sanduhr>().sotSound.isPlaying) {
			GameObject.Find("timer_sanduhr").GetComponent<Sanduhr>().playSandOfTime(3f/wartezeit);
			GameObject.Find("timer_sanduhr").GetComponent<Sanduhr>().rotateSandUhr(true);
		}

		// wenn 4 sec kein Gesicht mehr erkannt wurde -> brich Spiel ab
		checkZeitLetzteOscMessage(4.0f);

		if ((Time.time - letzterZeitstempel) > wartezeit) {
			setAbbruch();
		}

		if((Time.time - letzterSprachZeitstempel) > randWaitSec) {
			if(!isTalking) {
				if(Time.time - letzterZeitstempel < wartezeit/3f) {
					// z1 -> im ersten Drittel der Wartezeit
					sprecheNachAnzPersonen("z1");
					Debug.Log("z1");
				}
				else if(Time.time - letzterZeitstempel < wartezeit/1.5f) {
					// z2 -> im zweiten Drittel der Wartezeit
					sprecheNachAnzPersonen("z2");
					Debug.Log("z2");
				}
				else if(Time.time - letzterZeitstempel < wartezeit - 5f){
					// z3 -> im letzten Drittel der Wartezeit, aber nicht 5 sec vor Schluss
					sprecheNachAnzPersonen("z3");
					Debug.Log("z3");
				}
			}
			setRandomWartezeit();
		}

	}

	// zufälliger aktiver Charakter spricht abhängig von Anzahl der Personen unterschiedliche Texte
	private void sprecheNachAnzPersonen(string z) {
		ArrayList list = new ArrayList();
		for(int i=0; i<charaktere.Length; i++) {
			if(charaktere[i].GetComponent<Charakter>().getFaceID() != -1)
				list.Add(i);
		}

		if(anzPersonen == 1) {
			charaktere[(int)list[rand.Next(0, list.Count)]].GetComponent<Charakter>().playSpecialSpeech(z+"nr1");
		}
		else if(anzPersonen == 2) {
			charaktere[(int)list[rand.Next(0, list.Count)]].GetComponent<Charakter>().playSpecialSpeech(z+"nr2");
		}
		else if(anzPersonen == 3) {
			charaktere[(int)list[rand.Next(0, list.Count)]].GetComponent<Charakter>().playSpecialSpeech(z+"nr3");
		}
		//charaktere[(int)list[rand.Next(0, list.Count)]].GetComponent<Charakter>().playSpeech("idle");
	}

	// Ebenen, die waehrend des Warten-Zustandes zu sehen sind, werden ausgeblendet
	private void hideWarteTexte(bool inklc) {
		timerSprites[0].GetComponent<TextElement>().fade(-1.5f);
		timerSprites[1].GetComponent<TextElement>().fade(-1.5f);
		timerSprites[2].GetComponent<TextElement>().fade(-1.5f);
		timerSprites[3].GetComponent<TextElement>().fade(-1.5f);
		GameObject.Find("timer_sanduhr").GetComponent<Sanduhr>().move(-2.0f);
		GameObject.Find("timer_sanduhr").GetComponent<Sanduhr>().rotateSandUhr(false);

		if(inklc) {
			foreach(GameObject c in charaktere) {
				c.GetComponent<Charakter>().fadeText(-0.5f);
				c.GetComponent<Charakter>().fadeArrow(-0.5f);
			}
			GameObject.Find("pl_mapbalken").GetComponent<TextElement>().fade(-0.5f);
		}
		//GameObject.Find("pl_text_anwesend").GetComponent<TextElement>().fade(-0.5f);
		//GameObject.Find("pl_text_abwesend").GetComponent<TextElement>().fade(-0.5f);

	}
	
	// zufaelliges Redeintervall
	public void setRandomWartezeit() {
		randWaitSec = rand.Next(500, 1000) / 100;
		letzterSprachZeitstempel = Time.time;
	}

	// trigger Auswurfmechanismus
	private void triggerDrucker() {
		try {
			string[] filecontent = File.ReadAllLines(configpath);
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
			Debug.Log("no access file "+configpath+e);
		}
	}

	// triggerDrucker aller einer Sekunde nach Anzahl der Personen 
	private IEnumerator auswurfQueue(int anz) {
		for(int i=0; i<anz; i++) {
			triggerDrucker();
			yield return new WaitForSeconds(1.0f);
		}
	}

	// initialisieren Begin-Status (bzw. Erfolg)
	private void setBegin() {
		status = Status.Beginn;
		logStatus("Erfolg");
		hideWarteTexte(false);
		GameObject.Find("timer_sanduhr").GetComponent<Sanduhr>().stopSounds();
	}

	// Verhandlungsanimation
	private IEnumerator animateVerhandlung() {
		yield return new WaitForSeconds(2.0f);
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
		yield return new WaitForSeconds(1.5f);
		hideWarteTexte(true);
		GameObject.Find("pl_Poster").GetComponent<TextElement>().fade (0.5f);
		GameObject.Find("pl_Poster_infos").GetComponent<TextElement>().fade(-0.5f);
		StartCoroutine(auswurfQueue(anzPersonen));
		isFinalTextVorbei = true;
		oscZeitstempel = Time.time;
	}

	// Begin-Routine
	private void begin() {
		if(!isTalking) {
			this.gameObject.GetComponents<AudioSource>()[0].Play();
			isTalking = true;
			StartCoroutine(animateVerhandlung());
		}

		if(isFinalTextVorbei) {
			checkZeitLetzteOscMessage(4.0f);
		}
	}

	// initialisieren Abbruch-Status (bzw. Timeout)
	private void setAbbruch() {
		status = Status.Abbruch;
		logStatus("Timeout");
		hideWarteTexte(true);
		fadeTimeout(timeouttyp, 3.0f);
		GameObject.Find("timer_sanduhr").GetComponent<Sanduhr>().stopSounds();
		// play timeout sound
		this.gameObject.GetComponents<AudioSource>()[1].Play();
		GameObject.Find("pl_Poster_timeout").GetComponent<TextElement>().fade(0.5f);
		GameObject.Find("pl_Poster_infos").GetComponent<TextElement>().fade(-0.5f);
		StartCoroutine(auswurfQueue(anzPersonen));
	}

	// Abbruch-Routine
	private void abbruch() {
		checkZeitLetzteOscMessage(4.0f);
	}

	// Hide/Unhide Objects
	private void setVisibility(GameObject pl, bool a) {
		Color color = pl.renderer.material.color;
		if(a) color.a = 1.0f;
		else color.a = 0.0f;
		pl.renderer.material.color = color;
	}

	// aktualisiere Bewegung des Charakters (wird von oscHandler aufgerufen)
	public void handleFacelist(int id, int xpos, int mfactor) {
		oscZeitstempel = Time.time;

		float xrelpos = xpos / 640f;

		foreach(GameObject charakter in charaktere) {
			if(charakter.GetComponent<Charakter>().getFaceID() == id) {
				charakter.GetComponent<Charakter>().setMotionFactor(mfactor);
				charakter.GetComponent<Charakter>().setXPos(xrelpos);
				break;
			}
		}
	}

	// neues Gesicht wurde erkannt (wird von oscHandler aufgerufen)
	public void triggerPersonIn(int id) {
		oscZeitstempel = Time.time;

		logStatus("PersonIn");

		if(!(status == Status.Idle || 
		     status == Status.Warten ||
		     status == Status.Intro)) 
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
				aktiviereCharakter(0, id, true); // aktiviere Protokollant (quiet)
				setIntro();
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
		charaktere[c].GetComponent<Charakter>().fade(1.5f);
		if (status == Status.Warten) {
			charaktere[c].GetComponent<Charakter>().fadeArrow(0.5f);
			charaktere[c].GetComponent<Charakter>().startBlink(4, 0.25f);
			//charaktere[c].GetComponent<Charakter>().animateText(2.0f);
		}
		// Zuschauer einblenden
		anzPersonen++;
		if(anzPersonen > 1) {
			zuschauer[anzPersonen-2].GetComponent<TextElement>().fade(0.5f);
		}
	}

	// Gesicht wird nicht mehr erkannt (wird von oscHandler aufgerufen)
	public void triggerPersonOut(int id) {
		oscZeitstempel = Time.time;

		logStatus("PersonOut");

		foreach(GameObject charakter in charaktere) {
			if(charakter.GetComponent<Charakter>().getFaceID() == id) {
				// wenn status Beginn oder Abbruch, noch nicht ausblenden
				if(status == Status.Beginn || status == Status.Abbruch) {
					//charakter.GetComponent<Charakter>().setMotionFactor(0);
					charakter.GetComponent<Charakter>().setFaceID(-1);
				}
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
					// Zuschauer ausblenden
					if(anzPersonen > 1) {
						zuschauer[anzPersonen-2].GetComponent<TextElement>().fade(-0.5f);
					}
					anzPersonen--;

				}
				break;
			}
		}
	}

}
