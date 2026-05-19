using UnityEngine;

public class ExplosionVisual : MonoBehaviour
{
    public float radius = 5.0f;
    public float duration = 1.0f;

    private float timer = 0.0f;
    private Material material;

    void Start()
    {
        this.material = this.GetComponent<MeshRenderer>().material;
        this.transform.localScale = Vector3.zero;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (material != null)
        {
            float fade = Mathf.Lerp(1f, 0f, timer / duration);
            Color c = material.color;
            c.a = fade;
            material.color = c;
        }

        this.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * radius * 2.0f, timer / duration);

        if (timer >= duration)
        {
            Destroy(gameObject);
        }
    }
}