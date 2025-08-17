using TMPro;
using UnityEngine;

public class FloatingDamageText : MonoBehaviour
{
    [SerializeField] private TextMeshPro text;
    [SerializeField] private float floatSpeed = 1f;
    [SerializeField] private float duration = 1.5f;

    private float timeElapsed;

    public void Setup(string damageAmount, Color color)
    {
        text.text = damageAmount;//.ToString();
        text.color = color;
    }

    private void Update()
    {
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        timeElapsed += Time.deltaTime;
        if (timeElapsed >= duration)
        {
            Destroy(gameObject);
        }

        transform.LookAt(Camera.main.transform);
        transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
    }
}
