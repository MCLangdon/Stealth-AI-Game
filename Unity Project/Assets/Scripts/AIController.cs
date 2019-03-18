using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIController : MonoBehaviour {

    public GameController game;
    public UnityEngine.AI.NavMeshAgent agent;
    public Transform closestItem;
    public Transform target;
    public Transform closestAlcove;
    public Transform lastTarget;

    Stack<PrimitiveTask> finalPlan;
    Stack<Task> tasksToProcess;
    int timer = 0;

	void Start () {
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        tasksToProcess = new Stack<Task>();
        finalPlan = new Stack<PrimitiveTask>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Item"))
        {
            other.gameObject.SetActive(false);
            game.AIPickUpItem(other.gameObject);
        }
        else if (other.gameObject.CompareTag("VisionField"))
        {
            // The agent has been detected by an enemy. Remove the agent from the game.
            gameObject.SetActive(false);
            game.displayAICaptured();
        }
    }


    // Update is called once per frame
    void Update () {
        if (timer >= 2)
        {


            GameController.WorldState currentState = game.GetCurrentState();
            plan(currentState);

            PlayMove();
            agent.SetDestination(target.position);

            timer = 0;
        }
        else
        {
            timer++;
        }
    }

    public void PlayMove()
    {
        while (finalPlan.Count > 0)
        {
            PrimitiveTask nextAction = finalPlan.Pop();
            if (nextAction.PrimitiveConditionsMet(game.GetCurrentState()))
            {
                nextAction.executeTask(this);
            }
            else
            {
                plan(game.GetCurrentState());
                break;
            }
        }
        plan(game.GetCurrentState());
    }

    public void plan(GameController.WorldState currentWorldState)
    {
        finalPlan = new Stack<PrimitiveTask>();
        Stack<PlannerState> decompHistory = new Stack<PlannerState>();
        GameController.WorldState WorkingWS = currentWorldState.Copy();

        tasksToProcess.Push(new PlayGame());

        while (tasksToProcess.Count > 0)
        {

            Task CurrentTask = tasksToProcess.Pop();
            if (CurrentTask.GetType().IsSubclassOf(typeof(CompoundTask)))
            {

                CompoundTask CurrentCompoundTask = (CompoundTask)CurrentTask;
                Method SatisfiedMethod = CurrentCompoundTask.FindSatisfiedMethod(WorkingWS);
                
                if (SatisfiedMethod != null)
                {
                    //PlannerState currentState = new PlannerState(CurrentCompoundTask, finalPlan, tasksToProcess, SatisfiedMethod);
                   // decompHistory.Push(currentState);
                    SatisfiedMethod.subTasks.Reverse();
                    foreach (Task t in SatisfiedMethod.subTasks) {
                        tasksToProcess.Push(t);
                    }
                    SatisfiedMethod.subTasks.Reverse();
                }
                else if (decompHistory.Count > 0)
                {
                    //RestoreToLastDecomposedTask():
                    PlannerState lastState = decompHistory.Pop();
                    CompoundTask lastCompoundTask = lastState.currentTask;
                    finalPlan = lastState.finalPlan;
                    tasksToProcess = lastState.tasksToProcess;
                    // Remove the failed method and return CompoundTask to the stack. On the next iteration, it will be checked again for a valid method.
                    lastCompoundTask.InvalidateMethod(lastState.currentMethod);
                    tasksToProcess.Push(lastCompoundTask);
                }  
            }
            else//Primitive Task
            {
                PrimitiveTask CurrentPrimitiveTask = (PrimitiveTask)CurrentTask;
                if (CurrentPrimitiveTask.PrimitiveConditionsMet(WorkingWS))
                {
                    // CurrentPrimitiveTask.ApplyEffects(this, WorkingWS);
                    // Add this PrimitiveTask to the bottom of the finalPlan
                    Stack<PrimitiveTask> temp = new Stack<PrimitiveTask>();
                    for(int i = 0; i < finalPlan.Count; i++)
                    {
                        PrimitiveTask nextTask = finalPlan.Pop();
                        temp.Push(nextTask);
                    }
                    temp.Push(CurrentPrimitiveTask);
                    finalPlan = new Stack<PrimitiveTask>();
                    for (int i = 0; i < temp.Count; i++)
                    {
                        PrimitiveTask nextTask = temp.Pop();
                        finalPlan.Push(nextTask);
                    }

                }
                else if (decompHistory.Count > 0)
                {
                    //RestoreToLastDecomposedTask();
                    PlannerState lastState = decompHistory.Pop();
                    CompoundTask lastCompoundTask = lastState.currentTask;
                    finalPlan = lastState.finalPlan;
                    tasksToProcess = lastState.tasksToProcess;
                    // Remove the failed method and return CompoundTask to the stack. On the next iteration, it will be checked again for a valid method.
                    lastCompoundTask.InvalidateMethod(lastState.currentMethod);
                    tasksToProcess.Push(lastCompoundTask);
                }
            }
        }
    }

    public class PlannerState
    {
        public CompoundTask currentTask;
        public Stack<PrimitiveTask> finalPlan;
        public Stack<Task> tasksToProcess;
        public Method currentMethod;

        public PlannerState(CompoundTask currentTask, Stack<PrimitiveTask> finalPlan, Stack<Task> tasksToProcess, Method currentMethod)
        {
            this.currentTask = currentTask;
            this.finalPlan = new Stack<PrimitiveTask>();
            this.tasksToProcess = new Stack<Task>();
            this.currentMethod = currentMethod;

            Stack<PrimitiveTask> temp = new Stack<PrimitiveTask>();
            // Create shallow copy of finalPlan
            for(int i = 0; i < finalPlan.Count; i++)
            {
                temp.Push(finalPlan.Pop());
            }
            for (int i = 0; i < temp.Count; i++)
            {
                PrimitiveTask t = temp.Pop();
                this.finalPlan.Push(t);
                finalPlan.Push(t);
            }

            // Create shallow copy of tasksToProcess
            Stack<Task> temp2 = new Stack<Task>();
            for (int i = 0; i < tasksToProcess.Count; i++)
            {
                temp2.Push(tasksToProcess.Pop());
            }
            for (int i = 0; i < temp2.Count; i++)
            {
                Task t = temp2.Pop();
                this.tasksToProcess.Push(t);
                tasksToProcess.Push(t);
            }

        }
    }

    public abstract class Task
    {
    }

    public abstract class PrimitiveTask : Task
    {
        public abstract bool ApplyEffects(AIController ai, GameController.WorldState WorkingWS);

        public abstract bool PrimitiveConditionsMet(GameController.WorldState WorkingWS);

        public abstract bool executeTask(AIController ai);
    }

    public abstract class CompoundTask : Task
    {
        public List<Method> methods;

        public void InvalidateMethod(Method invalidMethod)
        {
            foreach (Method m in methods)
            {
                if (m.Equals(invalidMethod))
                {
                    methods.Remove(m);
                    break;
                }
            }
        }

        public Method FindSatisfiedMethod(GameController.WorldState WorkingWS)
        {
            foreach (Method m in this.methods)
            {
                if (m.PreconditionsSatisfied(WorkingWS))
                {
                    return m;
                }
            }
            return null;
        }
    }

    public abstract class Method
    {
        public List<Task> subTasks;
        public GameController.WorldState preconditions;
        public string name;

        public Method(string name, List<Task> subTasks)
        {
            this.name = name;
            this.subTasks = subTasks;
        }

        public bool Equals(Method m) {
            return (this.name.Equals(m.name));
        }

        public abstract bool PreconditionsSatisfied(GameController.WorldState gameState);
    }

    public class CollectItemMethod : Method
    {
        public CollectItemMethod() : base("Collect nearest item", new List<Task>()) {
            FindNearestItem task1 = new FindNearestItem();
            GoToItem task2 = new GoToItem();
            this.subTasks.Add(task1);
            this.subTasks.Add(task2);
        }

        public override bool PreconditionsSatisfied(GameController.WorldState gameState)
        {
            // There are still items remaining
            if (gameState.numItemsRemaining <= 0)
            {
                return false;
            }
            // No enemies are nearby
            else if (gameState.distToClosestEnemy <= 10)
            {
                return false;
            }
            else
            {
                return true;
            }

        }
    }

    public class HideFromEnemyMethod : Method
    {
        public HideFromEnemyMethod() : base("Hide from closest enemy", new List<Task>()) {
            Hide task1 = new Hide();
            this.subTasks.Add(task1);
        }

        public override bool PreconditionsSatisfied(GameController.WorldState gameState)
        {
            // No preconditions, AI agent can always hide from enemy
            return true;
        }
    }

    public class UseTeleportMethod : Method
    {
        public UseTeleportMethod() : base("Use Teleport", new List<Task>())
        {
            Teleport task1 = new Teleport();
            this.subTasks.Add(task1);
        }

        public override bool PreconditionsSatisfied(GameController.WorldState gameState)
        {
            if (gameState.aiTeleportsRemaining <= 0)
            {
                return false;
            }
            else if (gameState.distToClosestEnemy <= 5 || (gameState.playerCloserToTargetItem && gameState.playerIsNearestAgent))
            {
                // An enemy is dangerously close, or the player is going to beat the AI to its target item
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public class GoToNearestAlcoveMethod : Method
    {
        public GoToNearestAlcoveMethod() : base("Go to the nearest alcove", new List<Task>())
        {
            FindNearestAlcove task1 = new FindNearestAlcove();
            GoToAlcove task2 = new GoToAlcove();
            this.subTasks.Add(task1);
            this.subTasks.Add(task2);
        }

        public override bool PreconditionsSatisfied(GameController.WorldState gameState)
        {
            if (gameState.enemyCloseToAITarget)
            {
                return false;
            }
            else if (gameState.aiMovingTowardEnemy)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    public class GoToLastAlcoveMethod : Method
    {
        public GoToLastAlcoveMethod() : base("Go to the previous alcove position", new List<Task>())
        {
            GoToPreviousTarget task1 = new GoToPreviousTarget();
            this.subTasks.Add(task1);
        }

        public override bool PreconditionsSatisfied(GameController.WorldState gameState)
        {
            if (gameState.aiPreviousTarget == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    public class PlayGame : CompoundTask
    {
        public PlayGame()
        {
            CollectItemMethod collectItem = new CollectItemMethod();
            UseTeleportMethod teleport = new UseTeleportMethod();
            HideFromEnemyMethod hideFromEnemy = new HideFromEnemyMethod();
            this.methods = new List<Method>();
            this.methods.Add(collectItem);
            this.methods.Add(teleport);
            this.methods.Add(hideFromEnemy);
        }
    }

    public class Hide : CompoundTask
    {

        public Hide()
        {
            GoToNearestAlcoveMethod method1 = new GoToNearestAlcoveMethod();
            GoToLastAlcoveMethod method2 = new GoToLastAlcoveMethod();
            this.methods = new List<Method>();
            this.methods.Add(method1);
            this.methods.Add(method2);
        }
    }

    public class FindNearestItem : PrimitiveTask
    {
        public override bool ApplyEffects(AIController ai, GameController.WorldState WorkingWS)
        {
            if (ai.game.itemsRemaining != null && WorkingWS.numItemsRemaining > 0)
            {
                List<Transform> itemsRemaining = ai.game.itemsRemaining;
                float minDistToItem = Vector3.Distance(ai.agent.transform.position, itemsRemaining[0].position);
                Transform closestItem = itemsRemaining[0];
                foreach (Transform t in itemsRemaining)
                {
                    float distToItem = Vector3.Distance(ai.agent.transform.position, t.position);
                    if (distToItem < minDistToItem)
                    {
                        // Check if no enemies are too close to this item.
                        float minEnemyDistToItem = 1000;
                        for (int i = 0; i < ai.game.topEnemies.Length; i++)
                        {
                            float topEnemyDistToItem = 1000;
                            float bottomEnemyDistToItem = 1000;
                            if (ai.game.topEnemies[i].visionField != null)
                            {
                                topEnemyDistToItem = Vector3.Distance(ai.game.topEnemies[i].visionField.position, t.position);
                            }
                            else
                            {
                                topEnemyDistToItem = Vector3.Distance(ai.game.topEnemies[i].transform.position, t.position);
                            }

                            if (ai.game.bottomEnemies[i].visionField != null)
                            {
                                bottomEnemyDistToItem = Vector3.Distance(ai.game.bottomEnemies[i].visionField.position, t.position);
                            }
                            else
                            {
                                bottomEnemyDistToItem = Vector3.Distance(ai.game.bottomEnemies[i].transform.position, t.position);
                            }

                            if (topEnemyDistToItem < minEnemyDistToItem)
                            {
                                minEnemyDistToItem = topEnemyDistToItem;
                            }
                            if (bottomEnemyDistToItem < minEnemyDistToItem)
                            {
                                minEnemyDistToItem = bottomEnemyDistToItem;
                            }
                        }



                        minDistToItem = distToItem;  
                        closestItem = t;
                    }
                }
                //     WorkingWS.closestItemToAI = closestItem;
                // WorkingWS.aiTentativeTarget = closestItem;
                return true; 
            }
            else
            { 
                return false;
            }
        }

        public override bool executeTask(AIController ai)
        {
            if (ai.game.itemsRemaining.Count > 0)
            {
                List<Transform> itemsRemaining = ai.game.itemsRemaining;
                float minDistToItem = Vector3.Distance(ai.agent.transform.position, itemsRemaining[0].position);
                Transform closestItem = itemsRemaining[0];
                foreach (Transform t in itemsRemaining)
                {
                    float distToItem = Vector3.Distance(ai.agent.transform.position, t.position);
                    if (distToItem < minDistToItem)
                    {
                        minDistToItem = distToItem;
                        closestItem = t.transform;
                    }
                }
                ai.closestItem = closestItem;
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool PrimitiveConditionsMet(GameController.WorldState WorkingWS)
        {
            if (WorkingWS.numItemsRemaining <= 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    public class GoToItem : PrimitiveTask
    {
        public override bool ApplyEffects(AIController ai, GameController.WorldState WorkingWS)
        {
                  if (WorkingWS.closestItemToAI != null)
                  {
                     // WorkingWS.aiPreviousTarget = WorkingWS.AITarget;
                     // WorkingWS.AITarget = WorkingWS.closestItemToAI;
                      return true;
                  }
                  else {
                      return false;
                  }
                  
        }

        public override bool executeTask(AIController ai)
        {
            if (ai.closestItem != null)
            {
                ai.lastTarget = ai.target;
                ai.target = ai.closestItem;
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool PrimitiveConditionsMet(GameController.WorldState WorkingWS)
        {
            if (WorkingWS.numItemsRemaining <= 0)
            {
                return false;
            }
            else if (WorkingWS.closestItemToAI == null)
            {
                return false;
            }
            else if(WorkingWS.aiMovingTowardEnemy)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    public class FindNearestAlcove : PrimitiveTask
    {
        public override bool ApplyEffects(AIController ai, GameController.WorldState WorkingWS)
        {
            List<Transform> alcoves = new List<Transform> { ai.game.bottomAlcove1, ai.game.bottomAlcove2, ai.game.bottomAlcove3, ai.game.bottomAlcove4, ai.game.bottomAlcove5, ai.game.topAlcove1, ai.game.topAlcove2, ai.game.topAlcove3, ai.game.topAlcove4, ai.game.topAlcove5, ai.game.leftAlcove, ai.game.rightAlcove };
            float minDistToAlcove = 1000;
            Transform closestAlcove = null;

            foreach (Transform t in alcoves)
            {
                float distToAlcove = Vector3.Distance(ai.agent.transform.position, t.position);
                if (distToAlcove < minDistToAlcove)
                {
                    // Check if no enemies are too close to this alcove.
                    float minEnemyDistToAlcove = 1000;
                    for (int i = 0; i < ai.game.topEnemies.Length; i++)
                    {
                        float topEnemyDistToAlcove = 1000;
                        float bottomEnemyDistToAlcove = 1000;
                        if (ai.game.topEnemies[i].visionField != null)
                        {
                            topEnemyDistToAlcove = Vector3.Distance(ai.game.topEnemies[i].visionField.position, t.position);
                        }
                        else
                        {
                            topEnemyDistToAlcove = Vector3.Distance(ai.game.topEnemies[i].transform.position, t.position);
                        }

                        if (ai.game.bottomEnemies[i].visionField != null)
                        {
                            bottomEnemyDistToAlcove = Vector3.Distance(ai.game.bottomEnemies[i].visionField.position, t.position);
                        }
                        else
                        {
                            bottomEnemyDistToAlcove = Vector3.Distance(ai.game.bottomEnemies[i].transform.position, t.position);
                        }

                        if (topEnemyDistToAlcove < minEnemyDistToAlcove)
                        {
                            minEnemyDistToAlcove = topEnemyDistToAlcove;
                        }
                        if (bottomEnemyDistToAlcove < minEnemyDistToAlcove)
                        {
                            minEnemyDistToAlcove = bottomEnemyDistToAlcove;
                        }
                    }

                    if (minEnemyDistToAlcove > 11)
                    {
                        minDistToAlcove = distToAlcove;
                        closestAlcove = t;
                    }


                }
            }

            // WorkingWS.nearestAlcove = closestAlcove;
            //WorkingWS.aiTentativeTarget = closestAlcove;
            if (closestAlcove != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool executeTask(AIController ai)
        {

            List<Transform> alcoves = new List<Transform> { ai.game.bottomAlcove1, ai.game.bottomAlcove2, ai.game.bottomAlcove3, ai.game.bottomAlcove4, ai.game.bottomAlcove5, ai.game.topAlcove1, ai.game.topAlcove2, ai.game.topAlcove3, ai.game.topAlcove4, ai.game.topAlcove5, ai.game.leftAlcove, ai.game.rightAlcove };
            float minDistToAlcove = 1000;
            Transform closestAlcove = null;

            foreach (Transform t in alcoves)
            {
                float distToAlcove = Vector3.Distance(ai.agent.transform.position, t.position);
                if (distToAlcove < minDistToAlcove)
                {
                    // Check if any enemies are too close to this alcove.
                    float minEnemyDistToAlcove = 1000;
                    for (int i = 0; i < ai.game.topEnemies.Length; i++)
                    {
                        float topEnemyDistToAlcove = 1000;
                        float bottomEnemyDistToAlcove = 1000;
                        if (ai.game.topEnemies[i].visionField != null)
                        {
                            topEnemyDistToAlcove = Vector3.Distance(ai.game.topEnemies[i].visionField.position, t.position);
                        }
                        else
                        {
                            topEnemyDistToAlcove = Vector3.Distance(ai.game.topEnemies[i].transform.position, t.position);
                        }

                        if (ai.game.bottomEnemies[i].visionField != null)
                        {
                            bottomEnemyDistToAlcove = Vector3.Distance(ai.game.bottomEnemies[i].visionField.position, t.position);
                        }
                        else
                        {
                            bottomEnemyDistToAlcove = Vector3.Distance(ai.game.bottomEnemies[i].transform.position, t.position);
                        }

                        if (topEnemyDistToAlcove < minEnemyDistToAlcove)
                        {
                            minEnemyDistToAlcove = topEnemyDistToAlcove;
                        }
                        if (bottomEnemyDistToAlcove < minEnemyDistToAlcove)
                        {
                            minEnemyDistToAlcove = bottomEnemyDistToAlcove;
                        }
                    }

                    if (minEnemyDistToAlcove > 11)
                    {
                        minDistToAlcove = distToAlcove;
                        closestAlcove = t;
                    }


                }
            }


            if (closestAlcove != null)
            {
                ai.closestAlcove = closestAlcove;
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool PrimitiveConditionsMet(GameController.WorldState WorkingWS)
        {
            // No Preconditions for finding nearest alcove, as there will always be alcoves.
            return true;
        }
    }

    public class GoToAlcove : PrimitiveTask
    {
        public override bool ApplyEffects(AIController ai, GameController.WorldState WorkingWS)
        {
            if (WorkingWS.nearestAlcove != null)
            {
               // WorkingWS.aiPreviousTarget = WorkingWS.AITarget;
               // WorkingWS.AITarget = WorkingWS.nearestAlcove;
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool executeTask(AIController ai)
        {
            if (ai.closestAlcove != null)
            {
                ai.lastTarget = ai.target;
                ai.target = ai.closestAlcove;
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool PrimitiveConditionsMet(GameController.WorldState WorkingWS)
        {
            if (WorkingWS.nearestAlcove == null)
            {
                return false;
            }
            else if (WorkingWS.aiMovingTowardEnemy)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    public class Teleport : PrimitiveTask
    {
        public override bool ApplyEffects(AIController ai, GameController.WorldState WorkingWS)
        {
            if (WorkingWS.aiTeleportsRemaining <= 0)
            {
                return false;
            }
            else
            {
                // WorkingWS.aiTeleportsRemaining--;
                return true;
            }
        }

        public override bool executeTask(AIController ai)
        {
            if (ai.game.aiTeleportsRemaining <= 0)
            {
                return false;
            }
            else
            {
                ai.game.aiPlayTeleport();
                return true;
            }
        }

        public override bool PrimitiveConditionsMet(GameController.WorldState WorkingWS)
        {
            if (WorkingWS.aiTeleportsRemaining <= 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    public class GoToPreviousTarget : PrimitiveTask
    {
        public override bool ApplyEffects(AIController ai, GameController.WorldState WorkingWS)
        {
            if (WorkingWS.aiPreviousTarget != null)
            {
                // WorkingWS.aiPreviousTarget = WorkingWS.AITarget;
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool executeTask(AIController ai)
        {
            if (ai.lastTarget != null)
            {
                ai.target = ai.lastTarget;
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool PrimitiveConditionsMet(GameController.WorldState WorkingWS)
        {
            if (WorkingWS.aiPreviousTarget == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
