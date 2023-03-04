using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class ManageIK : MonoBehaviour
{
    Animator animator;
    [SerializeField] bool ikActive = true;
    [SerializeField] bool umbrella = true;
    [SerializeField] bool eating = false;


    [Header("Head")]
    [SerializeField] float lookWeight;
    [SerializeField] float maxDistance = 5f;

    [SerializeField] Transform playerTarget;

    [Header("Right Hand")]
    [SerializeField][Range(0, 1)] float rightHandWeight;
    [SerializeField] Transform rightHandTarget = null;
    //[SerializeField] Transform rightHandHint = null;

    [Header("Left Hand")]
    [SerializeField][Range(0,1)] float leftHandWeight;
    [SerializeField] Transform leftHandTarget = null;
    [SerializeField] Transform leftHandHint = null;

    MeshRenderer umbrellaRender;

    GameObject pivot;

    public void SetRightHand(Transform newTarget) => rightHandTarget = newTarget;

    void Start()
    {
        animator = GetComponent<Animator>();
        pivot = new GameObject("Pivot");
        pivot.transform.parent = transform;
        pivot.transform.localPosition = new Vector3(0,2.5f, 0);
        if(leftHandTarget != null)
            umbrellaRender = leftHandTarget.parent.GetComponent<MeshRenderer>();
    }

    void Update()
    {
        pivot.transform.LookAt(playerTarget);
        float pivotYrotation = pivot.transform.localRotation.y;

        float distance = Vector3.Distance(pivot.transform.position, playerTarget.position);

        if(pivotYrotation < 0.65f && pivotYrotation > -0.65f && distance < maxDistance)
        {
            lookWeight = Mathf.Lerp(lookWeight, 1, Time.deltaTime * 2.5f);
        }
        else
        {
            lookWeight = Mathf.Lerp(lookWeight, 0, Time.deltaTime * 2.5f);
        }
    }

    private void OnAnimatorIK()
    {
        HandleHead();
        HandleLeftHand();
        HandleRightHand();

    }

    private void HandleHead()
    {
        if (playerTarget == null) return;
        if (ikActive)
        {
            animator.SetLookAtWeight(lookWeight);
            animator.SetLookAtPosition(playerTarget.position);
        }
        else
        {
            animator.SetLookAtWeight(0);
        }
    }

    private void HandleLeftHand()
    {
        if (leftHandTarget == null) return;
        umbrellaRender.enabled = umbrella;
        if (umbrella)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, leftHandWeight);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, leftHandWeight);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandTarget.position);
            animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandTarget.rotation);

            animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, leftHandWeight);
            animator.SetIKHintPosition(AvatarIKHint.LeftElbow, leftHandHint.position);


        }
        else
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0);
        }
    }

    private void HandleRightHand()
    {
        if (rightHandTarget == null) return;
        if (eating)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, rightHandWeight);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, rightHandWeight);
            animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandTarget.position);
            animator.SetIKRotation(AvatarIKGoal.RightHand, rightHandTarget.rotation);

            //animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, leftHandWeight);
            //animator.SetIKHintPosition(AvatarIKHint.LeftElbow, leftHandHint.position);
        }
        else
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0);
        }
    }
}
