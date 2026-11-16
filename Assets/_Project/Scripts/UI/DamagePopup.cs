using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] TMP_Text _text;
    [SerializeField] float _lifetime = 0.9f;
    [SerializeField] float _riseSpeed = 1.2f;
    [SerializeField] float _normalSize = 4f;
    [SerializeField] float _headshotSize = 5.5f;

    float _age;
    Color _baseColor;

    public void Show(float damage, bool headshot)
    {
        _age = 0f;
        _text.text = headshot ? $"{damage:F0}!" : $"{damage:F0}";
        _baseColor = headshot ? new Color(1f, 0.35f, 0.2f) : Color.white;
        _text.color = _baseColor;
        _text.fontSize = headshot ? _headshotSize : _normalSize;
    }

    void Update()
    {
        _age += Time.deltaTime;
        transform.position += Vector3.up * (_riseSpeed * Time.deltaTime);

        float alpha = 1f - (_age / _lifetime);
        _text.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, alpha);

        if (_age >= _lifetime) Destroy(gameObject);
    }

    void LateUpdate()
    {
        if (Camera.main != null)
            transform.rotation = Camera.main.transform.rotation;
    }
}
