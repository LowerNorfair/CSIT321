/*
    DESCRIPTION: Enemy state controller Patrol state (moving and searching for player)

    AUTHOR DD/MM/YY: Quentin 27/09/22

    - EDITOR DD/MM/YY CHANGES:
	- Quentin 4/10/22: modified how movement is applied, added out of bound check
    - Kaleb 19/11/22: Added scriptable object data
    - Quentin 1/12/22: Changed movement to use navmeshagent
	- Quentin 7/1/23: Fixed enemy not engaging when returning, coroutine edit for agro
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyPatrolState : EnemyStateMachine
{
    private Vector3 movementVector;
    private Vector3 prevPosition;
    private bool outOfBounds = false;
    private IEnumerator waitCoroutine;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);
        prevPosition = controller.origin;
        waitCoroutine = WaitToMove(4.0f);
        controller.StartCoroutine(waitCoroutine);
        controller.agent.destination = controller.origin;

        // when non-agro hide canvas
        controller.canvas.SetActive(false);
    }

    // update state, track player and check if the object has moved out of bounds
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        TrackPlayer();
        UpdateAnimatorProperties(animator);

        if (outOfBounds)
        {
            outOfBounds = (Vector3.Distance(transform.position, controller.origin) >= 0.9);
        }

        // flip sprite towards the direction they are moving
        if (prevPosition.x < transform.position.x != controller.facingRight)
            controller.FlipSprite();

    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // display canvas when entering attack or chase state
        controller.canvas.SetActive(true);
        controller.StopCoroutine(waitCoroutine);
    }

    // move evey few seconds
    private IEnumerator WaitToMove(float time)
    {
        while (true)
        {

            prevPosition = transform.position;

            // check if moved too far from origin
            if (Vector3.Distance(controller.origin, transform.position) > controller.data.WanderRadius + 3.0)
            {
                outOfBounds = true;
                movementVector = controller.origin;
                controller.agent.destination = movementVector;
            }

            // pause coroutine while out of bounds
            while (outOfBounds)
            {
                TrackPlayer();
                yield return null;
            }

            // wait for (time) and pick a direction to walk in
            if (controller.isColliding)
            {
                movementVector *= -1;
            }
            else if (Vector3.Distance(controller.origin, transform.position) > controller.data.WanderRadius)
            {
                movementVector = controller.origin;
            }
            else
            {
                movementVector = Random.insideUnitSphere + controller.origin;
            }

            controller.agent.destination = movementVector;

            yield return new WaitForSeconds(time);
        }
    }
}