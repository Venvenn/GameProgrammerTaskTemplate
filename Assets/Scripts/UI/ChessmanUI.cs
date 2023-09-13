using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the ui of an individual chessman
/// </summary>
public class ChessmanUI : MonoBehaviour
{
    private const float SPINNER_SPEED = 50;
    
    private Camera _mainCamera;

    [SerializeField]
    private TextMeshProUGUI _healthText;
    [SerializeField]
    private TextMeshProUGUI _strengthText;
    [SerializeField] 
    private Image _spinner;
    
    private void Start()
    {
        _mainCamera = Camera.main;
    }

    public void Update()
    {
        if (_spinner.IsActive())
        {
            _spinner.transform.Rotate(0,0,SPINNER_SPEED * Time.deltaTime);
        }
    }

    public void SetHealth(float currentHealth, float maxHealth)
    {
        _healthText.text = $"{currentHealth}/{maxHealth}";
    }
    
    public void SetStrength(float strength)
    {
        _strengthText.text = strength.ToString();
    }

    public void SetPositionInScreenSpace(Vector3 worldPosition)
    {
        Vector3 screenPos = _mainCamera.WorldToScreenPoint(worldPosition);
        transform.position = screenPos;
    }

    public void ToggleSpinner(bool enable)
    {
        _spinner.gameObject.SetActive(enable);
    }
    
}
