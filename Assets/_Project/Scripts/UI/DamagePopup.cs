using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] TMP_Text _text;
    [SerializeField] float _lifetime = 0.9f;
    [SerializeField] float _riseSpeed = 1.2f;

    float _age;
    Color _baseColor;

    public void Show(float damage, bool headshot)
    {
        _age = 0f;
        _text.text = headshot ? $"{damage:F0}!" : $"{damage:F0}";
        _baseColor = headshot ? new Color(1f, 0.35f, 0.2f) : Color.white;
        _text.color = _baseColor;
        _text.fontSize = headshot ? 42f : 30f;
        gameObject.SetActive(true);
    }

    void Update()
    {
        _age += Time.deltaTime;
        transform.position += Vector3.up * (_riseSpeed * Time.deltaTime);

        float alpha = 1f - (_age / _lifetime);
        _text.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, alpha);

        if (_age >= _lifetime) gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (Camera.main != null)
            transform.rotation = Camera.main.transform.rotation;
    }

}
