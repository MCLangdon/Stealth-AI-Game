using UnityEngine;


public class PlayerController : MonoBehaviour
{
    public float playerSpeed;
    public GameController game;

    bool actionsEnabled = true;

    void Update()
    {
        if (actionsEnabled)
        {
            var x = Input.GetAxis("Horizontal") * Time.deltaTime * playerSpeed;
            var z = Input.GetAxis("Vertical") * Time.deltaTime * playerSpeed;

            transform.Translate(x, 0, 0);
            transform.Translate(0, 0, z);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Item"))
        {
            other.gameObject.SetActive(false);
            game.PlayerPickUpItem(other.gameObject);
        }
        else if (other.gameObject.CompareTag("VisionField"))
        {
            // The player has been detected by an enemy. Remove the player from the game.
            gameObject.SetActive(false);
            game.displayPlayerCaptured();
        }
    }

    public void disableActions()
    {
        actionsEnabled = false;
    }
}