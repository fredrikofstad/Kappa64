using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
public class NPCController : MonoBehaviour
{
	[SerializeField] float movingTurnSpeed = 360;
	[SerializeField] float stationaryTurnSpeed = 180;
	[SerializeField] float moveSpeed = 1f;

	Rigidbody body;
	Animator animator;

	//for calculating movement and rotation
	float turnAmount;
	float forwardAmount;
	Vector3 groundNormal = Vector3.up; // consider raycast later

    private void Start()
    {
		animator = GetComponent<Animator>();
		body = GetComponent<Rigidbody>();
		body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
	}

    public void Move(Vector3 move)
    {
		// convert the world relative moveInput vector into a local-relative
		// turn amount and forward amount required to head in the desired
		// direction.
		if (move.magnitude > 1f) move.Normalize();
		move = transform.InverseTransformDirection(move);
		move = Vector3.ProjectOnPlane(move, groundNormal);
		turnAmount = Mathf.Atan2(move.x, move.z);
		forwardAmount = move.z;

		ApplyTurnRotation();
	}

	void ApplyTurnRotation()
	{
		float turnSpeed = Mathf.Lerp(stationaryTurnSpeed, movingTurnSpeed, forwardAmount);
		transform.Rotate(0, turnAmount * turnSpeed * Time.deltaTime, 0);
	}

	void OnAnimatorMove()
	{
		Vector3 newPosition = transform.position;
		newPosition.x += moveSpeed * Time.deltaTime;
		transform.position = newPosition;
	}

}
