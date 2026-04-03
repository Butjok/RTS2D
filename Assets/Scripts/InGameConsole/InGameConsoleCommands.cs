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

public static class InGameConsoleCommands {

    private const bool autoDiscoverCommands = true;

    static InGameConsoleCommands() {
        if (autoDiscoverCommands) {
            var types = typeof(InGameConsoleCommands).Assembly.GetTypes();
            allCommands = new List<InGameConsoleCommand>();
            foreach (var type in types)
                if (type.IsSubclassOf(typeof(InGameConsoleCommand)) && !type.IsAbstract)
                    allCommands.Add((InGameConsoleCommand)System.Activator.CreateInstance(type));
        }
    }

    // Place here all the commands you want to be available in the in-game console.
    private static readonly List<InGameConsoleCommand> allCommands;

    public static IReadOnlyList<InGameConsoleCommand> Commands => allCommands;
}

public class StopBuildingPlacementInGameConsoleCommand : InGameConsoleCommand {
    public StopBuildingPlacementInGameConsoleCommand() : base(nameof(StopBuildingPlacementInGameConsoleCommand)) { }

    public override object Execute(World world, IReadOnlyList<object> arguments) {
        world.PlayerController.StopBuildingPlacement();
        return null;
    }
}

public class ShowWalkableCellsInGameConsoleCommand : BooleanInGameConsoleCommand {
    public ShowWalkableCellsInGameConsoleCommand() : base(nameof(ShowWalkableCellsInGameConsoleCommand)) { }

    protected override bool GetValue(World world) {
        return world.Grid.showWalkableCells;
    }

    protected override void SetValue(World world, bool value) {
        world.Grid.showWalkableCells = value;
    }
}

public class ShowOccupiedCellsInGameConsoleCommand : BooleanInGameConsoleCommand {
    public ShowOccupiedCellsInGameConsoleCommand() : base(nameof(ShowOccupiedCellsInGameConsoleCommand)) { }

    protected override bool GetValue(World world) {
        return world.Grid.showOccupiedCells;
    }

    protected override void SetValue(World world, bool value) {
        world.Grid.showOccupiedCells = value;
    }
}

public class ShowReservedCellsInGameConsoleCommand : BooleanInGameConsoleCommand {
    public ShowReservedCellsInGameConsoleCommand() : base(nameof(ShowReservedCellsInGameConsoleCommand)) { }

    protected override bool GetValue(World world) {
        return world.Grid.showReservedCells;
    }

    protected override void SetValue(World world, bool value) {
        world.Grid.showReservedCells = value;
    }
}

public class ShowMovePathCellsInGameConsoleCommand : BooleanInGameConsoleCommand {
    public ShowMovePathCellsInGameConsoleCommand() : base(nameof(ShowMovePathCellsInGameConsoleCommand)) { }

    protected override bool GetValue(World world) {
        return world.PlayerController.showMovePathCells;
    }

    protected override void SetValue(World world, bool value) {
        world.PlayerController.showMovePathCells = value;
    }
}