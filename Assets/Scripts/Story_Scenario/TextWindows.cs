﻿using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using SoundSystem;
using System.Threading;
using System;

public class TextWindows : SceneSingleton<TextWindows>
{
    [Header("オブジェクト")]
    [SerializeField] private GameObject SpeechBubble;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private GameObject SkipIcon;
    [SerializeField] private CanvasGroup logoCanvasG;

    [Header("パラメータ")]
    [SerializeField] private float blankPeriod = 1.5f;
    [SerializeField, Range(0f, 1f)] private float minAlpha = 0.2f;
    [SerializeField] private int waitmsec = 3000;

    private IdleLogoBlink idleLogoBlink;
    private CancellationTokenSource _idleBlinkCts;
    private CancellationToken _lifetimeToken;

    private bool isPaused = false;         // Pause状態を管理するフラグ
    private bool skipRequested = false;    // スキップがリクエストされたかを管理

    private void Start()
    {
        _lifetimeToken = this.GetCancellationTokenOnDestroy();

        idleLogoBlink = new IdleLogoBlink(logoCanvasG, blankPeriod, minAlpha);
        SpeechBubble.SetActive(false);
        SkipIcon.SetActive(false);
    }

    public void Pause()
    {
        isPaused = !isPaused;
    }

    /// <summary>
    /// 文字を１文字ずつ表示し、一定数表示後はユーザー入力で全文表示に切り替えます。
    /// なお、UI上（例：ボタン）の場合は入力を無視し、背景等特定のオブジェクト上での入力のみ有効とします。
    /// また、文字表示の間隔待機中も入力を早期検出できるようにしています。
    /// </summary>
    /// <param name="name">キャラクター名など</param>
    /// <param name="body">表示するテキスト</param>
    /// <param name="interval">表示間隔（ミリ秒）</param>
    /// <param name="skipThreshold">表示文字数のスキップ許可閾値（%）</param>
    public async UniTask DisplayTextAsync(
        string name,
        string body,
        int interval,    // ミリ秒単位
        int skipThreshold, // 表示文字数のスキップ許可閾値（%）
        CancellationToken ct
    )
    {
        // 既存のアイドルブリンク処理をキャンセル
        _idleBlinkCts?.Cancel();
        _idleBlinkCts?.Dispose();
        _idleBlinkCts = new CancellationTokenSource();

        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            _idleBlinkCts.Token,
            _lifetimeToken,
            ct
        );

        SpeechBubble.SetActive(true);
        // SkipIcon.SetActive(false);


        // nameText.SetText(name);
        bodyText.SetText(body);
        bodyText.maxVisibleCharacters = 0;
        bodyText.ForceMeshUpdate();
        bodyText.UpdateVertexData(TMP_VertexDataUpdateFlags.All);
        // nameText.ForceMeshUpdate();

        // int totalLength = bodyText.GetParsedText().Length;
        int totalLength = bodyText.textInfo.characterCount;

        int skipLimit = Mathf.CeilToInt(totalLength * (skipThreshold / 100f));
        skipRequested = false;
        int visibleCount = 0;

        while (visibleCount < totalLength)
        {
            // Pause中は待機
            await UniTask.WaitUntil(() => !isPaused);
            if (skipRequested)
            {
                linkedCts.Cancel();
                bodyText.maxVisibleCharacters = totalLength;
                break;
            }

            // スキップ許可前は通常のDelayで文字を1文字ずつ表示
            if (visibleCount < skipLimit)
            {
                await UniTask.Delay(interval);
                visibleCount++;
                bodyText.maxVisibleCharacters = visibleCount;

                // SoundPlayer.instance.PlaySe("TypeHit");
            }
            else
            {
                // スキップ可能になったら、Delayと入力待機を同時実行
                var delayTask = UniTask.Delay(interval, cancellationToken: linkedCts.Token);
                var inputTask = UniTask.WaitUntil(() => IsSkipInputValid());
                int winner = await UniTask.WhenAny(delayTask, inputTask);

                // 入力が先に検出された
                if (winner == 1)
                {
                    Debug.Log($"Skipped Text at {visibleCount} / {skipLimit}");
                    linkedCts.Cancel();
                    bodyText.maxVisibleCharacters = totalLength;
                    break;
                }
                else
                {
                    visibleCount++;
                    bodyText.maxVisibleCharacters = visibleCount;
                    if (!SkipIcon.activeSelf)
                        SkipIcon.SetActive(true);
                }
            }
        }

        /*
        // SkipIconは必ず表示しておく
        if (!SkipIcon.activeSelf)
        {
            SkipIcon.SetActive(true);
        }
        */

        // 表示完了後のアイドルブリンク開始タスク
        UniTask.Void(async () =>
        {
            try
            {
                await UniTask.Delay(waitmsec, cancellationToken: linkedCts.Token);
                if (!linkedCts.Token.IsCancellationRequested && !IsSkipInputValid())
                {
                    idleLogoBlink.StartBlink(linkedCts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // キャンセルされても何もしない
            }
        });

        // 最終インタラクト待ち
        await UniTask.WaitUntil(
            () => IsSkipInputValid() && !isPaused,
            cancellationToken: linkedCts.Token
        );
        idleLogoBlink.StopBlink();

        try
        {   SoundPlayer.instance.PlaySe("TypeHit"); }
        catch
        {   }
        // SkipIcon.SetActive(false);
    }

    /// <summary>
    /// UIの上にポインターがある場合、かつそのUIが「AllowInteract」タグを持たなければ入力を無視する
    /// </summary>
    /// <returns>入力を処理して良いかどうか</returns>
    private bool ShouldProcessInput()
    {
        return true;

        // EventSystemが存在し、かつポインターがUI上にある場合
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            // もしヒットしたUIの中に「ExclusionUI」タグを持つものがあれば、入力は無効
            foreach (var result in results)
            {
                if (result.gameObject.CompareTag("ExclusionUI"))
                {
                    return false;
                }
            }
        }
        return true;
    }

    /// <summary>
    /// スキップ入力（Spaceキーまたは左クリック）が有効な状態かどうかを返します。
    /// 入力発生と同時に、UI上（許可対象以外）ならば無視します。
    /// </summary>
    private bool IsSkipInputValid()
    {
        return (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)) && ShouldProcessInput();
    }
}
