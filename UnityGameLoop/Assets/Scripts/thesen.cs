using UnityEngine;
using System.Collections;
using System;
using System.IO.Ports;

public class thesen : MonoBehaviour {

	public MovieTexture movTexture;
	public MovieTexture[] movarr;

	public Status status;

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

		//movTexture = Resources.Load("Vids/luther_hammer", typeof(MovieTexture)) as MovieTexture;
		setMovTexture(1, true);
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
			aktuellerBereich = Bereich.Leer;
			letzterZeitstempel = Time.time;
		}
		else if (Input.GetKeyUp(KeyCode.F2)) {
			//setMovTexture(2);
			//status = Status.Aufmerksam;
			aktuellerBereich = Bereich.Fern;
			letzterZeitstempel = Time.time;
		}
		else if (Input.GetKeyUp(KeyCode.F3)) {
			//setMovTexture(3);
			//status = Status.Idle;
			aktuellerBereich = Bereich.Mittel;
			letzterZeitstempel = Time.time;
		}
		else if (Input.GetKeyUp(KeyCode.F4)) {
			//setMovTexture(4);
			//status = Status.Aufmerksam;
			aktuellerBereich = Bereich.Nah;
			letzterZeitstempel = Time.time;
		}

		if(status != Status.Idle && Time.time - oscZeitstempel > 4.0f) {
			status = Status.Idle;
			aktuellerBereich = Bereich.Leer;
			setMovTexture(1, true);
			Debug.Log("Status.IDLE");
		}

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

	private void setMovTexture(int nr, bool loop) {
		movTexture = movarr[nr-1];
		renderer.material.mainTexture = movTexture;
		movTexture.Stop();
		movTexture.loop = loop;
		movTexture.Play();
	}

	private void idle() {
		if(aktuellerBereich == Bereich.Fern) {
			setMovTexture(2, true);
			status = Status.Aufmerksam;
			// TODO: Übergangssequenzen

		}
		if(aktuellerBereich == Bereich.Mittel) {
			setMovTexture(3, true);
			status = Status.Anschlagen;
			// TODO: Übergangssequenzen
			
		}
		if(aktuellerBereich == Bereich.Nah) {
			setMovTexture(5, false);
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
			setMovTexture(5, false);
			status = Status.Schreck;
			// TODO: Übergangssequenzen
			
		}
		if(aktuellerBereich == Bereich.Mittel /*|| warNutzerInaktiv(5)*/) {
			setMovTexture(3, true);
			status = Status.Anschlagen;
			// TODO: Übergangssequenzen
			
		}
		if(aktuellerBereich == Bereich.Leer) {
			setMovTexture(1, true);
			status = Status.Idle;
			// TODO: Übergangssequenzen
			
		}
	}

	private void anschlagen() {
		if (aktuellerBereich == Bereich.Leer /*|| warNutzerInaktiv(5)*/) {
			setMovTexture(1, true);
			status = Status.Idle;
		} else if (aktuellerBereich == Bereich.Fern) {
			setMovTexture(2, true);
			status = Status.Aufmerksam;
		} else if (aktuellerBereich == Bereich.Nah) {
			setMovTexture(4, false);
			status = Status.Reaktion;
		}
	}

	private void reaktion() {

		#region Druckeransteuerung
		// create SerialPort("COM3", 9600);
		SerialPort port = new SerialPort("DruckerImpuls", 9600);
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

		if (aktuellerBereich == Bereich.Leer) {
			setMovTexture(1, true);
			status = Status.Idle;
		}
	}

	private void schreck() {
		if (aktuellerBereich == Bereich.Leer) {
			setMovTexture(1, true);
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
