using UnityEngine;
using System.Collections;


public class skybox : MonoBehaviour {

	[SerializeField] float rotationSpeed = 1.0f;

	
	void Update() {
		transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
	}

	

	
	
	
}
