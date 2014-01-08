﻿using UnityEngine;
using System.Collections;
using System;
using System.IO.Ports;


public class worms : MonoBehaviour {

	public MovieTexture movTexture;
	public GameObject[] charaktere;

	private ArrayList einblendListe = new ArrayList();

	public Status status;

	public MovieTexture timeTexture;

	private bool personIn;
	private bool personOut;
	private bool removeAll = false;

	public GUIText guitext;

	public enum Status {
		Idle,
		Warten,
		Beginn,
		Abbruch
	}

	GameObject pl_begin;
	GameObject pl_abbruch;

	private int anzPersonen;

	public GUISkin guiSkin;

	private float oscZeitstempel;
	private float letzterZeitstempel;

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
		setTransparenz(pl_begin, 0.0f);
		pl_abbruch = GameObject.Find("pl_abbruch");
		setTransparenz(pl_abbruch, 0.0f);

		foreach (GameObject charakter in charaktere) {
			setTransparenz(charakter, 0.0f);
		}

		status = Status.Idle;
		letzterZeitstempel = Time.time;
		oscZeitstempel = Time.time;

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
		else if (Input.GetKeyUp(KeyCode.F3)) {
			//setMovTexture(3);
			//status = Status.Idle;

			letzterZeitstempel = Time.time;
		}
		else if (Input.GetKeyUp(KeyCode.F4)) {
			//setMovTexture(4);
			//status = Status.Aufmerksam;

			letzterZeitstempel = Time.time;
		}

		//guitext.text = Mathf.RoundToInt(120 - Time.time + (float)letzterZeitstempel).ToString();
		guitext.text = "120";

		// Einblenden Animation
		/*for(int i=0; i< einblendListe.Count; i++) {
			ArrayList al = (ArrayList)einblendListe[i];
			Color color = ((GameObject)al[0]).renderer.material.color;
			//Debug.Log(color.a);
			//Debug.Log(einblendListe.Count);
			float incr = (float)al[1] * Time.deltaTime;
			if(color.a + incr > 1.0f) 
				color.a = 1.0f;
			else if( color.a + incr < 0.0f)
				color.a = 0.0f;
			else 
				color.a += incr;
			((GameObject)al[0]).renderer.material.color = color;
			if(color.a == 1.0f || color.a == 0.0f) {
				einblendListe.Remove(al);
				i--;
			}
		}*/

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

	private void setMovTexture(int nr, bool loop) {
		//movTexture = movarr[nr-1];
		renderer.material.mainTexture = movTexture;
		movTexture.loop = loop;
		movTexture.Play();
	}

	private void idle() {
		if(removeAll) {
			foreach(GameObject charakter in charaktere) {
				charakter.GetComponent<Charakter>().resetCharakter();
			}
			removeAll = false;
		}

		if (personIn) {
			status = Status.Warten;
			letzterZeitstempel = Time.time;
			timeTexture.Play();
		}



	}

	private void warten() {
		if(Time.time - oscZeitstempel > 4.0f) {
			status = Status.Idle;
			removeAll = true;
			timeTexture.Stop();
			Debug.Log("Status.IDLE");
		}

		if (personIn) {
			//Protokollant immer zuerst
			if (anzPersonen == 0) {
				setTransparenz(charaktere[0], 0.5f);
			} else {

				//setTransparenz(charaktere[anzPersonen], 0.5f);
				//TODO Einblenden nach Richtung
			}

			anzPersonen++;

			personIn = false;
			//Debug.Log(Time.time);
		}

		if (personOut) {
			anzPersonen--;
			//setTransparenz(charaktere[anzPersonen], -0.5f);

			personOut = false;
			if(anzPersonen == 0)
				status = Status.Idle;
		}

		if (anzPersonen >= charaktere.Length) {
			status = Status.Beginn;
			timeTexture.Pause();
		} else if ((Time.time - letzterZeitstempel) > 120 && anzPersonen < charaktere.Length) {
			status = Status.Abbruch;
		}
	}

	private void begin() {
		Debug.Log("Begin");

		setTransparenz(pl_begin, 1.0f);


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

		if (personOut) {
			anzPersonen--;
			
			personOut = false;
			if(anzPersonen == 0) {
				foreach (GameObject charakter in charaktere) {
					//setTransparenz(charakter, -0.5f);
				}
				status = Status.Idle;
				timeTexture.Stop();
				setTransparenz(pl_begin, 0.0f);
			}
		}
	}

	private void abbruch() {
		Debug.Log("Abbruch");

		setTransparenz(pl_abbruch, 1.0f);

		if (personOut) {
			anzPersonen--;
			
			personOut = false;
			if(anzPersonen == 0) {
				foreach (GameObject charakter in charaktere) {
					//setTransparenz(charakter, -0.5f);
				}
				status = Status.Idle;
				timeTexture.Stop();
				setTransparenz(pl_abbruch, 0.0f);
			}
		}
	}

	private void setTransparenz(GameObject pl, float fade) {

			Color color = pl.renderer.material.color;
			color.a = fade;
			pl.renderer.material.color = color;
			
		/*
		foreach(ArrayList al in einblendListe) {
			if(charakter == al[0]) {
				al[1] = fade;
				return;
			}
		}

		ArrayList eintrag = new ArrayList();
		eintrag.Add(charakter);
		eintrag.Add(fade);
		einblendListe.Add(eintrag);*/
	}

	public void handleFacelist(int id, int mfactor) {
		oscZeitstempel = Time.time;
		foreach(GameObject charakter in charaktere) {
			if(charakter.GetComponent<Charakter>().getFaceID() == id) {
				charakter.GetComponent<Charakter>().setMotionFactor(mfactor);
				break;
			}
		}
	}

	public void triggerPersonIn(int id) {
		oscZeitstempel = Time.time;
		if(id > charaktere.Length - 1) return;
		Debug.Log("new ID" + id);
		charaktere[id].GetComponent<Charakter>().setFaceID(id);
		charaktere[id].GetComponent<Charakter>().fade(0.5f);

		personIn = true;
	}

	public void triggerPersonOut(int id) {
		oscZeitstempel = Time.time;
		if(id > charaktere.Length - 1) return;
		foreach(GameObject charakter in charaktere) {

			if(charakter.GetComponent<Charakter>().getFaceID() == id) {
				charakter.GetComponent<Charakter>().resetCharakter();
				break;
			}
		}
		personOut = true;
	}
}
