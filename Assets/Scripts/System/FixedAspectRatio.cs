using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FixedAspectRatio : MonoBehaviour
{
    public float targetAspect = 4f / 3f; // �Œ肵�����A�X�y�N�g��i�����ł�4:3�j

    void Start()
    {
        Camera cam = GetComponent<Camera>();

        // ���݂̉�ʂ̃A�X�y�N�g��
        float windowAspect = (float)Screen.width / (float)Screen.height;

        // �A�X�y�N�g��̔䗦
        float scaleHeight = windowAspect / targetAspect;

        if (scaleHeight < 1.0f)
        {
            // ���ɍ��сi���^�[�{�b�N�X�j
            Rect rect = cam.rect;

            rect.width = 1.0f;
            rect.height = scaleHeight;
            rect.x = 0;
            rect.y = (1.0f - scaleHeight) / 2.0f;

            cam.rect = rect;
        }
        else
        {
            // �c�ɍ��сi�s���[�{�b�N�X�j
            float scaleWidth = 1.0f / scaleHeight;

            Rect rect = cam.rect;

            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f;
            rect.y = 0;

            cam.rect = rect;
        }
    }
}
