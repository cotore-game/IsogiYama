using Cysharp.Threading.Tasks;
using UnityEngine;

public class CameraController
{
    private readonly Camera camera;
    private readonly Transform cameraTransform;
    private readonly Vector3 initialPosition;

    /// <summary>
    /// �R���X�g���N�^�� Camera ���󂯎��A�����ʒu���L�����܂��B
    /// </summary>
    public CameraController(Camera cam)
    {
        camera = cam ?? throw new System.ArgumentNullException(nameof(cam));
        cameraTransform = cam.transform;
        initialPosition = cameraTransform.localPosition;
    }

    /// <summary>
    /// �w�莞�� duration �b�A���x magnitude �ŃJ������U�������܂��B
    /// </summary>
    public async UniTask ShakeAsync(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // �����_���ȃI�t�Z�b�g�𐶐�
            Vector3 offset = Random.insideUnitSphere * magnitude;
            cameraTransform.localPosition = initialPosition + offset;

            // ���t���[���܂őҋ@
            await UniTask.Yield(PlayerLoopTiming.Update);
            elapsed += Time.deltaTime;
        }

        // �U���I����͕K�������ʒu�ɖ߂�
        cameraTransform.localPosition = initialPosition;
    }
}
