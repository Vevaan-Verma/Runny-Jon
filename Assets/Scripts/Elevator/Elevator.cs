using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Elevator : MonoBehaviour {

    [Header("References")]
    [SerializeField] private Transform topFloorTarget;
    [SerializeField] private Transform bottomFloorTarget;
    private Rigidbody rb;
    private Vector3 topFloor;
    private Vector3 bottomFloor;
    private float floorDistance;

    [Header("Movement")]
    [SerializeField] private float movementDelay;
    [SerializeField] private float movementDuration;
    private Coroutine moveCoroutine;
    private bool atBottomFloor;
    private bool elevatorCalling;

    [Header("Doors")]
    [SerializeField] private ElevatorDoor bottomLeftDoor;
    [SerializeField] private ElevatorDoor bottomRightDoor;
    [SerializeField] private ElevatorDoor topLeftDoor;
    [SerializeField] private ElevatorDoor topRightDoor;

    private void Start() {

        rb = GetComponent<Rigidbody>();
        topFloor = topFloorTarget.position;
        bottomFloor = bottomFloorTarget.position;
        atBottomFloor = transform.position == bottomFloor;
        floorDistance = Vector3.Distance(topFloor, bottomFloor);

    }

    public void CallElevator(CallingButton.ButtonType buttonType) {

        if (elevatorCalling)
            return;

        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        Vector3 targetPosition = buttonType == CallingButton.ButtonType.Top ? topFloor : bottomFloor;

        if (targetPosition == transform.position) {

            CloseDoors();
            OpenDoors();

        }

        moveCoroutine = StartCoroutine(MoveElevator(targetPosition, true));

    }

    public void ElevatorEntered() {

        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        CloseDoors();
        moveCoroutine = StartCoroutine(MoveElevator(atBottomFloor ? topFloor : bottomFloor, false));

    }

    public void ElevatorExited() {

        CloseDoors();

    }

    private IEnumerator MoveElevator(Vector3 targetPosition, bool called) {

        if (called)
            elevatorCalling = true;

        CloseDoors();

        if (transform.position != targetPosition) {

            float duration = movementDuration;

            if (!called) {

                yield return new WaitForSeconds(movementDelay);

            } else {

                duration = (Vector3.Distance(atBottomFloor ? bottomFloor : topFloor, transform.position) / floorDistance) * movementDuration;

            }

            float currentTime = 0f;
            Vector3 startPosition = transform.position;

            while (currentTime < duration) {

                currentTime += Time.deltaTime;
                rb.MovePosition(Vector3.Lerp(startPosition, targetPosition, currentTime / duration));
                yield return null;

            }

            transform.position = targetPosition;

        }

        atBottomFloor = targetPosition == bottomFloor;
        OpenDoors();

        if (called)
            elevatorCalling = false;

    }

    private void OpenDoors() {

        if (atBottomFloor) {

            bottomLeftDoor.Open();
            bottomRightDoor.Open();

        } else {

            topLeftDoor.Open();
            topRightDoor.Open();

        }
    }

    private void CloseDoors() {

        bottomLeftDoor.Close();
        bottomRightDoor.Close();
        topLeftDoor.Close();
        topRightDoor.Close();

    }
}