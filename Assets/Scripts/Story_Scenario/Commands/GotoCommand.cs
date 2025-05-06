using Cysharp.Threading.Tasks;

namespace IsogiYama.Commands
{
    public class GotoCommand : CommandBase
    {
        ProgressManager progressManager;

        public override async UniTask ExecuteAsync(LineData<ScenarioFields> lineData)
        {
            progressManager = InstanceRegister.Get<ProgressManager>();

            int targetIndex = lineData.Get<int>(ScenarioFields.PageCtrl);

            progressManager.IncrementIndex();
            // Header�����l�����Ȃ���OutOfIndex�ɂȂ肩�˂Ȃ� + Index��0����n�܂��Ă邩��2�����Ȃ��Ƃ����Ȃ�
            progressManager.IndexSkip(targetIndex - 2);
            await UniTask.Delay(1);
        }
    }
}