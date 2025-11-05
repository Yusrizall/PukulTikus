using UnityEngine;
using UnityEngine.UI;

public class UIHearts : MonoBehaviour
{
    [SerializeField] private Image heart1;
    [SerializeField] private Image heart2;
    [SerializeField] private Image heart3;
    [SerializeField] private Sprite fullSprite;
    [SerializeField] private Sprite emptySprite;

    public void SetHearts(int current, int max)
    {
        current = Mathf.Clamp(current, 0, max);
        var imgs = new[] { heart1, heart2, heart3 };

        // aktifkan hanya sebanyak 'max'
        for (int i = 0; i < imgs.Length; i++)
        {
            if (imgs[i]) imgs[i].gameObject.SetActive(i < max);
        }

        // set full/empty
        for (int i = 0; i < imgs.Length; i++)
        {
            var img = imgs[i];
            if (!img || !img.gameObject.activeSelf) continue;
            bool full = i < current;
            if (fullSprite && emptySprite)
                img.sprite = full ? fullSprite : emptySprite;
            img.enabled = true;
        }
    }
}
