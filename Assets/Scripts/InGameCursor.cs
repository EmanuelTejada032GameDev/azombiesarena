using UnityEngine;
using UnityEngine.UI;

public class InGameCursor : MonoBehaviour
{
    private RectTransform _crosshairRectTransform;
    private Image _crosshairImage;

    private void Awake()
    {
        _crosshairRectTransform = GetComponent<RectTransform>();
        _crosshairImage = GetComponent<Image>();
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.State == GameState.Playing)
        {
            if (_crosshairImage != null && !_crosshairImage.enabled)
            {
                _crosshairImage.enabled = true;
            }

            if (_crosshairRectTransform != null)
            {
                _crosshairRectTransform.position = Input.mousePosition;
            }
        }
        else
        {
            if (_crosshairImage != null && _crosshairImage.enabled)
            {
                _crosshairImage.enabled = false;
            }
        }
    }
}
