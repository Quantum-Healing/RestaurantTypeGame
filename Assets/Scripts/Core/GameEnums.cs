namespace KitchenEmpire
{
    public enum GamePhase
    {
        Planning,   // Between days - place machines, buy upgrades, wire power
        DayActive,  // Customers coming, cooking, serving
        DaySummary  // End of day results
    }

    public enum ToolMode
    {
        Select,
        Place,
        Move,
        Wire,
        Demolish,
        Interact // During day - pick up / place items
    }

    public enum MachineCategory
    {
        Basic,
        Cooking,
        Service,
        Storage,
        Utility,
        Automation,
        Power,
        Special
    }

    public enum CustomerState
    {
        Entering,
        WalkingToTable,
        WaitingForFood,
        Eating,
        Leaving,
        LeavingAngry
    }

    public enum IngredientType
    {
        None,
        // Raw
        RawMeat,
        RawFish,
        RawVeggie,
        Flour,
        // Processed
        ChoppedMeat,
        ChoppedVeggie,
        CookedMeat,
        CookedFish,
        CookedVeggie,
        FriedMeat,
        Fries,
        RoastedVeggie,
        Dough,
        Bread,
        Salad,
        // Utility
        DirtyPlate,
        CleanPlate
    }

    public enum MachineType
    {
        Counter,
        Stove,
        PrepTable,
        ServingCounter,
        Table,
        Fridge,
        Oven,
        Fryer,
        Dishwasher,
        Mixer,
        Sink,
        Conveyor,
        Generator,
        Door
    }

    public enum Direction
    {
        Right = 0,
        Down = 1,
        Left = 2,
        Up = 3
    }
}
