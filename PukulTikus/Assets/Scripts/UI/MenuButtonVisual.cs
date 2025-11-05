using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[ExecuteAlways]
[RequireComponent(typeof(Image))]
public class MenuButtonVisual : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Sprites")]
    public Sprite idleSprite;
    public Sprite selectedSprite;

    [Header("Debug / Preview")]
    [Tooltip("Centang ini untuk menampilkan tampilan Selected di Scene View (tidak perlu Play)")]
    public bool previewSelected = false;

    [HideInInspector] public bool isSelected = false;

    private Image img;

    private void Awake()
    {
        if (!img) img = GetComponent<Image>();
        UpdateSprite();
    }

    private void OnValidate()
    {
        // Dipanggil otomatis saat ada perubahan di Inspector (bahkan sebelum Play)
        if (!img) img = GetComponent<Image>();
        UpdateSprite();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateSprite();
    }

    private void UpdateSprite()
    {
        if (!img) return;

        // Prioritas: previewSelected (Scene view), kalau Play mode pakai isSelected
        bool active = Application.isPlaying ? isSelected : previewSelected;

        if (active && selectedSprite)
            img.sprite = selectedSprite;
        else if (!active && idleSprite)
            img.sprite = idleSprite;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Application.isPlaying)
            SetSelected(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (Application.isPlaying)
            SetSelected(false);
    }
}
