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

public class StartBuildingPlacementInGameConsoleCommand : InGameConsoleCommand {
    public StartBuildingPlacementInGameConsoleCommand() : base(nameof(StartBuildingPlacementInGameConsoleCommand)) { }

    public override object Execute(World world, IReadOnlyList<object> arguments) {
        foreach (var constructionOption in world.PlayerController.EnumerateConstructionOptions())
            if (constructionOption.Prefab is Building buildingPrefab) {
                world.PlayerController.StartBuildingPlacement(buildingPrefab);
                break;
            }
        return null;
    }
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