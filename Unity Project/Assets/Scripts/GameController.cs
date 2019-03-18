using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class GameController : MonoBehaviour {

    public int numOfEnemies;

    public Transform playerPrefab;
    public Transform aiAgent;
    public Transform enemy;
    public Transform item;
    public Transform visionFieldPrefab;

    public Transform topAlcove1;
    public Transform topAlcove2;
    public Transform topAlcove3;
    public Transform topAlcove4;
    public Transform topAlcove5;
    public Transform bottomAlcove1;
    public Transform bottomAlcove2;
    public Transform bottomAlcove3;
    public Transform bottomAlcove4;
    public Transform bottomAlcove5;

    public Transform leftAlcove;
    public Transform rightAlcove;

    public List<Transform> itemsRemaining;

    public Transform topLeftDoor;
    public Transform topRightDoor;
    public Transform bottomLeftDoor;
    public Transform bottomRightDoor;

    public Text playerScoreText;
    public Text playerTeleportsText;
    public Text aiScoreText;
    public Text aiTeleportsText;
    public Text playerCapturedText;
    public Text aiCapturedText;
    public Text endGameText;

    public EnemyController[] topEnemies;
    public EnemyController[] bottomEnemies;

    public int aiTeleportsRemaining = 2;

    int playerScore = 0;
    int aiScore = 0;
    PlayerController player;
    AIController ai;
    bool playerCaptured;
    bool aiCaptured;
    int playerTeleportsRemaining = 2;
    WorldState currentWS;
    bool enemyNearAI = false;
    

    // Use this for initialization
    void Start () {
        System.Diagnostics.Debug.WriteLine("Starting Game...");

        // Randomly pick positions for the player and AI to start at
        Transform[] topAlcoves = { topAlcove1, topAlcove2, topAlcove3, topAlcove4, topAlcove5 };
        Transform[] bottomAlcoves = { bottomAlcove1, bottomAlcove2, bottomAlcove3, bottomAlcove4, bottomAlcove5 };

        // Initialize items in each alcove
        itemsRemaining = new List<Transform>();
        for (int i = 0; i < 5; i++)
        {
            itemsRemaining.Add(Instantiate(item, bottomAlcoves[i].position, Quaternion.identity).transform);
        }
        for (int i = 0; i < 5; i++)
        {
            itemsRemaining.Add(Instantiate(item, topAlcoves[i].position, Quaternion.identity).transform);
        }

        int playerAlcove = Random.Range(0, 5);
        player = Instantiate(playerPrefab, topAlcoves[playerAlcove].position, Quaternion.identity).GetComponent<PlayerController>();
        player.game = this;
        int aiAlcove = Random.Range(0, 5);
        ai = Instantiate(aiAgent, bottomAlcoves[aiAlcove].position, Quaternion.identity).GetComponent<AIController>();
        ai.game = this;
        ai.target = bottomAlcoves[aiAlcove];

        // Initate two enemies, one on the top and one on the bottom. Randomly decide which door each one starts at.
        topEnemies = new EnemyController[numOfEnemies];
        bottomEnemies = new EnemyController[numOfEnemies];

        for (int i = 0; i < numOfEnemies; i++)
        {
            int doorDecision = Random.Range(0, 2); // Pick a number 0 or 1
            if (doorDecision == 0)
            {
                // The top enemy starts at the left door
                topEnemies[i] = Instantiate(enemy, topLeftDoor.GetChild(0).position, Quaternion.identity).GetComponent<EnemyController>();
                topEnemies[i].source = topLeftDoor;
                topEnemies[i].target = topRightDoor;
                topEnemies[i].visionField = Instantiate(visionFieldPrefab, new Vector3(-17, 1, 5.4f), Quaternion.identity);
                topEnemies[i].visionOffset = 1;
                topEnemies[i].visionZPosition = 5.4f;
                topEnemies[i].visionFieldPrefab = visionFieldPrefab;
            }
            else
            {
                // The top enemy starts at the right door
                topEnemies[i] = Instantiate(enemy, topRightDoor.GetChild(0).position, Quaternion.identity).GetComponent<EnemyController>();
                topEnemies[i].source = topRightDoor;
                topEnemies[i].target = topLeftDoor;
                topEnemies[i].visionField = Instantiate(visionFieldPrefab, new Vector3(17, 1, 5.4f), Quaternion.identity);
                topEnemies[i].visionOffset = -1;
                topEnemies[i].visionZPosition = 5.4f;
                topEnemies[i].visionFieldPrefab = visionFieldPrefab;
            }

            doorDecision = Random.Range(0, 2); // Pick a number 0 or 1
            if (doorDecision == 0)
            {
                // The bottom enemy starts at the left door
                bottomEnemies[i] = Instantiate(enemy, bottomLeftDoor.GetChild(0).position, Quaternion.identity).GetComponent<EnemyController>();
                bottomEnemies[i].source = bottomLeftDoor;
                bottomEnemies[i].target = bottomRightDoor;
                bottomEnemies[i].visionField = Instantiate(visionFieldPrefab, new Vector3(-17, 1, -5.4f), Quaternion.identity);
                bottomEnemies[i].visionOffset = 1;
                bottomEnemies[i].visionZPosition = -5.4f;
                bottomEnemies[i].visionFieldPrefab = visionFieldPrefab;
            }
            else
            {
                // The bottom enemy starts at the right door
                bottomEnemies[i] = Instantiate(enemy, bottomRightDoor.GetChild(0).position, Quaternion.identity).GetComponent<EnemyController>();
                bottomEnemies[i].source = bottomRightDoor;
                bottomEnemies[i].target = bottomLeftDoor;
                bottomEnemies[i].visionField = Instantiate(visionFieldPrefab, new Vector3(17, 1, -5.4f), Quaternion.identity);
                bottomEnemies[i].visionOffset = -1;
                bottomEnemies[i].visionZPosition = -5.4f;
                bottomEnemies[i].visionFieldPrefab = visionFieldPrefab;
            }
        }

        playerTeleportsText.text = "Teleports: " + playerTeleportsRemaining;
        aiTeleportsText.text = "Teleports: " + aiTeleportsRemaining;
    }

    public void PlayerPickUpItem(GameObject item)
    {
        playerScore++;
        itemsRemaining.Remove(item.transform);
        if (ai.closestItem.Equals(item.transform))
        {
            ai.closestItem = null;
            ai.plan(GetCurrentState());
        }
        playerScoreText.text = "Your score: " + playerScore;
        
        if (itemsRemaining.Count == 0)
        {
            endGame();
        }
    }

    public void AIPickUpItem(GameObject item)
    {
        aiScore++;
        itemsRemaining.Remove(item.transform);
        if (ai.closestItem.Equals(item.transform))
        {
            ai.closestItem = null;
            ai.plan(GetCurrentState());
        }
        aiScoreText.text = "Opponent's score: " + aiScore;

        if (itemsRemaining.Count == 0)
        {
            endGame();
        }
    }

    public void displayPlayerCaptured()
    {
        playerCapturedText.text = "You have been captured!";
        playerCaptured = true;
        // If AI has also been captured, the game is over.
        if (aiCaptured && playerCaptured)
        {
            endGame();
        }
    }

    public void displayAICaptured()
    {
        aiCapturedText.text = "Your opponent has been captured!";
        aiCaptured = true;
        // If player has also been captured, the game is over.
        if (aiCaptured && playerCaptured)
        {
            endGame();
        }
    }

    public WorldState GetCurrentState()
    {
        WorldState currentState = new WorldState();
        currentState.closestItemToAI = ai.closestItem;
        currentState.numItemsRemaining = itemsRemaining.Count;
        currentState.AITarget = ai.target;

        // Find min distance between AI Agent and the field of vision of the closest enemy
        if (topEnemies.Length > 0)
        {
            float minDistFromAIToEnemy = 1000;
            if (topEnemies[0].visionField != null)
            {
                minDistFromAIToEnemy = Vector3.Distance(ai.transform.position, topEnemies[0].visionField.transform.position);
            } 
            else
            {
                minDistFromAIToEnemy = Vector3.Distance(ai.transform.position, topEnemies[0].transform.position);

            }
            for (int i = 0; i < topEnemies.Length; i++)
            {
                float distToTopEnemy = 1000;
                float distToBottomEnemy = 1000;
                if (topEnemies[i].visionField != null)
                {
                    distToTopEnemy = Vector3.Distance(ai.transform.position, topEnemies[i].visionField.transform.position);
                }
                else
                {
                    distToTopEnemy = Vector3.Distance(ai.transform.position, topEnemies[i].transform.position);
                }

                if (bottomEnemies[i].visionField != null)
                {
                    distToBottomEnemy = Vector3.Distance(ai.transform.position, bottomEnemies[i].visionField.transform.position);
                }
                else
                {
                    distToBottomEnemy = Vector3.Distance(ai.transform.position, bottomEnemies[i].transform.position);
                }

                if (distToTopEnemy < minDistFromAIToEnemy)
                {
                    minDistFromAIToEnemy = distToTopEnemy;
                }
                if (distToBottomEnemy < minDistFromAIToEnemy)
                {
                    minDistFromAIToEnemy = distToBottomEnemy;
                }
            }
            currentState.distToClosestEnemy = minDistFromAIToEnemy;
        }
        else
        {
            // If no enemies on board
            currentState.distToClosestEnemy = 1000;
        }

        currentState.enemyNearby = enemyNearAI;
        currentState.aiTeleportsRemaining = aiTeleportsRemaining;

        bool playerCloserToTargetItem = false;
        // Check if AI is heading towards an item that the player is closer to
        if (ai.target.Equals(ai.closestItem))
        {
            float aiDistToItem = Vector3.Distance(ai.transform.position, ai.target.position);
            float playerDistToItem = Vector3.Distance(player.transform.position, ai.target.position);
            if (playerDistToItem <= aiDistToItem)
            {
                playerCloserToTargetItem = true;
            }
        }
        currentState.playerCloserToTargetItem = playerCloserToTargetItem;

        // Determine if AI using teleport will teleport the player
        float aiDistToPlayer = Vector3.Distance(ai.transform.position, player.transform.position);
        currentState.playerIsNearestAgent = (aiDistToPlayer <= currentState.distToClosestEnemy);

        // Check if AI is currently facing a nearby enemy
        bool facingNearbyEnemy = false;
        RaycastHit hit;
        if (Physics.Raycast(ai.transform.position, ai.transform.forward, out hit, 5) && hit.transform != ai.transform && hit.transform)
        {
            for (int i = 0; i < topEnemies.Length; i++)
            {
                if (hit.transform.Equals(topEnemies[i].transform || hit.transform.Equals(bottomEnemies[i].transform))){
                    facingNearbyEnemy = true;
                    break;
                }
            }
        }
        currentState.aiMovingTowardEnemy = facingNearbyEnemy;

        // Check if an enemy is very close to the AI's target
        bool enemyNearAITarget = false;
        if (topEnemies.Length > 0)
        {
            Transform t = ai.target;
            float minEnemyDistToTarget;
            if (topEnemies[0].visionField != null)
            {
                minEnemyDistToTarget = Vector3.Distance(topEnemies[0].visionField.transform.position, t.position);
            }
            else
            {
                minEnemyDistToTarget = Vector3.Distance(topEnemies[0].transform.position, t.position);
            }
            for (int i = 0; i < topEnemies.Length; i++)
            {
                float topEnemyDistToItem;
                float bottomEnemyDistToItem;
                if (topEnemies[i].visionField != null)
                {
                    topEnemyDistToItem = Vector3.Distance(topEnemies[i].visionField.position, t.position);
                }
                else
                {
                    topEnemyDistToItem = Vector3.Distance(topEnemies[i].transform.position, t.position);
                }

                if (bottomEnemies[i].visionField != null)
                {
                    bottomEnemyDistToItem = Vector3.Distance(bottomEnemies[i].visionField.position, t.position);
                }
                else
                {
                    bottomEnemyDistToItem = Vector3.Distance(bottomEnemies[i].transform.position, t.position);
                }

                if (topEnemyDistToItem < minEnemyDistToTarget)
                {
                    minEnemyDistToTarget = topEnemyDistToItem;
                }
                if (bottomEnemyDistToItem < minEnemyDistToTarget)
                {
                    minEnemyDistToTarget = bottomEnemyDistToItem;
                }              
            }

            if (minEnemyDistToTarget < 12)
            {
                enemyNearAITarget = true;
            }
        }
        currentState.enemyCloseToAITarget = enemyNearAITarget;

        currentState.aiPreviousTarget = ai.lastTarget;

        return currentState;
    }

    private void endGame()
    {
        if (playerScore > aiScore)
        {
            endGameText.text = "Victory!";
        }
        else if (playerScore == aiScore)
        {
            endGameText.text = "Tie";
        }
        else
        {
            endGameText.text = "Defeat!";
        }

        player.disableActions();
    }

    private void playerPlayTeleport()
    {
        // Find closest agent/enemy
        float distToAI = Vector3.Distance(player.transform.position, ai.transform.position);
        float minTopEnemyDist = Vector3.Distance(player.transform.position, topEnemies[0].transform.position);
        int minTopIndex = 0;
        float minBottomEnemyDist = Vector3.Distance(player.transform.position, bottomEnemies[0].transform.position);
        int minBottomIndex = 0;
        for (int i = 1; i < topEnemies.Length; i++)
        {
            if (Vector3.Distance(player.transform.position, topEnemies[i].transform.position) < minTopEnemyDist)
            {
                minTopEnemyDist = Vector3.Distance(player.transform.position, topEnemies[i].transform.position);
                minTopIndex = i;
            }

            if (Vector3.Distance(player.transform.position, bottomEnemies[i].transform.position) < minBottomEnemyDist)
            {
                minBottomEnemyDist = Vector3.Distance(player.transform.position, bottomEnemies[i].transform.position);
                minBottomIndex = i;
            }
        }

        float minEnemyDist;
        EnemyController closestEnemy;
        if (minBottomEnemyDist < minTopEnemyDist)
        {
            minEnemyDist = minBottomEnemyDist;
            closestEnemy = bottomEnemies[minBottomIndex];
        }
        else
        {
            minEnemyDist = minTopEnemyDist;
            closestEnemy = topEnemies[minTopIndex];
        }

        if (minEnemyDist < distToAI
            )
        {
            // Teleport the closest enemy
            closestEnemy.respawn();
        }
        else
        {
            // Teleport the AI agent to a random alcove
            Transform[] alcoves = { topAlcove1, topAlcove2, topAlcove3, topAlcove4, topAlcove5, bottomAlcove1, bottomAlcove2, bottomAlcove3, bottomAlcove4, bottomAlcove5 };
            int aiAlcove = Random.Range(0, 10);
            ai.transform.position = alcoves[aiAlcove].position;
        }
        
        playerTeleportsRemaining--;
        playerTeleportsText.text = "Teleports: " + playerTeleportsRemaining;
    }

    public void aiPlayTeleport()
    {
        // Find closest agent/enemy
        float distToPlayer = Vector3.Distance(ai.transform.position, player.transform.position);
        float minEnemyDist = 1000;
        EnemyController closestEnemy = null;
        if (topEnemies.Length > 0)
        {
            float minTopEnemyDist = Vector3.Distance(ai.transform.position, topEnemies[0].transform.position);
            int minTopIndex = 0;
            float minBottomEnemyDist = Vector3.Distance(ai.transform.position, bottomEnemies[0].transform.position);
            int minBottomIndex = 0;
            for (int i = 1; i < topEnemies.Length; i++)
            {
                if (Vector3.Distance(ai.transform.position, topEnemies[i].transform.position) < minTopEnemyDist)
                {
                    minTopEnemyDist = Vector3.Distance(ai.transform.position, topEnemies[i].transform.position);
                    minTopIndex = i;
                }

                if (Vector3.Distance(ai.transform.position, bottomEnemies[i].transform.position) < minBottomEnemyDist)
                {
                    minBottomEnemyDist = Vector3.Distance(ai.transform.position, bottomEnemies[i].transform.position);
                    minBottomIndex = i;
                }
            }

            if (minBottomEnemyDist < minTopEnemyDist)
            {
                minEnemyDist = minBottomEnemyDist;
                closestEnemy = bottomEnemies[minBottomIndex];
            }
            else
            {
                minEnemyDist = minTopEnemyDist;
                closestEnemy = topEnemies[minTopIndex];
            }
        }

        if (minEnemyDist < distToPlayer)
        {
            // Teleport the closest enemy
            closestEnemy.respawn();
        }
        else
        {
            // Teleport the player to a random alcove
            Transform[] alcoves = { topAlcove1, topAlcove2, topAlcove3, topAlcove4, topAlcove5, bottomAlcove1, bottomAlcove2, bottomAlcove3, bottomAlcove4, bottomAlcove5 };
            int playerAlcove = Random.Range(0, 10);
            player.transform.position = alcoves[playerAlcove].position;
        }
        aiTeleportsRemaining--;
        aiTeleportsText.text = "Teleports: " + aiTeleportsRemaining;
    }

    // Update is called once per frame
    void Update () {
        if (Input.GetKeyDown("space"))
        {
            if (playerTeleportsRemaining > 0)
            {
                // Player plays teleport.
                playerPlayTeleport();
            }
        }
        enemyNearAI = false;
        Collider[] hitColliders = Physics.OverlapSphere(ai.transform.position, 20);
        for (int i = 0; i < hitColliders.Length; i++)
        {
            Collider hitCollider = hitColliders[i];
            for (int j = 0; j < topEnemies.Length; j++)
            {
                EnemyController e1 = topEnemies[j];
                EnemyController e2 = bottomEnemies[j];
                if (e1.visionField != null)
                {
                    if (e1.visionField.GetComponent<Collider>().Equals(hitCollider))
                    {
                        enemyNearAI = true;
                        break;
                    }
                }
                if (e2.visionField != null)
                {
                    if (e2.visionField.GetComponent<Collider>().Equals(hitCollider))
                    {
                        enemyNearAI = true;
                        break;
                    }
                }
            }
            if (enemyNearAI == true)
            {
                ai.plan(GetCurrentState());
                ai.PlayMove();
                break;
            }
        }
    }


    public class WorldState
    {
        public Transform closestItemToAI;
        public int numItemsRemaining;
        public Transform AITarget;
        public float distToClosestEnemy;
        public Transform nearestAlcove;
        public int aiTeleportsRemaining;
        public bool playerCloserToTargetItem;
        public bool playerIsNearestAgent;
        public bool enemyNearby;
        public Transform aiPreviousTarget;
        public bool aiMovingTowardEnemy;
        public bool enemyCloseToAITarget;

        public WorldState Copy()
        {
            WorldState copy = new WorldState();
            copy.closestItemToAI = this.closestItemToAI;
            copy.numItemsRemaining = this.numItemsRemaining;
            copy.AITarget = this.AITarget;
            copy.distToClosestEnemy = this.distToClosestEnemy;
            copy.nearestAlcove = this.nearestAlcove;
            copy.aiTeleportsRemaining = this.aiTeleportsRemaining;
            copy.enemyNearby = this.enemyNearby;
            copy.aiPreviousTarget = this.aiPreviousTarget;
            copy.playerCloserToTargetItem = this.playerCloserToTargetItem;
            copy.playerIsNearestAgent = this.playerIsNearestAgent;
            copy.aiMovingTowardEnemy = this.aiMovingTowardEnemy;
            copy.enemyCloseToAITarget = this.enemyCloseToAITarget;
            return copy;
        }
    }
}
