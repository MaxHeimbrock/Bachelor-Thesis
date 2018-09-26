using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum State{
	Opened,
	FlexedIndex,
	AllFlexed
};

public class menu_controller : MonoBehaviour {
	public GameObject menu;
	public GameObject menu_highlight;
	public GameObject glove_controller;

	private Glove glove;

	private bool thumb_flexed;
	private bool index_flexed;
	private bool middle_flexed;
	private bool ring_flexed;
	private bool little_flexed;

	private State state;
	private float  last_transition_time;

	private State last_menu_state;
	private int nb_menu;
	private int active_menu;
	private bool show_menu;

	//===============================================
	// Use this for initialization
	void Start () {
		show_menu = false;
		thumb_flexed = false;
		index_flexed = false;
		middle_flexed = false;
		ring_flexed = false;
		little_flexed = false;

		state = State.Opened;
		last_transition_time = Time.fixedTime;

		nb_menu = 4;
		active_menu = 0;
	}

	//===============================================
	// Update is called once per frame
	void Update () {
		if (glove == null) {
			glove = glove_controller.GetComponent<serial_port_receiver> ().glove;
			return;
		}

		update_glove_actions (glove);
		// Transitions 
		update_statemachine();

		//Debug.Log ("state " + state);

		update_menu_display ();
	}

	//===============================================
	void update_menu_display(){
	
		if (last_menu_state == State.Opened && state == State.AllFlexed) {
			show_menu = !show_menu;
		}

		if (last_menu_state == State.Opened && state == State.FlexedIndex) {
			active_menu++;
			active_menu = active_menu % nb_menu;
		}

		if (last_menu_state == State.Opened && state == State.AllFlexed) {
			active_menu=0;

		}

		// show the state graphically
		menu.SetActive(show_menu);
		if (show_menu) {
			Vector3 pos = menu_highlight.transform.localPosition;
			pos.y = 190 - 100 * active_menu;
			menu_highlight.transform.localPosition = pos;
		}


		last_menu_state = state;
	}

	//===============================================
	void update_statemachine(){

		if (Time.fixedTime +0.1f < last_transition_time ) {
			return;
		}
		//Debug.Log (last_transition_time +" "+ Time.fixedTime);
		if (state == State.Opened) {
			if (index_flexed && middle_flexed && ring_flexed) {
				last_transition_time = Time.fixedTime;
				state = State.AllFlexed;
				return;
			}
			if (index_flexed) {
				last_transition_time = Time.fixedTime;
				state = State.FlexedIndex;
				return;
			}
		}

		if (state == State.FlexedIndex) {
			if (!thumb_flexed && !index_flexed && !middle_flexed && !ring_flexed && !little_flexed) {
				last_transition_time = Time.fixedTime;
				state = State.Opened;
				return;
			}
		}

		if (state == State.AllFlexed) {
			if (!thumb_flexed && !index_flexed && !middle_flexed && !ring_flexed && !little_flexed) {
				last_transition_time = Time.fixedTime;
				state = State.Opened;
				return;
			}
		}
	}

	//===============================================
	void update_glove_actions(Glove glove){
		
		float thumb_action = glove.values [1] * glove.values [1] +
			glove.values [2] * glove.values [2] +
			glove.values [3] * glove.values [3];

		float index_action = glove.values [9] * glove.values [9] +
			glove.values [11] * glove.values [11] +
			glove.values [11] * glove.values [11];

		float middle_action = glove.values [17] * glove.values [17] +
			glove.values [18] * glove.values [18] +
			glove.values [19] * glove.values [19];

		float ring_action = glove.values [25] * glove.values [25] +
			glove.values [26] * glove.values [26] +
			glove.values [27] * glove.values [27];

		float little_action = glove.values [33] * glove.values [33] +
			glove.values [34] * glove.values [34] +
			glove.values [35] * glove.values [35];
		


		// Input state
		if (thumb_action > 2.0f) {
			thumb_flexed = true;
		} else {
			thumb_flexed=false;
		}

		if (index_action> 2.0f) {
			index_flexed = true;
		} else {
			index_flexed=false;
		}

		if (middle_action> 2.0f) {
			middle_flexed = true;
		} else {
			middle_flexed=false;
		}

		if (ring_action> 2.0f) {
			ring_flexed = true;
		} else {
			ring_flexed=false;
		}

		if (little_action> 2.0f) {
			little_flexed = true;
		} else {
			little_flexed=false;
		}
		/*
		Debug.Log (thumb_flexed + " " 
				+ index_flexed + " "
				+ middle_flexed + " " 
				+ ring_flexed + " "
				+ little_flexed );
				*/
	}
}
