// Copyright 2026 Viktor Fedotov
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUI : WorldBehaviour {

    private const int maxLineLength = 500;

    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private Image speakerPortraitImage;
    [SerializeField] private TMP_Text lineText;
    [SerializeField] private float moveDuration = 0.25f;
    [SerializeField] private RectTransform rectTransform;

    private string line;
    private char[] partialLine = new char[maxLineLength];
    private bool isTypingCompleted;
    private IEnumerator textTypingAnimationCoroutine;
    private float height;

    public bool IsTypingCompleted => isTypingCompleted;

    private void OnValidate() {
        rectTransform = GetComponent<RectTransform>();
    }

    private void Awake() {
        height = rectTransform.rect.height;
        Hide(false);
    }

    public void Show() {
        gameObject.SetActive(true);
        StartCoroutine(MoveTo(0));
    }
    public void Hide(bool animate = true) {
        if (animate)
            StartCoroutine(MoveTo(-height, () => gameObject.SetActive(false)));
        else {
            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, -height);
            gameObject.SetActive(false);
        }
    }

    private IEnumerator MoveTo(float y, Action onComplete = null) {
        var startY = rectTransform.anchoredPosition.y;
        var startTime = Time.time;
        while (Time.time < startTime + moveDuration) {
            var t = (Time.time - startTime) / moveDuration;
            var newY = Mathf.Lerp(startY, y, t);
            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, newY);
            yield return null;
        }
        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, y);
        onComplete?.Invoke();
    }

    public void SetSpeaker(DialogueSpeaker speaker) {
        speakerNameText.text = speaker.SpeakerName;
        speakerPortraitImage.sprite = speaker.SpeakerPortrait;
    }

    public void SetLine(string line) {
        this.line = line;
        if (textTypingAnimationCoroutine != null) {
            StopCoroutine(textTypingAnimationCoroutine);
            textTypingAnimationCoroutine = null;
        }
        isTypingCompleted = false;
        textTypingAnimationCoroutine = TextTypingAnimation();
        StartCoroutine(textTypingAnimationCoroutine);
    }

    private IEnumerator TextTypingAnimation(float speed = 25) {
        var duration = line.Length / speed;
        var startTime = Time.time;
        while (Time.time < startTime + duration) {
            var t = (Time.time - startTime) / duration;
            var partialLength = Mathf.FloorToInt(t * line.Length);
            for (var i = 0; i < partialLength && i < maxLineLength; i++)
                partialLine[i] = line[i];
            lineText.SetText(partialLine, 0, partialLength);
            yield return null;
        }
        lineText.SetText(line);
        isTypingCompleted = true;
    }

    public void InterruptTyping() {
        lineText.SetText(line);
        if (textTypingAnimationCoroutine != null) {
            StopCoroutine(textTypingAnimationCoroutine);
            textTypingAnimationCoroutine = null;
        }
        isTypingCompleted = true;
    }
}