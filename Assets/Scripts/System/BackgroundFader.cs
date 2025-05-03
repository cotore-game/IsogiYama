using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// �w�i�摜�̃N���X�t�F�[�h�؂�ւ���S���N���X
/// </summary>
public class BackgroundFader
{
    private readonly Image mainImage;
    private readonly Image subImage;
    private readonly CanvasGroup canvasGroup;
    private readonly Dictionary<string, Sprite> spriteLookup;

    public BackgroundFader(Image mainImage, Image subImage, CanvasGroup canvasGroup, List<Sprite> sprites)
    {
        this.mainImage = mainImage;
        this.subImage = subImage;
        this.canvasGroup = canvasGroup;

        // Sprite lookup ������
        spriteLookup = new Dictionary<string, Sprite>(sprites.Count);
        foreach (var s in sprites)
        {
            if (s == null) continue;
            spriteLookup[s.name] = s;
        }

        // �������
        this.subImage.gameObject.SetActive(false);
        this.canvasGroup.alpha = 1f;
    }

    /// <summary>
    /// �w��L�[�Ŕw�i���N���X�t�F�[�h�؂�ւ�
    /// </summary>
    public async UniTask ChangeBackgroundAsync(string key, float duration = 0.5f)
    {
        if (!spriteLookup.TryGetValue(key, out var target))
        {
            Debug.LogWarning($"[BackgroundFader] Sprite not found: {key}");
            return;
        }

        // Sub�ɃZ�b�g���ėL����
        subImage.sprite = target;
        subImage.gameObject.SetActive(true);

        // �t�F�[�h�A�E�g���X���b�v���t�F�[�h�C��
        await FadeSwapAsync(duration);
    }

    private async UniTask FadeSwapAsync(float duration)
    {
        float t = 0f;
        // �t�F�[�h�A�E�g(Main)
        while (t < duration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = 1f - (t / duration);
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
        canvasGroup.alpha = 0f;

        // Main��Sub��Sprite�𓯊�
        mainImage.sprite = subImage.sprite;

        // �t�F�[�h�C��(Main)
        t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = t / duration;
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
        canvasGroup.alpha = 1f;

        // Sub���\���ɖ߂�
        subImage.gameObject.SetActive(false);
    }
}