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

using System.Collections.Generic;
using UnityEngine;

public class ConstructionQueue : MonoBehaviour {

    [SerializeField] private Building owningBuilding;
    [SerializeField] private List<ConstructionQueueItem> constructionQueue;

    public Building OwningBuilding => owningBuilding;

    public bool Contains(ConstructionQueueItem item) {
        return constructionQueue.Contains(item);
    }

    public void Remove(ConstructionQueueItem item) {
        constructionQueue.Remove(item);
    }

    public ConstructionQueueItem StartBuilding(ConstructionOption constructionOption) {
        Debug.Assert(constructionOption.MeetsPrerequisites(owningBuilding.OwningPlayer));
        var item = new ConstructionQueueItem(this, constructionOption);
        constructionQueue.Add(item);
        if (owningBuilding.OwningPlayer.PlayerController)
            owningBuilding.OwningPlayer.PlayerController.NotifyStartBuilding(item);
        return item;
    }

    private void Update() {
        
        while (constructionQueue.Count > 0 && !constructionQueue[0])
            constructionQueue.RemoveAt(0);
        
        if (constructionQueue.Count > 0) {
            var constructionQueueItem = constructionQueue[0];

            if (constructionQueueItem.BuildStatus == ConstructionQueueItem.Status.QueuedOrBuilding) {

                constructionQueueItem.timeElapsed += Time.deltaTime;

                // just completed a new item
                if (constructionQueueItem.timeElapsed >= constructionQueueItem.ConstructionOption.BuildTime && constructionQueueItem.BuildStatus != ConstructionQueueItem.Status.ConstructionComplete) {
                    constructionQueueItem.BuildStatus = ConstructionQueueItem.Status.ConstructionComplete;

                    if (owningBuilding.OwningPlayer.PlayerController)
                        owningBuilding.OwningPlayer.PlayerController.NotifyConstructionComplete(owningBuilding, constructionQueueItem.ConstructionOption.Prefab);

                    if (constructionQueueItem.ConstructionOption.Prefab is Unit) {
                        constructionQueueItem.amount--;
                        if (constructionQueueItem.amount <= 0) {
                            constructionQueue.Remove(constructionQueueItem);
                            constructionQueueItem.Invalidate();
                        }
                    }
                    else if (constructionQueueItem.ConstructionOption.Prefab is Building) {
                        // do nothing here
                    }
                }
            }
        }
    }
}