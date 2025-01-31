/*
    DESCRIPTION: Enemy state for attacking
 
    AUTHOR DD/MM/YY: Quentin 27/09/22

    - EDITOR DD/MM/YY CHANGES:
    - Quentin 1/12/22: Integrate with nav agent
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAttackState : EnemyStateMachine
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);
        controller.agent.isStopped = true;

        // when entering attack state, become agro
        if (!aggroCoroutine) controller.StartCoroutine(AggroTimer());
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // On update track the player to check they are still within range, face player if they moved

        if (controller.canMoveWhileAttacking)
        {
            TrackPlayer();
            UpdateAnimatorProperties(animator);
            FacePlayer();
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        controller.agent.isStopped = false;
    }

}
