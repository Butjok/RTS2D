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
using UnityEngine;

[Serializable]
public class ConstructionQueueItem {

    public enum Status {
        Invalid,
        QueuedOrBuilding,
        OnHold,
        ConstructionComplete,
        Cancelled
    }

    [SerializeField] private ConstructionOption constructionOption;
    [SerializeField] private ConstructionQueue constructionQueue;
    public int amount = 1;

    public float timeElapsed;
    private Status buildStatus;

    public ConstructionOption ConstructionOption => constructionOption;
    public float Progress => Mathf.Clamp01(timeElapsed / constructionOption.BuildTime);

    public ConstructionQueueItem(ConstructionQueue constructionQueue, ConstructionOption constructionOption) {
        this.constructionQueue = constructionQueue;
        this.constructionOption = constructionOption;
        buildStatus = Status.QueuedOrBuilding;
    }

    public Status BuildStatus {
        get => buildStatus;
        set {
            if (buildStatus == value)
                return;
            buildStatus = value;
            
            var playerController = constructionQueue.OwningBuilding.OwningPlayer.PlayerController;

            switch (buildStatus) {

                // assume that if it changed to QueuedOrBuilding, it was OnHold, so it mean it is being resumed
                case Status.QueuedOrBuilding: {
                    Debug.Assert(constructionQueue.Contains(this));
                    if (playerController)
                        playerController.NotifyResumeBuilding(this);
                    buildStatus = Status.QueuedOrBuilding;
                    break;
                }

                case Status.OnHold: {
                    Debug.Assert(constructionQueue.Contains(this));
                    if (playerController)
                        playerController.NotifyPutOnHold(this);
                    buildStatus = Status.OnHold;
                    break;
                }

                case Status.Cancelled: {
                    Debug.Assert(constructionQueue.Contains(this));
                    constructionQueue.Remove(this);
                    if (playerController)
                        playerController.NotifyCancelled(this);
                    Invalidate();
                    break;
                }
            }
        }
    }

    public bool IsValid() {
        return buildStatus != Status.Invalid;
    }
    public void Invalidate() {
        buildStatus = Status.Invalid;
    }

    public static implicit operator bool(ConstructionQueueItem item) => item != null && item.IsValid();
}