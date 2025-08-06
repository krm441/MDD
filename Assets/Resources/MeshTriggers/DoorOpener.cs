using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DoorOpenStyle
{
    FromTopToBottom,
}

public class DoorOpener : MonoBehaviour, IInteractableAction
{
    [SerializeField] private DoorOpenStyle doorOpenStyle = DoorOpenStyle.FromTopToBottom;
    [SerializeField] private Renderer doorRenderer;
    [SerializeField] private Renderer doorMask;
    [SerializeField] private Transform doorTransform;
    [SerializeField] private float openDuration = 2f;

    public void Execute(GameObject caller)
    {
        OpenDoor();
    }

    public void OpenDoor()
    {
        SoundPlayer.PlayClipAtPoint("DoorMoveHeavy", transform.position);
        float doorHeight = doorRenderer.bounds.size.y;
        Vector3 endPos = doorTransform.position;

        if (doorOpenStyle == DoorOpenStyle.FromTopToBottom)
            endPos -= Vector3.up * doorHeight;

        StartCoroutine(MoveDoor(doorTransform.position, endPos));
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
