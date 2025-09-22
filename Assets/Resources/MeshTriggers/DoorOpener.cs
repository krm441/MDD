using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public enum DoorOpenStyle
{
    FromTopToBottom,
}

public class DoorOpener : MonoBehaviour, IInteractableAction
{
    [SerializeField] private GameManagerMDD gameManager;
    [SerializeField] private DoorOpenStyle doorOpenStyle = DoorOpenStyle.FromTopToBottom;
    [SerializeField] private Renderer doorRenderer;
    [SerializeField] private Renderer doorMask;
    [SerializeField] private Transform doorTransform;
    [SerializeField] private float openDuration = 2f;

    void Start()
    {
        gameManager = FindObjectOfType<GameManagerMDD>();
        Assert.IsNotNull(gameManager);
    }

    public void Execute(GameObject caller)
    {
        OpenDoor();
    }

    public void OpenDoor()
    {
        gameManager.soundPlayer.PlayClipAtPoint("DoorMoveHeavy", transform.position);
        float doorHeight = doorRenderer.bounds.size.y;
        Vector3 endPos = doorTransform.position;

        if (doorOpenStyle == DoorOpenStyle.FromTopToBottom)
            endPos -= Vector3.up * doorHeight;

        StartCoroutine(MoveDoor(doorTransform.position, endPos));
    }

    public void OpenDoorInstant()
    {
        // stop running animation
        StopAllCoroutines();

        float doorHeight = doorRenderer.bounds.size.y;
        Vector3 endPos = doorTransform.position;

        if (doorOpenStyle == DoorOpenStyle.FromTopToBottom)
            endPos -= Vector3.up * doorHeight;

        doorTransform.position = endPos;
        doorMask.enabled = false;
        doorRenderer.enabled = false;
    }


    private IEnumerator MoveDoor(Vector3 from, Vector3 to)
    {
        doorMask.enabled = true;
        float elapsed = 0f;
        while (elapsed < openDuration)
        {
            doorTransform.position = Vector3.Lerp(from, to, elapsed / openDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        doorTransform.position = to;
        doorMask.enabled = false;
        doorRenderer.enabled = false;
    }
}
