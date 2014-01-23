using UnityEngine;
using System.Collections;
using System;
using System.IO.Ports;
using System.IO;
using System.Collections.Generic;

public class thesen : MonoBehaviour {

	public MovieTexture movTexture;
	public MovieTexture[] movarr;

	public AudioClip[] audioclips;

	private ArrayList sounds;

	public Status status;

	private System.Random rand;

	private int lastAudio = 0;

	public enum Status {
		Idle,
		Aufmerksam,
		Anschlagen,
		Reaktion,
		Schreck
	}

	private enum Bereich {
		Leer,
		Fern,
		Mittel,
		Nah
	}

	private Bereich aktuellerBereich = Bereich.Leer;
	private float letzterZeitstempel;
	private float oscZeitstempel;


	// Use this for initialization
	void Start() {
		sounds =  new ArrayList();

		rand = new System.Random();

		foreach(AudioClip ac in audioclips) {
			AudioSource asource = this.gameObject.AddComponent<AudioSource>();
			asource.clip = ac;
			asource.playOnAwake = false;
			sounds.Add(asource); 
		}


		//movTexture = Resources.Load("Vids/luther_hammer", typeof(MovieTexture)) as MovieTexture;
		setMovTexture(1, true, 0);
		status = Status.Idle;
		aktuellerBereich = Bereich.Leer;
		letzterZeitstempel = Time.time;
		oscZeitstempel = Time.time;

	}

	// Update is called once per frame
	void Update () {
		// Tastenabfrage
		if (Input.GetKeyUp(KeyCode.F1)) {
			//setMovTexture(1);
			//status = Status.Idle;
			setMovTexture(1, true, 0);
			aktuellerBereich = Bereich.Leer;
			letzterZeitstempel = Time.time;
			oscZeitstempel = 0;
			status = Status.Idle;
		}
		else if (Input.GetKeyUp(KeyCode.F2)) {
			setMovTexture(2, true, rand.Next(1,4));
			//setMovTexture(2);
			//status = Status.Aufmerksam;
			aktuellerBereich = Bereich.Fern;
			letzterZeitstempel = Time.time;
			oscZeitstempel = 0;
			status = Status.Aufmerksam;
		}
		else if (Input.GetKeyUp(KeyCode.F3)) {
			//setMovTexture(3);
			//status = Status.Idle;
			setMovTexture(3, true, 8);
			aktuellerBereich = Bereich.Mittel;
			letzterZeitstempel = Time.time;
			oscZeitstempel = 0;
			status = Status.Anschlagen;
		}
		else if (Input.GetKeyUp(KeyCode.F4)) {
			//setMovTexture(4);
			//status = Status.Aufmerksam;
			setMovTexture(4, false, 7);
			aktuellerBereich = Bereich.Nah;
			letzterZeitstempel = Time.time;
			oscZeitstempel = 0;
			status = Status.Reaktion;
		} 
		else if (Input.GetKeyUp(KeyCode.F5)) {
			//setMovTexture(4);
			//status = Status.Aufmerksam;
			setMovTexture(5, false, rand.Next(4,7));
			aktuellerBereich = Bereich.Nah;
			letzterZeitstempel = Time.time;
			oscZeitstempel = 0;
			status = Status.Schreck;
		} 

		/*
		if(status != Status.Idle && Time.time - oscZeitstempel > 4.0f) {
			status = Status.Idle;
			aktuellerBereich = Bereich.Leer;
			oscZeitstempel = 0;
			setMovTexture(1, true, 0);
			Debug.Log("Status.IDLE");
		}*/

		// GameLoop
			
		switch(status) {
		case Status.Idle :
			idle();
			break;
		case Status.Aufmerksam :
			aufmerksam();
			break;
		case Status.Anschlagen :
			anschlagen();
			break;
		case Status.Reaktion :
			reaktion();
			break;
		case Status.Schreck :
			schreck();
			break;

		}
	}

	private void setMovTexture(int nr, bool loop, int audioNr) {
		movTexture = movarr[nr-1];
		renderer.material.mainTexture = movTexture;
		movTexture.Stop();
		movTexture.loop = loop;
		movTexture.Play();



		if (((AudioSource)sounds [lastAudio]).isPlaying)
			((AudioSource)sounds [lastAudio]).Stop ();

		//audio.clip = movTexture.audioClip;
		//audio.Play();
		((AudioSource)sounds[audioNr]).Play();

		lastAudio = audioNr;
	}

	private void idle() {
		if(aktuellerBereich == Bereich.Fern) {
			setMovTexture(2, true, rand.Next(1,4));
			status = Status.Aufmerksam;
			// TODO: Übergangssequenzen

		}
		if(aktuellerBereich == Bereich.Mittel) {
			setMovTexture(3, true, 8);
			status = Status.Anschlagen;
			// TODO: Übergangssequenzen
			
		}
		if(aktuellerBereich == Bereich.Nah) {
			setMovTexture(5, false, rand.Next(4,7));
			status = Status.Schreck;
			// TODO: Übergangssequenzen
			
		}
		/*if(aktuellerBereich == Bereich.Leer) {
			setMovTexture(1, true);
			// TODO: Übergangssequenzen
			
		}*/
	}

	private void aufmerksam() {
		if(aktuellerBereich == Bereich.Nah) {
			setMovTexture(5, false, rand.Next(4,7));
			status = Status.Schreck;
			// TODO: Übergangssequenzen
			
		}
		if(aktuellerBereich == Bereich.Mittel /*|| warNutzerInaktiv(5)*/) {
			setMovTexture(3, true, 8);
			status = Status.Anschlagen;	
			// TODO: Übergangssequenzen
			
		}
		if(aktuellerBereich == Bereich.Leer) {
			setMovTexture(1, true, 0);
			status = Status.Idle;
			// TODO: Übergangssequenzen
			
		}
	}

	private void anschlagen() {
		if (aktuellerBereich == Bereich.Leer /*|| warNutzerInaktiv(5)*/) {
			setMovTexture(1, true, 0);
			status = Status.Idle;
		} else if (aktuellerBereich == Bereich.Fern) {
			setMovTexture(2, true, rand.Next(1,4));
			status = Status.Aufmerksam;
		} else if (aktuellerBereich == Bereich.Nah) {
			setMovTexture(4, false, 7);
			status = Status.Reaktion;
		}
	}

	private void reaktion() {

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

	private void schreck() {
		if (aktuellerBereich == Bereich.Leer) {
			setMovTexture(1, true, 0);
			status = Status.Idle;
		}
	}

	private bool warNutzerInaktiv(int sec) {
		if(Time.time - letzterZeitstempel > sec) {
			letzterZeitstempel = Time.time;
			return true;
		}
		else
			return false;
	}

	public void setzeAktuellenBereich(int b) {
		oscZeitstempel = Time.time;
		switch(b) {
		case 0: aktuellerBereich = Bereich.Fern; break;
		case 1: aktuellerBereich = Bereich.Mittel; break;
		case 2: aktuellerBereich = Bereich.Nah; break;
		default: aktuellerBereich = Bereich.Leer; break;
		}
	}
}
